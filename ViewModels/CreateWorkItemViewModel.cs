using TaskFlow.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Carries the form data for adding a new work item to a project.
    /// Separating this from the WorkItem model avoids exposing database fields
    /// (e.g. CreatedById, CreatedAt) to the form binding layer.
    /// </summary>
    public class CreateWorkItemViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Enum dropdowns — rendered with asp-items="Html.GetEnumSelectList<T>()" in the view
        [Display(Name = "Type")]
        public ItemType Type { get; set; } = ItemType.Task;

        [Display(Name = "Priority")]
        public Priority Priority { get; set; } = Priority.Medium;

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        // The ID of the developer to assign; null means "unassigned"
        [Display(Name = "Assign To")]
        public string? AssignedToId { get; set; }

        // Passed as a hidden field in the form — not entered by the user directly
        public int ProjectId { get; set; }
    }
}
