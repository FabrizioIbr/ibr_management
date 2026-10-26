using Ibr.Application.Users;
using Ibr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ibr.Infrastructure.Users;

public sealed class UserService(IbrDbContext dbContext) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null && user.IsActive == true)
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(user => new UserDto(
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PositionId,
                user.IsActive ?? false))
            .ToListAsync(cancellationToken);
    }
}
