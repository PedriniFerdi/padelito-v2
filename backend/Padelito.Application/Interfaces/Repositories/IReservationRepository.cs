using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<List<Reservation>> GetReservationsAsync(
        int clubId,
        IReadOnlyCollection<int> statusIds,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int? statusId,
        CancellationToken cancellationToken);

    Task<Reservation?> GetReservationAsync(int id, int clubId, bool trackChanges, CancellationToken cancellationToken);
    Task<List<AvailableTurn>> GetAvailabilityAsync(int clubId, DateOnly date, CancellationToken cancellationToken);
    Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken);
    Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken);
    Task<AvailableTurn?> GetAvailableTurnAsync(int id, CancellationToken cancellationToken);
    Task<Promotion?> GetPromotionAsync(int id, CancellationToken cancellationToken);
    Task<ReservationStatus?> GetStatusAsync(int id, CancellationToken cancellationToken);
    Task<bool> IsOccupiedAsync(DateOnly date, int availableTurnId, CancellationToken cancellationToken);
    Task<bool> HasPaymentsAsync(int reservationId, CancellationToken cancellationToken);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
