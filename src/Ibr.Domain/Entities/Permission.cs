namespace Ibr.Domain.Entities;

public class Permission
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
