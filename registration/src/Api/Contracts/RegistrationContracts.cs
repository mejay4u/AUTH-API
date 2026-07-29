using Registration.Application.Common.Models;

namespace Registration.Api.Contracts;

/// <summary>
/// Step 3: the reviewed personal information. Sent once the member has verified their email and
/// confirmed the details. <c>DateOfBirth</c> is an ISO date (yyyy-MM-dd); <c>ContactNumber</c> is
/// optional.
/// </summary>
public sealed record InitiateRegistrationRequest(
    string Email,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string? ContactNumber);

/// <summary>
/// The identifier the app holds for the rest of the wizard. Named <c>UserId</c> because that is what
/// the design calls it, even though the underlying record is still pending.
/// </summary>
public sealed record InitiateRegistrationResponse(Guid UserId, string Email, string Status)
{
    public static InitiateRegistrationResponse From(InitiateRegistrationResult r) =>
        new(r.UserId, r.Email, r.Status);
}

/// <summary>Step 4: the password, with its confirmation. Creates the account.</summary>
public sealed record CreateAccountRequest(Guid UserId, string Password, string ConfirmPassword);

/// <summary>Response returned when an account is created.</summary>
public sealed record CreateAccountResponse(Guid UserId, string Email, string Username)
{
    public static CreateAccountResponse From(CreateAccountResult r) => new(r.UserId, r.Email, r.Username);
}
