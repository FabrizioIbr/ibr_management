using System.Text.Json;
using Ibr.Application.Vacations;

namespace Ibr.Infrastructure.Vacations;

public sealed class VacationSettingsService : IVacationSettingsService, IDisposable
{
    private static readonly VacationSettingsDto Fallback = new(90, 60);
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private VacationSettingsDto _current;

    public VacationSettingsService(string? contentRootPath)
    {
        _path = Path.Combine(contentRootPath ?? AppContext.BaseDirectory, "App_Data", "vacaciones-config.json");
        _current = Read();
    }

    public VacationSettingsDto Current => _current;

    public async Task<VacationSettingsDto> SaveAsync(int alertDays, int criticalDays, CancellationToken cancellationToken = default)
    {
        if (alertDays is < 1 or > 365)
        {
            throw new InvalidOperationException("Los días de alerta deben estar entre 1 y 365.");
        }

        if (criticalDays is < 1 or > 365)
        {
            throw new InvalidOperationException("Los días críticos deben estar entre 1 y 365.");
        }

        if (criticalDays >= alertDays)
        {
            throw new InvalidOperationException("Los días críticos deben ser menores que los días de alerta.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var updated = new VacationSettingsDto(alertDays, criticalDays);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            _current = updated;
            return updated;
        }
        finally
        {
            _gate.Release();
        }
    }

    private VacationSettingsDto Read()
    {
        try
        {
            if (!File.Exists(_path)) return Fallback;
            var stored = JsonSerializer.Deserialize<VacationSettingsDto>(File.ReadAllText(_path));
            return stored is not null && stored.AlertDays > 0 && stored.CriticalDays > 0 && stored.CriticalDays < stored.AlertDays
                ? stored
                : Fallback;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return Fallback;
        }
    }

    public void Dispose() => _gate.Dispose();
}
