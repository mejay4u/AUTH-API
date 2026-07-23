using Registration.Application.Common.Models;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Write boundary for creating portal users in the EXISTING user table (no new tables are added).
/// The EF/Dapper implementation lives in Infrastructure; a mock is used in Development.
/// </summary>
public interface IUserRegistrationRepository
{
    /// <summary>True if a user with this email/username already exists (duplicate-email check).</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    /// <summary>Insert the new user into the existing user table and return its generated id.</summary>
    Task<Guid> CreateUserAsync(NewUserRegistration registration, CancellationToken cancellationToken);
}
