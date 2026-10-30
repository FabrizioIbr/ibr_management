namespace Ibr.Application.Vacations;

public sealed record VacationBalanceDto(
    int EmployeeId,
    string FullName,
    string Email,
    int? BossEmployeeId,
    string? BossName,
    decimal DaysGenerated,
    decimal DaysPending,
    decimal DaysVencido,
    decimal DaysCritico,
    decimal DaysAlerta,
    decimal DaysSoonToExpire,
    DateOnly? NextExpiration,
    string RiskLevel,
    /// <summary>Días en solicitudes pre-aprobadas pendientes de gozar.</summary>
    decimal DaysPreApproved = 0)
{
    public bool HasActiveRequests => DaysPreApproved > 0;
}

public sealed record VacationDashboardDto(
    int TeamSize,
    decimal TotalPendingDays,
    decimal TotalSoonToExpireDays,
    int MembersAtRisk,
    IReadOnlyDictionary<string, int> RiskCounts,
    IReadOnlyList<VacationBalanceDto> TopCritical);

public sealed record VacationSettingsDto(int AlertDays, int CriticalDays);

/// <summary>Resultado de registrar un plan de vacaciones desde el portal.</summary>
public sealed record VacationPlanResult(bool Succeeded, int? VacationId = null, string? Error = null);

/// <summary>Periodo vacacional con su estado en lenguaje natural para el detalle del colaborador.</summary>
public sealed record VacationPeriodResumenDto(
    int PeriodId,
    string AnioPeriodo,
    decimal DiasGanados,
    decimal DiasGozados,
    decimal Saldo,
    DateOnly? VenceEl,
    bool EsProporcional,
    string Estado);

/// <summary>Resumen explicativo de un colaborador para el drawer de saldos.</summary>
public sealed record VacationColaboradorResumenDto(
    int EmployeeId,
    string Nombre,
    string Email,
    string? Cargo,
    string? Area,
    DateOnly? FechaIngreso,
    decimal DiasDisponibles,
    decimal DiasProximosAVencer,
    DateOnly? FechaVencimientoProxima,
    int? DiasFaltantesParaVencer,
    decimal DiasVencidos,
    decimal DiasAcumulando,
    IReadOnlyList<VacationPeriodResumenDto> Periodos,
    IReadOnlyList<VacationRequestDto> Solicitudes);

public interface IVacationSettingsService
{
    VacationSettingsDto Current { get; }

    Task<VacationSettingsDto> SaveAsync(int alertDays, int criticalDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// Consultas del portal Control de Vacaciones. Salvo que unrestricted=true
/// (SystemAdmin/RRHH), el alcance se limita al árbol jerárquico del jefe (CTE recursivo).
/// </summary>
public interface IVacationTeamService
{
    Task<VacationDashboardDto> GetDashboardAsync(int bossEmployeeId, bool unrestricted, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VacationBalanceDto>> GetBalancesAsync(int bossEmployeeId, bool unrestricted, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VacationRequestDto>> GetTeamRequestsAsync(
        int bossEmployeeId,
        bool unrestricted,
        string? status,
        DateOnly? startFrom,
        DateOnly? startTo,
        string? employee = null,
        CancellationToken cancellationToken = default);

    /// <summary>FIFO de un colaborador. Devuelve null si está fuera del alcance del jefe.</summary>
    Task<EmployeeVacationDto?> GetMemberPeriodsAsync(int bossEmployeeId, int targetEmployeeId, bool unrestricted, CancellationToken cancellationToken = default);

    /// <summary>Resumen explicativo (datos + periodos + solicitudes) de un colaborador del equipo.</summary>
    Task<VacationColaboradorResumenDto?> GetColaboradorResumenAsync(int bossEmployeeId, int targetEmployeeId, bool unrestricted, CancellationToken cancellationToken = default);

    /// <summary>Registra un plan de vacaciones (solicitud "pre_approved") para un colaborador del equipo.</summary>
    Task<VacationPlanResult> PlanificarAsync(int bossEmployeeId, int targetEmployeeId, DateOnly start, DateOnly end, bool unrestricted, CancellationToken cancellationToken = default);
}
