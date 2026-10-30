using Ibr.Application.Auth;
using Ibr.Domain.Entities;
using Ibr.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

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
        var appUser = await dbContext.AppUsers
            .AsNoTracking()
            .Include(candidate => candidate.Employee)
            .Include(candidate => candidate.Roles)
            .Where(candidate => candidate.IsActive == true
                && candidate.EmailCorporativo == email)
            .SingleOrDefaultAsync(cancellationToken);

        if (appUser is not null)
        {
            if (string.IsNullOrEmpty(appUser.PasswordHash)) return null;

            var appVerification = passwordHasher.VerifyHashedPassword(
                new User { UserId = appUser.UserId, Email = appUser.EmailCorporativo ?? appUser.Email },
                appUser.PasswordHash,
                password);

            if (appVerification == PasswordVerificationResult.Failed) return null;

            return new AuthenticatedUserDto(
                appUser.UserId,
                appUser.EmailCorporativo ?? appUser.Email,
                appUser.Employee?.FullName ?? appUser.Email)
            {
                Roles = appUser.Roles.Select(role => role.RoleName).ToArray(),
                EmployeeId = appUser.EmployeeId
            };
        }

        // Compatibilidad con los datos legacy mientras se termina la migración desde Users.
        User? user;
        try
        {
            user = await dbContext.Users
                .AsNoTracking()
                .Include(candidate => candidate.Roles)
                .Where(candidate => candidate.DeletedAt == null
                    && candidate.IsActive == true
                    && candidate.Email == email)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (DbException)
        {
            return null;
        }

        if (user is null || string.IsNullOrEmpty(user.PasswordHash)) return null;

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

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
