-- Registration service — database-first schema for its OWN database.
-- EF Core maps to these tables (it does not create or migrate them). Run this once against the
-- registration database before pointing the API at it with Database:Provider = SqlServer.

IF SCHEMA_ID('registration') IS NULL
    EXEC('CREATE SCHEMA registration');
GO

-- Portal users, created once registration completes (eligibility confirmed against Facets).
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
        -- Eligibility, from the Facets match. SubscriberId and PlanId are what Descope maps into the
        -- session JWT's custom claims.
        SubscriberId  NVARCHAR(50)     NULL,
        PlanId        NVARCHAR(50)     NULL,
        -- Last four digits only. The full SSN is used to match against Facets and then discarded.
        SsnLast4      NCHAR(4)         NULL,
        IsActive      BIT              NOT NULL CONSTRAINT DF_registration_Users_IsActive  DEFAULT (1),
        CreatedUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_Users_CreatedUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_registration_Users_Email    ON registration.Users (Email);
    CREATE UNIQUE INDEX UX_registration_Users_Username ON registration.Users (Username);
END
GO

-- In-progress member records: created by initiateRegistration (after Descope verifies the email),
-- given a password hash at the password step, and deleted when promoted to a user.
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
        -- Null until the password step.
        PasswordHash  NVARCHAR(512)    NULL,
        PasswordSalt  NVARCHAR(256)    NULL,
        CreatedUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_PendingRegistrations_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        ExpiresUtc    DATETIME2(3)     NOT NULL
    );

    -- One in-progress registration per address: this is what makes "resume on retry" safe.
    CREATE UNIQUE INDEX UX_registration_PendingRegistrations_Email      ON registration.PendingRegistrations (Email);
    CREATE INDEX        IX_registration_PendingRegistrations_ExpiresUtc ON registration.PendingRegistrations (ExpiresUtc);
END
GO

-- Cleanup of abandoned registrations — run on a schedule (SQL Agent job / cron). Matters more than it
-- looks: an abandoned row holds the unique index on that email address until it is removed.
--   DELETE FROM registration.PendingRegistrations WHERE ExpiresUtc < SYSUTCDATETIME();
