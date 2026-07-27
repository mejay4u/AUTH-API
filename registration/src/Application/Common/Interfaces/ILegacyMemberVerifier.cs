using Registration.Application.Common.Models;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Verifies a legacy member's email + password against the legacy system during JIT migration. The
/// legacy system performs the salted SHA-256/512 verification and returns the member's profile — the
/// hashing detail stays inside the legacy system. Implementations: an HTTP client to the legacy verify
/// API (default) or a direct member-portal DB reader (future, if DB access is granted).
/// </summary>
public interface ILegacyMemberVerifier
{
    Task<LegacyVerificationResult> VerifyAsync(string email, string password, CancellationToken cancellationToken);
}
