using Microsoft.EntityFrameworkCore;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class UserRepository(PadelitoDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByUsernameWithDetailsAsync(string username, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.Employee)
            .Include(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    public Task<User?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(x => x.Employee)
            .Include(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
