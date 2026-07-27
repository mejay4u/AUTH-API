-- Registration service — database-first schema for its OWN database (profile system of record).
-- Descope owns authentication/passwords; this database stores NO password material.
-- EF Core maps to these tables (it does not create or migrate them). Run once before pointing the API
-- at the DB with Database:Provider = SqlServer.

IF SCHEMA_ID('registration') IS NULL
    EXEC('CREATE SCHEMA registration');
GO

-- Portal user profile + Descope identity mapping + migration provenance.
IF OBJECT_ID('registration.Users', 'U') IS NULL
BEGIN
    CREATE TABLE registration.Users
    (
        Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_registration_Users PRIMARY KEY,
        Email          NVARCHAR(256)    NOT NULL,
        Username       NVARCHAR(256)    NOT NULL,
        FirstName      NVARCHAR(100)    NOT NULL,
        LastName       NVARCHAR(100)    NOT NULL,
        DateOfBirth    DATE             NULL,
        ZipCode        NVARCHAR(10)     NULL,
        ContactNumber  NVARCHAR(20)     NULL,
        DescopeUserId  NVARCHAR(128)    NULL,
        Origin         NVARCHAR(20)     NOT NULL,     -- 'Registration' | 'Migrated'
        LegacyMemberId NVARCHAR(128)    NULL,
        IsActive       BIT              NOT NULL CONSTRAINT DF_registration_Users_IsActive DEFAULT (1),
        CreatedUtc     DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_Users_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedUtc     DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_Users_UpdatedUtc DEFAULT (SYSUTCDATETIME()),
        MigratedUtc    DATETIME2(3)     NULL
    );

    CREATE UNIQUE INDEX UX_registration_Users_Email    ON registration.Users (Email);
    CREATE UNIQUE INDEX UX_registration_Users_Username ON registration.Users (Username);
    -- Descope id is unique only when present (migrated users have none until Descope links them).
    CREATE UNIQUE INDEX UX_registration_Users_DescopeUserId ON registration.Users (DescopeUserId)
        WHERE DescopeUserId IS NOT NULL;
END
GO

-- Append-only audit of JIT migrations.
IF OBJECT_ID('registration.MigrationAudit', 'U') IS NULL
BEGIN
    CREATE TABLE registration.MigrationAudit
    (
        Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_registration_MigrationAudit PRIMARY KEY,
        Email          NVARCHAR(256)    NOT NULL,
        LegacyMemberId NVARCHAR(128)    NULL,
        DescopeUserId  NVARCHAR(128)    NULL,
        Success        BIT              NOT NULL,
        Detail         NVARCHAR(256)    NULL,
        SourceIp       NVARCHAR(45)     NULL,
        OccurredUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_MigrationAudit_OccurredUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_registration_MigrationAudit_OccurredUtc ON registration.MigrationAudit (OccurredUtc);
END
GO
