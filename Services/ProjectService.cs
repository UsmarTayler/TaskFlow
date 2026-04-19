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

        // ── Audit ─────────────────────────────────────────────────────────────────

        // Passes the logged-in user's ID to the DbContext so SaveChanges can record who made each change
        public void SetCurrentUser(string? userId) => _context.CurrentUserId = userId;

        // ── Projects ──────────────────────────────────────────────────────────────

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            // Load work items (for progress %) and owner (for display name)
            return await _context.Projects
                .Include(p => p.WorkItems)
                .Include(p => p.Owner)
                .Include(p => p.Organisation)
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

        public async Task<List<WorkItemHistory>> GetWorkItemHistoryAsync(int workItemId)
        {
            // Load the ChangedBy user so we can display their name in the history table
            return await _context.WorkItemHistories
                .Include(h => h.ChangedBy)
                .Where(h => h.WorkItemId == workItemId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        // ── Edit / Delete ─────────────────────────────────────────────────────────

        public async Task<bool> UpdateProjectAsync(Project project)
        {
            // FindAsync uses the change tracker before hitting the DB — efficient for single-row updates
            var existing = await _context.Projects.FindAsync(project.ProjectId);
            if (existing is null) return false;

            // Only copy the fields the user is allowed to edit; ignore system fields like CreatedAt
            existing.Name        = project.Name;
            existing.Description = project.Description;
            existing.Status      = project.Status;
            existing.DueDate     = project.DueDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project is null) return false;

            // EF Core cascade delete removes all child WorkItems and their WorkItemHistory rows
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateWorkItemAsync(WorkItem item)
        {
            var existing = await _context.WorkItems.FindAsync(item.WorkItemId);
            if (existing is null) return false;

            // Overwrite all user-editable fields; audit log is captured by SaveChangesAsync override
            existing.Title        = item.Title;
            existing.Description  = item.Description;
            existing.Type         = item.Type;
            existing.Priority     = item.Priority;
            existing.Status       = item.Status;
            existing.DueDate      = item.DueDate;
            existing.AssignedToId = item.AssignedToId;
            existing.UpdatedAt    = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteWorkItemAsync(int id)
        {
            var item = await _context.WorkItems.FindAsync(id);
            if (item is null) return false;

            // Cascade delete removes the item's WorkItemHistory rows automatically
            _context.WorkItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Scoped queries (multi-tenancy) ────────────────────────────────────────

        public async Task<List<Project>> GetProjectsForUserAsync(string userId)
        {
            // Collect the IDs of every organisation this user belongs to
            var orgIds = await _context.OrganisationMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.OrganisationId)
                .ToListAsync();

            return await _context.Projects
                .Include(p => p.WorkItems)
                .Include(p => p.Owner)
                .Include(p => p.Organisation)
                .Where(p =>
                    // Personal project: no org, owned by this user
                    (p.OrganisationId == null && p.OwnerId == userId) ||
                    // Org project: belongs to an org the user is a member of
                    (p.OrganisationId.HasValue && orgIds.Contains(p.OrganisationId.Value))
                )
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // ── Comments ──────────────────────────────────────────────────────────────

        public async Task<List<WorkItemComment>> GetCommentsAsync(int workItemId)
        {
            // Oldest first — chronological order feels natural for a discussion thread
            return await _context.WorkItemComments
                .Include(c => c.Author)
                .Where(c => c.WorkItemId == workItemId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<WorkItemComment?> GetCommentByIdAsync(int id)
        {
            return await _context.WorkItemComments.FindAsync(id);
        }

        public async Task AddCommentAsync(WorkItemComment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            _context.WorkItemComments.Add(comment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(int id)
        {
            var comment = await _context.WorkItemComments.FindAsync(id);
            if (comment is null) return;

            _context.WorkItemComments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}
