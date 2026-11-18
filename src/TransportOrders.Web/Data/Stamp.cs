using System.Globalization;

namespace TransportOrders.Web.Data;

/// <summary>Перевод даты и времени в строку базы данных и обратно (формат «ГГГГ-ММ-ДДTчч:мм»).</summary>
public static class Stamp
{
    private const string Format = "yyyy-MM-dd'T'HH:mm";

    public static string ToDb(DateTime value) => value.ToString(Format, CultureInfo.InvariantCulture);

    public static DateTime FromDb(string value) =>
        DateTime.ParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
