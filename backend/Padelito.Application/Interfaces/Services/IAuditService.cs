using Padelito.Application.DTOs.Audit;

namespace Padelito.Application.Interfaces.Services;

public interface IAuditService
{
    Task<IReadOnlyList<ReservationAuditListDto>> GetReservationAuditsAsync(
        int clubId, ReservationAuditFilterDto filter, CancellationToken cancellationToken);
}
