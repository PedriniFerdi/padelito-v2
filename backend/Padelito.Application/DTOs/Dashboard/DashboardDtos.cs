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

