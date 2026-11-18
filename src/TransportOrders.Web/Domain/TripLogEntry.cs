namespace TransportOrders.Web.Domain;

/// <summary>Запись журнала заявки.</summary>
public sealed record TripLogEntry(DateTime LoggedAt, TripStatus Status, string Message);
