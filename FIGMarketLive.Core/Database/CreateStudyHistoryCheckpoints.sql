SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON;
SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Lock int;
    EXEC @Lock=sys.sp_getapplock @Resource='FIGMarketLive.StudyHistorySchema',@LockMode='Exclusive',@LockOwner='Transaction';
    IF @Lock<0 THROW 51090,'Cannot lock checkpoint schema.',1;
    IF OBJECT_ID('dbo.StudyHistory','U') IS NULL
        CREATE TABLE dbo.StudyHistory(
            Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,StudyColId int NOT NULL,RawTime bigint NOT NULL,
            [Open] decimal(28,10) NOT NULL,High decimal(28,10) NOT NULL,Low decimal(28,10) NOT NULL,[Close] decimal(28,10) NOT NULL,Volume int NOT NULL,
            Studies varbinary(max) NOT NULL CONSTRAINT DF_StudyHistory_MarketLive_BinaryStudies DEFAULT(0x),
            CONSTRAINT FK_StudyHistory_MarketLive_StudyCol FOREIGN KEY(StudyColId) REFERENCES dbo.StudyCol(Id));
    IF COL_LENGTH('dbo.StudyHistory','CheckpointState') IS NULL ALTER TABLE dbo.StudyHistory ADD CheckpointState nvarchar(max) NULL;
    IF COL_LENGTH('dbo.StudyHistory','StudiesOverride') IS NULL ALTER TABLE dbo.StudyHistory ADD StudiesOverride varbinary(max) NULL;
    IF COL_LENGTH('dbo.StudyHistory','StudiesBaseDefinitionHash') IS NULL ALTER TABLE dbo.StudyHistory ADD StudiesBaseDefinitionHash varbinary(32) NULL;
    IF COL_LENGTH('dbo.StudyHistory','CheckpointSavedAt') IS NULL ALTER TABLE dbo.StudyHistory ADD CheckpointSavedAt datetime2 NULL;
    IF COL_LENGTH('dbo.StudyHistory','MarketLiveDefinitionHash') IS NULL ALTER TABLE dbo.StudyHistory ADD MarketLiveDefinitionHash varbinary(32) NULL;
    IF COL_LENGTH('dbo.StudyHistory','MarketLivePrefixHash') IS NULL ALTER TABLE dbo.StudyHistory ADD MarketLivePrefixHash varbinary(32) NULL;
    IF COL_LENGTH('dbo.StudyHistory','MarketLivePrefixCount') IS NULL ALTER TABLE dbo.StudyHistory ADD MarketLivePrefixCount int NULL;
    IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='Studies' AND is_nullable=0 AND default_object_id=0 AND system_type_id IN(167,231))
        ALTER TABLE dbo.StudyHistory ADD CONSTRAINT DF_StudyHistory_MarketLive_Studies DEFAULT(N'{}') FOR Studies;
    IF EXISTS(SELECT StudyColId,RawTime FROM dbo.StudyHistory GROUP BY StudyColId,RawTime HAVING COUNT_BIG(*)>1)
        THROW 51091,'StudyHistory has duplicate bars. Resolve duplicates before enabling checkpoints.',1;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='UX_StudyHistory_MarketLive_Bar')
        CREATE UNIQUE INDEX UX_StudyHistory_MarketLive_Bar ON dbo.StudyHistory(StudyColId,RawTime);
    IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='UX_StudyHistory_Checkpoint')
        DROP INDEX UX_StudyHistory_Checkpoint ON dbo.StudyHistory;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.StudyHistory') AND name='UX_StudyHistory_CheckpointBar')
        EXEC(N'CREATE UNIQUE INDEX UX_StudyHistory_CheckpointBar ON dbo.StudyHistory(StudyColId,RawTime DESC) WHERE CheckpointSavedAt IS NOT NULL;');
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
