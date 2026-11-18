namespace TransportOrders.Web.Domain;

/// <summary>Русские подписи значений перечислений для страниц.</summary>
public static class Labels
{
    public static string Text(this CarKind kind) => kind switch
    {
        CarKind.Sedan => "легковой",
        CarKind.Minibus => "микроавтобус",
        CarKind.Van => "фургон",
        _ => kind.ToString()
    };

    public static string Text(this CarState state) => state switch
    {
        CarState.Operational => "в строю",
        CarState.InRepair => "на ремонте",
        CarState.Decommissioned => "списан",
        _ => state.ToString()
    };

    public static string Text(this TripStatus status) => status switch
    {
        TripStatus.New => "новая",
        TripStatus.CarAssigned => "автомобиль назначен",
        TripStatus.OnRoute => "в пути",
        TripStatus.Done => "выполнена",
        TripStatus.Rejected => "отклонена",
        TripStatus.Cancelled => "отменена",
        _ => status.ToString()
    };

    /// <summary>Класс оформления метки статуса.</summary>
    public static string Css(this TripStatus status) => "st-" + status.ToString().ToLowerInvariant();
}
