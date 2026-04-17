namespace TaskFlow.Models
{
    /// <summary>
    /// Represents the lifecycle state of a project.
    /// Used on the project list to filter and to colour-code status badges.
    /// </summary>
    public enum ProjectStatus
    {
        Active,    // The project is currently being worked on
        OnHold,    // Work has been paused (waiting on a decision, budget, etc.)
        Completed, // All work items are done and the project has been delivered
        Archived   // The project is no longer active and is kept for historical reference
    }
}
