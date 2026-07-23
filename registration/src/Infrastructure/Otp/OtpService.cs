using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Common.Options;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Otp;

/// <summary>
/// Cache-backed email OTP service. Stores only a SHA-256 hash of each code (never the raw code), with
/// expiry, a wrong-guess attempt limit, a resend cooldown, and a per-email request cap — all in
/// <see cref="IMemoryCache"/> so no database table is added. Swap in <c>IDistributedCache</c> for a
/// multi-instance deployment without changing callers.
/// </summary>
public sealed class OtpService(
    IMemoryCache cache,
    IOptions<OtpOptions> options,
    TimeProvider timeProvider) : IOtpService
{
    private readonly OtpOptions _options = options.Value;

    public Task<Result<OtpChallenge>> IssueAsync(string email, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var key = OtpKey(email);
        var existing = cache.Get<OtpEntry>(key);

        if (existing is not null)
        {
            if (now - existing.LastIssuedUtc < TimeSpan.FromSeconds(_options.ResendCooldownSeconds))
            {
                return Task.FromResult(Result.Failure<OtpChallenge>(RegistrationErrors.OtpResendTooSoon));
            }

            if (existing.RequestCount >= _options.MaxRequestsPerEmail)
            {
                return Task.FromResult(Result.Failure<OtpChallenge>(RegistrationErrors.OtpRequestLimitReached));
            }
        }

        var code = GenerateNumericCode(_options.CodeLength);
        var expiresUtc = now.AddMinutes(_options.ExpiryMinutes);

        // The tracking entry outlives the code so the request cap and cooldown survive code expiry.
        var trackingExpiryUtc = now.AddMinutes(Math.Max(_options.ExpiryMinutes, _options.VerifiedWindowMinutes));

        var entry = new OtpEntry
        {
            CodeHash = HashCode(code),
            ExpiresUtc = expiresUtc,
            AttemptsRemaining = _options.MaxVerifyAttempts,
            RequestCount = (existing?.RequestCount ?? 0) + 1,
            LastIssuedUtc = now,
            TrackingExpiryUtc = trackingExpiryUtc
        };

        Save(key, entry);

        return Task.FromResult(Result.Success(new OtpChallenge(code, expiresUtc)));
    }

    public Task<Result> VerifyAsync(string email, string code, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var key = OtpKey(email);
        var entry = cache.Get<OtpEntry>(key);

        if (entry is null || now > entry.ExpiresUtc)
        {
            return Task.FromResult(Result.Failure(RegistrationErrors.OtpExpiredOrNotFound));
        }

        if (entry.AttemptsRemaining <= 0)
        {
            return Task.FromResult(Result.Failure(RegistrationErrors.OtpTooManyAttempts));
        }

        var matches = CryptographicOperations.FixedTimeEquals(HashCode(code), entry.CodeHash);
        if (!matches)
        {
            entry.AttemptsRemaining--;
            Save(key, entry);

            return Task.FromResult(Result.Failure(
                entry.AttemptsRemaining <= 0 ? RegistrationErrors.OtpTooManyAttempts : RegistrationErrors.OtpInvalid));
        }

        // Success: mark the email verified for a limited window and consume the code.
        cache.Set(
            VerifiedKey(email),
            true,
            new MemoryCacheEntryOptions { AbsoluteExpiration = now.AddMinutes(_options.VerifiedWindowMinutes) });
        cache.Remove(key);

        return Task.FromResult(Result.Success());
    }

    public Task<bool> IsVerifiedAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(cache.TryGetValue(VerifiedKey(email), out bool verified) && verified);

    public Task ConsumeVerifiedAsync(string email, CancellationToken cancellationToken)
    {
        cache.Remove(VerifiedKey(email));
        cache.Remove(OtpKey(email));
        return Task.CompletedTask;
    }

    private void Save(string key, OtpEntry entry) =>
        cache.Set(key, entry, new MemoryCacheEntryOptions { AbsoluteExpiration = entry.TrackingExpiryUtc });

    private static string OtpKey(string email) => $"registration:otp:{email}";
    private static string VerifiedKey(string email) => $"registration:otp-verified:{email}";

    private static byte[] HashCode(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));

    private static string GenerateNumericCode(int length)
    {
        var digits = new char[length];
        for (var i = 0; i < length; i++)
        {
            digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(digits);
    }

    /// <summary>Mutable cache entry tracking the current code and per-email counters.</summary>
    private sealed class OtpEntry
    {
        public required byte[] CodeHash { get; set; }
        public required DateTimeOffset ExpiresUtc { get; set; }
        public required int AttemptsRemaining { get; set; }
        public required int RequestCount { get; set; }
        public required DateTimeOffset LastIssuedUtc { get; set; }
        public required DateTimeOffset TrackingExpiryUtc { get; set; }
    }
}
