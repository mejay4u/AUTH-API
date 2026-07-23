using MediatR;
using Registration.Domain.Common;

namespace Registration.Application.Registration.SendEmailOtp;

/// <summary>
/// Issue (or re-issue) a 6-digit email verification code and send it via email. The same command
/// backs both the "send" and "resend" endpoints — the resend cooldown and per-email request cap are
/// enforced server-side by <c>IOtpService</c>.
/// </summary>
public sealed record SendEmailOtpCommand(string Email) : IRequest<Result>;
