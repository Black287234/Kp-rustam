using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

public class AvailabilityTests
{
    [Test, DisplayName("Подходящие автомобили идут первыми, начиная с наименьшего запаса мест")]
    public async Task Options_are_ordered_by_fit()
    {
        using var b = new Bench();
        var id = b.NewTrip(10, "09:00", "10:00", passengers: 5);
        var options = b.Availability.OptionsFor(b.Trips.Find(id)!);
        var fitting = options.TakeWhile(o => o.Fits).Select(o => o.Car.PassengerSeats).ToList();
        await Assert.That(fitting).IsNotEmpty();
        await Assert.That(fitting).IsInOrder();
        await Assert.That(fitting.Min()).IsGreaterThanOrEqualTo(5);
        await Assert.That(options.Any(o => o.Car.State == CarState.Decommissioned)).IsFalse();
    }

    [Test, DisplayName("У неподходящего автомобиля указана причина")]
    public async Task Obstacles_are_explained()
    {
        using var b = new Bench();
        var busy = b.AssignedTrip(5, 10, "09:00", "11:00");
        var id = b.NewTrip(10, "10:00", "12:00", passengers: 2);
        var options = b.Availability.OptionsFor(b.Trips.Find(id)!).ToDictionary(o => o.Car.Id);
        await Assert.That(options[6].Obstacle).IsEqualTo("на ремонте");
        await Assert.That(options[5].Obstacle!).Contains($"ЗТ-{busy:0000}");
        await Assert.That(options[2].Fits).IsTrue();
    }

    [Test, DisplayName("Свободные автомобили на промежуток: без занятых, ремонтных и маловместительных")]
    public async Task Free_cars_exclude_busy_and_small()
    {
        using var b = new Bench();
        b.AssignedTrip(3, 10, "09:00", "12:00", passengers: 6);
        var day = Bench.Day(10);
        var free = b.Availability.FreeCars(new Period(day.ToDateTime(new TimeOnly(10, 0)), day.ToDateTime(new TimeOnly(11, 0))), 5);
        await Assert.That(free.Select(c => c.Id)).IsEquivalentTo([8, 4]); // KIA Carnival (7) и Газель (15)
    }

    [Test, DisplayName("График дня показывает поездки по автомобилям")]
    public async Task Board_groups_trips_by_car()
    {
        using var b = new Bench();
        var id = b.AssignedTrip(4, 12, "08:00", "10:00", passengers: 10);
        var line = b.Availability.Board(Bench.Day(12)).Single(l => l.Car.Id == 4);
        await Assert.That(line.Trips.Select(t => t.Id)).Contains(id);
        await Assert.That(b.Availability.Board(Bench.Day(12)).Any(l => l.Car.Id == 7)).IsFalse();
    }

    [Test, DisplayName("Закреплённые за автомобилем поездки: только предстоящие назначенные и в пути")]
    public async Task Commitments_list_only_upcoming_holds()
    {
        using var b = new Bench();
        var future = b.AssignedTrip(5, 10, "09:00", "10:00");
        var cancelled = b.AssignedTrip(5, 11, "09:00", "10:00");
        b.Dispatch.Cancel(cancelled, "Поездка не нужна");
        var held = b.Availability.Commitments(5, b.Clock.LocalNow).Select(t => t.Id).ToList();
        await Assert.That(held).IsEquivalentTo([future]);
    }
}
