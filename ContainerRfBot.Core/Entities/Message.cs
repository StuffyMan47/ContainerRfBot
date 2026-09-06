namespace ContainerRfBot.Core.Entities;

public class Message
{
    public long Id { get; set; }
    
    public long UserId { get; set; }
    public User User { get; set; }
    
    public string Content { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}