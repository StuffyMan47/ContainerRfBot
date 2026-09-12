using System.Text.Json;
using ContainerRfBot.Bot.AiTunnelService;
using ContainerRfBot.Bot.AiTunnelService.Model;
using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Enums;
using ContainerRfBot.Core.Interfaces;
using Max.Bot;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.Bot.Services.MaxBot;

public class MaxBotService
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly MaxClient _maxBotClient;
    private readonly IAiTunnelClient _aiTunnelClient;
    private readonly IYooKassaService _yooKassaService;
    private readonly ILogger<MaxBotService> _logger;

    public MaxBotService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        MaxClient maxBotClient,
        IAiTunnelClient aiTunnelClient,
        IYooKassaService yooKassaService,
        ILogger<MaxBotService> logger)
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _maxBotClient = maxBotClient;
        _aiTunnelClient = aiTunnelClient;
        _yooKassaService = yooKassaService;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Callback != null)
            {
                await HandleCallbackAsync(update.Callback, cancellationToken);
                return;
            }

            if (update.Message is not { } message)
                return;

            var maxUserId = message.Sender?.Id;
            var text = message.Text ?? string.Empty;

            if (maxUserId == null) return;

            var user = await SaveOrUpdateUserAsync(message.Sender!, cancellationToken);

            if (text == "/start")
            {
                await SendMainMenuAsync(user, cancellationToken);
                return;
            }

            // Route based on state
            if (user.State == BotState.WaitingForPhone)
            {
                user.PhoneNumber = text;
                user.State = BotState.None;
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _maxBotClient.Messages.SendMessageAsync(user.Id, "Номер телефона успешно обновлен!", cancellationToken: cancellationToken);
                await SendMainMenuAsync(user, cancellationToken);
                return;
            }
            
            if (user.State == BotState.WaitingForAdDetails)
            {
                await HandleCreateAdMessage(user, update, message, cancellationToken);
                return;
            }

            // Fallback
            await HandleMessage(user, text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
        }
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        if (callback.User == null) return;

        var user = await SaveOrUpdateUserAsync(callback.User, cancellationToken);
        var payload = callback.Payload;

        switch (payload)
        {
            case "profile":
                var subText = user.HasSubscription && user.SubscriptionExpirationDate.HasValue 
                    ? $"до {user.SubscriptionExpirationDate.Value:dd.MM.yyyy}" 
                    : "отсутствует";
                var phoneText = string.IsNullOrEmpty(user.PhoneNumber) ? "не указан" : user.PhoneNumber;
                var profileMsg = $"Профиль пользователя\nПодписка: {subText}\nНомер телефона: {phoneText}";

                var inlineKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[]
                        {
                            new InlineKeyboardButton
                            {
                                Text = "Изменить номер телефона",
                                Type = ButtonType.Callback,
                                Payload = "change_phone"
                            }
                        }
                    }
                };

                await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest
                {
                    Text = profileMsg,
                    Attachments = new AttachmentRequest[] 
                    {
                        new AttachmentRequest { Type = "inline_keyboard", Payload = new Dictionary<string, object> { { "buttons", inlineKb.Buttons } } }
                    }
                }, user.Id, cancellationToken: cancellationToken);
                break;

            case "instruction":
                await _maxBotClient.Messages.SendMessageAsync(user.Id, "Здесь будет инструкция по использованию сервиса...", cancellationToken: cancellationToken);
                break;
                
            case "buy_subscription":
                var paymentUrl = await _yooKassaService.CreatePaymentAsync(
                    user.Id, 
                    1000m, // Пример суммы
                    "Оплата подписки на 30 дней", 
                    "https://max.ru/", // URL для возврата после оплаты (замените на нужный deeplink или сайт)
                    cancellationToken);
                
                if (string.IsNullOrEmpty(paymentUrl))
                {
                    await _maxBotClient.Messages.SendMessageAsync(user.Id, "Произошла ошибка при создании ссылки на оплату. Попробуйте позже.", cancellationToken: cancellationToken);
                }
                else
                {
                    var payKb = new InlineKeyboard
                    {
                        Buttons = new[]
                        {
                            new[] { new InlineKeyboardButton { Text = "Перейти к оплате", Type = ButtonType.Link, Url = paymentUrl } }
                        }
                    };
                    await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest
                    {
                        Text = "Для оплаты подписки перейдите по ссылке ниже:",
                        Attachments = new AttachmentRequest[] { new AttachmentRequest { Type = "inline_keyboard", Payload = new Dictionary<string, object> { { "buttons", payKb.Buttons } } } }
                    }, user.Id, cancellationToken: cancellationToken);
                }
                break;

            case "create_ad":
                user.State = BotState.WaitingForAdDetails;
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _maxBotClient.Messages.SendMessageAsync(user.Id, "Пожалуйста, напишите характеристики контейнера (тип, цена, город продажи, состояние и т.д.):", cancellationToken: cancellationToken);
                break;

            case "change_phone":
                user.State = BotState.WaitingForPhone;
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _maxBotClient.Messages.SendMessageAsync(user.Id, "Пожалуйста, введите ваш новый номер телефона:", cancellationToken: cancellationToken);
                break;
                
            case "admin_confirm_payment":
                if (!user.IsAdmin) break;
                var allUsers = await _userRepository.GetAllUsersAsync(cancellationToken);
                var userButtons = new List<InlineKeyboardButton[]>();
                foreach (var u in allUsers)
                {
                    var subInfo = u.HasSubscription ? "(с подпиской)" : "(без подписки)";
                    userButtons.Add(new[] { new InlineKeyboardButton { Text = $"User {u.Id} {subInfo}", Type = ButtonType.Callback, Payload = $"admin_select_user_{u.Id}" } });
                }
                var usersKb = new InlineKeyboard { Buttons = userButtons.ToArray() };
                await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest
                {
                    Text = "Выберите пользователя для подтверждения оплаты:",
                    Attachments = new AttachmentRequest[] { new AttachmentRequest { Type = "inline_keyboard", Payload = new Dictionary<string, object> { { "buttons", usersKb.Buttons } } } }
                }, user.Id, cancellationToken: cancellationToken);
                break;
                
            case "admin_cancel":
                await _maxBotClient.Messages.SendMessageAsync(user.Id, "Действие отменено.", cancellationToken: cancellationToken);
                break;
        }

        if (payload?.StartsWith("admin_select_user_") == true)
        {
            if (!user.IsAdmin) return;
            var targetUserIdStr = payload.Replace("admin_select_user_", "");
            if (long.TryParse(targetUserIdStr, out var targetId))
            {
                var confirmKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Да", Type = ButtonType.Callback, Payload = $"admin_grant_sub_{targetId}" } },
                        new[] { new InlineKeyboardButton { Text = "Нет", Type = ButtonType.Callback, Payload = "admin_cancel" } }
                    }
                };
                await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest
                {
                    Text = $"Вы уверены, что хотите выдать подписку пользователю {targetId} на 30 дней?",
                    Attachments = new AttachmentRequest[] { new AttachmentRequest { Type = "inline_keyboard", Payload = new Dictionary<string, object> { { "buttons", confirmKb.Buttons } } } }
                }, user.Id, cancellationToken: cancellationToken);
            }
        }
        else if (payload?.StartsWith("admin_grant_sub_") == true)
        {
            if (!user.IsAdmin) return;
            var targetUserIdStr = payload.Replace("admin_grant_sub_", "");
            if (long.TryParse(targetUserIdStr, out var targetId))
            {
                var targetUser = await _userRepository.GetByIdAsync(targetId, cancellationToken);
                if (targetUser != null)
                {
                    targetUser.HasSubscription = true;
                    targetUser.SubscriptionExpirationDate = DateTime.UtcNow.AddDays(30);
                    await _userRepository.UpdateAsync(targetUser, cancellationToken);
                    await _maxBotClient.Messages.SendMessageAsync(user.Id, $"Подписка пользователю {targetId} успешно выдана на 30 дней.", cancellationToken: cancellationToken);
                    await _maxBotClient.Messages.SendMessageAsync(targetId, "Администратор подтвердил вашу оплату! Подписка продлена на 30 дней.", cancellationToken: cancellationToken);
                }
            }
        }
    }

    private async Task SendMainMenuAsync(Core.Entities.User user, CancellationToken cancellationToken)
    {
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[] { new InlineKeyboardButton { Text = "Профиль", Type = ButtonType.Callback, Payload = "profile" } },
            new[] { new InlineKeyboardButton { Text = "Оплатить подписку", Type = ButtonType.Callback, Payload = "buy_subscription" } },
            new[] { new InlineKeyboardButton { Text = "Инструкция", Type = ButtonType.Callback, Payload = "instruction" } },
            new[] { new InlineKeyboardButton { Text = "Создать объявление о продаже", Type = ButtonType.Callback, Payload = "create_ad" } }
        };

        if (user.IsAdmin)
        {
            buttons.Add(new[] { new InlineKeyboardButton { Text = "Подтвердить оплату (Админ)", Type = ButtonType.Callback, Payload = "admin_confirm_payment" } });
        }

        var inlineKb = new InlineKeyboard
        {
            Buttons = buttons.ToArray()
        };

        await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest
        {
            Text = "Главное меню",
            Attachments = new AttachmentRequest[] 
            {
                new AttachmentRequest { Type = "inline_keyboard", Payload = new Dictionary<string, object> { { "buttons", inlineKb.Buttons } } }
            }
        }, user.Id, cancellationToken: cancellationToken);
    }

    private async Task<Core.Entities.User> SaveOrUpdateUserAsync(Max.Bot.Types.User sender, CancellationToken cancellationToken)
    {
        var dbUser = await _userRepository.GetByIdAsync(sender.Id, cancellationToken);

        if (dbUser == null)
        {
            dbUser = new Core.Entities.User
            {
                Id = sender.Id,
                IsAdmin = false,
                HasSubscription = false,
                State = BotState.None
            };
            await _userRepository.AddAsync(dbUser, cancellationToken);
        }

        return dbUser;
    }

    private async Task HandleMessage(Core.Entities.User user, string text, CancellationToken cancellationToken)
    {
        var message = new Core.Entities.Message
        {
            UserId = user.Id,
            Content = text,
            SentAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message, cancellationToken);
    }

    private async Task HandleCreateAdMessage(Core.Entities.User user, Update update, Max.Bot.Types.Message message, CancellationToken cancellationToken)
    {
        List<AiContainerResponse> objects = new List<AiContainerResponse>();
        string result = null;
        try
        {
            result = await _aiTunnelClient.SendMessage(message.Text ?? string.Empty);
            objects = JsonSerializer.Deserialize<List<AiContainerResponse>>(result) ?? new List<AiContainerResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке сообщения в ai tunnel");
            await _maxBotClient.Messages.SendMessageAsync(user.Id, "Произошла ошибка при обработке вашего сообщения.", cancellationToken: cancellationToken);
            return;
        }

        var allMissingFields = new HashSet<string>();
        bool hasInvalidRecords = false;

        foreach (var obj in objects)
        {
            var missingFields = GetMissingFields(obj);
            if (missingFields.Any())
            {
                hasInvalidRecords = true;
                foreach (var field in missingFields)
                {
                    allMissingFields.Add(field);
                }
            }
        }

        if (hasInvalidRecords)
        {
            var missingFieldsList = string.Join(", ", allMissingFields);
            var responseText = $"Дополните Ваше предложение необходимой информацией ({missingFieldsList}) Так выйдем на сделку быстрее";

            await _maxBotClient.Messages.ReplyToMessageAsync(
                chatId: user.Id,
                messageId: update.Message?.Mid ?? "",
                text: responseText,
                cancellationToken: cancellationToken);
            return;
        }

        // Успешно распарсили. Сбрасываем стейт и продолжаем логику.
        user.State = BotState.None;
        await _userRepository.UpdateAsync(user, cancellationToken);
        
        await _maxBotClient.Messages.SendMessageAsync(user.Id, "Объявление успешно создано! (заглушка)", cancellationToken: cancellationToken);

        // Record the message
        await HandleMessage(user, message.Text ?? "", cancellationToken);
    }

    private List<string> GetMissingFields(AiContainerResponse container)
    {
        var missingFields = new List<string>();

        // Цена
        if ((container.PriceWithTax == null || container.PriceWithTax == 0) &&
            (container.PriceWithoutTax == null || container.PriceWithoutTax == 0))
        {
            missingFields.Add("стоимость контейнера");
        }

        // Size
        if (string.IsNullOrWhiteSpace(container.Size) ||
            container.Size.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            missingFields.Add("размер контейнера");
        }

        // Type
        if (string.IsNullOrWhiteSpace(container.Type) ||
            container.Type.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            missingFields.Add("тип контейнера (HC, DC)");
        }

        // City
        if (string.IsNullOrWhiteSpace(container.City) ||
            container.City.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            missingFields.Add("город");
        }

        return missingFields;
    }
}