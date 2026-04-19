using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    /// <summary>
    /// Represents a comment left by a team member on a work item.
    /// Comments are ordered chronologically and displayed below the change history
    /// on the work item detail page. Authors can delete their own comments;
    /// Admins and ProjectManagers can delete any comment.
    /// </summary>
    public class WorkItemComment
    {
        public int WorkItemCommentId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Body { get; set; } = string.Empty;

        // Set to UtcNow on creation — never updated (comments are not editable)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // FK → parent work item; cascade delete removes comments when the item is deleted
        public int WorkItemId { get; set; }
        public WorkItem? WorkItem { get; set; }

        // FK → comment author; SetNull so the comment is preserved if the user account is deleted
        public string? AuthorId { get; set; }
        public ApplicationUser? Author { get; set; }
    }
}
