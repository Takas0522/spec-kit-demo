# Quick Start Guide: Shared ToDo Management Application

**Last Updated**: October 28, 2025  
**Version**: 1.0.0

## Overview

This guide will help you set up and run the Shared ToDo Management application locally for development.

## Prerequisites

### Required Software

- **.NET SDK 9.0+**: [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+**: [Download](https://nodejs.org/)
- **Azure SQL Database** or **SQL Server 2019+**
- **Azure Entra ID Tenant** (for authentication)
- **Git**: For version control

### Development Tools (Recommended)

- **Visual Studio Code** with extensions:
  - C# Dev Kit
  - Angular Language Service
  - Tailwind CSS IntelliSense
  - REST Client
- **Azure Data Studio** or **SQL Server Management Studio**: For database management
- **Postman** or **Insomnia**: For API testing (optional)

## Project Structure

```
/workspace
├── src/
│   ├── front/          # Angular frontend
│   ├── api/            # ASP.NET Core WebAPI
│   └── database/       # SQL Server database project
├── specs/
│   └── 001-shared-todo-app/
│       ├── spec.md
│       ├── plan.md
│       ├── research.md
│       ├── data-model.md
│       └── contracts/
└── tests/
    ├── api.tests/      # Backend tests
    └── front/          # Frontend tests
```

## Setup Instructions

### 1. Clone Repository

```bash
git clone <repository-url>
cd workspace
git checkout 001-shared-todo-app
```

### 2. Azure Entra ID Configuration

#### Create App Registrations

You need two app registrations: one for the SPA (frontend) and one for the API (backend).

**A. Create API App Registration**

1. Go to [Azure Portal](https://portal.azure.com) → Azure Entra ID → App registrations
2. Click "New registration"
   - Name: `ToDo API`
   - Supported account types: `Accounts in this organizational directory only`
   - Redirect URI: (leave blank)
3. Click "Register"
4. Note down:
   - **Application (client) ID**: `<API_CLIENT_ID>`
   - **Directory (tenant) ID**: `<TENANT_ID>`
5. Go to "Expose an API"
   - Click "Add a scope"
   - Application ID URI: `api://todo-api` (or accept default)
   - Scope name: `Tasks.ReadWrite`
   - Who can consent: `Admins and users`
   - Admin consent display name: `Read and write tasks`
   - Admin consent description: `Allows the app to read and write tasks on your behalf`
   - Save
6. Note down:
   - **Application ID URI**: `api://todo-api`

**B. Create Frontend App Registration**

1. Azure Portal → Azure Entra ID → App registrations → New registration
   - Name: `ToDo Frontend`
   - Supported account types: `Accounts in this organizational directory only`
   - Redirect URI: 
     - Platform: `Single-page application (SPA)`
     - URI: `http://localhost:4200`
2. Click "Register"
3. Note down:
   - **Application (client) ID**: `<FRONTEND_CLIENT_ID>`
4. Go to "API permissions"
   - Click "Add a permission"
   - Select "My APIs" → "ToDo API"
   - Select "Tasks.ReadWrite"
   - Click "Add permissions"
   - Click "Grant admin consent for [tenant]"

### 3. Database Setup

#### Option A: Azure SQL Database (Recommended)

1. Create Azure SQL Database:
   ```bash
   # Using Azure CLI
   az sql server create \
     --name todo-sql-server \
     --resource-group todo-rg \
     --location eastus \
     --admin-user sqladmin \
     --admin-password <StrongPassword123!>
   
   az sql db create \
     --resource-group todo-rg \
     --server todo-sql-server \
     --name TodoDB \
     --service-objective S0
   ```

2. Configure firewall to allow your IP:
   ```bash
   az sql server firewall-rule create \
     --resource-group todo-rg \
     --server todo-sql-server \
     --name AllowMyIP \
     --start-ip-address <your-ip> \
     --end-ip-address <your-ip>
   ```

3. Get connection string:
   ```
   Server=tcp:todo-sql-server.database.windows.net,1433;
   Database=TodoDB;
   User ID=sqladmin;
   Password=<StrongPassword123!>;
   Encrypt=True;
   TrustServerCertificate=False;
   Connection Timeout=30;
   ```

#### Option B: Local SQL Server

1. Install SQL Server 2019+ or use Docker:
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
     -p 1433:1433 --name sql-server \
     -d mcr.microsoft.com/mssql/server:2019-latest
   ```

2. Connection string:
   ```
   Server=localhost,1433;
   Database=TodoDB;
   User ID=sa;
   Password=YourStrong@Passw0rd;
   Encrypt=False;
   ```

dotnet restore
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=TodoDB;User ID=sa;Password=YourStrong@Passw0rd"
dotnet user-secrets set "AzureAd:TenantId" "<TENANT_ID>"
dotnet user-secrets set "AzureAd:ClientId" "<API_CLIENT_ID>"
dotnet run
### 4. Backend Setup (ASP.NET Core API)

#### Install Dependencies

```bash
cd src/api
dotnet restore
```

#### Configure Settings

Create or update `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TodoDB;User ID=sa;Password=YourStrong@Passw0rd;Encrypt=False;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<TENANT_ID>",
    "ClientId": "<API_CLIENT_ID>",
    "Audience": "api://todo-api"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

**⚠️ Security**: Never commit `appsettings.Development.json` with real credentials. Use User Secrets or environment variables.

#### Using User Secrets (Recommended)

```bash
cd src/api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=TodoDB;User ID=sa;Password=YourStrong@Passw0rd"
dotnet user-secrets set "AzureAd:TenantId" "<TENANT_ID>"
dotnet user-secrets set "AzureAd:ClientId" "<API_CLIENT_ID>"
```

#### Database-First Entity Scaffolding

This project uses a database-first approach. The database schema (tables, columns, relationships) is the source of truth. Whenever the schema changes, re-scaffold the EF Core entities.

1. Ensure the target database (`TodoDB`) exists with the latest schema.
2. Install EF Core CLI (if not installed):
   ```bash
   dotnet tool install --global dotnet-ef
   ```
3. Scaffold the DbContext and entity classes:
   ```bash
   cd src/api
   dotnet ef dbcontext scaffold "Server=localhost,1433;Database=TodoDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer \
     -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring
   ```
4. Extend behavior using partial classes or Fluent API (avoid editing generated files directly).

If using Azure SQL, substitute the connection string accordingly.

#### Run API

```bash
cd src/api
dotnet run
```

API will be available at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000
- Swagger UI: https://localhost:5001/swagger

#### Resilience & Retry (Backend)

Add resilient HTTP and database access in `Program.cs`:

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
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null))
);
```

Log retry attempts and circuit breaker state changes for observability.

### 5. Frontend Setup (Angular)

#### Install Dependencies

```bash
cd src/front
npm install
```

#### Install Additional Packages

```bash
# Install MSAL for authentication
npm install @azure/msal-browser @azure/msal-angular

# Install Tailwind CSS
npm install tailwindcss @tailwindcss/postcss postcss --force

# (Migration) Remove Jasmine/Karma test stack once Jest is configured
npm remove karma karma-chrome-launcher karma-coverage karma-jasmine karma-jasmine-html-reporter jasmine-core @types/jasmine

# Install Jest + Testing Library
npm install -D jest @types/jest ts-jest jest-environment-jsdom @testing-library/angular @testing-library/jest-dom
```

Add `jest.config.ts` (see Testing section) and update `package.json` test script to:

add `.postcssrc.json`
``` json
{
  "plugins": {
    "@tailwindcss/postcss": {}
  }
}
```

```json
"test": "jest"
```

#### Configure Tailwind CSS

Update `tailwind.config.js`:

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
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
        },
      },
    },
  },
  plugins: [],
}
```

Update `src/styles.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer components {
  .btn-primary {
    @apply bg-primary-600 text-white px-4 py-2 rounded-lg hover:bg-primary-700 transition-colors font-medium;
  }
  
  .btn-secondary {
    @apply bg-gray-200 text-gray-800 px-4 py-2 rounded-lg hover:bg-gray-300 transition-colors font-medium;
  }
  
  .input-field {
    @apply w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent;
  }
  
  .card {
    @apply bg-white shadow-md rounded-lg p-6 border border-gray-200;
  }
}
```

#### Configure Environment

Create `src/environments/environment.development.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  msalConfig: {
    auth: {
      clientId: '<FRONTEND_CLIENT_ID>',
      authority: 'https://login.microsoftonline.com/<TENANT_ID>',
      redirectUri: 'http://localhost:4200',
      postLogoutRedirectUri: 'http://localhost:4200'
    },
    cache: {
      cacheLocation: 'localStorage',
      storeAuthStateInCookie: false
    }
  },
  apiConfig: {
    scopes: ['api://todo-api/Tasks.ReadWrite'],
    uri: 'https://localhost:5001/api'
  }
};
```

Create `src/environments/environment.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.todo-app.example.com/api',
  msalConfig: {
    auth: {
      clientId: '<FRONTEND_CLIENT_ID>',
      authority: 'https://login.microsoftonline.com/<TENANT_ID>',
      redirectUri: 'https://todo-app.example.com',
      postLogoutRedirectUri: 'https://todo-app.example.com'
    },
    cache: {
      cacheLocation: 'localStorage',
      storeAuthStateInCookie: false
    }
  },
  apiConfig: {
    scopes: ['api://todo-api/Tasks.ReadWrite'],
    uri: 'https://api.todo-app.example.com/api'
  }
};
```

#### Run Frontend

```bash
cd src/front
npm start
```

Frontend will be available at: http://localhost:4200

## Verification

### 1. Check API Health

```bash
curl https://localhost:5001/swagger -k
# Should return Swagger UI HTML

curl https://localhost:5001/api/users/me -k \
  -H "Authorization: Bearer <token>"
# Should return 401 (without token) or user profile (with valid token)
```

### 2. Check Database Connection

dotnet ef database update --verbose
```bash
cd src/api
# Re-scaffold after schema change (example)
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring
```

### 3. Test Authentication Flow

1. Open http://localhost:4200 in browser
2. Click "Sign In" button
3. Authenticate with Azure Entra ID credentials
4. Verify redirect back to application
5. Check browser console for access token

### 4. Test API with Postman

**Get Access Token**:
1. Postman → Authorization tab
2. Type: OAuth 2.0
3. Configure:
   - Grant Type: Implicit
   - Auth URL: `https://login.microsoftonline.com/<TENANT_ID>/oauth2/v2.0/authorize`
   - Client ID: `<FRONTEND_CLIENT_ID>`
   - Scope: `api://todo-api/Tasks.ReadWrite`
4. Get New Access Token
5. Use Token

**Create Task**:
```http
POST https://localhost:5001/api/tasks
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Test Task",
  "description": "This is a test task"
}
```

Expected: 201 Created with task object

### 5. Verify Endpoints with VS Code REST Client

This project includes `src/api/api.http` for quick in-editor HTTP requests.

#### Setup Environment File (Optional)
Create `src/api/http-client.env.json`:
```json
{
  "dev": {
    "api_HostAddress": "https://localhost:5001",
    "access_token": "<PASTE_JWT_TOKEN>"
  }
}
```

Add to `.gitignore` if tokens are included.

#### Sample Requests (`api.http`)
```http
@api_HostAddress = https://localhost:5001

### Get current user profile
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

#### Usage Steps
1. Acquire JWT access token (frontend login or Azure CLI).
2. Paste into `http-client.env.json` under `access_token`.
3. Open `api.http` and click "Send Request" above a request line.
4. Confirm response matches `contracts/openapi.yaml` schemas.
5. Test error cases: remove `Authorization` header (401), invalid payload (400), request deleted task (404).

#### Tips
- Do not commit tokens.
- For quick token retrieval via Azure CLI (if configured):
  ```bash
  az account get-access-token --resource api://todo-api --query accessToken -o tsv
  ```
- Consider adding a script to auto-inject token in future iterations.

## Common Issues

### Issue: API returns 401 Unauthorized

**Solution**:
- Verify token is valid (check expiration)
- Verify `AzureAd` configuration in `appsettings.json`
- Verify token audience matches API's `ClientId`
- Check token scopes include `Tasks.ReadWrite`

### Issue: CORS error in browser

**Solution**:
- Verify `Cors:AllowedOrigins` includes `http://localhost:4200`
- Check API CORS middleware is configured in `Program.cs`
- Clear browser cache

### Issue: Database connection fails

**Solution**:
- Verify SQL Server is running
- Check connection string credentials
- Test connection with Azure Data Studio or SSMS
- Check firewall rules (Azure SQL)

### Issue: Frontend build errors

**Solution**:
```bash
cd src/front
rm -rf node_modules package-lock.json
npm install
```

dotnet ef database drop  # WARNING: Deletes all data
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
### Issue: Entity classes out-of-date

**Solution**:
```bash
cd src/api
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring
```

## Development Workflow

### 1. Create a New Feature

```bash
# Create feature branch
git checkout -b feature/add-task-tags

# Make changes
# ... code ...

# Run tests
cd src/api
dotnet test

cd ../front
npm test

# Commit and push
git add .
git commit -m "feat: add task tags feature"
git push origin feature/add-task-tags
```

### 2. Database Schema Changes (Database-First Workflow)

1. Change the schema directly in the database (SQL script or SSDT project in `src/database`).
2. Apply the script / publish the SQL project.
3. Re-scaffold EF Core entities:
  ```bash
  cd src/api
  dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring
  ```
4. Add/Update partial classes or Fluent API configurations (avoid editing scaffolded files).
5. Run backend tests to ensure compatibility.

### 3. API Changes

```bash
# Update controller/service
# ... code ...

# Update OpenAPI spec
# Edit specs/001-shared-todo-app/contracts/openapi.yaml

# Run API
dotnet run

# Test with Swagger UI
# https://localhost:5001/swagger
```

### 4. Frontend Changes

```bash
cd src/front

# Create component
ng generate component features/tasks/task-list

# Update component
# ... code ...

# Run with hot reload
npm start

# Run tests
npm test
```

## Testing

### Backend Tests

```bash
cd tests/api.tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

### Frontend Tests (Jest)

#### Configuration Files

Create `jest.config.ts` in `src/front`:
```ts
import type { Config } from 'jest';

const config: Config = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  roots: ['<rootDir>/src'],
  testMatch: ['**/*.spec.ts'],
  moduleFileExtensions: ['ts', 'js', 'json'],
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  globals: {
    'ts-jest': {
      tsconfig: '<rootDir>/tsconfig.spec.json'
    }
  },
  collectCoverage: true,
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov']
};

export default config;
```

Create `setup-jest.ts`:
```ts
import '@testing-library/jest-dom';
```

Update `tsconfig.spec.json` if needed to include `"types": ["jest"]`.

#### Running Tests

```bash
cd src/front
npm test
```

Sample test (`app.spec.ts`):
```ts
import { render } from '@testing-library/angular';
import { AppComponent } from './app';

describe('AppComponent', () => {
  it('renders application title', async () => {
    const { getByText } = await render(AppComponent);
    expect(getByText(/shared todo/i)).toBeInTheDocument();
  });
});
```

#### Frontend Fetch Retry Helper

Add a reusable fetch with retry (exponential backoff + jitter):
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

Use in services where transient failures are expected.

## Building for Production

dotnet publish -c Release -o ./publish
### Backend

```bash
cd src/api
dotnet publish -c Release -o ./publish
```

Ensure DbContext is aligned (re-scaffold if schema changed before publishing).

### Frontend

```bash
cd src/front
npm run build

# Output in dist/ folder
```

## Deployment

### Azure App Service (Backend)

```bash
# Create App Service
az webapp create \
  --name todo-api \
  --resource-group todo-rg \
  --plan todo-plan \
  --runtime "DOTNET|9.0"

# Deploy
cd src/api
az webapp deploy \
  --resource-group todo-rg \
  --name todo-api \
  --src-path ./publish
```

### Azure Static Web Apps (Frontend)

```bash
# Create Static Web App
az staticwebapp create \
  --name todo-frontend \
  --resource-group todo-rg \
  --location eastus

# Deploy (via GitHub Actions or manual)
cd src/front
npm run build
az staticwebapp deploy \
  --name todo-frontend \
  --resource-group todo-rg \
  --app-location ./dist
```

## Environment Variables

### Backend

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Database connection string | `Server=...;Database=TodoDB;...` |
| `AzureAd__TenantId` | Azure Entra ID tenant ID | `12345678-1234-...` |
| `AzureAd__ClientId` | API client ID | `87654321-4321-...` |
| `Cors__AllowedOrigins` | Allowed CORS origins | `["https://app.example.com"]` |

### Frontend

| Variable | Description | Example |
|----------|-------------|---------|
| `API_URL` | Backend API base URL | `https://api.example.com/api` |
| `MSAL_CLIENT_ID` | Frontend client ID | `abcd1234-5678-...` |
| `MSAL_TENANT_ID` | Azure Entra ID tenant ID | `12345678-1234-...` |

## Next Steps

1. ✅ Complete frontend component implementation (see `tasks.md` after running `/speckit.tasks`)
2. ✅ Implement backend services and controllers
3. ✅ Add comprehensive tests
4. ✅ Configure CI/CD pipeline
5. ✅ Deploy to Azure
6. ✅ Monitor with Application Insights

## Resources

- [Angular Documentation](https://angular.dev)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Azure Entra ID Documentation](https://docs.microsoft.com/azure/active-directory)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)

## Support

For questions or issues:
- Check the [spec.md](./spec.md) for feature requirements
- Review [research.md](./research.md) for technical decisions
- Consult [data-model.md](./data-model.md) for database schema
- See [contracts/openapi.yaml](./contracts/openapi.yaml) for API specification
- Open an issue in the project repository
