namespace Ibr.Application.Users;

public sealed record UserDto(
    int Id,
    string FullName,
    string Email,
    string PositionName);
