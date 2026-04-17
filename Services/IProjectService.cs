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
    }
}
