using System.Text.Json.Serialization;
using ContainerRfBot.Core.Enums.SiteEnums;

namespace ContainerRfBot.Bot.Services.SiteService.Model;

public class SendContainersInfoRequest
{
    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = string.Empty;
    
    [JsonPropertyName("categoryId")]
    public required CategoryEnum CategoryId { get; set; }

    [JsonPropertyName("quantity")]
    public required int Quantity { get; set; }
    
    [JsonPropertyName("condition")]
    public ConditionEnum Condition { get; set; }
    
    [JsonPropertyName("location")]
    public required LocationDetails Location { get; set; }
    
    [JsonPropertyName("telegramUsername")]
    public required string Username { get; set; }
    
    [JsonPropertyName("phone")]
    public string? PhoneNumber { get; set; }
    
    [JsonPropertyName("priceType")]
    public PriceType PriceType { get; set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("currency")]
    public required CurrencyEnum Currency { get; set; }
    
    [JsonPropertyName("address")]
    public required string Address { get; set; }
    
    [JsonPropertyName("description")]
    public required string Description { get; set; }
}

public class LocationDetails
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}
