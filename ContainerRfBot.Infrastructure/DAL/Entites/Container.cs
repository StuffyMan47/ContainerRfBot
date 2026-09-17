using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Enums.SiteEnums;

namespace ContainerRfBot.Infrastructure.DAL.Entites;

public class Container : BaseEntity
{
    public long ArticleId { get; set; }
    public CategoryEnum CategoryId { get; set; }
    public int Quantity { get; set; }
    public ConditionEnum Condition { get; set; }
    public string Username { get; set; } = string.Empty;
    public PriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public CurrencyEnum Currency { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public long? UserId { get; set; }
    public User? User { get; set; }
    public string? MessageId { get; set; }
}
