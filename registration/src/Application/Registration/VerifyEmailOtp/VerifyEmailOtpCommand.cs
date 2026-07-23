using MediatR;
using Registration.Domain.Common;

namespace Registration.Application.Registration.VerifyEmailOtp;

/// <summary>Verify the code the user entered on the "Check your Email" screen.</summary>
public sealed record VerifyEmailOtpCommand(string Email, string Code) : IRequest<Result>;
