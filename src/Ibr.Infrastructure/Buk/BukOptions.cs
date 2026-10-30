namespace Ibr.Infrastructure.Buk;

/// <summary>
/// Configuración de la API de Buk. Se enlaza desde la sección <c>Buk</c>.
/// El token no debe vivir en appsettings ni en el código: User Secrets en desarrollo
/// y Azure Key Vault / variables de entorno en producción.
/// </summary>
public sealed class BukOptions
{
    public const string SectionName = "Buk";

    /// <summary>URL base del tenant, incluyendo país. Ej.: https://ibrlatam.buk.pe/api/v1/peru/</summary>
    public string? BaseUrl { get; set; }

    /// <summary>API Key (header auth_token) generada en Buk → Configuración → Accesos API.</summary>
    public string? Token { get; set; }

    /// <summary>Tamaño de página entre 25 y 100. Buk usa 25 por defecto.</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>Ejecuta la importación de áreas, cargos y empleados activos al iniciar la aplicación.</summary>
    public bool AutoSync { get; set; }

    /// <summary>Empresa de Buk que se importa como IBR Perú.</summary>
    public int CompanyId { get; set; } = 2;

    /// <summary>Texto que identifica áreas excluidas del catálogo local.</summary>
    public string ExcludedAreaMarker { get; set; } = "ELIMINAR";

    /// <summary>Pausa entre consultas individuales de detalle de empleados.</summary>
    public int EmployeeDetailDelayMilliseconds { get; set; } = 200;

    /// <summary>Máximo de reintentos para 429 y errores 5xx al consultar empleados.</summary>
    public int EmployeeDetailMaxRetries { get; set; } = 3;

    /// <summary>Pausa entre llamadas paginadas a Buk.</summary>
    public int RequestDelayMilliseconds { get; set; } = 200;

    public bool HasBaseUrl => Uri.TryCreate(BaseUrl, UriKind.Absolute, out _);

    public bool HasToken => !string.IsNullOrWhiteSpace(Token);

    public bool IsConfigured => HasBaseUrl && HasToken;
}
