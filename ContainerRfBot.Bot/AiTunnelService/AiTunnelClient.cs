using ContainerRfBot.Bot.AiTunnelService;

namespace ContainerRfBot.Bot.AiTunnelService;

public class AiTunnelClient : IAiTunnelClient
{
    public Task<string> SendMessage(string message)
    {
        // В реальном проекте здесь будет вызов API (как в ContainerFather)
        return Task.FromResult("[]"); 
    }
}