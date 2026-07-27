using System.Net;
using System.Net.Http.Json;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;

namespace Registration.Infrastructure.Legacy;

/// <summary>
/// Calls the legacy verify API, which verifies the salted SHA-256/512 password inside the legacy system
/// and returns the member profile. The legacy hashing scheme never leaves that system. Base address and
/// credentials are configured on the typed <see cref="HttpClient"/> in DI.
/// </summary>
public sealed class HttpLegacyMemberVerifier(HttpClient httpClient) : ILegacyMemberVerifier
{
    public async Task<LegacyVerificationResult> VerifyAsync(string email, string password, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "verify", new LegacyVerifyRequest(email, password), cancellationToken);

        // The legacy API signals "no match" with 401/404 — treat as a failed verification, not an error.
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            return LegacyVerificationResult.Failed;
        }

        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<LegacyVerifyResponse>(cancellationToken);
        if (dto is null || !dto.Verified)
        {
            return LegacyVerificationResult.Failed;
        }

        return LegacyVerificationResult.Success(new LegacyMemberProfile(
            string.IsNullOrWhiteSpace(dto.Email) ? email : dto.Email,
            dto.FirstName ?? string.Empty,
            dto.LastName ?? string.Empty,
            dto.LegacyMemberId ?? string.Empty,
            dto.DateOfBirth,
            dto.ZipCode,
            dto.ContactNumber));
    }

    private sealed record LegacyVerifyRequest(string Email, string Password);

    private sealed record LegacyVerifyResponse(
        bool Verified,
        string? Email,
        string? FirstName,
        string? LastName,
        string? LegacyMemberId,
        DateOnly? DateOfBirth,
        string? ZipCode,
        string? ContactNumber);
}
