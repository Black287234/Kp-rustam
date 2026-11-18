using Microsoft.Extensions.Logging.Abstractions;
using TransportOrders.Web.Data;
using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

/// <summary>
/// Испытательный стенд: отдельный файл базы SQLite со всеми миграциями, хранилища и сервисы модуля.
/// Часы остановлены на 07:00 сегодняшнего дня — учебные данные миграции 0002 тоже считаются от сегодняшнего дня.
/// Собственные заявки тесты ставят на дальние даты (через 10 дней и позже), чтобы не пересекаться с учебными.
/// </summary>
public sealed class Bench : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"transport-test-{Guid.NewGuid():N}.db");

    public Bench()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        Database = new SqliteDatabase($"Data Source={_file};Pooling=False");
        new SchemaMigrator(Database, NullLogger<SchemaMigrator>.Instance).Run();
        Clock = new StoppedClock(DateTime.Today.AddHours(7));
        Cars = new CarStore(Database);
        Staff = new EmployeeStore(Database);
        Trips = new TripStore(Database);
        Availability = new CarAvailability(Cars, Trips);
        Requests = new RequestDesk(Trips, Staff, Clock);
        Dispatch = new DispatchDesk(Trips, Cars, Availability, Clock);
    }

    public SqliteDatabase Database { get; }
    public StoppedClock Clock { get; }
    public CarStore Cars { get; }
    public EmployeeStore Staff { get; }
    public TripStore Trips { get; }
    public CarAvailability Availability { get; }
    public RequestDesk Requests { get; }
    public DispatchDesk Dispatch { get; }

    /// <summary>День через <paramref name="days"/> дней от сегодняшнего.</summary>
    public static DateOnly Day(int days) => DateOnly.FromDateTime(DateTime.Today.AddDays(days));

    /// <summary>Подаёт заявку от сотрудника № 3 и возвращает её номер; падает, если заявка не принята.</summary>
    public int NewTrip(int dayOffset, string from, string to, int passengers = 1, int requesterId = 3)
    {
        var (verdict, id) = Requests.Register(new TripDraft(requesterId, Day(dayOffset), TimeOnly.Parse(from), TimeOnly.Parse(to),
            "ул. Ленина, 41", $"Пункт {Guid.NewGuid():N}"[..14], "Служебная поездка для теста", passengers));
        if (!verdict.Allowed) throw new InvalidOperationException(verdict.Summary);
        return id;
    }

    /// <summary>Заявка с назначенным автомобилем.</summary>
    public int AssignedTrip(int carId, int dayOffset, string from, string to, int passengers = 1)
    {
        var id = NewTrip(dayOffset, from, to, passengers);
        var verdict = Dispatch.AssignCar(id, carId);
        if (!verdict.Allowed) throw new InvalidOperationException(verdict.Summary);
        return id;
    }

    public TripStatus StatusOf(int id) => Trips.Find(id)!.Status;

    public void Dispose()
    {
        foreach (var f in new[] { _file, _file + "-wal", _file + "-shm" })
            if (File.Exists(f)) File.Delete(f);
    }
}
