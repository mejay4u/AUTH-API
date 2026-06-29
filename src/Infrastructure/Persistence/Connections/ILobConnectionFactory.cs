using System.Data.Common;

namespace AuthApi.Infrastructure.Persistence.Connections;

/// <summary>
/// Creates a database connection for a specific line of business. Each call returns a fresh, unopened
/// <see cref="DbConnection"/>; the caller (or Dapper) opens it, and ADO.NET connection pooling makes
/// this cheap. This is how we "create a connection dynamically" across the 4 LOB databases.
/// </summary>
public interface ILobConnectionFactory
{
    DbConnection Create(string lob);
}
