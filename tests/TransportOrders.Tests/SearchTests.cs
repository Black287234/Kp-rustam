using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

/// <summary>Поиск и отбор. Первая проверка закрепляет ошибку этапа 2: поиск по-русски зависел от регистра.</summary>
public class SearchTests
{
    [Test, DisplayName("Поиск автомобиля по-русски не зависит от регистра")]
    [Arguments("газ")]
    [Arguments("ГАЗ")]
    [Arguments("Газель")]
    public async Task Car_search_ignores_case(string text)
    {
        using var b = new Bench();
        await Assert.That(b.Cars.List(text).Select(c => c.Plate)).Contains("К408СУ 186");
    }

    [Test, DisplayName("Поиск сотрудника по части ФИО в нижнем регистре")]
    public async Task Employee_search_ignores_case()
    {
        using var b = new Bench();
        await Assert.That(b.Staff.List("иванова").Single().PersonnelNo).IsEqualTo("10231");
    }

    [Test, DisplayName("Отбор заявок по статусу, автомобилю и датам")]
    public async Task Trip_filter_combines_conditions()
    {
        using var b = new Bench();
        var id = b.AssignedTrip(8, 15, "09:00", "10:00");
        b.NewTrip(15, "11:00", "12:00");
        var found = b.Trips.List(new TripFilter { Status = TripStatus.CarAssigned, CarId = 8, DayFrom = Bench.Day(15), DayTo = Bench.Day(15) });
        await Assert.That(found.Select(t => t.Id)).IsEquivalentTo([id]);
        await Assert.That(b.Trips.List(new TripFilter { DayFrom = Bench.Day(16) })).IsEmpty();
    }

    [Test, DisplayName("«Только незакрытые» скрывает выполненные, отклонённые и отменённые")]
    public async Task Open_only_hides_closed_requests()
    {
        using var b = new Bench();
        var open = b.Trips.List(new TripFilter { OpenOnly = true });
        await Assert.That(open.All(t => !TripLifecycle.IsClosed(t.Status))).IsTrue();
        await Assert.That(open.Count).IsLessThan(b.Trips.List().Count);
    }
}
