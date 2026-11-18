namespace TransportOrders.Web.Domain;

/// <summary>Сотрудник организации, который может подавать заявки на поездки.</summary>
public sealed class Employee
{
    public int Id { get; set; }

    /// <summary>Табельный номер из пяти цифр.</summary>
    public string PersonnelNo { get; set; } = "";

    public string FullName { get; set; } = "";
    public string Department { get; set; } = "";
    public string Position { get; set; } = "";
    public string? Phone { get; set; }

    /// <summary>Работает ли сотрудник. Уволенные остаются в справочнике ради истории заявок.</summary>
    public bool IsActive { get; set; } = true;
}
