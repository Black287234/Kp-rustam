namespace TransportOrders.Tests;

/// <summary>Часы, которые стоят на заданном моменте: тесты не зависят от реального времени.</summary>
public sealed class StoppedClock(DateTime localNow) : TimeProvider
{
    public DateTime LocalNow { get; set; } = localNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(LocalNow, DateTimeKind.Utc));
}
