namespace TaskFlow.Models
{
    /// <summary>
    /// Records a single change made to a work item — who changed what, from what value, to what value.
    /// Entries are written automatically by AppDbContext.SaveChanges() so nothing is ever missed.
    /// </summary>
    public class WorkItemHistory
    {
        // Primary key
        public int WorkItemHistoryId { get; set; }

        // The work item this history entry belongs to
        public int WorkItemId { get; set; }
        public WorkItem? WorkItem { get; set; }

        // The name of the property that changed (e.g. "Status", "Priority", "AssignedToId")
        public string Field { get; set; } = string.Empty;

        // The value before the change (stored as a string for simplicity)
        public string? OldValue { get; set; }

        // The value after the change
        public string? NewValue { get; set; }

        // Who made the change — null if the system made it (e.g. seeder)
        public string? ChangedById { get; set; }
        public ApplicationUser? ChangedBy { get; set; }

        // When the change was saved to the database
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
