namespace ContainerRfBot.Core.Models;

public class MessageModel
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}