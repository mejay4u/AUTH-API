using Registration.Domain.Migration;

namespace Registration.Application.Common.Interfaces;

/// <summary>Append-only writer for JIT migration audit records.</summary>
public interface IMigrationAuditRepository
{
    Task AddAsync(MigrationAudit audit, CancellationToken cancellationToken);
}
