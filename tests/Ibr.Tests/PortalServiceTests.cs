using Ibr.Application.Portals;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Maestros;
using Ibr.Infrastructure.Persistence;
using Ibr.Infrastructure.Portals;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ibr.Tests;

public sealed class PortalServiceTests : IAsyncLifetime
{
    private const int AdminId = 1;
    private const int CollaboratorId = 2;
    private const int GuestId = 3;

    private SqliteConnection _connection = null!;
    private DbContextOptions<IbrDbContext> _options = null!;
    private PortalService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        _connection.CreateFunction("getdate", () => DateTime.Now);
        _options = new DbContextOptionsBuilder<IbrDbContext>().UseSqlite(_connection).Options;

        await using var db = new IbrDbContext(_options);
        await db.Database.EnsureCreatedAsync();

        var admin = new Role { RoleId = 1, RoleName = "System_Admin" };
        var collaborator = new Role { RoleId = 3, RoleName = "Collaborator" };
        db.Positions.Add(new Position { PositionId = 1, PositionName = "Developer" });
        db.Users.AddRange(
            new User { UserId = AdminId, FirstName = "Ada", LastName = "Admin", Email = "ada@example.test", PositionId = 1, IsActive = true, Roles = [admin] },
            new User { UserId = CollaboratorId, FirstName = "Carl", LastName = "Collab", Email = "carl@example.test", PositionId = 1, IsActive = true, Roles = [collaborator] },
            new User { UserId = GuestId, FirstName = "Gus", LastName = "Guest", Email = "gus@example.test", PositionId = 1, IsActive = true });
        await db.SaveChangesAsync();

        var guest = await db.Users.FindAsync(GuestId);
        db.Portals.AddRange(
            new Portal
            {
                PortalId = 2, Name = "Gestión Maestros", ColorHex = "#0A1DFF", Icon = "<svg></svg>", Status = "DISPONIBLE", RoleId = 1,
                Sections =
                [
                    new PortalSection { Title = "Usuarios", Route = "/admin/usuarios", Status = "Activo", SectionGroup = "Seguridad", DisplayOrder = 4 },
                    new PortalSection { Title = "Posiciones", Route = "/admin/posiciones", Status = "ACTIVO", SectionGroup = "Organización", DisplayOrder = 1 },
                    new PortalSection { Title = "Oculta", Route = "/admin/oculta", Status = "Activo", DisplayOrder = 2, IsActive = false }
                ]
            },
            new Portal
            {
                PortalId = 4, Name = "Control de Vacaciones", ColorHex = "#F690B8", Status = "Disponible",
                Sections =
                [
                    new PortalSection { Title = "Nueva solicitud", Route = "/vacaciones/nueva", Status = "Próximamente", SectionGroup = "Mis vacaciones" },
                    new PortalSection { Title = "Aprobaciones", Route = "/vacaciones/aprobaciones/", Status = "Activo", SectionGroup = "Equipo", DisplayOrder = 2 }
                ]
            },
            new Portal { PortalId = 5, Name = "Compras", ColorHex = "#000000", Status = "En construcción", Users = [guest!] },
            new Portal { PortalId = 6, Name = "Inactivo", ColorHex = "#000000", Status = "Disponible", IsActive = false },
            new Portal { PortalId = 7, Name = "Borrado", ColorHex = "#000000", Status = "Disponible", DeletedAt = DateTime.Now });
        await db.SaveChangesAsync();

        _service = new PortalService(new TestDbContextFactory(_options));
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task Inactive_and_soft_deleted_portals_are_excluded()
    {
        var portals = await _service.GetPortalsAsync(AdminId);

        Assert.Equal(["Compras", "Control de Vacaciones", "Gestión Maestros"], portals.Select(portal => portal.Name));
    }

    [Theory]
    [InlineData(AdminId, new[] { "Gestión Maestros" })]
    [InlineData(CollaboratorId, new string[0])]
    [InlineData(GuestId, new[] { "Compras" })]
    [InlineData(0, new string[0])]
    public async Task Access_requires_role_or_explicit_user_grant(int userId, string[] expected)
    {
        var portals = await _service.GetPortalsAsync(userId);

        Assert.Equal(expected, portals.Where(portal => portal.HasAccess).Select(portal => portal.Name));
    }

    [Fact]
    public async Task Sections_are_visible_ordered_and_status_is_normalized()
    {
        var portals = await _service.GetPortalsAsync(AdminId);
        var maestros = portals.Single(portal => portal.Id == 2);
        var compras = portals.Single(portal => portal.Id == 5);

        Assert.Equal(["Posiciones", "Usuarios"], maestros.Sections.Select(section => section.Title));
        Assert.All(maestros.Sections, section => Assert.True(section.Available));
        Assert.Equal("Disponible", maestros.Status);
        Assert.True(maestros.Available);
        Assert.True(maestros.Restricted);
        Assert.False(compras.Available);
        Assert.True(compras.Restricted);
        Assert.True(portals.Single(portal => portal.Id == 4).Restricted);
    }

    [Fact]
    public async Task Routes_resolve_to_their_portal()
    {
        var portals = await _service.GetPortalsAsync(AdminId);
        var vacaciones = portals.Single(portal => portal.Id == 4);

        Assert.True(vacaciones.Owns("/portal/4"));
        Assert.True(vacaciones.Owns("/Vacaciones/Aprobaciones/15?tab=2"));
        Assert.True(vacaciones.Owns("vacaciones/nueva"));
        Assert.False(vacaciones.Owns("/vacaciones/nuevas"));
        Assert.False(vacaciones.Owns("/portal/2"));
        Assert.False(portals.Single(portal => portal.Id == 2).Owns("/admin/oculta"));
    }

    [Fact]
    public async Task Portals_are_managed_from_maestros_catalogs()
    {
        await using var db = new IbrDbContext(_options);
        var maestros = new MaestrosService(db, new ServiceCollection()
            .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
            .BuildServiceProvider());

        var created = await maestros.SaveAsync("portales", new Dictionary<string, object?>
        {
            [nameof(Portal.Name)] = "Recursos Humanos",
            [nameof(Portal.ColorHex)] = "#123456",
            [nameof(Portal.Status)] = "Disponible",
            [nameof(Portal.RoleId)] = 3,
            [nameof(Portal.Users)] = new List<object> { GuestId },
            [nameof(Portal.IsActive)] = true
        });
        var invalidStatus = await maestros.SaveAsync("secciones", new Dictionary<string, object?>
        {
            [nameof(PortalSection.Title)] = "X",
            [nameof(PortalSection.PortalId)] = 4,
            [nameof(PortalSection.Route)] = "/x",
            [nameof(PortalSection.Status)] = "Cerrado",
            [nameof(PortalSection.DisplayOrder)] = 1
        });

        Assert.True(created.Succeeded, created.Error);
        Assert.False(invalidStatus.Succeeded);
        Assert.Contains("Estado", invalidStatus.Error);

        var record = (await maestros.GetRecordsAsync("portales")).Single(row => (string?)row["__title"] == "Recursos Humanos");
        Assert.Equal("Collaborator", record["RoleId__text"]);
        Assert.Equal("Gus Guest", record["Users__text"]);

        Assert.Contains(await _service.GetPortalsAsync(CollaboratorId), portal => portal.Name == "Recursos Humanos" && portal.HasAccess);
        Assert.Contains(await _service.GetPortalsAsync(GuestId), portal => portal.Name == "Recursos Humanos" && portal.HasAccess);
        Assert.DoesNotContain(await _service.GetPortalsAsync(AdminId), portal => portal.Name == "Recursos Humanos" && portal.HasAccess);
    }

    private sealed class TestDbContextFactory(DbContextOptions<IbrDbContext> options) : IDbContextFactory<IbrDbContext>
    {
        public IbrDbContext CreateDbContext() => new(options);
    }
}
