using TaskFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Data
{
    /// <summary>
    /// Seeds the database with demo roles, users, organisations, projects, and work items.
    /// Called from Program.cs on every startup. Fully incremental — each record is checked
    /// individually before insertion, so it is safe to run against an existing database and
    /// will never create duplicates.
    /// </summary>
    public static class DbInitializer
    {
        // ── Demo credentials ──────────────────────────────────────────────────────
        private const string AdminEmail   = "admin@taskflow.dev";
        private const string PmEmail      = "pm@taskflow.dev";
        private const string Dev1Email    = "alice@taskflow.dev";
        private const string Dev2Email    = "bob@taskflow.dev";
        private const string Dev3Email    = "charlie@taskflow.dev";   // Nova Labs only
        private const string Password     = "Admin@1234";

        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context     = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Create the SQLite database file and schema if they don't exist yet
            await context.Database.EnsureCreatedAsync();

            // ── Roles ─────────────────────────────────────────────────────────────
            foreach (var role in new[] { "Admin", "ProjectManager", "Developer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // ── Users ─────────────────────────────────────────────────────────────
            var admin   = await EnsureUser(userManager, AdminEmail,  "System Administrator", "Admin");
            var pm      = await EnsureUser(userManager, PmEmail,     "Sarah Mitchell",       "ProjectManager");
            var alice   = await EnsureUser(userManager, Dev1Email,   "Alice Dlamini",        "Developer");
            var bob     = await EnsureUser(userManager, Dev2Email,   "Bob Naidoo",           "Developer");
            var charlie = await EnsureUser(userManager, Dev3Email,   "Charlie Venter",       "Developer");

            // ── Organisations ─────────────────────────────────────────────────────
            // Each org is checked by its fixed invite code — safe to re-run on existing DBs.
            // Acme Software:  PM + Alice + Bob
            // Nova Labs:      Charlie only (separate tenant — dashboards must not cross-pollinate)
            // Admin is a site-level superuser, not in any org (sees everything).

            var acme = await EnsureOrganisation(context,
                inviteCode:  "ACME42",
                name:        "Acme Software",
                description: "Demo organisation. PM + Developers see only this org's projects.",
                createdDaysAgo: 90,
                ownerId:     pm?.Id);

            if (acme != null)
            {
                await EnsureMember(context, acme.OrganisationId, pm?.Id,    daysAgo: 90);
                await EnsureMember(context, acme.OrganisationId, alice?.Id, daysAgo: 85);
                await EnsureMember(context, acme.OrganisationId, bob?.Id,   daysAgo: 85);
                await context.SaveChangesAsync();
            }

            var nova = await EnsureOrganisation(context,
                inviteCode:  "NOVA99",
                name:        "Nova Labs",
                description: "Second demo org — completely isolated from Acme. Charlie is the sole member.",
                createdDaysAgo: 60,
                ownerId:     charlie?.Id);

            if (nova != null)
            {
                await EnsureMember(context, nova.OrganisationId, charlie?.Id, daysAgo: 60);
                await context.SaveChangesAsync();
            }

            // ── Projects ──────────────────────────────────────────────────────────
            // Each project is checked by name. If it doesn't exist it is created with
            // its work items. Existing projects are never modified.

            var acmeId = acme?.OrganisationId;
            var novaId = nova?.OrganisationId;

            // ── Acme: Customer Portal Redesign ────────────────────────────────────
            await EnsureProject(context, "Customer Portal Redesign", new Project
            {
                Name           = "Customer Portal Redesign",
                Description    = "Modernise the client-facing portal with a new UI, improved performance, and OAuth2 login.",
                Status         = ProjectStatus.Active,
                DueDate        = DateTime.UtcNow.AddMonths(2),
                CreatedAt      = DateTime.UtcNow.AddDays(-30),
                OwnerId        = pm?.Id,
                OrganisationId = acmeId,
                WorkItems      = new List<WorkItem>
                {
                    new() { Title = "Design new login page mockup",   Description = "Create Figma wireframes for the revamped login and registration screens.",                                   Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Done,       DueDate = DateTime.UtcNow.AddDays(-10), CreatedAt = DateTime.UtcNow.AddDays(-28), UpdatedAt = DateTime.UtcNow.AddDays(-12), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new() { Title = "Implement OAuth2 / SSO login",   Description = "Integrate Azure AD and Google OAuth so enterprise users can sign in with existing credentials.",             Type = ItemType.Feature,     Priority = Priority.Critical, Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(7),   CreatedAt = DateTime.UtcNow.AddDays(-20), UpdatedAt = DateTime.UtcNow.AddDays(-1),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new() { Title = "Fix broken password reset link", Description = "Password reset emails are delivering an expired token. Investigate token TTL settings.",                     Type = ItemType.Bug,         Priority = Priority.High,     Status = ItemStatus.Done,       DueDate = DateTime.UtcNow.AddDays(-5),  CreatedAt = DateTime.UtcNow.AddDays(-18), UpdatedAt = DateTime.UtcNow.AddDays(-6),  AssignedToId = bob?.Id,   CreatedById = alice?.Id },
                    new() { Title = "Mobile-responsive navbar",       Description = "Navbar collapses incorrectly on screens below 768 px. Needs Bootstrap 5 grid review.",                      Type = ItemType.Improvement, Priority = Priority.Medium,   Status = ItemStatus.InReview,   DueDate = DateTime.UtcNow.AddDays(4),   CreatedAt = DateTime.UtcNow.AddDays(-15), UpdatedAt = DateTime.UtcNow.AddDays(-2),  AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new() { Title = "Write API documentation",        Description = "Document all REST endpoints using Swagger / OpenAPI 3.0.",                                                  Type = ItemType.Task,        Priority = Priority.Low,      Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(14),  CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10), CreatedById = pm?.Id },
                    new() { Title = "Add session timeout warning",    Description = "Show a modal 2 minutes before the user session expires with option to extend.",                              Type = ItemType.Improvement, Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(10),  CreatedAt = DateTime.UtcNow.AddDays(-8),  UpdatedAt = DateTime.UtcNow.AddDays(-8),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                }
            });

            // ── Acme: Mobile App v2.0 ─────────────────────────────────────────────
            await EnsureProject(context, "Mobile App v2.0", new Project
            {
                Name           = "Mobile App v2.0",
                Description    = "Major release adding push notifications, dark mode, and cross-platform performance improvements.",
                Status         = ProjectStatus.Active,
                DueDate        = DateTime.UtcNow.AddMonths(3),
                CreatedAt      = DateTime.UtcNow.AddDays(-45),
                OwnerId        = pm?.Id,
                OrganisationId = acmeId,
                WorkItems      = new List<WorkItem>
                {
                    new() { Title = "Implement push notifications",     Description = "Integrate Firebase Cloud Messaging for iOS and Android. Include notification preference settings.",         Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(10),  CreatedAt = DateTime.UtcNow.AddDays(-40), UpdatedAt = DateTime.UtcNow.AddDays(-3),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new() { Title = "Fix crash on iOS 17 launch",       Description = "App crashes immediately on startup for users on iOS 17.0.1. Stack trace points to UIWindowScene initialisation.", Type = ItemType.Bug,    Priority = Priority.Critical, Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(2),   CreatedAt = DateTime.UtcNow.AddDays(-5),  UpdatedAt = DateTime.UtcNow.AddDays(-1),  AssignedToId = bob?.Id,   CreatedById = alice?.Id },
                    new() { Title = "Add dark mode support",            Description = "Implement system-aware dark theme using adaptive colours across all screens.",                              Type = ItemType.Feature,     Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(21),  CreatedAt = DateTime.UtcNow.AddDays(-35), UpdatedAt = DateTime.UtcNow.AddDays(-35), AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new() { Title = "Performance profiling & optimise", Description = "Profile cold start time and in-app navigation. Target < 2s cold start on mid-range devices.",              Type = ItemType.Task,        Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(28),  CreatedAt = DateTime.UtcNow.AddDays(-20), UpdatedAt = DateTime.UtcNow.AddDays(-20), CreatedById = pm?.Id },
                    new() { Title = "App Store screenshot refresh",     Description = "Update App Store and Play Store screenshots to reflect new v2.0 UI.",                                     Type = ItemType.Task,        Priority = Priority.Low,      Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(35),  CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10), CreatedById = pm?.Id },
                    new() { Title = "Offline mode data sync",           Description = "Cache key data locally and sync when connectivity is restored. Use SQLite + background sync service.",    Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.InReview,   DueDate = DateTime.UtcNow.AddDays(6),   CreatedAt = DateTime.UtcNow.AddDays(-25), UpdatedAt = DateTime.UtcNow.AddDays(-2),  AssignedToId = alice?.Id, CreatedById = pm?.Id },
                }
            });

            // ── Acme: Internal HR Portal ──────────────────────────────────────────
            await EnsureProject(context, "Internal HR Portal", new Project
            {
                Name           = "Internal HR Portal",
                Description    = "Self-service HR platform for employee onboarding, leave requests, and payroll viewing.",
                Status         = ProjectStatus.Completed,
                DueDate        = DateTime.UtcNow.AddDays(-15),
                CreatedAt      = DateTime.UtcNow.AddDays(-90),
                OwnerId        = pm?.Id,
                OrganisationId = acmeId,
                WorkItems      = new List<WorkItem>
                {
                    new() { Title = "Employee onboarding form",        Description = "Multi-step onboarding wizard that captures personal details, tax info, and emergency contacts.",            Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-60), CreatedAt = DateTime.UtcNow.AddDays(-85), UpdatedAt = DateTime.UtcNow.AddDays(-62), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new() { Title = "Leave request approval workflow", Description = "Submit, approve, and track annual, sick, and family responsibility leave.",                                  Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-45), CreatedAt = DateTime.UtcNow.AddDays(-80), UpdatedAt = DateTime.UtcNow.AddDays(-46), AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new() { Title = "Payroll integration with Sage",   Description = "Read-only payslip display via Sage 300 People REST API. Token-based auth.",                                 Type = ItemType.Feature,     Priority = Priority.Critical, Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-30), CreatedAt = DateTime.UtcNow.AddDays(-75), UpdatedAt = DateTime.UtcNow.AddDays(-32), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                    new() { Title = "User acceptance testing (UAT)",   Description = "Coordinate UAT sessions with HR team. Document and resolve all P1/P2 issues before go-live.",               Type = ItemType.Task,        Priority = Priority.Medium,   Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-20), CreatedAt = DateTime.UtcNow.AddDays(-35), UpdatedAt = DateTime.UtcNow.AddDays(-22), AssignedToId = bob?.Id,   CreatedById = pm?.Id },
                    new() { Title = "POPI Act compliance review",      Description = "Audit data collection forms and storage to ensure POPI Act compliance. Add consent checkboxes.",            Type = ItemType.Improvement, Priority = Priority.High,     Status = ItemStatus.Done, DueDate = DateTime.UtcNow.AddDays(-18), CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-19), AssignedToId = alice?.Id, CreatedById = pm?.Id },
                }
            });

            // ── Nova Labs: AI Chatbot Platform ────────────────────────────────────
            await EnsureProject(context, "AI Chatbot Platform", new Project
            {
                Name           = "AI Chatbot Platform",
                Description    = "Build an internal LLM-powered support bot that handles tier-1 queries automatically.",
                Status         = ProjectStatus.Active,
                DueDate        = DateTime.UtcNow.AddMonths(2),
                CreatedAt      = DateTime.UtcNow.AddDays(-30),
                OwnerId        = charlie?.Id,
                OrganisationId = novaId,
                WorkItems      = new List<WorkItem>
                {
                    new() { Title = "Integrate OpenAI API",            Description = "Wire up GPT-4o via the OpenAI SDK. Handle streaming responses and token limits.",                           Type = ItemType.Feature,     Priority = Priority.Critical, Status = ItemStatus.InProgress, DueDate = DateTime.UtcNow.AddDays(5),   CreatedAt = DateTime.UtcNow.AddDays(-28), UpdatedAt = DateTime.UtcNow.AddDays(-2),  AssignedToId = charlie?.Id, CreatedById = charlie?.Id },
                    new() { Title = "Build conversation history store", Description = "Persist chat sessions in SQLite with a 30-day rolling window.",                                            Type = ItemType.Feature,     Priority = Priority.High,     Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(14),  CreatedAt = DateTime.UtcNow.AddDays(-20), UpdatedAt = DateTime.UtcNow.AddDays(-20), AssignedToId = charlie?.Id, CreatedById = charlie?.Id },
                    new() { Title = "Rate limiting & abuse prevention", Description = "Cap requests per user per minute. Block prompt injection attempts.",                                        Type = ItemType.Improvement, Priority = Priority.Medium,   Status = ItemStatus.Todo,       DueDate = DateTime.UtcNow.AddDays(21),  CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10), CreatedById = charlie?.Id },
                }
            });

            // ── Nova Labs: Developer Dashboard ────────────────────────────────────
            await EnsureProject(context, "Developer Dashboard", new Project
            {
                Name           = "Developer Dashboard",
                Description    = "Unified metrics dashboard aggregating CI/CD pipeline status, error rates, and deployment history.",
                Status         = ProjectStatus.Active,
                DueDate        = DateTime.UtcNow.AddMonths(1),
                CreatedAt      = DateTime.UtcNow.AddDays(-15),
                OwnerId        = charlie?.Id,
                OrganisationId = novaId,
                WorkItems      = new List<WorkItem>
                {
                    new() { Title = "GitHub Actions pipeline widget", Description = "Pull live workflow run status via GitHub REST API. Show last 10 runs per repo.",                              Type = ItemType.Feature, Priority = Priority.High,   Status = ItemStatus.InReview, DueDate = DateTime.UtcNow.AddDays(4),  CreatedAt = DateTime.UtcNow.AddDays(-12), UpdatedAt = DateTime.UtcNow.AddDays(-1),  AssignedToId = charlie?.Id, CreatedById = charlie?.Id },
                    new() { Title = "Error rate chart (Sentry)",      Description = "Embed Sentry error frequency chart. Alert threshold badge when error rate > 1%.",                            Type = ItemType.Feature, Priority = Priority.Medium, Status = ItemStatus.Todo,      DueDate = DateTime.UtcNow.AddDays(10), CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10), AssignedToId = charlie?.Id, CreatedById = charlie?.Id },
                }
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Finds an org by invite code; creates it if it doesn't exist yet.</summary>
        private static async Task<Organisation?> EnsureOrganisation(
            AppDbContext context, string inviteCode, string name, string description,
            int createdDaysAgo, string? ownerId)
        {
            var org = await context.Organisations.FirstOrDefaultAsync(o => o.InviteCode == inviteCode);
            if (org != null) return org;

            org = new Organisation
            {
                Name        = name,
                Description = description,
                InviteCode  = inviteCode,
                CreatedAt   = DateTime.UtcNow.AddDays(-createdDaysAgo),
                OwnerId     = ownerId
            };
            context.Organisations.Add(org);
            await context.SaveChangesAsync();   // needed so OrganisationId is populated before member rows
            return org;
        }

        /// <summary>Adds a membership row only if it doesn't already exist.</summary>
        private static async Task EnsureMember(AppDbContext context, int orgId, string? userId, int daysAgo)
        {
            if (userId == null) return;
            var exists = await context.OrganisationMembers
                .AnyAsync(m => m.OrganisationId == orgId && m.UserId == userId);
            if (!exists)
                context.OrganisationMembers.Add(new OrganisationMember
                {
                    OrganisationId = orgId,
                    UserId         = userId,
                    JoinedAt       = DateTime.UtcNow.AddDays(-daysAgo)
                });
        }

        /// <summary>Creates a project (with its work items) only if none exists with that name.</summary>
        private static async Task EnsureProject(AppDbContext context, string name, Project project)
        {
            if (!await context.Projects.AnyAsync(p => p.Name == name))
            {
                context.Projects.Add(project);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>Creates a user with the given role if they don't already exist.</summary>
        private static async Task<ApplicationUser?> EnsureUser(
            UserManager<ApplicationUser> um, string email, string fullName, string role)
        {
            var user = await um.FindByEmailAsync(email);
            if (user != null) return user;

            user = new ApplicationUser
            {
                UserName       = email,
                Email          = email,
                FullName       = fullName,
                EmailConfirmed = true
            };

            var result = await um.CreateAsync(user, Password);
            if (result.Succeeded)
                await um.AddToRoleAsync(user, role);

            return user;
        }
    }
}
