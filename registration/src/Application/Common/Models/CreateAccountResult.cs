namespace Registration.Application.Common.Models;

/// <summary>The outcome of a successful account creation, returned to the client.</summary>
public sealed record CreateAccountResult(Guid UserId, string Email, string Username);
