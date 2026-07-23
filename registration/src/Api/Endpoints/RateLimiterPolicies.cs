namespace Registration.Api.Endpoints;

public static class RateLimiterPolicies
{
    /// <summary>Per-IP fixed-window limiter applied to all registration endpoints.</summary>
    public const string Registration = "registration";
}
