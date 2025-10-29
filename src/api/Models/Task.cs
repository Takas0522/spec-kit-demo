namespace api.Models;

public class Task
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid OwnerId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<TaskShare> Shares { get; set; } = new List<TaskShare>();
}
