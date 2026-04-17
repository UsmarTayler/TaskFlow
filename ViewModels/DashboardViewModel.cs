namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Aggregated data passed to the dashboard view.
    /// The controller computes all values from the in-memory project/work-item lists
    /// so the view stays free of business logic.
    /// </summary>
    public class DashboardViewModel
    {
        // ── Summary cards ─────────────────────────────────────────────────────────

        public int TotalProjects    { get; set; }  // total number of projects
        public int ActiveProjects   { get; set; }  // projects currently in Active status
        public int TotalItems       { get; set; }  // work items across all projects
        public int OpenItems        { get; set; }  // items that are not yet Done
        public int ItemsDueThisWeek { get; set; }  // open items with a due date in the next 7 days
        public int ItemsDoneToday   { get; set; }  // items completed today — shows recent velocity

        // ── Chart data (serialised to JSON in the Razor view via JsonSerializer) ──

        /// <summary>Work item counts per workflow status — drives the doughnut chart.</summary>
        public Dictionary<string, int> ByStatus   { get; set; } = new();

        /// <summary>Work item counts per priority level — drives the vertical bar chart.</summary>
        public Dictionary<string, int> ByPriority { get; set; } = new();

        /// <summary>Task count per project name — drives the horizontal bar chart.</summary>
        public Dictionary<string, int> ByProject  { get; set; } = new();

        // ── Recent activity table ─────────────────────────────────────────────────

        /// <summary>The 8 most recently updated work items shown in the activity feed.</summary>
        public List<RecentItemRow> RecentItems { get; set; } = new();
    }

    /// <summary>
    /// A slim projection of a WorkItem used in the dashboard's recent-activity table.
    /// Using a projection avoids loading unnecessary navigation properties just for display.
    /// </summary>
    public class RecentItemRow
    {
        public int      WorkItemId  { get; set; }
        public string   Title       { get; set; } = string.Empty;
        public string   ProjectName { get; set; } = string.Empty;
        public string   Status      { get; set; } = string.Empty;
        public string   Priority    { get; set; } = string.Empty;
        public DateTime UpdatedAt   { get; set; }
    }
}
