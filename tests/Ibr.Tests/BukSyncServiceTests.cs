using Ibr.Infrastructure.Buk;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ibr.Tests;

public sealed class BukSyncServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<IbrDbContext> _options = null!;
    private FakeBukApiClient _client = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        _options = new DbContextOptionsBuilder<IbrDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var db = new IbrDbContext(_options);
        await db.Database.EnsureCreatedAsync();
        db.Companies.Add(new Company { CompanyId = 2, Name = "IBR Perú", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });
        await db.SaveChangesAsync();

        _client = new FakeBukApiClient();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task SyncAsync_upserts_areas_cargos_and_links()
    {
        var service = CreateService();

        var result = await service.SyncAsync();

        Assert.Equal(3, result.Areas);
        Assert.Equal(2, result.Cargos);
        Assert.Equal(3, result.CargoAreas);
        Assert.Equal(2, result.EmployeesRead);
        Assert.Equal(1, result.EmployeesSaved);
        Assert.Equal(1, result.NewUsers);

        await using var db = new IbrDbContext(_options);
        var areas = await db.Areas.OrderBy(area => area.AreaId).ToListAsync();
        Assert.Equal("División", areas[0].Name);
        Assert.Null(areas[0].ParentAreaId);
        Assert.Equal(1, areas[1].ParentAreaId);
        Assert.Equal("Gerencia", areas[1].DepartmentName);
        Assert.Equal(2, areas[2].ParentAreaId);

        var cargo = await db.Cargos.SingleAsync(item => item.CargoId == 10);
        Assert.Equal("Desarrollador", cargo.Name);
        Assert.Equal("Tecnología", cargo.RoleFamilyName);
        Assert.Equal(3, (await db.Employees.SingleAsync()).AreaId);

        var links = await db.Set<Dictionary<string, object>>("CargoArea").ToListAsync();
        Assert.Equal(3, links.Count);
    }

    [Fact]
    public async Task SyncAsync_is_idempotent_and_updates_existing_rows()
    {
        var service = CreateService();
        await service.SyncAsync();

        _client.Areas[1].Name = "Gerencia Nueva";
        _client.Cargos[0].Name = "Desarrollador Senior";
        _client.Employees[0].FullName = "Boris Abad Actualizado";

        var second = await service.SyncAsync();

        Assert.Equal(3, second.Areas);
        Assert.Equal(2, second.Cargos);
        Assert.Equal(1, second.EmployeesSaved);
        Assert.Equal(0, second.NewUsers);
        await using var db = new IbrDbContext(_options);
        Assert.Equal("Gerencia Nueva", (await db.Areas.SingleAsync(area => area.AreaId == 2)).Name);
        Assert.Equal("Desarrollador Senior", (await db.Cargos.SingleAsync(cargo => cargo.CargoId == 10)).Name);
        Assert.Equal("Boris Abad Actualizado", (await db.Employees.SingleAsync()).FullName);
        Assert.Equal(3, await db.Areas.CountAsync());
        Assert.Equal(2, await db.Cargos.CountAsync());
        Assert.Equal(3, await db.Set<Dictionary<string, object>>("CargoArea").CountAsync());
        Assert.Equal(1, await db.AppUsers.CountAsync());
    }

    [Fact]
    public async Task SyncAsync_uses_token_given_for_this_call_when_no_configured_token()
    {
        var service = CreateService(configuredToken: null);

        await service.SyncAsync("token-de-la-ui");

        Assert.Equal("token-de-la-ui", _client.ReceivedToken);
    }

    [Fact]
    public async Task SyncAsync_fails_when_there_is_no_token()
    {
        var service = CreateService(configuredToken: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SyncAsync());

        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private BukSyncService CreateService(string? configuredToken = "test-token") => new(
        new TestDbContextFactory(_options),
        _client,
        Options.Create(new BukOptions
        {
            BaseUrl = "https://ibrlatam.buk.pe/api/v1/peru/",
            Token = configuredToken,
            CompanyId = 2
        }),
        NullLogger<BukSyncService>.Instance);

    private sealed class TestDbContextFactory(DbContextOptions<IbrDbContext> options) : IDbContextFactory<IbrDbContext>
    {
        public IbrDbContext CreateDbContext() => new(options);

        public Task<IbrDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class FakeBukApiClient : IBukApiClient
    {
        public string? ReceivedToken { get; private set; }

        public List<BukArea> Areas { get; } =
        [
            new() { Id = 1, Name = "División", Status = "active" },
            new() { Id = 2, Name = "Gerencia", Status = "active", ParentArea = new BukAreaParent { Id = 1 }, FirstLevelId = 1, FirstLevelName = "División", SecondLevelId = 2, SecondLevelName = "Gerencia" },
            new() { Id = 3, Name = "Equipo", Status = "active", ParentArea = new BukAreaParent { Id = 2 }, FirstLevelId = 1, FirstLevelName = "División", SecondLevelId = 2, SecondLevelName = "Gerencia" }
        ];

        public List<BukCargo> Cargos { get; } =
        [
            new() { Id = 10, Code = "dev", Name = "Desarrollador", RoleFamily = new BukRoleFamily { Id = 5, Name = "Tecnología" }, AreaIds = [2, 3] },
            new() { Id = 11, Code = "pm", Name = "Project Manager", RoleFamily = new BukRoleFamily { Id = 5, Name = "Tecnología" }, AreaIds = [3] }
        ];

        public List<BukEmployee> Employees { get; } =
        [
            new()
            {
                Id = 100,
                PersonId = 200,
                FirstName = "Boris",
                Surname = "Abad",
                FullName = "Boris Abad",
                Email = "boris@example.test",
                ActiveSince = "2021-11-09",
                Status = "active",
                CurrentJob = new BukEmployeeJob
                {
                    CompanyId = 2,
                    AreaId = 2,
                    MainAreaId = 3,
                    Role = new BukEmployeeRole { Id = 10 },
                    Boss = new BukEmployeeBoss { Id = 100 }
                }
            },
            new()
            {
                Id = 101,
                FullName = "Fuera de IBR",
                Email = "otro@example.test",
                CurrentJob = new BukEmployeeJob { CompanyId = 3 }
            }
        ];

        public Task<IReadOnlyList<BukArea>> GetAreasAsync(string token, CancellationToken cancellationToken)
        {
            ReceivedToken = token;
            return Task.FromResult<IReadOnlyList<BukArea>>(Areas);
        }

        public Task<IReadOnlyList<BukCargo>> GetCargosAsync(string token, CancellationToken cancellationToken)
        {
            ReceivedToken = token;
            return Task.FromResult<IReadOnlyList<BukCargo>>(Cargos);
        }

        public Task<IReadOnlyList<BukEmployee>> GetEmployeesAsync(string token, CancellationToken cancellationToken)
        {
            ReceivedToken = token;
            return Task.FromResult<IReadOnlyList<BukEmployee>>(Employees);
        }

        public Task<BukEmployee?> GetEmployeeAsync(int employeeId, string token, CancellationToken cancellationToken)
        {
            ReceivedToken = token;
            return Task.FromResult(Employees.SingleOrDefault(employee => employee.Id == employeeId));
        }

        public Task<IReadOnlyList<BukEarnedVacation>> GetEarnedVacationsAsync(int employeeId, string token, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BukEarnedVacation>>([]);

        public Task<IReadOnlyList<BukVacationRequest>> GetRequestedVacationsAsync(string token, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BukVacationRequest>>([]);
    }
}

