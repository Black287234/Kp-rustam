namespace TransportOrders.Web.Domain;

/// <summary>Статус заявки на поездку.</summary>
public enum TripStatus
{
    New = 1,
    CarAssigned = 2,
    OnRoute = 3,
    Done = 4,
    Rejected = 5,
    Cancelled = 6
}
