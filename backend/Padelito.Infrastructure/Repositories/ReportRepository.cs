using Microsoft.EntityFrameworkCore;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class ReportRepository(PadelitoDbContext dbContext) : IReportRepository
{
    public Task<List<Reservation>> GetReservationsAsync(int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? statusId, CancellationToken cancellationToken)
    {
        var query = dbContext.Reservations
            .Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court)
            .Include(x => x.ReservationStatus).Include(x => x.Promotion).Include(x => x.Payments)
            .Where(x => x.AvailableTurn.Court.ClubId == clubId);
        if (dateFrom.HasValue) query = query.Where(x => x.ReservationDate >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(x => x.ReservationDate <= dateTo.Value);
        if (statusId.HasValue) query = query.Where(x => x.ReservationStatusId == statusId.Value);
        return query.AsNoTracking().OrderByDescending(x => x.ReservationDate).ThenBy(x => x.AvailableTurn.StartTime).ToListAsync(cancellationToken);
    }
}
