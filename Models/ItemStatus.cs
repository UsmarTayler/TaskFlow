namespace TaskFlow.Models
{
    /// <summary>
    /// Represents the stage a work item has reached in the development workflow.
    /// The UI renders each status as a colour-coded badge.
    /// </summary>
    public enum ItemStatus
    {
        Todo,        // Item has been created but no work has started yet
        InProgress,  // Actively being worked on by an assigned developer
        InReview,    // Development is complete and the item is awaiting code review or QA
        Done         // The item has been reviewed, accepted, and closed
    }
}
