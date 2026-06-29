using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Persistence.Connections;

/// <summary>
/// Resolves the connection string for the given LOB from <see cref="LobConnectionOptions"/> and returns
/// a new <see cref="SqlConnection"/>. Stateless and thread-safe → registered as a singleton.
/// </summary>
public sealed class LobConnectionFactory(IOptions<LobConnectionOptions> options) : ILobConnectionFactory
{
    private readonly LobConnectionOptions _options = options.Value;

    public DbConnection Create(string lob)
    {
        if (string.IsNullOrWhiteSpace(lob))
        {
            throw new ArgumentException("A LOB is required to resolve the database connection.", nameof(lob));
        }

        if (!_options.Connections.TryGetValue(lob, out var connectionString) ||
            string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string is configured for LOB '{lob}'. " +
                $"Add it under '{LobConnectionOptions.SectionName}:Connections:{lob}'.");
        }

        return new SqlConnection(connectionString);
    }
}
