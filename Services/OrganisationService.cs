using TaskFlow.Data;
using TaskFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Services
{
    /// <summary>
    /// EF Core implementation of <see cref="IOrganisationService"/>.
    /// </summary>
    public class OrganisationService : IOrganisationService
    {
        private readonly AppDbContext _context;

        public OrganisationService(AppDbContext context) => _context = context;

        public async Task<Organisation> CreateOrganisationAsync(Organisation org)
        {
            // Generate a unique 6-character invite code before saving
            org.InviteCode = await GenerateUniqueCodeAsync();
            org.CreatedAt  = DateTime.UtcNow;

            _context.Organisations.Add(org);
            await _context.SaveChangesAsync();

            // Auto-enrol the owner as the first member
            if (org.OwnerId != null)
            {
                _context.OrganisationMembers.Add(new OrganisationMember
                {
                    OrganisationId = org.OrganisationId,
                    UserId         = org.OwnerId,
                    JoinedAt       = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return org;
        }

        public async Task<Organisation?> GetOrganisationByIdAsync(int id)
        {
            return await _context.Organisations
                .Include(o => o.Owner)
                .Include(o => o.Members)
                    .ThenInclude(m => m.User)
                .Include(o => o.Projects)
                    .ThenInclude(p => p.WorkItems)
                .FirstOrDefaultAsync(o => o.OrganisationId == id);
        }

        public async Task<List<Organisation>> GetOrganisationsForUserAsync(string userId)
        {
            // Return all orgs where this user is a member (includes orgs they own)
            return await _context.Organisations
                .Include(o => o.Owner)
                .Include(o => o.Members)
                .Include(o => o.Projects)
                .Where(o => o.Members.Any(m => m.UserId == userId))
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        public async Task<List<Organisation>> GetAllOrganisationsAsync()
        {
            return await _context.Organisations
                .Include(o => o.Owner)
                .Include(o => o.Members)
                .Include(o => o.Projects)
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        public async Task<bool> JoinByInviteCodeAsync(string inviteCode, string userId)
        {
            // Normalise to uppercase so codes are case-insensitive
            var org = await _context.Organisations
                .FirstOrDefaultAsync(o => o.InviteCode == inviteCode.Trim().ToUpper());

            if (org is null) return false;

            // Silently succeed if already a member — idempotent join
            var already = await _context.OrganisationMembers
                .AnyAsync(m => m.OrganisationId == org.OrganisationId && m.UserId == userId);

            if (!already)
            {
                _context.OrganisationMembers.Add(new OrganisationMember
                {
                    OrganisationId = org.OrganisationId,
                    UserId         = userId,
                    JoinedAt       = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> AddMemberAsync(int orgId, string userId)
        {
            var org = await _context.Organisations.FindAsync(orgId);
            if (org is null) return false;

            var already = await _context.OrganisationMembers
                .AnyAsync(m => m.OrganisationId == orgId && m.UserId == userId);

            if (already) return false;   // indicate "already a member" so caller can show a message

            _context.OrganisationMembers.Add(new OrganisationMember
            {
                OrganisationId = orgId,
                UserId         = userId,
                JoinedAt       = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(int orgId, string userId)
        {
            var member = await _context.OrganisationMembers
                .FirstOrDefaultAsync(m => m.OrganisationId == orgId && m.UserId == userId);

            if (member is null) return false;

            _context.OrganisationMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsMemberAsync(int orgId, string userId)
        {
            return await _context.OrganisationMembers
                .AnyAsync(m => m.OrganisationId == orgId && m.UserId == userId);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Generates a random 6-character alphanumeric code and retries until
        /// it doesn't clash with an existing one. Ambiguous characters (O, 0, 1, I)
        /// are excluded to avoid confusion when codes are shared verbally or by email.
        /// </summary>
        private async Task<string> GenerateUniqueCodeAsync()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string code;
            do
            {
                code = string.Concat(Enumerable.Range(0, 6)
                    .Select(_ => chars[Random.Shared.Next(chars.Length)]));
            }
            while (await _context.Organisations.AnyAsync(o => o.InviteCode == code));

            return code;
        }
    }
}
