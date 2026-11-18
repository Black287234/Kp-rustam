using System.ComponentModel.DataAnnotations;
using TransportOrders.Web.Dispatch;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Models;

/// <summary>Поля формы заявки на поездку.</summary>
public sealed class TripForm
{
    public int Id { get; set; }

    [Display(Name = "Заявитель")]
    [Range(1, int.MaxValue, ErrorMessage = "Выберите заявителя")]
    public int RequesterId { get; set; }

    [Display(Name = "Дата поездки")]
    [Required(ErrorMessage = "Укажите дату поездки")]
    public DateOnly? Day { get; set; }

    [Display(Name = "Время выезда")]
    [Required(ErrorMessage = "Укажите время выезда")]
    public TimeOnly? From { get; set; }

    [Display(Name = "Время возвращения")]
    [Required(ErrorMessage = "Укажите время возвращения")]
    public TimeOnly? To { get; set; }

    [Display(Name = "Откуда")]
    [Required(ErrorMessage = "Укажите пункт отправления")]
    [StringLength(120, ErrorMessage = "Адрес — не длиннее 120 символов")]
    public string Origin { get; set; } = "ул. Ленина, 41 (главный офис)";

    [Display(Name = "Куда")]
    [Required(ErrorMessage = "Укажите пункт назначения")]
    [StringLength(120, ErrorMessage = "Адрес — не длиннее 120 символов")]
    public string Destination { get; set; } = "";

    [Display(Name = "Цель поездки")]
    [Required(ErrorMessage = "Укажите цель поездки")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Цель поездки — от 5 до 200 символов")]
    public string Purpose { get; set; } = "";

    [Display(Name = "Пассажиров")]
    public int Passengers { get; set; } = 1;

    public TripDraft ToDraft() =>
        new(RequesterId, Day!.Value, From!.Value, To!.Value, Origin, Destination, Purpose, Passengers);

    public static TripForm Of(TripRequest r) => new()
    {
        Id = r.Id, RequesterId = r.RequesterId, Day = DateOnly.FromDateTime(r.StartsAt),
        From = TimeOnly.FromDateTime(r.StartsAt), To = TimeOnly.FromDateTime(r.EndsAt),
        Origin = r.Origin, Destination = r.Destination, Purpose = r.Purpose, Passengers = r.Passengers
    };
}
