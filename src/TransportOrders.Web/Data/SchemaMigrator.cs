using System.Reflection;
using Dapper;

namespace TransportOrders.Web.Data;

/// <summary>
/// Применяет SQL-скрипты из папки Data/Migrations по порядку номеров.
/// Выполненные скрипты записываются в таблицу schema_history и повторно не запускаются.
/// </summary>
public sealed class SchemaMigrator(SqliteDatabase database, ILogger<SchemaMigrator> log)
{
    private const string Prefix = "TransportOrders.Web.Data.Migrations.";

    public void Run()
    {
        using var connection = database.Open();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS schema_history (
                version    INTEGER PRIMARY KEY,
                script     TEXT NOT NULL,
                applied_at TEXT NOT NULL)
            """);
        var done = connection.Query<long>("SELECT version FROM schema_history").ToHashSet();

        var assembly = typeof(SchemaMigrator).Assembly;
        var scripts = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal))
            .Select(n => (Resource: n, Name: n[Prefix.Length..], Version: long.Parse(n[Prefix.Length..].Split('_')[0])))
            .OrderBy(s => s.Version);

        foreach (var script in scripts.Where(s => !done.Contains(s.Version)))
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(Read(assembly, script.Resource), transaction: transaction);
            connection.Execute(
                "INSERT INTO schema_history (version, script, applied_at) VALUES (@Version, @Name, @At)",
                new { script.Version, script.Name, At = Stamp.ToDb(DateTime.Now) }, transaction);
            transaction.Commit();
            log.LogInformation("Применён скрипт базы данных {Script}", script.Name);
        }
    }

    private static string Read(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
