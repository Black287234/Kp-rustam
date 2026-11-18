using Microsoft.AspNetCore.Mvc;
using TransportOrders.Web.Dispatch;

namespace TransportOrders.Web.Controllers;

/// <summary>График занятости автомобилей на выбранный день.</summary>
public sealed class BoardController(CarAvailability availability, TimeProvider clock) : Controller
{
    public IActionResult Index(DateOnly? day)
    {
        var shown = day ?? DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        ViewData["day"] = shown;
        ViewData["now"] = clock.GetLocalNow().DateTime;
        return View(availability.Board(shown));
    }
}
