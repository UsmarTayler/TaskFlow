using TaskFlow.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Data
{
    /// <summary>
    /// The application's EF Core database context.
    /// Inherits from IdentityDbContext so that ASP.NET Core Identity tables
    /// (Users, Roles, UserRoles, etc.) are created in the same SQLite database.
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        // The options (connection string, provider) are injected by DI from Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet properties — each one maps to a table in the database
        public DbSet<Project>  Projects  { get; set; }
        public DbSet<WorkItem> WorkItems { get; set; }

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
        }
    }
}
