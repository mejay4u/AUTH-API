using Microsoft.EntityFrameworkCore;
using Registration.Domain.Registration;
using Registration.Domain.Users;

namespace Registration.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the registration service's OWN database. Database-first: the schema is authored
/// in SQL (see <c>scripts/create-registration-user-table.sql</c>) and this context is mapped to it via
/// <see cref="IEntityTypeConfiguration{TEntity}"/> — the app does not own migrations. The same context
/// runs against SQL Server (real DB) and the InMemory provider (Development); only the provider
/// registration in <c>DependencyInjection</c> changes.
/// </summary>
public sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistrationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
