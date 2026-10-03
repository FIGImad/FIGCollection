-- Run ONLY against the main execution database. No source capture is enabled here.
SET XACT_ABORT ON;
GO
IF OBJECT_ID('dbo.SignalPublicationConfig') IS NULL
    CREATE TABLE dbo.SignalPublicationConfig (
        Id int NOT NULL PRIMARY KEY CHECK(Id=1), ProducerId varchar(50) NULL,
        Enabled bit NOT NULL DEFAULT 0);
IF NOT EXISTS(SELECT 1 FROM dbo.SignalPublicationConfig WHERE Id=1)
    INSERT dbo.SignalPublicationConfig(Id,Enabled) VALUES(1,0);
IF EXISTS(SELECT 1 FROM dbo.SignalPublicationConfig WHERE Enabled=1)
    THROW 51003, 'A publication source cannot also be a receiver.', 1;
GO
IF OBJECT_ID('dbo.SignalPublicationReceipt') IS NULL
    CREATE TABLE dbo.SignalPublicationReceipt (
        ProducerId varchar(50) NOT NULL, SignalTag varchar(50) NOT NULL,
        SignalId int NOT NULL UNIQUE, Revision bigint NOT NULL,
        PayloadHash char(64) NOT NULL, Payload nvarchar(max) NOT NULL CHECK(ISJSON(Payload)=1),
        LastVerifiedAt datetime2 NOT NULL,
        CONSTRAINT PK_SignalPublicationReceipt PRIMARY KEY(ProducerId,SignalTag));
-- No foreign key on SignalId: reconciliation can recover a deleted materialized Signal.
GO
