using Microsoft.AspNetCore.Mvc;
using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Interfaces;

namespace ContainerRfBot.API.Controllers;

[ApiController]
[Route("api/messages")]
public class MessageController : ControllerBase
{
    private readonly IMessageRepository _messageRepository;

    public MessageController(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    [HttpPost]
    public async Task<IActionResult> AddMessage([FromBody] AddMessageRequest request)
    {
        var message = new Message
        {
            UserId = request.UserId,
            Content = request.Content
        };

        await _messageRepository.AddAsync(message);
        return Ok(message);
    }
    
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetMessagesByUser(long userId)
    {
        var messages = await _messageRepository.GetByUserIdAsync(userId);
        return Ok(messages);
    }
}

public class AddMessageRequest
{
    public long UserId { get; set; }
    public string Content { get; set; }
}