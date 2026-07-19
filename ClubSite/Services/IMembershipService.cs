using ClubSite.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClubSite.Services;

public interface IMembershipService
{
    Task<bool> RegisterMemberAsync(User user, string password, string roleName,
        CancellationToken cancellationToken = default);

    Task<bool> RenewMembershipAsync(string userId, string newRoleName,
        CancellationToken cancellationToken = default);

    Task<bool> CancelMembershipAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<List<ClubMembership>> GetMembershipHistoryAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserWithMembershipAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateMemberProfileAsync(MemberProfile profile,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateMembershipAsync(string userId,
        CancellationToken cancellationToken = default);

    Task<string> GenerateMembershipNumberAsync(
        CancellationToken cancellationToken = default);
}