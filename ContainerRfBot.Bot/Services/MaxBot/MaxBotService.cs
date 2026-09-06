using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Interfaces;
using Max.Bot;
using Max.Bot.Types;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.Bot.Services.MaxBot;

public class MaxBotService
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly MaxClient _maxBotClient;
    private readonly ILogger<MaxBotService> _logger;

    public MaxBotService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        MaxClient maxBotClient,
        ILogger<MaxBotService> logger)
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _maxBotClient = maxBotClient;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message)
                return;

            var maxUserId = message.Sender?.Id;
            var text = message.Text ?? string.Empty;

            if (maxUserId == null) return;

            var userId = await SaveOrUpdateUserAsync(message.Sender!, cancellationToken);

            await HandleMessage(userId, text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    private async Task<long> SaveOrUpdateUserAsync(Max.Bot.Types.User sender, CancellationToken cancellationToken)
    {
        var dbUser = await _userRepository.GetByIdAsync(sender.Id, cancellationToken);

        if (dbUser == null)
        {
            dbUser = new Core.Entities.User
            {
                Id = sender.Id,
                IsAdmin = false,
                HasSubscription = false
            };
            await _userRepository.AddAsync(dbUser, cancellationToken);
        }

        return dbUser.Id;
    }

    private async Task HandleMessage(long userId, string text, CancellationToken cancellationToken)
    {
        var message = new Core.Entities.Message
        {
            UserId = userId,
            Content = text,
            SentAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message, cancellationToken);
    }
}