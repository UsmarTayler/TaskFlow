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
        public DbSet<Project>            Projects            { get; set; }
        public DbSet<WorkItem>           WorkItems           { get; set; }
        public DbSet<WorkItemHistory>    WorkItemHistories   { get; set; }
        public DbSet<WorkItemComment>    WorkItemComments    { get; set; }
        public DbSet<Organisation>       Organisations       { get; set; }
        public DbSet<OrganisationMember> OrganisationMembers { get; set; }

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

            // Project → Owner
            builder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Project → Organisation (SetNull — project becomes personal if org is deleted)
            builder.Entity<Project>()
                .HasOne(p => p.Organisation)
                .WithMany(o => o.Projects)
                .HasForeignKey(p => p.OrganisationId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItem → Project (cascade — deleting a project removes all its tasks)
            builder.Entity<WorkItem>()
                .HasOne(w => w.Project)
                .WithMany(p => p.WorkItems)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkItem → AssignedTo
            builder.Entity<WorkItem>()
                .HasOne(w => w.AssignedTo)
                .WithMany()
                .HasForeignKey(w => w.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItem → CreatedBy
            builder.Entity<WorkItem>()
                .HasOne(w => w.CreatedBy)
                .WithMany()
                .HasForeignKey(w => w.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItemHistory → WorkItem (cascade)
            builder.Entity<WorkItemHistory>()
                .HasOne(h => h.WorkItem)
                .WithMany()
                .HasForeignKey(h => h.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkItemHistory → ChangedBy
            builder.Entity<WorkItemHistory>()
                .HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkItemComment → WorkItem (cascade — comments deleted with the task)
            builder.Entity<WorkItemComment>()
                .HasOne(c => c.WorkItem)
                .WithMany()
                .HasForeignKey(c => c.WorkItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkItemComment → Author (SetNull — keep comment if author's account is deleted)
            builder.Entity<WorkItemComment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Organisation → Owner
            builder.Entity<Organisation>()
                .HasOne(o => o.Owner)
                .WithMany()
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ensure invite codes are unique across all organisations
            builder.Entity<Organisation>()
                .HasIndex(o => o.InviteCode)
                .IsUnique();

            // OrganisationMember → Organisation (cascade — memberships removed when org deleted)
            builder.Entity<OrganisationMember>()
                .HasOne(m => m.Organisation)
                .WithMany(o => o.Members)
                .HasForeignKey(m => m.OrganisationId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrganisationMember → User (cascade — memberships removed when user account deleted)
            builder.Entity<OrganisationMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent a user from being added to the same org twice
            builder.Entity<OrganisationMember>()
                .HasIndex(m => new { m.OrganisationId, m.UserId })
                .IsUnique();
        }
    }
}
