using Padelito.Application.DTOs.Dashboard;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Application.Common;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class DashboardService(IDashboardRepository repository, TimeProvider timeProvider, TimeZoneInfo clubTimeZone) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(int clubId, CancellationToken cancellationToken)
    {
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), clubTimeZone);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, clubTimeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, clubTimeZone);

        var clients = await repository.CountActiveClientsAsync(cancellationToken);
        var courts = await repository.CountActiveCourtsAsync(clubId, cancellationToken);
        var reservations = await repository.CountReservationsAsync(clubId, date, cancellationToken);
        var income = await repository.SumIncomeAsync(clubId, fromUtc, toUtc, cancellationToken);
        var latest = await repository.GetLatestReservationsAsync(clubId, date, 5, cancellationToken);
        return new(date, clients, courts, reservations, income, latest.Select(x => new DashboardReservationDto(
            x.Id, x.ReservationDate, $"{x.Client.Person.FirstName} {x.Client.Person.LastName}",
            x.AvailableTurn.Court.Name, x.AvailableTurn.StartTime, x.ReservationStatus.Name, x.FinalPrice)).ToList());
    }

    public async Task<DashboardRevenueIntelligenceDto> GetRevenueIntelligenceAsync(int clubId, DashboardRevenueIntelligenceFilterDto filter, CancellationToken cancellationToken)
    {
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), clubTimeZone);
        var defaultTo = DateOnly.FromDateTime(localNow.DateTime);
        var dateTo = filter.DateTo ?? defaultTo;
        var dateFrom = filter.DateFrom ?? dateTo.AddDays(-29);
        if (dateTo < dateFrom)
            throw new BusinessException("End date must be on or after start date.");

        var data = await repository.GetRevenueIntelligenceAsync(clubId, dateFrom, dateTo, cancellationToken);
        var days = dateTo.DayNumber - dateFrom.DayNumber + 1;
        var activeReservations = data.Reservations.Where(x => x.ReservationStatusId != ReservationStatusIds.Cancelled).ToList();
        var totalAvailableSlots = data.ActiveTurns.Count * days;
        var reservedSlots = activeReservations.Count;
        var totalRevenue = activeReservations.Sum(Paid);
        var reservedValue = activeReservations.Sum(x => x.FinalPrice);
        var pendingBalance = activeReservations.Sum(x => Math.Max(0, x.FinalPrice - Paid(x)));
        var cancellationRate = Rate(data.Reservations.Count(x => x.ReservationStatusId == ReservationStatusIds.Cancelled), data.Reservations.Count);
        var averageOccupancy = Rate(reservedSlots, totalAvailableSlots);

        var activeTurnsByCourt = data.ActiveTurns.GroupBy(x => new { x.CourtId, x.Court.Name }).ToDictionary(x => x.Key.CourtId, x => new
        {
            x.Key.Name,
            Slots = x.Count()
        });
        var reservationsByCourt = activeReservations.GroupBy(x => x.AvailableTurn.CourtId).ToDictionary(x => x.Key, x => x.ToList());
        var courts = activeTurnsByCourt.Select(x =>
        {
            reservationsByCourt.TryGetValue(x.Key, out var courtReservations);
            courtReservations ??= [];
            var availableSlots = x.Value.Slots * days;
            return new DashboardCourtPerformanceDto(
                x.Key,
                x.Value.Name,
                courtReservations.Count,
                availableSlots,
                Rate(courtReservations.Count, availableSlots),
                courtReservations.Sum(Paid));
        }).OrderByDescending(x => x.Revenue).ThenByDescending(x => x.OccupancyRate).ThenBy(x => x.CourtName).ToList();

        var slotCapacityByHour = data.ActiveTurns
            .GroupBy(x => x.StartTime.Hour)
            .ToDictionary(x => x.Key, x => x.Count());
        var reservationsByDayHour = activeReservations
            .GroupBy(x => new { DayOfWeek = (int)x.ReservationDate.DayOfWeek, x.AvailableTurn.StartTime.Hour })
            .ToDictionary(x => (x.Key.DayOfWeek, x.Key.Hour), x => x.Count());
        var daysInRange = DatesInRange(dateFrom, dateTo).Select(x => (int)x.DayOfWeek).Distinct().OrderBy(x => x).ToList();
        var demand = daysInRange
            .SelectMany(day => slotCapacityByHour.Keys.OrderBy(hour => hour).Select(hour =>
            {
                var reservationCount = reservationsByDayHour.GetValueOrDefault((day, hour));
                var dayHourCapacity = slotCapacityByHour[hour] * CountDatesForDay(dateFrom, dateTo, day);
                return new DashboardDemandDto(day, DayName(day), hour, reservationCount, Rate(reservationCount, dayHourCapacity));
            }))
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.Hour).ToList();

        var rankedDemand = demand.Select(x => new DashboardDemandWindowDto(x.DayOfWeek, x.DayName, x.Hour, x.ReservationCount, x.OccupancyRate)).ToList();
        var peakDemand = rankedDemand.OrderByDescending(x => x.ReservationCount).ThenByDescending(x => x.OccupancyRate).ThenBy(x => x.DayOfWeek).ThenBy(x => x.Hour).Take(5).ToList();
        var offPeakDemand = rankedDemand.OrderBy(x => x.ReservationCount).ThenBy(x => x.OccupancyRate).ThenBy(x => x.DayOfWeek).ThenBy(x => x.Hour).Take(5).ToList();

        var bestPromotion = activeReservations
            .Where(x => x.PromotionId.HasValue && x.Promotion is not null)
            .GroupBy(x => new { PromotionId = x.PromotionId!.Value, x.Promotion!.Name })
            .Select(x =>
            {
                var collected = x.Sum(Paid);
                var gross = x.Sum(r => r.BasePrice);
                var final = x.Sum(r => r.FinalPrice);
                return new DashboardPromotionPerformanceDto(x.Key.PromotionId, x.Key.Name, x.Count(), gross, Math.Max(0, gross - final), collected);
            })
            .OrderByDescending(x => x.CollectedRevenue > 0 ? x.CollectedRevenue : x.GrossRevenue - x.DiscountTotal)
            .ThenByDescending(x => x.ReservationCount)
            .FirstOrDefault();

        return new DashboardRevenueIntelligenceDto(
            dateFrom,
            dateTo,
            new DashboardRevenueIntelligenceSummaryDto(totalRevenue, reservedValue, pendingBalance, cancellationRate, averageOccupancy),
            courts,
            demand,
            peakDemand,
            offPeakDemand,
            bestPromotion);
    }

    private static decimal Paid(Reservation reservation) => reservation.Payments.Sum(x => x.Amount);

    private static decimal Rate(int count, int total) => total <= 0 ? 0 : Math.Round(count * 100m / total, 2);

    private static int CountDatesForDay(DateOnly dateFrom, DateOnly dateTo, int dayOfWeek)
    {
        var count = 0;
        foreach (var date in DatesInRange(dateFrom, dateTo))
        {
            if ((int)date.DayOfWeek == dayOfWeek)
                count++;
        }
        return count;
    }

    private static IEnumerable<DateOnly> DatesInRange(DateOnly dateFrom, DateOnly dateTo)
    {
        for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
            yield return date;
    }

    private static string DayName(int dayOfWeek) => dayOfWeek switch
    {
        0 => "Sunday",
        1 => "Monday",
        2 => "Tuesday",
        3 => "Wednesday",
        4 => "Thursday",
        5 => "Friday",
        6 => "Saturday",
        _ => "Dia"
    };
}
