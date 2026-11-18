using System.ComponentModel.DataAnnotations;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Models;

/// <summary>Поля формы добавления и изменения автомобиля.</summary>
public sealed class CarForm
{
    public int Id { get; set; }

    [Display(Name = "Госномер")]
    [Required(ErrorMessage = "Укажите госномер")]
    [RegularExpression(@"^\s*[АВЕКМНОРСТУХавекмнорстух]\d{3}[АВЕКМНОРСТУХавекмнорстух]{2}\s?\d{2,3}\s*$",
        ErrorMessage = "Госномер в формате А123ВС 186: буквы А, В, Е, К, М, Н, О, Р, С, Т, У, Х и код региона")]
    public string Plate { get; set; } = "";

    [Display(Name = "Марка")]
    [Required(ErrorMessage = "Укажите марку")]
    [StringLength(40, ErrorMessage = "Марка — не длиннее 40 символов")]
    public string Make { get; set; } = "";

    [Display(Name = "Модель")]
    [Required(ErrorMessage = "Укажите модель")]
    [StringLength(40, ErrorMessage = "Модель — не длиннее 40 символов")]
    public string Model { get; set; } = "";

    [Display(Name = "Тип")]
    public CarKind Kind { get; set; } = CarKind.Sedan;

    [Display(Name = "Пассажирских мест")]
    [Range(1, 30, ErrorMessage = "Пассажирских мест — от 1 до 30")]
    public int PassengerSeats { get; set; } = 4;

    [Display(Name = "Год выпуска")]
    [Range(1990, 2100, ErrorMessage = "Год выпуска — от 1990")]
    public int ReleaseYear { get; set; } = DateTime.Today.Year;

    [Display(Name = "Состояние")]
    public CarState State { get; set; } = CarState.Operational;

    /// <summary>Госномер в едином виде: заглавные буквы и один пробел перед регионом.</summary>
    public string NormalizedPlate
    {
        get
        {
            var p = Plate.Replace(" ", "").ToUpperInvariant();
            return p.Length > 6 ? $"{p[..6]} {p[6..]}" : p;
        }
    }

    public static CarForm From(Car c) => new()
    {
        Id = c.Id, Plate = c.Plate, Make = c.Make, Model = c.Model, Kind = c.Kind,
        PassengerSeats = c.PassengerSeats, ReleaseYear = c.ReleaseYear, State = c.State
    };

    public Car ToCar() => new()
    {
        Id = Id, Plate = NormalizedPlate, Make = Make.Trim(), Model = Model.Trim(), Kind = Kind,
        PassengerSeats = PassengerSeats, ReleaseYear = ReleaseYear, State = State
    };
}
