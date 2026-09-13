using System.Text.Json;
using ContainerRfBot.Core.Interfaces;
using ContainerRfBot.Core.UseCases;
using Max.Bot;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.API.Controllers;

[ApiController]
[Route("api/payments/yookassa")]
public class YooKassaController : ControllerBase
{
    private readonly ManageSubscriptionUseCase _manageSubscriptionUseCase;
    private readonly MaxClient _maxClient;
    private readonly ILogger<YooKassaController> _logger;

    public YooKassaController(ManageSubscriptionUseCase manageSubscriptionUseCase, MaxClient maxClient, ILogger<YooKassaController> logger)
    {
        _manageSubscriptionUseCase = manageSubscriptionUseCase;
        _maxClient = maxClient;
        _logger = logger;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("YooKassa Webhook Controller is alive!");
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
            if (payload.TryGetProperty("event", out var eventType) && eventType.GetString() == "payment.succeeded")
            {
                if (payload.TryGetProperty("object", out var paymentObj) && 
                    paymentObj.TryGetProperty("metadata", out var metadata) && 
                    metadata.TryGetProperty("userId", out var userIdElement))
                {
                    if (long.TryParse(userIdElement.GetString(), out var userId))
                    {
                        var expirationDate = DateTime.UtcNow.AddDays(30);
                        await _manageSubscriptionUseCase.SetSubscriptionStatusAsync(userId, true, expirationDate, cancellationToken);
                        
                        await _maxClient.Messages.SendMessageToUserAsync(userId: userId, text: "Ваша оплата успешно получена! Подписка активирована на 30 дней.", cancellationToken: cancellationToken);
                        _logger.LogInformation("Subscription granted via YooKassa for User {UserId}", userId);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing YooKassa webhook");
            return BadRequest();
        }
    }
}