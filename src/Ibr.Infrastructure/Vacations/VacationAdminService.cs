using Ibr.Application.Vacations;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Vacations;

public sealed class VacationAdminService(IbrDbContext db) : IVacationAdminService
{
    private static readonly string[] PendingStatuses = ["submitted", "pre_approved"];

    public async Task<VacationKpisDto> GetKpisAsync(CancellationToken cancellationToken = default)
    {
        var counts = await db.VacationRequests
            .AsNoTracking()
            .GroupBy(request => request.Status)
            .Select(group => new { Status = group.Key, Total = group.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = counts.ToDictionary(item => item.Status, item => item.Total, StringComparer.OrdinalIgnoreCase);
        return new VacationKpisDto(
            byStatus.GetValueOrDefault("approved"),
            byStatus.GetValueOrDefault("submitted") + byStatus.GetValueOrDefault("pre_approved"),
            byStatus.GetValueOrDefault("rejected"),
            byStatus.GetValueOrDefault("signature_required"));
    }

    public async Task<IReadOnlyList<VacationRequestDto>> SearchRequestsAsync(
        VacationRequestFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = db.VacationRequests
            .AsNoTracking()
            .Where(request => request.Employee.CompanyId == 2);

        if (!string.IsNullOrWhiteSpace(filter.Employee))
        {
            var term = filter.Employee.Trim();
            var terms = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = terms.Aggregate(query, (current, part) =>
                current.Where(request => request.Employee.FullName.Contains(part)));
        }

        if (filter.StartFrom is { } from)
        {
            query = query.Where(request => request.StartDate >= from);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && !string.Equals(filter.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var status = filter.Status.Trim().ToLowerInvariant();
            query = status switch
            {
                "pending" => query.Where(request => PendingStatuses.Contains(request.Status)),
                _ => query.Where(request => request.Status == status)
            };
        }

        return await query
            .OrderByDescending(request => request.StartDate)
            .ThenBy(request => request.Employee.FullName)
            .Select(request => new VacationRequestDto(
                request.VacationId,
                request.EmployeeId,
                request.Employee.FullName,
                request.Employee.Email,
                request.StartDate,
                request.EndDate,
                request.CalendarDays,
                request.WorkingDays,
                request.Status,
                request.VacationTypeId,
                request.RequestedAt,
                request.ApprovedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeSearchDto>> SearchEmployeesAsync(string term, CancellationToken cancellationToken = default)
    {
        var query = db.Employees
            .AsNoTracking()
            .Where(employee => employee.CompanyId == 2);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var text = term.Trim();
            var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = terms.Aggregate(query, (current, part) =>
                current.Where(employee => employee.FullName.Contains(part) || employee.Email.Contains(part)));
        }

        return await query
            .OrderBy(employee => employee.FullName)
            .Take(25)
            .Select(employee => new EmployeeSearchDto(employee.EmployeeId, employee.FullName, employee.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeVacationDto?> GetEmployeeVacationsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(candidate => candidate.EmployeeId == employeeId && candidate.CompanyId == 2)
            .Select(candidate => new { candidate.EmployeeId, candidate.FullName, candidate.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null) return null;

        var periods = await db.VacationPeriods
            .AsNoTracking()
            .Where(period => period.EmployeeId == employeeId)
            .OrderBy(period => period.PeriodStartDate)
            .ToListAsync(cancellationToken);

        var takenDays = await db.VacationRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == employeeId && request.Status == "approved")
            .Select(request => request.CalendarDays)
            .ToListAsync(cancellationToken);

        // FIFO: los días gozados se asignan al periodo más antiguo primero.
        // Los periodos con días negativos son ajustes de RRHH: no aportan saldo y consumen
        // el pool de días disponibles (equivalen a días ya gozados).
        var pool = takenDays.Sum() + periods.Where(period => period.EarnedDays < 0).Sum(period => -period.EarnedDays);
        var result = new List<VacationPeriodDto>(periods.Count);
        foreach (var period in periods)
        {
            decimal used;
            if (period.EarnedDays <= 0)
            {
                used = period.EarnedDays;
            }
            else
            {
                used = Math.Min(Math.Max(pool, 0), period.EarnedDays);
                pool -= used;
            }

            result.Add(new VacationPeriodDto(
                period.PeriodId,
                period.PeriodStartDate,
                period.PeriodEndDate,
                period.EarnedDays,
                used,
                period.EarnedDays - used,
                period.ExpirationDate,
                period.IsProportional ?? false));
        }

        var totalEarned = periods.Sum(period => period.EarnedDays);
        var totalTaken = takenDays.Sum();

        return new EmployeeVacationDto(
            employee.EmployeeId,
            employee.FullName,
            employee.Email,
            totalEarned,
            totalTaken,
            totalEarned - totalTaken,
            result);
    }
}
