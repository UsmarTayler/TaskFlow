using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Models
{
    /// <summary>
    /// Extends the built-in ASP.NET Core Identity user with a display name.
    /// All authentication (passwords, tokens, lockout) is handled by IdentityUser.
    /// Roles used in this application: "Admin", "ProjectManager", "Developer"
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Full name shown in the UI (navbar avatar, assignment dropdowns, etc.)
        public string FullName { get; set; } = string.Empty;
    }
}
