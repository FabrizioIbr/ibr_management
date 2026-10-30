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
            Description = "Agrupaciones asignables a usuarios.",
            Display = role => role.RoleName
        }
            .Field(nameof(Role.RoleName), "Nombre", grid: false)
            .Field(nameof(Role.Description), "Descripción"),

        new CatalogDefinition<Permission>
        {
            Slug = "permisos", Label = "Permisos", Singular = "permiso", Icon = "key", Group = "Seguridad",
            Description = "Acciones autorizables del sistema.",
            Display = permission => permission.PermissionName
        }
            .Field(nameof(Permission.PermissionName), "Nombre", grid: false)
            .Field(nameof(Permission.Description), "Descripción"),

        new CatalogDefinition<Company>
        {
            Slug = "empresas", Label = "Empresas", Singular = "empresa", Icon = "business", Group = "Organización",
            Description = "Empresas sincronizadas desde Buk.",
            Display = company => company.Name,
            StatusField = nameof(Company.IsActive),
            CanDelete = false
        }
            .Field(nameof(Company.Name), "Nombre", grid: false)
            .Field(nameof(Company.Ruc), "RUC")
            .Field(nameof(Company.BusinessActivity), "Actividad comercial", grid: false)
            .Field(nameof(Company.Email), "Correo", FieldKind.Email)
            .Field(nameof(Company.Address), "Dirección", grid: false)
            .Field(nameof(Company.City), "Ciudad")
            .Field(nameof(Company.IsActive), "Activa"),

        new CatalogDefinition<Employee>
        {
            Slug = "empleados", Label = "Empleados", Singular = "empleado", Icon = "badge", Group = "Organización",
            Description = "Directorio de empleados sincronizados desde Buk.",
            Display = employee => employee.FullName,
            TitleLabel = "Empleado",
            CanDelete = false
        }
            .Field(nameof(Employee.FullName), "Nombre completo", grid: false, section: "Información general")
            .Field(nameof(Employee.Email), "Correo Buk", FieldKind.Email, section: "Información general")
            .Field(nameof(Employee.CompanyId), "Empresa", grid: false, section: "Información general")
            .Field(nameof(Employee.HireDate), "Ingreso", FieldKind.Date, section: "Información general")
            .Field(nameof(Employee.AreaId), "Área", section: "Ubicación organizacional")
            .Field(nameof(Employee.CargoId), "Cargo", section: "Ubicación organizacional")
            .Field(nameof(Employee.BossId), "Jefe", section: "Ubicación organizacional")
            .Field(nameof(Employee.Status), "Estado", grid: false, section: "Ubicación organizacional")
            .Field(nameof(Employee.TipoContrato), "Tipo de contrato", grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.RegimenLaboral), "Régimen laboral", grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.JornadaHorasSemana), "Horas semanales", grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.DiasLaboralesSemana), "Días laborales por semana", grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.DiasLaboralesDetalle), "Días laborales", grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.DiasVacacionesAnuales), "Vacaciones anuales", grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.VacacionesProgresivasDesde), "Vacaciones progresivas desde", FieldKind.Date, grid: false, section: "Datos contractuales")
            .Field(nameof(Employee.FechaCese), "Fecha de cese", FieldKind.Date, grid: false, section: "Cese", hideWhenActive: true)
            .Field(nameof(Employee.MotivoCese), "Motivo de cese", grid: false, section: "Cese", hideWhenActive: true),

        new CatalogDefinition<AppUser>
        {
            Slug = "usuarios-app", Label = "Usuarios de la aplicación", Singular = "usuario", Icon = "group", Group = "Seguridad",
            Description = "Cuentas enlazadas a empleados y sus roles de acceso.",
            Display = user => user.EmailCorporativo ?? user.Email,
            CanDelete = false
        }
            .Field(nameof(AppUser.EmailCorporativo), "Correo corporativo", FieldKind.Email, grid: false)
            .Field(nameof(AppUser.Email), "Correo Buk", FieldKind.Email)
            .Field(nameof(AppUser.EmployeeId), "Empleado")
            .Field(nameof(AppUser.AppRole), "Rol de aplicación")
            .Many(nameof(AppUser.Roles), "Roles")
            .Field(nameof(AppUser.IsActive), "Activo")
            .Password("Contraseña", (appUser, password, services) =>
                appUser.PasswordHash = services.GetRequiredService<IPasswordHasher<User>>().HashPassword(
                    new User { UserId = appUser.UserId, Email = appUser.EmailCorporativo ?? appUser.Email }, password)),

        new CatalogDefinition<Area>
        {
            Slug = "areas", Label = "Áreas", Singular = "área", Feminine = true, Icon = "account_tree", Group = "Organización",
            Description = "Áreas y jerarquía importadas desde Buk.",
            Display = area => area.Name,
            TitleLabel = "Área",
            CanDelete = false
        }
            .Field(nameof(Area.Name), "Nombre", grid: false)
            .Field(nameof(Area.Status), "Estado")
            .Field(nameof(Area.ParentAreaId), "Área padre")
            .Field(nameof(Area.DepartmentName), "Departamento")
            .Field(nameof(Area.DivisionName), "División")
            .Field(nameof(Area.CostCenter), "Centro de costo"),

        new CatalogDefinition<Cargo>
        {
            Slug = "cargos", Label = "Cargos", Singular = "cargo", Icon = "work", Group = "Organización",
            Description = "Cargos importados desde Buk y sus áreas.",
            Display = cargo => TitleCase.To(cargo.Name),
            TitleLabel = "Cargo",
            CanDelete = false,
            GroupField = nameof(Cargo.RoleFamilyName),
            GroupEmptyLabel = "Sin familia",
            GroupOrder = ["1era línea - Gerencia", "2da línea - Jefes", "3era línea - Supervisión", "Ejecutivos"]
        }
            .Field(nameof(Cargo.Name), "Nombre", grid: false)
            .Field(nameof(Cargo.Code), "Código", grid: false)
            .Field(nameof(Cargo.RoleFamilyName), "Familia de rol", grid: false)
            .Field(nameof(Cargo.Description), "Descripción", grid: false)
            .Field(nameof(Cargo.Requirements), "Requisitos", grid: false)
            .Many(nameof(Cargo.Areas), "Áreas"),

        // Catálogo legacy no publicado: la instalación actual no tiene la tabla Positions.
        new CatalogDefinition<Position>
        {
            Slug = "posiciones", Label = "Posiciones", Singular = "posición", Feminine = true, Icon = "work", Group = "Organización",
            Description = "Catálogo legacy no publicado en el portal actual.",
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
            SoftDeleteField = nameof(Portal.DeletedAt),
            RowLinkText = "Gestionar secciones",
            RowLinkIcon = "dashboard_customize",
            RowLinkHref = "/admin/secciones?portal_id={key}"
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
            TitleLabel = "Sección"
        }
            .Field(nameof(PortalSection.Title), "Título", grid: false)
            .Field(nameof(PortalSection.PortalId), "Portal", grid: false)
            .Field(nameof(PortalSection.SectionGroup), "Grupo menú")
            .Field(nameof(PortalSection.Route), "Ruta (ej. /portal-x/mi-pagina)")
            .Field(nameof(PortalSection.Status), "Disponibilidad", options: PortalStatus.SectionOptions)
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
