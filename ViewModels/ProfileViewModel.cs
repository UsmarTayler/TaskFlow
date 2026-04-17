using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Shared view model for both GET (display) and POST (edit) on the profile page.
    /// Read-only stats (email, roles, task counts) are populated by the controller
    /// from the database on GET; editable fields (name, password) are bound on POST.
    /// </summary>
    public class ProfileViewModel
    {
        // ── Read-only display info ────────────────────────────────────────────────
        // These are not posted back on form submit — they're re-fetched from the DB each time

        public string       Email          { get; set; } = string.Empty;
        public List<string> Roles          { get; set; } = new();

        // Task statistics shown as summary cards on the profile page
        public int          TasksAssigned  { get; set; }
        public int          TasksCompleted { get; set; }

        // ── Editable fields ───────────────────────────────────────────────────────

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // ── Password change section (all optional) ────────────────────────────────
        // The controller removes these from ModelState validation if NewPassword is blank,
        // so leaving all three empty simply skips the password-change step

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        // [Compare] validates that ConfirmNewPassword matches NewPassword on both sides
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
