using MediatR;
using Registration.Domain.Common;

namespace Registration.Application.Registration.VerifyEmailOtp;

/// <summary>Verify the code the user entered, for a given registration session.</summary>
public sealed record VerifyEmailOtpCommand(Guid RegistrationId, string Code) : IRequest<Result>;
