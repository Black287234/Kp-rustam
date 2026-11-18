using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransportOrders.Web.Dispatch;

namespace TransportOrders.Web.Controllers;

internal static class ModelStateExtensions
{
    /// <summary>Переносит причины отказа в ошибки формы, чтобы они показались рядом с полями.</summary>
    public static void Absorb(this ModelStateDictionary state, Verdict verdict)
    {
        foreach (var (field, text) in verdict.Reasons)
            state.AddModelError(field, text);
    }
}
