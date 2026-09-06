namespace ContainerRfBot.Core.Interfaces;

public interface INotificationService
{
    Task SendSubscriptionExpiringNotificationAsync(long userId, int daysLeft, CancellationToken cancellationToken = default);
}