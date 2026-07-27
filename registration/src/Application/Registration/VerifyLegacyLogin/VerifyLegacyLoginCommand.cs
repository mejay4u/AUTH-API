using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.VerifyLegacyLogin;

/// <summary>
/// JIT migration hook (called by Descope on a legacy user's first login): verify the legacy credentials
/// and, on success, create/return the profile so Descope takes over the password going forward.
/// </summary>
public sealed record VerifyLegacyLoginCommand(string Email, string Password, string? SourceIp)
    : IRequest<Result<MemberProfile>>;
