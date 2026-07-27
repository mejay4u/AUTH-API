using Registration.Application.Common.Interfaces;
using Registration.Domain.Migration;

namespace Registration.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IMigrationAuditRepository"/> (append-only).</summary>
public sealed class EfMigrationAuditRepository(RegistrationDbContext db) : IMigrationAuditRepository
{
    public async Task AddAsync(MigrationAudit audit, CancellationToken cancellationToken)
    {
        db.MigrationAudits.Add(audit);
        await db.SaveChangesAsync(cancellationToken);
    }
}
