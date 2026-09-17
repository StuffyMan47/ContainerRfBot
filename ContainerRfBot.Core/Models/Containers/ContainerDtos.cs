using ContainerRfBot.Core.Enums.SiteEnums;

namespace ContainerRfBot.Core.Models.Containers;

public class CreateContainerListRequest
{
    public Guid Id { get; set; }
    public long ArticleId { get; set; }
    public CategoryEnum CategoryId { get; set; }
    public required int Quantity { get; set; }
    public ConditionEnum Condition { get; set; }
    public required string Username { get; set; }
    public PriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public CurrencyEnum Currency { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public long? UserId { get; set; }
    public string? MessageId { get; set; }
}

public class GetContainerListResponse
{
    public Guid Id { get; set; }
    public long ArticleId { get; set; }
    public CategoryEnum CategoryId { get; set; }
    public required int Quantity { get; set; }
    public ConditionEnum Condition { get; set; }
    public required string Username { get; set; }
    public PriceType PriceType { get; set; }
    public decimal Price { get; set; }
    public CurrencyEnum Currency { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
