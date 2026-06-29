using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Development/mock implementation of <see cref="IAccountRepository"/> backed by the seeded in-memory
/// database. Used when <c>Database:Provider = InMemory</c> so the API runs end-to-end without the four
/// real LOB databases. The <c>lob</c> argument is accepted for parity with the real repository; the mock
/// ignores it for the query (single seeded store).
/// </summary>
public sealed class MockAccountRepository(AuthDbContext db) : IAccountRepository
{
    public Task<MemberPortalLoginData?> GetMemberPortalLoginDataAsync(string username, string lob, CancellationToken cancellationToken)
        => QueryAsync(m => m.Username == username, cancellationToken);

    public Task<MemberPortalLoginData?> GetMemberPortalLoginDataByIdAsync(Guid memberId, string lob, CancellationToken cancellationToken)
        => QueryAsync(m => m.Id == memberId, cancellationToken);

    private async Task<MemberPortalLoginData?> QueryAsync(
        System.Linq.Expressions.Expression<Func<Member, bool>> predicate,
        CancellationToken cancellationToken)
    {
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
