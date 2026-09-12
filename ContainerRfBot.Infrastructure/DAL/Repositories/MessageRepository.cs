using Microsoft.EntityFrameworkCore;
using ContainerRfBot.Core.Models;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Infrastructure.DAL.DbContext;
using ContainerRfBot.Infrastructure.DAL.Entites;

namespace ContainerRfBot.Infrastructure.DAL.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MessageModel message, CancellationToken cancellationToken = default)
    {
        var entity = new Message
        {
            UserId = message.UserId,
            Content = message.Content,
            SentAt = message.SentAt
        };
        await _context.Messages.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        message.Id = entity.Id;
    }

    public async Task<List<MessageModel>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var messages = await _context.Messages
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync(cancellationToken);
            
        return messages.Select(m => new MessageModel
        {
            Id = m.Id,
            UserId = m.UserId,
            Content = m.Content,
            SentAt = m.SentAt
        }).ToList();
    }
}