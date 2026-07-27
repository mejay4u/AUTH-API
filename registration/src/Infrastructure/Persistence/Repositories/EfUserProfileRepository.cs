using Microsoft.EntityFrameworkCore;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Users;

namespace Registration.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserProfileRepository"/>. <see cref="GetByEmailAsync"/> returns a
/// tracked entity, so a caller can mutate it and call <see cref="UpsertAsync"/> to persist the update;
/// a brand-new (detached) entity is inserted. This keeps the upsert idempotent for webhook replays.
/// </summary>
public sealed class EfUserProfileRepository(RegistrationDbContext db) : IUserProfileRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task UpsertAsync(User user, CancellationToken cancellationToken)
    {
        if (db.Entry(user).State == EntityState.Detached)
        {
            db.Users.Add(user);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
