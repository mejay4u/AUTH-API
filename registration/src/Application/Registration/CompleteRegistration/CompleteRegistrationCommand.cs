using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.CompleteRegistration;

/// <summary>
/// Phase 4 of the flow: confirm the member against Facets and finish registration. Keyed on email
/// (the flow has it throughout) rather than the id from phase 2.
/// </summary>
public sealed record CompleteRegistrationCommand(
    string Email,
    string Ssn,
    string? MemberId) : IRequest<Result<CompleteRegistrationResult>>;
