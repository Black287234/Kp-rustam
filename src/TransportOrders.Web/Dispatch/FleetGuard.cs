using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Dispatch;

/// <summary>
/// Проверка изменений в карточке автомобиля, за которым закреплены поездки:
/// его нельзя вывести из строя и нельзя уменьшить число мест ниже числа пассажиров закреплённой поездки.
/// </summary>
public sealed class FleetGuard(CarAvailability availability, TimeProvider clock)
{
    public Verdict CheckChange(Car changed)
    {
        var held = availability.Commitments(changed.Id, clock.GetLocalNow().DateTime);
        if (held.Count == 0) return Verdict.Ok();

        var verdict = new Verdict();
        if (changed.State != CarState.Operational)
            verdict.Deny(
                $"За автомобилем закреплены поездки: {string.Join(", ", held.Select(t => $"{t.Number} ({t.StartsAt:dd.MM HH:mm})"))}. " +
                "Сначала снимите автомобиль с этих заявок или назначьте другой", nameof(Car.State));

        var crowded = held.Where(t => t.Passengers > changed.PassengerSeats).ToList();
        if (crowded.Count > 0)
            verdict.Deny(
                $"Мест не может быть меньше, чем пассажиров в закреплённых поездках: " +
                string.Join(", ", crowded.Select(t => $"{t.Number} — {t.Passengers} чел.")), nameof(Car.PassengerSeats));
        return verdict;
    }
}
