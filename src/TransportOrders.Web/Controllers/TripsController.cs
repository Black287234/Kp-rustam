using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TransportOrders.Web.Data;
using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;
using TransportOrders.Web.Models;

namespace TransportOrders.Web.Controllers;

/// <summary>
/// Заявки на поездки: отбор и поиск, карточка с журналом, подача и изменение заявки,
/// действия диспетчера (назначение автомобиля, выезд, завершение, отклонение, отмена).
/// </summary>
public sealed class TripsController(
    TripStore trips, EmployeeStore staff, CarStore cars,
    RequestDesk desk, DispatchDesk dispatch, CarAvailability availability) : Controller
{
    public IActionResult Index([FromQuery] TripFilter filter)
    {
        ViewData["cars"] = cars.List();
        var found = trips.List(filter);
        // Запрос от HTMX: обновляется только таблица, страница целиком не перерисовывается
        if (Request.Headers.ContainsKey("HX-Request"))
            return PartialView("_Rows", found);
        return View(new TripListPage(filter, found));
    }

    public IActionResult Card(int id)
    {
        var trip = trips.Find(id);
        if (trip is null) return NotFound();
        return View(new TripCardPage(trip, trips.Log(id), TripLifecycle.ActionsFor(trip.Status)));
    }

    /// <summary>Список автомобилей с отметкой, подходят ли они под заявку (подгружается через HTMX).</summary>
    public IActionResult Options(int id)
    {
        var trip = trips.Find(id);
        if (trip is null) return NotFound();
        return PartialView("_Options", (trip, availability.OptionsFor(trip)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AssignCar(int id, int carId) => Done(id, dispatch.AssignCar(id, carId), "Автомобиль назначен");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveCar(int id) => Done(id, dispatch.RemoveCar(id), "Автомобиль снят с заявки, она снова ждёт назначения");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Depart(int id) => Done(id, dispatch.Depart(id), "Выезд отмечен, заявка в пути");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Complete(int id) => Done(id, dispatch.Complete(id), "Поездка завершена");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reject(int id, string? reason) => Done(id, dispatch.Reject(id, reason), "Заявка отклонена");

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cancel(int id, string? reason) => Done(id, dispatch.Cancel(id, reason), "Заявка отменена");

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
        if (trip.Status != TripStatus.New)
        {
            TempData["refused"] = $"Заявка в статусе «{trip.Status.Text()}»: изменяются только новые заявки";
            return RedirectToAction(nameof(Card), new { id });
        }
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

    private IActionResult Done(int id, Verdict verdict, string success)
    {
        if (verdict.Allowed) TempData["done"] = success;
        else TempData["refused"] = verdict.Summary;
        return RedirectToAction(nameof(Card), new { id });
    }

    private void FillRequesters() =>
        ViewData["requesters"] = staff.List(onlyActive: true)
            .Select(e => new SelectListItem($"{e.FullName} ({e.Department})", e.Id.ToString()))
            .ToList();
}
