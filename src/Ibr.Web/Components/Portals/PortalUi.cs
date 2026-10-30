using System.Security.Claims;
using System.Text.RegularExpressions;
using Ibr.Application.Portals;

namespace Ibr.Web.Components.Portals;

/// <summary>Grupo de secciones con título (caption del sidebar / bloque del inicio del portal).</summary>
public sealed record PortalSectionGroup(string Title, IReadOnlyList<PortalSectionDto> Items);

/// <summary>Ayudas de presentación para los portales que vienen de la BD (tablas Portals / PortalSections).</summary>
public static partial class PortalUi
{
    public const string DefaultColor = "#1525F0";
    public const string DefaultGroup = "Secciones";

    /// <summary>ColorHex validado (#RGB o #RRGGBB); cualquier otro valor usa el azul de la marca.</summary>
    public static string Color(string? hex) => hex is not null && HexColor().IsMatch(hex.Trim()) ? hex.Trim() : DefaultColor;

    /// <summary>Variables CSS de acento para tarjetas y encabezados del portal.</summary>
    public static string AccentStyle(string? hex)
    {
        var color = Color(hex);
        return $"--accent: {color}; --accent-soft: color-mix(in srgb, {color} 12%, #fff);";
    }

    /// <summary>Secciones agrupadas por SectionGroup, en el orden de DisplayOrder.</summary>
    public static IReadOnlyList<PortalSectionGroup> Groups(this PortalDto portal) => portal.Sections
        .GroupBy(section => section.Group ?? DefaultGroup)
        .Select(group => new PortalSectionGroup(group.Key, group.ToList()))
        .ToList();

    /// <summary>UserID de la sesión (claim NameIdentifier) o 0 si no hay.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>EmployeeID vinculado en Usuarios (claim employee_id) o 0 si el usuario no es colaborador.</summary>
    public static int GetEmployeeId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("employee_id"), out var id) ? id : 0;

    /// <summary>Rol con alcance total sobre el portal de vacaciones.</summary>
    public static bool IsVacationAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("System_Admin") || user.IsInRole("RRHH");

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColor();
}
