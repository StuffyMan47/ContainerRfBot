using Microsoft.AspNetCore.Mvc;
using ContainerRfBot.Core.Entities;
using ContainerRfBot.Core.Interfaces;

namespace ContainerRfBot.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = new User
        {
            Id = request.Id, // In a real app this would likely come from auth or a proper ID generator if not provided
            IsAdmin = request.IsAdmin,
            HasSubscription = request.HasSubscription,
            SubscriptionExpirationDate = request.SubscriptionExpirationDate
        };

        await _userRepository.AddAsync(user);
        return Ok(user);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }
}

public class CreateUserRequest
{
    public long Id { get; set; }
    public bool IsAdmin { get; set; }
    public bool HasSubscription { get; set; }
    public DateTime? SubscriptionExpirationDate { get; set; }
}