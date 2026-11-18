using Microsoft.AspNetCore.Mvc;
using TransportOrders.Web.Data;
using TransportOrders.Web.Models;

namespace TransportOrders.Web.Controllers;

/// <summary>Справочник сотрудников. Удаления нет: уволенный сотрудник помечается и остаётся в истории заявок.</summary>
public sealed class EmployeesController(EmployeeStore staff) : Controller
{
    public IActionResult Index(string? q)
    {
        ViewData["q"] = q;
        return View(staff.List(q));
    }

    public IActionResult Card(int id)
    {
        var e = staff.Find(id);
        return e is null ? NotFound() : View(e);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EmployeeForm());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(EmployeeForm form)
    {
        if (ModelState.IsValid && staff.PersonnelNoTaken(form.PersonnelNo.Trim()))
            ModelState.AddModelError(nameof(form.PersonnelNo), "Сотрудник с таким табельным номером уже есть");
        if (!ModelState.IsValid) return View("Form", form);

        var id = staff.Add(form.ToEmployee());
        TempData["done"] = "Сотрудник добавлен";
        return RedirectToAction(nameof(Card), new { id });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var e = staff.Find(id);
        return e is null ? NotFound() : View("Form", EmployeeForm.From(e));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EmployeeForm form)
    {
        if (staff.Find(id) is null) return NotFound();
        form.Id = id;
        if (ModelState.IsValid && staff.PersonnelNoTaken(form.PersonnelNo.Trim(), id))
            ModelState.AddModelError(nameof(form.PersonnelNo), "Сотрудник с таким табельным номером уже есть");
        if (!ModelState.IsValid) return View("Form", form);

        staff.Update(form.ToEmployee());
        TempData["done"] = "Изменения сохранены";
        return RedirectToAction(nameof(Card), new { id });
    }
}
