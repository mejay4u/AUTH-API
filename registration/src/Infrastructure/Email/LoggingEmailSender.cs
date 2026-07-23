using Microsoft.Extensions.Logging;
using Registration.Application.Common.Interfaces;

namespace Registration.Infrastructure.Email;

/// <summary>
/// Development-only <see cref="IEmailSender"/> that logs the code instead of sending mail, so the OTP
/// flow can be exercised end-to-end without an SMTP server. NEVER registered outside Development —
/// logging codes in production would defeat the purpose of the OTP.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendOtpAsync(string email, string code, DateTimeOffset expiresUtc, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[DEV] OTP for {Email}: {Code} (expires {ExpiresUtc:u}). No email sent (LoggingEmailSender).",
            email, code, expiresUtc);
        return Task.CompletedTask;
    }
}
