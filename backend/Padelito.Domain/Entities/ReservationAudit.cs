namespace Padelito.Domain.Entities;

public sealed class ReservationAudit
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public required string Action { get; set; }
    public required string Description { get; set; }
    public required string Username { get; set; }
    public DateTime CreatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
