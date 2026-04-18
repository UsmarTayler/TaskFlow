using TaskFlow.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Binds the Edit Project form — mirrors the editable subset of Project fields.
    /// ProjectId is included so the POST handler knows which row to update.
    /// </summary>
    public class EditProjectViewModel
    {
        // Hidden field — identifies the project being edited
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(120, ErrorMessage = "Name must be 120 characters or fewer.")]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description must be 1000 characters or fewer.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Status is editable so PMs can mark projects as On Hold, Completed, or Archived
        [Display(Name = "Status")]
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }
    }
}
