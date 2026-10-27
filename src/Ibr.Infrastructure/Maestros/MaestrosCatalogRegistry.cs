using Ibr.Application.Maestros;
using Ibr.Application.Portals;
using Ibr.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Ibr.Infrastructure.Maestros;

/// <summary>
/// Registro único de catálogos de Gestión Maestros. Para publicar una tabla nueva:
/// entidad + configuración EF en Persistence, y una entrada aquí. Menú, buscador, grid
/// y formulario se generan solos. Ver docs/ui-standards.md.
/// </summary>
internal static class MaestrosCatalogRegistry
{
    public static readonly IReadOnlyList<CatalogDefinition> All =
    [
        new CatalogDefinition<User>
        {
            Slug = "usuarios", Label = "Usuarios", Singular = "usuario", Icon = "group", Group = "Seguridad",
            Description = "Personas con acceso al portal.",
            Display = user => $"{user.FirstName} {user.LastName}",
            StatusField = nameof(User.IsActive),
            SoftDeleteField = nameof(User.DeletedAt)
        }
            .Field(nameof(User.FirstName), "Nombres", grid: false)
            .Field(nameof(User.LastName), "Apellidos", grid: false)
            .Field(nameof(User.Email), "Correo", FieldKind.Email)
            .Field(nameof(User.PositionId), "Posición")
            .Many(nameof(User.Roles), "Roles")
            .Field(nameof(User.IsActive), "Activo")
            .Password("Contraseña", (user, password, services) =>
                user.PasswordHash = services.GetRequiredService<IPasswordHasher<User>>().HashPassword(user, password)),

        new CatalogDefinition<Role>
        {
            Slug = "roles", Label = "Roles", Singular = "rol", Icon = "badge", Group = "Seguridad",
            Description = "Agrupaciones de permisos asignables a usuarios.",
            Display = role => role.RoleName
        }
            .Field(nameof(Role.RoleName), "Nombre", grid: false)
            .Field(nameof(Role.Description), "Descripción")
            .Many(nameof(Role.Permissions), "Permisos"),

        new CatalogDefinition<Permission>
        {
            Slug = "permisos", Label = "Permisos", Singular = "permiso", Icon = "key", Group = "Seguridad",
            Description = "Acciones autorizables del sistema.",
            Display = permission => permission.PermissionName
        }
            .Field(nameof(Permission.PermissionName), "Nombre", grid: false)
            .Field(nameof(Permission.Description), "Descripción"),

        new CatalogDefinition<Position>
        {
            Slug = "posiciones", Label = "Posiciones", Singular = "posición", Feminine = true, Icon = "work", Group = "Organización",
            Description = "Cargos, su departamento y a quién reportan.",
            Display = position => position.PositionName,
            TitleLabel = "Posición"
        }
            .Field(nameof(Position.PositionName), "Nombre", grid: false)
            .Field(nameof(Position.Department), "Departamento")
            .Field(nameof(Position.ReportsToPositionId), "Reporta a"),

        new CatalogDefinition<Portal>
        {
            Slug = "portales", Label = "Portales", Singular = "portal", Icon = "dashboard", Group = "Configuración",
            Description = "Tarjetas del Dashboard y quién puede verlas.",
            Display = portal => portal.Name,
            StatusField = nameof(Portal.IsActive),
            SoftDeleteField = nameof(Portal.DeletedAt)
        }
            .Field(nameof(Portal.Name), "Nombre", grid: false)
            .Field(nameof(Portal.Description), "Descripción")
            .Field(nameof(Portal.Status), "Estado", options: PortalStatus.PortalOptions)
            .Field(nameof(Portal.ColorHex), "Color (hex, ej. #1525F0)")
            .Field(nameof(Portal.Icon), "Icono (nombre Material o código SVG)", grid: false)
            .Field(nameof(Portal.RoleId), "Rol con acceso")
            .Many(nameof(Portal.Users), "Usuarios con acceso", grid: false)
            .Field(nameof(Portal.IsActive), "Activo"),

        new CatalogDefinition<PortalSection>
        {
            Slug = "secciones", Label = "Secciones", Singular = "sección", Feminine = true, Icon = "view_list", Group = "Configuración",
            Description = "Páginas de cada portal: tarjetas del inicio y enlaces del sidebar.",
            Display = section => section.Title,
            TitleLabel = "Sección",
            StatusField = nameof(PortalSection.IsActive)
        }
            .Field(nameof(PortalSection.Title), "Título", grid: false)
            .Field(nameof(PortalSection.PortalId), "Portal")
            .Field(nameof(PortalSection.SectionGroup), "Grupo del sidebar")
            .Field(nameof(PortalSection.Route), "Ruta (ej. /portal-x/mi-pagina)")
            .Field(nameof(PortalSection.Status), "Estado", options: PortalStatus.SectionOptions)
            .Field(nameof(PortalSection.DisplayOrder), "Orden")
            .Field(nameof(PortalSection.Icon), "Icono (nombre Material o código SVG)", FieldKind.Text, grid: false)
            .Field(nameof(PortalSection.Description), "Descripción", grid: false)
            .Field(nameof(PortalSection.IsActive), "Visible"),
    ];

    private static readonly Dictionary<Type, CatalogDefinition> ByEntity = All.ToDictionary(catalog => catalog.EntityType);

    public static CatalogDefinition? Find(string slug) =>
        All.FirstOrDefault(catalog => string.Equals(catalog.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public static CatalogDefinition? ForEntity(Type entityType) => ByEntity.GetValueOrDefault(entityType);
}
