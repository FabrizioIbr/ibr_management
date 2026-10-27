namespace Ibr.Domain.Entities;

public class Portal
{
    public int PortalId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ColorHex { get; set; } = null!;
    public string? Icon { get; set; }
    public string Status { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
    public ICollection<PortalSection> Sections { get; set; } = new List<PortalSection>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
