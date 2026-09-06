using ContainerRfBot.Core.Interfaces.Settings.Models;

namespace ContainerRfBot.Core.Interfaces.Settings;

public interface ISetting
{
    public BotConfiguration BotConfiguration { get; set; }
    public YooKassaConfiguration YooKassaConfiguration { get; set; }
}