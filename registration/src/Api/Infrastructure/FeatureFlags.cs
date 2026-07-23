using Microsoft.FeatureManagement;

namespace Registration.Api.Infrastructure;

public static class FeatureFlags
{
    /// <summary>Master flag that turns the whole registration API on/off (config: FeatureManagement:Registration).</summary>
    public const string Registration = "Registration";
}

/// <summary>
/// Endpoint filter that returns 404 when the named feature flag is disabled, so the entire registration
/// surface can be toggled per environment without redeploying.
/// </summary>
public sealed class FeatureGateEndpointFilter(string featureName) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManager>();

        if (!await featureManager.IsEnabledAsync(featureName))
        {
            return Results.Problem(
                title: "Feature unavailable",
                detail: "Registration is not currently available.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return await next(context);
    }
}
