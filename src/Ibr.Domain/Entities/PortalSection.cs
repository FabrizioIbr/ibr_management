namespace Ibr.Domain.Entities;

public class PortalSection
{
    public int SectionId { get; set; }
    public int PortalId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string Route { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? SectionGroup { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public Portal Portal { get; set; } = null!;
}
