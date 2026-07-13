using Microsoft.EntityFrameworkCore;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class AuditRepository(PadelitoDbContext dbContext) : IAuditRepository
{
    public Task<List<ReservationAudit>> GetReservationAuditsAsync(int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? reservationId, string? action, string? username, CancellationToken cancellationToken)
    {
        var query = dbContext.ReservationAudits
            .Include(x => x.Reservation).ThenInclude(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.Reservation).ThenInclude(x => x.AvailableTurn).ThenInclude(x => x.Court)
            .Where(x => x.Reservation.AvailableTurn.Court.ClubId == clubId);
        if (dateFrom.HasValue) query = query.Where(x => x.CreatedAt >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (dateTo.HasValue) query = query.Where(x => x.CreatedAt < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (reservationId.HasValue) query = query.Where(x => x.ReservationId == reservationId.Value);
        if (action is not null) query = query.Where(x => x.Action == action);
        if (username is not null) query = query.Where(x => x.Username.Contains(username));
        return query.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
    }
}
