namespace Padelito.Domain.Entities;

public static class ReservationStatusIds
{
    public const int Pending = 1;
    public const int Confirmed = 2;
    public const int Cancelled = 3;
    public const int Completed = 4;

    public static readonly int[] Active = [Pending, Confirmed];
    public static readonly int[] History = [Cancelled, Completed];
}
