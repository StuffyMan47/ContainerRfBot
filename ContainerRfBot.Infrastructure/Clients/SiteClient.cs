using System.Text;
using System.Text.Json;
using ContainerRfBot.Bot.Services.SiteService;
using ContainerRfBot.Bot.Services.SiteService.Model;
using ContainerRfBot.Core.Interfaces.Settings;
using ContainerRfBot.Core.Interfaces.Settings.Models;
using Microsoft.Extensions.Options;

namespace ContainerRfBot.Infrastructure.Clients;

public class SiteClient : ISiteClient
{
    private readonly BotConfiguration _botConfiguration;
  
    public SiteClient(ISetting setting)
    {
        _botConfiguration = setting.BotConfiguration;
    }

    public async Task<SendContainersInfoResponse> SendContainersInfo(List<SendContainersInfoRequest> request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfiguration.SiteUrl))
        {
            throw new InvalidOperationException("SiteUrl is not configured in BotConfiguration.");
        }

        using var client = new HttpClient();
        client.BaseAddress = new Uri(_botConfiguration.SiteUrl);
        if (!string.IsNullOrEmpty(_botConfiguration.SiteToken))
        {
            client.DefaultRequestHeaders.Add("X-API-KEY", _botConfiguration.SiteToken);
        }
        client.DefaultRequestHeaders.Add("User-Agent", "CSharpClient/1.0");

        var json = JsonSerializer.Serialize(request,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var response = await client.PostAsync("",
            new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var completion = JsonSerializer.Deserialize<SendContainersInfoResponse>(responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
        return completion ?? new SendContainersInfoResponse();
    }
}
