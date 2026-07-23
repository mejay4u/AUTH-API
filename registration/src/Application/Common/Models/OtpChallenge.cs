namespace Registration.Application.Common.Models;

/// <summary>
/// A freshly issued one-time passcode and when it expires. The raw <paramref name="Code"/> is returned
/// only so the use case can email it — it is never persisted in clear text (only a hash is stored).
/// </summary>
public sealed record OtpChallenge(string Code, DateTimeOffset ExpiresUtc);
