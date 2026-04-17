using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Carries the login form data.
    /// Kept deliberately minimal — only the fields the login form actually uses.
    /// </summary>
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]  // renders as <input type="password"> via tag helpers
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        // Maps to the "Remember me" checkbox — tells Identity to issue a persistent cookie
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
