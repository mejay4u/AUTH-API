using Registration.Domain.Registration;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Persistence for the pre-account registration session (<see cref="PendingRegistration"/>) in the
/// registration database. The EF Core implementation lives in Infrastructure.
/// </summary>
public interface IPendingRegistrationRepository
{
    /// <summary>Create a new session and return its id (the registrationId handed to the client).</summary>
    Task<Guid> CreateAsync(PendingRegistration pending, CancellationToken cancellationToken);

    /// <summary>Load a session by id, or <c>null</c> if it does not exist.</summary>
    Task<PendingRegistration?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Mark the session's email as verified (after a successful OTP).</summary>
    Task MarkEmailVerifiedAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Delete the session (after the account is created, or to clean up).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
