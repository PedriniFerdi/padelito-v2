using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameWithDetailsAsync(string username, CancellationToken cancellationToken);
    Task<User?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken);
}
