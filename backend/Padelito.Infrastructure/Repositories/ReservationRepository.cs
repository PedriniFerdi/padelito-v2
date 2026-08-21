using System.Data;
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

    public Task<List<Reservation>> GetOperationsBoardReservationsAsync(int clubId, DateOnly date, CancellationToken cancellationToken)
    {
        return DetailsQuery(false)
            .Where(x => x.AvailableTurn.Court.ClubId == clubId && x.ReservationDate == date)
            .OrderBy(x => x.AvailableTurn.Court.Name)
            .ThenBy(x => x.AvailableTurn.StartTime)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
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

    public Task<bool> HasPaymentsAsync(int reservationId, CancellationToken cancellationToken) =>
        dbContext.Payments.AnyAsync(x => x.ReservationId == reservationId, cancellationToken);

    public async Task<Reservation> ChangeStatusAsync(
        int id,
        int clubId,
        int newStatusId,
        string username,
        DateTime localNow,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var reservation = await DetailsQuery(true)
            .FirstOrDefaultAsync(x => x.Id == id && x.AvailableTurn.Court.ClubId == clubId, cancellationToken)
            ?? throw new BusinessException("The reservation does not exist.");

        if (newStatusId == ReservationStatusIds.Completed)
            throw new BusinessException("Reservations are completed automatically after the time slot ends.");
        if (reservation.ReservationDate.ToDateTime(reservation.AvailableTurn.StartTime) <= localNow)
            throw new BusinessException("A reservation cannot be changed after the time slot has started.");
        var valid = reservation.ReservationStatusId switch
        {
            ReservationStatusIds.Pending => newStatusId is ReservationStatusIds.Confirmed or ReservationStatusIds.Cancelled,
            ReservationStatusIds.Confirmed => newStatusId == ReservationStatusIds.Cancelled,
            _ => false
        };
        if (!valid)
            throw new BusinessException("The requested status change is not allowed.");
        if (newStatusId == ReservationStatusIds.Cancelled && reservation.Payments.Count != 0)
            throw new BusinessException("Reservations with recorded payments cannot be canceled.");

        var newStatus = await dbContext.ReservationStatuses.FirstOrDefaultAsync(
            x => x.Id == newStatusId, cancellationToken)
            ?? throw new BusinessException("The selected status does not exist.");
        var previousStatus = reservation.ReservationStatus.Name;
        reservation.ReservationStatusId = newStatus.Id;
        reservation.ReservationStatus = newStatus;
        reservation.Audits.Add(new ReservationAudit
        {
            ReservationId = reservation.Id,
            Action = "StatusChanged",
            Description = $"Status changed from {previousStatus} to {newStatus.Name}.",
            Username = username,
            CreatedAt = utcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return reservation;
    }

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
            throw new ConflictException("The selected time slot is already reserved for that date.");
        }
    }

    private IQueryable<Reservation> DetailsQuery(bool trackChanges)
    {
        var query = dbContext.Reservations
            .Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court).ThenInclude(x => x.CourtType)
            .Include(x => x.Employee).ThenInclude(x => x.Person)
            .Include(x => x.Promotion).Include(x => x.ReservationStatus).Include(x => x.Payments).AsQueryable();
        return trackChanges ? query : query.AsNoTracking();
    }
}
