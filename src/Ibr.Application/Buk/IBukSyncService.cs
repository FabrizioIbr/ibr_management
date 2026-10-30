namespace Ibr.Application.Buk;

/// <summary>
/// Sincroniza desde la API de Buk las tablas locales Areas, Cargos, Cargos_Areas,
/// Empleados y Usuarios.
/// El token se lee de configuración (User Secrets / Azure Key Vault) o se recibe en memoria
/// para una sincronización puntual; nunca se persiste ni se expone en la UI.
/// </summary>
public interface IBukSyncService
{
    /// <summary>URL base de Buk configurada, para mostrarla en la pantalla de integración.</summary>
    string? BaseUrl { get; }

    /// <summary>Indica si <c>Buk:BaseUrl</c> tiene una URL válida.</summary>
    bool HasBaseUrl { get; }

    /// <summary>Indica si hay un token guardado en User Secrets / Key Vault.</summary>
    bool HasConfiguredToken { get; }

    /// <summary>Indica si están configurados <c>Buk:BaseUrl</c> y <c>Buk:Token</c>.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Descarga y hace upsert de áreas, cargos y empleados. Si <paramref name="token"/> viene informado,
    /// se usa solo para esta llamada; si no, se usa el token configurado.
    /// </summary>
    Task<BukSyncResult> SyncAsync(string? token = null, CancellationToken cancellationToken = default);
}
