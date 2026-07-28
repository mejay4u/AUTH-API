using Registration.Domain.Registration;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Persistence for the in-progress member record (<see cref="PendingRegistration"/>) in the
/// registration database. The EF Core implementation lives in Infrastructure.
/// </summary>
public interface IPendingRegistrationRepository
{
    /// <summary>Create the Pending record and return its id (the userId handed back to Descope).</summary>
    Task<Guid> CreateAsync(PendingRegistration pending, CancellationToken cancellationToken);

    /// <summary>Load by id, or <c>null</c> if it does not exist.</summary>
    Task<PendingRegistration?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Load by email address — how <c>completeRegistration</c> finds the record, since that step is
    /// keyed on email rather than on the id.
    /// </summary>
    Task<PendingRegistration?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Store the password hash and salt against the record (the password step).</summary>
    Task SetPasswordAsync(Guid id, string passwordHash, string passwordSalt, CancellationToken cancellationToken);

    /// <summary>Delete the record (after it is promoted to a user, or to clean up).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
