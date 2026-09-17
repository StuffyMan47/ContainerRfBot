using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Models.Containers;
using ContainerRfBot.Infrastructure.DAL.DbContext;
using ContainerRfBot.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;

namespace ContainerRfBot.Infrastructure.DAL.Repositories;

public class ContainerRepository : IContainerRepository
{
    private readonly AppDbContext _dbContext;

    public ContainerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateContainerList(List<CreateContainerListRequest> containers, CancellationToken cancellationToken)
    {
        _dbContext.Containers.AddRange(containers.Select(x => new Container
        {
            ArticleId = x.ArticleId,
            Address = x.Address,
            CategoryId = x.CategoryId,
            CreatedAt = DateTime.UtcNow,
            Condition = x.Condition,
            Currency = x.Currency,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Quantity = x.Quantity,
            Username = x.Username,
            PriceType = x.PriceType,
            Price = x.Price,
            MessageId = x.MessageId,
            UserId = x.UserId,
        }));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task CreateContainer(CreateContainerListRequest container, CancellationToken cancellationToken)
    {
        var cont = new Container
        {
            ArticleId = container.ArticleId,
            Address = container.Address,
            CategoryId = container.CategoryId,
            CreatedAt = DateTime.UtcNow,
            Condition = container.Condition,
            Currency = container.Currency,
            Latitude = container.Latitude,
            Longitude = container.Longitude,
            Quantity = container.Quantity,
            Username = container.Username,
            PriceType = container.PriceType,
            Price = container.Price,
            MessageId = container.MessageId,
            UserId = container.UserId,
        };
        _dbContext.Containers.Add(cont);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<GetContainerListResponse>> GetContainerList(List<long>? ids, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        var result = _dbContext.Containers
            .AsNoTracking()
            .AsQueryable();

        if (ids != null && ids.Count > 0)
        {
            result = result.Where(x => ids.Contains(x.ArticleId));
        }

        if (date.HasValue)
        {
            var startOfDay = date.Value.Date;
            var nextDay = startOfDay.AddDays(1);
            result = result.Where(x => x.CreatedAt >= startOfDay && x.CreatedAt < nextDay);
        }
        
        return await result
            .Select(x => new GetContainerListResponse
            {
                Id = Guid.NewGuid(),
                ArticleId = x.ArticleId,
                Address = x.Address,
                CategoryId = x.CategoryId,
                CreatedAt = x.CreatedAt,
                Condition = x.Condition,
                Currency = x.Currency,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Quantity = x.Quantity,
                Username = x.Username,
                PriceType = x.PriceType,
                Price = x.Price,
            })
            .ToListAsync(cancellationToken);
    }
}
