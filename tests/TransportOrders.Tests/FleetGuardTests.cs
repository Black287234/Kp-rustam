using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

/// <summary>Закрепляет исправление: у автомобиля с закреплёнными поездками нельзя уменьшить число мест ниже нужного.</summary>
public class FleetGuardTests
{
    private static Car Changed(Bench b, int id, Action<Car> change)
    {
        var car = b.Cars.Find(id)!;
        change(car);
        return car;
    }

    [Test, DisplayName("Нельзя уменьшить число мест ниже числа пассажиров закреплённой поездки")]
    public async Task Seats_cannot_drop_below_held_passengers()
    {
        using var b = new Bench();
        var trip = b.AssignedTrip(4, 10, "08:00", "12:00", passengers: 12);
        var guard = new FleetGuard(b.Availability, b.Clock);
        var verdict = guard.CheckChange(Changed(b, 4, c => c.PassengerSeats = 4));
        await Assert.That(verdict.Allowed).IsFalse();
        await Assert.That(verdict.Summary).Contains($"ЗТ-{trip:0000} — 12 чел.");
        await Assert.That(guard.CheckChange(Changed(b, 4, c => c.PassengerSeats = 12)).Allowed).IsTrue();
    }

    [Test, DisplayName("Нельзя отправить в ремонт автомобиль с закреплённой поездкой")]
    public async Task Car_with_trips_cannot_go_to_repair()
    {
        using var b = new Bench();
        b.AssignedTrip(5, 10, "09:00", "10:00");
        var guard = new FleetGuard(b.Availability, b.Clock);
        await Assert.That(guard.CheckChange(Changed(b, 5, c => c.State = CarState.InRepair)).Summary).Contains("закреплены поездки");
    }

    [Test, DisplayName("Автомобиль без поездок можно менять свободно")]
    public async Task Free_car_can_change()
    {
        using var b = new Bench();
        var guard = new FleetGuard(b.Availability, b.Clock);
        await Assert.That(guard.CheckChange(Changed(b, 8, c => { c.PassengerSeats = 2; c.State = CarState.InRepair; })).Allowed).IsTrue();
    }
}
