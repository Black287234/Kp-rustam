namespace TransportOrders.Web.Domain;

/// <summary>Заявка сотрудника на использование служебного автомобиля.</summary>
public sealed class TripRequest
{
    public int Id { get; set; }

    /// <summary>Номер заявки для людей: «ЗТ-0007».</summary>
    public string Number => $"ЗТ-{Id:0000}";

    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = "";
    public string RequesterDepartment { get; set; } = "";

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Purpose { get; set; } = "";
    public int Passengers { get; set; } = 1;

    public TripStatus Status { get; set; } = TripStatus.New;

    public int? CarId { get; set; }
    public string? CarPlate { get; set; }
    public string? CarTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Route => $"{Origin} → {Destination}";
}
