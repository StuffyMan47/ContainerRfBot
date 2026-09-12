using ContainerRfBot.Core.Enums;

namespace ContainerRfBot.Core.Models;

public class UserModel
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool HasSubscription { get; set; }
    public DateTime? SubscriptionExpirationDate { get; set; }
    public string? PhoneNumber { get; set; }
    public BotState State { get; set; }
}