using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface ICatalogRepository
{
    Task<List<Client>> GetClientsAsync(CancellationToken cancellationToken);
    Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken);
    Task AddClientAsync(Client client, CancellationToken cancellationToken);

    Task<List<EmployeeReadModel>> GetEmployeesAsync(int clubId, CancellationToken cancellationToken);
    Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken);
    Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken);

    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken);
    Task<User?> GetUserAsync(int id, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task<List<Role>> GetRolesAsync(CancellationToken cancellationToken);
    Task<List<PaymentMethod>> GetPaymentMethodsAsync(CancellationToken cancellationToken);
    Task<Role?> GetRoleAsync(int id, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(string username, int? excludingUserId, CancellationToken cancellationToken);
    Task<bool> PersonDniExistsAsync(string dni, int? excludingPersonId, CancellationToken cancellationToken);
    Task<bool> EmployeeHasUserAsync(int employeeId, int? excludingUserId, CancellationToken cancellationToken);

    Task<List<CourtType>> GetCourtTypesAsync(CancellationToken cancellationToken);
    Task<CourtType?> GetCourtTypeAsync(int id, CancellationToken cancellationToken);
    Task AddCourtTypeAsync(CourtType courtType, CancellationToken cancellationToken);
    Task<bool> CourtTypeExistsAsync(string description, int? excludingId, CancellationToken cancellationToken);

    Task<List<Court>> GetCourtsAsync(int clubId, CancellationToken cancellationToken);
    Task<Court?> GetCourtAsync(int id, CancellationToken cancellationToken);
    Task AddCourtAsync(Court court, CancellationToken cancellationToken);
    Task<bool> CourtNameExistsAsync(int clubId, string name, int? excludingId, CancellationToken cancellationToken);

    Task<List<AvailableTurn>> GetAvailableTurnsAsync(int clubId, CancellationToken cancellationToken);
    Task<AvailableTurn?> GetAvailableTurnAsync(int id, CancellationToken cancellationToken);
    Task AddAvailableTurnAsync(AvailableTurn turn, CancellationToken cancellationToken);
    Task<bool> AvailableTurnOverlapsAsync(int courtId, TimeOnly startTime, TimeOnly endTime, int? excludingId, CancellationToken cancellationToken);

    Task<List<Promotion>> GetPromotionsAsync(CancellationToken cancellationToken);
    Task<Promotion?> GetPromotionAsync(int id, CancellationToken cancellationToken);
    Task AddPromotionAsync(Promotion promotion, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record EmployeeReadModel(
    int Id,
    string FirstName,
    string LastName,
    string Dni,
    string Phone,
    string Email,
    bool IsActive,
    bool HasUser);
