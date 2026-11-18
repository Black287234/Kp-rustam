using Dapper;
using Microsoft.Data.Sqlite;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Data;

/// <summary>Чтение и запись заявок на поездки и их журнала.</summary>
public sealed class TripStore(SqliteDatabase database)
{
    private const string Select = """
        SELECT r.id, r.requester_id, e.full_name AS requester_name, e.department AS requester_department,
               r.starts_at, r.ends_at, r.origin, r.destination, r.purpose, r.passengers, r.status,
               r.car_id, c.plate AS car_plate, c.make || ' ' || c.model AS car_title, r.created_at, r.resolution
        FROM trip_request r
        JOIN employee e ON e.id = r.requester_id
        LEFT JOIN car c ON c.id = r.car_id
        """;

    /// <summary>Строка таблицы trip_request с временем в виде текста.</summary>
    private sealed class Row
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = "";
        public string RequesterDepartment { get; set; } = "";
        public string StartsAt { get; set; } = "";
        public string EndsAt { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Purpose { get; set; } = "";
        public int Passengers { get; set; }
        public int Status { get; set; }
        public int? CarId { get; set; }
        public string? CarPlate { get; set; }
        public string? CarTitle { get; set; }
        public string CreatedAt { get; set; } = "";
        public string? Resolution { get; set; }

        public TripRequest ToDomain() => new()
        {
            Id = Id, RequesterId = RequesterId, RequesterName = RequesterName, RequesterDepartment = RequesterDepartment,
            StartsAt = Stamp.FromDb(StartsAt), EndsAt = Stamp.FromDb(EndsAt),
            Origin = Origin, Destination = Destination, Purpose = Purpose, Passengers = Passengers,
            Status = (TripStatus)Status, CarId = CarId, CarPlate = CarPlate, CarTitle = CarTitle,
            CreatedAt = Stamp.FromDb(CreatedAt), Resolution = Resolution
        };
    }

    public IReadOnlyList<TripRequest> List(TripFilter? filter = null)
    {
        filter ??= new TripFilter();
        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter.Text))
            where.Add("(fold(r.origin) LIKE fold(@Like) OR fold(r.destination) LIKE fold(@Like) OR fold(r.purpose) LIKE fold(@Like) OR fold(e.full_name) LIKE fold(@Like))");
        if (filter.Status is not null) where.Add("r.status = @Status");
        if (filter.DayFrom is not null) where.Add("r.starts_at >= @From");
        if (filter.DayTo is not null) where.Add("r.starts_at < @To");
        if (filter.CarId is not null) where.Add("r.car_id = @CarId");
        if (filter.OpenOnly) where.Add("r.status IN (1, 2, 3)");

        var sql = Select + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + " ORDER BY r.starts_at DESC, r.id DESC";
        using var db = database.Open();
        return db.Query<Row>(sql, new
        {
            Like = $"%{filter.Text?.Trim()}%",
            Status = (int?)filter.Status,
            From = filter.DayFrom is { } f ? Stamp.ToDb(f.ToDateTime(TimeOnly.MinValue)) : null,
            To = filter.DayTo is { } t ? Stamp.ToDb(t.AddDays(1).ToDateTime(TimeOnly.MinValue)) : null,
            filter.CarId
        }).Select(r => r.ToDomain()).ToList();
    }

    /// <summary>Поездки, которые занимают автомобиль и пересекаются с промежутком.</summary>
    public IReadOnlyList<TripRequest> HoldingCar(int carId, Period period, int exceptRequestId = 0)
    {
        using var db = database.Open();
        return db.Query<Row>(Select + """
             WHERE r.car_id = @carId AND r.status IN (2, 3) AND r.id <> @exceptRequestId
               AND r.starts_at < @End AND @Start < r.ends_at
             ORDER BY r.starts_at
            """, new { carId, exceptRequestId, Start = Stamp.ToDb(period.Start), End = Stamp.ToDb(period.End) })
            .Select(r => r.ToDomain()).ToList();
    }

    /// <summary>Поездки с автомобилем (назначенные, в пути и выполненные), пересекающиеся с промежутком, — для графика.</summary>
    public IReadOnlyList<TripRequest> WithCarWithin(Period period)
    {
        using var db = database.Open();
        return db.Query<Row>(Select + """
             WHERE r.car_id IS NOT NULL AND r.status IN (2, 3, 4)
               AND r.starts_at < @End AND @Start < r.ends_at
             ORDER BY r.starts_at
            """, new { Start = Stamp.ToDb(period.Start), End = Stamp.ToDb(period.End) })
            .Select(r => r.ToDomain()).ToList();
    }

    /// <summary>
    /// Меняет статус заявки, если она всё ещё в ожидаемом статусе, и пишет запись в журнал.
    /// Stale — заявку успели изменить; CarBusy — база отказала из-за занятости автомобиля.
    /// </summary>
    public MoveOutcome Move(int id, TripStatus expected, TripStatus next, int? carId, string? resolution, string message, DateTime now)
    {
        using var db = database.Open();
        using var tx = db.BeginTransaction();
        try
        {
            var changed = db.Execute("""
                UPDATE trip_request SET status = @Next, car_id = @carId, resolution = @resolution
                WHERE id = @id AND status = @Expected
                """, new { id, Expected = (int)expected, Next = (int)next, carId, resolution }, tx);
            if (changed == 0) return MoveOutcome.Stale;
            WriteLog(db, tx, id, now, next, message);
            tx.Commit();
            return MoveOutcome.Done;
        }
        catch (SqliteException ex) when (ex.Message.Contains("car_busy"))
        {
            return MoveOutcome.CarBusy;
        }
    }

    public TripRequest? Find(int id)
    {
        using var db = database.Open();
        return db.QuerySingleOrDefault<Row>(Select + " WHERE r.id = @id", new { id })?.ToDomain();
    }

    public IReadOnlyList<TripLogEntry> Log(int requestId)
    {
        using var db = database.Open();
        return db.Query<(string At, int Status, string Message)>(
                "SELECT logged_at, status, message FROM trip_log WHERE request_id = @requestId ORDER BY logged_at, id",
                new { requestId })
            .Select(x => new TripLogEntry(Stamp.FromDb(x.At), (TripStatus)x.Status, x.Message))
            .ToList();
    }

    public int Add(TripRequest r, DateTime now)
    {
        using var db = database.Open();
        using var tx = db.BeginTransaction();
        var id = db.ExecuteScalar<int>("""
            INSERT INTO trip_request (requester_id, starts_at, ends_at, origin, destination, purpose, passengers, status, created_at)
            VALUES (@RequesterId, @StartsAt, @EndsAt, @Origin, @Destination, @Purpose, @Passengers, 1, @CreatedAt);
            SELECT last_insert_rowid();
            """, Params(r, now), tx);
        WriteLog(db, tx, id, now, TripStatus.New, "Заявка создана");
        tx.Commit();
        return id;
    }

    public void Update(TripRequest r, DateTime now)
    {
        using var db = database.Open();
        using var tx = db.BeginTransaction();
        db.Execute("""
            UPDATE trip_request SET requester_id = @RequesterId, starts_at = @StartsAt, ends_at = @EndsAt,
                origin = @Origin, destination = @Destination, purpose = @Purpose, passengers = @Passengers
            WHERE id = @Id
            """, Params(r, now), tx);
        WriteLog(db, tx, r.Id, now, r.Status, "Изменены данные заявки");
        tx.Commit();
    }

    private static object Params(TripRequest r, DateTime now) => new
    {
        r.Id, r.RequesterId, StartsAt = Stamp.ToDb(r.StartsAt), EndsAt = Stamp.ToDb(r.EndsAt),
        r.Origin, r.Destination, r.Purpose, r.Passengers, CreatedAt = Stamp.ToDb(now)
    };

    public enum MoveOutcome { Done, Stale, CarBusy }

    internal static void WriteLog(SqliteConnection db, SqliteTransaction tx, int requestId, DateTime at, TripStatus status, string message) =>
        db.Execute(
            "INSERT INTO trip_log (request_id, logged_at, status, message) VALUES (@requestId, @At, @status, @message)",
            new { requestId, At = Stamp.ToDb(at), status = (int)status, message }, tx);
}
