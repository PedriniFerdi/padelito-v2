namespace Padelito.Domain.Entities;

public sealed class AvailableTurn
{
    public int Id { get; set; }
    public int CourtId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public Court Court { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
