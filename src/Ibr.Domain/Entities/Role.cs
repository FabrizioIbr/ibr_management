namespace Ibr.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
