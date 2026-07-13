using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<List<Reservation>> GetReservationsAsync(
        int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? statusId,
        CancellationToken cancellationToken);
}
