using Ibr.Application.Maestros;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Maestros;
using Ibr.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ibr.Tests;

public sealed class MaestrosServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IbrDbContext _dbContext = null!;
    private MaestrosService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        // Users.CreatedAt tiene default SQL Server getdate(); se emula en SQLite.
        _connection.CreateFunction("getdate", () => DateTime.Now);

        var options = new DbContextOptionsBuilder<IbrDbContext>().UseSqlite(_connection).Options;
        _dbContext = new IbrDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var admin = new Role { RoleId = 1, RoleName = "System_Admin" };
        var reader = new Role { RoleId = 2, RoleName = "Reader" };
        _dbContext.Roles.AddRange(admin, reader);
        _dbContext.Permissions.Add(new Permission { PermissionId = 1, PermissionName = "users.read" });
        _dbContext.Positions.Add(new Position { PositionId = 1, PositionName = "Developer", Department = "TI" });
        _dbContext.Users.AddRange(
            new User { UserId = 1, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.test", PositionId = 1, IsActive = true, Roles = [admin] },
            new User { UserId = 2, FirstName = "Old", LastName = "User", Email = "old@example.test", PositionId = 1, IsActive = false, DeletedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var services = new ServiceCollection()
            .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
            .BuildServiceProvider();
        _service = new MaestrosService(_dbContext, services);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public void Schema_is_inferred_from_ef_metadata()
    {
        var users = _service.FindCatalog("usuarios")!;

        Assert.Equal(nameof(User.UserId), users.KeyField);
        var email = users.Fields.Single(field => field.Name == nameof(User.Email));
        Assert.Equal(FieldKind.Email, email.Kind);
        Assert.True(email.Required);
        Assert.Equal(150, email.MaxLength);
        Assert.Equal(FieldKind.Lookup, users.Fields.Single(field => field.Name == nameof(User.PositionId)).Kind);
        Assert.Equal(FieldKind.MultiLookup, users.Fields.Single(field => field.Name == nameof(User.Roles)).Kind);
        Assert.Equal(FieldKind.Boolean, users.Fields.Single(field => field.Name == nameof(User.IsActive)).Kind);
    }

    [Fact]
    public void Every_registered_catalog_builds_its_schema()
    {
        var catalogs = _service.GetCatalogs();

        Assert.NotEmpty(catalogs);
        Assert.Equal(catalogs.Count, catalogs.Select(catalog => catalog.Slug).Distinct().Count());
    }

    [Fact]
    public async Task GetRecords_skips_soft_deleted_and_resolves_relation_texts()
    {
        var records = await _service.GetRecordsAsync("usuarios");

        var ada = Assert.Single(records);
        Assert.Equal("Ada Lovelace", ada[CatalogSchema.TitleKey]);
        Assert.Equal("Developer", ada[FieldSchema.TextKey(nameof(User.PositionId))]);
        Assert.Equal("System_Admin", ada[FieldSchema.TextKey(nameof(User.Roles))]);
    }

    [Fact]
    public async Task Save_creates_user_with_hashed_password_and_roles()
    {
        var result = await _service.SaveAsync("usuarios", new Dictionary<string, object?>
        {
            [nameof(User.FirstName)] = "Grace",
            [nameof(User.LastName)] = "Hopper",
            [nameof(User.Email)] = "grace@example.test",
            [nameof(User.PositionId)] = 1,
            [nameof(User.IsActive)] = true,
            [nameof(User.Roles)] = new List<object> { 2 },
            ["__password"] = "S3cret!"
        });

        Assert.True(result.Succeeded, result.Error);
        var grace = await _dbContext.Users.Include(user => user.Roles).SingleAsync(user => user.Email == "grace@example.test");
        Assert.False(string.IsNullOrEmpty(grace.PasswordHash));
        Assert.Equal("Reader", Assert.Single(grace.Roles).RoleName);
    }

    [Fact]
    public async Task Save_rejects_missing_required_fields_and_password_on_create()
    {
        var missingEmail = await _service.SaveAsync("usuarios", new Dictionary<string, object?>
        {
            [nameof(User.FirstName)] = "Grace", [nameof(User.LastName)] = "Hopper", [nameof(User.PositionId)] = 1, ["__password"] = "x"
        });
        var missingPassword = await _service.SaveAsync("usuarios", new Dictionary<string, object?>
        {
            [nameof(User.FirstName)] = "Grace", [nameof(User.LastName)] = "Hopper", [nameof(User.Email)] = "g@example.test", [nameof(User.PositionId)] = 1
        });

        Assert.False(missingEmail.Succeeded);
        Assert.Contains("Correo", missingEmail.Error);
        Assert.False(missingPassword.Succeeded);
        Assert.Contains("Contraseña", missingPassword.Error);
    }

    [Fact]
    public async Task Save_updates_existing_record_and_replaces_many_to_many()
    {
        var record = (await _service.GetRecordsAsync("usuarios")).Single();
        record[nameof(User.LastName)] = "King";
        record[nameof(User.Roles)] = new List<object> { 2 };

        var result = await _service.SaveAsync("usuarios", record);

        Assert.True(result.Succeeded, result.Error);
        _dbContext.ChangeTracker.Clear();
        var ada = await _dbContext.Users.Include(user => user.Roles).SingleAsync(user => user.UserId == 1);
        Assert.Equal("King", ada.LastName);
        Assert.Equal("Reader", Assert.Single(ada.Roles).RoleName);
    }

    [Fact]
    public async Task Save_reports_duplicate_unique_values()
    {
        var result = await _service.SaveAsync("roles", new Dictionary<string, object?> { [nameof(Role.RoleName)] = "Reader" });

        Assert.False(result.Succeeded);
        Assert.Contains("Ya existe", result.Error);
    }

    [Fact]
    public async Task Delete_soft_deletes_users_and_blocks_positions_in_use()
    {
        var deleteUser = await _service.DeleteAsync("usuarios", 1);
        var deletePosition = await _service.DeleteAsync("posiciones", 1);

        Assert.True(deleteUser.Succeeded, deleteUser.Error);
        Assert.Empty(await _service.GetRecordsAsync("usuarios"));
        Assert.False(deletePosition.Succeeded);
    }
}
