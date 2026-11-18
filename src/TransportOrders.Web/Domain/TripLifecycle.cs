namespace TransportOrders.Web.Domain;

/// <summary>
/// Жизненный цикл заявки: какие действия допустимы в каждом статусе и к какому статусу они ведут.
/// <code>
/// новая ──назначить──► автомобиль назначен ──выезд──► в пути ──завершить──► выполнена
///   │  ◄──снять автомобиль──┘        │
///   ├──отклонить──► отклонена        └──отменить──► отменена
///   └──отменить───► отменена
/// </code>
/// </summary>
public static class TripLifecycle
{
    private static readonly Dictionary<(TripStatus From, TripAction Action), TripStatus> Moves = new()
    {
        [(TripStatus.New, TripAction.AssignCar)] = TripStatus.CarAssigned,
        [(TripStatus.New, TripAction.Reject)] = TripStatus.Rejected,
        [(TripStatus.New, TripAction.Cancel)] = TripStatus.Cancelled,
        [(TripStatus.CarAssigned, TripAction.RemoveCar)] = TripStatus.New,
        [(TripStatus.CarAssigned, TripAction.Depart)] = TripStatus.OnRoute,
        [(TripStatus.CarAssigned, TripAction.Cancel)] = TripStatus.Cancelled,
        [(TripStatus.OnRoute, TripAction.Complete)] = TripStatus.Done
    };

    /// <summary>Статус после действия или null, если действие в этом статусе недопустимо.</summary>
    public static TripStatus? After(TripStatus from, TripAction action) =>
        Moves.TryGetValue((from, action), out var to) ? to : null;

    public static IReadOnlyList<TripAction> ActionsFor(TripStatus from) =>
        Moves.Keys.Where(k => k.From == from).Select(k => k.Action).ToList();

    /// <summary>Заявка закрыта и больше не меняется.</summary>
    public static bool IsClosed(TripStatus s) => s is TripStatus.Done or TripStatus.Rejected or TripStatus.Cancelled;

    /// <summary>Заявка занимает автомобиль: он назначен или уже в пути.</summary>
    public static bool HoldsCar(TripStatus s) => s is TripStatus.CarAssigned or TripStatus.OnRoute;

    public static string Text(this TripAction action) => action switch
    {
        TripAction.AssignCar => "назначить автомобиль",
        TripAction.RemoveCar => "снять автомобиль",
        TripAction.Depart => "отметить выезд",
        TripAction.Complete => "завершить поездку",
        TripAction.Reject => "отклонить",
        TripAction.Cancel => "отменить",
        _ => action.ToString()
    };
}
