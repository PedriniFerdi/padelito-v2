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

public sealed record ReservationCreateDto(
    int ClientId,
    int AvailableTurnId,
    int? PromotionId,
    DateOnly ReservationDate,
    int ReservationStatusId);

public sealed record ReservationChangeStatusDto(int ReservationStatusId);
