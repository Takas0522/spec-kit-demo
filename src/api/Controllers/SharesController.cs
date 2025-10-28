using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;
using api.Models.DTOs;

namespace api.Controllers;

[ApiController]
[Route("api/tasks/{taskId}/[controller]")]
public class SharesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SharesController> _logger;

    public SharesController(ApplicationDbContext context, ILogger<SharesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/tasks/{taskId}/shares
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskShareDto>>> GetTaskShares(Guid taskId, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        if (task == null)
        {
            return NotFound();
        }

        // Only owner can see shares
        if (task.OwnerId != currentUserId)
        {
            return Forbid();
        }

        var shares = await _context.TaskShares
            .Where(ts => ts.TaskId == taskId)
            .Include(ts => ts.Task)
            .Include(ts => ts.SharedByUser)
            .Include(ts => ts.SharedWithUser)
            .Select(ts => new TaskShareDto
            {
                Id = ts.Id,
                TaskId = ts.TaskId,
                TaskTitle = ts.Task != null ? ts.Task.Title : "",
                SharedByUserId = ts.SharedByUserId,
                SharedByUserEmail = ts.SharedByUser != null ? ts.SharedByUser.Email : "",
                SharedWithUserId = ts.SharedWithUserId,
                SharedWithUserEmail = ts.SharedWithUser != null ? ts.SharedWithUser.Email : "",
                SharedAt = ts.SharedAt
            })
            .ToListAsync();

        return Ok(shares);
    }

    // POST: api/tasks/{taskId}/shares
    [HttpPost]
    public async Task<ActionResult<TaskShareDto>> ShareTask(Guid taskId, [FromBody] CreateTaskShareDto createShareDto, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        if (task == null)
        {
            return NotFound("Task not found");
        }

        // Only owner can share
        if (task.OwnerId != currentUserId)
        {
            return Forbid();
        }

        // Find user to share with
        var sharedWithUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == createShareDto.SharedWithUserEmail && u.IsActive);

        if (sharedWithUser == null)
        {
            return BadRequest("User not found");
        }

        // Can't share with self
        if (sharedWithUser.Id == currentUserId)
        {
            return BadRequest("Cannot share task with yourself");
        }

        // Check if already shared
        var existingShare = await _context.TaskShares
            .FirstOrDefaultAsync(ts => ts.TaskId == taskId && ts.SharedWithUserId == sharedWithUser.Id);

        if (existingShare != null)
        {
            return BadRequest("Task already shared with this user");
        }

        var taskShare = new TaskShare
        {
            TaskId = taskId,
            SharedByUserId = currentUserId,
            SharedWithUserId = sharedWithUser.Id
        };

        _context.TaskShares.Add(taskShare);
        await _context.SaveChangesAsync();

        var owner = await _context.Users.FindAsync(currentUserId);

        var shareDto = new TaskShareDto
        {
            Id = taskShare.Id,
            TaskId = taskShare.TaskId,
            TaskTitle = task.Title,
            SharedByUserId = taskShare.SharedByUserId,
            SharedByUserEmail = owner?.Email ?? "",
            SharedWithUserId = taskShare.SharedWithUserId,
            SharedWithUserEmail = sharedWithUser.Email,
            SharedAt = taskShare.SharedAt
        };

        return CreatedAtAction(nameof(GetTaskShares), new { taskId }, shareDto);
    }

    // DELETE: api/tasks/{taskId}/shares/{shareId}
    [HttpDelete("{shareId}")]
    public async Task<IActionResult> RevokeShare(Guid taskId, Guid shareId, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var taskShare = await _context.TaskShares
            .Include(ts => ts.Task)
            .FirstOrDefaultAsync(ts => ts.Id == shareId && ts.TaskId == taskId);

        if (taskShare == null)
        {
            return NotFound();
        }

        // Only owner can revoke
        if (taskShare.Task?.OwnerId != currentUserId)
        {
            return Forbid();
        }

        _context.TaskShares.Remove(taskShare);
        await _context.SaveChangesAsync();

        return NoContent();
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

        return demoUser.Id;
    }
}
