using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

/// <summary>
/// Назначение автомобиля и смена статусов. В учебных данных: 1 — LADA Vesta (4 места), 3 — Hyundai H-1 (7),
/// 4 — Газель (15), 6 — Ford Transit на ремонте, 7 — Renault Logan списан.
/// </summary>
public class DispatchTests
{
    [Test, DisplayName("Свободный автомобиль назначается, заявка переходит в «автомобиль назначен»")]
    public async Task Free_car_is_assigned()
    {
        using var b = new Bench();
        var id = b.NewTrip(10, "09:00", "11:00");
        var verdict = b.Dispatch.AssignCar(id, 1);
        await Assert.That(verdict.Allowed).IsTrue();
        var trip = b.Trips.Find(id)!;
        await Assert.That(trip.Status).IsEqualTo(TripStatus.CarAssigned);
        await Assert.That(trip.CarId).IsEqualTo(1);
        await Assert.That(b.Trips.Log(id).Last().Message).Contains("А214КМ 186");
    }

    [Test, DisplayName("Занятый автомобиль не назначается, в отказе — номер мешающей заявки")]
    public async Task Busy_car_is_refused()
    {
        using var b = new Bench();
        var first = b.AssignedTrip(1, 10, "09:00", "11:00");
        var second = b.NewTrip(10, "10:30", "12:00");
        var verdict = b.Dispatch.AssignCar(second, 1);
        await Assert.That(verdict.Allowed).IsFalse();
        await Assert.That(verdict.Summary).Contains($"ЗТ-{first:0000}");
        await Assert.That(b.StatusOf(second)).IsEqualTo(TripStatus.New);
    }

    [Test, DisplayName("Поездка встык после другой допускается")]
    public async Task Back_to_back_trip_is_allowed()
    {
        using var b = new Bench();
        b.AssignedTrip(1, 10, "09:00", "11:00");
        var next = b.NewTrip(10, "11:00", "12:00");
        await Assert.That(b.Dispatch.AssignCar(next, 1).Allowed).IsTrue();
    }

    [Test, DisplayName("Автомобиль на ремонте и списанный не назначаются")]
    [Arguments(6, "на ремонте")]
    [Arguments(7, "списан")]
    public async Task Car_out_of_service_is_refused(int carId, string word)
    {
        using var b = new Bench();
        var id = b.NewTrip(10, "09:00", "10:00");
        var verdict = b.Dispatch.AssignCar(id, carId);
        await Assert.That(verdict.Summary).Contains(word);
    }

    [Test, DisplayName("Мест в автомобиле должно хватить на всех пассажиров; ровно столько же — можно")]
    public async Task Seats_must_fit_passengers()
    {
        using var b = new Bench();
        var five = b.NewTrip(10, "09:00", "10:00", passengers: 5);
        await Assert.That(b.Dispatch.AssignCar(five, 1).Summary).Contains("пассажирских мест");
        var four = b.NewTrip(10, "12:00", "13:00", passengers: 4);
        await Assert.That(b.Dispatch.AssignCar(four, 1).Allowed).IsTrue();
    }

    [Test, DisplayName("Назначенной заявке нельзя назначить автомобиль повторно")]
    public async Task Assigned_request_cannot_be_assigned_again()
    {
        using var b = new Bench();
        var id = b.AssignedTrip(1, 10, "09:00", "10:00");
        await Assert.That(b.Dispatch.AssignCar(id, 5).Summary).Contains("недоступно");
    }

    [Test, DisplayName("Выезд — не раньше чем за час до начала и не после окончания")]
    public async Task Departure_window()
    {
        using var b = new Bench();
        var id = b.AssignedTrip(1, 10, "09:00", "11:00");
        var start = Bench.Day(10).ToDateTime(new TimeOnly(9, 0));

        b.Clock.LocalNow = start.AddMinutes(-61);
        await Assert.That(b.Dispatch.Depart(id).Summary).Contains("не раньше чем за час");

        b.Clock.LocalNow = start.AddHours(2);
        await Assert.That(b.Dispatch.Depart(id).Summary).Contains("закончилось");

        b.Clock.LocalNow = start.AddMinutes(-60);
        await Assert.That(b.Dispatch.Depart(id).Allowed).IsTrue();
        await Assert.That(b.StatusOf(id)).IsEqualTo(TripStatus.OnRoute);
    }

    [Test, DisplayName("Завершить можно только поездку в пути")]
    public async Task Only_trip_on_route_can_be_completed()
    {
        using var b = new Bench();
        var id = b.AssignedTrip(1, 10, "09:00", "11:00");
        await Assert.That(b.Dispatch.Complete(id).Allowed).IsFalse();
        b.Clock.LocalNow = Bench.Day(10).ToDateTime(new TimeOnly(9, 0));
        b.Dispatch.Depart(id);
        await Assert.That(b.Dispatch.Complete(id).Allowed).IsTrue();
        await Assert.That(b.StatusOf(id)).IsEqualTo(TripStatus.Done);
    }

    [Test, DisplayName("Отклонение и отмена требуют причины не короче 5 символов")]
    public async Task Closing_requires_reason()
    {
        using var b = new Bench();
        var id = b.NewTrip(10, "09:00", "10:00");
        await Assert.That(b.Dispatch.Reject(id, " нет ").Allowed).IsFalse();
        await Assert.That(b.Dispatch.Reject(id, "Нет служебной необходимости").Allowed).IsTrue();
        var trip = b.Trips.Find(id)!;
        await Assert.That(trip.Status).IsEqualTo(TripStatus.Rejected);
        await Assert.That(trip.Resolution).IsEqualTo("Нет служебной необходимости");
    }

    [Test, DisplayName("Отменённая заявка освобождает автомобиль")]
    public async Task Cancelled_request_frees_the_car()
    {
        using var b = new Bench();
        var first = b.AssignedTrip(1, 10, "09:00", "11:00");
        await Assert.That(b.Dispatch.Cancel(first, "Совещание отменено").Allowed).IsTrue();
        var second = b.NewTrip(10, "09:30", "10:30");
        await Assert.That(b.Dispatch.AssignCar(second, 1).Allowed).IsTrue();
    }

    [Test, DisplayName("Снятие автомобиля возвращает заявку в «новая» и освобождает автомобиль")]
    public async Task Removing_car_returns_request_to_new()
    {
        using var b = new Bench();
        var id = b.AssignedTrip(1, 10, "09:00", "11:00");
        await Assert.That(b.Dispatch.RemoveCar(id).Allowed).IsTrue();
        var trip = b.Trips.Find(id)!;
        await Assert.That(trip.Status).IsEqualTo(TripStatus.New);
        await Assert.That(trip.CarId).IsNull();
        await Assert.That(b.Availability.Conflicts(1, trip.Period)).IsEmpty();
    }

    [Test, DisplayName("Прошедшей поездке автомобиль не назначается")]
    public async Task Car_is_not_assigned_to_past_trip()
    {
        using var b = new Bench();
        var id = b.NewTrip(10, "09:00", "10:00");
        b.Clock.LocalNow = Bench.Day(10).ToDateTime(new TimeOnly(10, 30));
        await Assert.That(b.Dispatch.AssignCar(id, 1).Summary).Contains("уже прошло");
    }

    [Test, DisplayName("База не даёт сохранить двойное назначение даже в обход проверки")]
    public async Task Database_trigger_blocks_double_booking()
    {
        using var b = new Bench();
        b.AssignedTrip(1, 10, "09:00", "11:00");
        var other = b.NewTrip(10, "10:00", "12:00");
        var outcome = b.Trips.Move(other, TripStatus.New, TripStatus.CarAssigned, 1, null, "в обход проверки", b.Clock.LocalNow);
        await Assert.That(outcome).IsEqualTo(Web.Data.TripStore.MoveOutcome.CarBusy);
        await Assert.That(b.StatusOf(other)).IsEqualTo(TripStatus.New);
    }

    [Test, DisplayName("Изменение уже изменённой заявки не проходит молча")]
    public async Task Stale_status_is_detected()
    {
        using var b = new Bench();
        var id = b.NewTrip(10, "09:00", "10:00");
        b.Dispatch.Reject(id, "Отклонено первым диспетчером");
        var outcome = b.Trips.Move(id, TripStatus.New, TripStatus.CarAssigned, 1, null, "второй диспетчер", b.Clock.LocalNow);
        await Assert.That(outcome).IsEqualTo(Web.Data.TripStore.MoveOutcome.Stale);
    }
}
