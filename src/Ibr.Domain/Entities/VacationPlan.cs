namespace Ibr.Domain.Entities;

public class VacationPlan
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly FechaInicioPropuesta { get; set; }
    public DateOnly FechaFinPropuesta { get; set; }
    public decimal DiasPropuestos { get; set; }
    public string Estado { get; set; } = "Planificado";
    public string? Observaciones { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByEmail { get; set; }
    public int? CreatedByEmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;
    public Employee? CreatedByEmployee { get; set; }
}
