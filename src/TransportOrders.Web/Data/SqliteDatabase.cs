using Microsoft.Data.Sqlite;

namespace TransportOrders.Web.Data;

/// <summary>Выдаёт открытые соединения с файлом базы SQLite.</summary>
public sealed class SqliteDatabase
{
    private readonly string _connectionString;

    public SqliteDatabase(string connectionString)
    {
        // Проверка внешних ключей в SQLite включается для каждого соединения
        _connectionString = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true }.ToString();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        // Встроенные LIKE и lower() в SQLite не учитывают регистр только у латиницы,
        // поэтому для поиска по-русски регистрируется своя функция приведения к нижнему регистру
        connection.CreateFunction("fold", (string? text) => text?.ToLowerInvariant(), isDeterministic: true);
        return connection;
    }
}
