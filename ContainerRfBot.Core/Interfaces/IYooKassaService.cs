namespace ContainerRfBot.Core.Interfaces;

public interface IYooKassaService
{
    Task<string?> CreatePaymentAsync(long userId, decimal amount, string description, string returnUrl, CancellationToken cancellationToken = default);
}