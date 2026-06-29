using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAccountRepository"/> (database-first, existing user table).
///
/// Today it reads via LINQ against the mapped tables (which run on the seeded in-memory mock), so the
/// API works end-to-end. When you point at the real database, replace the LINQ in
/// <see cref="QueryAsync"/> with a call to your existing stored procedure / view — the method signature
/// and the <see cref="MemberPortalLoginData"/> return type stay the same, so nothing else changes.
/// </summary>
public sealed class AccountRepository(AuthDbContext db) : IAccountRepository
{
    public Task<MemberPortalLoginData?> GetMemberPortalLoginDataAsync(string username, CancellationToken cancellationToken)
        => QueryAsync(m => m.Username == username, cancellationToken);

    public Task<MemberPortalLoginData?> GetMemberPortalLoginDataByIdAsync(Guid memberId, CancellationToken cancellationToken)
        => QueryAsync(m => m.Id == memberId, cancellationToken);

    private async Task<MemberPortalLoginData?> QueryAsync(
        System.Linq.Expressions.Expression<Func<Member, bool>> predicate,
        CancellationToken cancellationToken)
    {
        // ───────────────────────────────────────────────────────────────────────────────────────────
        // DATABASE-FIRST SWAP-IN POINT
        // Replace the LINQ below with your existing stored procedure / query, e.g.:
        //
        //   var rows = await db.Database
        //       .SqlQueryRaw<MemberPortalLoginRow>(
        //           "EXEC dbo.GetMemberPortalLoginData @Username = {0}", username)
        //       .ToListAsync(cancellationToken);
        //   // then group the flattened rows into one MemberPortalLoginData (member + LOBs + plans)
        //
        // Keep returning MemberPortalLoginData so the handlers remain unchanged.
        // ───────────────────────────────────────────────────────────────────────────────────────────
        var member = await db.Members
            .AsNoTracking()
            .Include(m => m.MemberLobs).ThenInclude(ml => ml.Lob)
            .Include(m => m.MemberPlans)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (member is null)
        {
            return null;
        }

        return new MemberPortalLoginData(
            member.Id,
            member.Username,
            member.PasswordHash,
            member.PasswordSalt,
            member.IsActive,
            member.Email,
            member.FirstName,
            member.LastName,
            member.LobCodes(),
            member.PlanIds());
    }
}
