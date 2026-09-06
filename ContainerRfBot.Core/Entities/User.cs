namespace ContainerRfBot.Core.Entities;

public class User
{
    public long Id { get; set; }
    
    // Telegram info is removed as requested
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Roles
    public bool IsAdmin { get; set; }
    
    // Subscription
    public bool HasSubscription { get; set; }
    public DateTime? SubscriptionExpirationDate { get; set; }
    
    // Messages tracking
    public List<Message> Messages { get; set; } = new();
}