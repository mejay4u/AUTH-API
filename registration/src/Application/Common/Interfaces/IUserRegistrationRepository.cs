using Registration.Application.Common.Models;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Write boundary for creating portal users in the registration database. The EF implementation lives
/// in Infrastructure.
/// </summary>
public interface IUserRegistrationRepository
{
    /// <summary>True if a user with this email/username already exists (duplicate-email check).</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Insert the new user. The id is supplied by the caller — it is the id of the pending record being
    /// promoted, so the identifier Descope was handed at the start stays valid afterwards.
    /// </summary>
    Task<Guid> CreateUserAsync(NewUserRegistration registration, CancellationToken cancellationToken);
}
