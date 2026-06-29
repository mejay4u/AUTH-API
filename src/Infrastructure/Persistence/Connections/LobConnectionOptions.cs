namespace AuthApi.Infrastructure.Persistence.Connections;

/// <summary>
/// Per-LOB connection strings. The member login data lives in a different database per line of business,
/// so we resolve the connection dynamically by the LOB supplied at login.
/// Bound from configuration section "LobConnections".
///
/// Example appsettings:
///   "LobConnections": {
///     "Connections": {
///       "DENTAL":  "Server=...;Database=DentalDb;...",
///       "VISION":  "Server=...;Database=VisionDb;...",
///       "MEDICAL": "Server=...;Database=MedicalDb;...",
///       "RX":      "Server=...;Database=RxDb;..."
///     }
///   }
/// In production, supply these from a secret store / Key Vault rather than appsettings.
/// </summary>
public sealed class LobConnectionOptions
{
    public const string SectionName = "LobConnections";

    /// <summary>Map of LOB code → connection string.</summary>
    public Dictionary<string, string> Connections { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}
