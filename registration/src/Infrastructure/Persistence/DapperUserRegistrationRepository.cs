using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;

namespace Registration.Infrastructure.Persistence;

/// <summary>
/// Production <see cref="IUserRegistrationRepository"/> that writes into the EXISTING user table via
/// Dapper (no new tables are created). Parameters are always passed as command parameters (never string
/// concatenation), so this is not vulnerable to SQL injection.
///
/// NOTE: confirm the actual stored-procedure / table and column names with the DBA before going live —
/// the proc names below are the integration points, mirroring the existing login repository's approach.
/// </summary>
public sealed class DapperUserRegistrationRepository(IConfiguration configuration) : IUserRegistrationRepository
{
    // TODO: confirm these with the DBA team (they must target the same user table the existing login reads).
    private const string ProcEmailExists = "dbo.MemberPortalUserExists";
    private const string ProcCreateUser = "dbo.CreateMemberPortalUser";

    private string ConnectionString =>
        configuration.GetConnectionString("MemberPortalDb")
        ?? throw new InvalidOperationException("ConnectionStrings:MemberPortalDb is required when UserStore:Provider=SqlServer.");

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(ConnectionString);

        var command = new CommandDefinition(
            ProcEmailExists,
            new { Email = email },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var count = await connection.ExecuteScalarAsync<int>(command);
        return count > 0;
    }

    public async Task<Guid> CreateUserAsync(NewUserRegistration registration, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(ConnectionString);

        var command = new CommandDefinition(
            ProcCreateUser,
            new
            {
                registration.Email,
                registration.Username,
                registration.PasswordHash,
                registration.PasswordSalt
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        // The proc returns the new user's id.
        return await connection.ExecuteScalarAsync<Guid>(command);
    }
}
