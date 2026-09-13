namespace ContainerRfBot.Bot.AiTunnelService.Dto;

public class ChatCompletionResponse
{
    public Choice[] Choices { get; set; } = Array.Empty<Choice>();
}

public class Choice
{
    public Message Message { get; set; } = new Message();
}

public class Message
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}