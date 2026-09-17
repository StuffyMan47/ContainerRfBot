using ContainerRfBot.Bot.Services.MaxBot;
using Max.Bot.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ContainerRfBot.API.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController : ControllerBase
{
    private readonly ILogger<BotController> _logger;

    public BotController(ILogger<BotController> logger)
    {
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update, [FromServices] MaxBotService maxBotService, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Webhook received update: Type={Type}", update?.Type);
        try
        {
            if (update != null)
            {
                await maxBotService.HandleUpdateAsync(update, cancellationToken);
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook update");
            return Ok(); // Возвращаем 200, чтобы Max не слал повторно ошибочный апдейт
        }
    }
}
