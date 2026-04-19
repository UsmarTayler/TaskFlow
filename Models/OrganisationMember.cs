namespace TaskFlow.Models
{
    /// <summary>
    /// Join table between Organisation and ApplicationUser.
    /// A user can be a member of multiple organisations; an organisation has many members.
    /// A unique index on (OrganisationId, UserId) prevents duplicate memberships.
    /// </summary>
    public class OrganisationMember
    {
        public int OrganisationMemberId { get; set; }

        // FK → Organisation; cascade delete removes memberships when the org is deleted
        public int OrganisationId { get; set; }
        public Organisation? Organisation { get; set; }

        // FK → ApplicationUser; cascade delete removes memberships when the user is deleted
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
