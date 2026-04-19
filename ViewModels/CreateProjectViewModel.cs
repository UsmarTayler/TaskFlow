using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Carries the form data for creating a new project.
    /// Using a dedicated ViewModel (rather than binding directly to the Project model)
    /// prevents over-posting attacks and keeps controller actions clean.
    /// </summary>
    public class CreateProjectViewModel
    {
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(120)]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Optional — date-only input rendered as <input type="date"> in the form
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        // Null = personal project (visible only to creator).
        // Set = org project (visible to all members of the selected organisation).
        [Display(Name = "Organisation")]
        public int? OrganisationId { get; set; }
    }
}
