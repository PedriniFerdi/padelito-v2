namespace Padelito.Application.DTOs.Dashboard;

public sealed record DashboardReservationDto(
    int Id,
    DateOnly ReservationDate,
    string ClientName,
    string CourtName,
    TimeOnly StartTime,
    string Status,
    decimal FinalPrice);

public sealed record DashboardSummaryDto(
    DateOnly OperationalDate,
    int ActiveClients,
    int ActiveCourts,
    int ReservationsToday,
    decimal IncomeToday,
    IReadOnlyList<DashboardReservationDto> LatestReservations);

public sealed record DashboardRevenueIntelligenceFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);

public sealed record DashboardRevenueIntelligenceSummaryDto(
    decimal TotalRevenue,
    decimal ReservedValue,
    decimal PendingBalance,
    decimal CancellationRate,
    decimal AverageOccupancyRate);

public sealed record DashboardCourtPerformanceDto(
    int CourtId,
    string CourtName,
    int ReservedSlots,
    int AvailableSlots,
    decimal OccupancyRate,
    decimal Revenue);

public sealed record DashboardDemandDto(
    int DayOfWeek,
    string DayName,
    int Hour,
    int ReservationCount,
    decimal OccupancyRate);

public sealed record DashboardDemandWindowDto(
    int DayOfWeek,
    string DayName,
    int Hour,
    int ReservationCount,
    decimal OccupancyRate);

public sealed record DashboardPromotionPerformanceDto(
    int PromotionId,
    string PromotionName,
    int ReservationCount,
    decimal GrossRevenue,
    decimal DiscountTotal,
    decimal CollectedRevenue);

public sealed record DashboardRevenueIntelligenceDto(
    DateOnly DateFrom,
    DateOnly DateTo,
    DashboardRevenueIntelligenceSummaryDto Summary,
    IReadOnlyList<DashboardCourtPerformanceDto> Courts,
    IReadOnlyList<DashboardDemandDto> Demand,
    IReadOnlyList<DashboardDemandWindowDto> PeakDemand,
    IReadOnlyList<DashboardDemandWindowDto> OffPeakDemand,
    DashboardPromotionPerformanceDto? BestPromotion);
