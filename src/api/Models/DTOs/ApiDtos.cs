using api.Models;

namespace api.Models.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = nameof(TaskStatus.NotStarted);
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsShared { get; set; }
}

public class CreateTaskDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
}

public class TaskShareDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public Guid SharedByUserId { get; set; }
    public string SharedByUserEmail { get; set; } = string.Empty;
    public Guid SharedWithUserId { get; set; }
    public string SharedWithUserEmail { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; }
}

public class CreateTaskShareDto
{
    public required string SharedWithUserEmail { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
