using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction the Application layer depends on. The concrete EF Core
/// <c>AuthDbContext</c> lives in Infrastructure; here we only expose what use cases need. This keeps
/// handlers testable and the Application layer free of any specific database provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Member> Members { get; }
    DbSet<Lob> Lobs { get; }
    DbSet<Plan> Plans { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
