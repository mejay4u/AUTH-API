using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the member database. It implements <see cref="IApplicationDbContext"/> so the
/// Application layer never references it directly. The same context works against the InMemory
/// provider (mock) and SQL Server (the real existing database) — only the provider registration in
/// <c>DependencyInjection</c> changes.
/// </summary>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Lob> Lobs => Set<Lob>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply every IEntityTypeConfiguration in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
