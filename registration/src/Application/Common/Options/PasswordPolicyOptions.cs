namespace Registration.Application.Common.Options;

/// <summary>
/// Fully configurable password policy (bound from the "PasswordPolicy" section). The account-creation
/// validator reads these values, so the policy can change without code edits. Defaults match the
/// Create Account screen's checklist: 14–56 characters with upper, lower, digit and special required.
/// Change these and the app's checklist together, or members will be told one thing and refused for
/// another.
/// </summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int MinLength { get; init; } = 14;
    public int MaxLength { get; init; } = 56;

    public bool RequireUppercase { get; init; } = true;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
    public bool RequireSpecialCharacter { get; init; } = true;

    /// <summary>The set of characters that count as "special" for the requirement above.</summary>
    public string AllowedSpecialCharacters { get; init; } = "@#$!%^&*()_-+=.,?";
}
