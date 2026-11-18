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
}
