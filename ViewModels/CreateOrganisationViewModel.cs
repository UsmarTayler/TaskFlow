using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    public class CreateOrganisationViewModel
    {
        [Required(ErrorMessage = "Organisation name is required.")]
        [StringLength(100, ErrorMessage = "Name must be 100 characters or fewer.")]
        [Display(Name = "Organisation Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}
