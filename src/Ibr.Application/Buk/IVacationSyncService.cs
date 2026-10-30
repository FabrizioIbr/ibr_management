namespace Ibr.Application.Buk;

public interface IVacationSyncService
{
    Task<VacationSyncResult> SyncAsync(string? token = null, CancellationToken cancellationToken = default);
}
