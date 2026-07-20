namespace Padelito.Domain.Entities;

public sealed class Person
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Dni { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Client? Client { get; set; }
    public Employee? Employee { get; set; }
}
