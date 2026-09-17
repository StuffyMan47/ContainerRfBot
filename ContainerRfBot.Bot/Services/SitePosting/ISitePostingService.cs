using ContainerRfBot.Bot.Services.Dto;

namespace ContainerRfBot.Bot.Services.SitePosting;

public interface ISitePostingService
{
    Task SendContainersToSite(List<ContainerRequestModel> containers);
}
