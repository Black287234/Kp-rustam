using Microsoft.AspNetCore.Mvc;
using TransportOrders.Web.Data;

namespace TransportOrders.Web.Controllers;

/// <summary>Страницы справочника автомобилей.</summary>
public sealed class CarsController(ICarSource cars) : Controller
{
    public IActionResult Index() => View(cars.All());

    public IActionResult Card(int id)
    {
        var car = cars.Find(id);
        return car is null ? NotFound() : View(car);
    }
}
