namespace Padelito.Domain.Entities;

public sealed class Role
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
