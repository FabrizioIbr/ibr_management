namespace Ibr.Domain.Entities;

public class Area
{
    public int AreaId { get; set; }
    public string Name { get; set; } = null!;
    public string? Status { get; set; }
    public int? ParentAreaId { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public string? CostCenter { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Area? ParentArea { get; set; }
    public ICollection<Area> Children { get; set; } = new List<Area>();
    public ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();
}
