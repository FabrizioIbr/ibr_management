namespace Ibr.Domain.Entities;

/// <summary>Cuenta de acceso a la aplicación, enlazada opcionalmente a un empleado de Buk.</summary>
public class AppUser
{
    public int UserId { get; set; }
    public int? EmployeeId { get; set; }
    public string Email { get; set; } = null!;
    public string? EmailCorporativo { get; set; }
    public string? PasswordHash { get; set; }
    public string? AppRole { get; set; } = "Empleado";
    public bool? IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Employee? Employee { get; set; }
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
