using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Handles all user identity actions: login, registration, logout,
    /// profile editing, and the access-denied landing page.
    /// No [Authorize] attribute at the class level — individual actions
    /// that require authentication opt in with the attribute directly.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser>   _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IProjectService _projectService;

        public AccountController(
            UserManager<ApplicationUser>   userManager,
            SignInManager<ApplicationUser> signInManager,
            IProjectService projectService)
        {
            _userManager    = userManager;
            _signInManager  = signInManager;
            _projectService = projectService;
        }

        // ── Login ─────────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Store the returnUrl so we can redirect back after a successful login
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            // PasswordSignInAsync handles password hashing comparison internally
            // lockoutOnFailure: false disables the account lockout feature for the demo
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return await RedirectToLocal(returnUrl);

            // Add a generic error (avoids revealing whether the email or password was wrong)
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ── Register ──────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName       = model.Email,
                Email          = model.Email,
                FullName       = model.FullName,
                EmailConfirmed = true  // skips email confirmation — fine for an internal tool
            };

            // CreateAsync hashes the password before storing it in the database
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // All self-registered users get the Developer role by default;
                // an Admin can promote them via the User Management page
                await _userManager.AddToRoleAsync(user, "Developer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                // New developers go straight to My Tasks — not the org-wide dashboard
                return RedirectToAction("MyTasks", "WorkItems");
            }

            // Surface Identity validation errors (e.g. "Password too short") to the user
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ── Logout ────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Signs out by deleting the authentication cookie
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // ── Profile ───────────────────────────────────────────────────────────────

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var roles   = await _userManager.GetRolesAsync(user);
            var myItems = await _projectService.GetWorkItemsByAssigneeAsync(user.Id);

            // Build the view model with stats so the profile page can show task counts
            var vm = new ProfileViewModel
            {
                FullName       = user.FullName,
                Email          = user.Email ?? "",
                Roles          = roles.ToList(),
                TasksAssigned  = myItems.Count,
                TasksCompleted = myItems.Count(w => w.Status == ItemStatus.Done)
            };

            return View(vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            // The password fields are only required when the user is actually changing their password.
            // If they're left blank, remove those keys from ModelState so validation passes.
            bool changingPassword = !string.IsNullOrWhiteSpace(model.NewPassword);
            if (!changingPassword)
            {
                ModelState.Remove(nameof(model.CurrentPassword));
                ModelState.Remove(nameof(model.NewPassword));
                ModelState.Remove(nameof(model.ConfirmNewPassword));
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            // Update the display name
            user.FullName = model.FullName;
            await _userManager.UpdateAsync(user);

            // Only attempt a password change if the user filled in the new password field
            if (changingPassword)
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is required.");
                    return View(model);
                }

                // ChangePasswordAsync verifies the current password before updating
                var result = await _userManager.ChangePasswordAsync(
                    user, model.CurrentPassword, model.NewPassword!);

                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ── Forgot Password ───────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Always show the confirmation view — never reveal whether an email exists
            if (user == null)
                return View("ForgotPasswordConfirmation", (string?)null);

            // Generate a password reset token using ASP.NET Core Identity's token provider
            var token     = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(
                "ResetPassword", "Account",
                new { token, email = user.Email },
                Request.Scheme)!;

            // In production this link would be emailed. For this demo we display it directly.
            return View("ForgotPasswordConfirmation", resetLink);
        }

        // ── Reset Password ────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult ResetPassword(string? token, string? email)
        {
            if (token == null || email == null)
                return RedirectToAction("ForgotPassword");

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // If user not found, redirect to confirmation anyway (don't reveal existence)
            if (user == null)
                return View("ResetPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
                return View("ResetPasswordConfirmation");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ── Access Denied ─────────────────────────────────────────────────────────

        // Shown when a user is authenticated but tries to access a route their role can't reach
        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ── Private helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Redirects to <paramref name="returnUrl"/> if it is local to prevent open-redirect attacks,
        /// otherwise routes the user to the right landing page based on their role:
        /// Developers go straight to My Tasks; Admins and PMs go to the Dashboard.
        /// </summary>
        private async Task<IActionResult> RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            // Send developers straight to their own task list — they don't need the org-wide dashboard
            var user = await _userManager.GetUserAsync(User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Developer"))
                return RedirectToAction("MyTasks", "WorkItems");

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
