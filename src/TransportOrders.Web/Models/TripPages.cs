using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Models;

/// <summary>Данные страницы списка заявок.</summary>
public sealed record TripListPage(TripFilter Filter, IReadOnlyList<TripRequest> Trips);

/// <summary>Данные карточки заявки: сама заявка, журнал и доступные действия.</summary>
public sealed record TripCardPage(TripRequest Trip, IReadOnlyList<TripLogEntry> Log, IReadOnlyList<TripAction> Actions)
{
    public bool Can(TripAction action) => Actions.Contains(action);
}
