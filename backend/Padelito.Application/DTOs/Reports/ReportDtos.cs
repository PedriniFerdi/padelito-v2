namespace Padelito.Application.DTOs.Reports;

public sealed record ReservationReportFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int? StatusId = null);

public sealed record ReservationReportSummaryDto(
    int ReservationCount,
    decimal FinalPriceTotal,
    decimal TotalPaid,
    decimal PendingBalance);

public sealed record ReservationReportRowDto(
    int ReservationId,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ClientName,
    string CourtName,
    int ReservationStatusId,
    string Status,
    string? PromotionName,
    decimal BasePrice,
    decimal FinalPrice,
    decimal TotalPaid,
    decimal PendingBalance,
    string PaymentStatus);

public sealed record ReservationReportDto(
    ReservationReportSummaryDto Summary,
    IReadOnlyList<ReservationReportRowDto> Rows);
