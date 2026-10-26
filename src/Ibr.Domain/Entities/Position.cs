namespace Ibr.Domain.Entities;

public class Position
{
    public int PositionId { get; set; }
    public string PositionName { get; set; } = null!;
    public string? Department { get; set; }
    public int? ReportsToPositionId { get; set; }
    public ICollection<Position> Subordinates { get; set; } = new List<Position>();
    public Position? ReportsToPosition { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
