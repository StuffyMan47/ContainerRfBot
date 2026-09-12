using Microsoft.EntityFrameworkCore;
using ContainerRfBot.Core.Models;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Infrastructure.DAL.DbContext;
using ContainerRfBot.Infrastructure.DAL.Entites;

namespace ContainerRfBot.Infrastructure.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user == null) return null;
        
        return MapToModel(user);
    }

    public async Task<List<UserModel>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.ToListAsync(cancellationToken);
        return users.Select(MapToModel).ToList();
    }

    public async Task<List<UserModel>> GetUsersWithExpiringSubscriptionAsync(TimeSpan timeToExpiration, CancellationToken cancellationToken = default)
    {
        var targetDate = DateTime.UtcNow.Add(timeToExpiration);
        var targetDatePlusOneDay = targetDate.AddDays(1);
        
        var users = await _context.Users
            .Where(x => x.HasSubscription && 
                        x.SubscriptionExpirationDate.HasValue && 
                        x.SubscriptionExpirationDate.Value >= targetDate &&
                        x.SubscriptionExpirationDate.Value < targetDatePlusOneDay)
            .ToListAsync(cancellationToken);
            
        return users.Select(MapToModel).ToList();
    }

    public async Task AddAsync(UserModel user, CancellationToken cancellationToken = default)
    {
        var entity = new User
        {
            Id = user.Id,
            CreatedAt = user.CreatedAt,
            IsAdmin = user.IsAdmin,
            HasSubscription = user.HasSubscription,
            SubscriptionExpirationDate = user.SubscriptionExpirationDate,
            PhoneNumber = user.PhoneNumber,
            State = user.State
        };
        
        await _context.Users.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserModel user, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Users.FirstOrDefaultAsync(x => x.Id == user.Id, cancellationToken);
        if (entity != null)
        {
            entity.IsAdmin = user.IsAdmin;
            entity.HasSubscription = user.HasSubscription;
            entity.SubscriptionExpirationDate = user.SubscriptionExpirationDate;
            entity.PhoneNumber = user.PhoneNumber;
            entity.State = user.State;
            
            _context.Users.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    
    private UserModel MapToModel(User user)
    {
        return new UserModel
        {
            Id = user.Id,
            CreatedAt = user.CreatedAt,
            IsAdmin = user.IsAdmin,
            HasSubscription = user.HasSubscription,
            SubscriptionExpirationDate = user.SubscriptionExpirationDate,
            PhoneNumber = user.PhoneNumber,
            State = user.State
        };
    }
}