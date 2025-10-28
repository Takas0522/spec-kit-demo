# Research: Shared ToDo Management Application

**Date**: October 28, 2025  
**Phase**: 0 - Outline & Research

## Overview

This document consolidates research findings for implementing a shared task management application using Angular 20, ASP.NET Core 9.0, Azure SQL Database, and Azure Entra ID authentication.

## Technology Decisions

### 1. Frontend Framework: Angular 20 with Tailwind CSS

**Decision**: Use Angular 20.3.0 with Tailwind CSS 4.x for component-based UI development.

**Rationale**:
- **Component-based architecture**: Angular's component model aligns perfectly with requirement for reusable UI elements
- **Standalone components**: Angular 20 uses standalone components by default, reducing boilerplate
- **Built-in dependency injection**: Simplifies service management and testing
- **TypeScript-first**: Strong typing reduces runtime errors
- **Tailwind CSS integration**: Utility-first CSS framework provides rapid UI development with consistent design
- **Tailwind + Angular synergy**: Tailwind's utility classes work excellently with Angular's component isolation

**Alternatives Considered**:
- **React**: More flexible but requires additional libraries for routing, state management; steeper learning curve for team
- **Vue**: Lighter weight but smaller enterprise ecosystem
- **Bootstrap/Material**: Component libraries provide pre-built components but less customization flexibility than Tailwind

**Implementation Details**:
- Use Angular signals for reactive state management
- Implement lazy loading for feature modules
- Use RxJS for asynchronous operations (HTTP, real-time updates)
- Tailwind configuration for custom design system (colors, spacing, components)

---

### 2. Authentication: Azure Entra ID with MSAL

**Decision**: Use Microsoft Authentication Library (MSAL) for Angular and Microsoft.Identity.Web for ASP.NET Core to integrate with Azure Entra ID.

**Rationale**:
- **Enterprise-grade security**: Azure Entra ID provides OAuth 2.0/OpenID Connect compliance
- **Single Sign-On (SSO)**: Users can authenticate with organizational credentials
- **Token-based security**: JWT tokens for stateless API authentication
- **Official Microsoft libraries**: @azure/msal-browser and @azure/msal-angular are actively maintained
- **Seamless integration**: Microsoft.Identity.Web provides built-in middleware for token validation

**Alternatives Considered**:
- **Custom JWT implementation**: More control but higher security risk, maintenance burden
- **Third-party auth (Auth0, Okta)**: Additional cost, vendor lock-in
- **Azure AD B2C**: More complex setup, unnecessary for internal/organizational users

**Implementation Details**:
- **Frontend**: 
  - Use `@azure/msal-browser` v3+ and `@azure/msal-angular` v3+
  - Implement MSAL Guard for route protection
  - HTTP Interceptor for automatic token attachment
  - Handle token refresh automatically
- **Backend**:
  - Use `Microsoft.Identity.Web` v2+
  - Configure JWT Bearer authentication
  - Validate tokens against Azure Entra ID
  - Extract user claims (email, OID, name)

**Configuration Requirements**:
```typescript
// Frontend: environment.ts
export const environment = {
  msalConfig: {
    auth: {
      clientId: '<APP_CLIENT_ID>',
      authority: 'https://login.microsoftonline.com/<TENANT_ID>',
      redirectUri: 'http://localhost:4200'
    }
  },
  apiConfig: {
    scopes: ['api://<API_CLIENT_ID>/Tasks.ReadWrite'],
    uri: 'https://localhost:5001/api'
  }
};
```

```json
// Backend: appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<TENANT_ID>",
    "ClientId": "<API_CLIENT_ID>",
    "Audience": "api://<API_CLIENT_ID>"
  }
}
```

---

### 3. Backend API: ASP.NET Core 9.0 WebAPI

**Decision**: Use ASP.NET Core 9.0 with minimal API style for lightweight, high-performance REST API.

**Rationale**:
- **.NET 9.0 features**: Latest LTS version with performance improvements
- **Native JSON serialization**: System.Text.Json with source generators for optimal performance
- **Built-in OpenAPI support**: Automatic API documentation
- **Middleware pipeline**: Flexible request/response processing
- **Cross-platform**: Runs on Linux containers for Azure deployment

**Alternatives Considered**:
- **Node.js/Express**: JavaScript ecosystem but less type-safe
- **Python/FastAPI**: Excellent for ML/data but less enterprise C# ecosystem
- **Java/Spring Boot**: More verbose, heavier runtime

**Architecture Pattern**:
```
Controllers (HTTP endpoints)
    ↓
Services (Business logic)
    ↓
Repositories/DbContext (Data access)
    ↓
Database (Azure SQL)
```

**Best Practices**:
- Use DTOs (Data Transfer Objects) to decouple API contracts from domain models
- Implement async/await for all I/O operations
- Use ActionResult<T> for consistent response format
- Implement global exception handling middleware
- Configure CORS for Angular SPA
- Use API versioning for future compatibility

---

### 4. Data Access: Entity Framework Core 9.0 (Database-First)

**Decision**: Use Entity Framework Core 9.0 with a Database-First (schema-first) approach. Schema changes originate in the Azure SQL Database (or SSDT project) and EF Core entities/DbContext are regenerated via scaffolding.

**Rationale**:
- **Source of truth clarity**: Production schema defined and managed centrally (DB project / SQL scripts).
- **Reduced migration drift**: Avoids stale migrations when multiple contributors modify schema.
- **Rapid updates**: Scaffolding regenerates entity models quickly after schema changes.
- **Fits existing database tooling**: Aligns with Azure SQL project under `database/`.

**Alternatives Considered**:
- **Code-first migrations**: Flexible, but risk of uncoordinated schema updates & merge conflicts.
- **Dapper**: High performance, but manual SQL for complex relationships and less productivity.
- **Raw ADO.NET**: Maximum control; too verbose for business feature velocity.

**Implementation Strategy**:
- Use `dotnet ef dbcontext scaffold` with `--force --use-database-names --no-onconfiguring` to regenerate entities into `Data/Entities`.
- Extend entities via partial classes (e.g., `Task.partial.cs`) for domain logic & data annotations if needed.
- Apply relationship tuning & global filters via Fluent API configuration classes (`Configurations/*`).
- Soft delete implemented via boolean flag + query filters.
- Add a lightweight script `scripts/scaffold-db.sh` (future) to standardize scaffolding.

**Scaffolding Command Example**:
```bash
dotnet ef dbcontext scaffold "Server=localhost,1433;Database=TodoDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True" \
  Microsoft.EntityFrameworkCore.SqlServer \
  -o Data/Entities -c ApplicationDbContext --context-dir Data \
  --force --use-database-names --no-onconfiguring
```

**Configuration Sample (Fluent API)**:
```csharp
public partial class ApplicationDbContext : DbContext
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Task>(builder =>
    {
      builder.HasKey(t => t.Id);
      builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
      builder.HasQueryFilter(t => !t.IsDeleted);
    });
  }
}
```

---

### 5. Database: Azure SQL Database

**Decision**: Use Azure SQL Database (PaaS) with Basic/Standard tier for development/production.

**Rationale**:
- **Fully managed**: No server maintenance, automatic backups, patching
- **High availability**: Built-in redundancy, 99.99% SLA
- **Scalability**: Can scale up/down based on load
- **Security**: Built-in encryption, firewall rules, threat detection
- **Integration**: Native support with Azure services (Key Vault, Monitor)

**Alternatives Considered**:
- **PostgreSQL**: Open-source but less integrated with Azure ecosystem
- **CosmosDB**: NoSQL, overkill for relational data with ACID requirements
- **SQL Server on VM**: More control but higher operational cost

**Schema Design Principles**:
- Normalize to 3NF for data integrity
- Add indexes on foreign keys and frequently queried columns
- Use uniqueidentifier (GUID) for primary keys (distributed system friendly)
- Implement audit columns (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
- Use check constraints for data validation

**Performance Considerations**:
- Connection pooling enabled by default
- Use parameterized queries (EF Core handles this)
- Implement pagination for list queries
- Add indexes on Status, OwnerId, SharedWithUserId

---

### 6. Real-time Updates Strategy

**Decision**: Start with short polling (5-second intervals), migrate to SignalR if needed.

**Rationale**:
- **MVP simplicity**: Polling is straightforward to implement
- **Sufficient for requirements**: 5-second update visibility meets spec
- **No infrastructure complexity**: No WebSocket management
- **Easy migration path**: Can add SignalR later without breaking changes

**Alternatives Considered**:
- **SignalR immediately**: More complex setup, overkill for 100 concurrent users
- **Server-Sent Events (SSE)**: Unidirectional, good fit but less .NET ecosystem support
- **WebSockets**: Requires connection management, fallback handling

**Implementation**:
```typescript
// Frontend polling service
export class TaskPollingService {
  private pollInterval = 5000; // 5 seconds
  
  startPolling(taskId: string): Observable<Task> {
    return interval(this.pollInterval).pipe(
      switchMap(() => this.taskService.getTask(taskId)),
      distinctUntilChanged((prev, curr) => 
        prev.status === curr.status && prev.modifiedAt === curr.modifiedAt
      )
    );
  }
}
```

**Future Migration to SignalR**:
When concurrent users > 500 or update latency requirement < 2 seconds:
- Add `Microsoft.AspNetCore.SignalR` package
- Create TaskHub for broadcasting updates
- Replace polling with SignalR connection in frontend
- Maintain REST API for backward compatibility

---

### 7. API Design: RESTful with OpenAPI

**Decision**: Design RESTful API following REST principles with OpenAPI 3.0 specification.

**Rationale**:
- **Industry standard**: REST is widely understood, easy to consume
- **Stateless**: Each request contains all necessary information
- **HTTP verbs**: GET, POST, PUT, DELETE map naturally to CRUD
- **OpenAPI documentation**: Auto-generated, interactive API docs

**Endpoint Structure**:
```
# Authentication (handled by Azure Entra ID)
GET  /api/users/me              # Get current user profile

# Tasks
GET    /api/tasks               # List user's tasks
GET    /api/tasks/{id}          # Get task details
POST   /api/tasks               # Create task
PUT    /api/tasks/{id}          # Update task
DELETE /api/tasks/{id}          # Delete task
PATCH  /api/tasks/{id}/status   # Update status only

# Task Sharing
GET    /api/tasks/{id}/shares          # List task shares
POST   /api/tasks/{id}/shares          # Share task with user
DELETE /api/tasks/{id}/shares/{userId} # Revoke share

# Shared with me
GET    /api/shared-tasks        # List tasks shared with current user
```

**API Conventions**:
- Use plural nouns for resources (`/tasks`, not `/task`)
- Use HTTP status codes correctly (200, 201, 204, 400, 401, 403, 404, 500)
- Return consistent error format:
  ```json
  {
    "error": {
      "code": "TASK_NOT_FOUND",
      "message": "Task with ID 123 not found",
      "details": []
    }
  }
  ```
- Include pagination for list endpoints: `?page=1&pageSize=20`
- Support filtering: `?status=InProgress`
- Support sorting: `?sortBy=dueDate&sortOrder=desc`

---

### 8. Testing Strategy (Updated: Jest & Resilience)

**Decision**: Use Jest for frontend unit/integration tests; xUnit for backend; expand scope to include resilience testing (retry behavior & transient fault handling).

**Frontend Testing (Jest)**:
- **Unit Tests**: Pure component logic & pipes using Angular testing utilities.
- **Integration Tests**: Component + service interactions (e.g., task list fetch & render).
- **Snapshot Tests**: For presentational components (stable UI fragments).
- **Optional Future**: Playwright/Cypress for E2E flows (auth, task CRUD, sharing).

**Backend Testing (xUnit)**:
- **Unit Tests**: Services with mocked repositories.
- **Integration Tests**: Controllers via `WebApplicationFactory` & TestServer hitting in-memory or local test DB.
- **Resilience Tests**: Simulate transient SQL exceptions to verify EF retry policy + Polly HTTP policy (future external service).
- **Contract Tests**: Dredd or OpenAPI schema validation for response structures.

**Test Coverage Goals**:
- Backend services: >80% line coverage.
- Frontend core components/services: >70%.
- Critical paths (authentication, task status update, sharing): strive for 100%.

**Frontend Jest Example**:
```typescript
import { render, screen } from '@testing-library/angular';
import { TaskCardComponent } from './task-card.component';

describe('TaskCardComponent', () => {
  it('renders title & status badge', async () => {
    await render(TaskCardComponent, {
      componentProperties: {
        task: { id: '1', title: 'Test', description: '', status: 'NotStarted', createdAt: new Date(), modifiedAt: new Date(), ownerId: 'u', isDeleted: false }
      }
    });
    expect(screen.getByText('Test')).toBeInTheDocument();
    expect(screen.getByText('NotStarted')).toBeInTheDocument();
  });
});
```

**Backend Resilience Test Snippet**:
```csharp
[Fact]
public async Task TaskService_Retries_OnTransientSqlError()
{
    // Arrange: mock repository to throw transient exception first two calls then succeed
    // Assert: verify retry count & final success
}
```

---

### 9. Security Best Practices

**Decision**: Implement defense-in-depth security strategy.

**Authentication & Authorization**:
- ✅ Azure Entra ID for identity management
- ✅ JWT token validation on every API request
- ✅ Role-based access control (RBAC) for future admin features
- ✅ Token expiration and refresh handling

**API Security**:
- ✅ HTTPS only (redirect HTTP → HTTPS)
- ✅ CORS configuration limited to frontend domain
- ✅ Rate limiting to prevent abuse
- ✅ Input validation with data annotations
- ✅ SQL injection prevention via EF Core parameterized queries
- ✅ XSS prevention via Angular's built-in sanitization

**Data Security**:
- ✅ Encryption at rest (Azure SQL default)
- ✅ Encryption in transit (TLS 1.2+)
- ✅ Sensitive data in Azure Key Vault (connection strings, secrets)
- ✅ User data isolation (query filters)

**Authorization Rules**:
```csharp
// User can only access their own tasks
[Authorize]
public async Task<ActionResult<TaskDto>> GetTask(Guid id)
{
    var task = await _taskService.GetTaskByIdAsync(id, User.GetUserId());
    if (task == null) return NotFound();
    return Ok(task);
}

// User can only share their own tasks
[Authorize]
public async Task<ActionResult> ShareTask(Guid taskId, ShareTaskRequest request)
{
    var result = await _taskService.ShareTaskAsync(
        taskId, 
        User.GetUserId(), // owner check
        request.SharedWithUserId
    );
    return result.Success ? Ok() : Forbid();
}
```

---

### 10. Tailwind CSS Configuration for Angular

**Decision**: Configure Tailwind CSS 4.x with Angular 20's build system.

**Rationale**:
- **Utility-first**: Rapid UI development without writing custom CSS
- **Component-friendly**: Utility classes work perfectly with Angular components
- **Design consistency**: Predefined spacing, colors, typography
- **Tree-shaking**: Only used utilities included in production build
- **Responsive design**: Built-in breakpoint system

**Installation & Configuration**:
```bash
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init
```

**tailwind.config.js**:
```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#f0f9ff',
          // ... custom brand colors
          900: '#0c4a6e',
        },
      },
    },
  },
  plugins: [],
}
```

**styles.css**:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer components {
  .btn-primary {
    @apply bg-primary-600 text-white px-4 py-2 rounded-lg hover:bg-primary-700 transition-colors;
  }
  
  .task-card {
    @apply bg-white shadow-md rounded-lg p-4 border border-gray-200 hover:shadow-lg transition-shadow;
  }
}
```

**Component Example**:
```typescript
@Component({
  selector: 'app-task-card',
  template: `
    <div class="task-card">
      <h3 class="text-lg font-semibold text-gray-900">{{ task.title }}</h3>
      <p class="text-sm text-gray-600 mt-2">{{ task.description }}</p>
      <div class="flex items-center justify-between mt-4">
        <span [class]="getStatusClass()">{{ task.status }}</span>
        <button class="btn-primary">Edit</button>
      </div>
    </div>
  `
})
export class TaskCardComponent {
  @Input() task!: Task;
  
  getStatusClass(): string {
    const baseClasses = 'px-3 py-1 rounded-full text-xs font-medium';
    const statusClasses = {
      NotStarted: 'bg-gray-100 text-gray-800',
      InProgress: 'bg-blue-100 text-blue-800',
      Completed: 'bg-green-100 text-green-800'
    };
    return `${baseClasses} ${statusClasses[this.task.status]}`;
  }
}
```

---

### 11. Communication Resilience & Retry Policies (NEW)

**Decision**: Implement layered resilience using frontend exponential backoff + backend Polly + EF Core connection resiliency.

**Rationale**:
- Cloud network variability requires robust transient fault handling.
- Azure SQL transient errors (e.g., throttling) must not surface directly to end users.
- Avoid duplicated writes; treat create operations as idempotent via client-side GUIDs.

**Frontend**:
- GET list/status polling: 3 attempts, exponential backoff base 500ms with jitter.
- Mutating requests: no automatic retry; user-initiated reattempt.
- Abort long requests at 8s using `AbortController`.

**Backend**:
- EF Core: `EnableRetryOnFailure(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(3))`.
- Polly HTTP (future external calls): Wait+Retry (decorrelated jitter) + CircuitBreaker + Timeout.
- Do NOT retry authentication/authorization failures (401/403) or validation (400).

**Observability**:
- Log retry attempts with correlation IDs.
- Emit metrics: retry counts, circuit breaker open events, transient error categories.

**User Messaging (JP)**:
- Retrying: "一時的な通信エラーが発生しました。再試行しています…"
- Final failure: "通信に失敗しました。ネットワーク状態を確認し再度お試しください。"

**Security**:
- Prevent replay issues by using idempotent identifiers on creation.
- Avoid leaking internal error codes; map to generic user messages.

---

---

## Research Tasks Completed

### ✅ Azure Entra ID Integration Patterns
- Researched MSAL.js v3 authentication flow
- Investigated token refresh strategies
- Identified required Azure App Registration configuration
- Documented scope and permission setup

### ✅ ASP.NET Core 9.0 Best Practices
- Reviewed minimal API vs controller-based API (chose controllers for structure)
- Investigated middleware pipeline optimization
- Researched global exception handling patterns
- Explored OpenAPI configuration with Azure Entra ID security

### ✅ Entity Framework Core 9.0 Features
- Reviewed new EF Core 9 features (compiled models, raw SQL improvements)
- Investigated optimal DbContext configuration
- Researched migration strategies for Azure SQL
- Explored query filters for user data isolation

### ✅ Angular 20 + Tailwind Integration
- Confirmed Angular 20 standalone component approach
- Researched Tailwind 4.x integration with Angular CLI
- Investigated component composition patterns
- Reviewed Angular signal-based state management

### ✅ Real-time Update Strategies
- Compared polling vs SignalR vs SSE
- Identified appropriate polling intervals
- Researched SignalR migration path
- Investigated RxJS operators for efficient polling

### ✅ Security Best Practices
- Reviewed OWASP Top 10 for web applications
- Researched JWT token security
- Investigated CORS configuration best practices
- Explored rate limiting strategies

---

## Open Questions / Future Considerations

### Phase 2 Decisions (Not blocking MVP):
1. **Notification System**: Email vs Push vs In-app notifications for task sharing
2. **Task Comments**: Add commenting feature for shared tasks
3. **Task Attachments**: File upload/download support
4. **Advanced Filtering**: Date ranges, tags, priority levels
5. **Audit Logging**: Detailed activity log for compliance
6. **Offline Support**: PWA capabilities with service workers
7. **Batch Operations**: Bulk status updates, bulk sharing

### Performance Optimization (If needed):
1. **Caching Strategy**: Redis for frequently accessed data
2. **CDN**: Static asset delivery optimization
3. **Database Optimization**: Query plan analysis, index tuning
4. **API Response Compression**: Gzip/Brotli for large payloads

### DevOps Considerations:
1. **CI/CD Pipeline**: Azure DevOps or GitHub Actions
2. **Infrastructure as Code**: Bicep or Terraform for Azure resources
3. **Monitoring**: Application Insights, Log Analytics
4. **Deployment Strategy**: Blue-green or canary deployments

---

## Conclusion

All technical decisions have been made with clear rationale. The technology stack is well-defined:
- **Frontend**: Angular 20 + Tailwind CSS + MSAL
- **Backend**: ASP.NET Core 9.0 + Entity Framework Core
- **Database**: Azure SQL Database
- **Authentication**: Azure Entra ID

All unknowns from Technical Context have been resolved. Ready to proceed to **Phase 1: Design & Contracts**.
