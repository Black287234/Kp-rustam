namespace TransportOrders.Web.Domain;

/// <summary>Действие диспетчера над заявкой.</summary>
public enum TripAction
{
    AssignCar,
    RemoveCar,
    Depart,
    Complete,
    Reject,
    Cancel
}
