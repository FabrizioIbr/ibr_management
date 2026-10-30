namespace Ibr.Domain.Entities;

public class Cargo
{
    public int CargoId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public int? RoleFamilyId { get; set; }
    public string? RoleFamilyName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Area> Areas { get; set; } = new List<Area>();
}
