using Microsoft.AspNetCore.Mvc;
using TransportOrders.Web.Data;
using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Controllers.Api;

/// <summary>Заявки и занятость автомобилей в формате JSON.</summary>
[ApiController]
[Route("api")]
public sealed class TripsApiController(TripStore trips, CarStore cars, CarAvailability availability) : ControllerBase
{
    [HttpGet("trips")]
    public IEnumerable<object> List([FromQuery] TripFilter filter) => trips.List(filter).Select(Describe);

    [HttpGet("trips/{id:int}")]
    public IActionResult One(int id) =>
        trips.Find(id) is { } t ? Ok(Describe(t)) : NotFound(new { message = $"Заявка № {id} не найдена" });

    /// <summary>Занят ли автомобиль: /api/cars/3/busy?from=2026-11-20T09:00&amp;to=2026-11-20T12:00</summary>
    [HttpGet("cars/{id:int}/busy")]
    public IActionResult Busy(int id, DateTime? from, DateTime? to)
    {
        if (cars.Find(id) is null) return NotFound(new { message = $"Автомобиль № {id} не найден" });
        if (from is null || to is null || to <= from)
            return BadRequest(new { message = "Укажите from и to, окончание позже начала" });
        var conflicts = availability.Conflicts(id, new Period(from.Value, to.Value));
        return Ok(new { busy = conflicts.Count > 0, conflicts = conflicts.Select(Describe) });
    }

    private static object Describe(TripRequest t) => new
    {
        t.Id,
        number = t.Number,
        status = t.Status.Text(),
        startsAt = Stamp.ToDb(t.StartsAt),
        endsAt = Stamp.ToDb(t.EndsAt),
        requester = t.RequesterName,
        t.Origin,
        t.Destination,
        t.Purpose,
        t.Passengers,
        car = t.CarPlate,
        t.Resolution
    };
}
