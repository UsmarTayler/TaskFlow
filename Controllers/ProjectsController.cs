using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Handles project listing, detail view, and project creation.
    /// All actions require authentication; only Admins and ProjectManagers can create projects.
    /// </summary>
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projects;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(IProjectService projects, UserManager<ApplicationUser> userManager)
        {
            _projects    = projects;
            _userManager = userManager;
        }

        // ── List all projects (with optional search + status filter) ──────────────

        /// <summary>
        /// Returns a filtered list of projects.
        /// <paramref name="q"/> is matched against name and description (case-insensitive).
        /// <paramref name="status"/> filters to a specific ProjectStatus enum value.
        /// Both are optional — omitting them returns all projects.
        /// </summary>
        public async Task<IActionResult> Index(string? q, string? status)
        {
            var list = await _projects.GetAllProjectsAsync();

            // Text search — filters in memory after the DB query (acceptable at demo scale)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var filter = q.Trim().ToLower();
                list = list.Where(p =>
                    p.Name.ToLower().Contains(filter) ||
                    (p.Description ?? "").ToLower().Contains(filter)
                ).ToList();
            }

            // Status filter — TryParse safely handles invalid/missing enum values
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ProjectStatus>(status, out var parsedStatus))
            {
                list = list.Where(p => p.Status == parsedStatus).ToList();
            }

            // Pass the current filter values back to the view so the search inputs stay populated
            ViewBag.Query  = q;
            ViewBag.Status = status;
            return View(list);
        }

        // ── Project detail (shows all work items for this project) ────────────────

        public async Task<IActionResult> Detail(int id)
        {
            var project = await _projects.GetProjectByIdAsync(id);
            if (project is null) return NotFound();  // triggers the 404 error page
            return View(project);
        }

        // ── Create project ────────────────────────────────────────────────────────

        // GET: show the empty create form
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public IActionResult Create() => View(new CreateProjectViewModel());

        // POST: validate and persist the new project
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]  // prevents cross-site request forgery on form submissions
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            // If validation attributes on the ViewModel failed, redisplay the form with errors
            if (!ModelState.IsValid) return View(model);

            var project = new Project
            {
                Name        = model.Name,
                Description = model.Description,
                DueDate     = model.DueDate,
                OwnerId     = _userManager.GetUserId(User)  // set the logged-in user as owner
            };

            await _projects.CreateProjectAsync(project);
            TempData["Success"] = $"Project \"{project.Name}\" created.";

            // Redirect to the new project's detail page after a successful save
            return RedirectToAction(nameof(Detail), new { id = project.ProjectId });
        }
    }
}
