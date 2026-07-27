namespace Registration.Application.Common.Models;

/// <summary>Profile returned to Descope from the JIT verify hook (and usable by the sync flow).</summary>
public sealed record MemberProfile(Guid UserId, string Email, string FirstName, string LastName, string Origin);
