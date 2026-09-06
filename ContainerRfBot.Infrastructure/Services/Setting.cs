using ContainerRfBot.Core.Interfaces.Settings;
using ContainerRfBot.Core.Interfaces.Settings.Models;
using Microsoft.Extensions.Configuration;

namespace ContainerRfBot.Infrastructure.Services;

public class Setting : ISetting
{
    public BotConfiguration BotConfiguration { get; set; }
    public YooKassaConfiguration YooKassaConfiguration { get; set; }

    public Setting(IConfiguration configuration)
    {
        BotConfiguration = configuration.GetSection("BotConfiguration").Get<BotConfiguration>() 
                           ?? throw new Exception("Не заданы настройки BotConfiguration");
                           
        YooKassaConfiguration = configuration.GetSection("YooKassaConfiguration").Get<YooKassaConfiguration>() 
                                ?? throw new Exception("Не заданы настройки YooKassaConfiguration");
    }
}