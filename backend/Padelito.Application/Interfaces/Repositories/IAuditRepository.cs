using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task<List<ReservationAudit>> GetReservationAuditsAsync(
        int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? reservationId,
        string? action, string? username, CancellationToken cancellationToken);
}
