namespace Padelito.Domain.Entities;

public sealed class CourtType
{
    public int Id { get; set; }
    public required string Description { get; set; }

    public ICollection<Court> Courts { get; set; } = new List<Court>();
}
