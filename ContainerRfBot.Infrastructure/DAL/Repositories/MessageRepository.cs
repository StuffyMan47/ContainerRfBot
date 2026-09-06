using Microsoft.EntityFrameworkCore;
using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Interfaces;

namespace ContainerRfBot.Infrastructure.DAL.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Message>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
    }
}