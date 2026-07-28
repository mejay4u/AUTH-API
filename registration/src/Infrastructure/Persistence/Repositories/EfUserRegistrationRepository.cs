using Microsoft.EntityFrameworkCore;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Users;

namespace Registration.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRegistrationRepository"/> against the registration
/// service's own database (<see cref="RegistrationDbContext"/>). The use cases depend only on the
/// interface, so this could be swapped for another provider without touching them.
/// </summary>
public sealed class EfUserRegistrationRepository(RegistrationDbContext db, TimeProvider timeProvider)
    : IUserRegistrationRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<Guid> CreateUserAsync(NewUserRegistration registration, CancellationToken cancellationToken)
    {
        var user = new User
        {
            // Reuses the pending record's id so the identifier Descope holds stays valid.
            Id = registration.Id,
            Email = registration.Email,
            Username = registration.Username,
            PasswordHash = registration.PasswordHash,
            PasswordSalt = registration.PasswordSalt,
            FirstName = registration.FirstName,
            LastName = registration.LastName,
            DateOfBirth = registration.DateOfBirth,
            ZipCode = registration.ZipCode,
            SubscriberId = registration.SubscriberId,
            PlanId = registration.PlanId,
            SsnLast4 = registration.SsnLast4,
            IsActive = true,
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
