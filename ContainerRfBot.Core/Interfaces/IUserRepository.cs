using ContainerRfBot.Core.Models;

namespace ContainerRfBot.Core.Interfaces;

public interface IUserRepository
{
    Task<UserModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<List<UserModel>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<List<UserModel>> GetUsersWithExpiringSubscriptionAsync(TimeSpan timeToExpiration, CancellationToken cancellationToken = default);
    Task AddAsync(UserModel user, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserModel user, CancellationToken cancellationToken = default);
}