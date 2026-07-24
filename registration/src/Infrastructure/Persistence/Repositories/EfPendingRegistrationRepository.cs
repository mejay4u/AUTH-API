using Microsoft.EntityFrameworkCore;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPendingRegistrationRepository"/> against the registration
/// database. Callers treat expired sessions as not usable (they check <c>IsExpired</c>); a scheduled
/// job purges old rows in bulk (see the DDL script).
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

    public async Task MarkEmailVerifiedAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (session is null)
        {
            return;
        }

        session.EmailVerified = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (session is null)
        {
            return;
        }

        db.PendingRegistrations.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
    }
}
