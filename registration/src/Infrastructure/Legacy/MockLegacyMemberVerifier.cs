using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;

namespace Registration.Infrastructure.Legacy;

/// <summary>
/// Development-only <see cref="ILegacyMemberVerifier"/> that accepts one well-known legacy credential so
/// the JIT migration flow can be exercised without the real legacy API. Never used outside Development.
/// </summary>
public sealed class MockLegacyMemberVerifier : ILegacyMemberVerifier
{
    public Task<LegacyVerificationResult> VerifyAsync(string email, string password, CancellationToken cancellationToken)
    {
        if (string.Equals(email, "legacy.user@example.com", StringComparison.OrdinalIgnoreCase)
            && password == "Legacy#123")
        {
            return Task.FromResult(LegacyVerificationResult.Success(new LegacyMemberProfile(
                email, "Legacy", "User", "LEG-1001", new DateOnly(1985, 3, 10), "12345", null)));
        }

        return Task.FromResult(LegacyVerificationResult.Failed);
    }
}
