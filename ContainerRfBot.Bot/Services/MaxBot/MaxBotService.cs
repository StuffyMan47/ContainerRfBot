using System.Text.Json;
using ContainerRfBot.Bot.AiTunnelService;
using ContainerRfBot.Bot.AiTunnelService.Model;
using ContainerRfBot.Bot.Helper;
using ContainerRfBot.Bot.Services.Dto;
using ContainerRfBot.Bot.Services.SitePosting;
using ContainerRfBot.Core.Models;
using ContainerRfBot.Core.Models.Containers;
using ContainerRfBot.Core.Enums;
using ContainerRfBot.Core.Enums.SiteEnums;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Interfaces.Settings.Models;
using Max.Bot;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;

namespace ContainerRfBot.Bot.Services.MaxBot;

public class MaxBotService
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IContainerRepository _containerRepository;
    private readonly ISitePostingService _sitePostingService;
    private readonly MaxClient _maxBotClient;
    private readonly IAiTunnelClient _aiTunnelClient;
    private readonly IYooKassaService _yooKassaService;
    private readonly ILogger<MaxBotService> _logger;
    private readonly BotConfiguration _botConfiguration;

    public MaxBotService(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IContainerRepository containerRepository,
        ISitePostingService sitePostingService,
        MaxClient maxBotClient,
        IAiTunnelClient aiTunnelClient,
        IYooKassaService yooKassaService,
        ILogger<MaxBotService> logger,
        IOptions<BotConfiguration> options)
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _containerRepository = containerRepository;
        _sitePostingService = sitePostingService;
        _maxBotClient = maxBotClient;
        _aiTunnelClient = aiTunnelClient;
        _yooKassaService = yooKassaService;
        _logger = logger;
        _botConfiguration = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleUpdateAsync started. Has Callback: {HasCallback}, Has Message: {HasMessage}", update.Callback != null, update.Message != null);
        try
        {
            if (update.Callback != null)
            {
                _logger.LogInformation("Processing Callback: {Payload}", update.Callback.Payload);
                await HandleCallbackAsync(update.Callback, cancellationToken);
                return;
            }

            if (update.Message is not { } message)
            {
                _logger.LogWarning("Update has no Message and no Callback.");
                return;
            }

            var maxUserId = message.Sender?.Id;
            var text = message.Text ?? string.Empty;
            
            _logger.LogInformation("Processing Message from {UserId}: {Text}", maxUserId, text);

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
                await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id, "Номер телефона успешно обновлен!", cancellationToken: cancellationToken);
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
                        },
                        new[]
                        {
                            new InlineKeyboardButton
                            {
                                Text = "Назад",
                                Type = ButtonType.Callback,
                                Payload = "back_to_main"
                            }
                        }
                    }
                };

                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id,
                    profileMsg,
                    keyboard: inlineKb,
                    cancellationToken: cancellationToken);
                break;

                        case "instruction":
                var instrKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Назад", Type = ButtonType.Callback, Payload = "back_to_main" } }
                    }
                };
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id, 
                    "Здесь будет инструкция по использованию сервиса...", 
                    keyboard: instrKb,
                    cancellationToken: cancellationToken);
                break;
                
            case "buy_subscription":
                var paymentUrl = await _yooKassaService.CreatePaymentAsync(
                    user.Id, 
                    300m, // Пример суммы
                    "Оплата подписки на 30 дней", 
                    "https://web.max.ru/402213961", // URL для возврата после оплаты (замените на нужный deeplink или сайт)
                    cancellationToken);
                
                if (string.IsNullOrEmpty(paymentUrl))
                {
                    await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id, "Произошла ошибка при создании ссылки на оплату. Попробуйте позже.", cancellationToken: cancellationToken);
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
                    await _maxBotClient.Messages.SendMessageToUserAsync(
                        userId: user.Id,
                        "Для оплаты подписки перейдите по ссылке ниже:",
                        keyboard: payKb,
                        cancellationToken: cancellationToken);
                }
                break;

                        case "create_ad":
                user.State = BotState.WaitingForAdDetails;
                await _userRepository.UpdateAsync(user, cancellationToken);
                
                var cancelAdKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Отмена", Type = ButtonType.Callback, Payload = "back_to_main" } }
                    }
                };
                
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id, 
                    "Пожалуйста, напишите характеристики контейнера (тип, цена, город продажи, состояние и т.д.):", 
                    keyboard: cancelAdKb,
                    cancellationToken: cancellationToken);
                break;

                        case "change_phone":
                user.State = BotState.WaitingForPhone;
                await _userRepository.UpdateAsync(user, cancellationToken);
                
                var cancelPhoneKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Отмена", Type = ButtonType.Callback, Payload = "back_to_main" } }
                    }
                };
                
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id, 
                    "Пожалуйста, введите ваш новый номер телефона:", 
                    keyboard: cancelPhoneKb,
                    cancellationToken: cancellationToken);
                break;
                
                        case "back_to_main":
                if (user.State != BotState.None)
                {
                    user.State = BotState.None;
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }
                await SendMainMenuAsync(user, cancellationToken);
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
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id,
                    "Выберите пользователя для подтверждения оплаты:",
                    keyboard: usersKb,
                    cancellationToken: cancellationToken);
                break;
                
            case "admin_cancel":
                await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id, "Действие отменено.", cancellationToken: cancellationToken);
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
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id,
                    $"Вы уверены, что хотите выдать подписку пользователю {targetId} на 30 дней?",
                    keyboard: confirmKb,
                    cancellationToken: cancellationToken);
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
                    await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id, $"Подписка пользователю {targetId} успешно выдана на 30 дней.", cancellationToken: cancellationToken);
                    await _maxBotClient.Messages.SendMessageToUserAsync(userId: targetId, "Администратор подтвердил вашу оплату! Подписка продлена на 30 дней.", cancellationToken: cancellationToken);
                }
            }
        }
    }

    private async Task SendMainMenuAsync(Core.Models.UserModel user, CancellationToken cancellationToken)
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

        await _maxBotClient.Messages.SendMessageToUserAsync(
            userId: user.Id,
            "Главное меню",
            keyboard: inlineKb,
            cancellationToken: cancellationToken);
    }

    private async Task<Core.Models.UserModel> SaveOrUpdateUserAsync(Max.Bot.Types.User sender, CancellationToken cancellationToken)
    {
        var dbUser = await _userRepository.GetByIdAsync(sender.Id, cancellationToken);

        if (dbUser == null)
        {
            dbUser = new Core.Models.UserModel
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

    private async Task HandleMessage(Core.Models.UserModel user, string text, CancellationToken cancellationToken)
    {
        var message = new Core.Models.MessageModel
        {
            UserId = user.Id,
            Content = text,
            SentAt = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(message, cancellationToken);
    }

    private async Task HandleCreateAdMessage(Core.Models.UserModel user, Update update, Max.Bot.Types.Message message, CancellationToken cancellationToken)
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
            await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id, "Произошла ошибка при обработке вашего сообщения.", cancellationToken: cancellationToken);
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

        try
        {
            if (objects != null && objects.Count > 0)
            {
                List<ContainerRequestModel> containers = [];
                foreach (var x in objects)
                {
                    var sourceId = Guid.NewGuid();
                    var articleId = GenerateArticleId();
                    var city = CityGeoService.GetCityCoordinatesAsync(x.City);
                    var container = new ContainerRequestModel
                    {
                        SourceId = sourceId,
                        ArticleId = articleId,
                        ConditionId = x.ConditionId ?? ConditionEnum.Cw,
                        CategoryId = x.CategoryId,
                        Availability = x.Availability,
                        City = x.City,
                        Latitude = city.Latitude.Value,
                        Longitude = city.Longitude.Value,
                        Date = DateTimeOffset.UtcNow,
                        Condition = x.ConditionName,
                        Currency = x.Currency.GetDescription(),
                        PriceWithoutTax = x.PriceWithoutTax,
                        PriceWithTax = x.PriceWithTax,
                        Size = x.Size,
                        Type = x.Type,
                        Count = x.Count,
                        Username = $"@{message.Sender?.Username ?? message.Sender?.FirstName}",
                        CurrencyId = x.Currency,
                        MessageUrl = ""
                    };
                    containers.Add(container);
                }
                var userInfo = await _userRepository.GetByIdAsync(message.Sender.Id, cancellationToken);
                var userDbId = userInfo?.Id ?? user.Id;
                var userPhone = userInfo?.PhoneNumber ?? user.PhoneNumber ?? "";

                // write to db
                await _containerRepository.CreateContainerList(containers.Select(x => new CreateContainerListRequest
                {
                    Id = x.SourceId,
                    ArticleId = x.ArticleId,
                    CategoryId = x.CategoryId,
                    Quantity = x.Count,
                    Condition = x.ConditionId,
                    Username = x.Username,
                    PriceType = x.PriceWithTax.HasValue ? PriceType.WithTax : PriceType.WithoutTax,
                    Price = x.PriceWithTax.HasValue ? x.PriceWithTax.Value : x.PriceWithoutTax.Value,
                    Currency = x.CurrencyId,
                    Address = x.City,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    UserId = userDbId,
                    MessageId = x.MessageUrl
                }).ToList(), cancellationToken);
                
                await WriteToGoogleSheets(containers, MessengerType.Max);
                
                await _sitePostingService.SendContainersToSite(containers);
                DateTime moscowTime = DateTime.UtcNow.AddHours(3);

                if (moscowTime.Hour >= 9 && moscowTime.Hour < 18)
                {
                    foreach (var container in containers)
                    {
                        var description = DescriptionHelper.GenerateDescription(
                            container.ConditionId,
                            container.CurrencyId,
                            container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                            container.PriceWithoutTax.HasValue
                                ? container.PriceWithoutTax.Value * (decimal)1.1
                                : container.PriceWithTax.Value * (decimal)1.1,
                            container.City,
                            container.CategoryId);
                        await SendMessageToChanel(description,
                            $"https://xn--e1aalcpcdvnp.xn--p1ai/?artnumber={container.ArticleId}",
                            userPhone);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _maxBotClient.Messages.SendMessageAsync(
                244266512,
                "Ошибка при записи данных в excel таблицу:" +
                $"{ex.Message}\n" +
                $"username: {message?.Sender?.Name}\n" +
                $"message: {message?.Text}",
                cancellationToken: cancellationToken);
            throw;
        }
        // Успешно распарсили. Сбрасываем стейт и продолжаем логику.
        user.State = BotState.None;
        await _userRepository.UpdateAsync(user, cancellationToken);
        
        await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id, "Объявление успешно создано! (заглушка)", cancellationToken: cancellationToken);

        // Record the message
        await HandleMessage(user, message.Text ?? "", cancellationToken);
    }
    
    private async Task WriteToGoogleSheets(List<ContainerRequestModel> models, MessengerType messengerType)
    {
        var credential = GoogleCredential.FromJson(_botConfiguration.GoogleAuth.Key)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        // Пример инициализации сервиса (в конструкторе)
        var sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential, // Ваше GoogleCredential
            ApplicationName = "ContainerFather.Bot",
        });
        var spreadsheetId = "1Q4aHnNPNFXxlwTxRNJk9IUf1m6V2wWV1HTc3rnu-ZbE"; // ID таблицы из URL
        // Проверка входных данных
        if (models == null || models.Count == 0)
        {
            Console.WriteLine("Попытка записи пустого списка данных в Google Таблицу");
            return;
        }

        try
        {
            var currentWeekStart = GetWeekStartMonday(DateTime.UtcNow);
            var sheetName = GetWeekSheetName(currentWeekStart);
            
            await EnsureSheetWithHeadersAsync(sheetsService, spreadsheetId, sheetName);
            
            // Преобразуем модели в данные для Google Sheets
            var values = new List<IList<object>>();

            foreach (var model in models)
            {
                // Безопасное форматирование даты с обработкой null
                string formattedDate = DateTimeOffset.UtcNow.ToString("dd.MM.yyyy HH:mm");

                var row = new List<object>
                {
                    model.ArticleId.ToString(),
                    model.Size,
                    model.Type,
                    model.Condition ?? string.Empty,
                    model.City,
                    formattedDate,
                    model.Username?.Trim() ?? string.Empty,
                    model.Availability ?? string.Empty,
                    model.PriceWithTax.HasValue
                        ? model.PriceWithTax.Value
                        : string.Empty, // Форматирование цены как валюты
                    model.PriceWithoutTax.HasValue ? model.PriceWithoutTax.Value : string.Empty,
                    model.Currency,
                    model.Count,
                    messengerType == MessengerType.Max ? "Max" : "Telegram",
                };
                values.Add(row);
            }

            // Динамическое определение диапазона
            var range = $"{sheetName}!A:M";

            var valueRange = new ValueRange
            {
                Values = values
            };

            // 3. Используем Append вместо Update
            var appendRequest = sheetsService.Spreadsheets.Values.Append(
                valueRange,
                spreadsheetId,
                range
            );

            // Важные настройки для корректной работы
            appendRequest.ValueInputOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            appendRequest.InsertDataOption =
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum
                    .INSERTROWS; // ← Добавлять новые строки
            appendRequest.IncludeValuesInResponse = true; // Для получения информации о результате

            // Выполнение запроса с таймаутом
            var response = await appendRequest.ExecuteAsync();

            Console.WriteLine($"Успешно записано {response.Updates.UpdatedRows} строк в Google Таблицу");
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 403)
        {
            Console.WriteLine("Ошибка доступа к Google Таблице: Проверьте права доступа сервисного аккаунта");
            throw new ApplicationException("Недостаточно прав для записи в таблицу", ex);
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            Console.WriteLine($"Таблица с ID {spreadsheetId} не найдена");
            throw new ApplicationException("Целевая таблица не существует", ex);
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine("Таймаут операции записи в Google Таблицу");
            throw new TimeoutException("Превышено время ожидания ответа от Google API", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка при записи в Google Таблицу: {ex.Message}");
            throw;
        }
    }
    
    private async Task EnsureSheetWithHeadersAsync(SheetsService service, string spreadsheetId, string sheetName)
    {
        // Получаем список существующих листов
        var getSpreadsheetRequest = service.Spreadsheets.Get(spreadsheetId);
        getSpreadsheetRequest.Fields = "sheets(properties.title)";
        var spreadsheet = await getSpreadsheetRequest.ExecuteAsync();
    
        var sheetExists = spreadsheet.Sheets?.Any(s => 
            s.Properties?.Title?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true) == true;

        if (!sheetExists)
        {
            Console.WriteLine($"🆕 Создаю новый лист: '{sheetName}'");
        
            // Создаём новый лист
            var batchRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties
                            {
                                Title = sheetName,
                                GridProperties = new GridProperties { RowCount = 5000, ColumnCount = 13 }
                            }
                        }
                    },
                    new Request
                    {
                        SetDataValidation = new SetDataValidationRequest
                        {
                            Range = new GridRange
                            {
                                SheetId = null, // Будет установлено после создания листа
                                StartRowIndex = 1,      // Строка 2 (0-based)
                                EndRowIndex = 5000,     // До строки 5000
                                StartColumnIndex = 1,  // Колонка A
                                EndColumnIndex = 13 // До конца колонки
                            },
                        }
                    }
                }
            };
        
            await service.Spreadsheets.BatchUpdate(batchRequest, spreadsheetId).ExecuteAsync();
        
            // Записываем заголовки в первую строку
            var headers = new List<IList<object>>
            {
                new List<object> 
                { 
                   "Артикул", "Размер", "Тип", "Состояние", "Город", "Дата", 
                    "Продавец", "Наличие", "Цена с НДС", "Цена без НДС", 
                    "Валюта", "Количество", "Источник", "Сообщение"
                }
            };
        
            var headerRange = new ValueRange { Values = headers };
            var headerRequest = service.Spreadsheets.Values.Update(headerRange, spreadsheetId, $"{sheetName}!A1:N1");
            headerRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await headerRequest.ExecuteAsync();
        }
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
    
    private static DateTime GetWeekStartMonday(DateTime date)
    {
        // Вычисляем, сколько дней нужно отнять, чтобы попасть в понедельник
        int daysToMonday = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-daysToMonday).Date;
    }

    // 🏷️ Формирование имени листа: "24.03.25-30.03.25"
    // Формат безопасен для Google Sheets (без запрещённых символов \ / ? * [ ])
    private static string GetWeekSheetName(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        return $"{weekStart:dd.MM.yy}-{weekEnd:dd.MM.yy}";
    }
    
    private static long GenerateArticleId()
    {
        // Generates an 8-9 digit number using time components and a small random part
        var now = DateTime.UtcNow;
        var timePart = (now.DayOfYear * 100000) + (now.Hour * 1000) + (now.Minute * 10) + (now.Second % 10);
        var randomPart = new Random().Next(10, 99);
        return long.Parse($"{timePart}{randomPart}");
    }
    
    public async Task SendMessageToChanel(string text, string url, string phoneNumber)
    {
        await _maxBotClient.Messages.SendMessageAsync(new SendMessageRequest()
        {
            Text = $"{text}\nСсылка: {url}\nНомер телефона: {phoneNumber}"
        }, -72880335247520);
    }
}