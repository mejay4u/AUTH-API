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
    /// Load by email address — how the initiate step spots an in-progress registration for someone
    /// who abandoned the wizard and came back.
    /// </summary>
    Task<PendingRegistration?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Delete the record (after it is promoted to a user, or to clean up).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
