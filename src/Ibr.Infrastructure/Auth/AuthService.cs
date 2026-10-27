using Ibr.Application.Auth;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Auth;

public sealed class AuthService(
    IbrDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<AuthenticatedUserDto?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(candidate => candidate.Roles)
            .Where(candidate => candidate.DeletedAt == null
                && candidate.IsActive == true
                && candidate.Email == email)
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            dbContext.Users.Attach(user);
            dbContext.Entry(user).Property(candidate => candidate.PasswordHash).IsModified = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AuthenticatedUserDto(
            user.UserId,
            user.Email,
            $"{user.FirstName} {user.LastName}")
        {
            Roles = user.Roles.Select(role => role.RoleName).ToArray()
        };
    }
}
