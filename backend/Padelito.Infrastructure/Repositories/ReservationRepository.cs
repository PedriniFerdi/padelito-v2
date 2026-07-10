using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Padelito.Application.Common;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class ReservationRepository(PadelitoDbContext dbContext) : IReservationRepository
{
    public Task<List<Reservation>> GetReservationsAsync(int clubId, IReadOnlyCollection<int> statusIds, DateOnly? dateFrom, DateOnly? dateTo, int? statusId, CancellationToken cancellationToken)
    {
        var query = DetailsQuery(false).Where(x => x.AvailableTurn.Court.ClubId == clubId && statusIds.Contains(x.ReservationStatusId));
        if (dateFrom.HasValue) query = query.Where(x => x.ReservationDate >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(x => x.ReservationDate <= dateTo.Value);
        if (statusId.HasValue) query = query.Where(x => x.ReservationStatusId == statusId.Value);
        return query.OrderBy(x => x.ReservationDate).ThenBy(x => x.AvailableTurn.StartTime).ThenBy(x => x.AvailableTurn.Court.Name).ToListAsync(cancellationToken);
    }

    public Task<Reservation?> GetReservationAsync(int id, int clubId, bool trackChanges, CancellationToken cancellationToken)
    {
        return DetailsQuery(trackChanges).FirstOrDefaultAsync(x => x.Id == id && x.AvailableTurn.Court.ClubId == clubId, cancellationToken);
    }

    public Task<List<AvailableTurn>> GetAvailabilityAsync(int clubId, DateOnly date, CancellationToken cancellationToken)
    {
        return dbContext.AvailableTurns
            .Include(x => x.Court).ThenInclude(x => x.CourtType)
            .Where(x => x.IsActive && x.Court.IsActive && x.Court.ClubId == clubId
                && !x.Reservations.Any(r => r.ReservationDate == date && r.ReservationStatusId != ReservationStatusIds.Cancelled))
            .AsNoTracking().OrderBy(x => x.StartTime).ThenBy(x => x.Court.Name).ToListAsync(cancellationToken);
    }

    public Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Clients.Include(x => x.Person).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Employees.Include(x => x.Person).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<AvailableTurn?> GetAvailableTurnAsync(int id, CancellationToken cancellationToken) =>
        dbContext.AvailableTurns.Include(x => x.Court).ThenInclude(x => x.CourtType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Promotion?> GetPromotionAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Promotions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ReservationStatus?> GetStatusAsync(int id, CancellationToken cancellationToken) =>
        dbContext.ReservationStatuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> IsOccupiedAsync(DateOnly date, int availableTurnId, CancellationToken cancellationToken) =>
        dbContext.Reservations.AnyAsync(x => x.ReservationDate == date && x.AvailableTurnId == availableTurnId
            && x.ReservationStatusId != ReservationStatusIds.Cancelled, cancellationToken);

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken) =>
        await dbContext.Reservations.AddAsync(reservation, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("El turno ya está reservado para la fecha seleccionada.");
        }
    }

    private IQueryable<Reservation> DetailsQuery(bool trackChanges)
    {
        var query = dbContext.Reservations
            .Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court).ThenInclude(x => x.CourtType)
            .Include(x => x.Employee).ThenInclude(x => x.Person)
            .Include(x => x.Promotion).Include(x => x.ReservationStatus).AsQueryable();
        return trackChanges ? query : query.AsNoTracking();
    }
}
