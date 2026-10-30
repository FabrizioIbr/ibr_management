using Ibr.Application.Vacations;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Ibr.Infrastructure.Vacations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Tests;

public sealed class VacationTeamServiceTests : IAsyncLifetime
{
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), "ibr-vac-tests-" + Guid.NewGuid().ToString("N"));
    private SqliteConnection _connection = null!;
    private IbrDbContext _db = null!;
    private VacationTeamService _service = null!;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _connection.CreateFunction("getdate", () => DateTime.Now);

        var options = new DbContextOptionsBuilder<IbrDbContext>().UseSqlite(_connection).Options;
        _db = new IbrDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _db.Companies.Add(new Company { CompanyId = 2, Name = "IBR Perú", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });
        _db.Employees.AddRange(
            Employee(1, "Jefa Uno", null, "activo"),
            Employee(2, "Beto Bajo", 1, "activo"),
            Employee(3, "Ceco Nieto", 2, "activo"),
            Employee(4, "Otro Jefe", null, "activo"),
            Employee(5, "Inactivo Fuera", 1, "inactivo"),
            Employee(6, "Huérfano", 4, "activo"),
            Employee(7, "Ajuste Caso", 4, "activo"));

        _db.VacationPeriods.AddRange(
            new VacationPeriod { EmployeeId = 2, PeriodStartDate = Today.AddYears(-2), PeriodEndDate = Today.AddYears(-1).AddDays(-1), EarnedDays = 30, ExpirationDate = Today.AddDays(-1) },
            new VacationPeriod { EmployeeId = 2, PeriodStartDate = Today.AddYears(-1), PeriodEndDate = Today.AddDays(-1), EarnedDays = 30, ExpirationDate = Today.AddDays(30) },
            new VacationPeriod { EmployeeId = 3, PeriodStartDate = Today.AddYears(-1), PeriodEndDate = Today.AddDays(-1), EarnedDays = 30, ExpirationDate = Today.AddDays(200) },
            new VacationPeriod { EmployeeId = 7, PeriodStartDate = new DateOnly(2022, 6, 1), PeriodEndDate = new DateOnly(2023, 5, 31), EarnedDays = 30, ExpirationDate = Today.AddDays(200) },
            new VacationPeriod { EmployeeId = 7, PeriodStartDate = new DateOnly(2022, 3, 1), PeriodEndDate = new DateOnly(2022, 8, 31), EarnedDays = -19 });

        _db.VacationRequests.AddRange(
            Request(10, 2, "approved", 35),
            Request(11, 2, "submitted", 5));

        await _db.SaveChangesAsync();

        var settings = new VacationSettingsService(_settingsPath);
        _service = new VacationTeamService(_db, settings, new VacationAdminService(_db));
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
        if (Directory.Exists(_settingsPath)) Directory.Delete(_settingsPath, recursive: true);
    }

    [Fact]
    public async Task Balances_include_the_boss_and_active_subordinate_tree()
    {
        var balances = await _service.GetBalancesAsync(bossEmployeeId: 1, unrestricted: false);

        Assert.Equal(new[] { "Beto Bajo", "Ceco Nieto", "Jefa Uno" }, balances.Select(balance => balance.FullName));
    }

    [Fact]
    public async Task Unrestricted_mode_sees_every_active_employee_of_company()
    {
        var balances = await _service.GetBalancesAsync(bossEmployeeId: 0, unrestricted: true);

        Assert.DoesNotContain(balances, balance => balance.FullName == "Inactivo Fuera");
        Assert.Contains(balances, balance => balance.EmployeeId == 4);
    }

    [Fact]
    public async Task Fifo_risk_marks_vencido_and_critico_windows()
    {
        var balances = await _service.GetBalancesAsync(bossEmployeeId: 1, unrestricted: false);
        var beto = balances.Single(balance => balance.EmployeeId == 2);

        // 60 generados - 35 gozados FIFO: 30 al primer periodo y 5 al segundo (saldo 25 vence en 30 días).
        Assert.Equal(60m, beto.DaysGenerated);
        Assert.Equal(25m, beto.DaysPending);
        Assert.Equal(25m, beto.DaysCritico);
        Assert.Equal(0m, beto.DaysVencido);
        Assert.Equal(VacationTeamService.LevelCritico, beto.RiskLevel);

        var ceco = balances.Single(balance => balance.EmployeeId == 3);
        Assert.Equal(VacationTeamService.LevelNormal, ceco.RiskLevel);
    }

    [Fact]
    public async Task Dashboard_aggregates_team_and_top_critical()
    {
        var dashboard = await _service.GetDashboardAsync(bossEmployeeId: 1, unrestricted: false);

        Assert.Equal(3, dashboard.TeamSize);
        Assert.Equal(1, dashboard.MembersAtRisk);
        Assert.Contains(dashboard.TopCritical, member => member.EmployeeId == 2);
        Assert.Equal(1, dashboard.RiskCounts[VacationTeamService.LevelCritico]);
        Assert.Equal(2, dashboard.RiskCounts[VacationTeamService.LevelNormal]);
    }

    [Fact]
    public async Task Requests_filter_by_status_and_respect_hierarchy()
    {
        var solicitado = await _service.GetTeamRequestsAsync(1, false, "solicitado", null, null);
        var todas = await _service.GetTeamRequestsAsync(1, false, null, null, null);
        var porBeto = await _service.GetTeamRequestsAsync(1, false, null, null, null, "Beto Bajo");
        var porCeco = await _service.GetTeamRequestsAsync(1, false, null, null, null, "Ceco");
        var fueraDelArbol = await _service.GetTeamRequestsAsync(1, false, null, null, null, "Otro");

        Assert.Single(solicitado);
        Assert.Equal(2, todas.Count);
        Assert.All(todas, request => Assert.Contains(request.EmployeeName, new[] { "Beto Bajo", "Ceco Nieto" }));
        Assert.Equal(2, porBeto.Count);
        Assert.Empty(porCeco);
        Assert.Empty(fueraDelArbol);
    }

    [Fact]
    public async Task Member_periods_reject_employees_outside_the_tree()
    {
        var denied = await _service.GetMemberPeriodsAsync(1, targetEmployeeId: 4, unrestricted: false);
        var allowed = await _service.GetMemberPeriodsAsync(1, targetEmployeeId: 2, unrestricted: false);
        var adminView = await _service.GetMemberPeriodsAsync(0, targetEmployeeId: 4, unrestricted: true);

        Assert.Null(denied);
        Assert.NotNull(allowed);
        Assert.NotNull(adminView);
    }

    [Fact]
    public async Task Colaborador_resumen_computes_expiring_days_and_scope()
    {
        var denied = await _service.GetColaboradorResumenAsync(1, targetEmployeeId: 4, unrestricted: false);
        var beto = await _service.GetColaboradorResumenAsync(1, targetEmployeeId: 2, unrestricted: false);

        Assert.Null(denied);
        Assert.NotNull(beto);

        // FIFO: 35 gozados consumen el primer periodo (agotado) y 5 del segundo; quedan 25 que vencen en 30 días.
        Assert.Equal(25m, beto.DiasDisponibles);
        Assert.Equal(25m, beto.DiasProximosAVencer);
        Assert.Equal(0m, beto.DiasVencidos);
        Assert.Equal(0m, beto.DiasAcumulando);
        Assert.Equal(30, beto.DiasFaltantesParaVencer);
        Assert.Equal(Today.AddDays(30), beto.FechaVencimientoProxima);
        Assert.Equal(new[] { "agotado", "por_vencer" }, beto.Periodos.Select(periodo => periodo.Estado));
        Assert.Equal(2, beto.Solicitudes.Count);
    }

    [Fact]
    public async Task Colaborador_resumen_handles_negative_adjustments()
    {
        var resumen = await _service.GetColaboradorResumenAsync(4, targetEmployeeId: 7, unrestricted: false);

        Assert.NotNull(resumen);

        // El ajuste de -19 consume 19 de los 30 días del período vigente: quedan 11 y la suma no se rompe.
        Assert.Equal(11m, resumen.DiasDisponibles);
        Assert.Equal(11m, resumen.DiasAcumulando);
        Assert.Equal(0m, resumen.DiasVencidos);
        Assert.Equal(0m, resumen.DiasProximosAVencer);

        var ajuste = resumen.Periodos.Single(periodo => periodo.Estado == "ajustado");
        Assert.Equal(-19m, ajuste.DiasGanados);
        Assert.Equal(-19m, ajuste.DiasGozados);
        Assert.Equal(0m, ajuste.Saldo);
        Assert.Equal("Ajuste Especial", ajuste.AnioPeriodo);

        // El ajuste del mismo año queda justo debajo del período original.
        Assert.Equal("2022 - 2023", resumen.Periodos[0].AnioPeriodo);
        Assert.Equal("ajustado", resumen.Periodos[1].Estado);
    }

    [Fact]
    public async Task Settings_validate_and_persist()
    {
        var settings = new VacationSettingsService(_settingsPath);

        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(() => settings.SaveAsync(30, 60));
        Assert.Contains("críticos", invalid.Message);

        var saved = await settings.SaveAsync(120, 45);
        Assert.Equal((120, 45), (saved.AlertDays, saved.CriticalDays));

        var reloaded = new VacationSettingsService(_settingsPath);
        Assert.Equal((120, 45), (reloaded.Current.AlertDays, reloaded.Current.CriticalDays));
    }

    private static Employee Employee(int id, string name, int? bossId, string status) =>
        new() { EmployeeId = id, CompanyId = 2, FullName = name, Email = $"{id}@example.test", BossId = bossId, Status = status };

    private static VacationRequest Request(int id, int employeeId, string status, decimal calendarDays) =>
        new()
        {
            VacationId = id,
            EmployeeId = employeeId,
            StartDate = Today,
            EndDate = Today.AddDays(1),
            CalendarDays = calendarDays,
            WorkingDays = calendarDays,
            Status = status
        };
}
