using TaskFlow.Models;
using TaskFlow.Services;
using TaskFlow.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Controllers
{
    /// <summary>
    /// Serves the main dashboard page — the default landing page after login.
    /// Builds summary statistics and chart data from all projects and work items,
    /// then passes them to the view as a typed DashboardViewModel.
    /// </summary>
    [Authorize]  // All dashboard routes require the user to be logged in
    public class DashboardController : Controller
    {
        private readonly IProjectService _projects;
        private readonly UserManager<ApplicationUser> _userManager;

        // Both dependencies are resolved by ASP.NET Core's built-in DI container
        public DashboardController(IProjectService projects, UserManager<ApplicationUser> userManager)
        {
            _projects    = projects;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Developers don't need the org-wide dashboard — redirect them to their own task list
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Developer"))
                return RedirectToAction("MyTasks", "WorkItems");

            // Fetch all data up front — the service handles includes so navigation properties
            // are populated and we can do LINQ on them in memory without extra DB round-trips
            var allProjects = await _projects.GetAllProjectsAsync();
            var allItems    = await _projects.GetAllWorkItemsAsync();

            var today   = DateTime.UtcNow.Date;
            var weekEnd = today.AddDays(7);

            var vm = new DashboardViewModel
            {
                // ── Summary card counts ──────────────────────────────────────────
                TotalProjects    = allProjects.Count,
                ActiveProjects   = allProjects.Count(p => p.Status == ProjectStatus.Active),
                TotalItems       = allItems.Count,
                OpenItems        = allItems.Count(w => w.Status != ItemStatus.Done),

                // Items due within the next 7 days that are still open
                ItemsDueThisWeek = allItems.Count(w =>
                    w.DueDate.HasValue &&
                    w.DueDate.Value.Date >= today &&
                    w.DueDate.Value.Date <= weekEnd &&
                    w.Status != ItemStatus.Done),

                // Items completed today — shows recent team velocity
                ItemsDoneToday = allItems.Count(w =>
                    w.UpdatedAt.Date == today && w.Status == ItemStatus.Done),

                // ── Doughnut chart: tasks by workflow status ─────────────────────
                // Keys match the labels rendered by Chart.js in the view
                ByStatus = new Dictionary<string, int>
                {
                    ["To Do"]       = allItems.Count(w => w.Status == ItemStatus.Todo),
                    ["In Progress"] = allItems.Count(w => w.Status == ItemStatus.InProgress),
                    ["In Review"]   = allItems.Count(w => w.Status == ItemStatus.InReview),
                    ["Done"]        = allItems.Count(w => w.Status == ItemStatus.Done),
                },

                // ── Bar chart: tasks by priority ─────────────────────────────────
                ByPriority = new Dictionary<string, int>
                {
                    ["Low"]      = allItems.Count(w => w.Priority == Priority.Low),
                    ["Medium"]   = allItems.Count(w => w.Priority == Priority.Medium),
                    ["High"]     = allItems.Count(w => w.Priority == Priority.High),
                    ["Critical"] = allItems.Count(w => w.Priority == Priority.Critical),
                },

                // ── Horizontal bar chart: task count per project ──────────────────
                // Exclude projects with no tasks to keep the chart readable
                ByProject = allProjects
                    .Where(p => p.WorkItems.Any())
                    .ToDictionary(p => p.Name, p => p.WorkItems.Count),

                // ── Recent activity table: last 8 updated work items ─────────────
                RecentItems = allItems
                    .OrderByDescending(w => w.UpdatedAt)
                    .Take(8)
                    .Select(w => new RecentItemRow
                    {
                        WorkItemId  = w.WorkItemId,
                        Title       = w.Title,
                        ProjectName = w.Project?.Name ?? "",
                        Status      = w.Status.ToString(),
                        Priority    = w.Priority.ToString(),
                        UpdatedAt   = w.UpdatedAt
                    })
                    .ToList()
            };

            return View(vm);
        }
    }
}
