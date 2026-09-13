using ContainerRfBot.Bot.Services;
using ContainerRfBot.Bot.Services.MaxBot;
using ContainerRfBot.Bot.AiTunnelService;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Interfaces.Settings;
using Max.Bot;
using Max.Bot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.Bot;

public static class Startup
{
    public static IServiceCollection AddBotLayer(this IServiceCollection services)
    {
        services.AddSingleton<MaxClient>(provider =>
        {
            var settings = provider.GetRequiredService<ISetting>();
            var logger = provider.GetRequiredService<ILogger<MaxClient>>();
            var token = settings.BotConfiguration.MaxToken;
            
            logger.LogInformation("Initializing MaxClient with token: {TokenPrefix}...", token?.Substring(0, Math.Min(token?.Length ?? 0, 5)));
            
            return new MaxClient(new MaxBotOptions
            {
                Token = token
            });
        });

        services.AddScoped<MaxBotService>();
        services.AddScoped<INotificationService, MaxNotificationService>();
        services.AddScoped<IAiTunnelClient, AiTunnelClient>();

        return services;
    }
}