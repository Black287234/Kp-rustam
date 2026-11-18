using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

public class PeriodTests
{
    private static Period At(string from, string to) =>
        new(DateTime.Today.Add(TimeSpan.Parse(from)), DateTime.Today.Add(TimeSpan.Parse(to)));

    [Test, DisplayName("Частичное пересечение промежутков")]
    [Arguments("09:00", "11:00", "10:00", "12:00")]
    [Arguments("10:00", "12:00", "09:00", "11:00")]
    [Arguments("09:00", "13:00", "10:00", "11:00")]
    [Arguments("10:00", "11:00", "09:00", "13:00")]
    [Arguments("10:00", "11:00", "10:00", "11:00")]
    public async Task Overlapping_periods(string a1, string a2, string b1, string b2)
    {
        await Assert.That(At(a1, a2).Overlaps(At(b1, b2))).IsTrue();
    }

    [Test, DisplayName("Поездки встык и разнесённые не пересекаются")]
    [Arguments("09:00", "11:00", "11:00", "12:00")]
    [Arguments("11:00", "12:00", "09:00", "11:00")]
    [Arguments("08:00", "09:00", "15:00", "16:00")]
    public async Task Touching_or_separate_periods_do_not_overlap(string a1, string a2, string b1, string b2)
    {
        await Assert.That(At(a1, a2).Overlaps(At(b1, b2))).IsFalse();
    }

    [Test, DisplayName("Промежуток с концом не позже начала не создаётся")]
    [Arguments("10:00", "10:00")]
    [Arguments("12:00", "09:00")]
    public async Task Empty_or_reversed_period_is_rejected(string from, string to)
    {
        await Assert.That(() => At(from, to)).Throws<ArgumentException>();
    }

    [Test, DisplayName("Длительность промежутка")]
    public async Task Length_is_end_minus_start()
    {
        await Assert.That(At("09:15", "11:45").Length).IsEqualTo(TimeSpan.FromMinutes(150));
    }
}
