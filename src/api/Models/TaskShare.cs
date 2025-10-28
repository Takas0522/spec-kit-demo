namespace api.Models;

public class TaskShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid TaskId { get; set; }
    public required Guid SharedByUserId { get; set; }
    public required Guid SharedWithUserId { get; set; }
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    public bool CanEdit { get; set; } = false;

    // Navigation properties
    public Task? Task { get; set; }
    public User? SharedByUser { get; set; }
    public User? SharedWithUser { get; set; }
}
