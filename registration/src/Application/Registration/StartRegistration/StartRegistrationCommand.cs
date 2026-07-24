using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.StartRegistration;

/// <summary>
/// Step 1 of the wizard: submit the personal information and open a registration session. Validates the
/// details, creates a <c>PendingRegistration</c>, and emails a verification code (unless the email is
/// already registered). Returns the session id used by the remaining pre-account steps.
/// </summary>
public sealed record StartRegistrationCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string Email,
    string? ContactNumber) : IRequest<Result<StartRegistrationResult>>;
