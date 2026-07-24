using Padelito.Application.Common;
using Padelito.Application.DTOs.Dashboard;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class DashboardServiceTests
{
    private static readonly TimeProvider Clock = new FixedTimeProvider(new(2026, 7, 23, 12, 0, 0, TimeSpan.Zero));
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.Utc;

    [Fact]
    public async Task Revenue_intelligence_validates_date_range()
    {
        var service = new DashboardService(new FakeDashboardRepository([], []), Clock, TimeZone);
        await Assert.ThrowsAsync<BusinessException>(() => service.GetRevenueIntelligenceAsync(1, new(new(2026, 7, 20), new(2026, 7, 10)), default));
    }

    [Fact]
    public async Task Revenue_intelligence_calculates_occupancy_cancellations_and_pending_balance()
    {
        var central = Court(1, "Central");
        var side = Court(2, "Lateral");
        var turns = new[]
        {
            Turn(1, central, 10),
            Turn(2, central, 18),
            Turn(3, side, 10),
            Turn(4, side, 18)
        };
        var reservations = new[]
        {
            Reservation(1, turns[0], new(2026, 7, 20), ReservationStatusIds.Confirmed, 100, 75),
            Reservation(2, turns[1], new(2026, 7, 21), ReservationStatusIds.Pending, 200, 0),
            Reservation(3, turns[2], new(2026, 7, 22), ReservationStatusIds.Cancelled, 300, 300)
        };
        var service = new DashboardService(new FakeDashboardRepository(reservations, turns), Clock, TimeZone);

        var dashboard = await service.GetRevenueIntelligenceAsync(1, new(new(2026, 7, 20), new(2026, 7, 22)), default);

        Assert.Equal(75, dashboard.Summary.TotalRevenue);
        Assert.Equal(300, dashboard.Summary.ReservedValue);
        Assert.Equal(225, dashboard.Summary.PendingBalance);
        Assert.Equal(33.33m, dashboard.Summary.CancellationRate);
        Assert.Equal(16.67m, dashboard.Summary.AverageOccupancyRate);
        Assert.Equal(2, dashboard.Courts.Single(x => x.CourtName == "Central").ReservedSlots);
        Assert.Equal(6, dashboard.Courts.Single(x => x.CourtName == "Central").AvailableSlots);
        Assert.Equal(0, dashboard.Courts.Single(x => x.CourtName == "Lateral").Revenue);
    }

    [Fact]
    public async Task Revenue_intelligence_ranks_promotions_by_collected_revenue_with_reserved_value_fallback()
    {
        var court = Court(1, "Central");
        var turn = Turn(1, court, 18);
        var promoA = new Promotion { Id = 1, Name = "Happy Hour", DiscountPercentage = 15, DateFrom = new(2026, 7, 1), DateTo = new(2026, 7, 31) };
        var promoB = new Promotion { Id = 2, Name = "Noche Pro", DiscountPercentage = 10, DateFrom = new(2026, 7, 1), DateTo = new(2026, 7, 31) };
        var reservations = new[]
        {
            Reservation(1, turn, new(2026, 7, 20), ReservationStatusIds.Confirmed, 200, 0, promoA, 250),
            Reservation(2, turn, new(2026, 7, 21), ReservationStatusIds.Confirmed, 120, 80, promoB, 150)
        };
        var service = new DashboardService(new FakeDashboardRepository(reservations, [turn]), Clock, TimeZone);

        var dashboard = await service.GetRevenueIntelligenceAsync(1, new(new(2026, 7, 20), new(2026, 7, 21)), default);

        Assert.NotNull(dashboard.BestPromotion);
        Assert.Equal("Happy Hour", dashboard.BestPromotion!.PromotionName);
        Assert.Equal(250, dashboard.BestPromotion.GrossRevenue);
        Assert.Equal(50, dashboard.BestPromotion.DiscountTotal);
    }

    private static Court Court(int id, string name) => new() { Id = id, ClubId = 1, CourtTypeId = 1, Name = name, HourPrice = 100, IsActive = true };

    private static AvailableTurn Turn(int id, Court court, int hour) => new()
    {
        Id = id,
        CourtId = court.Id,
        Court = court,
        StartTime = new(hour, 0),
        EndTime = new(hour + 1, 0),
        IsActive = true
    };

    private static Reservation Reservation(int id, AvailableTurn turn, DateOnly date, int statusId, decimal finalPrice, decimal paid, Promotion? promotion = null, decimal? basePrice = null)
    {
        var reservation = new Reservation
        {
            Id = id,
            ClientId = id,
            Client = new Client { Id = id, PersonId = id, Person = new Person { Id = id, FirstName = "Ana", LastName = "Paz", Dni = $"30{id:000000}", Phone = "1140001001", Email = $"ana{id}@example.com" } },
            AvailableTurnId = turn.Id,
            AvailableTurn = turn,
            EmployeeId = 1,
            ReservationDate = date,
            ReservationStatusId = statusId,
            ReservationStatus = new ReservationStatus { Id = statusId, Name = statusId == ReservationStatusIds.Cancelled ? "Canceled" : "Confirmed" },
            PromotionId = promotion?.Id,
            Promotion = promotion,
            BasePrice = basePrice ?? finalPrice,
            FinalPrice = finalPrice,
            CreatedAt = DateTime.UtcNow
        };
        if (paid > 0)
            reservation.Payments.Add(new Payment { Id = id, ReservationId = id, Amount = paid, PaymentDate = DateTime.UtcNow });
        return reservation;
    }
}

internal sealed class FakeDashboardRepository(IReadOnlyList<Reservation> reservations, IReadOnlyList<AvailableTurn> turns) : IDashboardRepository
{
    public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    public Task<int> CountActiveCourtsAsync(int clubId, CancellationToken cancellationToken) => Task.FromResult(0);
    public Task<int> CountReservationsAsync(int clubId, DateOnly date, CancellationToken cancellationToken) => Task.FromResult(0);
    public Task<decimal> SumIncomeAsync(int clubId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) => Task.FromResult(0m);
    public Task<List<Reservation>> GetLatestReservationsAsync(int clubId, DateOnly date, int take, CancellationToken cancellationToken) => Task.FromResult(new List<Reservation>());
    public Task<DashboardRevenueIntelligenceData> GetRevenueIntelligenceAsync(int clubId, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken) =>
        Task.FromResult(new DashboardRevenueIntelligenceData(
            reservations.Where(x => x.AvailableTurn.Court.ClubId == clubId && x.ReservationDate >= dateFrom && x.ReservationDate <= dateTo).ToList(),
            turns.Where(x => x.Court.ClubId == clubId && x.IsActive && x.Court.IsActive).ToList()));
}
