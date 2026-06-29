/* ============================================================================
   Auth API — refresh-token storage (Option 1: dedicated, Auth-owned table)

   This table is owned by the Auth API and lives in its own 'auth' schema,
   intentionally DECOUPLED from the legacy member/user table (no foreign key).
   Only a SHA-256 hash of each refresh token is stored — never the raw token.

   Run this once against the database the Auth API points at
   (Database:Provider = SqlServer, ConnectionStrings:AuthDb).

   NOTE on MemberId type: this script uses UNIQUEIDENTIFIER to match the current
   model. If your existing member key is an INT/BIGINT, change MemberId here AND
   the Guid types in RefreshToken / MemberPortalLoginData accordingly.
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'auth')
    EXEC (N'CREATE SCHEMA auth');
GO

IF OBJECT_ID(N'auth.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE auth.RefreshTokens
    (
        Id                  UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        MemberId            UNIQUEIDENTIFIER NOT NULL,   -- plain value, NOT a FK to the user table
        TokenHash           NVARCHAR(256)    NOT NULL,   -- SHA-256 (base64url) of the raw token
        ExpiresUtc          DATETIME2(7)     NOT NULL,
        CreatedUtc          DATETIME2(7)     NOT NULL,
        CreatedByIp         NVARCHAR(64)     NULL,
        RevokedUtc          DATETIME2(7)     NULL,
        RevokedByIp         NVARCHAR(64)     NULL,
        ReplacedByTokenHash NVARCHAR(256)    NULL        -- links a rotated token to its successor
    );

    -- Fast, unique lookup by token hash (refresh validation).
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash
        ON auth.RefreshTokens (TokenHash);

    -- Member-scoped queries (revoke-all on reuse detection).
    CREATE INDEX IX_RefreshTokens_MemberId
        ON auth.RefreshTokens (MemberId);
END
GO
