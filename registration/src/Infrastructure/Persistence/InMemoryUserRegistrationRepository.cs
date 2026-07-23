using System.Collections.Concurrent;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;

namespace Registration.Infrastructure.Persistence;

/// <summary>
/// Development/mock <see cref="IUserRegistrationRepository"/> backed by an in-memory dictionary, so the
/// registration flow runs end-to-end without the real database. Registered as a singleton to retain
/// created users for the process lifetime. Not for production use.
/// </summary>
public sealed class InMemoryUserRegistrationRepository : IUserRegistrationRepository
{
    private readonly ConcurrentDictionary<string, Guid> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_usersByEmail.ContainsKey(email));

    public Task<Guid> CreateUserAsync(NewUserRegistration registration, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        if (!_usersByEmail.TryAdd(registration.Email, id))
        {
            // Lost a race with a concurrent create — surface the existing id.
            return Task.FromResult(_usersByEmail[registration.Email]);
        }

        return Task.FromResult(id);
    }
}
