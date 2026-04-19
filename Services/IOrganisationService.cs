using TaskFlow.Models;

namespace TaskFlow.Services
{
    /// <summary>
    /// Defines all data-access operations for organisations and their memberships.
    /// Separating this from IProjectService keeps each interface focused and
    /// makes the codebase easier to test and extend.
    /// </summary>
    public interface IOrganisationService
    {
        /// <summary>Creates a new organisation, generates a unique invite code, and adds the owner as first member.</summary>
        Task<Organisation> CreateOrganisationAsync(Organisation org);

        /// <summary>Returns a single organisation by ID with members, owner, and projects loaded. Null if not found.</summary>
        Task<Organisation?> GetOrganisationByIdAsync(int id);

        /// <summary>Returns all organisations the specified user belongs to.</summary>
        Task<List<Organisation>> GetOrganisationsForUserAsync(string userId);

        /// <summary>Returns all organisations (admin use).</summary>
        Task<List<Organisation>> GetAllOrganisationsAsync();

        /// <summary>
        /// Adds the user to the organisation identified by the invite code.
        /// Returns false if the code is not found; returns true (without duplicating) if already a member.
        /// </summary>
        Task<bool> JoinByInviteCodeAsync(string inviteCode, string userId);

        /// <summary>Adds a user to an organisation by ID (admin / owner action). Returns false if org not found or user already a member.</summary>
        Task<bool> AddMemberAsync(int orgId, string userId);

        /// <summary>Removes a member from an organisation. Returns false if membership not found.</summary>
        Task<bool> RemoveMemberAsync(int orgId, string userId);

        /// <summary>Returns true if the user is a member of the specified organisation.</summary>
        Task<bool> IsMemberAsync(int orgId, string userId);
    }
}
