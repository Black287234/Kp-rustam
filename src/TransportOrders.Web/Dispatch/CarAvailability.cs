using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Dispatch;

/// <summary>Вариант автомобиля для заявки: подходит ли он и если нет — почему.</summary>
public sealed record CarOption(Car Car, bool Fits, string? Obstacle);

/// <summary>Строка графика занятости: автомобиль и его поездки за день.</summary>
public sealed record BoardLine(Car Car, IReadOnlyList<TripRequest> Trips);

/// <summary>Проверка занятости автомобилей и подбор свободного автомобиля под заявку.</summary>
public sealed class CarAvailability(CarStore cars, TripStore trips)
{
    /// <summary>Поездки, из-за которых автомобиль занят в указанный промежуток.</summary>
    public IReadOnlyList<TripRequest> Conflicts(int carId, Period period, int exceptRequestId = 0) =>
        trips.HoldingCar(carId, period, exceptRequestId);

    /// <summary>
    /// Все действующие автомобили с отметкой, подходят ли они под заявку.
    /// Подходящие идут первыми, среди них — с наименьшим запасом мест, чтобы не занимать большой автомобиль зря.
    /// </summary>
    public IReadOnlyList<CarOption> OptionsFor(TripRequest trip)
    {
        var options = new List<CarOption>();
        foreach (var car in cars.List().Where(c => c.State != CarState.Decommissioned))
        {
            string? obstacle = null;
            if (car.State == CarState.InRepair)
                obstacle = "на ремонте";
            else if (car.PassengerSeats < trip.Passengers)
                obstacle = $"мест {car.PassengerSeats}, а пассажиров {trip.Passengers}";
            else if (Conflicts(car.Id, trip.Period, trip.Id) is { Count: > 0 } busy)
                obstacle = "занят: " + string.Join("; ", busy.Select(b => $"{b.Number} {b.StartsAt:HH:mm}–{b.EndsAt:HH:mm}"));
            options.Add(new CarOption(car, obstacle is null, obstacle));
        }
        return options
            .OrderByDescending(o => o.Fits)
            .ThenBy(o => o.Car.PassengerSeats - trip.Passengers)
            .ThenBy(o => o.Car.Plate, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Свободные исправные автомобили на промежуток с нужным числом мест.</summary>
    public IReadOnlyList<Car> FreeCars(Period period, int passengers) =>
        cars.List(state: CarState.Operational)
            .Where(c => c.PassengerSeats >= passengers && Conflicts(c.Id, period).Count == 0)
            .OrderBy(c => c.PassengerSeats)
            .ThenBy(c => c.Plate, StringComparer.Ordinal)
            .ToList();

    /// <summary>График занятости всех действующих автомобилей на день.</summary>
    public IReadOnlyList<BoardLine> Board(DateOnly day)
    {
        var dayPeriod = new Period(day.ToDateTime(TimeOnly.MinValue), day.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var busy = trips.WithCarWithin(dayPeriod).ToLookup(t => t.CarId!.Value);
        return cars.List()
            .Where(c => c.State != CarState.Decommissioned)
            .OrderBy(c => c.Plate, StringComparer.Ordinal)
            .Select(c => new BoardLine(c, busy[c.Id].ToList()))
            .ToList();
    }

    /// <summary>Предстоящие поездки, за которыми закреплён автомобиль (назначен и ещё не закончились, либо в пути).</summary>
    public IReadOnlyList<TripRequest> Commitments(int carId, DateTime now) =>
        trips.List(new TripFilter { CarId = carId, OpenOnly = true })
            .Where(t => TripLifecycle.HoldsCar(t.Status) && (t.Status == TripStatus.OnRoute || t.EndsAt > now))
            .OrderBy(t => t.StartsAt)
            .ToList();
}
