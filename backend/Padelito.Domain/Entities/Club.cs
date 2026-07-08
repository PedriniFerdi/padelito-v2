namespace Padelito.Domain.Entities;

public sealed class Club
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
