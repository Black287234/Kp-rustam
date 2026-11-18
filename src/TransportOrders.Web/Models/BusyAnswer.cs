using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Models;

/// <summary>Ответ на вопрос «свободен ли автомобиль в это время».</summary>
public sealed record BusyAnswer(Car Car, Period? Period, IReadOnlyList<TripRequest> Conflicts, string? Problem);
