using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Dispatch;

/// <summary>Работа диспетчера с заявкой: назначение автомобиля и смена статуса.</summary>
public sealed class DispatchDesk(TripStore trips, CarStore cars, CarAvailability availability, TimeProvider clock)
{
    /// <summary>За сколько до начала поездки можно отметить выезд.</summary>
    public static readonly TimeSpan EarliestDeparture = TimeSpan.FromHours(1);

    private DateTime Now => clock.GetLocalNow().DateTime;

    public Verdict AssignCar(int requestId, int carId)
    {
        var (trip, refusal) = Load(requestId, TripAction.AssignCar);
        if (trip is null) return refusal!;
        if (trip.EndsAt <= Now)
            return Verdict.Refuse("Время поездки уже прошло — назначать автомобиль поздно");

        var car = cars.Find(carId);
        if (car is null) return Verdict.Refuse("Автомобиль не найден", "carId");
        if (car.State != CarState.Operational)
            return Verdict.Refuse($"Автомобиль {car.Plate} {car.State.Text()} и не может выйти в рейс", "carId");
        if (car.PassengerSeats < trip.Passengers)
            return Verdict.Refuse($"В автомобиле {car.Plate} {car.PassengerSeats} пассажирских мест, а едут {trip.Passengers}", "carId");

        var busy = availability.Conflicts(carId, trip.Period, trip.Id);
        if (busy.Count > 0)
            return Verdict.Refuse(
                $"Автомобиль {car.Plate} занят в это время: " +
                string.Join("; ", busy.Select(b => $"заявка {b.Number}, {b.StartsAt:dd.MM HH:mm}–{b.EndsAt:HH:mm}")), "carId");

        return Apply(trip, TripAction.AssignCar, carId, null, $"Назначен автомобиль {car.Plate} ({car.Title})");
    }

    public Verdict RemoveCar(int requestId)
    {
        var (trip, refusal) = Load(requestId, TripAction.RemoveCar);
        if (trip is null) return refusal!;
        return Apply(trip, TripAction.RemoveCar, null, null, $"Автомобиль {trip.CarPlate} снят с заявки");
    }

    public Verdict Depart(int requestId)
    {
        var (trip, refusal) = Load(requestId, TripAction.Depart);
        if (trip is null) return refusal!;
        if (Now < trip.StartsAt - EarliestDeparture)
            return Verdict.Refuse($"Выезд можно отметить не раньше чем за час до начала поездки ({trip.StartsAt - EarliestDeparture:dd.MM HH:mm})");
        if (Now >= trip.EndsAt)
            return Verdict.Refuse("Время поездки уже закончилось — отметить выезд нельзя, заявку можно отменить");
        var car = cars.Find(trip.CarId!.Value)!;
        if (car.State != CarState.Operational)
            return Verdict.Refuse($"Автомобиль {car.Plate} {car.State.Text()} — снимите его с заявки и назначьте другой");
        return Apply(trip, TripAction.Depart, trip.CarId, null, $"Автомобиль {trip.CarPlate} выехал");
    }

    public Verdict Complete(int requestId)
    {
        var (trip, refusal) = Load(requestId, TripAction.Complete);
        if (trip is null) return refusal!;
        return Apply(trip, TripAction.Complete, trip.CarId, null, "Поездка выполнена");
    }

    public Verdict Reject(int requestId, string? reason) => Close(requestId, TripAction.Reject, reason, "Отклонена");

    public Verdict Cancel(int requestId, string? reason) => Close(requestId, TripAction.Cancel, reason, "Отменена");

    private Verdict Close(int requestId, TripAction action, string? reason, string verb)
    {
        var (trip, refusal) = Load(requestId, action);
        if (trip is null) return refusal!;
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length < 5)
            return Verdict.Refuse("Укажите причину — не короче 5 символов", "reason");
        return Apply(trip, action, trip.CarId, reason, $"{verb}: {reason}");
    }

    /// <summary>Загружает заявку и проверяет, что действие допустимо в её текущем статусе.</summary>
    private (TripRequest? Trip, Verdict? Refusal) Load(int requestId, TripAction action)
    {
        var trip = trips.Find(requestId);
        if (trip is null) return (null, Verdict.Refuse("Заявка не найдена"));
        if (TripLifecycle.After(trip.Status, action) is null)
            return (null, Verdict.Refuse($"Действие «{action.Text()}» недоступно для заявки в статусе «{trip.Status.Text()}»"));
        return (trip, null);
    }

    private Verdict Apply(TripRequest trip, TripAction action, int? carId, string? resolution, string message)
    {
        var next = TripLifecycle.After(trip.Status, action)!.Value;
        return trips.Move(trip.Id, trip.Status, next, carId, resolution, message, Now) switch
        {
            TripStore.MoveOutcome.Done => Verdict.Ok(),
            TripStore.MoveOutcome.CarBusy => Verdict.Refuse("Автомобиль только что занят другой заявкой — выберите другой", "carId"),
            _ => Verdict.Refuse("Заявку за это время изменил другой пользователь — обновите страницу")
        };
    }
}
