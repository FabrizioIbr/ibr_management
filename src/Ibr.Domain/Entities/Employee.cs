namespace Ibr.Domain.Entities;

public class Employee
{
    public int EmployeeId { get; set; }
    public int? PersonId { get; set; }
    public int CompanyId { get; set; }
    public string? CodeSheet { get; set; }
    public string? FirstName { get; set; }
    public string? Surname { get; set; }
    public string? SecondSurname { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateOnly? HireDate { get; set; }
    public string? Status { get; set; }
    public int? AreaId { get; set; }
    public int? CargoId { get; set; }
    public int? BossId { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateOnly? FechaCese { get; set; }
    public string? MotivoCese { get; set; }
    public string? TipoContrato { get; set; }
    public string? RegimenLaboral { get; set; }
    public int? JornadaHorasSemana { get; set; }
    public int? DiasLaboralesSemana { get; set; }
    public string? DiasLaboralesDetalle { get; set; }
    public decimal? DiasVacacionesAnuales { get; set; }
    public DateOnly? VacacionesProgresivasDesde { get; set; }
    public decimal? SaldoActualBuk { get; set; }

    public Employee? Boss { get; set; }
    public Company? Company { get; set; }
    public Area? Area { get; set; }
    public Cargo? Cargo { get; set; }
}
