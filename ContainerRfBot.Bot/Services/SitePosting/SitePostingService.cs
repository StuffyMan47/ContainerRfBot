using System.Globalization;
using ContainerRfBot.Bot.Helper;
using ContainerRfBot.Bot.Services.Dto;
using ContainerRfBot.Bot.Services.SiteService;
using ContainerRfBot.Bot.Services.SiteService.Model;
using ContainerRfBot.Core.Enums.SiteEnums;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Interfaces.Settings.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Max.Bot;
using Max.Bot.Types.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContainerRfBot.Bot.Services.SitePosting;

public class SitePostingService : ISitePostingService
{
    private readonly BotConfiguration _botConfiguration;
    private readonly MaxClient _maxClient;
    private readonly ISiteClient _siteClient;
    private readonly IContainerRepository _containerRepository;
    private readonly ILogger<SitePostingService> _logger;
    
    public SitePostingService(
        ISiteClient siteClient,
        IContainerRepository containerRepository,
        MaxClient maxClient,
        IOptions<BotConfiguration> options,
        ILogger<SitePostingService> logger)
    {
        _botConfiguration = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _siteClient = siteClient ?? throw new ArgumentNullException(nameof(siteClient));
        _containerRepository = containerRepository ?? throw new ArgumentNullException(nameof(containerRepository));
        _maxClient = maxClient ?? throw new ArgumentNullException(nameof(maxClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task SendConfirmToAdmin()
    {
        var adminId = 244266512L;
        var currentDate = DateTimeOffset.UtcNow.ToString("dd.MM.yyyy HH:mm");
        var messageText = $"Пора отметить чекбоксы в гугл таблице. Текущее время: {currentDate}";
        
        var inlineKeyboard = new Max.Bot.Types.InlineKeyboard
        {
            Buttons = new[]
            {
                new[]
                {
                    new Max.Bot.Types.InlineKeyboardButton
                    {
                        Text = "Отправить выбранные предложения на сайт",
                        Type = ButtonType.Callback,
                        Payload = $"post_to_site_{currentDate}"
                    }
                }
            }
        };

        await _maxClient.Messages.SendMessageToUserAsync(
            userId: adminId,
            text: messageText,
            keyboard: inlineKeyboard);
    }

    public async Task ReadGoogleTable(DateTimeOffset date)
    {
        if (_botConfiguration.GoogleAuth?.Key == null)
        {
            _logger.LogWarning("GoogleAuth key is not configured.");
            return;
        }

        var credential = GoogleCredential.FromJson(_botConfiguration.GoogleAuth.Key)
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        var sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ContainerRfBot.Bot",
        });

        var spreadsheetId = "1Q4aHnNPNFXxlwTxRNJk9IUf1m6V2wWV1HTc3rnu-ZbE";

        try
        {
            var spreadsheet = await sheetsService.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            if (spreadsheet.Sheets == null || spreadsheet.Sheets.Count == 0)
            {
                return;
            }

            var lastSheetTitle = spreadsheet.Sheets.Last().Properties.Title;
            var range = $"{lastSheetTitle}!A:M";

            var request = sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();

            if (response.Values == null || response.Values.Count == 0)
            {
                return;
            }

            var selectedIds = new List<long>();

            foreach (var row in response.Values.Skip(1))
            {
                if (row.Count > 11)
                {
                    var idStr = row[0]?.ToString();
                    var dateStr = row[5]?.ToString();
                    var isCheckedStr = row[12]?.ToString();
                    
                    string[] formats = [
                        "dd.MM.yyyy H:mm:ss",
                        "dd.MM.yyyy HH:mm:ss", 
                        "dd.MM.yyyy H:mm",
                        "dd.MM.yyyy HH:mm"
                    ];
                    
                    DateTime.TryParseExact(dateStr, formats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var rowDate);
                    if (long.TryParse(idStr, out var id) 
                        && !string.IsNullOrEmpty(dateStr) 
                        && rowDate != default 
                        && rowDate.Date == date.Date 
                        && (isCheckedStr?.Equals("TRUE", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        selectedIds.Add(id);
                    }
                }
            }

            if (selectedIds.Count > 0)
            {
                var containerResponses = await _containerRepository.GetContainerList(selectedIds, null, CancellationToken.None);
                
                var containersToPost = containerResponses.Select(c => new ContainerRequestModel
                {
                    SourceId = c.Id,
                    ArticleId = c.ArticleId,
                    ConditionId = c.Condition,
                    City = c.Address,
                    Longitude = c.Longitude,
                    Latitude = c.Latitude, 
                    CurrencyId = c.Currency,
                    Currency = c.Currency.ToString(),
                    Count = c.Quantity,
                    PriceWithoutTax = c.PriceType == PriceType.WithoutTax ? c.Price : null,
                    PriceWithTax = c.PriceType == PriceType.WithTax ? c.Price : null,
                    Username = c.Username,
                    CategoryId = c.CategoryId,
                    Size = c.CategoryId.ToString(),
                    Type = c.CategoryId.ToString(),
                    MessageUrl = ""
                }).ToList();

                await SendContainersToSite(containersToPost);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не получилось прочитать Google таблицу");
            try
            {
                await _maxClient.Messages.SendMessageToUserAsync(
                    userId: 244266512L,
                    text: "Не получилось прочитать Google таблицу: " + ex.Message);
            }
            catch { }
        }
    }

    public async Task SendContainersToSite(List<ContainerRequestModel> containers)
    {
        try
        {
            List<SendContainersInfoRequest> requests = [];
            foreach (var container in containers)
            {
                var city = CityGeoService.GetCityCoordinatesAsync(container.City);
                var description = DescriptionHelper.GenerateDescription(
                    container.ConditionId, 
                    container.CurrencyId,
                    container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                    container.PriceWithoutTax.HasValue ? container.PriceWithoutTax.Value * (decimal)1.1 : container.PriceWithTax.Value * (decimal)1.1,
                    container.City,
                    container.CategoryId);
                
                var request = new SendContainersInfoRequest
                {
                    SourceId = container.ArticleId.ToString(),
                    Condition = container.ConditionId,
                    Address = container.City,
                    Currency = container.CurrencyId,
                    Quantity = container.Count,
                    PhoneNumber = null,
                    PriceType = container.PriceWithoutTax.HasValue ? PriceType.WithoutTax : PriceType.WithTax,
                    Price = container.PriceWithoutTax.HasValue ? container.PriceWithoutTax.Value * (decimal)1.1 : container.PriceWithTax.Value * (decimal)1.1,
                    Location = new LocationDetails()
                    {
                        Latitude = city.Latitude.Value,
                        Longitude = city.Longitude.Value,
                    },
                    Username = container.Username,
                    CategoryId = container.CategoryId,
                    Description = description,
                };
                requests.Add(request);
            }
            var result = await _siteClient.SendContainersInfo(requests, CancellationToken.None);
            
            _logger.LogInformation("Результат отправки объявлений на сайт: {Result}. Ошибки: {Errors}", result.Result, string.Join(", ", result.Errors));

            try
            {
                await _maxClient.Messages.SendMessageToUserAsync(
                    userId: 244266512L,
                    text: $"Результат отправки объявлений на сайт {result.Result}\n" +
                          $"Выложены записи с артикулами {string.Join(", ", result.Created)}\n" +
                          $"Ошибки: {string.Join(", ", result.Errors)}");
            }
            catch { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не получилось отправить контейнеры на сайт");
            try
            {
                await _maxClient.Messages.SendMessageToUserAsync(
                    userId: 244266512L,
                    text: "Не получилось отправить контейнеры на сайт \n" + ex.Message);
            }
            catch { }
        }
    }
}
