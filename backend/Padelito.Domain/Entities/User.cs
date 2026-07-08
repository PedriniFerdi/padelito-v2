namespace Padelito.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public int EmployeeId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
