using Microsoft.EntityFrameworkCore;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class DashboardRepository(PadelitoDbContext dbContext) : IDashboardRepository
{
    public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken) =>
        dbContext.Clients.CountAsync(x => x.Person.IsActive, cancellationToken);

    public Task<int> CountActiveCourtsAsync(int clubId, CancellationToken cancellationToken) =>
        dbContext.Courts.CountAsync(x => x.ClubId == clubId && x.IsActive, cancellationToken);

    public Task<int> CountReservationsAsync(int clubId, DateOnly date, CancellationToken cancellationToken) =>
        dbContext.Reservations.CountAsync(x => x.AvailableTurn.Court.ClubId == clubId && x.ReservationDate == date && x.ReservationStatusId != ReservationStatusIds.Cancelled, cancellationToken);

    public async Task<decimal> SumIncomeAsync(int clubId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        await dbContext.Payments.Where(x => x.Reservation.AvailableTurn.Court.ClubId == clubId && x.PaymentDate >= fromUtc && x.PaymentDate < toUtc)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

    public Task<List<Reservation>> GetLatestReservationsAsync(int clubId, DateOnly date, int take, CancellationToken cancellationToken) =>
        dbContext.Reservations.Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court).Include(x => x.ReservationStatus)
            .Where(x => x.AvailableTurn.Court.ClubId == clubId && x.ReservationDate == date)
            .AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(take).ToListAsync(cancellationToken);
}
