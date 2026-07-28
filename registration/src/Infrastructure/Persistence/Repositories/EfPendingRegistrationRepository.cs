using Microsoft.EntityFrameworkCore;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPendingRegistrationRepository"/> against the registration
/// database. Callers treat expired records as unusable (they check <c>IsExpired</c>); a scheduled job
/// purges old rows in bulk (see the DDL script).
/// </summary>
public sealed class EfPendingRegistrationRepository(RegistrationDbContext db) : IPendingRegistrationRepository
{
    public async Task<Guid> CreateAsync(PendingRegistration pending, CancellationToken cancellationToken)
    {
        db.PendingRegistrations.Add(pending);
        await db.SaveChangesAsync(cancellationToken);
        return pending.Id;
    }

    public Task<PendingRegistration?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.PendingRegistrations.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<PendingRegistration?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.PendingRegistrations.AsNoTracking().FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

    public async Task SetPasswordAsync(
        Guid id,
        string passwordHash,
        string passwordSalt,
        CancellationToken cancellationToken)
    {
        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pending is null)
        {
            return;
        }

        pending.PasswordHash = passwordHash;
        pending.PasswordSalt = passwordSalt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pending is null)
        {
            return;
        }

        db.PendingRegistrations.Remove(pending);
        await db.SaveChangesAsync(cancellationToken);
    }
}
