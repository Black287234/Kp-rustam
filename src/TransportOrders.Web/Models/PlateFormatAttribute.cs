using System.ComponentModel.DataAnnotations;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Models;

/// <summary>Проверка госномера после приведения к единому виду (латинские буквы того же начертания допускаются).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PlateFormatAttribute : ValidationAttribute
{
    public PlateFormatAttribute() =>
        ErrorMessage = "Госномер в формате А123ВС 186: буквы А, В, Е, К, М, Н, О, Р, С, Т, У, Х и код региона";

    public override bool IsValid(object? value) =>
        value is not string text || string.IsNullOrWhiteSpace(text) || PlateNumber.IsValid(text);
}
