using AuthApi.Application.Common.Models;

namespace AuthApi.Application.Common.Interfaces;

/// <summary>
/// Data-access boundary for member login data — the clean-architecture equivalent of the legacy
/// <c>AccountRepository</c>. The interface lives in Application; the EF Core (database-first)
/// implementation lives in Infrastructure. The login/refresh handlers depend only on this contract,
/// so the underlying query (table read today, stored procedure later) can change without touching them.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Returns the member's login data for the given username from the LOB's database, or <c>null</c> if
    /// no such member exists. Equivalent to the legacy <c>GetMemberPortalLoginData</c> stored proc.
    /// The <paramref name="lob"/> selects which of the per-LOB databases to query.
    /// </summary>
    Task<MemberPortalLoginData?> GetMemberPortalLoginDataAsync(string username, string lob, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the member's current login data by id from the LOB's database — used by the refresh flow
    /// to re-read up-to-date Plan/LOB data when issuing a new access token.
    /// </summary>
    Task<MemberPortalLoginData?> GetMemberPortalLoginDataByIdAsync(Guid memberId, string lob, CancellationToken cancellationToken);
}
