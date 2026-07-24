using Padelito.Application.Common;
using Padelito.Application.DTOs.Reports;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task Report_calculates_totals_and_payment_states()
    {
        var repository = new FakeReportRepository([Reservation(1, 1, 100, 100), Reservation(2, 1, 200, 75)]);
        var report = await new ReportService(repository).GetReservationsAsync(1, new(), default);
        Assert.Equal(2, report.Summary.ReservationCount);
        Assert.Equal(300, report.Summary.FinalPriceTotal);
        Assert.Equal(175, report.Summary.TotalPaid);
        Assert.Equal(125, report.Summary.PendingBalance);
        Assert.Contains(report.Rows, x => x.PaymentStatus == "Paid");
        Assert.Contains(report.Rows, x => x.PaymentStatus == "Partially paid");
    }

    [Fact]
    public async Task Report_validates_range_and_repository_isolates_club()
    {
        var repository = new FakeReportRepository([Reservation(1, 1, 100, 0), Reservation(2, 2, 100, 0)]);
        var service = new ReportService(repository);
        await Assert.ThrowsAsync<BusinessException>(() => service.GetReservationsAsync(1, new(new(2026, 7, 20), new(2026, 7, 10)), default));
        var report = await service.GetReservationsAsync(1, new(), default);
        Assert.Single(report.Rows);
        Assert.Equal(1, report.Rows[0].ReservationId);
    }

    [Fact]
    public async Task Csv_has_bom_accents_and_invariant_decimals()
    {
        var service = new ReportService(new FakeReportRepository([Reservation(1, 1, 123.45m, 0, "LucÃ­a") ]));
        var bytes = await service.ExportReservationsCsvAsync(1, new(), default);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("LucÃ­a", csv);
        Assert.Contains("123.45", csv);
    }

    private static Reservation Reservation(int id, int clubId, decimal finalPrice, decimal paid, string firstName = "Ana")
    {
        var reservation = new Reservation
        {
            Id = id, ClientId = id, Client = new Client { Id = id, PersonId = id, Person = new Person { Id = id, FirstName = firstName, LastName = "Paz", Dni = $"30{id:000000}", Phone = "1140001001", Email = $"persona{id}@example.com" } },
            AvailableTurnId = id, AvailableTurn = new AvailableTurn { Id = id, CourtId = id, Court = new Court { Id = id, ClubId = clubId, CourtTypeId = 1, Name = "Central", HourPrice = 100 }, StartTime = new(18,0), EndTime = new(19,0) },
            EmployeeId = 1, ReservationDate = new(2026,7,12), ReservationStatusId = 2, ReservationStatus = new ReservationStatus { Id = 2, Name = "Confirmed" },
            BasePrice = finalPrice, FinalPrice = finalPrice, CreatedAt = DateTime.UtcNow
        };
        if (paid > 0) reservation.Payments.Add(new Payment { Id = id, ReservationId = id, Amount = paid });
        return reservation;
    }
}

internal sealed class FakeReportRepository(IReadOnlyList<Reservation> reservations) : IReportRepository
{
    public Task<List<Reservation>> GetReservationsAsync(int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? statusId, CancellationToken cancellationToken) =>
        Task.FromResult(reservations.Where(x => x.AvailableTurn.Court.ClubId == clubId
            && (!dateFrom.HasValue || x.ReservationDate >= dateFrom) && (!dateTo.HasValue || x.ReservationDate <= dateTo)
            && (!statusId.HasValue || x.ReservationStatusId == statusId)).ToList());
}
