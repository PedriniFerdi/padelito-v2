using Padelito.Application.Common;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task Create_accepts_partial_payments_and_calculates_balance()
    {
        var repository = new FakePaymentRepository();
        var service = new PaymentService(repository);
        var first = await service.CreateAsync(1, Request(40), default);
        var second = await service.CreateAsync(1, Request(60), default);
        Assert.Equal(60, first.PendingBalance);
        Assert.Equal(100, second.TotalPaid);
        Assert.Equal(0, second.PendingBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Create_rejects_invalid_amount(decimal amount)
    {
        var service = new PaymentService(new FakePaymentRepository());
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(1, Request(amount), default));
    }

    [Fact]
    public async Task Create_rejects_cancelled_reservation_and_unknown_method()
    {
        var cancelled = new FakePaymentRepository();
        cancelled.Reservation.ReservationStatusId = ReservationStatusIds.Cancelled;
        await Assert.ThrowsAsync<BusinessException>(() => new PaymentService(cancelled).CreateAsync(1, Request(10), default));

        var unknownMethod = new FakePaymentRepository { MethodExists = false };
        await Assert.ThrowsAsync<BusinessException>(() => new PaymentService(unknownMethod).CreateAsync(1, Request(10), default));
    }

    [Fact]
    public async Task List_rejects_inverted_date_range()
    {
        var service = new PaymentService(new FakePaymentRepository());
        await Assert.ThrowsAsync<BusinessException>(() => service.GetPaymentsAsync(1,
            new(new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 11)), default));
    }

    private static PaymentCreateDto Request(decimal amount) => new(1, 1, amount, new DateTime(2026, 7, 12, 15, 0, 0, DateTimeKind.Utc), null);
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    public bool MethodExists { get; set; } = true;
    public Reservation Reservation { get; }
    private readonly PaymentMethod _method = new() { Id = 1, Description = "Efectivo" };

    public FakePaymentRepository()
    {
        var person = new Person { Id = 1, FirstName = "Ana", LastName = "Paz", IsActive = true };
        var client = new Client { Id = 1, PersonId = 1, Person = person };
        var court = new Court { Id = 1, ClubId = 1, CourtTypeId = 1, Name = "Central", IsActive = true };
        Reservation = new Reservation { Id = 1, ClientId = 1, Client = client, AvailableTurnId = 1,
            AvailableTurn = new AvailableTurn { Id = 1, CourtId = 1, Court = court }, EmployeeId = 1,
            ReservationDate = new DateOnly(2026, 7, 12), ReservationStatusId = ReservationStatusIds.Confirmed,
            FinalPrice = 100 };
    }

    public Task<List<Payment>> GetPaymentsAsync(int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? methodId, int? reservationId, CancellationToken cancellationToken) =>
        Task.FromResult(Reservation.Payments.ToList());
    public Task<Reservation?> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken) => Task.FromResult<Reservation?>(id == 1 && clubId == 1 ? Reservation : null);
    public Task<PaymentMethod?> GetMethodAsync(int id, CancellationToken cancellationToken) => Task.FromResult<PaymentMethod?>(MethodExists && id == 1 ? _method : null);
    public Task<Payment> AddPaymentAsync(int clubId, Payment payment, CancellationToken cancellationToken)
    {
        payment.Id = Reservation.Payments.Count + 1;
        Reservation.Payments.Add(payment);
        return Task.FromResult(payment);
    }
}
