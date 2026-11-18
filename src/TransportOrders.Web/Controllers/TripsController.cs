using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TransportOrders.Web.Data;
using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Models;

namespace TransportOrders.Web.Controllers;

/// <summary>Заявки на поездки: список, карточка с журналом, создание и изменение новой заявки.</summary>
public sealed class TripsController(TripStore trips, EmployeeStore staff, RequestDesk desk) : Controller
{
    public IActionResult Index() => View(trips.List());

    public IActionResult Card(int id)
    {
        var trip = trips.Find(id);
        if (trip is null) return NotFound();
        ViewData["log"] = trips.Log(id);
        return View(trip);
    }

    [HttpGet]
    public IActionResult Create()
    {
        FillRequesters();
        return View("Form", new TripForm { Day = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), From = new TimeOnly(9, 0), To = new TimeOnly(11, 0) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(TripForm form)
    {
        if (ModelState.IsValid)
        {
            var (verdict, id) = desk.Register(form.ToDraft());
            if (verdict.Allowed)
            {
                TempData["done"] = $"Заявка ЗТ-{id:0000} зарегистрирована";
                return RedirectToAction(nameof(Card), new { id });
            }
            ModelState.Absorb(verdict);
        }
        FillRequesters();
        return View("Form", form);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var trip = trips.Find(id);
        if (trip is null) return NotFound();
        FillRequesters();
        return View("Form", TripForm.Of(trip));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, TripForm form)
    {
        if (trips.Find(id) is null) return NotFound();
        form.Id = id;
        if (ModelState.IsValid)
        {
            var verdict = desk.Amend(id, form.ToDraft());
            if (verdict.Allowed)
            {
                TempData["done"] = "Изменения сохранены";
                return RedirectToAction(nameof(Card), new { id });
            }
            ModelState.Absorb(verdict);
        }
        FillRequesters();
        return View("Form", form);
    }

    private void FillRequesters() =>
        ViewData["requesters"] = staff.List(onlyActive: true)
            .Select(e => new SelectListItem($"{e.FullName} ({e.Department})", e.Id.ToString()))
            .ToList();
}
