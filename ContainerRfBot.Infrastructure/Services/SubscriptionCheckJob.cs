using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ContainerRfBot.Core.Interfaces;

namespace ContainerRfBot.Infrastructure.Services;

public class SubscriptionCheckJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionCheckJob> _logger;

    public SubscriptionCheckJob(IServiceProvider serviceProvider, ILogger<SubscriptionCheckJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // Check for 3 days
                await CheckAndNotifyAsync(userRepository, notificationService, TimeSpan.FromDays(3), 3, stoppingToken);

                // Check for 1 day
                await CheckAndNotifyAsync(userRepository, notificationService, TimeSpan.FromDays(1), 1, stoppingToken);

                // Wait for the next day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during subscription check job.");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Wait a bit before retrying on error
            }
        }
    }

    private async Task CheckAndNotifyAsync(IUserRepository userRepository, INotificationService notificationService, TimeSpan timeToExpiration, int daysLeft, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetUsersWithExpiringSubscriptionAsync(timeToExpiration, cancellationToken);
        
        foreach (var user in users)
        {
            try
            {
                await notificationService.SendSubscriptionExpiringNotificationAsync(user.Id, daysLeft, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", user.Id);
            }
        }
    }
}