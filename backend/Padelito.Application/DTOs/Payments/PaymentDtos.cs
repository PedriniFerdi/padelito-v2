namespace Padelito.Application.DTOs.Payments;

public sealed record PaymentFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int? MethodId = null,
    int? ReservationId = null);

public sealed record PaymentCreateDto(
    int ReservationId,
    int PaymentMethodId,
    decimal Amount,
    DateTime PaymentDate,
    string? Note);

public sealed record PaymentListDto(
    int Id,
    int ReservationId,
    DateOnly ReservationDate,
    string ClientName,
    string CourtName,
    int PaymentMethodId,
    string PaymentMethod,
    decimal Amount,
    DateTime PaymentDate,
    string? Note,
    decimal FinalPrice,
    decimal TotalPaid,
    decimal PendingBalance);

public sealed record PaymentSummaryDto(decimal FinalPrice, decimal TotalPaid, decimal PendingBalance);
