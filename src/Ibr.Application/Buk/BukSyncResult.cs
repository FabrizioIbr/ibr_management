namespace Ibr.Application.Buk;

public sealed record BukSyncResult(
    int Areas,
    int Cargos,
    int CargoAreas,
    int EmployeesRead,
    int EmployeesSaved,
    int NewUsers,
    DateTimeOffset SyncedAt);
