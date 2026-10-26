namespace Ibr.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetActiveAsync(CancellationToken cancellationToken = default);
}
