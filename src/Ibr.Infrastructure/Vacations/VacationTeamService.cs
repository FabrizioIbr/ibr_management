using Ibr.Application.Vacations;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Vacations;

public sealed class VacationTeamService(
    IbrDbContext db,
    IVacationSettingsService settings,
    IVacationAdminService adminService) : IVacationTeamService
{
    public const string LevelNormal = "normal";
    public const string LevelAlerta = "alerta";
    public const string LevelCritico = "critico";
    public const string LevelVencido = "vencido";

    private static readonly string[] PendingStatuses = ["submitted", "pre_approved"];

    public async Task<VacationDashboardDto> GetDashboardAsync(
        int bossEmployeeId,
        bool unrestricted,
        CancellationToken cancellationToken = default)
    {
        var balances = await GetBalancesAsync(bossEmployeeId, unrestricted, cancellationToken);
        var riskCounts = balances
            .GroupBy(balance => balance.RiskLevel)
            .ToDictionary(group => group.Key, group => group.Count());

        return new VacationDashboardDto(
            balances.Count,
            balances.Sum(balance => balance.DaysPending),
            balances.Sum(balance => balance.DaysSoonToExpire),
            balances.Count(balance => balance.RiskLevel is LevelCritico or LevelVencido),
            riskCounts,
            balances
                .Where(balance => balance.DaysVencido > 0 || balance.DaysCritico > 0)
                .OrderByDescending(balance => balance.DaysVencido + balance.DaysCritico)
                .Take(5)
                .ToList());
    }

    public async Task<IReadOnlyList<VacationBalanceDto>> GetBalancesAsync(
        int bossEmployeeId,
        bool unrestricted,
        CancellationToken cancellationToken = default)
    {
        var team = await GetTeamEmployeesAsync(bossEmployeeId, unrestricted, cancellationToken);
        if (team.Count == 0) return [];

        var employeeIds = team.Select(employee => employee.EmployeeId).ToArray();
        var periods = await db.VacationPeriods
            .AsNoTracking()
            .Where(period => employeeIds.Contains(period.EmployeeId))
            .OrderBy(period => period.EmployeeId)
            .ThenBy(period => period.PeriodStartDate)
            .ToListAsync(cancellationToken);
        var requests = await db.VacationRequests
            .AsNoTracking()
            .Where(request => employeeIds.Contains(request.EmployeeId) && (request.Status == "approved" || request.Status == "pre_approved"))
            .Select(request => new { request.EmployeeId, request.Status, request.CalendarDays })
            .ToListAsync(cancellationToken);

        var bossIds = team.Where(employee => employee.BossId != null).Select(employee => employee.BossId!.Value).Distinct().ToArray();
        var bosses = bossIds.Length == 0
            ? new Dictionary<int, string>()
            : await db.Employees
                .AsNoTracking()
                .Where(employee => bossIds.Contains(employee.EmployeeId))
                .ToDictionaryAsync(employee => employee.EmployeeId, employee => employee.FullName, cancellationToken);

        var periodsByEmployee = periods.GroupBy(period => period.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var takenByEmployee = requests.Where(request => request.Status == "approved")
            .GroupBy(row => row.EmployeeId).ToDictionary(group => group.Key, group => group.Sum(row => row.CalendarDays));
        var preApprovedByEmployee = requests.Where(request => request.Status == "pre_approved")
            .GroupBy(row => row.EmployeeId).ToDictionary(group => group.Key, group => group.Sum(row => row.CalendarDays));
        var config = settings.Current;
        var today = DateOnly.FromDateTime(DateTime.Now);

        return team
            .Select(employee => ComputeBalance(employee, periodsByEmployee.GetValueOrDefault(employee.EmployeeId, []),
                takenByEmployee.GetValueOrDefault(employee.EmployeeId), preApprovedByEmployee.GetValueOrDefault(employee.EmployeeId),
                bosses, config.AlertDays, config.CriticalDays, today))
            .OrderBy(balance => balance.FullName)
            .ToList();
    }

    public async Task<IReadOnlyList<VacationRequestDto>> GetTeamRequestsAsync(
        int bossEmployeeId,
        bool unrestricted,
        string? status,
        DateOnly? startFrom,
        DateOnly? startTo,
        string? employee = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.VacationRequests.AsNoTracking();

        if (unrestricted)
        {
            query = query.Where(request => request.Employee.CompanyId == 2 && request.Employee.Status == "activo");
        }
        else
        {
            var employeeIds = await GetTeamEmployeeIdsAsync(bossEmployeeId, cancellationToken);
            if (employeeIds.Count == 0) return [];
            query = query.Where(request => employeeIds.Contains(request.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(employee))
        {
            var terms = employee.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = terms.Aggregate(query, (current, part) => current.Where(request =>
                request.Employee.FullName.Contains(part) || request.Employee.Email.Contains(part)));
        }

        query = status?.Trim().ToLowerInvariant() switch
        {
            "aprobado" => query.Where(request => request.Status == "approved"),
            "solicitado" => query.Where(request => PendingStatuses.Contains(request.Status)),
            "rechazado" => query.Where(request => request.Status == "rejected"),
            _ => query
        };

        if (startFrom is { } from)
        {
            query = query.Where(request => request.StartDate >= from);
        }

        if (startTo is { } to)
        {
            query = query.Where(request => request.StartDate <= to);
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

    public async Task<EmployeeVacationDto?> GetMemberPeriodsAsync(
        int bossEmployeeId,
        int targetEmployeeId,
        bool unrestricted,
        CancellationToken cancellationToken = default)
    {
        if (targetEmployeeId <= 0) return null;

        if (!unrestricted)
        {
            var employeeIds = await GetTeamEmployeeIdsAsync(bossEmployeeId, cancellationToken);
            if (!employeeIds.Contains(targetEmployeeId)) return null;
        }

        return await adminService.GetEmployeeVacationsAsync(targetEmployeeId, cancellationToken);
    }

    public async Task<VacationColaboradorResumenDto?> GetColaboradorResumenAsync(
        int bossEmployeeId,
        int targetEmployeeId,
        bool unrestricted,
        CancellationToken cancellationToken = default)
    {
        if (targetEmployeeId <= 0) return null;

        if (!unrestricted)
        {
            var employeeIds = await GetTeamEmployeeIdsAsync(bossEmployeeId, cancellationToken);
            if (!employeeIds.Contains(targetEmployeeId)) return null;
        }

        var employee = await db.Employees
            .AsNoTracking()
            .Where(candidate => candidate.EmployeeId == targetEmployeeId && candidate.CompanyId == 2)
            .Select(candidate => new
            {
                candidate.EmployeeId,
                candidate.FullName,
                candidate.Email,
                candidate.HireDate,
                Cargo = candidate.Cargo != null ? candidate.Cargo.Name : null,
                Area = candidate.Area != null ? candidate.Area.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null) return null;

        var vacations = await adminService.GetEmployeeVacationsAsync(targetEmployeeId, cancellationToken);
        var config = settings.Current;
        var today = DateOnly.FromDateTime(DateTime.Now);

        decimal disponibles = 0, proximos = 0, vencidos = 0;
        DateOnly? proximaFecha = null;
        var periodos = new List<(VacationPeriodResumenDto Dto, int Anio, bool EsAjuste, DateOnly Inicio)>();

        foreach (var period in vacations?.Periods ?? [])
        {
            var esAjuste = period.EarnedDays < 0;
            var saldo = esAjuste ? 0 : Math.Max(0, period.RemainingDays);
            string estado;
            if (esAjuste)
            {
                estado = "ajustado";
            }
            else if (period.RemainingDays <= 0)
            {
                estado = "agotado";
            }
            else if (period.ExpirationDate is not { } vencimiento)
            {
                estado = "disponible";
                disponibles += saldo;
            }
            else if (vencimiento < today)
            {
                estado = "vencido";
                vencidos += saldo;
            }
            else
            {
                disponibles += saldo;
                estado = vencimiento <= today.AddDays(config.AlertDays) ? "por_vencer" : "disponible";
                if (estado == "por_vencer") proximos += saldo;
                if (proximaFecha is null || vencimiento < proximaFecha) proximaFecha = vencimiento;
            }

            var anioCompleto = period.PeriodEndDate.DayNumber - period.PeriodStartDate.DayNumber + 1 >= 365;
            periodos.Add((
                new VacationPeriodResumenDto(
                    period.PeriodId,
                    anioCompleto ? $"{period.PeriodStartDate.Year} - {period.PeriodEndDate.Year}" : "Ajuste Especial",
                    period.EarnedDays,
                    period.UsedDays,
                    period.RemainingDays,
                    period.ExpirationDate,
                    period.IsProportional,
                    estado),
                period.PeriodStartDate.Year,
                esAjuste,
                period.PeriodStartDate));
        }

        // Orden por año; los ajustes del mismo año quedan justo debajo de los períodos originales.
        var periodosOrdenados = periodos
            .OrderBy(item => item.Anio)
            .ThenBy(item => item.EsAjuste ? 1 : 0)
            .ThenBy(item => item.Inicio)
            .Select(item => item.Dto)
            .ToList();

        var solicitudes = await db.VacationRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == targetEmployeeId)
            .OrderByDescending(request => request.StartDate)
            .Select(request => new VacationRequestDto(
                request.VacationId,
                request.EmployeeId,
                employee.FullName,
                employee.Email,
                request.StartDate,
                request.EndDate,
                request.CalendarDays,
                request.WorkingDays,
                request.Status,
                request.VacationTypeId,
                request.RequestedAt,
                request.ApprovedAt))
            .ToListAsync(cancellationToken);

        return new VacationColaboradorResumenDto(
            employee.EmployeeId,
            employee.FullName,
            employee.Email,
            employee.Cargo,
            employee.Area,
            employee.HireDate,
            disponibles,
            proximos,
            proximaFecha,
            proximaFecha is { } fecha ? fecha.DayNumber - today.DayNumber : null,
            vencidos,
            disponibles - proximos,
            periodosOrdenados,
            solicitudes);
    }

    public async Task<VacationPlanResult> PlanificarAsync(
        int bossEmployeeId,
        int targetEmployeeId,
        DateOnly start,
        DateOnly end,
        bool unrestricted,
        CancellationToken cancellationToken = default)
    {
        if (targetEmployeeId <= 0) return new VacationPlanResult(false, null, "Colaborador no válido.");

        if (!unrestricted)
        {
            var employeeIds = await GetTeamEmployeeIdsAsync(bossEmployeeId, cancellationToken);
            if (!employeeIds.Contains(targetEmployeeId)) return new VacationPlanResult(false, null, "El colaborador no está en tu equipo.");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (start < today) return new VacationPlanResult(false, null, "La fecha de inicio no puede ser anterior a hoy.");
        if (end < start) return new VacationPlanResult(false, null, "La fecha de fin no puede ser anterior al inicio.");
        if (end.DayNumber - start.DayNumber + 1 > 366) return new VacationPlanResult(false, null, "El rango no puede superar un año.");

        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.EmployeeId == targetEmployeeId && candidate.CompanyId == 2, cancellationToken);
        if (employee is null) return new VacationPlanResult(false, null, "El colaborador no existe en IBR Perú.");

        var conflict = await db.VacationRequests.AnyAsync(request => request.EmployeeId == targetEmployeeId
            && request.Status != "rejected" && request.StartDate <= end && request.EndDate >= start, cancellationToken);
        if (conflict) return new VacationPlanResult(false, null, "El colaborador ya tiene una solicitud que cruza esas fechas.");

        var plan = new VacationRequest
        {
            EmployeeId = targetEmployeeId,
            StartDate = start,
            EndDate = end,
            CalendarDays = end.DayNumber - start.DayNumber + 1,
            WorkingDays = CountWorkingDays(start, end, employee.DiasLaboralesSemana),
            Status = "pre_approved",
            RequestedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };
        db.VacationRequests.Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        return new VacationPlanResult(true, plan.VacationId);
    }

    private static decimal CountWorkingDays(DateOnly start, DateOnly end, int? daysPerWeek)
    {
        DayOfWeek[] restDays = daysPerWeek is >= 6 ? [DayOfWeek.Sunday] : [DayOfWeek.Saturday, DayOfWeek.Sunday];
        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (!restDays.Contains(day.DayOfWeek)) count++;
        }

        return count;
    }

    private async Task<List<Employee>> GetTeamEmployeesAsync(int bossEmployeeId, bool unrestricted, CancellationToken cancellationToken)    {
        if (unrestricted)
        {
            return await db.Employees
                .AsNoTracking()
                .Where(employee => employee.CompanyId == 2 && employee.Status == "activo")
                .ToListAsync(cancellationToken);
        }

        var ids = await GetTeamEmployeeIdsAsync(bossEmployeeId, cancellationToken);
        if (ids.Count == 0) return [];

        return await db.Employees
            .AsNoTracking()
            .Where(employee => ids.Contains(employee.EmployeeId) && employee.Status == "activo")
            .ToListAsync(cancellationToken);
    }

    private async Task<List<int>> GetTeamEmployeeIdsAsync(int bossEmployeeId, CancellationToken cancellationToken)
    {
        if (bossEmployeeId <= 0) return [];

        var recursive = db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true
            ? "WITH RECURSIVE"
            : "WITH";
        var sql = $"""
            {recursive} Jerarquia AS (
                SELECT EmployeeID, BossID, 1 AS NivelJerarquico
                FROM Empleados
                WHERE BossID = @bossId AND Status = 'activo'
                UNION ALL
                SELECT e.EmployeeID, e.BossID, j.NivelJerarquico + 1
                FROM Empleados e
                INNER JOIN Jerarquia j ON e.BossID = j.EmployeeID
                WHERE e.Status = 'activo'
            )
            SELECT EmployeeID FROM Jerarquia
            """;

        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "bossId";
            parameter.Value = bossEmployeeId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var ids = new List<int>();
            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(reader.GetInt32(0));
            }

            // El propio jefe también forma parte de su lista (balances, excepciones, planificación y filtros).
            if (!ids.Contains(bossEmployeeId)) ids.Add(bossEmployeeId);
            return ids;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static VacationBalanceDto ComputeBalance(
        Employee employee,
        List<VacationPeriod> periods,
        decimal takenDays,
        decimal preApprovedDays,
        IReadOnlyDictionary<int, string> bosses,
        int alertDays,
        int criticalDays,
        DateOnly today)
    {
        var generated = periods.Sum(period => period.EarnedDays);
        var pending = Math.Max(0, generated - takenDays);
        // Los ajustes negativos consumen el pool igual que los días gozados.
        var pool = takenDays + periods.Where(period => period.EarnedDays < 0).Sum(period => -period.EarnedDays);
        decimal vencido = 0, critico = 0, alerta = 0;
        DateOnly? nextExpiration = null;

        // FIFO: el consumo se asigna al periodo más antiguo primero.
        foreach (var period in periods)
        {
            if (period.EarnedDays <= 0) continue;

            var used = Math.Min(Math.Max(pool, 0), period.EarnedDays);
            pool -= used;
            var remaining = period.EarnedDays - used;
            if (remaining <= 0 || period.ExpirationDate is not { } expiration)
            {
                continue;
            }

            if (expiration < today)
            {
                vencido += remaining;
            }
            else if (expiration <= today.AddDays(criticalDays))
            {
                critico += remaining;
            }
            else if (expiration <= today.AddDays(alertDays))
            {
                alerta += remaining;
            }

            if (nextExpiration is null || expiration < nextExpiration)
            {
                nextExpiration = expiration;
            }
        }

        var level = vencido > 0 ? LevelVencido : critico > 0 ? LevelCritico : alerta > 0 ? LevelAlerta : LevelNormal;

        return new VacationBalanceDto(
            employee.EmployeeId,
            employee.FullName,
            employee.Email,
            employee.BossId,
            employee.BossId is { } bossId ? bosses.GetValueOrDefault(bossId) : null,
            generated,
            pending,
            vencido,
            critico,
            alerta,
            critico + alerta,
            nextExpiration,
            level,
            preApprovedDays);
    }
}
