using Ibr.Application.Buk;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ibr.Infrastructure.Buk;

/// <summary>
/// Upsert de las tablas locales a partir de los maestros de Buk.
/// No borra registros locales: solo inserta o actualiza por el ID de Buk.
/// </summary>
internal sealed class BukSyncService(
    IDbContextFactory<IbrDbContext> dbContextFactory,
    IBukApiClient bukApiClient,
    IOptions<BukOptions> options,
    ILogger<BukSyncService> logger) : IBukSyncService
{
    public string? BaseUrl => options.Value.BaseUrl;
    public bool HasBaseUrl => options.Value.HasBaseUrl;
    public bool HasConfiguredToken => options.Value.HasToken;
    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<BukSyncResult> SyncAsync(string? token = null, CancellationToken cancellationToken = default)
    {
        var effectiveToken = string.IsNullOrWhiteSpace(token) ? options.Value.Token : token.Trim();
        if (!HasBaseUrl || string.IsNullOrWhiteSpace(effectiveToken))
        {
            throw new InvalidOperationException(
                "Falta configurar Buk. Revisa Buk:BaseUrl e ingresa el token en la pantalla de integración o en User Secrets / Azure Key Vault.");
        }

        var areas = await bukApiClient.GetAreasAsync(effectiveToken, cancellationToken);
        var cargos = await bukApiClient.GetCargosAsync(effectiveToken, cancellationToken);
        var employees = await bukApiClient.GetEmployeesAsync(effectiveToken, cancellationToken);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.Now;
        await UpsertAreasAsync(db, areas, now, options.Value.ExcludedAreaMarker, cancellationToken);
        var cargoAreas = await UpsertCargosAsync(db, cargos, now, cancellationToken);
        var employeesSaved = await UpsertEmployeesAsync(db, employees, options.Value.CompanyId, now, cancellationToken);
        var newUsers = await CreateUsersAsync(db, employees, options.Value.CompanyId, now, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await BackfillEmployeeDetailsAsync(db, effectiveToken, now, cancellationToken);

        var result = new BukSyncResult(
            areas.Count,
            cargos.Count,
            cargoAreas,
            employees.Count,
            employeesSaved,
            newUsers,
            DateTimeOffset.UtcNow);

        logger.LogInformation(
            "Total de empleados leídos: {EmployeesRead}, Empleados de IBR Perú guardados/actualizados: {EmployeesSaved}, Usuarios nuevos creados: {NewUsers}",
            result.EmployeesRead, result.EmployeesSaved, result.NewUsers);

        return result;
    }

    private async Task BackfillEmployeeDetailsAsync(
        IbrDbContext db,
        string token,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var pending = await db.Employees
            .Where(employee => employee.FechaCese == null
                || employee.MotivoCese == null
                || employee.TipoContrato == null
                || employee.RegimenLaboral == null
                || employee.JornadaHorasSemana == null
                || employee.DiasLaboralesSemana == null
                || employee.DiasLaboralesDetalle == null
                || employee.DiasVacacionesAnuales == null
                || employee.VacacionesProgresivasDesde == null)
            .OrderBy(employee => employee.EmployeeId)
            .ToListAsync(cancellationToken);

        var updated = 0;
        var unchanged = 0;
        var errors = 0;

        foreach (var employee in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (options.Value.EmployeeDetailDelayMilliseconds > 0)
            {
                await Task.Delay(options.Value.EmployeeDetailDelayMilliseconds, cancellationToken);
            }

            try
            {
                var detail = await bukApiClient.GetEmployeeAsync(employee.EmployeeId, token, cancellationToken);
                if (detail is null)
                {
                    errors++;
                    logger.LogError("EmployeeID {EmployeeId}: Buk respondió 404 al consultar el detalle.", employee.EmployeeId);
                    continue;
                }

                var fields = ApplyEmployeeDetails(employee, detail, now, logger);
                if (fields.Count == 0)
                {
                    unchanged++;
                    continue;
                }

                await db.SaveChangesAsync(cancellationToken);
                updated++;
                logger.LogInformation("EmployeeID {EmployeeId}: campos actualizados: {Fields}.", employee.EmployeeId, string.Join(", ", fields));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors++;
                logger.LogError(exception, "EmployeeID {EmployeeId}: error actualizando detalle desde Buk.", employee.EmployeeId);
                db.ChangeTracker.Clear();
            }
        }

        logger.LogInformation(
            "Backfill de empleados: total procesados {Processed}, actualizados {Updated}, sin cambios {Unchanged}, errores {Errors}.",
            pending.Count, updated, unchanged, errors);
    }

    private static List<string> ApplyEmployeeDetails(
        Employee employee,
        BukEmployee source,
        DateTime now,
        ILogger logger)
    {
        var job = source.CurrentJob;
        var days = job?.Days?.Where(day => !string.IsNullOrWhiteSpace(day)).ToList() ?? [];
        var daysCount = days.Count == 0 ? (int?)null : days.Count;
        var daysDetail = days.Count == 0 ? null : string.Concat(days.Select(MapDay));
        var regimen = Truncate(job?.EmploymentRegimen, 50);
        var vacationDays = VacationDays(regimen, employee.EmployeeId, logger);
        var values = new Dictionary<string, object?>
        {
            ["FechaCese"] = ParseDate(source.ActiveUntil),
            ["MotivoCese"] = Truncate(source.TerminationReason, 100),
            ["TipoContrato"] = Truncate(job?.ContractType, 100),
            ["RegimenLaboral"] = regimen,
            ["JornadaHorasSemana"] = job?.RegularHours is { } hours ? (int?)Math.Round(hours, MidpointRounding.AwayFromZero) : null,
            ["DiasLaboralesSemana"] = daysCount,
            ["DiasLaboralesDetalle"] = Truncate(daysDetail, 10),
            ["DiasVacacionesAnuales"] = vacationDays,
            ["VacacionesProgresivasDesde"] = ParseDate(source.ProgressiveVacationsStart)
        };

        var fields = new List<string>();
        SetIfChanged(employee, nameof(Employee.FechaCese), values["FechaCese"], fields);
        SetIfChanged(employee, nameof(Employee.MotivoCese), values["MotivoCese"], fields);
        SetIfChanged(employee, nameof(Employee.TipoContrato), values["TipoContrato"], fields);
        SetIfChanged(employee, nameof(Employee.RegimenLaboral), values["RegimenLaboral"], fields);
        SetIfChanged(employee, nameof(Employee.JornadaHorasSemana), values["JornadaHorasSemana"], fields);
        SetIfChanged(employee, nameof(Employee.DiasLaboralesSemana), values["DiasLaboralesSemana"], fields);
        SetIfChanged(employee, nameof(Employee.DiasLaboralesDetalle), values["DiasLaboralesDetalle"], fields);
        SetIfChanged(employee, nameof(Employee.DiasVacacionesAnuales), values["DiasVacacionesAnuales"], fields);
        SetIfChanged(employee, nameof(Employee.VacacionesProgresivasDesde), values["VacacionesProgresivasDesde"], fields);
        return fields;
    }

    private static decimal VacationDays(string? regimen, int employeeId, ILogger logger)
    {
        if (string.Equals(regimen, "general", StringComparison.OrdinalIgnoreCase)) return 30m;
        if (regimen?.Contains("mype", StringComparison.OrdinalIgnoreCase) == true) return 15m;

        logger.LogWarning("EmployeeID {EmployeeId}: régimen laboral '{Regimen}' no reconocido; se aplican 30 días de vacaciones.", employeeId, regimen ?? "NULL");
        return 30m;
    }

    private static string MapDay(string day) => day.Trim().ToLowerInvariant() switch
    {
        "l" => "L",
        "m" => "M",
        "w" => "X",
        "j" => "J",
        "v" => "V",
        "s" => "S",
        "d" => "D",
        _ => day.Trim().ToUpperInvariant()
    };

    private static void SetIfChanged(Employee employee, string field, object? value, ICollection<string> fields)
    {
        var property = typeof(Employee).GetProperty(field)!;
        var current = property.GetValue(employee);
        if (Equals(current, value)) return;
        property.SetValue(employee, value);
        fields.Add(field);
    }

    private static async Task<int> UpsertEmployeesAsync(
        IbrDbContext db,
        IReadOnlyList<BukEmployee> employees,
        int companyId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var filtered = employees
            .Where(employee => employee.CurrentJob?.CompanyId == companyId)
            .ToList();
        var ids = filtered.Select(employee => employee.Id).ToArray();
        var existing = ids.Length == 0
            ? new Dictionary<int, Employee>()
            : await db.Employees
                .Where(employee => ids.Contains(employee.EmployeeId))
                .ToDictionaryAsync(employee => employee.EmployeeId, cancellationToken);

        foreach (var source in filtered)
        {
            if (!existing.TryGetValue(source.Id, out var employee))
            {
                employee = new Employee { EmployeeId = source.Id };
                db.Employees.Add(employee);
            }

            var job = source.CurrentJob!;
            employee.PersonId = source.PersonId;
            employee.CompanyId = job.CompanyId!.Value;
            employee.CodeSheet = Truncate(source.CodeSheet, 50);
            employee.FirstName = Truncate(source.FirstName, 100);
            employee.Surname = Truncate(source.Surname, 100);
            employee.SecondSurname = Truncate(source.SecondSurname, 100);
            employee.FullName = Truncate(source.FullName, 200)
                ?? BuildFullName(source)
                ?? $"Empleado {source.Id}";
            employee.Email = Truncate(source.Email, 150)
                ?? Truncate(source.PersonalEmail, 150)
                ?? string.Empty;
            employee.HireDate = ParseDate(source.ActiveSince);
            employee.Status = Truncate(source.Status, 50);
            employee.AreaId = job.MainAreaId ?? job.AreaId;
            employee.CargoId = job.Role?.Id;
            employee.BossId = job.Boss?.Id;
            employee.LastSyncAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return filtered.Count;
    }

    private static async Task<int> CreateUsersAsync(
        IbrDbContext db,
        IReadOnlyList<BukEmployee> employees,
        int companyId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var candidates = employees
            .Where(employee => employee.CurrentJob?.CompanyId == companyId)
            .Select(employee => new
            {
                employee.Id,
                Email = Truncate(employee.Email, 150) ?? Truncate(employee.PersonalEmail, 150)
            })
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Email))
            .ToList();
        if (candidates.Count == 0) return 0;

        var emails = candidates.Select(candidate => candidate.Email!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var existingUsers = await db.AppUsers
            .Where(user => emails.Contains(user.Email) || emails.Contains(user.EmailCorporativo!))
            .Select(user => new { user.Email, user.EmailCorporativo })
            .ToListAsync(cancellationToken);
        var knownEmails = existingUsers
            .SelectMany(user => new[] { user.Email, user.EmailCorporativo })
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var candidate in candidates)
        {
            if (!knownEmails.Add(candidate.Email!)) continue;

            db.AppUsers.Add(new AppUser
            {
                EmployeeId = candidate.Id,
                Email = candidate.Email!,
                EmailCorporativo = candidate.Email!,
                PasswordHash = null,
                AppRole = "Empleado",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            created++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    private static async Task UpsertAreasAsync(
        IbrDbContext db,
        IReadOnlyList<BukArea> areas,
        DateTime now,
        string excludedMarker,
        CancellationToken cancellationToken)
    {
        var includedAreas = areas.Where(area => !IsExcludedArea(area, excludedMarker)).ToList();
        var ids = includedAreas.Select(area => area.Id).ToArray();
        var existing = await db.Areas
            .Where(area => ids.Contains(area.AreaId))
            .ToDictionaryAsync(area => area.AreaId, cancellationToken);

        foreach (var source in includedAreas)
        {
            if (!existing.TryGetValue(source.Id, out var area))
            {
                area = new Area { AreaId = source.Id, CreatedAt = now };
                db.Areas.Add(area);
            }

            area.Name = Truncate(source.Name, 200) ?? $"Área {source.Id}";
            area.Status = Truncate(source.Status, 50);
            area.DepartmentId = source.SecondLevelId;
            area.DepartmentName = Truncate(source.SecondLevelName, 200);
            area.DivisionId = source.FirstLevelId;
            area.DivisionName = Truncate(source.FirstLevelName, 200);
            area.CostCenter = Truncate(source.CostCenter, 50);
            area.ParentAreaId = null;
            area.UpdatedAt = now;
        }

        // Primera pasada sin padre: evita depender del orden de inserción de la jerarquía.
        await db.SaveChangesAsync(cancellationToken);

        var localIds = existing.Keys.Concat(includedAreas.Select(area => area.Id)).ToHashSet();
        foreach (var source in includedAreas)
        {
            var parentId = source.ParentArea?.Id;
            var area = existing.TryGetValue(source.Id, out var tracked)
                ? tracked
                : await db.Areas.FindAsync([source.Id], cancellationToken);

            if (area is not null)
            {
                area.ParentAreaId = parentId is { } id && localIds.Contains(id) ? id : null;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsExcludedArea(BukArea area, string marker) =>
        !string.IsNullOrWhiteSpace(marker)
        && new[] { area.Name, area.FirstLevelName, area.SecondLevelName }
            .Any(value => value?.Contains(marker, StringComparison.OrdinalIgnoreCase) == true);

    private static async Task<int> UpsertCargosAsync(
        IbrDbContext db,
        IReadOnlyList<BukCargo> cargos,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var ids = cargos.Select(cargo => cargo.Id).ToArray();
        var existing = await db.Cargos
            .Where(cargo => ids.Contains(cargo.CargoId))
            .ToDictionaryAsync(cargo => cargo.CargoId, cancellationToken);

        foreach (var source in cargos)
        {
            if (!existing.TryGetValue(source.Id, out var cargo))
            {
                cargo = new Cargo { CargoId = source.Id, CreatedAt = now };
                db.Cargos.Add(cargo);
            }

            cargo.Code = Truncate(source.Code, 100);
            cargo.Name = Truncate(source.Name, 200) ?? source.Code ?? $"Cargo {source.Id}";
            cargo.Description = source.Description;
            cargo.Requirements = source.Requirements;
            cargo.RoleFamilyId = source.RoleFamily?.Id;
            cargo.RoleFamilyName = Truncate(source.RoleFamily?.Name, 100);
            cargo.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        var requestedAreaIds = cargos
            .SelectMany(cargo => cargo.AreaIds ?? [])
            .Distinct()
            .ToArray();
        var areaIds = await db.Areas
            .Where(area => requestedAreaIds.Contains(area.AreaId))
            .Select(area => area.AreaId)
            .ToListAsync(cancellationToken);
        var validAreas = areaIds.ToHashSet();

        var links = db.Set<Dictionary<string, object>>("CargoArea");
        var existingLinks = await links
            .Where(link => ids.Contains(EF.Property<int>(link, "CargoID")))
            .ToListAsync(cancellationToken);
        foreach (var link in existingLinks)
        {
            links.Remove(link);
        }

        var added = 0;
        var seen = new HashSet<(int CargoId, int AreaId)>();
        foreach (var cargo in cargos)
        {
            foreach (var areaId in cargo.AreaIds ?? [])
            {
                if (!validAreas.Contains(areaId) || !seen.Add((cargo.Id, areaId))) continue;

                links.Add(new Dictionary<string, object>
                {
                    ["CargoID"] = cargo.Id,
                    ["AreaID"] = areaId
                });
                added++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return added;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string? BuildFullName(BukEmployee employee)
    {
        var fullName = string.Join(' ', new[] { employee.FirstName, employee.Surname, employee.SecondSurname }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return Truncate(fullName, 200);
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;
}
