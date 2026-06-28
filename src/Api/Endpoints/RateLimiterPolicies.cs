namespace AuthApi.Api.Endpoints;

public static class RateLimiterPolicies
{
    /// <summary>Per-IP limiter applied to the (anonymous) authentication endpoints to blunt brute force.</summary>
    public const string Auth = "auth";
}
