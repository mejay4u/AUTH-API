using MediatR;
using Registration.Domain.Common;

namespace Registration.Application.Registration.ResendOtp;

/// <summary>Re-issue the verification code for an existing registration session.</summary>
public sealed record ResendOtpCommand(Guid RegistrationId) : IRequest<Result>;
