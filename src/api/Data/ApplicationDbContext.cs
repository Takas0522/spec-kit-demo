using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Models.Task> Tasks { get; set; }
    public DbSet<TaskShare> TaskShares { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntraObjectId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.EntraObjectId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // Task configuration
        modelBuilder.Entity<Models.Task>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(Models.TaskStatus.NotStarted);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(e => new { e.OwnerId, e.IsDeleted, e.Status });
            entity.HasIndex(e => e.DueDate);

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedTasks)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraint for Title not empty will be added in migration
            // Check constraint for Status values will be added in migration
        });

        // TaskShare configuration
        modelBuilder.Entity<TaskShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TaskId, e.SharedWithUserId }).IsUnique();
            entity.HasIndex(e => e.SharedWithUserId);
            entity.HasIndex(e => e.TaskId);
            entity.Property(e => e.SharedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.CanEdit).HasDefaultValue(false);

            entity.HasOne(e => e.Task)
                .WithMany(t => t.Shares)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SharedByUser)
                .WithMany(u => u.SharedByMe)
                .HasForeignKey(e => e.SharedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SharedWithUser)
                .WithMany(u => u.SharedWithMe)
                .HasForeignKey(e => e.SharedWithUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraint for not sharing with self will be added in migration
        });
    }
}
