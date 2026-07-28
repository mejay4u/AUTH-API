using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.InitiateRegistration;

/// <summary>
/// Phase 2 of the flow: Descope has verified the email and now hands us the details it collected in
/// phase 1. Creates the member record in Pending state and returns its id, which Descope keeps for the
/// password step.
/// </summary>
public sealed record InitiateRegistrationCommand(
    string Email,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode) : IRequest<Result<InitiateRegistrationResult>>;
