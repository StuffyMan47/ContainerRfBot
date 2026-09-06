using Microsoft.EntityFrameworkCore;
using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Interfaces;

namespace ContainerRfBot.Infrastructure.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<User>> GetUsersWithExpiringSubscriptionAsync(TimeSpan timeToExpiration, CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.Add(timeToExpiration);
        var targetDatePlusOneDay = targetDate.AddDays(1);
        
        return _context.Users
            .Where(x => x.HasSubscription && 
                        x.SubscriptionExpirationDate.HasValue && 
                        x.SubscriptionExpirationDate.Value >= targetDate &&
                        x.SubscriptionExpirationDate.Value < targetDatePlusOneDay)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}