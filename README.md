# TaskFlow — Project & Task Management System

A full-stack ASP.NET Core MVC web application for managing software projects and work items. Teams track features, bugs, and tasks across multiple projects with role-based access for Admins, Project Managers, and Developers.

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

**Projects**
- Full CRUD: create, edit (name, description, status, due date), and delete projects
- Browse all projects with live text search and status filter
- Progress bar showing percentage of tasks completed
- Kanban board view (drag-and-drop cards across Todo / In Progress / In Review / Done columns)
- CSV export of all tasks in a project

**Work Items**
- Full CRUD: create, edit all fields, and delete tasks, features, bugs, and improvements
- Assign to developers; set priority (Low / Medium / High / Critical) and due dates
- Overdue indicator on any item past its due date that isn't yet Done
- Status update form available to all users (including Developers)
- "My Tasks" personal view auto-sorted by due date
- Full **change history** — every status, priority, assignment, title, or due-date change is logged automatically with who made it and when

**Access Control**
- Role-based navigation (Admin, Project Manager, Developer)
- Project Managers create, edit, and manage projects and tasks; Developers update status
- Admins can delete projects; Admin panel for managing users and changing roles

---

## Demo Accounts

Seeded automatically on first run (password: `Admin@1234`):

| Role | Email | Name |
|---|---|---|
| Admin | admin@taskflow.dev | System Administrator |
| Project Manager | pm@taskflow.dev | Sarah Mitchell |
| Developer | alice@taskflow.dev | Alice Dlamini |
| Developer | bob@taskflow.dev | Bob Naidoo |

---

## Seeded Data

Three realistic projects with 17 work items across varied types, priorities, and statuses:

- **Customer Portal Redesign** (Active) — OAuth2 login, responsive navbar, password reset fix, API docs
- **Mobile App v2.0** (Active) — Push notifications, iOS crash fix, dark mode, offline sync
- **Internal HR Portal** (Completed) — Onboarding, leave requests, Sage payroll integration, POPI compliance

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

### Run the tests

```bash
dotnet test
```

---

## Project Structure

```
TaskFlow/
├── Controllers/
│   ├── AccountController.cs      # Login, Register, Logout, Profile
│   ├── AdminController.cs        # User management (Admin only)
│   ├── DashboardController.cs    # Dashboard stats + Chart.js data
│   ├── ProjectsController.cs     # Project CRUD + Kanban board
│   └── WorkItemsController.cs    # Work item CRUD, status, assign, CSV export
├── Data/
│   ├── AppDbContext.cs           # EF Core DbContext with auto-audit SaveChanges
│   └── DbInitializer.cs         # Roles, demo users, and 17 sample work items
├── Models/
│   ├── ApplicationUser.cs        # Extended Identity user (FullName)
│   ├── Project.cs                # Project entity with progress helpers
│   ├── WorkItem.cs               # Task entity with IsOverdue computed property
│   ├── WorkItemHistory.cs        # Audit trail entry (field, old/new value, who, when)
│   ├── ItemStatus.cs             # Todo | InProgress | InReview | Done
│   ├── ItemType.cs               # Feature | Bug | Task | Improvement
│   ├── Priority.cs               # Low | Medium | High | Critical
│   └── ProjectStatus.cs          # Active | OnHold | Completed | Archived
├── Services/
│   ├── IProjectService.cs        # Abstraction for all data access
│   └── ProjectService.cs         # EF Core implementation (async throughout)
├── ViewModels/
│   ├── CreateProjectViewModel.cs
│   ├── CreateWorkItemViewModel.cs
│   ├── EditProjectViewModel.cs
│   ├── EditWorkItemViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   └── UserManagementViewModel.cs
├── Views/
│   ├── Account/    Login, Register, Profile, AccessDenied
│   ├── Admin/      Users (role management)
│   ├── Dashboard/  Index (charts + summary cards)
│   ├── Projects/   Index, Create, Edit, Detail, Kanban
│   └── WorkItems/  Create, Edit, Detail, MyTasks
├── wwwroot/css/site.css
├── appsettings.json              # SQLite connection string
└── Program.cs                    # DI, Identity, EF Core, middleware pipeline

TaskFlow.Tests/
├── ProjectServiceTests.cs        # 16 service-layer tests (EF InMemory)
└── ProjectsControllerTests.cs    # 10 controller tests (Moq)
```

---

## Architecture Highlights

**Service layer abstraction** — `IProjectService` decouples controllers from EF Core. All controller tests use `Mock<IProjectService>` and never touch the database.

**Automatic audit log** — `AppDbContext.SaveChangesAsync` is overridden to intercept every `WorkItem` modification. Changes to Status, Priority, AssignedToId, Title, and DueDate are written to `WorkItemHistory` in the same transaction as the data change — no manual logging required in controllers.

**Kanban drag-and-drop** — Built with the native HTML5 Drag and Drop API (no external JS libraries). Cards optimistically move in the DOM on drop, then persist via a `fetch` POST to `WorkItems/SetStatus`. A Bootstrap toast confirms success or failure.

**EF Core relationships** — `WorkItem → Project` (cascade delete), three optional FKs to `ApplicationUser` (set null on delete to avoid orphaned records). `WorkItemHistory → WorkItem` also cascades so deleting a task cleans up its audit trail.

**Chart.js integration** — Dashboard serialises C# dictionaries to JSON directly in Razor; Chart.js renders client-side with zero build tooling.

**CSV export** — Pure C# `StringBuilder`, RFC 4180 compliant escaping, no third-party dependencies.

---

## CI/CD

`.github/workflows/dotnet.yml` runs on every push and pull request to `master`/`main`:

1. Restore NuGet packages
2. Build in Release mode
3. Run all 26 unit tests
