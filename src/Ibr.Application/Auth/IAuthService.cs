namespace Ibr.Application.Auth;

public interface IAuthService
{
    Task<AuthenticatedUserDto?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
