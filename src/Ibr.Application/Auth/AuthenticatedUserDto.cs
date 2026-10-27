namespace Ibr.Application.Auth;

public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string FullName)
{
    public IReadOnlyList<string> Roles { get; init; } = [];
}
