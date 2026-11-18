namespace TransportOrders.Web.Domain;

/// <summary>Промежуток времени [начало; окончание). Конец не входит в промежуток.</summary>
public readonly record struct Period
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public Period(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ArgumentException("Окончание промежутка должно быть позже начала", nameof(end));
        Start = start;
        End = end;
    }

    /// <summary>
    /// Промежутки пересекаются, если один начинается раньше, чем закончился другой.
    /// Поездки «встык» (одна заканчивается в 11:00, другая начинается в 11:00) не пересекаются.
    /// </summary>
    public bool Overlaps(Period other) => Start < other.End && other.Start < End;

    public TimeSpan Length => End - Start;

    public override string ToString() => Start.Date == End.Date
        ? $"{Start:dd.MM.yyyy HH:mm}–{End:HH:mm}"
        : $"{Start:dd.MM.yyyy HH:mm} – {End:dd.MM.yyyy HH:mm}";
}
