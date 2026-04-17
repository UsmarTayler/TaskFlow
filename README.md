# TaskFlow — Project & Task Management System

A full-stack ASP.NET Core MVC web application for managing software projects and work items. Teams can track features, bugs, and tasks across multiple projects with role-based access for Admins, Project Managers, and Developers.

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
- Recent activity feed

**Projects**
- Create and browse projects with live progress bars
- Project detail view with full task list, status filters, and CSV export

**Work Items**
- Create tasks, features, bugs, and improvements with priority and due dates
- Assign items to developers; update status inline from the detail page
- Overdue indicator on any item past its due date
- "My Tasks" personal view for developers

**Access Control**
- Role-based nav (Admin, Project Manager, Developer)
- Project Managers create projects and tasks; Developers update status
- Admin panel for managing users and changing roles

---

## Demo Accounts

Seeded automatically on first run (password: `Admin@1234`):

| Role | Email |
|---|---|
| Admin | admin@taskflow.dev |
| Project Manager | pm@taskflow.dev |
| Developer | alice@taskflow.dev |
| Developer | bob@taskflow.dev |

---

## Seeded Data

Three realistic projects with 17 work items across varied types, priorities, and statuses:

- **Customer Portal Redesign** (Active) — OAuth2 login, responsive UI, API docs
- **Mobile App v2.0** (Active) — Push notifications, iOS crash fix, dark mode, offline sync
- **Internal HR Portal** (Completed) — Onboarding, leave requests, Sage payroll integration

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
dotnet run --project CMCS.Mvc.csproj
```

The SQLite database (`taskflow.db`) is created and seeded automatically on first run.

> **Note:** If you previously ran the old claims-based version, delete `cmcs.db` from the project root — the new database is `taskflow.db`.

### Run the tests

```bash
dotnet test
```

---

## Project Structure

```
CMCS.Mvc/
├── Controllers/
│   ├── AccountController.cs      # Login, Register, Logout
│   ├── AdminController.cs        # User management (Admin only)
│   ├── DashboardController.cs    # Dashboard stats + chart data
│   ├── ProjectsController.cs     # Project CRUD
│   └── WorkItemsController.cs    # Task CRUD, status updates, CSV export
├── Data/
│   ├── AppDbContext.cs           # EF Core DbContext + Identity
│   └── DbInitializer.cs         # Roles, users, and 17 sample work items
├── Models/
│   ├── ApplicationUser.cs       # Extended Identity user (FullName)
│   ├── Project.cs               # Project entity with progress helpers
│   ├── WorkItem.cs              # Task entity with IsOverdue computed property
│   ├── ItemStatus.cs            # Todo | InProgress | InReview | Done
│   ├── ItemType.cs              # Feature | Bug | Task | Improvement
│   ├── Priority.cs              # Low | Medium | High | Critical
│   └── ProjectStatus.cs         # Active | OnHold | Completed | Archived
├── Services/
│   ├── IProjectService.cs       # Abstraction for all data access
│   └── ProjectService.cs        # EF Core implementation
├── ViewModels/
│   ├── CreateProjectViewModel.cs
│   ├── CreateWorkItemViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   └── UserManagementViewModel.cs
├── Views/
│   ├── Account/    Login, Register, AccessDenied
│   ├── Admin/      Users (role management)
│   ├── Dashboard/  Index (charts + stats)
│   ├── Projects/   Index, Create, Detail
│   └── WorkItems/  Create, Detail, MyTasks
├── wwwroot/css/site.css
├── appsettings.json             # SQLite connection string
└── Program.cs                   # DI, Identity, EF Core, middleware

CMCS.Tests/
├── ProjectServiceTests.cs       # 16 service-layer tests
└── ProjectsControllerTests.cs   # 10 controller tests
```

---

## Architecture Notes

- **Service layer abstraction** — `IProjectService` decouples controllers from EF Core, enabling full unit testing with mocked services.
- **EF Core relationships** — `WorkItem` → `Project` (cascade delete), three optional FKs to `ApplicationUser` (set null on delete to preserve data integrity).
- **Chart.js integration** — Dashboard serialises C# dictionaries to JSON directly in Razor; Chart.js renders client-side with zero extra build tooling.
- **CSV export** — Pure C# `StringBuilder`, no third-party dependencies; returns a `FileContentResult` with `text/csv`.
- **`IsOverdue` computed property** — `[NotMapped]`, evaluated in-memory, highlights items past their due date that are not yet `Done`.

---

## CI/CD

`.github/workflows/dotnet.yml` runs on every push and pull request to `master`/`main`:

1. Restore NuGet packages
2. Build in Release mode
3. Run all 26 unit tests
