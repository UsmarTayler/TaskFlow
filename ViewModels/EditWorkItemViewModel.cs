using TaskFlow.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Binds the Edit Work Item form — holds all user-editable fields plus the
    /// hidden identifiers needed for routing after a successful save.
    /// </summary>
    public class EditWorkItemViewModel
    {
        // Hidden fields — identify the item and its parent project
        public int WorkItemId { get; set; }
        public int ProjectId  { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title must be 200 characters or fewer.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description must be 2000 characters or fewer.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Type")]
        public ItemType Type { get; set; } = ItemType.Task;

        [Display(Name = "Priority")]
        public Priority Priority { get; set; } = Priority.Medium;

        // Allow full status changes from the edit form (not just the quick-update dropdown)
        [Display(Name = "Status")]
        public ItemStatus Status { get; set; } = ItemStatus.Todo;

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        // Nullable — null means the task is unassigned
        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }
    }
}
