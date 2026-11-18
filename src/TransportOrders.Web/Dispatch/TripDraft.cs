namespace TransportOrders.Web.Dispatch;

/// <summary>Данные заявки, введённые пользователем: поездка в пределах одного дня.</summary>
public sealed record TripDraft(
    int RequesterId,
    DateOnly Day,
    TimeOnly From,
    TimeOnly To,
    string Origin,
    string Destination,
    string Purpose,
    int Passengers)
{
    public DateTime StartsAt => Day.ToDateTime(From);
    public DateTime EndsAt => Day.ToDateTime(To);
}
