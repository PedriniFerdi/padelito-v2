using Padelito.Application.Common;
using Padelito.Application.DTOs.Audit;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Application.Services;

public sealed class AuditService(IAuditRepository repository) : IAuditService
{
    public async Task<IReadOnlyList<ReservationAuditListDto>> GetReservationAuditsAsync(int clubId, ReservationAuditFilterDto filter, CancellationToken cancellationToken)
    {
        if (filter.DateFrom.HasValue && filter.DateTo.HasValue && filter.DateTo < filter.DateFrom)
            throw new BusinessException("La fecha hasta debe ser mayor o igual a la fecha desde.");

        var audits = await repository.GetReservationAuditsAsync(
            clubId, filter.DateFrom, filter.DateTo, filter.ReservationId,
            Normalize(filter.Action), Normalize(filter.Username), cancellationToken);
        return audits.Select(x => new ReservationAuditListDto(
            x.Id, x.ReservationId, x.Reservation.ReservationDate,
            $"{x.Reservation.Client.Person.FirstName} {x.Reservation.Client.Person.LastName}",
            x.Reservation.AvailableTurn.Court.Name, x.Action, x.Description, x.Username, x.CreatedAt)).ToList();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
