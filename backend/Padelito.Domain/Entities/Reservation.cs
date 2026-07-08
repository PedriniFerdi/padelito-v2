namespace Padelito.Domain.Entities;

public sealed class Reservation
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int AvailableTurnId { get; set; }
    public int EmployeeId { get; set; }
    public int? PromotionId { get; set; }
    public DateOnly ReservationDate { get; set; }
    public int ReservationStatusId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    public Client Client { get; set; } = null!;
    public AvailableTurn AvailableTurn { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Promotion? Promotion { get; set; }
    public ReservationStatus ReservationStatus { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ReservationAudit> Audits { get; set; } = new List<ReservationAudit>();
}
