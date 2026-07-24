using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Issues and verifies email one-time passcodes. State (hashed code, expiry, attempt/resend counters)
/// lives in a cache keyed by email — the resend cooldown and per-email request cap survive across
/// registration sessions for the same address. Whether an email is "verified" is tracked on the
/// registration session (<c>PendingRegistration.EmailVerified</c>), not here.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate a new OTP for the email, enforcing the resend cooldown and the per-email request cap.
    /// Returns the challenge (code + expiry) to be emailed, or a failure describing the limit hit.
    /// </summary>
    Task<Result<OtpChallenge>> IssueAsync(string email, CancellationToken cancellationToken);

    /// <summary>Verify a submitted code (constant-time, attempt-limited) against the email's current code.</summary>
    Task<Result> VerifyAsync(string email, string code, CancellationToken cancellationToken);
}
