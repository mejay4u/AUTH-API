using Registration.Domain.Users;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Read/write access to the portal user profile store (our system of record). No password material is
/// involved — Descope owns credentials. The EF Core implementation lives in Infrastructure.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>Load a user by (normalized) email, or <c>null</c> if none exists.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Insert or update the user (idempotent — matches an existing row by email).</summary>
    Task UpsertAsync(User user, CancellationToken cancellationToken);
}
