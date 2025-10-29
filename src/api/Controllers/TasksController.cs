using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Models;
using api.Models.DTOs;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ApplicationDbContext context, ILogger<TasksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/tasks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks([FromQuery] string? userId = null)
    {
        // For demo purposes, use a fixed user ID if not provided
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var tasks = await _context.Tasks
            .Where(t => t.OwnerId == currentUserId && !t.IsDeleted)
            .Include(t => t.Owner)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                OwnerId = t.OwnerId,
                OwnerEmail = t.Owner != null ? t.Owner.Email : "",
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                ModifiedAt = t.ModifiedAt,
                IsShared = t.Shares.Any()
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // GET: api/tasks/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDto>> GetTask(Guid id, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var task = await _context.Tasks
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (task == null)
        {
            return NotFound();
        }

        // Check if user owns the task or has it shared with them
        var isOwner = task.OwnerId == currentUserId;
        var hasAccess = isOwner || await _context.TaskShares
            .AnyAsync(ts => ts.TaskId == id && ts.SharedWithUserId == currentUserId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var taskDto = new TaskDto
        {
            Id = task.Id,
            OwnerId = task.OwnerId,
            OwnerEmail = task.Owner?.Email ?? "",
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            ModifiedAt = task.ModifiedAt,
            IsShared = await _context.TaskShares.AnyAsync(ts => ts.TaskId == id)
        };

        return Ok(taskDto);
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto createTaskDto, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var task = new Models.Task
        {
            OwnerId = currentUserId,
            Title = createTaskDto.Title,
            Description = createTaskDto.Description,
            DueDate = createTaskDto.DueDate
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var owner = await _context.Users.FindAsync(currentUserId);

        var taskDto = new TaskDto
        {
            Id = task.Id,
            OwnerId = task.OwnerId,
            OwnerEmail = owner?.Email ?? "",
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            ModifiedAt = task.ModifiedAt,
            IsShared = false
        };

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, taskDto);
    }

    // PUT: api/tasks/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto updateTaskDto, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (task == null)
        {
            return NotFound();
        }

        // Only owner can update
        if (task.OwnerId != currentUserId)
        {
            return Forbid();
        }

        if (updateTaskDto.Title != null)
            task.Title = updateTaskDto.Title;

        if (updateTaskDto.Description != null)
            task.Description = updateTaskDto.Description;

        if (updateTaskDto.Status != null && Enum.TryParse<Models.TaskStatus>(updateTaskDto.Status, out var status))
            task.Status = status;

        if (updateTaskDto.DueDate != null)
            task.DueDate = updateTaskDto.DueDate;

        task.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/tasks/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id, [FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (task == null)
        {
            return NotFound();
        }

        // Only owner can delete
        if (task.OwnerId != currentUserId)
        {
            return Forbid();
        }

        task.IsDeleted = true;
        task.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/tasks/shared
    [HttpGet("shared")]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetSharedTasks([FromQuery] string? userId = null)
    {
        var currentUserId = userId != null ? Guid.Parse(userId) : await GetOrCreateDemoUser();

        var sharedTasks = await _context.TaskShares
            .Where(ts => ts.SharedWithUserId == currentUserId)
            .Include(ts => ts.Task)
            .ThenInclude(t => t!.Owner)
            .Where(ts => ts.Task != null && !ts.Task.IsDeleted)
            .Select(ts => new TaskDto
            {
                Id = ts.Task!.Id,
                OwnerId = ts.Task.OwnerId,
                OwnerEmail = ts.Task.Owner != null ? ts.Task.Owner.Email : "",
                Title = ts.Task.Title,
                Description = ts.Task.Description,
                Status = ts.Task.Status.ToString(),
                DueDate = ts.Task.DueDate,
                CreatedAt = ts.Task.CreatedAt,
                ModifiedAt = ts.Task.ModifiedAt,
                IsShared = true
            })
            .ToListAsync();

        return Ok(sharedTasks);
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
