using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;

namespace Registration.Infrastructure.Email;

/// <summary>
/// Sends the OTP email over SMTP. Configuration-driven (host/port/TLS/credentials/from). Used outside
/// Development; in Development a logging no-op sender is used so the flow runs without a mail server.
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendOtpAsync(string email, string code, DateTimeOffset expiresUtc, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseTls,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrEmpty(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = "Your verification code",
            Body =
                $"Your verification code is {code}.\n\n" +
                $"It expires at {expiresUtc.UtcDateTime:HH:mm} UTC. " +
                "If you did not request this, you can ignore this email.",
            IsBodyHtml = false
        };
        message.To.Add(email);

        await client.SendMailAsync(message, cancellationToken);
    }
}
