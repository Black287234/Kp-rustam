using Microsoft.AspNetCore.Mvc;
using TransportOrders.Web.Data;
using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;
using TransportOrders.Web.Models;

namespace TransportOrders.Web.Controllers;

/// <summary>Справочник автомобилей: список, карточка, добавление и изменение.</summary>
public sealed class CarsController(CarStore cars, CarAvailability availability, FleetGuard guard, TimeProvider clock) : Controller
{
    public IActionResult Index(string? q, CarState? state)
    {
        ViewData["q"] = q;
        ViewData["state"] = state;
        return View(cars.List(q, state));
    }

    public IActionResult Card(int id)
    {
        var car = cars.Find(id);
        if (car is null) return NotFound();
        ViewData["commitments"] = availability.Commitments(id, clock.GetLocalNow().DateTime);
        return View(car);
    }

    /// <summary>Проверка занятости автомобиля на дату и время (ответ — фрагмент страницы для HTMX).</summary>
    public IActionResult Busy(int id, DateOnly? day, TimeOnly? from, TimeOnly? to)
    {
        var car = cars.Find(id);
        if (car is null) return NotFound();
        if (day is null || from is null || to is null || to <= from)
            return PartialView("_Busy", new BusyAnswer(car, null, [], "Укажите дату и время: окончание позже начала"));
        var period = new Domain.Period(day.Value.ToDateTime(from.Value), day.Value.ToDateTime(to.Value));
        return PartialView("_Busy", new BusyAnswer(car, period, availability.Conflicts(id, period), null));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new CarForm());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CarForm form)
    {
        if (ModelState.IsValid && cars.PlateTaken(form.NormalizedPlate))
            ModelState.AddModelError(nameof(form.Plate), $"Автомобиль с госномером {form.NormalizedPlate} уже есть в справочнике");
        if (!ModelState.IsValid) return View("Form", form);

        var id = cars.Add(form.ToCar());
        TempData["done"] = $"Автомобиль {form.NormalizedPlate} добавлен";
        return RedirectToAction(nameof(Card), new { id });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var car = cars.Find(id);
        return car is null ? NotFound() : View("Form", CarForm.From(car));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, CarForm form)
    {
        if (cars.Find(id) is null) return NotFound();
        form.Id = id;
        if (ModelState.IsValid && cars.PlateTaken(form.NormalizedPlate, id))
            ModelState.AddModelError(nameof(form.Plate), $"Автомобиль с госномером {form.NormalizedPlate} уже есть в справочнике");
        // Автомобиль с закреплёнными поездками нельзя вывести из строя или «ужать» по числу мест
        if (ModelState.IsValid)
            ModelState.Absorb(guard.CheckChange(form.ToCar()));
        if (!ModelState.IsValid) return View("Form", form);

        cars.Update(form.ToCar());
        TempData["done"] = "Изменения сохранены";
        return RedirectToAction(nameof(Card), new { id });
    }
}
