using Microsoft.Extensions.DependencyInjection;
using ContainerRfBot.Core.Interfaces;

namespace ContainerRfBot.Core.UseCases;

public class ManageSubscriptionUseCase
{
    private readonly IUserRepository _userRepository;

    public ManageSubscriptionUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task SetSubscriptionStatusAsync(long userId, bool hasSubscription, DateTime? expirationDate = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new Exception("User not found"); // Or custom exception
        }

        user.HasSubscription = hasSubscription;
        user.SubscriptionExpirationDate = expirationDate;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}