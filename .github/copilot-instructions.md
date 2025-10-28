# workspace Development Guidelines# workspace Development Guidelines



Auto-generated from all feature plans. Last updated: 2025-10-28Auto-generated from all feature plans. Last updated: 2025-10-28



## Active Technologies## Active Technologies



### Frontend (001-shared-todo-app)- (001-shared-todo-app)

- Angular 20.3.0 with TypeScript 5.9.2

- Tailwind CSS 4.x for styling## Project Structure

- MSAL (Microsoft Authentication Library) for Azure Entra ID authentication

- signal for reactive programming

- Jest for testing

### Backend (001-shared-todo-app)```

- ASP.NET Core 9.0 WebAPI with C#

- Entity Framework Core 9.0 for ORM
  - database-first migrations

- Microsoft.Identity.Web for Azure Entra ID token validation

- Azure SQL Database (managed PaaS)
  - local development with Azure SQL Edge

- xUnit for testing

## Code Style

### Authentication

- Azure Entra ID (formerly Azure AD): Follow standard conventions

- OAuth 2.0 / OpenID Connect flow

- JWT Bearer token authentication## Recent Changes



## Project Structure- 001-shared-todo-app: Added



```text<!-- MANUAL ADDITIONS START -->

src/<!-- MANUAL ADDITIONS END -->

├── front/                    # Angular 20 SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/        # Singleton services, guards, interceptors
│   │   │   ├── features/    # Feature modules (tasks, shared-tasks, auth)
│   │   │   ├── shared/      # Shared components, pipes, directives
│   │   │   └── models/      # TypeScript interfaces/types
│   │   └── styles.css       # Tailwind CSS configuration
│   └── tailwind.config.js
│
├── api/                      # ASP.NET Core 9.0 WebAPI
│   ├── Controllers/          # API endpoints
│   ├── Models/               # Domain models / DTOs
│   ├── Data/                 # EF Core DbContext
│   ├── Services/             # Business logic
│   └── Program.cs
│
└── database/                 # SQL Server database project
    ├── Tables/
    └── postDeployment.sql

tests/
├── api.tests/                # Backend unit tests
  └── front/                    # Frontend tests (Jest)

specs/001-shared-todo-app/
├── spec.md                   # Feature specification
├── plan.md                   # Implementation plan
├── research.md               # Technical research
├── data-model.md             # Database schema
├── quickstart.md             # Development guide
└── contracts/                # API contracts
    ├── openapi.yaml
    └── README.md
```

## Commands

### Frontend Development
```bash
cd src/front
npm install              # Install dependencies
npm start                # Run dev server (http://localhost:4200)
npm test                 # Run unit tests
npm run build            # Build for production
```

### Backend Development
```bash
cd src/api
dotnet restore           # Restore packages
dotnet run               # Run API (https://localhost:5001)
dotnet test              # Run tests
```

### Database Reverse Engineering (Database-First)

# 既存データベースから DbContext + エンティティを生成
cd src/api
dotnet ef dbcontext scaffold "Server=localhost,1433;Database=TodoDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring

### スキーマ変更手順
# 1. DBを変更 (SSDT / 手動 / スクリプト)
# 2. 再スキャフォールドでモデル更新
cd src/api
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring

### 初期スナップショット (必要時のみ)
cd src/api
dotnet ef migrations add InitialSnapshot --ignore-changes

### 差分スクリプト生成 (マイグレーション利用時)
dotnet ef migrations script -o Migrations.sql

### 注意
* 自動生成コード(Data/Entities)は直接編集せず、Partialクラスや Fluent API で拡張
* DBがソースオブトゥルース。コード側追加は DB反映後に再スキャフォールド
* --force で上書き。手動変更したファイルは分離 (Partial / 独自ディレクトリ)

## Code Style

### Angular/TypeScript
- Use standalone components (Angular 20 default)
- Follow Angular style guide (https://angular.dev/style-guide)
- Use Tailwind utility classes for styling
- Implement reactive patterns with RxJS
- Use Angular signals for state management
- Component structure: Smart (container) + Dumb (presentational) components

### C#/.NET
- Follow C# coding conventions (https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use async/await for all I/O operations
- Use ActionResult<T> for API responses
- Implement DTOs for API contracts
- Use Entity Framework Core for data access
- Follow repository pattern for data access layer

### API Design
- RESTful endpoints with standard HTTP verbs (GET, POST, PUT, DELETE, PATCH)
- Use consistent error response format
- Implement pagination for list endpoints
- Follow OpenAPI specification in specs/001-shared-todo-app/contracts/openapi.yaml

### Database
- Database-first (Azure SQL is source of truth; scaffold entities)
- Re-scaffold with: `dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring`
- Configure entity relationships with Fluent API (partial classes)
- Soft delete via `IsDeleted` flag
- Audit columns (`CreatedAt`, `ModifiedAt`) maintained via triggers / app logic
- GUID primary keys

## Authentication & Authorization

- All API endpoints require Azure Entra ID authentication
- Use MSAL Angular wrapper for token acquisition in frontend
- Use Microsoft.Identity.Web for token validation in backend
- Required scope: `api://todo-api/Tasks.ReadWrite`
- Authorization rules:
  - Users can only access their own tasks or tasks shared with them
  - Only task owners can update/delete/share tasks
  - Shared users have read-only access

## Data Model

### Entities
- **User**: Authenticated user with Azure Entra ID integration
- **Task**: Todo item with title, description, status (NotStarted/InProgress/Completed), due date
- **TaskShare**: Sharing relationship between task owner and recipient

### Relationships
- User 1:N Tasks (owner)
- User 1:N TaskShares (sharer and sharee)
- Task 1:N TaskShares

## Recent Changes

- Switched frontend testing stack from Jasmine/Karma to Jest + ts-jest
- Adopted database-first EF Core strategy (schema authoritative in SQL project)
- Added resilience strategy (Polly HTTP retries, circuit breaker, EF automatic transient retries, frontend fetch exponential backoff)
- Updated quickstart guide for Jest and scaffolding workflow
## Resilience Strategy Summary

### Backend
- Polly policies: WaitAndRetry (exponential-ish), CircuitBreaker, Timeout on HttpClient
- EF Core `EnableRetryOnFailure` for transient SQL errors
- Structured logging for retry attempts and circuit breaker state changes

```csharp
builder.Services.AddHttpClient("Default")
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(new[] {
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    }))
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
    .AddTransientHttpErrorPolicy(p => p.TimeoutAsync(TimeSpan.FromSeconds(10)));

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null))
);
```

### Frontend
Reusable fetch helper with exponential backoff + jitter for transient 5xx errors.

```ts
export async function fetchWithRetry(url: string, init: RequestInit = {}, retries = 3): Promise<Response> {
  let attempt = 0;
  while (true) {
    try {
      const res = await fetch(url, init);
      if (!res.ok && res.status >= 500 && attempt < retries) throw new Error(`HTTP ${res.status}`);
      return res;
    } catch (err) {
      if (attempt >= retries) throw err;
      const delay = Math.pow(2, attempt) * 200 + Math.random() * 150;
      await new Promise(r => setTimeout(r, delay));
      attempt++;
    }
  }
}
```

### Observability
- Log correlation IDs per request
- Capture retry attempt count and final outcome
- Emit metrics: `http_retry_total`, `circuit_breaker_open` (future Application Insights / Prometheus integration)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
