using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    /// <summary>
    /// Represents a project — the top-level container that groups related work items.
    /// Projects are owned by a ProjectManager or Admin and can have many WorkItems.
    /// </summary>
    public class Project
    {
        // Primary key — EF Core auto-generates this as an identity column
        public int ProjectId { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Current lifecycle state of the project (Active, OnHold, Completed, Archived)
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        // Automatically set to the current UTC time when the project is created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to ApplicationUser — nullable so deleting a PM doesn't delete the project
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        // FK → Organisation — null means this is a personal project visible only to the owner.
        // When set, all members of that organisation can see the project.
        // SetNull on org delete so the project is demoted to personal rather than deleted.
        public int? OrganisationId { get; set; }
        public Organisation? Organisation { get; set; }

        // Navigation property — EF Core loads work items via Include() in queries
        public List<WorkItem> WorkItems { get; set; } = new();

        // ── Computed helpers (evaluated in memory, not stored in the database) ────

        // Total number of work items belonging to this project
        public int TotalItems  => WorkItems.Count;

        // Number of work items that have been marked as Done
        public int DoneItems   => WorkItems.Count(w => w.Status == ItemStatus.Done);

        // Percentage of tasks completed — used to render the progress bar in the UI
        public int ProgressPct => TotalItems == 0 ? 0 : (int)Math.Round(DoneItems * 100.0 / TotalItems);
    }
}
