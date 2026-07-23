using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<int> CountActiveClientsAsync(CancellationToken cancellationToken);
    Task<int> CountActiveCourtsAsync(int clubId, CancellationToken cancellationToken);
    Task<int> CountReservationsAsync(int clubId, DateOnly date, CancellationToken cancellationToken);
    Task<decimal> SumIncomeAsync(int clubId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
    Task<List<Reservation>> GetLatestReservationsAsync(int clubId, DateOnly date, int take, CancellationToken cancellationToken);
    Task<DashboardRevenueIntelligenceData> GetRevenueIntelligenceAsync(int clubId, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken);
}

public sealed record DashboardRevenueIntelligenceData(
    IReadOnlyList<Reservation> Reservations,
    IReadOnlyList<AvailableTurn> ActiveTurns);
