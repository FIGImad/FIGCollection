-- Run ONLY against the private signal database, with FIGSignalSvc stopped.
-- Does not enable publication: remote service startup initializes the producer.
SET XACT_ABORT ON;
GO
IF OBJECT_ID('dbo.SignalPublicationConfig') IS NULL
    CREATE TABLE dbo.SignalPublicationConfig (
        Id int NOT NULL PRIMARY KEY CHECK(Id=1), ProducerId varchar(50) NULL,
        Enabled bit NOT NULL DEFAULT 0);
IF NOT EXISTS(SELECT 1 FROM dbo.SignalPublicationConfig WHERE Id=1)
    INSERT dbo.SignalPublicationConfig(Id,Enabled) VALUES(1,0);
IF COL_LENGTH('dbo.Signal','SyncVersion') IS NULL
    ALTER TABLE dbo.Signal ADD SyncVersion bigint NOT NULL CONSTRAINT DF_Signal_SyncVersion DEFAULT 0;
IF COL_LENGTH('dbo.Signal','SyncedVersion') IS NULL
    ALTER TABLE dbo.Signal ADD SyncedVersion bigint NOT NULL CONSTRAINT DF_Signal_SyncedVersion DEFAULT 0;
IF COL_LENGTH('dbo.Signal','SyncedAt') IS NULL
    ALTER TABLE dbo.Signal ADD SyncedAt datetime2 NULL;
IF COL_LENGTH('dbo.Signal','SyncError') IS NULL
    ALTER TABLE dbo.Signal ADD SyncError nvarchar(1000) NULL;
GO
IF COL_LENGTH('dbo.Signal','IsSynced') IS NULL
    ALTER TABLE dbo.Signal ADD IsSynced AS CONVERT(bit,CASE WHEN SyncVersion>0 AND SyncVersion=SyncedVersion THEN 1 ELSE 0 END);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Signal') AND name='UX_Signal_PublicationTag')
    CREATE UNIQUE INDEX UX_Signal_PublicationTag ON dbo.Signal(Tag);
IF OBJECT_ID('dbo.SignalPublicationOutbox') IS NULL
    CREATE TABLE dbo.SignalPublicationOutbox (
        SignalId int NOT NULL REFERENCES dbo.Signal(Id), Revision bigint NOT NULL,
        MessageId uniqueidentifier NOT NULL DEFAULT NEWID() UNIQUE,
        Payload nvarchar(max) NOT NULL CHECK(ISJSON(Payload)=1),
        CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), AcknowledgedAt datetime2 NULL,
        CONSTRAINT PK_SignalPublicationOutbox PRIMARY KEY(SignalId,Revision));
GO
CREATE OR ALTER TRIGGER dbo.trigSignalPublication ON dbo.Signal AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    -- Metadata acknowledgement writes and our own revision increment must not recurse.
    IF NOT (UPDATE(Tag) OR UPDATE(Strategy) OR UPDATE(Status) OR UPDATE(Side)
        OR UPDATE(StartTime) OR UPDATE(StopTime) OR UPDATE(StartPrice)
        OR UPDATE(StopPrice) OR UPDATE(LastUpdated)) RETURN;
    IF NOT EXISTS(SELECT 1 FROM dbo.SignalPublicationConfig WHERE Id=1 AND Enabled=1) RETURN;
    IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
        WHERE EXISTS(SELECT i.Tag,i.Strategy,i.Side,i.StartTime EXCEPT SELECT d.Tag,d.Strategy,d.Side,d.StartTime)
        OR (d.Status='STOP' AND EXISTS(SELECT i.Status,i.StopTime EXCEPT SELECT d.Status,d.StopTime)))
        THROW 51001, 'Published signal identity is immutable and STOP is final; prices may be corrected.', 1;
    DECLARE @Changed TABLE(Id int PRIMARY KEY);
    INSERT @Changed SELECT i.Id FROM inserted i LEFT JOIN deleted d ON d.Id=i.Id
        WHERE d.Id IS NULL OR EXISTS(
            SELECT i.Tag,i.Strategy,i.Status,i.Side,i.StartTime,i.StopTime,i.StartPrice,i.StopPrice,i.LastUpdated
            EXCEPT SELECT d.Tag,d.Strategy,d.Status,d.Side,d.StartTime,d.StopTime,d.StartPrice,d.StopPrice,d.LastUpdated);
    UPDATE s SET SyncVersion=SyncVersion+1, SyncError=NULL
        FROM dbo.Signal s JOIN @Changed c ON c.Id=s.Id;
    INSERT dbo.SignalPublicationOutbox(SignalId,Revision,Payload)
        SELECT s.Id,s.SyncVersion,(SELECT s.Tag,s.Strategy,s.Status,s.Side,s.StartTime,s.StopTime,
            s.StartPrice,s.StopPrice,s.LastUpdated FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)
        FROM dbo.Signal s JOIN @Changed c ON c.Id=s.Id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_signal_publication_initialize @ProducerId varchar(50) AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@ProducerId)),'') IS NULL THROW 51002, 'Producer identity is required.', 1;
    BEGIN TRANSACTION;
    -- Lock Signal before config, matching the order used by writers/trigger.
    DECLARE @Count bigint;
    SELECT @Count=COUNT_BIG(*) FROM dbo.Signal WITH(TABLOCKX,HOLDLOCK);
    IF EXISTS(SELECT 1 FROM dbo.SignalPublicationConfig WITH(UPDLOCK,HOLDLOCK)
        WHERE Id=1 AND ProducerId IS NOT NULL AND ProducerId<>@ProducerId)
        THROW 51002, 'Producer identity cannot be changed after initialization.', 1;
    IF OBJECT_ID('dbo.SignalPublicationReceipt') IS NOT NULL
        THROW 51002, 'Receiver database cannot be used as a publication source.', 1;
    UPDATE dbo.SignalPublicationConfig SET ProducerId=@ProducerId,Enabled=1 WHERE Id=1;
    UPDATE dbo.Signal SET SyncVersion=1 WHERE SyncVersion=0;
    INSERT dbo.SignalPublicationOutbox(SignalId,Revision,Payload)
        SELECT s.Id,s.SyncVersion,(SELECT s.Tag,s.Strategy,s.Status,s.Side,s.StartTime,s.StopTime,
            s.StartPrice,s.StopPrice,s.LastUpdated FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)
        FROM dbo.Signal s WHERE NOT EXISTS(SELECT 1 FROM dbo.SignalPublicationOutbox o
            WHERE o.SignalId=s.Id AND o.Revision=s.SyncVersion);
    COMMIT;
END;
GO
-- Keep existing Event integration, but ignore synchronization bookkeeping writes.
-- DISTINCT also handles multiple signals for one strategy in a single statement.
CREATE OR ALTER TRIGGER dbo.trigSignalOnChange ON dbo.Signal AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(Tag) OR UPDATE(Strategy) OR UPDATE(Status) OR UPDATE(Side)
        OR UPDATE(StartTime) OR UPDATE(StopTime) OR UPDATE(StartPrice)
        OR UPDATE(StopPrice) OR UPDATE(LastUpdated)) RETURN;
    DECLARE @Now datetime=GETDATE();
    UPDATE e SET [TimeStamp]=@Now,[Message]='Updated for Strategy: '+i.Strategy
        FROM dbo.Event e JOIN (SELECT DISTINCT Strategy FROM inserted) i
        ON e.EventType='SIGNAL' AND e.EventId=i.Strategy;
    INSERT dbo.Event(EventType,EventId,[Message],[TimeStamp])
        SELECT 'SIGNAL',i.Strategy,'Inserted automatically for Strategy: '+i.Strategy,@Now
        FROM (SELECT DISTINCT Strategy FROM inserted) i WHERE NOT EXISTS(
            SELECT 1 FROM dbo.Event e WITH(UPDLOCK,HOLDLOCK) WHERE e.EventType='SIGNAL' AND e.EventId=i.Strategy);
END;
GO
