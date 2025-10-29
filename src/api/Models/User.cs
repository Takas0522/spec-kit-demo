namespace api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EntraObjectId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Task> OwnedTasks { get; set; } = new List<Task>();
    public ICollection<TaskShare> SharedByMe { get; set; } = new List<TaskShare>();
    public ICollection<TaskShare> SharedWithMe { get; set; } = new List<TaskShare>();
}
