using System.ComponentModel.DataAnnotations;

namespace AuthApi.Application.Common.Security;

/// <summary>
/// Brute-force protection: lock an account for a cooldown period after too many failed attempts.
/// Bound from configuration section "AccountLockout".
/// </summary>
public sealed class AccountLockoutOptions
{
    public const string SectionName = "AccountLockout";

    [Range(1, 100)]
    public int MaxFailedAttempts { get; init; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; init; } = 15;

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(LockoutMinutes);
}
