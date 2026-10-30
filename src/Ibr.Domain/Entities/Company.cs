namespace Ibr.Domain.Entities;

public class Company
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? Ruc { get; set; }
    public string? BusinessActivity { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
