using Microsoft.EntityFrameworkCore;
using Registration.Domain.Migration;
using Registration.Domain.Users;

namespace Registration.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the registration service's own database (database-first: schema authored in SQL,
/// EF maps to it, no app migrations). Holds the profile system of record and the migration audit log —
/// no password material.
/// </summary>
public sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<MigrationAudit> MigrationAudits => Set<MigrationAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistrationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
