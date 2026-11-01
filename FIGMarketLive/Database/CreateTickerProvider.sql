-- Optional manual schema installation. The application also creates this table
-- transactionally before migrating legacy DataSelections.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.Ticker','U') IS NULL THROW 51061, 'Ticker table is missing.', 1;
IF OBJECT_ID('dbo.TickerProvider','U') IS NULL
    CREATE TABLE dbo.TickerProvider(
        TickerId int NOT NULL,
        ProviderType varchar(50) NOT NULL,
        ProviderInstrumentId nvarchar(128) NOT NULL,
        ProviderSymbol nvarchar(100) NOT NULL,
        CONSTRAINT PK_TickerProvider PRIMARY KEY(TickerId,ProviderType),
        CONSTRAINT FK_TickerProvider_Ticker FOREIGN KEY(TickerId) REFERENCES dbo.Ticker(Id));
IF COL_LENGTH('dbo.TickerProvider','HistoryStartRawTime') IS NULL
    ALTER TABLE dbo.TickerProvider ADD HistoryStartRawTime bigint NULL;
COMMIT;
