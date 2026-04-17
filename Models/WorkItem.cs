using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskFlow.Models
{
    /// <summary>
    /// Represents a single unit of work (task, bug, feature, improvement) within a project.
    /// Named WorkItem rather than Task to avoid naming conflicts with System.Threading.Tasks.Task.
    /// </summary>
    public class WorkItem
    {
        // Primary key — auto-generated identity column
        public int WorkItemId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Classifies the kind of work (Feature, Bug, Task, Improvement)
        [Display(Name = "Type")]
        public ItemType Type { get; set; } = ItemType.Task;

        // How urgent the item is (Low → Critical)
        [Display(Name = "Priority")]
        public Priority Priority { get; set; } = Priority.Medium;

        // Tracks where the item is in the workflow (Todo → InProgress → InReview → Done)
        [Display(Name = "Status")]
        public ItemStatus Status { get; set; } = ItemStatus.Todo;

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        // CreatedAt is set once when the item is first saved; UpdatedAt is refreshed on every change
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // FK → parent project; cascade-delete is configured in AppDbContext so
        // deleting a project removes all its work items automatically
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        // FK → the developer assigned to this item; SetNull on user delete keeps the item intact
        public string? AssignedToId { get; set; }
        public ApplicationUser? AssignedTo { get; set; }

        // FK → the user who created this item (PM or Admin); also SetNull on user delete
        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        // [NotMapped] means EF Core does not try to persist this property to the database.
        // True when the item has a past due date and hasn't been completed yet.
        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && Status != ItemStatus.Done;
    }
}
