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
        IsActive      BIT              NOT NULL CONSTRAINT DF_registration_Users_IsActive  DEFAULT (1),
        CreatedUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_registration_Users_CreatedUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_registration_Users_Email    ON registration.Users (Email);
    CREATE UNIQUE INDEX UX_registration_Users_Username ON registration.Users (Username);
END
GO
