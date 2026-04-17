using TaskFlow.Data;
using TaskFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Services
{
    /// <summary>
    /// EF Core implementation of <see cref="IProjectService"/>.
    /// All queries are async to avoid blocking the thread pool while waiting on I/O.
    /// Include() calls eagerly load related entities so the controller doesn't need
    /// to trigger lazy-loading or make extra round-trips.
    /// </summary>
    public class ProjectService : IProjectService
    {
        // Injected by DI — scoped lifetime means one context per HTTP request
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context) => _context = context;

        // ── Projects ──────────────────────────────────────────────────────────────

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            // Load work items (for progress %) and owner (for display name)
            return await _context.Projects
                .Include(p => p.WorkItems)
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)  // newest projects appear first
                .ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            // Also load who each task is assigned to so the detail table can show names
            return await _context.Projects
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.AssignedTo)
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.ProjectId == id);
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            // Override CreatedAt here so callers don't have to remember to set it
            project.CreatedAt = DateTime.UtcNow;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;  // EF Core populates the auto-generated ProjectId after SaveChanges
        }

        // ── Work Items ────────────────────────────────────────────────────────────

        public async Task<List<WorkItem>> GetAllWorkItemsAsync()
        {
            // Load the parent project and both user FKs so the dashboard/CSV export can access them
            return await _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .OrderByDescending(w => w.UpdatedAt)  // most recently touched items appear first
                .ToListAsync();
        }

        public async Task<List<WorkItem>> GetWorkItemsByProjectAsync(int projectId)
        {
            return await _context.WorkItems
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .Where(w => w.ProjectId == projectId)
                .OrderByDescending(w => w.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<WorkItem>> GetWorkItemsByAssigneeAsync(string userId)
        {
            // Used for the "My Tasks" view — filters by the logged-in user's ID
            return await _context.WorkItems
                .Include(w => w.Project)
                .Where(w => w.AssignedToId == userId)
                .OrderByDescending(w => w.UpdatedAt)
                .ToListAsync();
        }

        public async Task<WorkItem?> GetWorkItemByIdAsync(int id)
        {
            return await _context.WorkItems
                .Include(w => w.Project)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .FirstOrDefaultAsync(w => w.WorkItemId == id);
        }

        public async Task<WorkItem> CreateWorkItemAsync(WorkItem item)
        {
            // Force both timestamps to the current time regardless of what the caller passed in
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            _context.WorkItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> UpdateWorkItemStatusAsync(int id, ItemStatus status)
        {
            // FindAsync uses the primary key and checks the change tracker before hitting the DB
            var item = await _context.WorkItems.FindAsync(id);
            if (item is null) return false;

            item.Status    = status;
            item.UpdatedAt = DateTime.UtcNow;  // keep the "last updated" timestamp current
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignWorkItemAsync(int id, string? userId)
        {
            var item = await _context.WorkItems.FindAsync(id);
            if (item is null) return false;

            // userId can be null to clear the assignment (unassign the task)
            item.AssignedToId = userId;
            item.UpdatedAt    = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
