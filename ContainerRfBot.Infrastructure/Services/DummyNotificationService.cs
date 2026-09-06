using ContainerRfBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.Infrastructure.Services;

public class DummyNotificationService : INotificationService
{
    private readonly ILogger<DummyNotificationService> _logger;

    public DummyNotificationService(ILogger<DummyNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendSubscriptionExpiringNotificationAsync(long userId, int daysLeft, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dummy notification: User {UserId}'s subscription expires in {DaysLeft} days.", userId, daysLeft);
        return Task.CompletedTask;
    }
}