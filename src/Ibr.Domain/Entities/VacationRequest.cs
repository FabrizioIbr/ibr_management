namespace Ibr.Domain.Entities;

public class VacationRequest
{
    public int VacationId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal CalendarDays { get; set; }
    public decimal WorkingDays { get; set; }
    public string Status { get; set; } = null!;
    public int? VacationTypeId { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool? RequiereAuditoria { get; set; }
    public DateTime? FechaDeteccionAnulacion { get; set; }

    public Employee Employee { get; set; } = null!;
}
