namespace Padelito.Domain.Entities;

public sealed class Client
{
    public int Id { get; set; }
    public int PersonId { get; set; }

    public Person Person { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
