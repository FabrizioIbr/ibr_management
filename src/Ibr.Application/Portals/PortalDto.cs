namespace Ibr.Application.Portals;

/// <summary>Sección de un portal (tabla PortalSections): una tarjeta del inicio y un enlace del sidebar.</summary>
public sealed record PortalSectionDto(
    int Id,
    string Title,
    string? Description,
    string? Icon,
    string Route,
    string? Group,
    bool Available);

/// <summary>Portal (tabla Portals) tal como lo ve un usuario concreto.</summary>
public sealed record PortalDto(
    int Id,
    string Name,
    string? Description,
    string ColorHex,
    string? Icon,
    string Status,
    bool Available,
    bool Restricted,
    bool HasAccess,
    IReadOnlyList<PortalSectionDto> Sections)
{
    public string Href => $"/portal/{Id}";

    /// <summary>Indica si la ruta pertenece al portal (su inicio o una sección/subruta de una sección).</summary>
    public bool Owns(string path) => MatchLength(path) > 0;

    /// <summary>Largo de la ruta registrada que coincide con <paramref name="path"/> (0 si ninguna); gana la más específica.</summary>
    public int MatchLength(string path)
    {
        path = PortalStatus.NormalizePath(path);
        if (path == Href) return int.MaxValue;
        return Sections
            .Select(section => PortalStatus.NormalizePath(section.Route))
            .Where(route => route.Length > 1 && (path == route || path.StartsWith(route + "/", StringComparison.Ordinal)))
            .Select(route => route.Length)
            .DefaultIfEmpty(0)
            .Max();
    }
}

/// <summary>Valores de los campos Status de Portals y PortalSections.</summary>
public static class PortalStatus
{
    public const string Available = "Disponible";
    public const string UnderConstruction = "En construcción";
    public const string Maintenance = "Mantenimiento";
    public static readonly IReadOnlyList<string> PortalOptions = [Available, UnderConstruction, Maintenance];

    public const string SectionActive = "Activo";
    public const string SectionSoon = "Próximamente";
    public static readonly IReadOnlyList<string> SectionOptions = [SectionActive, SectionSoon];

    /// <summary>Compara ignorando mayúsculas y tildes ("DISPONIBLE" == "Disponible").</summary>
    public static bool Is(string? value, string status) =>
        string.Compare(value?.Trim(), status, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) == 0;

    /// <summary>Etiqueta canónica de un estado guardado con otra capitalización.</summary>
    public static string Label(string? value, IReadOnlyList<string> options) =>
        options.FirstOrDefault(option => Is(value, option)) ?? value?.Trim() ?? string.Empty;

    public static string NormalizePath(string path)
    {
        var end = path.IndexOfAny(['?', '#']);
        if (end >= 0) path = path[..end];
        path = "/" + path.Trim().Trim('/');
        return path.ToLowerInvariant();
    }
}
