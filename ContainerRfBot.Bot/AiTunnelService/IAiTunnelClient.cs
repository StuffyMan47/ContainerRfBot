namespace ContainerRfBot.Bot.AiTunnelService;

public interface IAiTunnelClient
{
    Task<string> SendMessage(string message);
}