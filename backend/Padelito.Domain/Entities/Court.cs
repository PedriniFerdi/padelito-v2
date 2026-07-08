namespace Padelito.Domain.Entities;

public sealed class Court
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int CourtTypeId { get; set; }
    public required string Name { get; set; }
    public decimal HourPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
    public CourtType CourtType { get; set; } = null!;
    public ICollection<AvailableTurn> AvailableTurns { get; set; } = new List<AvailableTurn>();
}
