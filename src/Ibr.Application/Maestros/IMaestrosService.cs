namespace Ibr.Application.Maestros;

/// <summary>
/// CRUD genérico de catálogos maestros. Los registros viajan como diccionarios
/// (nombre de campo → valor) descritos por <see cref="CatalogSchema"/>.
/// </summary>
public interface IMaestrosService
{
    IReadOnlyList<CatalogSchema> GetCatalogs();

    CatalogSchema? FindCatalog(string slug);

    Task<IReadOnlyList<IDictionary<string, object?>>> GetRecordsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Opciones de cada campo Lookup/MultiLookup, indexadas por nombre de campo.</summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<LookupOption>>> GetLookupsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Crea (clave vacía) o actualiza un registro.</summary>
    Task<SaveResult> SaveAsync(string slug, IDictionary<string, object?> values, CancellationToken cancellationToken = default);

    Task<SaveResult> DeleteAsync(string slug, object key, CancellationToken cancellationToken = default);
}
