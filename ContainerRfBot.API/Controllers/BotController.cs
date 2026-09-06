using ContainerRfBot.Bot.Services.MaxBot;
using Max.Bot.Types;
using Microsoft.AspNetCore.Mvc;

namespace ContainerRfBot.API.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update, [FromServices] MaxBotService maxBotService, CancellationToken cancellationToken)
    {
        await maxBotService.HandleUpdateAsync(update, cancellationToken);
        return Ok();
    }
}