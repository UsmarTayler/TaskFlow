# TaskFlow — Project & Task Management System

A full-stack ASP.NET Core MVC web application for managing software projects and work items across multiple organisations. Teams track features, bugs, and tasks with role-based access for Admins, Project Managers, and Developers — with full multi-tenant isolation between organisations.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC (.NET 8) |
| Language | C# 12 |
| Database | SQLite via Entity Framework Core 8 |
| Authentication | ASP.NET Core Identity (role-based) |
| Frontend | Bootstrap 5.3, Razor Views, Chart.js 4 |
| Testing | xUnit 2, Moq, EF Core InMemory |
| CI/CD | GitHub Actions |

---

## Features

**Dashboard**
- Summary cards: total projects, open items, due this week, completed today
- Live charts: tasks by status (doughnut), by priority (bar), by project (horizontal bar)
- Recent activity feed showing the last 8 modified items
- Developers are automatically redirected to their personal task list
- Scoped to the user's organisations — Admins see site-wide stats

**Organisations (Multi-Tenancy)**
- Create organisations and invite team members via a 6-character invite code
- Owners and Admins can also add members directly by email
- Members can be removed by the org owner or an Admin
- Projects are scoped to an organisation — members only see their own org's data
- Users can also maintain personal projects outside any organisation
- Full tenant isolation: a Developer in Org A cannot see Org B's projects or tasks

**Projects**
- Full CRUD: create, edit (name, description, status, due date), and delete projects
- Assign a project to an organisation or keep it personal
- Browse all projects with live text search and status filter
- Progress bar showing percentage of tasks completed
- Kanban board view (drag-and-drop cards across Todo / In Progress / In Review / Done columns)
- CSV export of all tasks in a project

**Work Items**
- Full CRUD: create, edit all fields, and delete tasks, features, bugs, and improvements
- Assign to developers; set priority (Low / Medium / High / Critical) and due dates
- Overdue indicator on any item past its due date that isn't yet Done
- Status update form available to all users (including Developers)
- "My Tasks" personal view showing all items assigned to the logged-in user
- Full **change history** — every status, priority, assignment, title, or due-date change is logged automatically with who made it and when
- **Comments** — any authenticated user can leave comments on a work item; authors, PMs, and Admins can delete them

**Access Control**
- Role-based navigation (Admin, Project Manager, Developer)
- Project Managers create, edit, and manage projects and tasks; Developers update status and comment
- Admins have site-wide visibility and a user management panel for changing roles
- Reassign work items from the detail page (PM/Admin only)

---

## Demo Accounts

Seeded automatically on first run (password: `Admin@1234`):

| Role | Email | Name | Organisation |
|---|---|---|---|
| Admin | admin@taskflow.dev | System Administrator | — (site-wide) |
| Project Manager | pm@taskflow.dev | Sarah Mitchell | Acme Software |
| Developer | alice@taskflow.dev | Alice Dlamini | Acme Software |
| Developer | bob@taskflow.dev | Bob Naidoo | Acme Software |
| Developer | charlie@taskflow.dev | Charlie Venter | Nova Labs |

> **Tip:** Log in as Charlie to see Nova Labs in isolation — no Acme projects appear. Log in as Admin to see all five projects across both organisations.

---

## Seeded Data

Two organisations with five realistic projects and 22 work items:

**Acme Software** (invite code: `ACME42`)
- **Customer Portal Redesign** (Active) — OAuth2 login, responsive navbar, password reset fix, API docs
- **Mobile App v2.0** (Active) — Push notifications, iOS crash fix, dark mode, offline sync
- **Internal HR Portal** (Completed) — Onboarding, leave requests, Sage payroll integration, POPI compliance

**Nova Labs** (invite code: `NOVA99`)
- **AI Chatbot Platform** (Active) — OpenAI integration, conversation history, rate limiting
- **Developer Dashboard** (Active) — GitHub Actions widget, Sentry error chart

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022+ or VS Code with the C# extension

### Run locally

```bash
git clone <your-repo-url>
cd TaskFlow

dotnet restore
dotnet run
```

The SQLite database (`taskflow.db`) is created and seeded automatically on first run. Navigate to `https://localhost:5001` and log in with any of the demo accounts above.

> The seeder is fully incremental — it is safe to run on every startup without duplicating data. Any missing organisations, members, or projects are added automatically.

### Run the tests

```bash
dotnet test
```

---

## Project Structure

```
TaskFlow/
├── Controllers/
│   ├── AccountController.cs        # Login, Register, Logout, Profile
│   ├── AdminController.cs          # User management (Admin only)
│   ├── DashboardController.cs      # Dashboard stats + Chart.js data
│   ├── OrganisationsController.cs  # Org CRUD, invite code join, member management
│   ├── ProjectsController.cs       # Project CRUD + Kanban board
│   └── WorkItemsController.cs      # Work item CRUD, status, assign, comments, CSV export
├── Data/
│   ├── AppDbContext.cs             # EF Core DbContext with auto-audit SaveChanges
│   └── DbInitializer.cs           # Roles, demo users, orgs, and 22 sample work items
├── Models/
│   ├── ApplicationUser.cs          # Extended Identity user (FullName)
│   ├── Organisation.cs             # Org entity (name, invite code, owner)
│   ├── OrganisationMember.cs       # Join table: user ↔ organisation
│   ├── Project.cs                  # Project entity with progress helpers
│   ├── WorkItem.cs                 # Task entity with IsOverdue computed property
│   ├── WorkItemComment.cs          # Comment entity (body, author, timestamp)
│   ├── WorkItemHistory.cs          # Audit trail entry (field, old/new value, who, when)
│   ├── ItemStatus.cs               # Todo | InProgress | InReview | Done
│   ├── ItemType.cs                 # Feature | Bug | Task | Improvement
│   ├── Priority.cs                 # Low | Medium | High | Critical
│   └── ProjectStatus.cs            # Active | OnHold | Completed | Archived
├── Services/
│   ├── IOrganisationService.cs     # Abstraction for org data access
│   ├── OrganisationService.cs      # EF Core implementation (invite codes, membership)
│   ├── IProjectService.cs          # Abstraction for project + work item data access
│   └── ProjectService.cs           # EF Core implementation (scoped queries, comments)
├── ViewModels/
│   ├── CreateOrganisationViewModel.cs
│   ├── JoinOrganisationViewModel.cs
│   ├── CreateProjectViewModel.cs
│   ├── CreateWorkItemViewModel.cs
│   ├── EditProjectViewModel.cs
│   ├── EditWorkItemViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   └── UserManagementViewModel.cs
├── Views/
│   ├── Account/         Login, Register, Profile, AccessDenied
│   ├── Admin/           Users (role management)
│   ├── Dashboard/       Index (charts + summary cards)
│   ├── Organisations/   Index, Create, Detail, Join
│   ├── Projects/        Index, Create, Edit, Detail, Kanban
│   └── WorkItems/       Create, Edit, Detail, MyTasks
├── wwwroot/css/site.css
├── appsettings.json                # SQLite connection string
└── Program.cs                      # DI, Identity, EF Core, middleware pipeline

TaskFlow.Tests/
├── ProjectServiceTests.cs          # Service-layer tests (EF InMemory)
└── ProjectsControllerTests.cs      # Controller tests (Moq)
```

---

## Architecture Highlights

**Multi-tenant data isolation** — `Project` carries a nullable `OrganisationId` FK. `ProjectService.GetProjectsForUserAsync` queries `OrganisationMembers` to build the user's org list, then returns only projects in those orgs plus personal projects they own. Admins call `GetAllProjectsAsync` and bypass this filter entirely.

**Service layer abstraction** — `IProjectService` and `IOrganisationService` decouple controllers from EF Core. All controller tests use `Mock<T>` and never touch the database.

**Automatic audit log** — `AppDbContext.SaveChangesAsync` is overridden to intercept every `WorkItem` modification. Changes to Status, Priority, AssignedToId, Title, and DueDate are written to `WorkItemHistory` in the same transaction as the data change — no manual logging required in controllers.

**Invite code system** — Organisations get a unique 6-character alphanumeric code (no ambiguous chars like O/0/I/1) generated at creation time. Anyone with the code can join via the Join page; owners can also add members directly by email.

**Kanban drag-and-drop** — Built with the native HTML5 Drag and Drop API (no external JS libraries). Cards optimistically move in the DOM on drop, then persist via a `fetch` POST to `WorkItems/SetStatus`. A Bootstrap toast confirms success or failure.

**EF Core relationships** — Cascade deletes for child records; `SetNull` on user FKs to avoid orphaned records when a user is removed. Unique indexes on `Organisation.InviteCode` and `(OrganisationId, UserId)` in `OrganisationMembers` enforced at the database level.

**CSV export** — Pure C# `StringBuilder`, RFC 4180 compliant escaping, no third-party dependencies.

---

## CI/CD

`.github/workflows/dotnet.yml` runs on every push and pull request to `master`/`main`:

1. Restore NuGet packages
2. Build in Release mode
3. Run all unit tests
