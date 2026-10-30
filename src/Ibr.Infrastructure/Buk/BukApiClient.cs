using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Ibr.Infrastructure.Buk;

internal interface IBukApiClient
{
    Task<IReadOnlyList<BukArea>> GetAreasAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<BukCargo>> GetCargosAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<BukEmployee>> GetEmployeesAsync(string token, CancellationToken cancellationToken);

    Task<BukEmployee?> GetEmployeeAsync(int employeeId, string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<BukEarnedVacation>> GetEarnedVacationsAsync(int employeeId, string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<BukVacationRequest>> GetRequestedVacationsAsync(string token, CancellationToken cancellationToken);
}

internal sealed class BukApiClient(IHttpClientFactory httpClientFactory, IOptions<BukOptions> options) : IBukApiClient
{
    private const int MinPageSize = 25;
    private const int MaxPageSize = 100;
    private const int MaxPages = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<BukArea>> GetAreasAsync(string token, CancellationToken cancellationToken) =>
        GetAllAsync<BukArea>("organization/areas?status=both", token, cancellationToken);

    public Task<IReadOnlyList<BukCargo>> GetCargosAsync(string token, CancellationToken cancellationToken) =>
        GetAllAsync<BukCargo>("roles", token, cancellationToken);

    public Task<IReadOnlyList<BukEmployee>> GetEmployeesAsync(string token, CancellationToken cancellationToken) =>
        GetAllAsync<BukEmployee>("employees/active", token, cancellationToken);

    public async Task<BukEmployee?> GetEmployeeAsync(int employeeId, string token, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.HasBaseUrl) throw new InvalidOperationException("Falta configurar Buk:BaseUrl.");
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Falta el token de Buk.");

        var client = httpClientFactory.CreateClient(nameof(BukApiClient));
        var maxRetries = Math.Max(0, settings.EmployeeDetailMaxRetries);
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(new Uri(settings.BaseUrl!.TrimEnd('/') + "/"), $"employees/{employeeId}"));
            request.Headers.TryAddWithoutValidation("auth_token", token);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<BukEmployeeResponse>(JsonOptions, cancellationToken);
                return payload?.Data;
            }

            var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (!retryable || attempt >= maxRetries)
            {
                throw new InvalidOperationException($"Buk respondió HTTP {(int)response.StatusCode} para el empleado {employeeId}.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)), cancellationToken);
        }
    }

    public Task<IReadOnlyList<BukEarnedVacation>> GetEarnedVacationsAsync(int employeeId, string token, CancellationToken cancellationToken) =>
        GetAllAsync<BukEarnedVacation>($"employees/{employeeId}/earned_vacations", token, cancellationToken);

    public Task<IReadOnlyList<BukVacationRequest>> GetRequestedVacationsAsync(string token, CancellationToken cancellationToken) =>
        GetAllAsync<BukVacationRequest>("vacations/requested", token, cancellationToken);

    private async Task<IReadOnlyList<T>> GetAllAsync<T>(string path, string token, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.HasBaseUrl)
        {
            throw new InvalidOperationException(
                "Falta configurar Buk:BaseUrl. Revisa appsettings o los secretos de la aplicación.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Falta el token de Buk. Ingrésalo en la pantalla de integración o configúralo en User Secrets / Azure Key Vault.");
        }

        var baseUri = new Uri(settings.BaseUrl!.TrimEnd('/') + "/", UriKind.Absolute);
        var pageSize = Math.Clamp(settings.PageSize, MinPageSize, MaxPageSize);
        var client = httpClientFactory.CreateClient(nameof(BukApiClient));
        var items = new List<T>();

        for (var page = 1; page <= MaxPages; page++)
        {
            if (page > 1 && options.Value.RequestDelayMilliseconds > 0)
            {
                await Task.Delay(options.Value.RequestDelayMilliseconds, cancellationToken);
            }

            var separator = path.Contains('?') ? '&' : '?';
            var uri = new Uri(baseUri, $"{path}{separator}page={page}&page_size={pageSize}");

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("auth_token", token);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new InvalidOperationException("No se pudo conectar con la API de Buk. Revisa Buk:BaseUrl y la red.", exception);
            }

            using (response)
            {
                await EnsureSuccessAsync(response, path, cancellationToken);

                var payload = await response.Content.ReadFromJsonAsync<BukPage<T>>(JsonOptions, cancellationToken)
                    ?? throw new InvalidOperationException("Buk devolvió una respuesta vacía o inválida.");

                items.AddRange(payload.Data ?? []);
                var totalPages = payload.Pagination?.TotalPages ?? 1;
                if (page >= totalPages) break;
            }
        }

        return items;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var code = (int)response.StatusCode;
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Buk rechazó las credenciales. Revisa que Buk:Token sea válido y tenga permisos de lectura.",
            HttpStatusCode.NotFound =>
                $"Buk no encontró el endpoint '{path}'. Revisa que Buk:BaseUrl incluya el país correcto (ej. /api/v1/chile).",
            HttpStatusCode.TooManyRequests =>
                "Buk limitó las solicitudes (HTTP 429). Espera unos minutos y vuelve a intentar.",
            _ =>
                $"Buk respondió HTTP {code} al consultar '{path}'."
        };

        await response.Content.ReadAsByteArrayAsync(cancellationToken);
        throw new InvalidOperationException(message);
    }
}
