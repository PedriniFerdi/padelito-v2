using Padelito.Application.DTOs.Reservations;

namespace Padelito.Application.Interfaces.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationListDto>> GetReservationsAsync(int clubId, ReservationFilterDto filter, CancellationToken cancellationToken);
    Task<ReservationDetailDto> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReservationAvailabilityDto>> GetAvailabilityAsync(int clubId, DateOnly date, CancellationToken cancellationToken);
    Task<ReservationDetailDto> CreateAsync(int clubId, int employeeId, string username, ReservationCreateDto request, CancellationToken cancellationToken);
    Task<ReservationDetailDto> ChangeStatusAsync(int id, int clubId, string username, ReservationChangeStatusDto request, CancellationToken cancellationToken);
}
