using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

public class RequestDeskTests
{
    private static TripDraft Draft(int dayOffset = 10, string from = "09:00", string to = "11:00", int passengers = 2,
        int requester = 3, string origin = "ул. Ленина, 41", string destination = "Аэропорт Сургут") =>
        new(requester, Bench.Day(dayOffset), TimeOnly.Parse(from), TimeOnly.Parse(to), origin, destination, "Встреча гостей", passengers);

    [Test, DisplayName("Правильная заявка регистрируется со статусом «новая» и записью в журнале")]
    public async Task Valid_request_is_registered()
    {
        using var b = new Bench();
        var (verdict, id) = b.Requests.Register(Draft());
        await Assert.That(verdict.Allowed).IsTrue();
        var trip = b.Trips.Find(id)!;
        await Assert.That(trip.Status).IsEqualTo(TripStatus.New);
        await Assert.That(trip.StartsAt).IsEqualTo(Bench.Day(10).ToDateTime(new TimeOnly(9, 0)));
        await Assert.That(b.Trips.Log(id).Single().Message).IsEqualTo("Заявка создана");
    }

    [Test, DisplayName("Поездка на прошедшее время отклоняется")]
    public async Task Past_trip_is_refused()
    {
        using var b = new Bench();
        var verdict = b.Requests.Check(Draft(dayOffset: -1));
        await Assert.That(verdict.Allowed).IsFalse();
        await Assert.That(verdict.Summary).Contains("прошедшее время");
    }

    [Test, DisplayName("Время возвращения не позже выезда — отказ")]
    [Arguments("11:00", "11:00")]
    [Arguments("15:00", "09:00")]
    public async Task Return_must_be_after_departure(string from, string to)
    {
        using var b = new Bench();
        var verdict = b.Requests.Check(Draft(from: from, to: to));
        await Assert.That(verdict.Reasons.Any(r => r.Field == nameof(TripDraft.To))).IsTrue();
    }

    [Test, DisplayName("Граница длительности: ровно 12 часов можно, больше — нельзя")]
    public async Task Twelve_hours_is_the_limit()
    {
        using var b = new Bench();
        await Assert.That(b.Requests.Check(Draft(from: "06:00", to: "18:00")).Allowed).IsTrue();
        await Assert.That(b.Requests.Check(Draft(from: "06:00", to: "18:01")).Allowed).IsFalse();
    }

    [Test, DisplayName("Число пассажиров от 1 до 30")]
    [Arguments(0, false)]
    [Arguments(1, true)]
    [Arguments(30, true)]
    [Arguments(31, false)]
    public async Task Passengers_range(int passengers, bool allowed)
    {
        using var b = new Bench();
        await Assert.That(b.Requests.Check(Draft(passengers: passengers)).Allowed).IsEqualTo(allowed);
    }

    [Test, DisplayName("Пункт назначения не может совпадать с пунктом отправления")]
    public async Task Destination_differs_from_origin()
    {
        using var b = new Bench();
        var verdict = b.Requests.Check(Draft(origin: "Склад", destination: "  склад "));
        await Assert.That(verdict.Summary).Contains("совпадает");
    }

    [Test, DisplayName("Уволенный сотрудник не подаёт заявки")]
    public async Task Dismissed_employee_cannot_request()
    {
        using var b = new Bench();
        var verdict = b.Requests.Check(Draft(requester: 10)); // Белоусов в учебных данных уволен
        await Assert.That(verdict.Summary).Contains("уволен");
    }

    [Test, DisplayName("Несуществующий заявитель — отказ")]
    public async Task Unknown_requester_is_refused()
    {
        using var b = new Bench();
        await Assert.That(b.Requests.Check(Draft(requester: 999)).Allowed).IsFalse();
    }

    [Test, DisplayName("Новую заявку можно изменить, назначенную — нельзя")]
    public async Task Only_new_request_can_be_amended()
    {
        using var b = new Bench();
        var id = b.NewTrip(11, "10:00", "11:00");
        await Assert.That(b.Requests.Amend(id, Draft(dayOffset: 11, from: "10:00", to: "12:00")).Allowed).IsTrue();
        await Assert.That(b.Trips.Find(id)!.EndsAt.Hour).IsEqualTo(12);

        b.Dispatch.AssignCar(id, 1);
        var verdict = b.Requests.Amend(id, Draft(dayOffset: 11, from: "13:00", to: "14:00"));
        await Assert.That(verdict.Summary).Contains("изменить нельзя");
    }
}
