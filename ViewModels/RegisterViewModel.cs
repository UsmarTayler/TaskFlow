using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Carries the registration form data.
    /// Validation attributes enforce the same password rules configured in Program.cs
    /// on the client side (via jQuery Unobtrusive Validation) and server side.
    /// </summary>
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]  // DataType.Password renders as <input type="password">
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        // [Compare] performs client-side and server-side confirmation matching
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
