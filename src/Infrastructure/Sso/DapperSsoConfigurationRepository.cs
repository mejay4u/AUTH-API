using System.Data;
using AuthApi.Application.Sso;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AuthApi.Infrastructure.Sso;

/// <summary>
/// Production implementation of <see cref="ISsoConfigurationRepository"/> — the rewrite of the legacy
/// <c>MemberRepository.FetchSSOData</c>. Calls the existing stored procedure with Dapper instead of
/// EF <c>FromSqlRaw</c> string concatenation. Always performs the full load for the LOB (SSOName is
/// sent as NULL, as the legacy full-load path did); name filtering happens in memory in the handler.
/// Caching is layered on by <see cref="CachedSsoConfigurationRepository"/>, not mixed in here.
/// </summary>
public sealed class DapperSsoConfigurationRepository(IConfiguration configuration) : ISsoConfigurationRepository
{
    // TODO: confirm the schema-qualified name with the DBA team (legacy UtilConstant.MEM_SP_SSO_Config).
    private const string Proc = "dbo.MEM_SP_SSO_Config";

    public async Task<IReadOnlyList<SsoConfigurationEntry>> GetForLobAsync(
        string lob, string? planCode, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("MemberDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:MemberDb is required for the SSO configuration lookup.");

        await using var connection = new SqlConnection(connectionString);

        var command = new CommandDefinition(
            Proc,
            new
            {
                LobId = lob,
                SSOName = (string?)null,
                PlanCode = string.IsNullOrWhiteSpace(planCode) ? null : planCode
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<SsoConfigRow>(command);

        return rows.Select(Map).ToArray();
    }

    private static SsoConfigurationEntry Map(SsoConfigRow row) => new(
        row.SSOConfigID,
        row.SSOName ?? string.Empty,
        row.SSODescription,
        row.SSOPingFedURL,
        row.SSOPingFedReturnURL,
        row.SSOAgentFileLocationPath,
        row.AssessmentName,
        row.Level,
        row.Active,
        row.EffectiveDate,
        row.TermDate,
        row.ArgusCustomerID);

    /// <summary>
    /// Shape returned by the proc — property names match the legacy <c>SSODetails</c> columns so
    /// Dapper maps by convention. Alias columns in the proc if the real names differ.
    /// </summary>
    private sealed class SsoConfigRow
    {
        public int SSOConfigID { get; init; }
        public string? SSOName { get; init; }
        public string? SSODescription { get; init; }
        public string? SSOPingFedURL { get; init; }
        public string? SSOPingFedReturnURL { get; init; }
        public string? SSOAgentFileLocationPath { get; init; }
        public string? AssessmentName { get; init; }
        public string? Level { get; init; }
        public bool Active { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public DateTime? TermDate { get; init; }
        public string? ArgusCustomerID { get; init; }
    }
}
