using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Manages organisations: creating, viewing, joining via invite code,
    /// and managing members. Any authenticated user can create or join an org.
    /// Only the org owner and site Admins can add/remove members.
    /// </summary>
    [Authorize]
    public class OrganisationsController : Controller
    {
        private readonly IOrganisationService _orgs;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganisationsController(IOrganisationService orgs, UserManager<ApplicationUser> userManager)
        {
            _orgs        = orgs;
            _userManager = userManager;
        }

        // ── My organisations ──────────────────────────────────────────────────────

        /// <summary>Lists all organisations the current user belongs to.</summary>
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            // Admins see every organisation; everyone else sees only their own
            var list = User.IsInRole("Admin")
                ? await _orgs.GetAllOrganisationsAsync()
                : await _orgs.GetOrganisationsForUserAsync(userId);

            return View(list);
        }

        // ── Organisation detail ───────────────────────────────────────────────────

        public async Task<IActionResult> Detail(int id)
        {
            var org = await _orgs.GetOrganisationByIdAsync(id);
            if (org is null) return NotFound();

            // Non-admins may only view orgs they belong to
            var userId = _userManager.GetUserId(User)!;
            if (!User.IsInRole("Admin") && !org.Members.Any(m => m.UserId == userId))
                return Forbid();

            return View(org);
        }

        // ── Create organisation ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Create() => View(new CreateOrganisationViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrganisationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User)!;
            var org = new Organisation
            {
                Name        = model.Name,
                Description = model.Description,
                OwnerId     = userId
            };

            await _orgs.CreateOrganisationAsync(org);
            TempData["Success"] = $"Organisation \"{org.Name}\" created. Your invite code is {org.InviteCode}.";
            return RedirectToAction(nameof(Detail), new { id = org.OrganisationId });
        }

        // ── Join via invite code ──────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Join() => View(new JoinOrganisationViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinOrganisationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = _userManager.GetUserId(User)!;
            var ok = await _orgs.JoinByInviteCodeAsync(model.InviteCode, userId);

            if (!ok)
            {
                // Code not found — show an inline validation error
                ModelState.AddModelError(nameof(model.InviteCode),
                    "Invite code not found. Please check the code and try again.");
                return View(model);
            }

            TempData["Success"] = "You have joined the organisation!";
            return RedirectToAction(nameof(Index));
        }

        // ── Add member by email (owner / Admin) ───────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int orgId, string email)
        {
            var org = await _orgs.GetOrganisationByIdAsync(orgId);
            if (org is null) return NotFound();

            // Only admin or the org owner may add members
            var currentUserId = _userManager.GetUserId(User)!;
            if (!User.IsInRole("Admin") && org.OwnerId != currentUserId)
                return Forbid();

            var target = await _userManager.FindByEmailAsync(email?.Trim() ?? "");
            if (target is null)
            {
                TempData["Error"] = $"No account found for '{email}'.";
                return RedirectToAction(nameof(Detail), new { id = orgId });
            }

            var ok = await _orgs.AddMemberAsync(orgId, target.Id);
            TempData[ok ? "Success" : "Error"] = ok
                ? $"{target.FullName} added to {org.Name}."
                : $"{target.FullName} is already a member.";

            return RedirectToAction(nameof(Detail), new { id = orgId });
        }

        // ── Remove member (owner / Admin) ─────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int orgId, string userId)
        {
            var org = await _orgs.GetOrganisationByIdAsync(orgId);
            if (org is null) return NotFound();

            var currentUserId = _userManager.GetUserId(User)!;
            if (!User.IsInRole("Admin") && org.OwnerId != currentUserId)
                return Forbid();

            // Prevent the owner from being removed — they must transfer ownership first
            if (userId == org.OwnerId)
            {
                TempData["Error"] = "The organisation owner cannot be removed.";
                return RedirectToAction(nameof(Detail), new { id = orgId });
            }

            await _orgs.RemoveMemberAsync(orgId, userId);
            TempData["Success"] = "Member removed.";
            return RedirectToAction(nameof(Detail), new { id = orgId });
        }
    }
}
