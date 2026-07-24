using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Reservations;

public sealed record ReservationFilterDto(
    string View = "active",
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int? StatusId = null);

public sealed record ReservationListDto(
    int Id,
    DateOnly ReservationDate,
    int ClientId,
    string ClientName,
    int AvailableTurnId,
    string CourtName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int ReservationStatusId,
    string Status,
    string? PromotionName,
    decimal BasePrice,
    decimal FinalPrice,
    DateTime CreatedAt);

public sealed record ReservationDetailDto(
    int Id,
    DateOnly ReservationDate,
    int ClientId,
    string ClientName,
    int AvailableTurnId,
    int CourtId,
    string CourtName,
    string CourtType,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int EmployeeId,
    string EmployeeName,
    int? PromotionId,
    string? PromotionName,
    decimal? DiscountPercentage,
    int ReservationStatusId,
    string Status,
    decimal BasePrice,
    decimal FinalPrice,
    decimal TotalPaid,
    decimal PendingBalance,
    string PaymentStatus,
    DateTime CreatedAt);

public sealed record ReservationAvailabilityDto(
    int AvailableTurnId,
    int CourtId,
    string CourtName,
    string CourtType,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal BasePrice);

public sealed record OperationsBoardDto(
    DateOnly OperationalDate,
    DateTimeOffset GeneratedAt,
    int ReservationsToday,
    int UpcomingUnpaidCount,
    int StartingSoonCount,
    int CompletedCount,
    IReadOnlyList<OperationsCourtTimelineDto> TimelineByCourt,
    IReadOnlyList<OperationsReservationDto> UpcomingUnpaidReservations,
    IReadOnlyList<OperationsReservationDto> StartingSoonReservations);

public sealed record OperationsCourtTimelineDto(
    int CourtId,
    string CourtName,
    IReadOnlyList<OperationsReservationDto> Reservations);

public sealed record OperationsReservationDto(
    int Id,
    DateOnly ReservationDate,
    int ClientId,
    string ClientName,
    int AvailableTurnId,
    int CourtId,
    string CourtName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int ReservationStatusId,
    string Status,
    decimal FinalPrice,
    decimal TotalPaid,
    decimal PendingBalance,
    string PaymentStatus);

public sealed record ReservationCreateDto(
    [Range(1, int.MaxValue, ErrorMessage = "Select a customer.")] int ClientId,
    [Range(1, int.MaxValue, ErrorMessage = "Select a time slot.")] int AvailableTurnId,
    int? PromotionId,
    DateOnly ReservationDate,
    [Range(1, int.MaxValue, ErrorMessage = "Select a status.")] int ReservationStatusId);

public sealed record ReservationChangeStatusDto([Range(1, int.MaxValue, ErrorMessage = "Select a status.")] int ReservationStatusId);
