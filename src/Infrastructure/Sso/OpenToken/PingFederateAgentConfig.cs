using System.Text;

namespace AuthApi.Infrastructure.Sso.OpenToken;

/// <summary>
/// A parsed PingFederate OpenToken agent configuration file — the <c>agent-config.txt</c> downloaded
/// per SSO connection from the PingFederate admin console (e.g. <c>hra-agent-config-qa.txt</c>).
/// The SSO configuration row's <c>AgentFileLocationPath</c> names the file; this type surfaces the
/// three properties token generation needs and keeps the rest available in <see cref="Properties"/>.
/// </summary>
public sealed class PingFederateAgentConfig
{
    private PingFederateAgentConfig(
        string tokenName,
        OpenTokenCipherSuite cipherSuite,
        byte[] sharedSecret,
        IReadOnlyDictionary<string, string> properties)
    {
        TokenName = tokenName;
        CipherSuite = cipherSuite;
        SharedSecret = sharedSecret;
        Properties = properties;
        TokenLifetime = SecondsProperty("token-lifetime", 300);
        RenewUntil = SecondsProperty("renew-until", 43200);
        NotBeforeTolerance = SecondsProperty("not-before-tolerance", 0);
    }

    /// <summary>Query-string parameter the token is delivered in (e.g. <c>JivaZeomegaOpenToken</c>).</summary>
    public string TokenName { get; }

    public OpenTokenCipherSuite CipherSuite { get; }

    /// <summary>The connection's shared secret (the file's base64 <c>password</c> property, decoded).</summary>
    public byte[] SharedSecret { get; }

    public IReadOnlyDictionary<string, string> Properties { get; }

    /// <summary>Validity window of an issued token (<c>token-lifetime</c>, seconds).</summary>
    public TimeSpan TokenLifetime { get; }

    /// <summary>How long an issued token may be renewed (<c>renew-until</c>, seconds).</summary>
    public TimeSpan RenewUntil { get; }

    /// <summary>Allowed clock skew when validating an inbound token (<c>not-before-tolerance</c>, seconds).</summary>
    public TimeSpan NotBeforeTolerance { get; }

    private TimeSpan SecondsProperty(string name, int defaultSeconds) =>
        TimeSpan.FromSeconds(
            Properties.TryGetValue(name, out var text) && int.TryParse(text, out var seconds)
                ? seconds
                : defaultSeconds);

    public static PingFederateAgentConfig Load(string path)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            properties[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        if (!properties.TryGetValue("password", out var password) || password.Length == 0)
        {
            throw new InvalidOperationException(
                $"Agent configuration '{Path.GetFileName(path)}' has no 'password' property; the file may be truncated or not an agent-config file.");
        }

        var cipherSuite = properties.TryGetValue("cipher-suite", out var suiteText) && int.TryParse(suiteText, out var suite)
            ? (OpenTokenCipherSuite)suite
            : OpenTokenCipherSuite.Aes128Cbc;

        return new PingFederateAgentConfig(
            properties.GetValueOrDefault("token-name", "opentoken"),
            cipherSuite,
            DecodeSharedSecret(password),
            properties);
    }

    private static byte[] DecodeSharedSecret(string password)
    {
        // The admin console writes the password base64-encoded; tolerate a plain-text value so
        // hand-written files (dev) also work.
        try
        {
            return Convert.FromBase64String(password);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(password);
        }
    }
}
