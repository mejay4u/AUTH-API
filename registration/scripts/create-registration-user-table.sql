-- Registration service — database-first schema for its OWN database.
-- EF Core maps to this table (it does not create or migrate it). Run this once against the
-- registration database before pointing the API at it with Database:Provider = SqlServer.

IF SCHEMA_ID('registration') IS NULL
    EXEC('CREATE SCHEMA registration');
GO

IF OBJECT_ID('registration.Users', 'U') IS NULL
BEGIN
    CREATE TABLE registration.Users
    (
        Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_registration_Users PRIMARY KEY,
        Email         NVARCHAR(256)    NOT NULL,
        Username      NVARCHAR(256)    NOT NULL,
        PasswordHash  NVARCHAR(512)    NOT NULL,
        PasswordSalt  NVARCHAR(256)    NOT NULL,
        FirstName     NVARCHAR(100)    NOT NULL,
        LastName      NVARCHAR(100)    NOT NULL,
        DateOfBirth   DATE             NOT NULL,
        ZipCode       NVARCHAR(10)     NOT NULL,
        ContactNumber NVARCHAR(20)     NULL,
        IsActive      BIT              NOT NULL CONSTRAINT DF_registration_Users_IsActive  DEFAULT (1),
        CreatedUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_Users_CreatedUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_registration_Users_Email    ON registration.Users (Email);
    CREATE UNIQUE INDEX UX_registration_Users_Username ON registration.Users (Username);
END
GO

-- Pre-account registration sessions (personal info + email-verified flag) until the account is created.
IF OBJECT_ID('registration.PendingRegistrations', 'U') IS NULL
BEGIN
    CREATE TABLE registration.PendingRegistrations
    (
        Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_registration_PendingRegistrations PRIMARY KEY,
        Email         NVARCHAR(256)    NOT NULL,
        FirstName     NVARCHAR(100)    NOT NULL,
        LastName      NVARCHAR(100)    NOT NULL,
        DateOfBirth   DATE             NOT NULL,
        ZipCode       NVARCHAR(10)     NOT NULL,
        ContactNumber NVARCHAR(20)     NULL,
        EmailVerified BIT              NOT NULL CONSTRAINT DF_registration_PendingRegistrations_EmailVerified DEFAULT (0),
        CreatedUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_PendingRegistrations_CreatedUtc   DEFAULT (SYSUTCDATETIME()),
        ExpiresUtc    DATETIME2(3)     NOT NULL
    );

    CREATE INDEX IX_registration_PendingRegistrations_ExpiresUtc ON registration.PendingRegistrations (ExpiresUtc);
END
GO

-- Cleanup of abandoned sessions — run on a schedule (SQL Agent job / cron):
--   DELETE FROM registration.PendingRegistrations WHERE ExpiresUtc < SYSUTCDATETIME();

