using Ibr.Domain.Entities;
using Ibr.Infrastructure.Auth;
using Ibr.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Tests;

public sealed class AuthServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IbrDbContext _dbContext = null!;
    private IPasswordHasher<User> _passwordHasher = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IbrDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new IbrDbContext(options);
        _passwordHasher = new PasswordHasher<User>();
        await _dbContext.Database.EnsureCreatedAsync();

        _dbContext.Positions.Add(new Position
        {
            PositionId = 1,
            PositionName = "Developer"
        });

        var validUser = CreateUser(1, "erick.escalante@ibr.com.pe", "Erick", "Escalante");
        validUser.PasswordHash = _passwordHasher.HashPassword(validUser, "erickibr");

        var inactiveUser = CreateUser(2, "inactive@example.test", "Inactive", "User");
        inactiveUser.IsActive = false;
        inactiveUser.PasswordHash = _passwordHasher.HashPassword(inactiveUser, "erickibr");

        var deletedUser = CreateUser(3, "deleted@example.test", "Deleted", "User");
        deletedUser.DeletedAt = DateTime.UtcNow;
        deletedUser.PasswordHash = _passwordHasher.HashPassword(deletedUser, "erickibr");

        _dbContext.Users.AddRange(
            validUser,
            inactiveUser,
            deletedUser,
            CreateUser(4, "nullhash@example.test", "Null", "Hash"));

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Valid_credentials_return_dto()
    {
        var result = await CreateService().ValidateCredentialsAsync(
            "erick.escalante@ibr.com.pe", "erickibr");

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("erick.escalante@ibr.com.pe", result.Email);
        Assert.Equal("Erick Escalante", result.FullName);
    }

    [Fact]
    public async Task Invalid_password_returns_null()
    {
        var result = await CreateService().ValidateCredentialsAsync(
            "erick.escalante@ibr.com.pe", "wrong-password");

        Assert.Null(result);
    }

    [Fact]
    public async Task Deleted_user_returns_null()
    {
        var result = await CreateService().ValidateCredentialsAsync(
            "deleted@example.test", "erickibr");

        Assert.Null(result);
    }

    [Fact]
    public async Task Inactive_user_returns_null()
    {
        var result = await CreateService().ValidateCredentialsAsync(
            "inactive@example.test", "erickibr");

        Assert.Null(result);
    }

    [Fact]
    public async Task Null_password_hash_returns_null_without_exception()
    {
        var result = await CreateService().ValidateCredentialsAsync(
            "nullhash@example.test", "erickibr");

        Assert.Null(result);
    }

    [Fact]
    public async Task Unknown_email_returns_null()
    {
        var result = await CreateService().ValidateCredentialsAsync(
            "unknown@example.test", "erickibr");

        Assert.Null(result);
    }

    private AuthService CreateService() => new(_dbContext, _passwordHasher);

    private static User CreateUser(int id, string email, string firstName, string lastName)
    {
        return new User
        {
            UserId = id,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PositionId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
