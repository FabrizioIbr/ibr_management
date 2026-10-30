namespace Ibr.Application.Vacations;

/// <summary>
/// Consultas de administración de vacaciones: KPIs, solicitudes filtradas,
/// búsqueda de empleados y periodos con asignación FIFO de días gozados.
/// </summary>
public interface IVacationAdminService
{
    Task<VacationKpisDto> GetKpisAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VacationRequestDto>> SearchRequestsAsync(VacationRequestFilterDto filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeSearchDto>> SearchEmployeesAsync(string term, CancellationToken cancellationToken = default);

    Task<EmployeeVacationDto?> GetEmployeeVacationsAsync(int employeeId, CancellationToken cancellationToken = default);
}
