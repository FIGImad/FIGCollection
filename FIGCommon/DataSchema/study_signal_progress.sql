-- Run once in FIGSignalSvc's calculation database with all signal generators stopped.
-- This marker is independent of StudyHistory/checkpoints and must not be rewound when rebuilding history.
-- Existing history (including its latest forming bar) is conservatively treated as already processed.
-- Signals already issued beyond a deleted history tail also raise the initial marker.
-- Re-running this migration never advances or resets an existing marker.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.StudyCol',N'U') IS NULL OR OBJECT_ID(N'dbo.StudyHistory',N'U') IS NULL
        OR OBJECT_ID(N'dbo.Signal',N'U') IS NULL OR OBJECT_ID(N'dbo.StrategyConfig',N'U') IS NULL
        THROW 51023, 'Select the FIGSignalSvc calculation database before running study_signal_progress.sql.', 1;

    IF OBJECT_ID(N'dbo.StudySignalProgress',N'U') IS NULL
        CREATE TABLE dbo.StudySignalProgress (
            StudyColId int NOT NULL CONSTRAINT PK_StudySignalProgress PRIMARY KEY,
            ProcessedThrough bigint NOT NULL,
            UpdatedAt datetime2 NOT NULL CONSTRAINT DF_StudySignalProgress_UpdatedAt DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_StudySignalProgress_StudyCol FOREIGN KEY(StudyColId) REFERENCES dbo.StudyCol(Id) ON DELETE CASCADE
        );

    EXEC sys.sp_executesql N'
        INSERT dbo.StudySignalProgress(StudyColId,ProcessedThrough)
        SELECT c.Id,COALESCE(MAX(t.RawTime),-1)
        FROM dbo.StudyCol c
        OUTER APPLY (
            SELECT CONVERT(bigint,h.RawTime) RawTime FROM dbo.StudyHistory h WHERE h.StudyColId=c.Id
            UNION ALL
            SELECT CONVERT(bigint,s.StartTime) FROM dbo.Signal s
                JOIN dbo.StrategyConfig sc ON sc.Strategy=s.Strategy WHERE sc.StudyColId=c.Id
            UNION ALL
            SELECT CONVERT(bigint,s.StopTime) FROM dbo.Signal s
                JOIN dbo.StrategyConfig sc ON sc.Strategy=s.Strategy WHERE sc.StudyColId=c.Id
        ) t
        WHERE NOT EXISTS (SELECT 1 FROM dbo.StudySignalProgress p WITH (UPDLOCK,HOLDLOCK) WHERE p.StudyColId=c.Id)
        GROUP BY c.Id;';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
