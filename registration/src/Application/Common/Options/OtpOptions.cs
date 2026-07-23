namespace Registration.Application.Common.Options;

/// <summary>
/// Configurable email-OTP settings (bound from the "Otp" section). Everything is tunable per
/// environment: code length, lifetime, the 60s resend cooldown shown on the Verify Email screen,
/// the per-email request cap (default 10), and how many wrong guesses a code allows.
/// </summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>Number of digits in the code (the screen shows 6).</summary>
    public int CodeLength { get; init; } = 6;

    /// <summary>How long an issued code remains valid.</summary>
    public int ExpiryMinutes { get; init; } = 10;

    /// <summary>Minimum seconds between two code requests for the same email (matches "Resend Code (0:59)").</summary>
    public int ResendCooldownSeconds { get; init; } = 60;

    /// <summary>Maximum number of codes that may be requested for one email. Configurable; default 10.</summary>
    public int MaxRequestsPerEmail { get; init; } = 10;

    /// <summary>Maximum wrong-guess attempts allowed against a single issued code.</summary>
    public int MaxVerifyAttempts { get; init; } = 5;

    /// <summary>How long an email stays "verified" after a successful code entry (window to create the account).</summary>
    public int VerifiedWindowMinutes { get; init; } = 30;
}
