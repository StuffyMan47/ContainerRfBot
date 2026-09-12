using ContainerRfBot.Core.Entities;

namespace ContainerRfBot.Infrastructure.DAL.Entites;

public class Message : BaseEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}