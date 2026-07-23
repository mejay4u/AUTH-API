namespace Registration.Infrastructure.Email;

/// <summary>Configurable SMTP settings (bound from "Smtp") for sending the OTP email.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;

    /// <summary>Enable TLS (STARTTLS on 587 / implicit on 465).</summary>
    public bool UseTls { get; init; } = true;

    public string? Username { get; init; }
    public string? Password { get; init; }

    public string FromAddress { get; init; } = "no-reply@memberportal.local";
    public string FromName { get; init; } = "Member Portal";
}
