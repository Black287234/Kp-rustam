using Dapper;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Data;

/// <summary>Чтение и запись справочника сотрудников.</summary>
public sealed class EmployeeStore(SqliteDatabase database)
{
    private const string Columns = "id, personnel_no, full_name, department, position, phone, is_active";

    public IReadOnlyList<Employee> List(string? text = null, bool onlyActive = false)
    {
        using var db = database.Open();
        var sql = $"SELECT {Columns} FROM employee WHERE 1 = 1";
        if (!string.IsNullOrWhiteSpace(text))
            sql += " AND (fold(full_name) LIKE fold(@Like) OR personnel_no LIKE @Like OR fold(department) LIKE fold(@Like))";
        if (onlyActive)
            sql += " AND is_active = 1";
        sql += " ORDER BY is_active DESC, full_name";
        return db.Query<Employee>(sql, new { Like = $"%{text?.Trim()}%" }).ToList();
    }

    public Employee? Find(int id)
    {
        using var db = database.Open();
        return db.QuerySingleOrDefault<Employee>($"SELECT {Columns} FROM employee WHERE id = @id", new { id });
    }

    public bool PersonnelNoTaken(string personnelNo, int exceptId = 0)
    {
        using var db = database.Open();
        return db.ExecuteScalar<long>(
            "SELECT count(*) FROM employee WHERE personnel_no = @personnelNo AND id <> @exceptId",
            new { personnelNo, exceptId }) > 0;
    }

    public int Add(Employee e)
    {
        using var db = database.Open();
        return db.ExecuteScalar<int>("""
            INSERT INTO employee (personnel_no, full_name, department, position, phone, is_active)
            VALUES (@PersonnelNo, @FullName, @Department, @Position, @Phone, @IsActive);
            SELECT last_insert_rowid();
            """, e);
    }

    public void Update(Employee e)
    {
        using var db = database.Open();
        db.Execute("""
            UPDATE employee SET personnel_no = @PersonnelNo, full_name = @FullName, department = @Department,
                position = @Position, phone = @Phone, is_active = @IsActive
            WHERE id = @Id
            """, e);
    }
}
