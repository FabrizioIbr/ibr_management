namespace Ibr.Domain.Entities;

public class VacationPeriod
{
    public int PeriodId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly PeriodEndDate { get; set; }
    public decimal EarnedDays { get; set; }
    public bool? IsProportional { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}
