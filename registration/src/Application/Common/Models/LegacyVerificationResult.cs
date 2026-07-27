namespace Registration.Application.Common.Models;

/// <summary>Outcome of verifying a legacy member's credentials against the legacy system.</summary>
public sealed record LegacyVerificationResult(bool Verified, LegacyMemberProfile? Profile)
{
    public static LegacyVerificationResult Success(LegacyMemberProfile profile) => new(true, profile);

    public static readonly LegacyVerificationResult Failed = new(false, null);
}
