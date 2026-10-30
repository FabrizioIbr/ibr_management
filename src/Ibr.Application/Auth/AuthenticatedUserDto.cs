namespace Ibr.Application.Auth;

public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string FullName)
{
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>Empleado vinculado en la tabla Usuarios (base del filtrado jerárquico).</summary>
    public int? EmployeeId { get; init; }
}
