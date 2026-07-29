using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.InitiateRegistration;

/// <summary>
/// Step 3 of the wizard: the member has verified their email and confirmed the details on the review
/// screen. Creates the pending member record and returns its id, which the app keeps for the
/// account-creation call.
/// </summary>
public sealed record InitiateRegistrationCommand(
    string Email,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string? ContactNumber,
    // The `sub` of the Descope token that authorised this call; null when running anonymously.
    string? DescopeUserId = null) : IRequest<Result<InitiateRegistrationResult>>;
