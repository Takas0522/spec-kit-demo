````markdown
# Implementation Plan: Shared ToDo Management Application

**Branch**: `001-shared-todo-app` | **Date**: October 28, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-shared-todo-app/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

A shared task management application that allows authenticated users to create, manage, and share tasks with different status levels (Not Started, In Progress, Completed). Users can share their tasks with other users for read-only visibility while maintaining full control over their own tasks. The application uses Angular 20 with Tailwind CSS for the frontend, ASP.NET Core 9.0 WebAPI for the backend, Azure SQL Database for storage, and Azure Entra ID (via MSAL) for authentication

## Technical Context

**Frontend**:
- Language/Version: TypeScript 5.9.2 / Angular 20.3.0
- Primary Dependencies: 
  - @azure/msal-browser / @azure/msal-angular (Azure Entra ID authentication)
  - Tailwind CSS 4.x (utility-first CSS framework)
  - RxJS 7.8 (reactive programming)
  - Angular Signals (state management)
  - Axios (optional; if fetch enhancements required) or native fetch
- Testing: Jest (ts-jest + @types/jest). Jasmine/Karma removed to reduce tooling surface; faster watch mode, richer assertion ecosystem.
- Target Platform: Modern browsers (Chrome, Firefox, Edge, Safari)

**Backend**:
- Language/Version: C# / .NET 9.0
- Primary Dependencies:
  - Microsoft.AspNetCore.OpenApi 9.0.10
  - Microsoft.Identity.Web (Azure Entra ID validation)
  - Microsoft.EntityFrameworkCore.SqlServer (Azure SQL)
  - Microsoft.AspNetCore.Authentication.JwtBearer
  - Polly (transient fault handling & resilience policies)
- Testing: xUnit (unit + integration) / Possible future contract tests with Dredd
- Target Platform: Linux containers / Azure App Service

**Storage**: Azure SQL Database (managed PaaS)
- Entity Framework Core 9.0 for ORM
- Database-First approach (Azure SQL schema is source of truth). Use EF scaffolding commands to regenerate entities/DbContext upon schema changes.
  - Command: `dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c ApplicationDbContext --context-dir Data --force --use-database-names --no-onconfiguring`
  - Do NOT manually edit generated entity files; extend via partial classes + Fluent API.

**Authentication**: Azure Entra ID (formerly Azure AD)
- MSAL.js for frontend token acquisition
- Microsoft.Identity.Web for backend token validation
- OAuth 2.0 / OpenID Connect flow

**Project Type**: Web application (SPA + REST API)

**Performance Goals**: 
- API response time: <200ms p95 for CRUD operations
- Frontend initial load: <3 seconds
- Task status updates visible within 5 seconds
- Support 100 concurrent users

**Constraints**: 
- Real-time updates via polling (initial implementation) or SignalR (future)
- CORS configuration for SPA-API communication
- Token refresh handling for long sessions
- Responsive design for mobile/tablet/desktop
 - Resilience required for unreliable networks (cloud environment)

**Scale/Scope**: 
- MVP for 100+ users
- ~15 API endpoints
- ~10 Angular components
- 4 database tables (Users, Tasks, TaskShares, audit tables)
 - Retry policies must keep user-perceived error rate low (<2% transient failures surfacing)

### Communication & Resilience Strategy

**Goals**:
- Minimize user disruption due to transient network/database faults.
- Ensure idempotent operations (task creation, sharing) are either committed once or clearly surfaced.

**Frontend Retry & Timeout Policy**:
- Use exponential backoff with jitter for non-mutating GET requests (e.g., task polling, shared tasks list).
- Default timeouts: 8s per request; abort with AbortController.
- Max attempts: 3 (initial + 2 retries) for GET; no automatic retry for mutating POST/PUT/PATCH/DELETE (user-initiated reattempt required) except share revocation idempotent DELETE (may retry once if 5xx/timeout occurs).
- Backoff schedule example: 500ms, 1500ms (adding jitter ±200ms).
- Cache successful GET responses in memory to prevent stale UI while retrying.

**Backend HTTP Client (if calling external services in future)**:
- Use Polly policies: 
  - `WaitAndRetryAsync` for transient network exceptions & 5xx (except 501/505) with decorrelated jitter (e.g., 200ms → 1s → 2.2s).
  - `CircuitBreakerAsync`: break after 5 consecutive failures for 30s to prevent cascading failure.
  - `TimeoutPolicy`: 10s hard timeout.
  - `FallbackPolicy`: return standardized error envelope.

**Database Resilience**:
- Enable EF Core resilient execution (`EnableRetryOnFailure`) with: max 5 retries, base delay 200ms, max 3s.
- Retries target Azure SQL transient errors (e.g., error codes: 40613, 40197, 40501, 10928, 10929). 
- Idempotency: Insert operations use GUID set client-side to avoid duplicates on retry.

**Concurrency Handling**:
- Use optimistic concurrency tokens (`rowversion` future enhancement) if frequent concurrent edits emerge.
- For now, last-write-wins with updated `ModifiedAt` timestamp.

**Error Surfacing**:
- User-friendly messages: “一時的な通信エラーが発生しました。再試行しています…”
- After final failure: “通信に失敗しました。ネットワーク状態を確認し再度お試しください。”

**Logging & Metrics**:
- Log each retry attempt with correlation ID.
- Metrics: retry count distribution, circuit breaker open events, EF transient error frequency.

**Security Consideration**:
- Avoid retrying 401/403 (authorization/authentication failures). Prompt re-auth.
- Do not retry 4xx validation errors.

**Sample Polly Registration (Backend)**:
```csharp
builder.Services.AddHttpClient("Default")
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(3))));
```

**Frontend Retry Helper (Example)**:
```typescript
async function fetchWithRetry(url: string, options: RequestInit = {}, attempts = 3): Promise<Response> {
  let delay = 500;
  for (let i = 0; i < attempts; i++) {
    try {
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 8000);
      const resp = await fetch(url, { ...options, signal: controller.signal });
      clearTimeout(timeout);
      if (resp.ok || (resp.status >= 400 && resp.status < 500)) return resp; // no retry on client errors
      throw new Error(`Server error ${resp.status}`);
    } catch (err) {
      if (i === attempts - 1) throw err;
      const jitter = Math.random() * 200;
      await new Promise(r => setTimeout(r, delay + jitter));
      delay *= 3; // exponential
    }
  }
  throw new Error('Unhandled retry termination');
}
```

### Testing Adjustments
- Frontend: Jest configured with `ts-jest`; snapshot tests for presentational components, DOM testing via `@testing-library/angular` (optional future addition).
- Remove Karma-specific config & scripts; ensure `npm test` invokes Jest.
- Backend: Introduce resilience tests (simulate transient SQL exception and verify EF retry). Use `Respawn` library for test DB reset (future).

### Tooling Implications
- Continuous scaffolding required for schema changes: add script `scripts/scaffold-db.sh` (future).
- Git ignore generated EF entity folder if regenerated frequently; treat partials and configuration classes as hand-maintained.


## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ⚠️ Constitution file is a template - no specific principles defined yet for this project.

**Action**: Proceeding with standard web application best practices:
- Component-based architecture (Angular components, .NET services)
- REST API with OpenAPI documentation
- Entity Framework Core for data access
- Comprehensive testing strategy (unit + integration)
- Security-first approach with Azure Entra ID

**Note**: If a project-specific constitution exists with mandatory principles (e.g., TDD, library-first), those should be documented here and gates enforced.

## Project Structure

### Documentation (this feature)

```text
specs/001-shared-todo-app/
├── spec.md              # Feature specification (already exists)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command) - TO BE CREATED
├── data-model.md        # Phase 1 output (/speckit.plan command) - TO BE CREATED
├── quickstart.md        # Phase 1 output (/speckit.plan command) - TO BE CREATED
├── contracts/           # Phase 1 output (/speckit.plan command) - TO BE CREATED
│   ├── openapi.yaml    # REST API OpenAPI 3.0 specification
│   └── README.md       # Contract documentation
├── checklists/          # Already exists
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── front/                    # Angular 20 SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/             # Singleton services, guards, interceptors
│   │   │   │   ├── auth/         # MSAL authentication
│   │   │   │   ├── services/     # API clients, state management
│   │   │   │   └── guards/       # Route guards
│   │   │   ├── features/         # Feature modules
│   │   │   │   ├── tasks/        # Task management components
│   │   │   │   ├── shared-tasks/ # Shared tasks view
│   │   │   │   └── auth/         # Login/logout components
│   │   │   ├── shared/           # Shared components, pipes, directives
│   │   │   ├── models/           # TypeScript interfaces/types
│   │   │   ├── app.config.ts
│   │   │   ├── app.routes.ts
│   │   │   └── app.ts
│   │   ├── styles.css           # Tailwind CSS configuration
│   │   └── index.html
│   ├── tailwind.config.js       # TO BE CREATED
│   ├── angular.json
│   ├── package.json
│   └── tsconfig.json
│
├── api/                          # ASP.NET Core 9.0 WebAPI
│   ├── Controllers/              # API endpoints
│   │   ├── TasksController.cs
│   │   ├── TaskSharesController.cs
│   │   └── UsersController.cs
│   ├── Models/                   # Domain models / DTOs
│   │   ├── Task.cs
│   │   ├── TaskShare.cs
│   │   ├── User.cs
│   │   └── DTOs/
│   ├── Data/                     # EF Core DbContext and configurations
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Services/                 # Business logic
│   │   ├── ITaskService.cs
│   │   ├── TaskService.cs
│   │   ├── ITaskShareService.cs
│   │   └── TaskShareService.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── api.csproj
│
└── database/                     # SQL project (already exists)
    ├── Tables/                   # TO BE CREATED
    │   ├── Users.sql
    │   ├── Tasks.sql
    │   └── TaskShares.sql
    ├── postDeployment.sql
    └── ToDo.sqlproj

tests/
├── api.tests/                    # Backend unit tests
│   ├── Controllers/
│   ├── Services/
│   └── api.tests.csproj
└── front/                        # Frontend tests (Jest)
    └── src/app/
        └── [component].spec.ts
```

**Structure Decision**: Web application structure selected based on feature requirements. The application is split into three main projects:
1. **front/**: Angular SPA with component-based architecture and Tailwind CSS
2. **api/**: ASP.NET Core WebAPI following clean architecture principles (Controllers → Services → Data)
3. **database/**: SQL Server database project for schema management

This structure supports clear separation of concerns, independent deployment, and aligns with the specified technology stack.

## Complexity Tracking

**Status**: No constitution violations detected.

The architecture follows standard web application patterns:
- Three-tier architecture (Frontend, API, Database)
- Standard authentication with Azure Entra ID
- RESTful API design
- Component-based UI with established framework (Angular)
- ORM pattern with Entity Framework Core

All complexity is justified by requirements and follows industry best practices for the chosen technology stack.

## API Communication Verification (VS Code REST Client)

To validate WebAPI endpoints locally and during development, the project uses the VS Code REST Client extension with the `api.http` file located at `src/api/api.http`.

### Goals
- Provide quick, reproducible manual checks for HTTP status codes, payloads, auth flows.
- Allow developers to iterate on endpoints without leaving the editor.
- Support environment-based host substitution and token injection.

### File: `src/api/api.http`
Current sample:
```http
@api_HostAddress = http://localhost:5182

GET {{api_HostAddress}}/weatherforecast/
Accept: application/json
```

### Planned Enhancements
Add endpoints mirroring the OpenAPI spec for tasks and shares:
```http
### Get own profile
GET {{api_HostAddress}}/api/users/me
Authorization: Bearer {{access_token}}
Accept: application/json

### List tasks
GET {{api_HostAddress}}/api/tasks?page=1&pageSize=20
Authorization: Bearer {{access_token}}
Accept: application/json

### Create task
POST {{api_HostAddress}}/api/tasks
Authorization: Bearer {{access_token}}
Content-Type: application/json
Accept: application/json

{
  "title": "Sample Task",
  "description": "Created via REST Client",
  "dueDate": "2025-10-31T00:00:00Z"
}

### Share task
POST {{api_HostAddress}}/api/tasks/{{taskId}}/share
Authorization: Bearer {{access_token}}
Content-Type: application/json
Accept: application/json

{
  "sharedWithUserEmail": "user2@example.com"
}
```

### Environment Variables
Create an accompanying environment file (optional) `src/api/http-client.env.json`:
```json
{
  "dev": {
    "api_HostAddress": "https://localhost:5001",
    "access_token": "<PASTE_JWT_TOKEN>"
  }
}
```
Switch environment at top of `.http` file using:
```http
@env=dev
```

### Usage Workflow
1. Run API: `dotnet run` (HTTPS https://localhost:5001 by default if configured).
2. Acquire token via frontend or Azure CLI (e.g., `az account get-access-token --resource api://todo-api`).
3. Paste token into `http-client.env.json` under `access_token`.
4. Open `api.http`; click `Send Request` above desired request.
5. Inspect response pane: verify headers, status, JSON shape vs `openapi.yaml`.

### Testing Auth & Error Cases
- Omit `Authorization` header → expect 401.
- Use expired token → expect 401 (WWW-Authenticate challenge).
- Submit invalid payload (e.g., missing title) → expect 400 with validation details.

### Integration With Plan
This manual verification layer complements automated tests and accelerates endpoint iteration before writing formal test cases.

### Future Enhancements
- Generate `api.http` automatically from `openapi.yaml`.
- Add pre-script section to fetch token programmatically.
- Include performance test snippet (multiple sequential requests) for quick latency eyeballing.
````
