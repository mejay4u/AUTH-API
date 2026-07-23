using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Issues and verifies email one-time passcodes and tracks whether an email has been verified.
/// State (hashed code, expiry, attempt/resend counters, verified flag) lives in a cache — never a
/// database table — per the "no new tables" constraint.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate a new OTP for the email, enforcing the resend cooldown and the per-email request cap.
    /// Returns the challenge (code + expiry) to be emailed, or a failure describing the limit hit.
    /// </summary>
    Task<Result<OtpChallenge>> IssueAsync(string email, CancellationToken cancellationToken);

    /// <summary>Verify a submitted code (constant-time, attempt-limited); marks the email verified on success.</summary>
    Task<Result> VerifyAsync(string email, string code, CancellationToken cancellationToken);

    /// <summary>Whether the email currently has a valid "verified" flag (set by a successful verify).</summary>
    Task<bool> IsVerifiedAsync(string email, CancellationToken cancellationToken);

    /// <summary>Clear all OTP/verification state for the email once the account has been created.</summary>
    Task ConsumeVerifiedAsync(string email, CancellationToken cancellationToken);
}
