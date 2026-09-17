using ContainerRfBot.Core.Models.Containers;

namespace ContainerRfBot.Core.Interfaces;

public interface IContainerRepository
{
    Task CreateContainerList(List<CreateContainerListRequest> containers, CancellationToken cancellationToken);
    Task<List<GetContainerListResponse>> GetContainerList(List<long>? ids, DateTimeOffset? date, CancellationToken cancellationToken);
    Task CreateContainer(CreateContainerListRequest container, CancellationToken cancellationToken);
}
