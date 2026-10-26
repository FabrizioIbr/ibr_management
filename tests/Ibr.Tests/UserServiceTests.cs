using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Ibr.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Tests;

public sealed class UserServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IbrDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IbrDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new IbrDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _dbContext.Positions.Add(new Position
        {
            PositionId = 1,
            PositionName = "Developer"
        });
        _dbContext.Users.Add(new User
        {
            UserId = 1,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            PositionId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_returns_active_user_as_dto()
    {
        var service = new UserService(_dbContext);

        var result = await service.GetActiveAsync();

        var user = Assert.Single(result);
        Assert.Equal("Ada", user.FirstName);
        Assert.Equal("Lovelace", user.LastName);
        Assert.Equal("ada@example.test", user.Email);
        Assert.True(user.IsActive);
    }
}
