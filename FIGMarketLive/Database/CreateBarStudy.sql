-- Run against a dedicated FIGMarketLive database containing Ticker, Interval,
-- DataSet, PriceData and StudyCol from the supplied schema.
-- This script does not convert or overwrite existing JSON study history.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.BarStudy', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BarStudy (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BarStudy PRIMARY KEY,
        StudyColId int NOT NULL CONSTRAINT FK_BarStudy_StudyCol REFERENCES dbo.StudyCol(Id),
        RawTime bigint NOT NULL,
        [Open] decimal(18,4) NOT NULL,
        High decimal(18,4) NOT NULL,
        Low decimal(18,4) NOT NULL,
        [Close] decimal(18,4) NOT NULL,
        Volume int NOT NULL,
        Studies varbinary(max) NOT NULL,
        CONSTRAINT IX_BarStudy_StudyColId_RawTime UNIQUE(StudyColId, RawTime)
    );
END;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.BarStudy')
    AND name='Studies' AND system_type_id=165 AND max_length=-1)
    THROW 51022, 'Existing Studies column is not varbinary(max). No conversion was performed.', 1;
COMMIT;
