SET XACT_ABORT ON;
IF OBJECT_ID('dbo.StudyRegeneration','U') IS NULL
BEGIN
    CREATE TABLE dbo.StudyRegeneration(
        StudyColId int NOT NULL PRIMARY KEY REFERENCES dbo.StudyCol(Id) ON DELETE CASCADE,
        DefinitionHash varbinary(32) NOT NULL,
        CompletedCount int NOT NULL,TotalCount int NOT NULL,
        PrefixHash varbinary(32) NULL,
        Status varchar(20) NOT NULL,
        UpdatedAt datetime2 NOT NULL,
        Message nvarchar(2000) NOT NULL DEFAULT('')
    );
END;
IF OBJECT_ID('dbo.StudyTailCheckpoint','U') IS NULL
BEGIN
    CREATE TABLE dbo.StudyTailCheckpoint(
        StudyColId int NOT NULL PRIMARY KEY REFERENCES dbo.StudyCol(Id) ON DELETE CASCADE,
        DefinitionHash varbinary(32) NOT NULL,PrefixCount int NOT NULL,
        PrefixHash varbinary(32) NOT NULL,CheckpointState nvarchar(max) NOT NULL,
        UpdatedAt datetime2 NOT NULL
    );
END;
