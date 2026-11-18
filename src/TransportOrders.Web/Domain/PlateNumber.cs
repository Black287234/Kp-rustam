using System.Text;
using System.Text.RegularExpressions;

namespace TransportOrders.Web.Domain;

/// <summary>Государственный регистрационный знак легкового автомобиля: «А123ВС 186».</summary>
public static partial class PlateNumber
{
    /// <summary>В госномерах используются только буквы, одинаковые в кириллице и латинице.</summary>
    private const string Cyrillic = "АВЕКМНОРСТУХ";
    private const string Latin = "ABEKMHOPCTYX";

    [GeneratedRegex(@"^[АВЕКМНОРСТУХ]\d{3}[АВЕКМНОРСТУХ]{2} \d{2,3}$")]
    private static partial Regex Pattern();

    /// <summary>
    /// Приводит номер к единому виду: заглавные кириллические буквы, один пробел перед кодом региона.
    /// Латинские буквы того же начертания (A, B, E, K, M, H, O, P, C, T, Y, X) заменяются кириллическими.
    /// </summary>
    public static string Normalize(string? text)
    {
        var sb = new StringBuilder();
        foreach (var ch in (text ?? "").ToUpperInvariant())
        {
            if (char.IsWhiteSpace(ch)) continue;
            var latin = Latin.IndexOf(ch);
            sb.Append(latin >= 0 ? Cyrillic[latin] : ch);
        }
        var s = sb.ToString();
        return s.Length > 6 ? $"{s[..6]} {s[6..]}" : s;
    }

    /// <summary>Номер после приведения соответствует формату «А123ВС 186».</summary>
    public static bool IsValid(string? text) => Pattern().IsMatch(Normalize(text));
}
