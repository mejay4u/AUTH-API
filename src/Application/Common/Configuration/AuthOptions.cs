namespace AuthApi.Application.Common.Configuration;

/// <summary>How this deployment of the API satisfies an authentication request.</summary>
public enum AuthMode
{
    /// <summary>
    /// The API authenticates on its own: it reads the member DB through <c>IAccountRepository</c>,
    /// verifies the password and mints its own RS256 tokens. This is the original behaviour and
    /// requires line-of-sight to the on-prem member databases.
    /// </summary>
    Local = 0,

    /// <summary>
    /// The API is a pass-through (BFA). It owns no credentials, no signing key and no database
    /// connection; it relays the call to the on-prem ARTS auth service through Apigee Internal and
    /// returns whatever ARTS answers. Used where the ARO cluster cannot reach the on-prem DB.
    /// </summary>
    PassThrough = 1
}

/// <summary>Top-level switches for the authentication surface, bound from the <c>Auth</c> section.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Local issuance vs. pass-through relay. See <see cref="AuthMode"/>.</summary>
    public AuthMode Mode { get; set; } = AuthMode.Local;

    /// <summary>
    /// Largest request body the pass-through will buffer and forward. Auth payloads are a few hundred
    /// bytes; the cap stops a caller from making the BFA hold large buffers on an anonymous endpoint.
    /// </summary>
    public int MaxRequestBodyBytes { get; set; } = 32 * 1024;
}
