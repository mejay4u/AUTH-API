namespace Registration.Application.Common.Models;

/// <summary>The result of opening a registration session — the id the client uses for the next steps.</summary>
public sealed record StartRegistrationResult(Guid RegistrationId);
