IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash VARBINARY(MAX) NOT NULL,
        PasswordSalt VARBINARY(64) NOT NULL,
        PasswordIterations INT NOT NULL,
        Role INT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL UNIQUE,
        PhoneNumber NVARCHAR(30) NOT NULL,
        KycStatus INT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accounts
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        AccountNumber NVARCHAR(30) NOT NULL UNIQUE,
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        AccountType INT NOT NULL,
        Currency CHAR(3) NOT NULL,
        Balance DECIMAL(18, 2) NOT NULL,
        DailyDebitLimit DECIMAL(18, 2) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        LastUpdatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT FK_Accounts_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    );
    CREATE INDEX IX_Accounts_CustomerId ON dbo.Accounts(CustomerId);
END;

IF OBJECT_ID(N'dbo.Transfers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transfers
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ExternalReference NVARCHAR(64) NOT NULL UNIQUE,
        FromAccountId UNIQUEIDENTIFIER NOT NULL,
        ToAccountId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        Currency CHAR(3) NOT NULL,
        Narrative NVARCHAR(300) NOT NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        CompletedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_Transfers_FromAccount FOREIGN KEY (FromAccountId) REFERENCES dbo.Accounts(Id),
        CONSTRAINT FK_Transfers_ToAccount FOREIGN KEY (ToAccountId) REFERENCES dbo.Accounts(Id)
    );
    CREATE INDEX IX_Transfers_FromAccountId ON dbo.Transfers(FromAccountId);
    CREATE INDEX IX_Transfers_ToAccountId ON dbo.Transfers(ToAccountId);
END;

IF OBJECT_ID(N'dbo.LedgerEntries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LedgerEntries
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        AccountId UNIQUEIDENTIFIER NOT NULL,
        Reference NVARCHAR(80) NOT NULL UNIQUE,
        EntryType INT NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        BalanceAfter DECIMAL(18, 2) NOT NULL,
        Narrative NVARCHAR(300) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        CONSTRAINT FK_LedgerEntries_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id)
    );
    CREATE INDEX IX_LedgerEntries_AccountId_CreatedAt ON dbo.LedgerEntries(AccountId, CreatedAtUtc DESC);
END;
