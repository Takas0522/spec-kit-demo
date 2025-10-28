# Tasks: Shared ToDo Management Application

**Input**: Design documents from `/specs/001-shared-todo-app/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Architecture Strategy**: Database-First (Database → API → Frontend)
- Database schema is source of truth
- EF Core entities scaffolded from database
- API implements business logic and endpoints
- Frontend consumes API with MSAL authentication

**Tests**: Not explicitly requested in specification, so test tasks are minimal. Focus on implementation.

**Organization**: Tasks organized by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

```
src/
├── database/           # SQL Server database project
├── api/                # ASP.NET Core WebAPI
└── front/              # Angular frontend
tests/
├── api.tests/          # Backend tests
└── front/              # Frontend tests
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Verify development prerequisites (SQL Server, .NET 9.0, Node.js 20+, Azure CLI)
- [ ] T002 [P] Configure Azure Entra ID app registrations (API and Frontend) per quickstart.md
- [ ] T003 [P] Setup local or Azure SQL Database instance and verify connectivity
- [ ] T004 [P] Configure User Secrets for API in src/api/ (ConnectionString, AzureAd settings)
- [ ] T005 [P] Install frontend dependencies with `npm install` in src/front/
- [ ] T006 [P] Configure Tailwind CSS in src/front/tailwind.config.js and src/front/src/styles.css
- [ ] T007 [P] Setup frontend environment files in src/front/src/environments/ (development and production)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database Schema (Database-First)

- [ ] T008 Create Users table in src/database/Tables/Users.sql with columns: Id (PK), EntraObjectId (unique), Email (unique), DisplayName, CreatedAt, LastLoginAt, IsActive
- [ ] T009 Create Tasks table in src/database/Tables/Tasks.sql with columns: Id (PK), OwnerId (FK to Users), Title, Description, Status, DueDate, CreatedAt, ModifiedAt, IsDeleted
- [ ] T010 Create TaskShares table in src/database/Tables/TaskShares.sql with columns: Id (PK), TaskId (FK to Tasks), SharedByUserId (FK to Users), SharedWithUserId (FK to Users), SharedAt, CanEdit
- [ ] T011 Add indexes and constraints to all tables (unique, check, foreign key) per data-model.md
- [ ] T012 Create trigger for auto-updating Tasks.ModifiedAt in src/database/Tables/Tasks.sql
- [ ] T013 Update postDeployment.sql in src/database/postDeployment.sql for any seed data or initial setup (optional)
- [ ] T014 Deploy database schema to local/Azure SQL instance using SSDT project or manual scripts

### API Scaffolding & Core Setup

- [ ] T015 Scaffold EF Core DbContext and entities from database using `dotnet ef dbcontext scaffold` in src/api/
- [ ] T016 Configure DbContext with connection string, resilience policies (EnableRetryOnFailure), and global query filters in src/api/Data/TodoDbContext.cs
- [ ] T017 [P] Configure authentication middleware with Microsoft.Identity.Web in src/api/Program.cs
- [ ] T018 [P] Configure CORS policy for frontend origin in src/api/Program.cs
- [ ] T019 [P] Setup global exception handling middleware in src/api/Program.cs
- [ ] T020 [P] Configure Polly resilience policies (retry, circuit breaker, timeout) for HttpClient in src/api/Program.cs
- [ ] T021 [P] Configure Swagger/OpenAPI with Azure Entra ID security definitions in src/api/Program.cs
- [ ] T022 [P] Create base error response DTOs in src/api/Models/DTOs/ErrorResponse.cs
- [ ] T023 [P] Setup structured logging configuration in src/api/appsettings.json and appsettings.Development.json

### Frontend Core Setup

- [ ] T024 Configure MSAL authentication in src/front/src/app/app.config.ts with providers
- [ ] T025 Create HTTP interceptor for automatic token attachment in src/front/src/app/core/interceptors/auth.interceptor.ts
- [ ] T026 [P] Create authentication guard in src/front/src/app/core/guards/auth.guard.ts
- [ ] T027 [P] Create base API service with retry logic (fetchWithRetry helper) in src/front/src/app/core/services/api.service.ts
- [ ] T028 [P] Create global error handling service in src/front/src/app/core/services/error-handler.service.ts
- [ ] T029 [P] Define TypeScript interfaces for all DTOs in src/front/src/app/models/ (User, Task, TaskShare, TaskStatus enum)
- [ ] T030 Setup routing structure in src/front/src/app/app.routes.ts with auth guard

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - User Registration and Authentication (Priority: P1) 🎯 MVP

**Goal**: Users can authenticate with Azure Entra ID and access their profile

**Independent Test**: Register/login with Azure Entra ID, view authenticated user profile in UI

### Implementation for User Story 1

#### API - User Profile Endpoint

- [ ] T031 [US1] Create UserDto in src/api/Models/DTOs/UserDto.cs
- [ ] T032 [US1] Create IUserService interface in src/api/Services/IUserService.cs
- [ ] T033 [US1] Implement UserService with auto-provisioning logic in src/api/Services/UserService.cs
- [ ] T034 [US1] Create UsersController with GET /api/users/me endpoint in src/api/Controllers/UsersController.cs
- [ ] T035 [US1] Add authorization attributes and user claim extraction in UsersController

#### Frontend - Authentication & Profile

- [ ] T036 [P] [US1] Create login component in src/front/src/app/features/auth/login.component.ts
- [ ] T037 [P] [US1] Create logout functionality in src/front/src/app/features/auth/logout.component.ts
- [ ] T038 [US1] Create auth service wrapping MSAL in src/front/src/app/core/services/auth.service.ts
- [ ] T039 [US1] Create user service for profile API calls in src/front/src/app/core/services/user.service.ts
- [ ] T040 [US1] Create user profile component to display authenticated user in src/front/src/app/features/auth/profile.component.ts
- [ ] T041 [US1] Add navigation header with login/logout buttons in src/front/src/app/shared/components/nav.component.ts
- [ ] T042 [US1] Style authentication components with Tailwind CSS

#### Integration & Testing

- [ ] T043 [US1] Test authentication flow: login → redirect → profile display → logout
- [ ] T044 [US1] Test auto-provisioning: new user login creates User record in database
- [ ] T045 [US1] Verify JWT token validation and claim extraction in API
- [ ] T046 [US1] Update src/api/api.http with user profile endpoint test (with token)

**Checkpoint**: User Story 1 complete - users can authenticate and view profile

---

## Phase 4: User Story 2 - Personal Task Management (Priority: P1) 🎯 MVP

**Goal**: Authenticated users can create, view, edit, update status, and delete their own tasks

**Independent Test**: Create task → change status → edit details → delete task, all operations persist

### Implementation for User Story 2

#### API - Task CRUD Endpoints

- [ ] T047 [P] [US2] Create TaskDto in src/api/Models/DTOs/TaskDto.cs
- [ ] T048 [P] [US2] Create CreateTaskDto in src/api/Models/DTOs/CreateTaskDto.cs
- [ ] T049 [P] [US2] Create UpdateTaskDto in src/api/Models/DTOs/UpdateTaskDto.cs
- [ ] T050 [P] [US2] Create UpdateTaskStatusDto in src/api/Models/DTOs/UpdateTaskStatusDto.cs
- [ ] T051 [P] [US2] Create PagedTaskListDto in src/api/Models/DTOs/PagedTaskListDto.cs
- [ ] T052 [US2] Create ITaskService interface in src/api/Services/ITaskService.cs
- [ ] T053 [US2] Implement TaskService with CRUD operations and ownership checks in src/api/Services/TaskService.cs
- [ ] T054 [US2] Create TasksController in src/api/Controllers/TasksController.cs
- [ ] T055 [US2] Implement GET /api/tasks endpoint with filtering, sorting, pagination in TasksController
- [ ] T056 [US2] Implement POST /api/tasks endpoint for task creation in TasksController
- [ ] T057 [US2] Implement GET /api/tasks/{id} endpoint in TasksController
- [ ] T058 [US2] Implement PUT /api/tasks/{id} endpoint with owner validation in TasksController
- [ ] T059 [US2] Implement PATCH /api/tasks/{id}/status endpoint in TasksController
- [ ] T060 [US2] Implement DELETE /api/tasks/{id} endpoint (soft delete) in TasksController

#### Frontend - Task Management UI

- [ ] T061 [P] [US2] Create task service for API calls in src/front/src/app/core/services/task.service.ts
- [ ] T062 [P] [US2] Create task list component in src/front/src/app/features/tasks/task-list.component.ts
- [ ] T063 [P] [US2] Create task card component for displaying individual tasks in src/front/src/app/features/tasks/task-card.component.ts
- [ ] T064 [US2] Create task create/edit modal component in src/front/src/app/features/tasks/task-form.component.ts
- [ ] T065 [US2] Create task status badge component with color coding in src/front/src/app/shared/components/status-badge.component.ts
- [ ] T066 [US2] Implement task filtering UI (status dropdown) in task-list component
- [ ] T067 [US2] Implement task sorting UI (sort by date, title, status) in task-list component
- [ ] T068 [US2] Implement pagination controls in task-list component
- [ ] T069 [US2] Add task status update buttons (Start, Complete, Reopen) in task-card component
- [ ] T070 [US2] Add task edit and delete actions in task-card component
- [ ] T071 [US2] Style task management components with Tailwind CSS (cards, forms, buttons)
- [ ] T072 [US2] Add loading states and error handling in task components

#### Integration & Testing

- [ ] T073 [US2] Test complete task lifecycle: create → view → update status → edit → delete
- [ ] T074 [US2] Test pagination: navigate through pages, verify correct items displayed
- [ ] T075 [US2] Test filtering: filter by NotStarted, InProgress, Completed
- [ ] T076 [US2] Test sorting: sort by createdAt, modifiedAt, dueDate, title
- [ ] T077 [US2] Verify only task owner can edit/delete their tasks (API returns 403 for non-owners)
- [ ] T078 [US2] Update src/api/api.http with all task CRUD endpoint tests

**Checkpoint**: User Story 2 complete - users can fully manage personal tasks

---

## Phase 5: User Story 3 - Task Sharing and Visibility (Priority: P2)

**Goal**: Task owners can share tasks with other users; shared users can view (read-only)

**Independent Test**: User A shares task → User B logs in and sees shared task (read-only)

### Implementation for User Story 3

#### API - Task Sharing Endpoints

- [ ] T079 [P] [US3] Create TaskShareDto in src/api/Models/DTOs/TaskShareDto.cs
- [ ] T080 [P] [US3] Create ShareTaskDto in src/api/Models/DTOs/ShareTaskDto.cs
- [ ] T081 [P] [US3] Create SharedTaskDto (extends TaskDto) in src/api/Models/DTOs/SharedTaskDto.cs
- [ ] T082 [P] [US3] Create PagedSharedTaskListDto in src/api/Models/DTOs/PagedSharedTaskListDto.cs
- [ ] T083 [US3] Create ITaskShareService interface in src/api/Services/ITaskShareService.cs
- [ ] T084 [US3] Implement TaskShareService with share/revoke logic in src/api/Services/TaskShareService.cs
- [ ] T085 [US3] Create TaskSharesController in src/api/Controllers/TaskSharesController.cs
- [ ] T086 [US3] Implement GET /api/tasks/{id}/shares endpoint in TaskSharesController
- [ ] T087 [US3] Implement POST /api/tasks/{id}/shares endpoint with user email lookup in TaskSharesController
- [ ] T088 [US3] Implement DELETE /api/tasks/{id}/shares/{userId} endpoint in TaskSharesController
- [ ] T089 [US3] Implement GET /api/shared-tasks endpoint with pagination in TaskSharesController
- [ ] T090 [US3] Add validation: prevent sharing with self, prevent duplicate shares (409 conflict)
- [ ] T091 [US3] Update TaskService to include share access checks (read-only for shared users)

#### Frontend - Task Sharing UI

- [ ] T092 [P] [US3] Create task-share service for API calls in src/front/src/app/core/services/task-share.service.ts
- [ ] T093 [P] [US3] Create shared tasks list component in src/front/src/app/features/shared-tasks/shared-tasks-list.component.ts
- [ ] T094 [US3] Create share task modal component in src/front/src/app/features/tasks/share-task-modal.component.ts
- [ ] T095 [US3] Create task share list component (shows who task is shared with) in src/front/src/app/features/tasks/task-share-list.component.ts
- [ ] T096 [US3] Add "Share" button to task-card component (only for owner)
- [ ] T097 [US3] Add "Revoke" button in task-share-list component (only for owner)
- [ ] T098 [US3] Add visual indicator for read-only tasks (lock icon, disabled edit buttons)
- [ ] T099 [US3] Add "Shared Tasks" navigation menu item in nav component
- [ ] T100 [US3] Implement email input with validation in share-task-modal
- [ ] T101 [US3] Style sharing components with Tailwind CSS

#### Integration & Testing

- [ ] T102 [US3] Test share flow: Owner shares task by email → API creates TaskShare record
- [ ] T103 [US3] Test shared task visibility: Shared user sees task in /shared-tasks endpoint
- [ ] T104 [US3] Test read-only enforcement: Shared user cannot edit/delete shared task (API 403)
- [ ] T105 [US3] Test revoke flow: Owner revokes share → shared user loses access
- [ ] T106 [US3] Test duplicate share prevention: Attempt to share twice returns 409 Conflict
- [ ] T107 [US3] Test share with non-existent email: Returns 404 Not Found
- [ ] T108 [US3] Verify shared task status updates visible to shared users in real-time (via polling)
- [ ] T109 [US3] Update src/api/api.http with task sharing endpoint tests

**Checkpoint**: User Story 3 complete - task sharing works with proper access control

---

## Phase 6: User Story 4 - Task Collaboration Dashboard (Priority: P3)

**Goal**: Users have a unified view of all shared tasks with filtering and organization

**Independent Test**: View dashboard showing all shared tasks, filter by status/owner, sort by various fields

### Implementation for User Story 4

#### API - Dashboard Enhancements

- [ ] T110 [US4] Add owner name to SharedTaskDto if not already included (OwnerName field)
- [ ] T111 [US4] Enhance GET /api/shared-tasks with additional filtering options (by owner, date range) in TaskSharesController
- [ ] T112 [US4] Add sorting support to GET /api/shared-tasks (by sharedAt, status, title, owner) in TaskSharesController

#### Frontend - Dashboard UI

- [ ] T113 [P] [US4] Create dashboard layout component in src/front/src/app/features/dashboard/dashboard.component.ts
- [ ] T114 [US4] Add task summary cards (total tasks, completed, in progress) in dashboard component
- [ ] T115 [US4] Create shared task filter panel in src/front/src/app/features/dashboard/filter-panel.component.ts
- [ ] T116 [US4] Implement filter by owner (dropdown of unique owners) in filter-panel
- [ ] T117 [US4] Implement filter by status (NotStarted, InProgress, Completed) in filter-panel
- [ ] T118 [US4] Implement filter by date range (shared within last week, month, etc.) in filter-panel
- [ ] T119 [US4] Add sorting dropdown (by sharedAt, status, title, owner) in dashboard
- [ ] T120 [US4] Create grouped view: group shared tasks by owner or status in dashboard
- [ ] T121 [US4] Add "Dashboard" navigation menu item in nav component
- [ ] T122 [US4] Style dashboard with Tailwind CSS (grid layout, cards, responsive design)

#### Integration & Testing

- [ ] T123 [US4] Test dashboard displays all shared tasks with correct owner names
- [ ] T124 [US4] Test filtering: filter by specific owner, verify only their tasks shown
- [ ] T125 [US4] Test filtering: filter by status, verify only matching status shown
- [ ] T126 [US4] Test sorting: sort by sharedAt desc, verify most recent first
- [ ] T127 [US4] Test grouped view: group by owner, verify tasks correctly grouped
- [ ] T128 [US4] Test summary cards: verify counts match actual task statuses
- [ ] T129 [US4] Update src/api/api.http with enhanced shared-tasks endpoint tests

**Checkpoint**: User Story 4 complete - comprehensive dashboard for shared task management

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Resilience & Performance

- [ ] T130 [P] Add loading spinners for all async operations in frontend
- [ ] T131 [P] Implement toast notifications for success/error messages in src/front/src/app/shared/components/toast.component.ts
- [ ] T132 [P] Add polling mechanism for task status updates (5-second interval) in task.service.ts
- [ ] T133 Test frontend retry logic: simulate network failure, verify exponential backoff works
- [ ] T134 Test backend resilience: simulate transient SQL error, verify EF retry policy works
- [ ] T135 [P] Add request correlation IDs for distributed tracing across frontend and backend

### Security & Validation

- [ ] T136 [P] Add input sanitization for all user inputs in frontend components
- [ ] T137 [P] Add server-side validation for all DTOs with data annotations in API
- [ ] T138 [P] Implement rate limiting middleware in API (if needed)
- [ ] T139 Verify CORS policy only allows configured frontend origin
- [ ] T140 Test token expiration handling: expired token → redirect to login

### Documentation & Developer Experience

- [ ] T141 [P] Update README.md in src/front/ with setup and run instructions
- [ ] T142 [P] Update README.md in src/api/ with setup and run instructions
- [ ] T143 [P] Add code comments for complex business logic in services
- [ ] T144 [P] Generate API documentation from Swagger/OpenAPI spec
- [ ] T145 Run through quickstart.md validation: verify all steps work end-to-end

### Testing (If Time Permits)

- [ ] T146 [P] Add unit tests for UserService in tests/api.tests/Services/UserServiceTests.cs
- [ ] T147 [P] Add unit tests for TaskService in tests/api.tests/Services/TaskServiceTests.cs
- [ ] T148 [P] Add unit tests for TaskShareService in tests/api.tests/Services/TaskShareServiceTests.cs
- [ ] T149 [P] Add integration tests for authentication flow in tests/api.tests/Integration/
- [ ] T150 [P] Add Jest unit tests for key frontend components in tests/front/

### Final Validation

- [ ] T151 Full end-to-end test: User A creates task → shares with User B → User B views → User A updates status → User B sees update
- [ ] T152 Performance test: Create 100 tasks, verify pagination and filtering remain responsive
- [ ] T153 Security test: Attempt to access other user's task without permission (should get 403)
- [ ] T154 Verify all acceptance scenarios from spec.md are implemented and working

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
  - Database schema MUST be deployed before API scaffolding (T008-T014 before T015)
  - API scaffolding MUST complete before API implementation
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - **US1 (Phase 3)**: Can start after Foundational - No dependencies on other stories
  - **US2 (Phase 4)**: Depends on US1 (needs authentication) - Builds on user profile
  - **US3 (Phase 5)**: Depends on US2 (needs tasks to exist) - Adds sharing to existing tasks
  - **US4 (Phase 6)**: Depends on US3 (needs shared tasks) - Enhances shared task view
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### Critical Path (Database-First Flow)

```
Setup (T001-T007)
    ↓
Database Schema (T008-T014)  ← MUST complete first
    ↓
API Scaffolding (T015)  ← Generates entities from DB
    ↓
API Configuration (T016-T023) + Frontend Setup (T024-T030)
    ↓
US1: Authentication (T031-T046)  ← Required for all other features
    ↓
US2: Personal Tasks (T047-T078)  ← Required for sharing
    ↓
US3: Task Sharing (T079-T109)  ← Required for dashboard
    ↓
US4: Dashboard (T110-T129)
    ↓
Polish (T130-T154)
```

### Within Each User Story

- **API first, then Frontend**: Complete API endpoints before starting frontend components
- **DTOs before Services**: Define data contracts before implementing logic
- **Services before Controllers**: Implement business logic before HTTP endpoints
- **Core before Integration**: Basic functionality before advanced features

### Database-First Workflow Notes

1. **T008-T014** (Database Schema): Complete all table definitions, constraints, indexes
2. **T014** (Deploy Schema): Deploy to SQL Server (local or Azure)
3. **T015** (Scaffold): Run `dotnet ef dbcontext scaffold` to generate C# entities
4. **Schema Changes**: If database schema changes later:
   - Update SQL table definitions
   - Redeploy schema
   - Re-run scaffold command (T015) with `--force` flag
   - Update partial classes or Fluent API as needed
   - Do NOT manually edit generated entity files

### Parallel Opportunities

#### Phase 1 (Setup)
- T002, T003, T004, T005, T006, T007 can all run in parallel (different tools/systems)

#### Phase 2 (Foundational)
- After database deployed (T014):
  - API configuration tasks (T016-T023) can run in parallel
  - Frontend setup tasks (T024-T030) can run in parallel
  - But T015 (scaffolding) must complete before T016

#### User Story 1 (US1)
- T031, T032 (DTOs and interfaces) can run in parallel
- T036, T037 (frontend login/logout) can run in parallel

#### User Story 2 (US2)
- T047-T051 (all DTOs) can run in parallel
- T061-T063 (frontend services and components) can be started in parallel after API is ready

#### User Story 3 (US3)
- T079-T082 (all DTOs) can run in parallel
- T092-T093 (frontend services and components) can run in parallel

#### User Story 4 (US4)
- T113-T115 (dashboard components) can run in parallel after API changes

#### Phase 7 (Polish)
- All tasks marked [P] can run in parallel
- T146-T150 (tests) can all run in parallel

---

## Parallel Execution Examples

### Parallel Example: Foundational Phase (After Database Deployed)

```bash
# Terminal 1: API Configuration
cd src/api
# T015: Scaffold entities
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -o Data/Entities -c TodoDbContext --context-dir Data --force --use-database-names --no-onconfiguring

# Then: T016-T023 (configure Program.cs, middleware, etc.)

# Terminal 2: Frontend Setup (in parallel)
cd src/front
# T024-T030: Configure MSAL, create interceptors, guards, base services
```

### Parallel Example: User Story 2 DTOs

```bash
# Create all DTO files at once (different files, no dependencies):
# T047: TaskDto
# T048: CreateTaskDto
# T049: UpdateTaskDto
# T050: UpdateTaskStatusDto
# T051: PagedTaskListDto
```

---

## Implementation Strategy

### MVP First (P1 Stories Only: US1 + US2)

1. **Complete Phase 1**: Setup (T001-T007)
2. **Complete Phase 2**: Foundational (T008-T030) - CRITICAL
3. **Complete Phase 3**: US1 - Authentication (T031-T046)
4. **Complete Phase 4**: US2 - Personal Tasks (T047-T078)
5. **STOP and VALIDATE**: Full authentication + personal task management working
6. **Deploy/Demo MVP**: Core functionality ready for users

### Incremental Delivery (Add P2 and P3)

1. **MVP** (US1 + US2) → Test independently → Deploy ✅
2. **Add US3** (Task Sharing, T079-T109) → Test independently → Deploy
3. **Add US4** (Dashboard, T110-T129) → Test independently → Deploy
4. **Polish** (Phase 7, T130-T154) → Final validation → Production deployment

### Parallel Team Strategy

With multiple developers after Foundational phase (T030) completes:

**Option A: Parallel User Stories (if experienced team)**
- Developer A: US1 (Authentication)
- Developer B: US2 (Personal Tasks) - starts after US1 API basics
- Developer C: Frontend setup and styling

**Option B: Sequential by Priority (recommended for database-first)**
- All developers: Complete US1 together (fastest)
- All developers: Complete US2 together
- Split: US3 (developer A) + US4 (developer B)

**Database-First Consideration**: Schema changes affect everyone, so coordinate carefully:
- One person manages database schema changes
- Communicate before re-scaffolding entities
- Use version control for schema scripts

---

## Notes

### Format & Organization

- [P] tasks = different files, no dependencies, safe to parallelize
- [Story] label maps task to specific user story (US1, US2, US3, US4)
- Each user story should be independently completable and testable
- Task IDs (T001-T154) are in suggested execution order

### Database-First Best Practices

- ✅ Database schema is source of truth
- ✅ Never manually edit generated entity files in `src/api/Data/Entities/`
- ✅ Use partial classes for extending entity behavior
- ✅ Use Fluent API in `TodoDbContext` for relationships and configurations
- ✅ Re-scaffold entities after ANY database schema change
- ✅ Commit database scripts (`src/database/Tables/*.sql`) to version control
- ✅ Document schema changes in migration notes

### Testing Strategy

- Tests are minimal (not explicitly requested in spec)
- Focus on manual validation via browser + API testing with `src/api/api.http`
- Key test scenarios in checkpoints after each user story
- Optional: Add automated tests in Phase 7 if time permits

### Resilience & Error Handling

- Frontend: Exponential backoff with jitter for transient failures
- Backend: Polly policies for HTTP + EF Core retry on transient SQL errors
- User-friendly error messages in Japanese (e.g., "一時的な通信エラーが発生しました")
- Correlation IDs for tracing across services

### Validation Checkpoints

- After US1: Can authenticate and see profile
- After US2: Can manage personal tasks (full CRUD)
- After US3: Can share tasks and view shared tasks (read-only)
- After US4: Dashboard shows all shared tasks with filtering
- After Phase 7: Production-ready with polish and documentation

### Common Pitfalls to Avoid

- ❌ Don't start API implementation before database is deployed
- ❌ Don't skip re-scaffolding after schema changes
- ❌ Don't manually edit generated entity files
- ❌ Don't implement frontend before API endpoints are ready
- ❌ Don't skip testing ownership/authorization logic
- ❌ Don't forget to configure CORS for frontend origin
- ❌ Don't commit secrets (use User Secrets or environment variables)

---

## Task Summary

- **Total Tasks**: 154
- **Phase 1 (Setup)**: 7 tasks
- **Phase 2 (Foundational)**: 23 tasks (CRITICAL - blocks all user stories)
- **Phase 3 (US1 - Authentication)**: 16 tasks
- **Phase 4 (US2 - Personal Tasks)**: 32 tasks
- **Phase 5 (US3 - Task Sharing)**: 31 tasks
- **Phase 6 (US4 - Dashboard)**: 20 tasks
- **Phase 7 (Polish)**: 25 tasks

### Parallel Opportunities Identified

- **Phase 1**: 6 tasks can run in parallel
- **Phase 2**: ~15 tasks can run in parallel (after database deployed)
- **Phase 3**: ~5 tasks can run in parallel
- **Phase 4**: ~8 tasks can run in parallel
- **Phase 5**: ~6 tasks can run in parallel
- **Phase 6**: ~4 tasks can run in parallel
- **Phase 7**: ~15 tasks can run in parallel

### MVP Scope (Recommended First Delivery)

- Phase 1: Setup (7 tasks)
- Phase 2: Foundational (23 tasks)
- Phase 3: US1 - Authentication (16 tasks)
- Phase 4: US2 - Personal Tasks (32 tasks)
- **Total MVP**: 78 tasks

This MVP delivers:
- ✅ Azure Entra ID authentication
- ✅ User profile management
- ✅ Full personal task CRUD operations
- ✅ Task status management (NotStarted, InProgress, Completed)
- ✅ Filtering, sorting, and pagination

### Independent Test Criteria

**US1 Test**: User can log in with Azure Entra ID → profile displays → can log out
**US2 Test**: User can create task → view in list → change status → edit title → delete task
**US3 Test**: User A shares task with User B → User B sees task (read-only) → User A updates status → User B sees update
**US4 Test**: User views dashboard → sees all shared tasks → filters by owner → sorts by date

---

**Generated**: October 28, 2025
**Feature**: 001-shared-todo-app
**Architecture**: Database-First (Database → API → Frontend)
