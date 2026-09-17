using ContainerRfBot.Bot.Services.SiteService.Model;

namespace ContainerRfBot.Bot.Services.SiteService;

public interface ISiteClient
{
    Task<SendContainersInfoResponse> SendContainersInfo(List<SendContainersInfoRequest> request, CancellationToken cancellationToken);
}
