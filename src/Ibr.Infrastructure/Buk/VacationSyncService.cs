using Ibr.Application.Buk;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ibr.Infrastructure.Buk;

internal sealed class VacationSyncService(
    IDbContextFactory<IbrDbContext> dbContextFactory,
    IBukApiClient bukApiClient,
    IOptions<BukOptions> options,
    ILogger<VacationSyncService> logger) : IVacationSyncService
{
    public async Task<VacationSyncResult> SyncAsync(string? token = null, CancellationToken cancellationToken = default)
    {
        var effectiveToken = string.IsNullOrWhiteSpace(token) ? options.Value.Token : token.Trim();
        if (!options.Value.HasBaseUrl || string.IsNullOrWhiteSpace(effectiveToken))
        {
            throw new InvalidOperationException("Falta configurar Buk:BaseUrl y Buk:Token.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var employeeIds = await db.Employees
            .Where(employee => employee.CompanyId == options.Value.CompanyId)
            .Select(employee => employee.EmployeeId)
            .ToListAsync(cancellationToken);
        var localIds = employeeIds.ToHashSet();
        var periods = 0;
        var requests = 0;

        foreach (var employeeId in employeeIds)
        {
            try
            {
                if (options.Value.RequestDelayMilliseconds > 0)
                {
                    await Task.Delay(options.Value.RequestDelayMilliseconds, cancellationToken);
                }

                var earned = await bukApiClient.GetEarnedVacationsAsync(employeeId, effectiveToken, cancellationToken);
                periods += await UpsertPeriodsAsync(db, employeeId, earned, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "EmployeeID {EmployeeId}: error sincronizando Vacaciones_Periodos.", employeeId);
            }
        }

        try
        {
            var requested = await bukApiClient.GetRequestedVacationsAsync(effectiveToken, cancellationToken);
            requests = await UpsertRequestsAsync(db, requested, localIds, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Error sincronizando Solicitudes_Vacaciones.");
        }

        logger.LogInformation(
            "Sincronización de vacaciones completada. Periodos insertados/actualizados: {Periods}. Solicitudes insertadas/actualizadas: {Requests}.",
            periods, requests);
        return new VacationSyncResult(periods, requests);
    }

    private static async Task<int> UpsertPeriodsAsync(
        IbrDbContext db,
        int employeeId,
        IReadOnlyList<BukEarnedVacation> source,
        CancellationToken cancellationToken)
    {
        var rows = source
            .Select(item => new { Item = item, Start = ParseDate(item.PeriodStartDate) })
            .Where(item => item.Start is not null)
            .ToList();
        if (rows.Count == 0) return 0;

        var starts = rows.Select(row => row.Start!.Value).ToArray();
        var existing = await db.VacationPeriods
            .Where(period => period.EmployeeId == employeeId && starts.Contains(period.PeriodStartDate))
            .ToDictionaryAsync(period => period.PeriodStartDate, cancellationToken);
        var changed = 0;

        foreach (var row in rows)
        {
            var start = row.Start!.Value;
            if (!existing.TryGetValue(start, out var period))
            {
                period = new VacationPeriod { EmployeeId = employeeId, PeriodStartDate = start };
                db.VacationPeriods.Add(period);
            }

            period.PeriodEndDate = start.AddYears(1).AddDays(-1);
            period.EarnedDays = row.Item.EarnedDays;
            period.IsProportional = row.Item.Proportional ?? false;
            period.ExpirationDate = ParseDate(row.Item.ExpirationDate);
            changed++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private static async Task<int> UpsertRequestsAsync(
        IbrDbContext db,
        IReadOnlyList<BukVacationRequest> source,
        IReadOnlySet<int> localEmployeeIds,
        CancellationToken cancellationToken)
    {
        var filtered = source.Where(request => localEmployeeIds.Contains(request.EmployeeId)).ToList();
        var ids = filtered.Select(request => request.Id).ToArray();
        if (ids.Length == 0) return 0;

        var existing = await db.VacationRequests
            .Where(request => ids.Contains(request.VacationId))
            .ToDictionaryAsync(request => request.VacationId, cancellationToken);
        var changed = 0;

        foreach (var sourceRequest in filtered)
        {
            var start = ParseDate(sourceRequest.StartDate);
            var end = ParseDate(sourceRequest.EndDate);
            if (start is null || end is null) continue;

            if (!existing.TryGetValue(sourceRequest.Id, out var request))
            {
                request = new VacationRequest { VacationId = sourceRequest.Id };
                db.VacationRequests.Add(request);
            }

            request.EmployeeId = sourceRequest.EmployeeId;
            request.StartDate = start.Value;
            request.EndDate = end.Value;
            request.CalendarDays = sourceRequest.CalendarDays;
            request.WorkingDays = sourceRequest.WorkingDays;
            request.Status = sourceRequest.Status?.Trim() ?? string.Empty;
            request.VacationTypeId = sourceRequest.VacationTypeId;
            request.RequestedAt = sourceRequest.RequestedAt;
            request.ApprovedAt = sourceRequest.ApprovedAt;
            changed++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;
}
