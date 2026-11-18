using Microsoft.AspNetCore.Mvc;

namespace TransportOrders.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    /// <summary>Страница для кода ответа 404 и других кодов ошибок.</summary>
    [Route("/Home/Code/{code:int}")]
    public IActionResult Code(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }

    /// <summary>Страница непредвиденной ошибки (только в рабочем режиме).</summary>
    public IActionResult Failure() => View();
}
