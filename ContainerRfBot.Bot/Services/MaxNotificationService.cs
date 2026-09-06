using ContainerRfBot.Core.Interfaces;
using Max.Bot;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.Bot.Services;

public class MaxNotificationService : INotificationService
{
    private readonly MaxClient _maxClient;
    private readonly ILogger<MaxNotificationService> _logger;

    public MaxNotificationService(MaxClient maxClient, ILogger<MaxNotificationService> logger)
    {
        _maxClient = maxClient;
        _logger = logger;
    }

    public async Task SendSubscriptionExpiringNotificationAsync(long userId, int daysLeft, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = $"Внимание! Ваша подписка истекает через {daysLeft} дней. Пожалуйста, продлите ее.";
            
            // В Max.Bot обычно отправляется сообщение по userId (или chatId)
            await _maxClient.Messages.SendMessageAsync(userId, text, cancellationToken: cancellationToken);
            _logger.LogInformation("Sent expiration notification to user {UserId}. Days left: {DaysLeft}", userId, daysLeft);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {UserId}", userId);
        }
    }
}