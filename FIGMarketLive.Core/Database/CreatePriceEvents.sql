SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource='FIGMarketLive.PriceEvents',@LockMode='Exclusive',@LockOwner='Transaction';
IF @Lock<0 THROW 51100,'Cannot lock price event schema.',1;
IF OBJECT_ID('dbo.Event','U') IS NULL
    CREATE TABLE dbo.Event(Id int IDENTITY PRIMARY KEY,EventType varchar(50) NOT NULL,EventId varchar(100) NOT NULL,
        Message varchar(256) NOT NULL,[TimeStamp] datetime NOT NULL,CONSTRAINT UX_Event_MarketLive UNIQUE(EventType,EventId));
IF COL_LENGTH('dbo.Event','Id') IS NULL OR COL_LENGTH('dbo.Event','EventType') IS NULL OR COL_LENGTH('dbo.Event','EventId') IS NULL
    OR COL_LENGTH('dbo.Event','Message') IS NULL OR COL_LENGTH('dbo.Event','TimeStamp') IS NULL
    THROW 51101,'dbo.Event is incompatible; Id, EventType, EventId, Message and TimeStamp are required.',1;
EXEC(N'CREATE OR ALTER TRIGGER dbo.trigMarketLivePriceDataChange ON dbo.PriceData AFTER INSERT,UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Changed TABLE(DataSetId int PRIMARY KEY,FirstRawTime bigint);
    INSERT @Changed
    SELECT DataSetId,MIN(RawTime) FROM (
        SELECT DataSetId,RawTime FROM
            (SELECT DataSetId,RawTime,[Open],High,Low,[Close],Volume FROM inserted
             EXCEPT SELECT DataSetId,RawTime,[Open],High,Low,[Close],Volume FROM deleted) a
        UNION ALL
        SELECT DataSetId,RawTime FROM
            (SELECT DataSetId,RawTime,[Open],High,Low,[Close],Volume FROM deleted
             EXCEPT SELECT DataSetId,RawTime,[Open],High,Low,[Close],Volume FROM inserted) b
    ) changed GROUP BY DataSetId;
    IF NOT EXISTS(SELECT 1 FROM @Changed) RETURN;
    DECLARE @Version varchar(36)=CONVERT(varchar(36),NEWID());
    UPDATE e WITH(UPDLOCK,HOLDLOCK) SET [TimeStamp]=GETDATE(),
        Message=CONCAT(''FirstRawTime='',c.FirstRawTime,'';Version='',@Version)
    FROM dbo.Event e JOIN @Changed c ON e.EventType=''MARKETLIVE_PRICEDATA'' AND e.EventId=CONVERT(varchar(100),c.DataSetId);
    INSERT dbo.Event(EventType,EventId,Message,[TimeStamp])
        SELECT ''MARKETLIVE_PRICEDATA'',CONVERT(varchar(100),c.DataSetId),CONCAT(''FirstRawTime='',c.FirstRawTime,'';Version='',@Version),GETDATE()
        FROM @Changed c WHERE NOT EXISTS(SELECT 1 FROM dbo.Event e WITH(UPDLOCK,HOLDLOCK)
            WHERE e.EventType=''MARKETLIVE_PRICEDATA'' AND e.EventId=CONVERT(varchar(100),c.DataSetId));
END');
ENABLE TRIGGER dbo.trigMarketLivePriceDataChange ON dbo.PriceData;
COMMIT;
