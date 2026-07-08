namespace Padelito.Domain.Entities;

public sealed class Employee
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int ClubId { get; set; }

    public Person Person { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
