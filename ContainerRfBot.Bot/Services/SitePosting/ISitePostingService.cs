using ContainerRfBot.Bot.Services.Dto;

namespace ContainerRfBot.Bot.Services.SitePosting;

public interface ISitePostingService
{
    Task SendConfirmToAdmin();
    Task ReadGoogleTable(DateTimeOffset date);
    Task SendContainersToSite(List<ContainerRequestModel> containers);
}
