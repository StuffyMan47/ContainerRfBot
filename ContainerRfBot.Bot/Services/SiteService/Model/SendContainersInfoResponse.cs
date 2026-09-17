using System.Text.Json.Serialization;

namespace ContainerRfBot.Bot.Services.SiteService.Model;

public class SendContainersInfoResponse
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public List<string> Created { get; set; } = [];
    
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];
}
