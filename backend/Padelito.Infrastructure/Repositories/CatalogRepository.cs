using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Padelito.Application.Common;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class CatalogRepository(PadelitoDbContext dbContext) : ICatalogRepository
{
    public Task<List<PaymentMethod>> GetPaymentMethodsAsync(CancellationToken cancellationToken) =>
        dbContext.PaymentMethods.AsNoTracking().OrderBy(x => x.Description).ToListAsync(cancellationToken);

    public Task<List<Client>> GetClientsAsync(CancellationToken cancellationToken)
    {
        return dbContext.Clients.Include(x => x.Person).AsNoTracking().OrderBy(x => x.Person.LastName).ThenBy(x => x.Person.FirstName).ToListAsync(cancellationToken);
    }

    public Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Clients.Include(x => x.Person).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Client?> GetClientProfileAsync(int id, int clubId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .Include(x => x.Person)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (client is null)
        {
            return null;
        }

        client.Reservations = await dbContext.Reservations
            .Include(x => x.AvailableTurn)
            .Include(x => x.Payments)
            .Where(x => x.ClientId == id && x.AvailableTurn.Court.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return client;
    }

    public async Task AddClientAsync(Client client, CancellationToken cancellationToken)
    {
        await dbContext.Clients.AddAsync(client, cancellationToken);
    }

    public Task<List<EmployeeReadModel>> GetEmployeesAsync(int clubId, CancellationToken cancellationToken)
    {
        return dbContext.Employees
            .Where(x => x.ClubId == clubId)
            .AsNoTracking()
            .OrderBy(x => x.Person.LastName)
            .ThenBy(x => x.Person.FirstName)
            .Select(x => new EmployeeReadModel(
                x.Id,
                x.Person.FirstName,
                x.Person.LastName,
                x.Person.Dni ?? string.Empty,
                x.Person.Phone ?? string.Empty,
                x.Person.Email ?? string.Empty,
                x.Person.IsActive,
                dbContext.Users.Any(user => user.EmployeeId == x.Id)))
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Employees.Include(x => x.Person).Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddEmployeeAsync(Employee employee, CancellationToken cancellationToken)
    {
        await dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public Task<List<User>> GetUsersAsync(CancellationToken cancellationToken)
    {
        return dbContext.Users.Include(x => x.Employee).ThenInclude(x => x.Person).Include(x => x.Role).AsNoTracking().OrderBy(x => x.Username).ToListAsync(cancellationToken);
    }

    public Task<User?> GetUserAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Users.Include(x => x.Employee).ThenInclude(x => x.Person).Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task<List<Role>> GetRolesAsync(CancellationToken cancellationToken)
    {
        return dbContext.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Role?> GetRoleAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, int? excludingUserId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.Username == username && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);
    }

    public Task<bool> PersonDniExistsAsync(string dni, int? excludingPersonId, CancellationToken cancellationToken)
    {
        return dbContext.People.AnyAsync(x => x.Dni == dni && (!excludingPersonId.HasValue || x.Id != excludingPersonId.Value), cancellationToken);
    }

    public Task<bool> EmployeeHasUserAsync(int employeeId, int? excludingUserId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(x => x.EmployeeId == employeeId && (!excludingUserId.HasValue || x.Id != excludingUserId.Value), cancellationToken);
    }

    public Task<List<CourtType>> GetCourtTypesAsync(CancellationToken cancellationToken)
    {
        return dbContext.CourtTypes.AsNoTracking().OrderBy(x => x.Description).ToListAsync(cancellationToken);
    }

    public Task<CourtType?> GetCourtTypeAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.CourtTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddCourtTypeAsync(CourtType courtType, CancellationToken cancellationToken)
    {
        await dbContext.CourtTypes.AddAsync(courtType, cancellationToken);
    }

    public Task<bool> CourtTypeExistsAsync(string description, int? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.CourtTypes.AnyAsync(x => x.Description == description && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    public Task<List<Court>> GetCourtsAsync(int clubId, CancellationToken cancellationToken)
    {
        return dbContext.Courts.Include(x => x.CourtType).Where(x => x.ClubId == clubId).AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Court?> GetCourtAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Courts.Include(x => x.CourtType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddCourtAsync(Court court, CancellationToken cancellationToken)
    {
        await dbContext.Courts.AddAsync(court, cancellationToken);
    }

    public Task<bool> CourtNameExistsAsync(int clubId, string name, int? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.Courts.AnyAsync(x => x.ClubId == clubId && x.Name == name && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
    }

    public Task<List<AvailableTurn>> GetAvailableTurnsAsync(int clubId, CancellationToken cancellationToken)
    {
        return dbContext.AvailableTurns.Include(x => x.Court).Where(x => x.Court.ClubId == clubId).AsNoTracking().OrderBy(x => x.Court.Name).ThenBy(x => x.StartTime).ToListAsync(cancellationToken);
    }

    public Task<AvailableTurn?> GetAvailableTurnAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.AvailableTurns.Include(x => x.Court).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAvailableTurnAsync(AvailableTurn turn, CancellationToken cancellationToken)
    {
        await dbContext.AvailableTurns.AddAsync(turn, cancellationToken);
    }

    public Task<bool> AvailableTurnOverlapsAsync(int courtId, TimeOnly startTime, TimeOnly endTime, int? excludingId, CancellationToken cancellationToken)
    {
        return dbContext.AvailableTurns.AnyAsync(
            x => x.CourtId == courtId
                && x.IsActive
                && x.StartTime < endTime
                && x.EndTime > startTime
                && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<List<Promotion>> GetPromotionsAsync(CancellationToken cancellationToken)
    {
        return dbContext.Promotions.AsNoTracking().OrderByDescending(x => x.DateFrom).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Promotion?> GetPromotionAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Promotions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddPromotionAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        await dbContext.Promotions.AddAsync(promotion, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is SqlException { Number: 51001 })
        {
            throw new ConflictException("The time slot overlaps another active slot for the same court.");
        }
    }
}
