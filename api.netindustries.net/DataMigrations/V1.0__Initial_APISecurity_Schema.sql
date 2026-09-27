-- 1. Create the dedicated security schema boundary
CREATE SCHEMA APISecurity;
GO

-- 2. Infrastructure Configuration Storage (Write Heavy)
CREATE TABLE APISecurity.SystemSecrets (
    SecretKey NVARCHAR(100) NOT NULL CONSTRAINT PK_SystemSecrets PRIMARY KEY,
    EncryptedData VARBINARY(MAX) NOT NULL,
    IV VARBINARY(16) NOT NULL,
    LastUpdated DATETIME NOT NULL CONSTRAINT DF_SystemSecrets_LastUpdated DEFAULT GETDATE()
);
GO

-- 3. Inbound Tenant Identity Storage (Read Heavy)
CREATE TABLE APISecurity.ApiIdentities (
    IdentityId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApiIdentities PRIMARY KEY,
    ClientName NVARCHAR(100) NOT NULL,
    EmailAddress NVARCHAR(256) NOT NULL CONSTRAINT UQ_ApiIdentities_Email UNIQUE,
    ApiKeyHash NVARCHAR(64) NOT NULL CONSTRAINT UQ_ApiIdentities_ApiKeyHash UNIQUE,
    IsActive BIT NOT NULL CONSTRAINT DF_ApiIdentities_IsActive DEFAULT 1,
    CreatedDate DATETIME NOT NULL CONSTRAINT DF_ApiIdentities_Created DEFAULT GETDATE()
);
GO

-- 4. Infrastructure Upsert Stored Procedure
CREATE PROCEDURE APISecurity.usp_UpsertSystemSecret
    @SecretKey NVARCHAR(100),
    @EncryptedData VARBINARY(MAX),
    @IV VARBINARY(16)
AS
BEGIN
    SET NOCOUNT ON;
    MERGE INTO APISecurity.SystemSecrets AS Target
    USING (SELECT @SecretKey AS SecretKey) AS Source
    ON Target.SecretKey = Source.SecretKey
    WHEN MATCHED THEN
        UPDATE SET EncryptedData = @EncryptedData, IV = @IV, LastUpdated = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT (SecretKey, EncryptedData, IV, LastUpdated)
        VALUES (@SecretKey, @EncryptedData, @IV, GETDATE());
END
GO

-- 5. Infrastructure Retrieval Stored Procedure
CREATE PROCEDURE APISecurity.usp_GetSystemSecret
    @SecretKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EncryptedData, IV FROM APISecurity.SystemSecrets WHERE SecretKey = @SecretKey;
END
GO

-- 6. Tenant Authentication Stored Procedure
CREATE PROCEDURE APISecurity.usp_GetIdentityByApiKeyHash
    @ApiKeyHash NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ClientName, EmailAddress, IsActive FROM APISecurity.ApiIdentities WHERE ApiKeyHash = @ApiKeyHash;
END
GO

-- 7. New Client Onboarding Registration Command Stored Procedure
CREATE PROCEDURE APISecurity.usp_InsertClientIdentity
    @ClientName NVARCHAR(100),
    @EmailAddress NVARCHAR(256),
    @ApiKeyHash NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS(SELECT 1 FROM APISecurity.ApiIdentities WHERE EmailAddress = @EmailAddress OR ApiKeyHash = @ApiKeyHash)
    BEGIN
        RAISERROR('An identity profile with this email address or API key signature already exists.', 16, 1);
        RETURN;
    END

    INSERT INTO APISecurity.ApiIdentities (ClientName, EmailAddress, ApiKeyHash, IsActive, CreatedDate)
    VALUES (@ClientName, @EmailAddress, @ApiKeyHash, 1, GETDATE());
END
GO
