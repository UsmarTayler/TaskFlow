using TaskFlow.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TaskFlow.Data
{
    /// <summary>
    /// The application's EF Core database context.
    /// Inherits from IdentityDbContext so that ASP.NET Core Identity tables
    /// (Users, Roles, UserRoles, etc.) are created in the same SQLite database.
    /// Overrides SaveChangesAsync to automatically write a WorkItemHistory entry
    /// whenever a tracked WorkItem property changes.
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        // The options (connection string, provider) are injected by DI from Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet properties — each one maps to a table in the database
        public DbSet<Project>         Projects         { get; set; }
        public DbSet<WorkItem>        WorkItems        { get; set; }
        public DbSet<WorkItemHistory> WorkItemHistories { get; set; }

        // The current user's ID — set by controllers before calling SaveChangesAsync
        // so the audit log knows who made each change
        public string? CurrentUserId { get; set; }

        /// <summary>
        /// Intercepts every save to capture WorkItem changes before they are committed.
        /// Builds WorkItemHistory rows for any modified tracked properties, then saves everything
        /// in a single transaction so history and data are always in sync.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Fields we want to track — add more here as the app grows
            var trackedFields = new[]
            {
                nameof(WorkItem.Status),
                nameof(WorkItem.Priority),
                nameof(WorkItem.AssignedToId),
                nameof(WorkItem.Title),
                nameof(WorkItem.DueDate)
            };

            var historyEntries = new List<WorkItemHistory>();

            // Check every entity EF Core is tracking for changes
            foreach (EntityEntry<WorkItem> entry in ChangeTracker.Entries<WorkItem>())
            {
                // Only interested in items that already exist and have been modified
                if (entry.State != EntityState.Modified) continue;

                foreach (var field in trackedFields)
                {
                    var prop = entry.Property(field);

                    // Skip fields that haven't actually changed
                    if (!prop.IsModified) continue;

                    historyEntries.Add(new WorkItemHistory
                    {
                        WorkItemId  = entry.Entity.WorkItemId,
                        Field       = field,
                        OldValue    = prop.OriginalValue?.ToString(),
                        NewValue    = prop.CurrentValue?.ToString(),
                        ChangedById = CurrentUserId,
                        ChangedAt   = DateTime.UtcNow
                    });
                }
            }

            // Add the history rows — they will be saved in the same transaction below
            if (historyEntries.Count > 0)
                WorkItemHistories.AddRange(historyEntries);

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Configures relationships that EF Core cannot infer automatically.
        /// Called once during startup when the model is first built.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Let Identity set up its own tables first
            base.OnModelCreating(builder);

            // Project → Owner (many projects can have the same owner)
            // SetNull means: if the owner user is deleted, OwnerId becomes NULL
            // instead of the project being deleted too
            builder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItem → Project (one project contains many work items)
            // Cascade means: deleting a project automatically deletes all its tasks
            builder.Entity<WorkItem>()
                .HasOne(w => w.Project)
                .WithMany(p => p.WorkItems)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkItem → AssignedTo (nullable FK — item survives if the assigned dev is deleted)
            builder.Entity<WorkItem>()
                .HasOne(w => w.AssignedTo)
                .WithMany()
                .HasForeignKey(w => w.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItem → CreatedBy (nullable FK — item survives if the creator is deleted)
            // SetNull also avoids EF Core's "multiple cascade paths" error in SQL Server
            builder.Entity<WorkItem>()
                .HasOne(w => w.CreatedBy)
                .WithMany()
                .HasForeignKey(w => w.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItemHistory → WorkItem (cascade — deleting a task removes its history too)
            builder.Entity<WorkItemHistory>()
                .HasOne(h => h.WorkItem)
                .WithMany()
                .HasForeignKey(h => h.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkItemHistory → ChangedBy (SetNull — keep history if user is deleted)
            builder.Entity<WorkItemHistory>()
                .HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
