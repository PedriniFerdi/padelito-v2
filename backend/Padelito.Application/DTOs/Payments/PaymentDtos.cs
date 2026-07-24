using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Payments;

public sealed record PaymentFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int? MethodId = null,
    int? ReservationId = null);

public sealed record PaymentCreateDto(
    [Range(1, int.MaxValue, ErrorMessage = "Select a reservation.")] int ReservationId,
    [Range(1, int.MaxValue, ErrorMessage = "Select a payment method.")] int PaymentMethodId,
    [Range(0.01, 99999999.99, ErrorMessage = "Amount must be greater than zero.")] decimal Amount,
    DateTime PaymentDate,
    [StringLength(255, ErrorMessage = "Note cannot exceed 255 characters.")] string? Note);

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
