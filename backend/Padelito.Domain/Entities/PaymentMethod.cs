namespace Padelito.Domain.Entities;

public sealed class PaymentMethod
{
    public int Id { get; set; }
    public required string Description { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
