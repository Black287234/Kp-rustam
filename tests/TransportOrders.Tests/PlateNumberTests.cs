using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

/// <summary>Закрепляет исправление: номер, набранный латинскими буквами того же начертания, принимается.</summary>
public class PlateNumberTests
{
    [Test, DisplayName("Госномер приводится к единому виду")]
    [Arguments("а214км 186", "А214КМ 186")]
    [Arguments("А214КМ186", "А214КМ 186")]
    [Arguments(" A777MP  186 ", "А777МР 186")]
    [Arguments("x001yx86", "Х001УХ 86")]
    public async Task Plate_is_normalized(string input, string expected)
    {
        await Assert.That(PlateNumber.Normalize(input)).IsEqualTo(expected);
    }

    [Test, DisplayName("Неверные госномера отклоняются")]
    [Arguments("AB123")]
    [Arguments("Б214КМ 186")]
    [Arguments("А21КМ 186")]
    [Arguments("А214КМ 1")]
    [Arguments("")]
    public async Task Wrong_plates_are_invalid(string input)
    {
        await Assert.That(PlateNumber.IsValid(input)).IsFalse();
    }

    [Test, DisplayName("Латинская и кириллическая записи одного номера считаются повтором")]
    public async Task Latin_duplicate_is_detected()
    {
        using var b = new Bench();
        await Assert.That(b.Cars.PlateTaken(PlateNumber.Normalize("A214KM 186"))).IsTrue();
    }
}
