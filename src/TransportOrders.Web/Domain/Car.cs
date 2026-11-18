namespace TransportOrders.Web.Domain;

/// <summary>Служебный автомобиль организации.</summary>
public sealed class Car
{
    public int Id { get; set; }

    /// <summary>Государственный регистрационный знак, например «А214КМ 186».</summary>
    public string Plate { get; set; } = "";

    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public CarKind Kind { get; set; }

    /// <summary>Число пассажирских мест (без водителя).</summary>
    public int PassengerSeats { get; set; }

    public int ReleaseYear { get; set; }
    public CarState State { get; set; } = CarState.Operational;

    public string Title => $"{Make} {Model}";
}
