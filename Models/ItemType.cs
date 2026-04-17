namespace TaskFlow.Models
{
    /// <summary>
    /// Classifies what kind of work a work item represents.
    /// Used in the UI to display a type badge and to help teams filter their backlog.
    /// </summary>
    public enum ItemType
    {
        Feature,     // A new capability being added to the product
        Bug,         // A defect or unintended behaviour that needs fixing
        Task,        // A general piece of work that doesn't fit Feature or Bug
        Improvement  // An enhancement to existing functionality (performance, UX, etc.)
    }
}
