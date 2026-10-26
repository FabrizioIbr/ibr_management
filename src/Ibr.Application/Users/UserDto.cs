namespace Ibr.Application.Users;

public sealed record UserDto(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    int PositionId,
    bool IsActive);
