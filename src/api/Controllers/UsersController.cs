using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;
using api.Models.DTOs;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/users/me
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser([FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var user = await _context.Users.FindAsync(currentUserId);

        if (user == null)
        {
            return NotFound();
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(userDto);
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // Helper method to create or get demo user
    private async Task<Guid> GetOrCreateDemoUser()
    {
        var demoUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "demo@example.com");

        if (demoUser == null)
        {
            demoUser = new User
            {
                EntraObjectId = "demo-object-id",
                Email = "demo@example.com",
                DisplayName = "Demo User"
            };
            _context.Users.Add(demoUser);
            await _context.SaveChangesAsync();
        }

        // Update last login
        demoUser.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return demoUser.Id;
    }
}
