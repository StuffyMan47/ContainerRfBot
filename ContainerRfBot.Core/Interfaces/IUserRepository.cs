using ContainerRfBot.Core.Entities;

namespace ContainerRfBot.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<List<User>> GetUsersWithExpiringSubscriptionAsync(TimeSpan timeToExpiration, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}