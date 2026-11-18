namespace TransportOrders.Web.Dispatch;

/// <summary>
/// Итог проверки операции: либо разрешено, либо список причин отказа.
/// У причины может быть имя поля формы, к которому она относится.
/// </summary>
public sealed class Verdict
{
    private readonly List<(string Field, string Text)> _reasons = [];

    public bool Allowed => _reasons.Count == 0;
    public IReadOnlyList<(string Field, string Text)> Reasons => _reasons;

    /// <summary>Первая причина отказа — для сообщения на странице.</summary>
    public string Summary => _reasons.Count == 0 ? "" : _reasons[0].Text;

    public Verdict Deny(string text, string field = "")
    {
        _reasons.Add((field, text));
        return this;
    }

    public static Verdict Refuse(string text, string field = "") => new Verdict().Deny(text, field);
    public static Verdict Ok() => new();
}
