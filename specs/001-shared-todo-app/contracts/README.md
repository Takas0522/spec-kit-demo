# API Contracts

**Version**: 1.0.0  
**Last Updated**: October 28, 2025

## Overview

This directory contains the API contract specifications for the Shared ToDo Management application.

## Files

- **openapi.yaml**: OpenAPI 3.0 specification for the REST API

## API Documentation

The API follows RESTful principles and uses JSON for request/response payloads.

### Base URL

- **Development**: `https://localhost:5001/api`
- **Production**: `https://api.todo-app.example.com/api`

### Authentication

All API endpoints require Azure Entra ID JWT token authentication:

```http
Authorization: Bearer <access_token>
```

**Required Scope**: `api://todo-app/Tasks.ReadWrite`

### Endpoints Summary

| Method | Path | Description | Auth Required |
|--------|------|-------------|---------------|
| GET | `/users/me` | Get current user profile | ✅ |
| GET | `/tasks` | List user's tasks | ✅ |
| POST | `/tasks` | Create new task | ✅ |
| GET | `/tasks/{id}` | Get task by ID | ✅ |
| PUT | `/tasks/{id}` | Update task | ✅ |
| DELETE | `/tasks/{id}` | Delete task | ✅ |
| PATCH | `/tasks/{id}/status` | Update task status | ✅ |
| GET | `/tasks/{id}/shares` | List task shares | ✅ |
| POST | `/tasks/{id}/shares` | Share task with user | ✅ |
| DELETE | `/tasks/{id}/shares/{userId}` | Revoke task share | ✅ |
| GET | `/shared-tasks` | List tasks shared with me | ✅ |

### Response Format

#### Success Response

```json
{
  "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "ownerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "ownerName": "John Doe",
  "title": "Complete project proposal",
  "description": "Write and submit the Q4 project proposal",
  "status": "InProgress",
  "dueDate": "2025-11-15T23:59:59Z",
  "createdAt": "2025-10-28T10:30:00Z",
  "modifiedAt": "2025-10-28T14:45:00Z",
  "isOwner": true
}
```

#### Error Response

```json
{
  "error": {
    "code": "TASK_NOT_FOUND",
    "message": "Task with ID b2c3d4e5-f6a7-8901-bcde-f12345678901 not found",
    "details": []
  }
}
```

### HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 OK | Request successful |
| 201 Created | Resource created successfully |
| 204 No Content | Request successful, no content to return |
| 400 Bad Request | Invalid request parameters or body |
| 401 Unauthorized | Authentication required or token invalid |
| 403 Forbidden | User lacks permission for this resource |
| 404 Not Found | Resource not found |
| 409 Conflict | Resource conflict (e.g., duplicate share) |
| 500 Internal Server Error | Server error |

### Pagination

List endpoints support pagination:

```
GET /api/tasks?page=1&pageSize=20
```

**Response**:
```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### Filtering and Sorting

**Filter by status**:
```
GET /api/tasks?status=InProgress
```

**Sort by field**:
```
GET /api/tasks?sortBy=dueDate&sortOrder=desc
```

**Combine filters**:
```
GET /api/tasks?status=InProgress&sortBy=dueDate&sortOrder=asc&page=1&pageSize=10
```

### Task Status Enum

| Value | Description | Japanese |
|-------|-------------|----------|
| NotStarted | Task not yet started | 未実施 |
| InProgress | Task is being worked on | 実施中 |
| Completed | Task is completed | 完了 |

## Example Workflows

### 1. Create and Share a Task

```http
# Step 1: Create task
POST /api/tasks
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Review design document",
  "description": "Review the Q4 design document",
  "dueDate": "2025-11-30T23:59:59Z"
}

# Response: 201 Created
{
  "id": "new-task-id",
  "ownerId": "user-id",
  "title": "Review design document",
  "status": "NotStarted",
  ...
}

# Step 2: Share with another user
POST /api/tasks/{new-task-id}/shares
Authorization: Bearer <token>
Content-Type: application/json

{
  "sharedWithUserEmail": "colleague@example.com"
}

# Response: 201 Created
{
  "id": "share-id",
  "taskId": "new-task-id",
  "sharedWithUserId": "colleague-id",
  "sharedWithUserEmail": "colleague@example.com",
  "sharedWithUserName": "Jane Smith",
  "sharedAt": "2025-10-28T15:00:00Z"
}
```

### 2. Update Task Status

```http
PATCH /api/tasks/{task-id}/status
Authorization: Bearer <token>
Content-Type: application/json

{
  "status": "InProgress"
}

# Response: 200 OK
{
  "id": "task-id",
  "status": "InProgress",
  "modifiedAt": "2025-10-28T15:30:00Z",
  ...
}
```

### 3. View Shared Tasks

```http
GET /api/shared-tasks?page=1&pageSize=20
Authorization: Bearer <token>

# Response: 200 OK
{
  "items": [
    {
      "id": "shared-task-id",
      "ownerId": "other-user-id",
      "ownerName": "John Doe",
      "title": "Review design document",
      "status": "InProgress",
      "sharedAt": "2025-10-28T15:00:00Z",
      "isOwner": false,
      ...
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

### 4. Revoke Task Share

```http
DELETE /api/tasks/{task-id}/shares/{user-id}
Authorization: Bearer <token>

# Response: 204 No Content
```

## Validation Rules

### Task Creation/Update

- **Title**: Required, 1-200 characters, cannot be empty/whitespace
- **Description**: Optional, max 2000 characters
- **Status**: Must be one of: NotStarted, InProgress, Completed
- **DueDate**: Optional, ISO 8601 date-time format

### Task Sharing

- **sharedWithUserEmail**: Required, valid email format
- **Cannot share with self**: SharedByUserId ≠ SharedWithUserId
- **Cannot share non-existent task**: TaskId must exist and not be deleted
- **Cannot share to non-existent user**: User with email must exist in system
- **No duplicate shares**: Each task can only be shared once with each user

## Authorization Rules

| Operation | Rule |
|-----------|------|
| Create Task | Any authenticated user |
| View Task | Owner OR shared with user |
| Update Task | Owner only |
| Delete Task | Owner only |
| Share Task | Owner only |
| Revoke Share | Owner only |
| View Shares | Owner only |

## Rate Limiting

(Future implementation)

- **Authenticated requests**: 1000 requests per hour per user
- **Task creation**: 100 tasks per hour per user
- **Sharing operations**: 50 shares per hour per user

## Versioning

API version is included in the URL path (currently implicit v1).

Future versions will use explicit versioning:
- v1: `/api/v1/tasks`
- v2: `/api/v2/tasks`

## Interactive Documentation

View interactive API documentation at:
- **Development**: https://localhost:5001/swagger
- **Production**: https://api.todo-app.example.com/swagger

## Client Libraries

### TypeScript/Angular

```typescript
// Auto-generated from OpenAPI spec
import { TasksApi, TaskDto, CreateTaskDto } from './api';

const api = new TasksApi(configuration);

// Create task
const newTask: CreateTaskDto = {
  title: 'My new task',
  description: 'Task description'
};
const task = await api.createTask(newTask);

// List tasks
const tasks = await api.getTasks({ 
  status: 'InProgress', 
  page: 1, 
  pageSize: 20 
});
```

### C# Client

```csharp
// Auto-generated from OpenAPI spec
var api = new TasksApi(configuration);

// Create task
var newTask = new CreateTaskDto
{
    Title = "My new task",
    Description = "Task description"
};
var task = await api.CreateTaskAsync(newTask);

// List tasks
var tasks = await api.GetTasksAsync(
    status: TaskStatus.InProgress, 
    page: 1, 
    pageSize: 20
);
```

## Testing

### Contract Testing

Verify API implementation matches the OpenAPI specification:

```bash
# Install Dredd (API contract testing tool)
npm install -g dredd

# Run contract tests
dredd openapi.yaml https://localhost:5001
```

### Example Test Cases

1. **Create Task**: POST /api/tasks → 201 Created
2. **Invalid Title**: POST /api/tasks (empty title) → 400 Bad Request
3. **Unauthorized Access**: GET /api/tasks (no token) → 401 Unauthorized
4. **Update Non-Owned Task**: PUT /api/tasks/{id} → 403 Forbidden
5. **Share with Non-Existent User**: POST /api/tasks/{id}/shares → 404 Not Found
6. **Duplicate Share**: POST /api/tasks/{id}/shares (same user twice) → 409 Conflict

## Change Log

### Version 1.0.0 (2025-10-28)

- Initial API specification
- User profile endpoint
- Task CRUD operations
- Task sharing operations
- Shared tasks listing
- Pagination, filtering, and sorting support

## Future Enhancements

### Version 2.0 (Planned)

- Task comments
- File attachments
- Task tags/categories
- Task priority levels
- Advanced search/filtering
- Webhook notifications
- Batch operations
- Export/import tasks

## Support

For API support or questions, contact:
- **Email**: support@example.com
- **Documentation**: https://docs.todo-app.example.com
- **GitHub Issues**: https://github.com/org/todo-app/issues
