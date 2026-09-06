using ContainerRfBot.Core.Enums;

namespace ContainerRfBot.Core.Entities;

public class User
{
    public long Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Roles
    public bool IsAdmin { get; set; }
    
    // Subscription
    public bool HasSubscription { get; set; }
    public DateTime? SubscriptionExpirationDate { get; set; }
    
    public string? PhoneNumber { get; set; }
    public BotState State { get; set; } = BotState.None;
    
    // Messages tracking
    public List<Message> Messages { get; set; } = new();
}