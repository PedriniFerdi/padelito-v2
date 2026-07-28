using Padelito.Application.Common;
using Padelito.Application.DTOs.Reservations;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class ReservationService(
    IReservationRepository repository,
    IReservationLifecycleService lifecycleService,
    TimeProvider timeProvider,
    TimeZoneInfo clubTimeZone) : IReservationService
{
    public async Task<IReadOnlyList<ReservationListDto>> GetReservationsAsync(
        int clubId,
        ReservationFilterDto filter,
        CancellationToken cancellationToken)
    {
        var view = filter.View.Trim().ToLowerInvariant();
        var statuses = view switch
        {
            "active" => ReservationStatusIds.Active,
            "history" => ReservationStatusIds.History,
            _ => throw new BusinessException("View must be active or history.")
        };

        if (filter.DateFrom.HasValue && filter.DateTo.HasValue && filter.DateTo < filter.DateFrom)
        {
            throw new BusinessException("End date must be on or after start date.");
        }

        if (filter.StatusId.HasValue && !statuses.Contains(filter.StatusId.Value))
        {
            throw new BusinessException("The status does not match the selected view.");
        }

        await lifecycleService.ReconcileAsync(clubId, cancellationToken);
        var localNow = GetLocalNow();
        var reservations = await repository.GetReservationsAsync(
            clubId, statuses, filter.DateFrom, filter.DateTo, filter.StatusId, cancellationToken);
        if (view == "active")
        {
            reservations = reservations.Where(x => SlotStart(x) > localNow).ToList();
        }
        return reservations.Select(x => ToListDto(x, localNow)).ToList();
    }

    public async Task<ReservationDetailDto> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken)
    {
        await lifecycleService.ReconcileAsync(clubId, cancellationToken);
        return ToDetailDto(await RequireReservationAsync(id, clubId, false, cancellationToken), GetLocalNow());
    }

    public async Task<IReadOnlyList<ReservationAvailabilityDto>> GetAvailabilityAsync(
        int clubId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var now = GetLocalNow();
        var today = DateOnly.FromDateTime(now);
        if (date < today)
        {
            throw new BusinessException("Availability cannot be checked for a past date.");
        }

        var currentTime = TimeOnly.FromDateTime(now);
        var turns = await repository.GetAvailabilityAsync(clubId, date, cancellationToken);
        return turns
            .Where(turn => date > today || turn.StartTime > currentTime)
            .Select(turn => new ReservationAvailabilityDto(
                turn.Id,
                turn.CourtId,
                turn.Court.Name,
                turn.Court.CourtType.Description,
                turn.StartTime,
                turn.EndTime,
                CalculateBasePrice(turn)))
            .ToList();
    }

    public async Task<OperationsBoardDto> GetOperationsBoardAsync(int clubId, CancellationToken cancellationToken)
    {
        await lifecycleService.ReconcileAsync(clubId, cancellationToken);
        var generatedAt = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(generatedAt, clubTimeZone).DateTime;
        var operationalDate = DateOnly.FromDateTime(localNow);
        var currentTime = TimeOnly.FromDateTime(localNow);
        var reservations = await repository.GetOperationsBoardReservationsAsync(clubId, operationalDate, cancellationToken);
        var activeReservations = reservations
            .Where(x => x.ReservationStatusId is ReservationStatusIds.Pending or ReservationStatusIds.Confirmed)
            .ToList();
        var operationItems = reservations.Select(x => ToOperationsDto(x, localNow)).ToList();

        var timeline = operationItems
            .GroupBy(x => new { x.CourtId, x.CourtName })
            .OrderBy(x => x.Key.CourtName)
            .Select(x => new OperationsCourtTimelineDto(
                x.Key.CourtId,
                x.Key.CourtName,
                x.OrderBy(item => item.StartTime).ThenBy(item => item.Id).ToList()))
            .ToList();

        var upcomingUnpaid = activeReservations
            .Select(x => ToOperationsDto(x, localNow))
            .Where(x => x.PendingBalance > 0)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.CourtName)
            .ToList();
        var startingSoon = activeReservations
            .Where(x =>
            {
                var minutesUntilStart = (x.AvailableTurn.StartTime - currentTime).TotalMinutes;
                return minutesUntilStart >= 0 && minutesUntilStart <= 60;
            })
            .Select(x => ToOperationsDto(x, localNow))
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.CourtName)
            .ToList();

        return new OperationsBoardDto(
            operationalDate,
            generatedAt,
            reservations.Count(x => x.ReservationStatusId != ReservationStatusIds.Cancelled),
            upcomingUnpaid.Count,
            startingSoon.Count,
            reservations.Count(x => x.ReservationStatusId == ReservationStatusIds.Completed),
            timeline,
            upcomingUnpaid,
            startingSoon);
    }

    public async Task<ReservationDetailDto> CreateAsync(
        int clubId,
        int employeeId,
        string username,
        ReservationCreateDto request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationStatusId is not (ReservationStatusIds.Pending or ReservationStatusIds.Confirmed))
        {
            throw new BusinessException("Reservations must be created as Pending or Confirmed.");
        }

        ValidateFutureDate(request.ReservationDate);

        var client = await repository.GetClientAsync(request.ClientId, cancellationToken)
            ?? throw new BusinessException("The selected customer does not exist.");
        if (!client.Person.IsActive)
        {
            throw new BusinessException("Reservations cannot be created for inactive customers.");
        }

        var employee = await repository.GetEmployeeAsync(employeeId, cancellationToken)
            ?? throw new BusinessException("The authenticated staff member does not exist.");
        if (!employee.Person.IsActive || employee.ClubId != clubId)
        {
            throw new BusinessException("The authenticated staff member is not active for this club.");
        }

        var turn = await repository.GetAvailableTurnAsync(request.AvailableTurnId, cancellationToken)
            ?? throw new BusinessException("The selected time slot does not exist.");
        if (!turn.IsActive || !turn.Court.IsActive)
        {
            throw new BusinessException("The court and time slot must be active.");
        }

        if (turn.Court.ClubId != clubId)
        {
            throw new BusinessException("The time slot does not belong to the authenticated club.");
        }

        ValidateFutureTime(request.ReservationDate, turn.StartTime);

        if (await repository.IsOccupiedAsync(request.ReservationDate, request.AvailableTurnId, cancellationToken))
        {
            throw new ConflictException("The selected time slot is already reserved for that date.");
        }

        Promotion? promotion = null;
        if (request.PromotionId.HasValue)
        {
            promotion = await repository.GetPromotionAsync(request.PromotionId.Value, cancellationToken)
                ?? throw new BusinessException("The selected promotion does not exist.");
            if (!promotion.IsActive || request.ReservationDate < promotion.DateFrom || request.ReservationDate > promotion.DateTo)
            {
                throw new BusinessException("The promotion is not active for the reservation date.");
            }
        }

        var status = await repository.GetStatusAsync(request.ReservationStatusId, cancellationToken)
            ?? throw new BusinessException("The selected status does not exist.");
        var basePrice = CalculateBasePrice(turn);
        var finalPrice = CalculateFinalPrice(basePrice, promotion?.DiscountPercentage);
        var reservation = new Reservation
        {
            ClientId = client.Id,
            Client = client,
            AvailableTurnId = turn.Id,
            AvailableTurn = turn,
            EmployeeId = employee.Id,
            Employee = employee,
            PromotionId = promotion?.Id,
            Promotion = promotion,
            ReservationDate = request.ReservationDate,
            ReservationStatusId = status.Id,
            ReservationStatus = status,
            BasePrice = basePrice,
            FinalPrice = finalPrice,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        reservation.Audits.Add(new ReservationAudit
        {
            Reservation = reservation,
            Action = "Created",
            Description = $"Reservation created with status {status.Name} for {client.Person.FirstName} {client.Person.LastName}, court {turn.Court.Name}, time slot {turn.StartTime:HH\\:mm}-{turn.EndTime:HH\\:mm}.",
            Username = username,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });

        await repository.AddAsync(reservation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDetailDto(reservation, GetLocalNow());
    }

    public async Task<ReservationDetailDto> ChangeStatusAsync(
        int id,
        int clubId,
        string username,
        ReservationChangeStatusDto request,
        CancellationToken cancellationToken)
    {
        await lifecycleService.ReconcileAsync(clubId, cancellationToken);
        var utcNow = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, clubTimeZone).DateTime;
        var reservation = await repository.ChangeStatusAsync(
            id,
            clubId,
            request.ReservationStatusId,
            username,
            localNow,
            utcNow.UtcDateTime,
            cancellationToken);
        return ToDetailDto(reservation, localNow);
    }

    private async Task<Reservation> RequireReservationAsync(int id, int clubId, bool trackChanges, CancellationToken cancellationToken)
    {
        return await repository.GetReservationAsync(id, clubId, trackChanges, cancellationToken)
            ?? throw new BusinessException("The reservation does not exist.");
    }

    private DateTime GetLocalNow()
    {
        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), clubTimeZone).DateTime;
    }

    private void ValidateFutureDate(DateOnly date)
    {
        if (date == default)
        {
            throw new BusinessException("Reservation date is required.");
        }

        if (date < DateOnly.FromDateTime(GetLocalNow()))
        {
            throw new BusinessException("Reservations cannot be created for a past date.");
        }
    }

    private void ValidateFutureTime(DateOnly date, TimeOnly startTime)
    {
        var now = GetLocalNow();
        if (date == DateOnly.FromDateTime(now) && startTime <= TimeOnly.FromDateTime(now))
        {
            throw new BusinessException("Reservations cannot be created for a time slot that has already started.");
        }
    }

    private static decimal CalculateBasePrice(AvailableTurn turn)
    {
        var durationMinutes = (decimal)(turn.EndTime - turn.StartTime).TotalMinutes;
        return decimal.Round(turn.Court.HourPrice * durationMinutes / 60m, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateFinalPrice(decimal basePrice, decimal? discountPercentage)
    {
        var multiplier = 1m - (discountPercentage ?? 0m) / 100m;
        return decimal.Round(basePrice * multiplier, 2, MidpointRounding.AwayFromZero);
    }

    private static ReservationListDto ToListDto(Reservation reservation, DateTime localNow)
    {
        var (totalPaid, pendingBalance, paymentStatus) = PaymentSummary(reservation);
        var (canCollect, canConfirm, canCancel) = Capabilities(reservation, localNow);
        return new ReservationListDto(
            reservation.Id,
            reservation.ReservationDate,
            reservation.ClientId,
            FullName(reservation.Client.Person),
            reservation.AvailableTurnId,
            reservation.AvailableTurn.Court.Name,
            reservation.AvailableTurn.StartTime,
            reservation.AvailableTurn.EndTime,
            reservation.ReservationStatusId,
            reservation.ReservationStatus.Name,
            reservation.Promotion?.Name,
            reservation.BasePrice,
            reservation.FinalPrice,
            totalPaid,
            pendingBalance,
            paymentStatus,
            canCollect,
            canConfirm,
            canCancel,
            reservation.CreatedAt);
    }

    private static ReservationDetailDto ToDetailDto(Reservation reservation, DateTime localNow)
    {
        var (totalPaid, pendingBalance, paymentStatus) = PaymentSummary(reservation);
        var (canCollect, canConfirm, canCancel) = Capabilities(reservation, localNow);
        return new ReservationDetailDto(
            reservation.Id,
            reservation.ReservationDate,
            reservation.ClientId,
            FullName(reservation.Client.Person),
            reservation.AvailableTurnId,
            reservation.AvailableTurn.CourtId,
            reservation.AvailableTurn.Court.Name,
            reservation.AvailableTurn.Court.CourtType.Description,
            reservation.AvailableTurn.StartTime,
            reservation.AvailableTurn.EndTime,
            reservation.EmployeeId,
            FullName(reservation.Employee.Person),
            reservation.PromotionId,
            reservation.Promotion?.Name,
            reservation.Promotion?.DiscountPercentage,
            reservation.ReservationStatusId,
            reservation.ReservationStatus.Name,
            reservation.BasePrice,
            reservation.FinalPrice,
            totalPaid,
            pendingBalance,
            paymentStatus,
            canCollect,
            canConfirm,
            canCancel,
            reservation.CreatedAt);
    }

    private static OperationsReservationDto ToOperationsDto(Reservation reservation, DateTime localNow)
    {
        var (totalPaid, pendingBalance, paymentStatus) = PaymentSummary(reservation);
        var (canCollect, canConfirm, canCancel) = Capabilities(reservation, localNow);
        return new OperationsReservationDto(
            reservation.Id,
            reservation.ReservationDate,
            reservation.ClientId,
            FullName(reservation.Client.Person),
            reservation.AvailableTurnId,
            reservation.AvailableTurn.CourtId,
            reservation.AvailableTurn.Court.Name,
            reservation.AvailableTurn.StartTime,
            reservation.AvailableTurn.EndTime,
            reservation.ReservationStatusId,
            reservation.ReservationStatus.Name,
            reservation.FinalPrice,
            totalPaid,
            pendingBalance,
            paymentStatus,
            canCollect,
            canConfirm,
            canCancel);
    }

    private static (decimal TotalPaid, decimal PendingBalance, string PaymentStatus) PaymentSummary(Reservation reservation)
    {
        var totalPaid = reservation.Payments.Sum(x => x.Amount);
        var canceled = reservation.ReservationStatusId == ReservationStatusIds.Cancelled;
        var active = reservation.ReservationStatusId is ReservationStatusIds.Pending or ReservationStatusIds.Confirmed;
        var pendingBalance = active ? Math.Max(0, reservation.FinalPrice - totalPaid) : 0;
        var paymentStatus = canceled ? "Canceled" : totalPaid == reservation.FinalPrice ? "Paid" : "Unpaid";
        return (totalPaid, pendingBalance, paymentStatus);
    }

    private static (bool CanCollect, bool CanConfirm, bool CanCancel) Capabilities(
        Reservation reservation,
        DateTime localNow)
    {
        var active = reservation.ReservationStatusId is ReservationStatusIds.Pending or ReservationStatusIds.Confirmed;
        var beforeStart = SlotStart(reservation) > localNow;
        var hasPayments = reservation.Payments.Count != 0;
        return (
            active && beforeStart && !hasPayments,
            reservation.ReservationStatusId == ReservationStatusIds.Pending && beforeStart,
            active && beforeStart && !hasPayments);
    }

    private static DateTime SlotStart(Reservation reservation) =>
        reservation.ReservationDate.ToDateTime(reservation.AvailableTurn.StartTime);

    private static string FullName(Person person) => $"{person.FirstName} {person.LastName}";
}
