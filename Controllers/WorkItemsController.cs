using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Manages individual work items: viewing, creating, updating status,
    /// reassigning, and exporting to CSV.
    /// All routes require authentication; create/assign/export are restricted to
    /// Admins and ProjectManagers.
    /// </summary>
    [Authorize]
    public class WorkItemsController : Controller
    {
        private readonly IProjectService _projects;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorkItemsController(IProjectService projects, UserManager<ApplicationUser> userManager)
        {
            _projects    = projects;
            _userManager = userManager;
        }

        // ── My Tasks — current user's assigned work items ─────────────────────────

        public async Task<IActionResult> MyTasks()
        {
            // GetUserId returns the NameIdentifier claim from the auth cookie
            var userId = _userManager.GetUserId(User)!;
            var items  = await _projects.GetWorkItemsByAssigneeAsync(userId);
            return View(items);
        }

        // ── Work item detail ──────────────────────────────────────────────────────

        public async Task<IActionResult> Detail(int id)
        {
            var item = await _projects.GetWorkItemByIdAsync(id);
            if (item is null) return NotFound();

            // Only Admins and PMs need the developer list (for the "Reassign" dropdown)
            // — avoid the extra DB query for regular developers
            if (User.IsInRole("Admin") || User.IsInRole("ProjectManager"))
            {
                var devs = await _userManager.GetUsersInRoleAsync("Developer");
                ViewBag.Developers = devs.OrderBy(d => d.FullName).ToList();
            }

            // Load comments and change history for the detail page
            ViewBag.Comments = await _projects.GetCommentsAsync(id);
            ViewBag.History  = await _projects.GetWorkItemHistoryAsync(id);

            return View(item);
        }

        // ── Create work item ──────────────────────────────────────────────────────

        // GET: render the empty create form pre-populated with the parent project ID
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Create(int projectId)
        {
            var project = await _projects.GetProjectByIdAsync(projectId);
            if (project is null) return NotFound();

            // Populate the "Assign To" dropdown with all Developer accounts
            var devs = await _userManager.GetUsersInRoleAsync("Developer");

            ViewBag.ProjectName = project.Name;
            ViewBag.Developers  = devs.OrderBy(d => d.FullName).ToList();

            return View(new CreateWorkItemViewModel { ProjectId = projectId });
        }

        // POST: validate the form and persist the new work item
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWorkItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate ViewBag data before redisplaying the form
                var devs = await _userManager.GetUsersInRoleAsync("Developer");
                ViewBag.Developers = devs.OrderBy(d => d.FullName).ToList();
                return View(model);
            }

            var item = new WorkItem
            {
                Title        = model.Title,
                Description  = model.Description,
                Type         = model.Type,
                Priority     = model.Priority,
                DueDate      = model.DueDate,
                ProjectId    = model.ProjectId,
                AssignedToId = model.AssignedToId,
                CreatedById  = _userManager.GetUserId(User)  // record who raised this task
            };

            await _projects.CreateWorkItemAsync(item);
            TempData["Success"] = $"Work item \"{item.Title}\" added.";

            // Return to the parent project's detail page after creation
            return RedirectToAction("Detail", "Projects", new { id = model.ProjectId });
        }

        // ── Update status ─────────────────────────────────────────────────────────

        /// <summary>
        /// Allows any authenticated user (including Developers) to advance their task's status.
        /// The form in the work item detail view POSTs here.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ItemStatus status, int projectId)
        {
            // Tell the service who is making this change so the audit log can record it
            _projects.SetCurrentUser(_userManager.GetUserId(User));
            var ok = await _projects.UpdateWorkItemStatusAsync(id, status);

            // Use TempData so the success/error message survives the redirect
            TempData[ok ? "Success" : "Error"] = ok ? "Status updated." : "Item not found.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ── Edit work item ────────────────────────────────────────────────────────

        // GET: pre-populate the full edit form
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _projects.GetWorkItemByIdAsync(id);
            if (item is null) return NotFound();

            // Populate developer dropdown (same as the Create form)
            var devs = await _userManager.GetUsersInRoleAsync("Developer");
            ViewBag.Developers = devs.OrderBy(d => d.FullName).ToList();

            var model = new EditWorkItemViewModel
            {
                WorkItemId   = item.WorkItemId,
                ProjectId    = item.ProjectId,
                Title        = item.Title,
                Description  = item.Description,
                Type         = item.Type,
                Priority     = item.Priority,
                Status       = item.Status,
                DueDate      = item.DueDate,
                AssignedToId = item.AssignedToId
            };
            return View(model);
        }

        // POST: validate and persist all editable fields
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditWorkItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var devs = await _userManager.GetUsersInRoleAsync("Developer");
                ViewBag.Developers = devs.OrderBy(d => d.FullName).ToList();
                return View(model);
            }

            // Set current user so the audit log records who made these changes
            _projects.SetCurrentUser(_userManager.GetUserId(User));

            var item = new WorkItem
            {
                WorkItemId   = model.WorkItemId,
                ProjectId    = model.ProjectId,
                Title        = model.Title,
                Description  = model.Description,
                Type         = model.Type,
                Priority     = model.Priority,
                Status       = model.Status,
                DueDate      = model.DueDate,
                AssignedToId = model.AssignedToId
            };

            var ok = await _projects.UpdateWorkItemAsync(item);
            if (!ok) return NotFound();

            TempData["Success"] = $"\"{model.Title}\" updated.";
            return RedirectToAction(nameof(Detail), new { id = model.WorkItemId });
        }

        // ── Delete work item ──────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int projectId)
        {
            var ok = await _projects.DeleteWorkItemAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Work item deleted." : "Item not found.";
            // Return to the parent project's detail page after deletion
            return RedirectToAction("Detail", "Projects", new { id = projectId });
        }

        // ── Comments ─────────────────────────────────────────────────────────────

        /// <summary>Any authenticated user can add a comment to a work item.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int workItemId, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["Error"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Detail), new { id = workItemId });
            }

            var comment = new WorkItemComment
            {
                WorkItemId = workItemId,
                Body       = body.Trim(),
                AuthorId   = _userManager.GetUserId(User)
            };

            await _projects.AddCommentAsync(comment);
            return RedirectToAction(nameof(Detail), new { id = workItemId });
        }

        /// <summary>
        /// Deletes a comment. Authors can delete their own; Admins and PMs can delete any.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int workItemId)
        {
            var comment = await _projects.GetCommentByIdAsync(commentId);
            if (comment is null)
                return RedirectToAction(nameof(Detail), new { id = workItemId });

            var userId = _userManager.GetUserId(User)!;
            var canDelete = comment.AuthorId == userId
                         || User.IsInRole("Admin")
                         || User.IsInRole("ProjectManager");

            if (!canDelete)
            {
                TempData["Error"] = "You can only delete your own comments.";
                return RedirectToAction(nameof(Detail), new { id = workItemId });
            }

            await _projects.DeleteCommentAsync(commentId);
            TempData["Success"] = "Comment deleted.";
            return RedirectToAction(nameof(Detail), new { id = workItemId });
        }

        // ── SetStatus (JSON endpoint for Kanban drag-and-drop) ────────────────────

        /// <summary>
        /// Lightweight POST used by the Kanban board's JavaScript.
        /// Returns { "success": true } on success so no full-page reload is needed.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, ItemStatus status)
        {
            _projects.SetCurrentUser(_userManager.GetUserId(User));
            var ok = await _projects.UpdateWorkItemStatusAsync(id, status);
            return Json(new { success = ok });
        }

        // ── Assign / reassign ─────────────────────────────────────────────────────

        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, string? assignedToId)
        {
            // Record who is making this reassignment in the audit log
            _projects.SetCurrentUser(_userManager.GetUserId(User));
            // Passing null for assignedToId clears the assignment (task becomes unassigned)
            var ok = await _projects.AssignWorkItemAsync(id, assignedToId);
            TempData[ok ? "Success" : "Error"] = ok ? "Assignee updated." : "Item not found.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ── CSV export ────────────────────────────────────────────────────────────

        /// <summary>
        /// Exports work items as a downloadable CSV file.
        /// If <paramref name="projectId"/> is provided, only that project's items are exported;
        /// otherwise all work items across every project are included.
        /// No third-party library is needed — the CSV is built with a plain StringBuilder.
        /// </summary>
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Export(int? projectId)
        {
            List<WorkItem> items = projectId.HasValue
                ? await _projects.GetWorkItemsByProjectAsync(projectId.Value)
                : await _projects.GetAllWorkItemsAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Title,Project,Type,Priority,Status,Assigned To,Due Date,Created,Updated");

            foreach (var w in items)
            {
                csv.AppendLine(string.Join(",",
                    w.WorkItemId,
                    CsvEscape(w.Title),
                    CsvEscape(w.Project?.Name ?? ""),
                    w.Type,
                    w.Priority,
                    w.Status,
                    CsvEscape(w.AssignedTo?.FullName ?? "Unassigned"),
                    w.DueDate?.ToString("yyyy-MM-dd") ?? "",
                    w.CreatedAt.ToString("yyyy-MM-dd"),
                    w.UpdatedAt.ToString("yyyy-MM-dd")
                ));
            }

            // Return the CSV bytes with the correct MIME type so the browser prompts a download
            var bytes    = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = projectId.HasValue ? $"project-{projectId}-tasks.csv" : "all-tasks.csv";
            return File(bytes, "text/csv", fileName);
        }

        // ── Private helper ────────────────────────────────────────────────────────

        /// <summary>
        /// Wraps a CSV cell in double-quotes and escapes embedded quotes if the value
        /// contains a comma, double-quote, or newline (per RFC 4180).
        /// </summary>
        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
