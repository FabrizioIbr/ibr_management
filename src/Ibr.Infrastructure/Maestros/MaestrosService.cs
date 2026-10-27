using Ibr.Application.Maestros;
using Ibr.Infrastructure.Persistence;

namespace Ibr.Infrastructure.Maestros;

public sealed class MaestrosService(IbrDbContext dbContext, IServiceProvider services) : IMaestrosService
{
    private readonly CatalogContext _context = new(dbContext, services);

    public IReadOnlyList<CatalogSchema> GetCatalogs() =>
        MaestrosCatalogRegistry.All.Select(catalog => catalog.GetSchema(dbContext.Model)).ToList();

    public CatalogSchema? FindCatalog(string slug) =>
        MaestrosCatalogRegistry.Find(slug)?.GetSchema(dbContext.Model);

    public Task<IReadOnlyList<IDictionary<string, object?>>> GetRecordsAsync(string slug, CancellationToken cancellationToken = default) =>
        Catalog(slug).GetRecordsAsync(_context, cancellationToken);

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<LookupOption>>> GetLookupsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var catalog = Catalog(slug);
        var entityType = dbContext.Model.FindEntityType(catalog.EntityType)!;
        var lookups = new Dictionary<string, IReadOnlyList<LookupOption>>();

        foreach (var field in catalog.GetSchema(dbContext.Model).Fields.Where(field => field.ShowInForm))
        {
            var targetType = field.Kind switch
            {
                FieldKind.Lookup => entityType.FindProperty(field.Name)!.GetContainingForeignKeys().First().PrincipalEntityType.ClrType,
                FieldKind.MultiLookup => entityType.FindSkipNavigation(field.Name)!.TargetEntityType.ClrType,
                _ => null
            };

            if (targetType is not null && MaestrosCatalogRegistry.ForEntity(targetType) is { } target)
            {
                lookups[field.Name] = await target.GetOptionsAsync(_context, cancellationToken);
            }
        }

        return lookups;
    }

    public Task<SaveResult> SaveAsync(string slug, IDictionary<string, object?> values, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();
        return Catalog(slug).SaveAsync(_context, values, cancellationToken);
    }

    public Task<SaveResult> DeleteAsync(string slug, object key, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();
        return Catalog(slug).DeleteAsync(_context, key, cancellationToken);
    }

    private static CatalogDefinition Catalog(string slug) =>
        MaestrosCatalogRegistry.Find(slug) ?? throw new KeyNotFoundException($"Catálogo '{slug}' no registrado.");
}
