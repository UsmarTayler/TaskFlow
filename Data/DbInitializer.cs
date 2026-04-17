using TaskFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Data
{
    /// <summary>
    /// Seeds the database with demo roles, users, projects, and work items on first run.
    /// Called from Program.cs during application startup using a scoped service scope.
    /// If the database already contains projects the seed is skipped, so it is safe to
    /// call on every startup without duplicating data.
    /// </summary>
    public static class DbInitializer
    {
        // ── Demo credentials ──────────────────────────────────────────────────────
        // These accounts are shown on the login page so reviewers can try each role
        private const string AdminEmail = "admin@taskflow.dev";
        private const string PmEmail    = "pm@taskflow.dev";
        private const string Dev1Email  = "alice@taskflow.dev";
        private const string Dev2Email  = "bob@taskflow.dev";
        private const string Password   = "Admin@1234";

        /// <summary>
        /// Main entry point — resolves services from DI and runs all seed operations.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context     = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Create the SQLite database file and schema if they don't exist yet
            await context.Database.EnsureCreatedAsync();

            // ── Roles ─────────────────────────────────────────────────────────────
            // Create the three application roles if they haven't been seeded before
            foreach (var role in new[] { "Admin", "ProjectManager", "Developer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // ── Users ─────────────────────────────────────────────────────────────
            // EnsureUser creates the user (if absent) and assigns them to the given role
            var admin = await EnsureUser(userManager, AdminEmail, "System Administrator", "Admin");
            var pm    = await EnsureUser(userManager, PmEmail,    "Sarah Mitchell",        "ProjectManager");
            var alice = await EnsureUser(userManager, Dev1Email,  "Alice Dlamini",         "Developer");
            var bob   = await EnsureUser(userManager, Dev2Email,  "Bob Naidoo",            "Developer");

            // ── Projects & Work Items (seed once) ─────────────────────────────────
            // Bail out early if projects already exist — prevents duplicate seed data
            if (await context.Projects.AnyAsync()) return;

            // ── Project 1: Customer Portal Redesign ───────────────────────────────
            var portal = new Project
            {
                Name        = "Customer Portal Redesign",
                Description = "Modernise the client-facing portal with a new UI, improved performance, and OAuth2 login.",
                Status      = ProjectStatus.Active,
                DueDate     = DateTime.UtcNow.AddMonths(2),
                CreatedAt   = DateTime.UtcNow.AddDays(-30),
                OwnerId     = pm?.Id,
                WorkItems   = new List<WorkItem>
                {
                    new WorkItem { Title = "Design new login page mockup",     Description = "Create Figma wireframes for the revamped login and registration screens.", Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Done,       DueDate = DateTime.UtcNow.AddDays(-10), CreatedAt = DateTime.UtcNow.AddDays(-28), UpdatedAt = DateTime.UtcNow.AddDays(-12), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new WorkItem { Title = "Implement OAuth2 / SSO login",     Description = "Integrate Azure AD and Google OAuth so enterprise users can sign in with existing credentials.", Type = ItemType.Feature, Priority = Priority.Critical, Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(7),   CreatedAt = DateTime.UtcNow.AddDays(-20), UpdatedAt = DateTime.UtcNow.AddDays(-1),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new WorkItem { Title = "Fix broken password reset link",   Description = "Password reset emails are delivering an expired token. Investigate token TTL settings.", Type = ItemType.Bug,         Priority = Priority.High,     Status = ItemStatus.Done,       DueDate = DateTime.UtcNow.AddDays(-5),  CreatedAt = DateTime.UtcNow.AddDays(-18), UpdatedAt = DateTime.UtcNow.AddDays(-6),  AssignedToId = bob?.Id,   CreatedById = alice?.Id },
                    new WorkItem { Title = "Mobile-responsive navbar",          Description = "Navbar collapses incorrectly on screens below 768 px. Needs Bootstrap 5 grid review.", Type = ItemType.Improvement, Priority = Priority.Medium,   Status = ItemStatus.InReview,   DueDate = DateTime.UtcNow.AddDays(4),   CreatedAt = DateTime.UtcNow.AddDays(-15), UpdatedAt = DateTime.UtcNow.AddDays(-2),  AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new WorkItem { Title = "Write API documentation",           Description = "Document all REST endpoints using Swagger / OpenAPI 3.0.", Type = ItemType.Task,        Priority = Priority.Low,      Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(14),  CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10), CreatedById = pm?.Id },
                    new WorkItem { Title = "Add session timeout warning",       Description = "Show a modal 2 minutes before the user session expires with option to extend.", Type = ItemType.Improvement, Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(10),  CreatedAt = DateTime.UtcNow.AddDays(-8),  UpdatedAt = DateTime.UtcNow.AddDays(-8),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                }
            };

            // ── Project 2: Mobile App v2.0 ────────────────────────────────────────
            var mobileApp = new Project
            {
                Name        = "Mobile App v2.0",
                Description = "Major release adding push notifications, dark mode, and cross-platform performance improvements.",
                Status      = ProjectStatus.Active,
                DueDate     = DateTime.UtcNow.AddMonths(3),
                CreatedAt   = DateTime.UtcNow.AddDays(-45),
                OwnerId     = pm?.Id,
                WorkItems   = new List<WorkItem>
                {
                    new WorkItem { Title = "Implement push notifications",      Description = "Integrate Firebase Cloud Messaging for iOS and Android. Include notification preference settings.", Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(10),  CreatedAt = DateTime.UtcNow.AddDays(-40), UpdatedAt = DateTime.UtcNow.AddDays(-3),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new WorkItem { Title = "Fix crash on iOS 17 launch",        Description = "App crashes immediately on startup for users on iOS 17.0.1. Stack trace points to UIWindowScene initialisation.", Type = ItemType.Bug,         Priority = Priority.Critical, Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(2),   CreatedAt = DateTime.UtcNow.AddDays(-5),  UpdatedAt = DateTime.UtcNow.AddDays(-1),  AssignedToId = bob?.Id,   CreatedById = alice?.Id },
                    new WorkItem { Title = "Add dark mode support",             Description = "Implement system-aware dark theme using adaptive colours across all screens.", Type = ItemType.Feature,     Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(21),  CreatedAt = DateTime.UtcNow.AddDays(-35), UpdatedAt = DateTime.UtcNow.AddDays(-35), AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new WorkItem { Title = "Performance profiling & optimise",  Description = "Profile cold start time and in-app navigation. Target < 2s cold start on mid-range devices.", Type = ItemType.Task,        Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(28),  CreatedAt = DateTime.UtcNow.AddDays(-20), UpdatedAt = DateTime.UtcNow.AddDays(-20), CreatedById = pm?.Id },
                    new WorkItem { Title = "App Store screenshot refresh",      Description = "Update App Store and Play Store screenshots to reflect new v2.0 UI.", Type = ItemType.Task,        Priority = Priority.Low,      Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(35),  CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10), CreatedById = pm?.Id },
                    new WorkItem { Title = "Offline mode data sync",            Description = "Cache key data locally and sync when connectivity is restored. Use SQLite + background sync service.", Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.InReview,   DueDate = DateTime.UtcNow.AddDays(6),   CreatedAt = DateTime.UtcNow.AddDays(-25), UpdatedAt = DateTime.UtcNow.AddDays(-2),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                }
            };

            // ── Project 3: Internal HR Portal ─────────────────────────────────────
            // This project is Completed — useful for showing the Completed filter on the UI
            var hrPortal = new Project
            {
                Name        = "Internal HR Portal",
                Description = "Self-service HR platform for employee onboarding, leave requests, and payroll viewing.",
                Status      = ProjectStatus.Completed,
                DueDate     = DateTime.UtcNow.AddDays(-15),
                CreatedAt   = DateTime.UtcNow.AddDays(-90),
                OwnerId     = pm?.Id,
                WorkItems   = new List<WorkItem>
                {
                    new WorkItem { Title = "Employee onboarding form",          Description = "Multi-step onboarding wizard that captures personal details, tax info, and emergency contacts.", Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-60), CreatedAt = DateTime.UtcNow.AddDays(-85), UpdatedAt = DateTime.UtcNow.AddDays(-62), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new WorkItem { Title = "Leave request approval workflow",   Description = "Submit, approve, and track annual, sick, and family responsibility leave.", Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-45), CreatedAt = DateTime.UtcNow.AddDays(-80), UpdatedAt = DateTime.UtcNow.AddDays(-46), AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new WorkItem { Title = "Payroll integration with Sage",     Description = "Read-only payslip display via Sage 300 People REST API. Token-based auth.", Type = ItemType.Feature,     Priority = Priority.Critical, Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-30), CreatedAt = DateTime.UtcNow.AddDays(-75), UpdatedAt = DateTime.UtcNow.AddDays(-32), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new WorkItem { Title = "User acceptance testing (UAT)",     Description = "Coordinate UAT sessions with HR team. Document and resolve all P1/P2 issues before go-live.", Type = ItemType.Task,        Priority = Priority.Medium,   Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-20), CreatedAt = DateTime.UtcNow.AddDays(-35), UpdatedAt = DateTime.UtcNow.AddDays(-22), AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new WorkItem { Title = "POPI Act compliance review",        Description = "Audit data collection forms and storage to ensure POPI Act compliance. Add consent checkboxes.", Type = ItemType.Improvement, Priority = Priority.High,     Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-18), CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-19), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                }
            };

            // Add all three projects (and their nested WorkItems) in a single transaction
            context.Projects.AddRange(portal, mobileApp, hrPortal);
            await context.SaveChangesAsync();
        }

        // ── Private helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a user with the given email / full name / role if they don't already exist.
        /// Returns the existing user if they were already seeded on a previous run.
        /// </summary>
        private static async Task<ApplicationUser?> EnsureUser(
            UserManager<ApplicationUser> um, string email, string fullName, string role)
        {
            // Check whether the user already exists before trying to create them
            var user = await um.FindByEmailAsync(email);
            if (user != null) return user;

            // Build the new Identity user — EmailConfirmed skips the email verification step
            user = new ApplicationUser
            {
                UserName       = email,
                Email          = email,
                FullName       = fullName,
                EmailConfirmed = true
            };

            // CreateAsync hashes the password and persists the user to the database
            var result = await um.CreateAsync(user, Password);
            if (result.Succeeded)
                await um.AddToRoleAsync(user, role);

            return user;
        }
    }
}
