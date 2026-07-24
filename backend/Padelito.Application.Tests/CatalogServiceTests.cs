using Microsoft.AspNetCore.Identity;
using Padelito.Application.Common;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task Client_profile_calculates_lifetime_metrics_from_club_reservations()
    {
        var repository = new CatalogRepositoryFake
        {
            ProfileClient = ClientWithReservations(
                Reservation(1, new(2026, 7, 6), ReservationStatusIds.Completed, new(19, 0), 100m, 100m),
                Reservation(2, new(2026, 7, 13), ReservationStatusIds.Completed, new(19, 0), 120m, 70m),
                Reservation(3, new(2026, 7, 15), ReservationStatusIds.Completed, new(18, 0), 90m, 90m),
                Reservation(4, new(2026, 7, 20), ReservationStatusIds.Confirmed, new(20, 0), 200m, 50m),
                Reservation(5, new(2026, 7, 21), ReservationStatusIds.Pending, new(21, 0), 160m, 0m),
                Reservation(6, new(2026, 7, 22), ReservationStatusIds.Cancelled, new(22, 0), 500m, 0m))
        };
        var service = new CatalogService(repository, new PasswordHasher<User>());

        var profile = await service.GetClientProfileAsync(1, 1, default);

        Assert.Equal(6, profile.TotalReservations);
        Assert.Equal(310m, profile.TotalPaid);
        Assert.Equal(360m, profile.PendingBalance);
        Assert.Equal("Monday", profile.FavoriteDayName);
        Assert.Equal(new TimeOnly(19, 0), profile.FavoriteStartTime);
        Assert.Equal(new DateOnly(2026, 7, 15), profile.LastVisitDate);
        Assert.Equal(1, profile.CancellationCount);
    }

    [Fact]
    public async Task Client_profile_throws_business_error_when_client_does_not_exist()
    {
        var service = new CatalogService(new CatalogRepositoryFake(), new PasswordHasher<User>());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.GetClientProfileAsync(404, 1, default));

        Assert.Equal("The customer does not exist.", exception.Message);
    }

    private static Client ClientWithReservations(params Reservation[] reservations)
    {
        var client = new Client
        {
            Id = 1,
            PersonId = 1,
            Person = new Person
            {
                Id = 1,
                FirstName = "Ana",
                LastName = "Paz",
                Dni = "30111222",
                Phone = "1140001001",
                Email = "ana@example.com",
                IsActive = true
            }
        };

        foreach (var reservation in reservations)
        {
            reservation.ClientId = client.Id;
            reservation.Client = client;
            client.Reservations.Add(reservation);
        }

        return client;
    }

    private static Reservation Reservation(int id, DateOnly date, int statusId, TimeOnly startTime, decimal finalPrice, decimal paid)
    {
        var reservation = new Reservation
        {
            Id = id,
            ReservationDate = date,
            ReservationStatusId = statusId,
            ReservationStatus = new ReservationStatus { Id = statusId, Name = statusId.ToString() },
            AvailableTurnId = id,
            AvailableTurn = new AvailableTurn
            {
                Id = id,
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                Court = new Court { Id = id, ClubId = 1, Name = $"Court {id}" }
            },
            BasePrice = finalPrice,
            FinalPrice = finalPrice,
            CreatedAt = DateTime.UtcNow
        };

        if (paid > 0)
        {
            reservation.Payments.Add(new Payment { Id = id, ReservationId = id, Amount = paid, PaymentDate = DateTime.UtcNow });
        }

        return reservation;
    }

    private sealed class CatalogRepositoryFake : ICatalogRepository
    {
        public Client? ProfileClient { get; init; }

        public Task<Client?> GetClientProfileAsync(int id, int clubId, CancellationToken cancellationToken) =>
            Task.FromResult(ProfileClient?.Id == id ? ProfileClient : null);

        public Task<List<PaymentMethod>> GetPaymentMethodsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<Client>> GetClientsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddClientAsync(Client client, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<EmployeeReadModel>> GetEmployeesAsync(int clubId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<User>> GetUsersAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<User?> GetUserAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddUserAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<Role>> GetRolesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Role?> GetRoleAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> UsernameExistsAsync(string username, int? excludingUserId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> PersonDniExistsAsync(string dni, int? excludingPersonId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> EmployeeHasUserAsync(int employeeId, int? excludingUserId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<CourtType>> GetCourtTypesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CourtType?> GetCourtTypeAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddCourtTypeAsync(CourtType courtType, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CourtTypeExistsAsync(string description, int? excludingId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<Court>> GetCourtsAsync(int clubId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Court?> GetCourtAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddCourtAsync(Court court, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CourtNameExistsAsync(int clubId, string name, int? excludingId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<AvailableTurn>> GetAvailableTurnsAsync(int clubId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<AvailableTurn?> GetAvailableTurnAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddAvailableTurnAsync(AvailableTurn turn, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> AvailableTurnOverlapsAsync(int courtId, TimeOnly startTime, TimeOnly endTime, int? excludingId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<Promotion>> GetPromotionsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Promotion?> GetPromotionAsync(int id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task AddPromotionAsync(Promotion promotion, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
