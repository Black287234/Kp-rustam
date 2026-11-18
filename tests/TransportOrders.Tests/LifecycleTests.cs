using TransportOrders.Web.Domain;

namespace TransportOrders.Tests;

public class LifecycleTests
{
    [Test, DisplayName("Разрешённые переходы статусов")]
    [Arguments(TripStatus.New, TripAction.AssignCar, TripStatus.CarAssigned)]
    [Arguments(TripStatus.New, TripAction.Reject, TripStatus.Rejected)]
    [Arguments(TripStatus.New, TripAction.Cancel, TripStatus.Cancelled)]
    [Arguments(TripStatus.CarAssigned, TripAction.RemoveCar, TripStatus.New)]
    [Arguments(TripStatus.CarAssigned, TripAction.Depart, TripStatus.OnRoute)]
    [Arguments(TripStatus.CarAssigned, TripAction.Cancel, TripStatus.Cancelled)]
    [Arguments(TripStatus.OnRoute, TripAction.Complete, TripStatus.Done)]
    public async Task Allowed_moves(TripStatus from, TripAction action, TripStatus expected)
    {
        await Assert.That(TripLifecycle.After(from, action)).IsEqualTo(expected);
    }

    [Test, DisplayName("Запрещённые переходы статусов")]
    [Arguments(TripStatus.New, TripAction.Depart)]
    [Arguments(TripStatus.New, TripAction.Complete)]
    [Arguments(TripStatus.CarAssigned, TripAction.AssignCar)]
    [Arguments(TripStatus.CarAssigned, TripAction.Reject)]
    [Arguments(TripStatus.OnRoute, TripAction.Cancel)]
    [Arguments(TripStatus.OnRoute, TripAction.RemoveCar)]
    [Arguments(TripStatus.Done, TripAction.Cancel)]
    [Arguments(TripStatus.Rejected, TripAction.AssignCar)]
    [Arguments(TripStatus.Cancelled, TripAction.AssignCar)]
    public async Task Forbidden_moves(TripStatus from, TripAction action)
    {
        await Assert.That(TripLifecycle.After(from, action)).IsNull();
    }

    [Test, DisplayName("У закрытой заявки нет действий")]
    [Arguments(TripStatus.Done)]
    [Arguments(TripStatus.Rejected)]
    [Arguments(TripStatus.Cancelled)]
    public async Task Closed_request_has_no_actions(TripStatus status)
    {
        await Assert.That(TripLifecycle.IsClosed(status)).IsTrue();
        await Assert.That(TripLifecycle.ActionsFor(status)).IsEmpty();
    }

    [Test, DisplayName("Автомобиль занимают только назначенные заявки и заявки в пути")]
    public async Task Only_assigned_and_on_route_hold_a_car()
    {
        var holding = Enum.GetValues<TripStatus>().Where(TripLifecycle.HoldsCar).ToArray();
        await Assert.That(holding).IsEquivalentTo([TripStatus.CarAssigned, TripStatus.OnRoute]);
    }
}
