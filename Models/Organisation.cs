using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models
{
    /// <summary>
    /// An Organisation groups users and projects together — the core of the multi-tenancy model.
    /// Members of an organisation can see all projects assigned to that organisation.
    /// Users without an organisation only see projects they personally own.
    /// </summary>
    public class Organisation
    {
        public int OrganisationId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Organisation Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>
        /// A 6-character alphanumeric code (e.g. "ACM3X9") that any user can enter
        /// on the Join page to become a member of this organisation.
        /// Generated automatically — no ambiguous characters (O, 0, 1, I).
        /// </summary>
        public string InviteCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // FK → the user who created the organisation (owner privileges)
        // SetNull so the org survives if the owner account is deleted
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        // Navigation properties
        public List<OrganisationMember> Members  { get; set; } = new();
        public List<Project>            Projects { get; set; } = new();
    }
}
