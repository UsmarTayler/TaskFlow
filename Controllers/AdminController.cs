using TaskFlow.Models;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Provides admin-only functionality: viewing all users and changing their roles.
    /// The [Authorize(Roles = "Admin")] attribute on the class means every action
    /// here is off-limits to ProjectManagers and Developers — they'll hit AccessDenied.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole>    _roleManager;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ── User list ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Displays all registered users with their current role(s) and a
        /// "Change Role" dropdown for each one.
        /// </summary>
        public async Task<IActionResult> Users()
        {
            // Fetch all defined roles to populate the dropdown options
            var allRoles = _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToList();

            var rows = new List<UserRow>();

            // GetRolesAsync needs to be awaited per-user, so we loop rather than using Select
            foreach (var user in _userManager.Users.OrderBy(u => u.FullName).ToList())
            {
                rows.Add(new UserRow
                {
                    Id       = user.Id,
                    FullName = user.FullName,
                    Email    = user.Email ?? "",
                    Roles    = (await _userManager.GetRolesAsync(user)).ToList()
                });
            }

            return View(new UserManagementViewModel { Users = rows, AllRoles = allRoles });
        }

        // ── Change a user's role ──────────────────────────────────────────────────

        /// <summary>
        /// Removes every role from the user and assigns them to <paramref name="newRole"/>.
        /// This enforces a single-role model — a user can only hold one role at a time.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Strip all existing roles before assigning the new one
            // (supports the "one role per user" model used in this application)
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"{user.FullName} is now a {newRole}.";
            return RedirectToAction(nameof(Users));
        }
    }
}
