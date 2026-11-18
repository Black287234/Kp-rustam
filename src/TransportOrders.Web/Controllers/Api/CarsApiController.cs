using Microsoft.AspNetCore.Mvc;
using TransportOrders.Web.Data;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Controllers.Api;

/// <summary>Чтение справочника автомобилей в формате JSON.</summary>
[ApiController]
[Route("api/cars")]
public sealed class CarsApiController(CarStore cars) : ControllerBase
{
    [HttpGet]
    public IEnumerable<object> List() => cars.List().Select(Describe);

    [HttpGet("{id:int}")]
    public IActionResult One(int id)
    {
        var car = cars.Find(id);
        return car is null
            ? NotFound(new { message = $"Автомобиль № {id} не найден" })
            : Ok(Describe(car));
    }

    private static object Describe(Car c) => new
    {
        c.Id,
        c.Plate,
        c.Make,
        c.Model,
        kind = c.Kind.Text(),
        c.PassengerSeats,
        c.ReleaseYear,
        state = c.State.Text()
    };
}
