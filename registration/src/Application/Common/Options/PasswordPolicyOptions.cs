namespace Registration.Application.Common.Options;

/// <summary>
/// Fully configurable password policy (bound from the "PasswordPolicy" section). The account-creation
/// validator reads these values, so the policy can change without code edits. Defaults per requirement:
/// min 8 / max 20, with uppercase, digit, and special-character requirements enabled.
/// </summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int MinLength { get; init; } = 8;
    public int MaxLength { get; init; } = 20;

    public bool RequireUppercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireSpecialCharacter { get; init; } = true;

    /// <summary>The set of characters that count as "special" for the requirement above.</summary>
    public string AllowedSpecialCharacters { get; init; } = "@#$!%^&*()_-+=.,?";
}
