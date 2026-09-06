using ContainerRfBot.Core.Entities;

namespace ContainerRfBot.Core.Interfaces;

public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task<List<Message>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}