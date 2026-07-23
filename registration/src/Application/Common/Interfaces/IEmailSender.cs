namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Sends transactional email. The SMTP implementation lives in Infrastructure; a logging/no-op
/// implementation is used in Development so the flow runs without a real mail server.
/// </summary>
public interface IEmailSender
{
    /// <summary>Email the one-time passcode to the address that requested it.</summary>
    Task SendOtpAsync(string email, string code, DateTimeOffset expiresUtc, CancellationToken cancellationToken);
}
