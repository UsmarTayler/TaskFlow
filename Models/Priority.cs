namespace TaskFlow.Models
{
    /// <summary>
    /// Indicates how urgently a work item needs to be addressed.
    /// The integer value of each member is used for sorting (higher = more urgent).
    /// </summary>
    public enum Priority
    {
        Low,      // Nice-to-have; can be deferred without impacting the sprint
        Medium,   // Should be completed this sprint but not blocking anything
        High,     // Important work that must be completed before the milestone
        Critical  // Blocking issue that stops progress — needs immediate attention
    }
}
