using Microsoft.AspNetCore.Mvc;
using ContainerRfBot.Core.UseCases;

namespace ContainerRfBot.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ManageSubscriptionUseCase _manageSubscriptionUseCase;

    public AdminController(ManageSubscriptionUseCase manageSubscriptionUseCase)
    {
        _manageSubscriptionUseCase = manageSubscriptionUseCase;
    }

    [HttpPost("users/{userId}/subscription")]
    public async Task<IActionResult> SetSubscription(long userId, [FromBody] SetSubscriptionRequest request)
    {
        try
        {
            await _manageSubscriptionUseCase.SetSubscriptionStatusAsync(userId, request.HasSubscription, request.ExpirationDate);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class SetSubscriptionRequest
{
    public bool HasSubscription { get; set; }
    public DateTime? ExpirationDate { get; set; }
}