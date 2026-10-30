using Ibr.Application.Buk;
using Ibr.Infrastructure.Buk;
using Microsoft.Extensions.Options;

namespace Ibr.Web.Services;

/// <summary>
/// Importa áreas, cargos y empleados activos desde Buk al iniciar la aplicación cuando
/// <c>Buk:AutoSync</c> está activo y hay token configurado (.env, User Secrets o Key Vault).
/// </summary>
internal sealed class BukAutoSyncService(
    IServiceScopeFactory scopeFactory,
    IOptions<BukOptions> options,
    ILogger<BukAutoSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.AutoSync)
        {
            return;
        }

        if (!options.Value.IsConfigured)
        {
            logger.LogInformation("Importación automática de Buk omitida: falta configurar Buk:BaseUrl o Buk:Token.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<IBukSyncService>();
            var result = await sync.SyncAsync(cancellationToken: stoppingToken);
            var vacationSync = scope.ServiceProvider.GetRequiredService<IVacationSyncService>();
            var vacationResult = await vacationSync.SyncAsync(cancellationToken: stoppingToken);

            logger.LogInformation(
                "Importación automática de Buk completada: {Areas} áreas, {Cargos} cargos, {CargoAreas} relaciones, {Employees} empleados, {NewUsers} usuarios nuevos, {Periods} periodos y {Requests} solicitudes.",
                result.Areas, result.Cargos, result.CargoAreas, result.EmployeesSaved, result.NewUsers,
                vacationResult.Periods, vacationResult.Requests);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo completar la importación automática de Buk.");
        }
    }
}
