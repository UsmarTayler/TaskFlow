using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";
    }
}
