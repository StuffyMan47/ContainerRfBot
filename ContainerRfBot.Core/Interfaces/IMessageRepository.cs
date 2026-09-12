using ContainerRfBot.Core.Models;

namespace ContainerRfBot.Core.Interfaces;

public interface IMessageRepository
{
    Task AddAsync(MessageModel message, CancellationToken cancellationToken = default);
    Task<List<MessageModel>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}