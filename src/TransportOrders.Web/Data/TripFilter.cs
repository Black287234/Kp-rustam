using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Data;

/// <summary>Условия отбора заявок. Пустое поле означает «не ограничивать».</summary>
public sealed class TripFilter
{
    /// <summary>Слово для поиска в маршруте, цели поездки или ФИО заявителя.</summary>
    public string? Text { get; set; }

    public TripStatus? Status { get; set; }
    public DateOnly? DayFrom { get; set; }
    public DateOnly? DayTo { get; set; }
    public int? CarId { get; set; }

    /// <summary>Показать только незакрытые заявки (новые, назначенные, в пути).</summary>
    public bool OpenOnly { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Text) && Status is null && DayFrom is null && DayTo is null && CarId is null && !OpenOnly;
}
