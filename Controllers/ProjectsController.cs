using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Handles project listing, detail view, creation, editing, and deletion.
    /// Non-admin users only see projects in their organisations or personal projects they own.
    /// </summary>
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projects;
        private readonly IOrganisationService _orgs;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(
            IProjectService projects,
            IOrganisationService orgs,
            UserManager<ApplicationUser> userManager)
        {
            _projects    = projects;
            _orgs        = orgs;
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
            var userId = _userManager.GetUserId(User)!;

            // Admins see every project; everyone else is scoped to their orgs + personal projects
            var list = User.IsInRole("Admin")
                ? await _projects.GetAllProjectsAsync()
                : await _projects.GetProjectsForUserAsync(userId);

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

        // GET: show the empty create form with org dropdown pre-populated
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User)!;
            ViewBag.Organisations = User.IsInRole("Admin")
                ? await _orgs.GetAllOrganisationsAsync()
                : await _orgs.GetOrganisationsForUserAsync(userId);
            return View(new CreateProjectViewModel());
        }

        // POST: validate and persist the new project
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var uid = _userManager.GetUserId(User)!;
                ViewBag.Organisations = User.IsInRole("Admin")
                    ? await _orgs.GetAllOrganisationsAsync()
                    : await _orgs.GetOrganisationsForUserAsync(uid);
                return View(model);
            }

            var project = new Project
            {
                Name           = model.Name,
                Description    = model.Description,
                DueDate        = model.DueDate,
                OrganisationId = model.OrganisationId,   // null = personal project
                OwnerId        = _userManager.GetUserId(User)
            };

            await _projects.CreateProjectAsync(project);
            TempData["Success"] = $"Project \"{project.Name}\" created.";

            // Redirect to the new project's detail page after a successful save
            return RedirectToAction(nameof(Detail), new { id = project.ProjectId });
        }

        // ── Edit project ──────────────────────────────────────────────────────────

        // GET: pre-populate the edit form with existing values
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _projects.GetProjectByIdAsync(id);
            if (project is null) return NotFound();

            var model = new EditProjectViewModel
            {
                ProjectId   = project.ProjectId,
                Name        = project.Name,
                Description = project.Description,
                Status      = project.Status,
                DueDate     = project.DueDate
            };
            return View(model);
        }

        // POST: validate changes and persist them
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var project = new Project
            {
                ProjectId   = model.ProjectId,
                Name        = model.Name,
                Description = model.Description,
                Status      = model.Status,
                DueDate     = model.DueDate
            };

            var ok = await _projects.UpdateProjectAsync(project);
            if (!ok) return NotFound();

            TempData["Success"] = $"Project \"{model.Name}\" updated.";
            return RedirectToAction(nameof(Detail), new { id = model.ProjectId });
        }

        // ── Delete project ────────────────────────────────────────────────────────

        /// <summary>
        /// Permanently deletes a project and all its work items.
        /// Restricted to Admins only — PMs can edit but not delete.
        /// The confirmation prompt lives in the UI (no separate GET step needed).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _projects.DeleteProjectAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Project deleted." : "Project not found.";
            return RedirectToAction(nameof(Index));
        }

        // ── Kanban board ──────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the Kanban board view for a specific project.
        /// Work items are grouped into status columns; drag-and-drop updates status via fetch.
        /// </summary>
        public async Task<IActionResult> Kanban(int id)
        {
            var project = await _projects.GetProjectByIdAsync(id);
            if (project is null) return NotFound();
            return View(project);
        }
    }
}
