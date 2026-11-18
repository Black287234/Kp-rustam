using Dapper;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Data;

/// <summary>Чтение и запись справочника автомобилей.</summary>
public sealed class CarStore(SqliteDatabase database)
{
    private const string Columns = "id, plate, make, model, kind, passenger_seats, release_year, state";

    public IReadOnlyList<Car> List(string? text = null, CarState? state = null)
    {
        using var db = database.Open();
        var sql = $"SELECT {Columns} FROM car WHERE 1 = 1";
        if (!string.IsNullOrWhiteSpace(text))
            sql += " AND (fold(plate) LIKE fold(@Like) OR fold(make) LIKE fold(@Like) OR fold(model) LIKE fold(@Like))";
        if (state is not null)
            sql += " AND state = @State";
        sql += " ORDER BY state, plate";
        return db.Query<Car>(sql, new { Like = $"%{text?.Trim()}%", State = (int?)state }).ToList();
    }

    public Car? Find(int id)
    {
        using var db = database.Open();
        return db.QuerySingleOrDefault<Car>($"SELECT {Columns} FROM car WHERE id = @id", new { id });
    }

    /// <summary>Есть ли другой автомобиль с таким госномером (без учёта регистра).</summary>
    public bool PlateTaken(string plate, int exceptId = 0)
    {
        using var db = database.Open();
        return db.ExecuteScalar<long>(
            "SELECT count(*) FROM car WHERE plate = @plate COLLATE NOCASE AND id <> @exceptId",
            new { plate, exceptId }) > 0;
    }

    public int Add(Car car)
    {
        using var db = database.Open();
        return db.ExecuteScalar<int>("""
            INSERT INTO car (plate, make, model, kind, passenger_seats, release_year, state)
            VALUES (@Plate, @Make, @Model, @Kind, @PassengerSeats, @ReleaseYear, @State);
            SELECT last_insert_rowid();
            """, car);
    }

    public void Update(Car car)
    {
        using var db = database.Open();
        db.Execute("""
            UPDATE car SET plate = @Plate, make = @Make, model = @Model, kind = @Kind,
                passenger_seats = @PassengerSeats, release_year = @ReleaseYear, state = @State
            WHERE id = @Id
            """, car);
    }
}
