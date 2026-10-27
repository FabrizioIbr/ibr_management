using System.Globalization;
using System.Reflection;
using Ibr.Application.Maestros;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ibr.Infrastructure.Maestros;

internal sealed record CatalogContext(IbrDbContext Db, IServiceProvider Services);

/// <summary>Contrato no genérico para que el servicio y los lookups operen sobre cualquier catálogo.</summary>
internal abstract class CatalogDefinition
{
    public required string Slug { get; init; }
    public required string Label { get; init; }
    public required string Singular { get; init; }
    public required string Icon { get; init; }
    public required string Group { get; init; }
    public required string Description { get; init; }

    /// <summary>Género gramatical de <see cref="Singular"/> ("Nueva posición").</summary>
    public bool Feminine { get; init; }

    public abstract Type EntityType { get; }

    public abstract CatalogSchema GetSchema(IModel model);

    public abstract Task<IReadOnlyList<IDictionary<string, object?>>> GetRecordsAsync(CatalogContext context, CancellationToken cancellationToken);

    public abstract Task<IReadOnlyList<LookupOption>> GetOptionsAsync(CatalogContext context, CancellationToken cancellationToken);

    public abstract Task<SaveResult> SaveAsync(CatalogContext context, IDictionary<string, object?> values, CancellationToken cancellationToken);

    public abstract Task<SaveResult> DeleteAsync(CatalogContext context, object key, CancellationToken cancellationToken);
}

/// <summary>
/// Catálogo de una entidad EF. Tipos, obligatoriedad, longitudes y relaciones se leen
/// de la metadata de EF Core; aquí solo se declaran qué campos se muestran y con qué etiqueta.
/// </summary>
internal sealed class CatalogDefinition<TEntity> : CatalogDefinition where TEntity : class, new()
{
    private const string PasswordField = "__password";

    private readonly List<FieldDeclaration> _fields = [];
    private Action<TEntity, string, IServiceProvider>? _applyPassword;
    private CatalogSchema? _schema;

    /// <summary>Texto legible del registro (columna principal del grid y texto en lookups).</summary>
    public required Func<TEntity, string> Display { get; init; }

    /// <summary>Encabezado de la columna principal.</summary>
    public string TitleLabel { get; init; } = "Nombre";

    /// <summary>Campo bool que indica si el registro está activo (badge y KPIs).</summary>
    public string? StatusField { get; init; }

    /// <summary>Campo DateTime? de borrado lógico. Si existe, eliminar solo lo marca.</summary>
    public string? SoftDeleteField { get; init; }

    public override Type EntityType => typeof(TEntity);

    /// <summary>Campo de la entidad. Con <paramref name="options"/> se edita como lista fija (<see cref="FieldKind.Choice"/>).</summary>
    public CatalogDefinition<TEntity> Field(string property, string label, FieldKind? kind = null, bool grid = true, bool form = true, IReadOnlyList<string>? options = null)
    {
        _fields.Add(new FieldDeclaration(property, label, options is null ? kind : FieldKind.Choice, grid, form, IsMany: false, options));
        return this;
    }

    /// <summary>Relación N:N (skip navigation de EF) editada como selección múltiple.</summary>
    public CatalogDefinition<TEntity> Many(string navigation, string label, bool grid = true)
    {
        _fields.Add(new FieldDeclaration(navigation, label, FieldKind.MultiLookup, grid, Form: true, IsMany: true));
        return this;
    }

    /// <summary>Campo virtual de contraseña: obligatorio al crear, opcional al editar.</summary>
    public CatalogDefinition<TEntity> Password(string label, Action<TEntity, string, IServiceProvider> apply)
    {
        _applyPassword = apply;
        _fields.Add(new FieldDeclaration(PasswordField, label, FieldKind.Password, Grid: false, Form: true, IsMany: false));
        return this;
    }

    public override CatalogSchema GetSchema(IModel model) => _schema ??= BuildSchema(model);

    public override async Task<IReadOnlyList<IDictionary<string, object?>>> GetRecordsAsync(CatalogContext context, CancellationToken cancellationToken)
    {
        var schema = GetSchema(context.Db.Model);
        var entityType = EntityMetadata(context.Db.Model);
        var keyProperty = KeyProperty(entityType);

        var query = Query(context.Db).AsNoTracking();
        foreach (var many in _fields.Where(field => field.IsMany))
        {
            query = query.Include(many.Name);
        }

        var entities = await query.ToListAsync(cancellationToken);

        var texts = new Dictionary<string, IReadOnlyDictionary<object, string>>();
        foreach (var field in schema.Fields.Where(field => field.Kind is FieldKind.Lookup or FieldKind.MultiLookup))
        {
            var options = await TargetCatalog(entityType, field).GetOptionsAsync(context, cancellationToken);
            texts[field.Name] = options.ToDictionary(option => option.Value, option => option.Text);
        }

        return entities.Select(entity =>
        {
            IDictionary<string, object?> row = new Dictionary<string, object?>
            {
                [schema.KeyField] = keyProperty.PropertyInfo!.GetValue(entity),
                [CatalogSchema.TitleKey] = Display(entity)
            };

            foreach (var field in schema.Fields.Where(field => !field.IsKey && field.Kind != FieldKind.Password))
            {
                if (field.Kind == FieldKind.MultiLookup)
                {
                    var skip = entityType.FindSkipNavigation(field.Name)!;
                    var targetKey = KeyProperty(skip.TargetEntityType).PropertyInfo!;
                    var ids = ((System.Collections.IEnumerable)skip.PropertyInfo!.GetValue(entity)!)
                        .Cast<object>().Select(target => targetKey.GetValue(target)!).ToList();
                    row[field.Name] = ids;
                    row[field.DisplayKey] = string.Join(", ", ids.Select(id => texts[field.Name].GetValueOrDefault(id, id.ToString()!)).Order());
                    continue;
                }

                var value = entityType.FindProperty(field.Name)!.PropertyInfo!.GetValue(entity);
                row[field.Name] = value;
                if (field.Kind == FieldKind.Lookup)
                {
                    row[field.DisplayKey] = value is null ? null : texts[field.Name].GetValueOrDefault(value, value.ToString()!);
                }
            }

            return row;
        }).OrderBy(row => (string?)row[CatalogSchema.TitleKey]).ToList();
    }

    public override async Task<IReadOnlyList<LookupOption>> GetOptionsAsync(CatalogContext context, CancellationToken cancellationToken)
    {
        var keyProperty = KeyProperty(EntityMetadata(context.Db.Model)).PropertyInfo!;
        var entities = await Query(context.Db).AsNoTracking().ToListAsync(cancellationToken);
        return entities
            .Select(entity => new LookupOption(keyProperty.GetValue(entity)!, Display(entity)))
            .OrderBy(option => option.Text)
            .ToList();
    }

    public override async Task<SaveResult> SaveAsync(CatalogContext context, IDictionary<string, object?> values, CancellationToken cancellationToken)
    {
        var db = context.Db;
        var schema = GetSchema(db.Model);
        var entityType = EntityMetadata(db.Model);
        var keyProperty = KeyProperty(entityType);
        var keyGenerated = keyProperty.ValueGenerated != ValueGenerated.Never;

        values.TryGetValue(schema.KeyField, out var rawKey);
        var key = ConvertValue(rawKey, keyProperty.ClrType);
        var hasKey = key is not null && !Equals(key, DefaultOf(keyProperty.ClrType));
        var entity = hasKey ? await db.FindAsync<TEntity>([key], cancellationToken) : null;

        if (entity is null && hasKey && keyGenerated)
        {
            return SaveResult.Fail("El registro ya no existe. Recarga la lista.");
        }

        var isNew = entity is null;
        if (isNew && !keyGenerated && !hasKey)
        {
            return SaveResult.Fail($"{schema.Fields.First(field => field.IsKey).Label} es obligatorio.");
        }

        var error = Validate(schema, values, isNew);
        if (error is not null)
        {
            return SaveResult.Fail(error);
        }

        entity ??= new TEntity();
        if (isNew && !keyGenerated)
        {
            keyProperty.PropertyInfo!.SetValue(entity, key);
        }

        foreach (var field in schema.Fields.Where(field => field.ShowInForm && !field.IsKey))
        {
            values.TryGetValue(field.Name, out var raw);
            switch (field.Kind)
            {
                case FieldKind.Password:
                    if (raw is string password && !string.IsNullOrWhiteSpace(password))
                    {
                        _applyPassword!(entity, password, context.Services);
                    }
                    break;

                case FieldKind.MultiLookup:
                    var skip = entityType.FindSkipNavigation(field.Name)!;
                    if (!isNew)
                    {
                        await db.Entry(entity).Collection(field.Name).LoadAsync(cancellationToken);
                    }

                    var targetKeyType = KeyProperty(skip.TargetEntityType).ClrType;
                    var targets = new List<object>();
                    foreach (var id in raw as System.Collections.IEnumerable ?? Array.Empty<object>())
                    {
                        var target = await db.FindAsync(skip.TargetEntityType.ClrType, [ConvertValue(id, targetKeyType)], cancellationToken);
                        if (target is not null) targets.Add(target);
                    }

                    ReplaceCollection(skip.PropertyInfo!.GetValue(entity)!, skip.TargetEntityType.ClrType, targets);
                    break;

                default:
                    var property = entityType.FindProperty(field.Name)!;
                    property.PropertyInfo!.SetValue(entity, ConvertValue(raw, property.ClrType));
                    break;
            }
        }

        if (isNew)
        {
            db.Add(entity);
        }

        return await SaveChangesAsync(db, cancellationToken);
    }

    public override async Task<SaveResult> DeleteAsync(CatalogContext context, object key, CancellationToken cancellationToken)
    {
        var db = context.Db;
        var keyProperty = KeyProperty(EntityMetadata(db.Model));
        var entity = await db.FindAsync<TEntity>([ConvertValue(key, keyProperty.ClrType)], cancellationToken);
        if (entity is null)
        {
            return SaveResult.Fail("El registro ya no existe. Recarga la lista.");
        }

        if (SoftDeleteField is not null)
        {
            // Misma convención que los defaults de la BD (getdate()): hora local del servidor.
            db.Entry(entity).Property(SoftDeleteField).CurrentValue = DateTime.Now;
        }
        else
        {
            try
            {
                db.Remove(entity);
            }
            catch (InvalidOperationException)
            {
                db.ChangeTracker.Clear();
                return SaveResult.Fail(InUseMessage);
            }
        }

        return await SaveChangesAsync(db, cancellationToken);
    }

    private IQueryable<TEntity> Query(IbrDbContext db) => SoftDeleteField is null
        ? db.Set<TEntity>()
        : db.Set<TEntity>().Where(entity => EF.Property<DateTime?>(entity, SoftDeleteField) == null);

    private CatalogSchema BuildSchema(IModel model)
    {
        var entityType = EntityMetadata(model);
        var keyProperty = KeyProperty(entityType);
        var keyGenerated = keyProperty.ValueGenerated != ValueGenerated.Never;

        var fields = new List<FieldSchema>
        {
            new(keyProperty.Name, "Código", InferKind(keyProperty), keyProperty.ClrType,
                Required: true, MaxLength: keyProperty.GetMaxLength(), ShowInGrid: false, ShowInForm: !keyGenerated, IsKey: true)
        };

        foreach (var declaration in _fields)
        {
            if (declaration.Kind == FieldKind.Password)
            {
                fields.Add(new FieldSchema(declaration.Name, declaration.Label, FieldKind.Password, typeof(string),
                    ShowInGrid: false, RequiredOnCreate: true));
                continue;
            }

            if (declaration.IsMany)
            {
                _ = entityType.FindSkipNavigation(declaration.Name)
                    ?? throw new InvalidOperationException($"{typeof(TEntity).Name}.{declaration.Name} no es una relación N:N configurada en EF.");
                fields.Add(new FieldSchema(declaration.Name, declaration.Label, FieldKind.MultiLookup, typeof(List<object>),
                    ShowInGrid: declaration.Grid, ShowInForm: declaration.Form));
                continue;
            }

            var property = entityType.FindProperty(declaration.Name)
                ?? throw new InvalidOperationException($"{typeof(TEntity).Name}.{declaration.Name} no es una propiedad mapeada en EF.");
            var isForeignKey = property.GetContainingForeignKeys().Any();
            var kind = declaration.Kind ?? (isForeignKey ? FieldKind.Lookup : InferKind(property));
            var required = !property.IsNullable && kind != FieldKind.Boolean;

            fields.Add(new FieldSchema(property.Name, declaration.Label, kind, property.ClrType,
                Required: required, MaxLength: property.GetMaxLength(), ShowInGrid: declaration.Grid, ShowInForm: declaration.Form, Options: declaration.Options));
        }

        return new CatalogSchema(Slug, Label, Singular, Icon, Group, Description, keyProperty.Name, TitleLabel, StatusField, fields, Feminine);
    }

    private static string? Validate(CatalogSchema schema, IDictionary<string, object?> values, bool isNew)
    {
        foreach (var field in schema.Fields.Where(field => field.ShowInForm && !field.IsKey))
        {
            values.TryGetValue(field.Name, out var raw);
            var isEmpty = raw is null || raw is string text && string.IsNullOrWhiteSpace(text);

            if (isEmpty && (field.Required || field.RequiredOnCreate && isNew))
            {
                return $"{field.Label} es obligatorio.";
            }

            if (raw is string value && field.MaxLength is { } max && value.Trim().Length > max)
            {
                return $"{field.Label} admite como máximo {max} caracteres.";
            }

            if (!isEmpty && field.Options is { } options && !options.Contains(raw?.ToString()?.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                return $"{field.Label} debe ser uno de: {string.Join(", ", options)}.";
            }
        }

        return null;
    }

    private static async Task<SaveResult> SaveChangesAsync(IbrDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return SaveResult.Ok();
        }
        catch (DbUpdateException exception)
        {
            var message = exception.GetBaseException().Message;
            if (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                return SaveResult.Fail("Ya existe un registro con ese valor. Revisa los campos únicos (nombre, correo…).");
            }

            if (message.Contains("REFERENCE", StringComparison.OrdinalIgnoreCase) || message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                return SaveResult.Fail(InUseMessage);
            }

            return SaveResult.Fail("No se pudo guardar el registro. Inténtalo de nuevo.");
        }
        finally
        {
            // En Blazor Server el DbContext vive todo el circuito: no dejar entidades rastreadas.
            db.ChangeTracker.Clear();
        }
    }

    private const string InUseMessage = "No se puede completar: el registro está relacionado con otros datos.";

    private static CatalogDefinition TargetCatalog(IEntityType entityType, FieldSchema field)
    {
        var targetType = field.Kind == FieldKind.MultiLookup
            ? entityType.FindSkipNavigation(field.Name)!.TargetEntityType.ClrType
            : entityType.FindProperty(field.Name)!.GetContainingForeignKeys().First().PrincipalEntityType.ClrType;

        return MaestrosCatalogRegistry.ForEntity(targetType)
            ?? throw new InvalidOperationException($"Registra un catálogo para {targetType.Name} en MaestrosCatalogRegistry: lo necesita {entityType.ClrType.Name}.{field.Name}.");
    }

    private static IEntityType EntityMetadata(IModel model) =>
        model.FindEntityType(typeof(TEntity)) ?? throw new InvalidOperationException($"{typeof(TEntity).Name} no está mapeada en IbrDbContext.");

    private static IProperty KeyProperty(IReadOnlyEntityType entityType) =>
        (IProperty)entityType.FindPrimaryKey()!.Properties.Single();

    private static FieldKind InferKind(IProperty property)
    {
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return type switch
        {
            _ when type == typeof(string) => property.GetMaxLength() is null or > 255 ? FieldKind.LongText : FieldKind.Text,
            _ when type == typeof(bool) => FieldKind.Boolean,
            _ when type == typeof(DateOnly) => FieldKind.Date,
            _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => FieldKind.DateTime,
            _ when type == typeof(decimal) || type == typeof(double) || type == typeof(float) => FieldKind.Decimal,
            _ when type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) => FieldKind.Integer,
            _ => FieldKind.Text
        };
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            text = text.Trim();
            if (text.Length == 0) return null;
            if (type == typeof(string)) return text;
            if (type == typeof(DateOnly)) return DateOnly.Parse(text, CultureInfo.InvariantCulture);
            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }

        if (type.IsInstanceOfType(value)) return value;
        if (type == typeof(DateOnly) && value is DateTime dateTime) return DateOnly.FromDateTime(dateTime);
        if (type == typeof(string)) return value.ToString();
        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    private static object? DefaultOf(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    private static void ReplaceCollection(object collection, Type itemType, List<object> items) =>
        ReplaceMethod.MakeGenericMethod(itemType).Invoke(null, [collection, items]);

    private static readonly MethodInfo ReplaceMethod =
        typeof(CatalogDefinition<TEntity>).GetMethod(nameof(Replace), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void Replace<TItem>(ICollection<TItem> collection, List<object> items)
    {
        collection.Clear();
        foreach (var item in items.Cast<TItem>())
        {
            collection.Add(item);
        }
    }

    private sealed record FieldDeclaration(string Name, string Label, FieldKind? Kind, bool Grid, bool Form, bool IsMany, IReadOnlyList<string>? Options = null);
}
