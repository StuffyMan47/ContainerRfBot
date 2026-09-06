using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.Interfaces.Settings;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.Infrastructure.Clients;

public class YooKassaService : IYooKassaService
{
    private readonly HttpClient _httpClient;
    private readonly ISetting _setting;
    private readonly ILogger<YooKassaService> _logger;

    public YooKassaService(HttpClient httpClient, ISetting setting, ILogger<YooKassaService> logger)
    {
        _httpClient = httpClient;
        _setting = setting;
        _logger = logger;
        
        var authBytes = Encoding.ASCII.GetBytes($"{_setting.YooKassaConfiguration.ShopId}:{_setting.YooKassaConfiguration.SecretKey}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
    }

    public async Task<string?> CreatePaymentAsync(long userId, decimal amount, string description, string returnUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var idempotenceKey = Guid.NewGuid().ToString();
            
            var requestBody = new
            {
                amount = new
                {
                    value = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    currency = "RUB"
                },
                capture = true,
                confirmation = new
                {
                    type = "redirect",
                    return_url = returnUrl
                },
                description = description,
                metadata = new
                {
                    userId = userId.ToString()
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.yookassa.ru/v3/payments")
            {
                Content = content
            };
            request.Headers.Add("Idempotence-Key", idempotenceKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("YooKassa payment creation failed: {StatusCode} {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(responseBody);
            
            if (jsonDoc.RootElement.TryGetProperty("confirmation", out var confirmation) && 
                confirmation.TryGetProperty("confirmation_url", out var confirmationUrl))
            {
                return confirmationUrl.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating payment in YooKassa");
            return null;
        }
    }
}