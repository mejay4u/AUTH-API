namespace Registration.Application.Common.Options;

/// <summary>
/// Configurable registration-session settings (bound from "Registration"). Controls how long a
/// pre-account <c>PendingRegistration</c> session stays valid before the user must start again.
/// </summary>
public sealed class RegistrationOptions
{
    public const string SectionName = "Registration";

    /// <summary>How long a registration session (personal info + verification) remains usable.</summary>
    public int SessionLifetimeMinutes { get; init; } = 60;
}
