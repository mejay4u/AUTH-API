namespace Registration.Application.Common;

/// <summary>
/// Normalises an email address to a canonical form so OTP keys, duplicate checks, and the stored
/// username all agree regardless of the casing/whitespace the client sent.
/// </summary>
internal static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
