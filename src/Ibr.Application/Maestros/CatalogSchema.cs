namespace Ibr.Application.Maestros;

/// <summary>Tipo de control que la UI usa para un campo.</summary>
public enum FieldKind
{
    Text,
    LongText,
    Email,
    Password,
    Integer,
    Decimal,
    Boolean,
    Date,
    DateTime,
    /// <summary>Lista fija de valores de texto (<see cref="FieldSchema.Options"/>).</summary>
    Choice,
    /// <summary>FK a otro catálogo: lista desplegable de una opción.</summary>
    Lookup,
    /// <summary>Relación N:N: selección múltiple.</summary>
    MultiLookup
}

/// <summary>
/// Describe un campo de un catálogo. Se genera a partir de la metadata de EF Core
/// (tipo, obligatoriedad, longitud, FK) más las etiquetas declaradas en el registro.
/// </summary>
public sealed record FieldSchema(
    string Name,
    string Label,
    FieldKind Kind,
    Type ValueType,
    bool Required = false,
    int? MaxLength = null,
    bool ShowInGrid = true,
    bool ShowInForm = true,
    bool IsKey = false,
    bool RequiredOnCreate = false,
    IReadOnlyList<string>? Options = null)
{
    /// <summary>Clave del texto legible de un Lookup/MultiLookup dentro del registro.</summary>
    public string DisplayKey => Kind is FieldKind.Lookup or FieldKind.MultiLookup ? TextKey(Name) : Name;

    /// <summary>Tipo con el que el grid ordena la columna.</summary>
    public Type DisplayType => Kind is FieldKind.Lookup or FieldKind.MultiLookup
        ? typeof(string)
        : ValueType.IsValueType && Nullable.GetUnderlyingType(ValueType) is null
            ? typeof(Nullable<>).MakeGenericType(ValueType)
            : ValueType;

    public static string TextKey(string name) => $"{name}__text";
}

/// <summary>Metadatos de un catálogo de Gestión Maestros.</summary>
public sealed record CatalogSchema(
    string Slug,
    string Label,
    string Singular,
    string Icon,
    string Group,
    string Description,
    string KeyField,
    string TitleLabel,
    string? StatusField,
    IReadOnlyList<FieldSchema> Fields,
    bool Feminine = false)
{
    /// <summary>Clave del texto principal (nombre legible) de cada registro.</summary>
    public const string TitleKey = "__title";

    public string Href => $"/admin/{Slug}";

    /// <summary>"Nuevo usuario" / "Nueva posición".</summary>
    public string NewText => $"{(Feminine ? "Nueva" : "Nuevo")} {Singular}";

    /// <summary>"Usuario creado" / "Posición creada".</summary>
    public string CreatedText => $"{char.ToUpperInvariant(Singular[0])}{Singular[1..]} {(Feminine ? "creada" : "creado")}";
}

public sealed record LookupOption(object Value, string Text);

public sealed record SaveResult(bool Succeeded, string? Error = null)
{
    public static SaveResult Ok() => new(true);
    public static SaveResult Fail(string error) => new(false, error);
}
