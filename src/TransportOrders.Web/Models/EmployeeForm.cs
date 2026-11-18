using System.ComponentModel.DataAnnotations;
using TransportOrders.Web.Domain;

namespace TransportOrders.Web.Models;

/// <summary>Поля формы сотрудника.</summary>
public sealed class EmployeeForm
{
    public int Id { get; set; }

    [Display(Name = "Табельный номер")]
    [Required(ErrorMessage = "Укажите табельный номер")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Табельный номер — ровно 5 цифр")]
    public string PersonnelNo { get; set; } = "";

    [Display(Name = "ФИО")]
    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(80, MinimumLength = 5, ErrorMessage = "ФИО — от 5 до 80 символов")]
    public string FullName { get; set; } = "";

    [Display(Name = "Подразделение")]
    [Required(ErrorMessage = "Укажите подразделение")]
    [StringLength(60)]
    public string Department { get; set; } = "";

    [Display(Name = "Должность")]
    [Required(ErrorMessage = "Укажите должность")]
    [StringLength(60)]
    public string Position { get; set; } = "";

    [Display(Name = "Телефон")]
    [RegularExpression(@"^[\d\s\-\+\(\)]{5,20}$", ErrorMessage = "Телефон — цифры, пробелы, скобки и дефисы")]
    public string? Phone { get; set; }

    [Display(Name = "Работает")]
    public bool IsActive { get; set; } = true;

    public static EmployeeForm From(Employee e) => new()
    {
        Id = e.Id, PersonnelNo = e.PersonnelNo, FullName = e.FullName, Department = e.Department,
        Position = e.Position, Phone = e.Phone, IsActive = e.IsActive
    };

    public Employee ToEmployee() => new()
    {
        Id = Id, PersonnelNo = PersonnelNo.Trim(), FullName = string.Join(' ', FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)),
        Department = Department.Trim(), Position = Position.Trim(),
        Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(), IsActive = IsActive
    };
}
