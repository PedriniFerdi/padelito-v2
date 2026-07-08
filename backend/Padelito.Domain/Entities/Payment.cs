namespace Padelito.Domain.Entities;

public sealed class Payment
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Note { get; set; }

    public Reservation Reservation { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
}
