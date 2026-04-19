using TaskFlow.Models;

namespace TaskFlow.Services
{
    /// <summary>
    /// Defines all data-access operations for projects and work items.
    /// Programming against this interface (rather than the concrete class) keeps
    /// controllers thin and makes unit testing straightforward — tests can supply
    /// a mock without touching the real database.
    /// </summary>
    public interface IProjectService
    {
        // ── Audit ─────────────────────────────────────────────────────────────────

        /// <summary>Sets the current user ID so the audit log can record who made each change.</summary>
        void SetCurrentUser(string? userId);

        // ── Projects ──────────────────────────────────────────────────────────────

        /// <summary>Returns all projects including their work items and owner, newest first.</summary>
        Task<List<Project>> GetAllProjectsAsync();

        /// <summary>Returns a single project by its primary key, or null if not found.</summary>
        Task<Project?> GetProjectByIdAsync(int id);

        /// <summary>Persists a new project and returns it with its generated ID.</summary>
        Task<Project> CreateProjectAsync(Project project);

        // ── Work Items ────────────────────────────────────────────────────────────

        /// <summary>Returns every work item across all projects, most recently updated first.</summary>
        Task<List<WorkItem>> GetAllWorkItemsAsync();

        /// <summary>Returns all work items belonging to the specified project.</summary>
        Task<List<WorkItem>> GetWorkItemsByProjectAsync(int projectId);

        /// <summary>Returns all work items assigned to a specific user — used for the "My Tasks" view.</summary>
        Task<List<WorkItem>> GetWorkItemsByAssigneeAsync(string userId);

        /// <summary>Returns a single work item by its primary key, or null if not found.</summary>
        Task<WorkItem?> GetWorkItemByIdAsync(int id);

        /// <summary>Persists a new work item and returns it with its generated ID.</summary>
        Task<WorkItem> CreateWorkItemAsync(WorkItem item);

        /// <summary>
        /// Changes the status of the work item with the given ID.
        /// Returns true on success, false if the item does not exist.
        /// </summary>
        Task<bool> UpdateWorkItemStatusAsync(int id, ItemStatus status);

        /// <summary>
        /// Reassigns a work item to a different user (or clears the assignment if userId is null).
        /// Returns true on success, false if the item does not exist.
        /// </summary>
        Task<bool> AssignWorkItemAsync(int id, string? userId);

        /// <summary>Returns the full change history for a work item, newest entries first.</summary>
        Task<List<WorkItemHistory>> GetWorkItemHistoryAsync(int workItemId);

        // ── Scoped queries (multi-tenancy) ────────────────────────────────────────

        /// <summary>
        /// Returns all projects visible to a specific user:
        /// personal projects they own (no org) plus all projects in organisations they belong to.
        /// Use <see cref="GetAllProjectsAsync"/> for admin views that need everything.
        /// </summary>
        Task<List<Project>> GetProjectsForUserAsync(string userId);

        // ── Comments ──────────────────────────────────────────────────────────────

        /// <summary>Returns all comments on a work item, oldest first.</summary>
        Task<List<WorkItemComment>> GetCommentsAsync(int workItemId);

        /// <summary>Returns a single comment by ID, or null if not found.</summary>
        Task<WorkItemComment?> GetCommentByIdAsync(int id);

        /// <summary>Persists a new comment.</summary>
        Task AddCommentAsync(WorkItemComment comment);

        /// <summary>Deletes a comment. Does not check ownership — callers must authorise first.</summary>
        Task DeleteCommentAsync(int id);

        // ── Edit / Delete ─────────────────────────────────────────────────────────

        /// <summary>
        /// Updates a project's editable fields (Name, Description, Status, DueDate).
        /// Returns true on success, false if the project does not exist.
        /// </summary>
        Task<bool> UpdateProjectAsync(Project project);

        /// <summary>
        /// Permanently deletes a project and all its work items (cascade).
        /// Returns true on success, false if the project does not exist.
        /// </summary>
        Task<bool> DeleteProjectAsync(int id);

        /// <summary>
        /// Updates a work item's editable fields (Title, Description, Type, Priority, Status, DueDate, AssignedToId).
        /// Returns true on success, false if the item does not exist.
        /// </summary>
        Task<bool> UpdateWorkItemAsync(WorkItem item);

        /// <summary>
        /// Permanently deletes a work item and its change history (cascade).
        /// Returns true on success, false if the item does not exist.
        /// </summary>
        Task<bool> DeleteWorkItemAsync(int id);
    }
}
