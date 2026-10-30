namespace Ibr.Application.Vacations;

public sealed record VacationKpisDto(int Approved, int Pending, int Rejected, int SignatureRequired);

public sealed record VacationRequestDto(
    int VacationId,
    int EmployeeId,
    string EmployeeName,
    string? EmployeeEmail,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal CalendarDays,
    decimal WorkingDays,
    string Status,
    int? VacationTypeId,
    DateTime? RequestedAt,
    DateTime? ApprovedAt);

public sealed record VacationRequestFilterDto(
    string? Employee,
    DateOnly? StartFrom,
    string? Status);

public sealed record EmployeeSearchDto(int EmployeeId, string FullName, string Email);

public sealed record VacationPeriodDto(
    int PeriodId,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    decimal EarnedDays,
    decimal UsedDays,
    decimal RemainingDays,
    DateOnly? ExpirationDate,
    bool IsProportional);

public sealed record EmployeeVacationDto(
    int EmployeeId,
    string EmployeeName,
    string EmployeeEmail,
    decimal TotalEarned,
    decimal TotalTaken,
    decimal CurrentBalance,
    IReadOnlyList<VacationPeriodDto> Periods);
