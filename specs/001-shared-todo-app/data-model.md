# Data Model: Shared ToDo Management Application

**Date**: October 28, 2025  
**Phase**: 1 - Design & Contracts

## Overview

This document defines the data model for the Shared ToDo Management application, including entities, relationships, validation rules, and state transitions.

## Entity Relationship Diagram

```
┌─────────────────┐
│     User        │
│─────────────────│
│ Id (PK)         │◄─────┐
│ Email           │      │
│ DisplayName     │      │ Owner
│ EntraObjectId   │      │
│ CreatedAt       │      │
│ IsActive        │      │
└─────────────────┘      │
         │               │
         │ Shared with  │
         │               │
         ▼               │
┌─────────────────┐      │
│   TaskShare     │      │
│─────────────────│      │
│ Id (PK)         │      │
│ TaskId (FK)     │──────┤
│ SharedByUserId  │      │
│ SharedWithUserId│──────┘
│ SharedAt        │
│ CanEdit         │ (future)
└─────────────────┘
         │
         │
         ▼
┌─────────────────┐
│     Task        │
│─────────────────│
│ Id (PK)         │
│ OwnerId (FK)    │──────┐
│ Title           │      │
│ Description     │      │
│ Status          │      │
│ DueDate         │      │
│ CreatedAt       │      │
│ ModifiedAt      │      │
│ IsDeleted       │      │
└─────────────────┘      │
                         │
                    Owns │
                         │
                         └─────► User
```

## Entities

### 1. User

**Purpose**: Represents a registered user authenticated via Azure Entra ID.

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK, NOT NULL, DEFAULT newid() | Primary key |
| EntraObjectId | nvarchar(100) | NOT NULL, UNIQUE | Azure Entra ID object ID (OID claim) |
| Email | nvarchar(256) | NOT NULL, UNIQUE | User email address (from token) |
| DisplayName | nvarchar(200) | NOT NULL | User display name |
| CreatedAt | datetime2 | NOT NULL, DEFAULT getutcdate() | Account creation timestamp |
| LastLoginAt | datetime2 | NULL | Last successful login |
| IsActive | bit | NOT NULL, DEFAULT 1 | Soft delete flag |

**Indexes**:
- Unique index on `EntraObjectId`
- Unique index on `Email`

**Relationships**:
- One-to-many with `Task` (as owner)
- One-to-many with `TaskShare` (as sharer and sharee)

**Validation Rules**:
- Email must be valid format (handled by Azure Entra ID)
- DisplayName max 200 characters
- EntraObjectId must match token claim

**Notes**:
- User records are automatically created on first login (auto-provisioning)
- Soft delete via `IsActive` flag to preserve referential integrity
- No password stored (handled by Azure Entra ID)

---

### 2. Task

**Purpose**: Represents a todo task owned by a user.

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK, NOT NULL, DEFAULT newid() | Primary key |
| OwnerId | uniqueidentifier | FK (User.Id), NOT NULL | Task owner |
| Title | nvarchar(200) | NOT NULL | Task title |
| Description | nvarchar(2000) | NULL | Task description (optional) |
| Status | nvarchar(20) | NOT NULL, DEFAULT 'NotStarted' | Task status enum |
| DueDate | datetime2 | NULL | Optional due date |
| CreatedAt | datetime2 | NOT NULL, DEFAULT getutcdate() | Creation timestamp |
| ModifiedAt | datetime2 | NOT NULL, DEFAULT getutcdate() | Last modification timestamp |
| IsDeleted | bit | NOT NULL, DEFAULT 0 | Soft delete flag |

**Indexes**:
- Clustered index on `Id`
- Non-clustered index on `OwnerId, IsDeleted, Status` (for user task queries)
- Non-clustered index on `DueDate` (for filtering)

**Relationships**:
- Many-to-one with `User` (OwnerId)
- One-to-many with `TaskShare`

**Validation Rules**:
- Title: Required, max 200 characters
- Description: Optional, max 2000 characters
- Status: Must be one of ['NotStarted', 'InProgress', 'Completed']
- DueDate: Optional, must be in future or present
- ModifiedAt: Automatically updated on any change

**Check Constraints**:
```sql
ALTER TABLE Tasks
ADD CONSTRAINT CK_Task_Status 
CHECK (Status IN ('NotStarted', 'InProgress', 'Completed'));

ALTER TABLE Tasks
ADD CONSTRAINT CK_Task_Title_NotEmpty
CHECK (LEN(TRIM(Title)) > 0);
```

**Triggers**:
```sql
-- Update ModifiedAt automatically
CREATE TRIGGER TR_Task_UpdateModifiedAt
ON Tasks
AFTER UPDATE
AS
BEGIN
    UPDATE Tasks
    SET ModifiedAt = GETUTCDATE()
    FROM Tasks t
    INNER JOIN inserted i ON t.Id = i.Id
    WHERE t.ModifiedAt = i.ModifiedAt; -- Avoid infinite loop
END;
```

**Notes**:
- Soft delete via `IsDeleted` to maintain task share history
- Cascade delete not used; tasks preserved when user deactivated
- Status transitions validated in application layer (see State Machine)

---

### 3. TaskShare

**Purpose**: Represents a sharing relationship between a task and another user.

**Attributes**:

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK, NOT NULL, DEFAULT newid() | Primary key |
| TaskId | uniqueidentifier | FK (Task.Id), NOT NULL | Shared task |
| SharedByUserId | uniqueidentifier | FK (User.Id), NOT NULL | User who shared (typically owner) |
| SharedWithUserId | uniqueidentifier | FK (User.Id), NOT NULL | User receiving access |
| SharedAt | datetime2 | NOT NULL, DEFAULT getutcdate() | When sharing occurred |
| CanEdit | bit | NOT NULL, DEFAULT 0 | Future: write permission flag |

**Indexes**:
- Clustered index on `Id`
- Unique index on `(TaskId, SharedWithUserId)` to prevent duplicate shares
- Non-clustered index on `SharedWithUserId` (for "shared with me" queries)
- Non-clustered index on `TaskId` (for listing shares per task)

**Relationships**:
- Many-to-one with `Task` (TaskId)
- Many-to-one with `User` (SharedByUserId)
- Many-to-one with `User` (SharedWithUserId)

**Validation Rules**:
- TaskId: Must reference existing, non-deleted task
- SharedByUserId: Must be task owner (enforced in application)
- SharedWithUserId: Must reference active user
- SharedByUserId ≠ SharedWithUserId (can't share with self)

**Check Constraints**:
```sql
ALTER TABLE TaskShares
ADD CONSTRAINT CK_TaskShare_NotSelf
CHECK (SharedByUserId <> SharedWithUserId);
```

**Notes**:
- Deleting a share = revoking access
- CanEdit flag reserved for future feature (currently all shares are read-only)
- No cascade delete; shares remain if task deleted (for audit)

---

## Status Enumeration

**Enum Name**: `TaskStatus`

**Values**:
```csharp
public enum TaskStatus
{
    NotStarted = 0,  // 未実施
    InProgress = 1,  // 実施中
    Completed = 2    // 完了
}
```

**Database Storage**: `nvarchar(20)` (stored as string for readability in SQL queries)

**Validation**:
- Frontend: Dropdown with localized labels
- Backend: Enum validation via model binding

---

## State Machine: Task Status Transitions

**Purpose**: Define valid status transitions to maintain data integrity.

```
┌─────────────┐
│ NotStarted  │
│   (初期状態)  │
└──────┬──────┘
       │
       │ User clicks "Start"
       ▼
┌─────────────┐
│ InProgress  │◄─────┐
│  (作業中)     │      │
└──────┬──────┘      │
       │             │ User clicks "Reopen"
       │             │
       │ User clicks │
       │ "Complete"  │
       ▼             │
┌─────────────┐      │
│  Completed  │──────┘
│   (完了)      │
└─────────────┘
```

**Valid Transitions**:

| From | To | Trigger | Authorization |
|------|-----|---------|---------------|
| NotStarted | InProgress | User starts task | Owner only |
| NotStarted | Completed | User marks done without starting | Owner only |
| InProgress | Completed | User completes task | Owner only |
| Completed | InProgress | User reopens task | Owner only |
| Any | NotStarted | User resets task (rare) | Owner only |

**Invalid Transitions**:
- Shared users cannot change status (read-only)
- Deleted tasks cannot change status

**Implementation**:
```csharp
public class TaskService
{
    public async Task<Result> UpdateTaskStatusAsync(
        Guid taskId, 
        string userId, 
        TaskStatus newStatus)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.OwnerId == userId);
            
        if (task == null)
            return Result.Fail("Task not found or not authorized");
        
        // Validate transition (can add complex rules here)
        if (!IsValidTransition(task.Status, newStatus))
            return Result.Fail($"Invalid status transition from {task.Status} to {newStatus}");
        
        task.Status = newStatus;
        await _context.SaveChangesAsync();
        
        return Result.Success();
    }
    
    private bool IsValidTransition(TaskStatus from, TaskStatus to)
    {
        // All transitions allowed for simplicity in MVP
        // Can add restrictions later if needed
        return true;
    }
}
```

---

## Data Validation Summary

### User Entity Validation

**Frontend**:
```typescript
interface User {
  id: string;
  email: string;        // Email format validated by MSAL
  displayName: string;  // Max 200 chars
  entraObjectId: string;
  createdAt: Date;
  isActive: boolean;
}
```

**Backend**:
```csharp
public class User
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EntraObjectId { get; set; } = null!;
    
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<TaskShare> SharedTasks { get; set; } = new List<TaskShare>();
}
```

---

### Task Entity Validation

**Frontend**:
```typescript
interface Task {
  id: string;
  ownerId: string;
  title: string;           // Required, max 200
  description?: string;    // Optional, max 2000
  status: TaskStatus;      // Enum
  dueDate?: Date;         // Optional
  createdAt: Date;
  modifiedAt: Date;
  isDeleted: boolean;
}

enum TaskStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  Completed = 'Completed'
}
```

**Backend**:
```csharp
public class Task
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid OwnerId { get; set; }
    
    [Required]
    [MaxLength(200)]
    [MinLength(1)]
    public string Title { get; set; } = null!;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    [Required]
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
    
    public DateTime? DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<TaskShare> Shares { get; set; } = new List<TaskShare>();
}
```

---

### TaskShare Entity Validation

**Frontend**:
```typescript
interface TaskShare {
  id: string;
  taskId: string;
  sharedByUserId: string;
  sharedWithUserId: string;
  sharedAt: Date;
  canEdit: boolean;        // Future feature
}
```

**Backend**:
```csharp
public class TaskShare
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid TaskId { get; set; }
    
    [Required]
    public Guid SharedByUserId { get; set; }
    
    [Required]
    public Guid SharedWithUserId { get; set; }
    
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
    
    public bool CanEdit { get; set; } = false; // Reserved for future
    
    // Navigation properties
    public Task Task { get; set; } = null!;
    public User SharedBy { get; set; } = null!;
    public User SharedWith { get; set; } = null!;
}
```

---

## Database Schema SQL

### Users Table

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EntraObjectId NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    DisplayName NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT UQ_User_EntraObjectId UNIQUE (EntraObjectId),
    CONSTRAINT UQ_User_Email UNIQUE (Email)
);

CREATE NONCLUSTERED INDEX IX_User_EntraObjectId ON Users(EntraObjectId);
CREATE NONCLUSTERED INDEX IX_User_Email ON Users(Email);
```

### Tasks Table

```sql
CREATE TABLE Tasks (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OwnerId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000) NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'NotStarted',
    DueDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_Task_Owner FOREIGN KEY (OwnerId) REFERENCES Users(Id),
    CONSTRAINT CK_Task_Status CHECK (Status IN ('NotStarted', 'InProgress', 'Completed')),
    CONSTRAINT CK_Task_Title_NotEmpty CHECK (LEN(TRIM(Title)) > 0)
);

CREATE NONCLUSTERED INDEX IX_Task_OwnerId_IsDeleted_Status 
    ON Tasks(OwnerId, IsDeleted, Status);
CREATE NONCLUSTERED INDEX IX_Task_DueDate ON Tasks(DueDate);
```

### TaskShares Table

```sql
CREATE TABLE TaskShares (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TaskId UNIQUEIDENTIFIER NOT NULL,
    SharedByUserId UNIQUEIDENTIFIER NOT NULL,
    SharedWithUserId UNIQUEIDENTIFIER NOT NULL,
    SharedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CanEdit BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_TaskShare_Task FOREIGN KEY (TaskId) REFERENCES Tasks(Id),
    CONSTRAINT FK_TaskShare_SharedBy FOREIGN KEY (SharedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_TaskShare_SharedWith FOREIGN KEY (SharedWithUserId) REFERENCES Users(Id),
    CONSTRAINT UQ_TaskShare_TaskUser UNIQUE (TaskId, SharedWithUserId),
    CONSTRAINT CK_TaskShare_NotSelf CHECK (SharedByUserId <> SharedWithUserId)
);

CREATE NONCLUSTERED INDEX IX_TaskShare_SharedWithUserId ON TaskShares(SharedWithUserId);
CREATE NONCLUSTERED INDEX IX_TaskShare_TaskId ON TaskShares(TaskId);
```

---

## Query Patterns

### 1. Get User's Tasks (with filtering)

```sql
-- Get all active tasks for a user
SELECT t.*
FROM Tasks t
WHERE t.OwnerId = @UserId 
  AND t.IsDeleted = 0
ORDER BY 
  CASE 
    WHEN t.Status = 'InProgress' THEN 1
    WHEN t.Status = 'NotStarted' THEN 2
    WHEN t.Status = 'Completed' THEN 3
  END,
  t.DueDate ASC NULLS LAST,
  t.CreatedAt DESC;
```

**EF Core LINQ**:
```csharp
var tasks = await _context.Tasks
    .Where(t => t.OwnerId == userId && !t.IsDeleted)
    .OrderBy(t => t.Status == TaskStatus.InProgress ? 1 : 
                  t.Status == TaskStatus.NotStarted ? 2 : 3)
    .ThenBy(t => t.DueDate)
    .ThenByDescending(t => t.CreatedAt)
    .ToListAsync();
```

---

### 2. Get Tasks Shared With User

```sql
SELECT t.*, u.DisplayName as OwnerName, ts.SharedAt
FROM TaskShares ts
INNER JOIN Tasks t ON ts.TaskId = t.Id
INNER JOIN Users u ON t.OwnerId = u.Id
WHERE ts.SharedWithUserId = @UserId
  AND t.IsDeleted = 0
ORDER BY ts.SharedAt DESC;
```

**EF Core LINQ**:
```csharp
var sharedTasks = await _context.TaskShares
    .Where(ts => ts.SharedWithUserId == userId)
    .Include(ts => ts.Task)
        .ThenInclude(t => t.Owner)
    .Where(ts => !ts.Task.IsDeleted)
    .OrderByDescending(ts => ts.SharedAt)
    .Select(ts => new SharedTaskDto
    {
        Task = ts.Task,
        OwnerName = ts.Task.Owner.DisplayName,
        SharedAt = ts.SharedAt
    })
    .ToListAsync();
```

---

### 3. Check Task Ownership or Share Access

```sql
-- Check if user can view task (owner or shared with)
SELECT COUNT(*)
FROM Tasks t
LEFT JOIN TaskShares ts ON t.Id = ts.TaskId AND ts.SharedWithUserId = @UserId
WHERE t.Id = @TaskId
  AND t.IsDeleted = 0
  AND (t.OwnerId = @UserId OR ts.Id IS NOT NULL);
```

**EF Core LINQ**:
```csharp
var hasAccess = await _context.Tasks
    .Where(t => t.Id == taskId && !t.IsDeleted)
    .Where(t => t.OwnerId == userId || 
                t.Shares.Any(s => s.SharedWithUserId == userId))
    .AnyAsync();
```

---

## Migration Strategy

### Database-First Approach

This project uses a database-first strategy. The SQL schema (managed via SSDT project under `src/database` or direct SQL scripts) is authoritative. Entity classes and `DbContext` are regenerated when the schema changes using EF Core scaffolding:

```bash
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring
```

Guidelines:
1. Apply schema changes directly to the database (development instance) or update the SQL project.
2. Re-scaffold immediately after changes.
3. Extend behavior via partial classes / Fluent API; do not modify generated files.
4. Avoid code-first migrations; they are intentionally not part of the workflow.
5. For deterministic local development, provide versioned SQL migration scripts in `src/database` (future enhancement).

### Data Seeding (Development Only)

Because of database-first, seeding is optional and performed via SQL or a one-time initializer that checks existence. Example (conceptual, not included in production runtime):

```sql
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (Id, EntraObjectId, Email, DisplayName)
    VALUES (NEWID(), 'test-oid-1', 'user1@example.com', 'Test User 1'),
           (NEWID(), 'test-oid-2', 'user2@example.com', 'Test User 2');
END;
```

Application-level seeding (if needed) should run behind a feature flag to prevent accidental production inserts.

---

## Performance Considerations

### Indexing Strategy

1. **User lookups by EntraObjectId**: Most common authentication query
2. **Task queries by OwnerId + IsDeleted**: Frequent user dashboard queries
3. **TaskShare queries by SharedWithUserId**: "Shared with me" view
4. **Covering index for task list**: Include Title, Status, DueDate to avoid key lookups

### Query Optimization

1. **Pagination**: Always paginate list queries (default 20 items per page)
2. **Projection**: Use `.Select()` to return only needed fields
3. **No tracking**: Use `.AsNoTracking()` for read-only queries
4. **Compiled queries**: For frequently executed queries (EF Core compiled queries)

### Connection Pooling

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:myserver.database.windows.net;Database=TodoDB;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;"
  }
}
```

---

## Audit and Compliance

### Soft Delete Pattern

All entities use soft delete (`IsDeleted` flag) to:
- Maintain referential integrity
- Enable audit trails
- Support "undo" functionality
- Comply with data retention policies

### Audit Columns

All entities include:
- `CreatedAt`: Automatic timestamp on insert
- `ModifiedAt`: Automatic timestamp on update (trigger/EF Core)
- Owner tracking via `OwnerId` / `SharedByUserId`

### Future: Full Audit Table

```sql
CREATE TABLE AuditLog (
    Id BIGINT IDENTITY PRIMARY KEY,
    TableName NVARCHAR(50) NOT NULL,
    RecordId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(10) NOT NULL, -- INSERT, UPDATE, DELETE
    ChangedBy UNIQUEIDENTIFIER NOT NULL,
    ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL
);
```

---

## Conclusion

The data model provides a solid foundation for the Shared ToDo Management application with:
- ✅ Clear entity relationships
- ✅ Comprehensive validation rules
- ✅ Proper indexing for performance
- ✅ Soft delete for data integrity
- ✅ State machine for status transitions
- ✅ Security through ownership checks
- ✅ Extensibility for future features (CanEdit flag, audit logs)

Ready to proceed with **API Contract Definition**.
