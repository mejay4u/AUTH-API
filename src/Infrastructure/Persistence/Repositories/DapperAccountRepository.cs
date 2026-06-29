using System.Data;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Infrastructure.Persistence.Connections;
using Dapper;

namespace AuthApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Production implementation of <see cref="IAccountRepository"/> using <b>Dapper</b> to call the existing
/// stored procedures against the correct per-LOB database (resolved by <see cref="ILobConnectionFactory"/>).
///
/// Parameters are passed as command parameters (never string-concatenated), so this is not vulnerable to
/// SQL injection. Connections are created per call and disposed immediately; ADO.NET pooling keeps that cheap.
/// </summary>
public sealed class DapperAccountRepository(ILobConnectionFactory connectionFactory) : IAccountRepository
{
    // TODO: confirm the actual stored procedure names with the DBA team.
    private const string ProcByUsername = "dbo.GetMemberPortalLoginData";
    private const string ProcById = "dbo.GetMemberPortalLoginDataById";

    public Task<MemberPortalLoginData?> GetMemberPortalLoginDataAsync(string username, string lob, CancellationToken cancellationToken)
        => QueryAsync(lob, ProcByUsername, new { Username = username }, cancellationToken);

    public Task<MemberPortalLoginData?> GetMemberPortalLoginDataByIdAsync(Guid memberId, string lob, CancellationToken cancellationToken)
        => QueryAsync(lob, ProcById, new { MemberId = memberId }, cancellationToken);

    private async Task<MemberPortalLoginData?> QueryAsync(
        string lob, string procName, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create(lob);

        var command = new CommandDefinition(
            procName,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        // Proc returns a single member row (confirmed). Dapper opens/closes the connection automatically.
        var row = await connection.QuerySingleOrDefaultAsync<MemberLoginRow>(command);

        return row is null ? null : Map(row, lob);
    }

    private static MemberPortalLoginData Map(MemberLoginRow row, string lob)
    {
        // If the proc returns the LOB/plan list as a column, use it; otherwise fall back to the LOB the
        // member logged in with. Adjust to match the real proc's columns.
        var lobs = string.IsNullOrWhiteSpace(row.Lobs) ? new[] { lob } : SplitCsv(row.Lobs);
        var planIds = SplitCsvInts(row.PlanIds);

        return new MemberPortalLoginData(
            row.MemberId,
            row.Username,
            row.PasswordHash,
            row.PasswordSalt,
            row.IsActive,
            row.Email,
            row.FirstName,
            row.LastName,
            lobs,
            planIds);
    }

    private static string[] SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int[] SplitCsvInts(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : SplitCsv(value)
                .Select(v => int.TryParse(v, out var i) ? i : (int?)null)
                .Where(i => i is not null)
                .Select(i => i!.Value)
                .ToArray();

    /// <summary>
    /// Shape returned by the stored proc (single row). Column names map by Dapper convention — rename the
    /// properties (or alias columns in the proc) to match your real schema.
    /// <c>Lobs</c>/<c>PlanIds</c> are optional comma-separated columns; if absent we use the login LOB.
    /// </summary>
    private sealed class MemberLoginRow
    {
        public Guid MemberId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string PasswordSalt { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Lobs { get; init; }
        public string? PlanIds { get; init; }
    }
}
