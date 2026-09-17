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
