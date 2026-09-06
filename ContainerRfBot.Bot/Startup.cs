using ContainerRfBot.Bot.Services;
using ContainerRfBot.Bot.Services.MaxBot;
using ContainerRfBot.Bot.AiTunnelService;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Interfaces.Settings;
using Max.Bot;
using Max.Bot.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerRfBot.Bot;

public static class Startup
{
    public static IServiceCollection AddBotLayer(this IServiceCollection services)
    {
        services.AddSingleton<MaxClient>(provider =>
        {
            var settings = provider.GetRequiredService<ISetting>();
            return new MaxClient(new MaxBotOptions
            {
                Token = settings.BotConfiguration.MaxToken
            });
        });

        services.AddScoped<MaxBotService>();
        services.AddScoped<INotificationService, MaxNotificationService>();
        services.AddScoped<IAiTunnelClient, AiTunnelClient>();

        return services;
    }
}