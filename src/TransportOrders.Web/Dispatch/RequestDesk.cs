using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Dispatch;

/// <summary>Приём заявок на поездки: проверка введённых данных, регистрация и изменение заявки.</summary>
public sealed class RequestDesk(TripStore trips, EmployeeStore staff, TimeProvider clock)
{
    /// <summary>Самая длинная поездка, которую можно оформить одной заявкой.</summary>
    public static readonly TimeSpan LongestTrip = TimeSpan.FromHours(12);

    /// <summary>Наибольшее число пассажиров (вместимость самого большого автомобиля автопарка с запасом).</summary>
    public const int MaxPassengers = 30;

    private DateTime Now => clock.GetLocalNow().DateTime;

    public Verdict Check(TripDraft draft)
    {
        var verdict = new Verdict();
        var requester = staff.Find(draft.RequesterId);
        if (requester is null)
            verdict.Deny("Выберите заявителя из списка сотрудников", nameof(TripDraft.RequesterId));
        else if (!requester.IsActive)
            verdict.Deny($"Сотрудник {requester.FullName} уволен и не может подавать заявки", nameof(TripDraft.RequesterId));

        if (draft.To <= draft.From)
            verdict.Deny("Время окончания поездки должно быть позже времени начала", nameof(TripDraft.To));
        else if (draft.EndsAt - draft.StartsAt > LongestTrip)
            verdict.Deny($"Поездка не может длиться дольше {LongestTrip.TotalHours:0} часов", nameof(TripDraft.To));

        if (draft.StartsAt < Now)
            verdict.Deny("Нельзя оформить поездку на прошедшее время", nameof(TripDraft.From));

        if (draft.Passengers is < 1 or > MaxPassengers)
            verdict.Deny($"Число пассажиров — от 1 до {MaxPassengers}", nameof(TripDraft.Passengers));

        if (string.Equals(draft.Origin.Trim(), draft.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
            verdict.Deny("Пункт назначения совпадает с пунктом отправления", nameof(TripDraft.Destination));

        return verdict;
    }

    public (Verdict Verdict, int Id) Register(TripDraft draft)
    {
        var verdict = Check(draft);
        if (!verdict.Allowed) return (verdict, 0);
        var id = trips.Add(Build(draft), Now);
        return (verdict, id);
    }

    public Verdict Amend(int id, TripDraft draft)
    {
        var current = trips.Find(id);
        if (current is null) return Verdict.Refuse("Заявка не найдена");
        if (current.Status != TripStatus.New)
            return Verdict.Refuse($"Заявку в статусе «{current.Status.Text()}» изменить нельзя: изменяются только новые заявки");

        var verdict = Check(draft);
        if (!verdict.Allowed) return verdict;
        var changed = Build(draft);
        changed.Id = id;
        changed.Status = current.Status;
        trips.Update(changed, Now);
        return verdict;
    }

    private static TripRequest Build(TripDraft d) => new()
    {
        RequesterId = d.RequesterId,
        StartsAt = d.StartsAt,
        EndsAt = d.EndsAt,
        Origin = d.Origin.Trim(),
        Destination = d.Destination.Trim(),
        Purpose = d.Purpose.Trim(),
        Passengers = d.Passengers
    };
}
