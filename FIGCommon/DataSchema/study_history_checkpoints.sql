-- Run in FIGSignalSvc's calculation database with FIGSignalSvc stopped.
-- Replaces the separate dbo.StudyCheckpoint table. Also supports a fresh installation.
-- Repeatable and transactional: unmatched/ambiguous/conflicting legacy rows abort without dropping data.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.StudyHistory', N'U') IS NULL
        THROW 51010, 'StudyHistory is required. Select the calculation database before running this migration.', 1;

    IF COL_LENGTH(N'dbo.StudyHistory', N'CheckpointState') IS NULL
        ALTER TABLE dbo.StudyHistory ADD CheckpointState nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.StudyHistory', N'CheckpointSavedAt') IS NULL
        ALTER TABLE dbo.StudyHistory ADD CheckpointSavedAt datetime2 NULL;

    -- Upgrade the former one-checkpoint-per-collection design without clearing saved state.
    IF EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id=OBJECT_ID(N'dbo.StudyHistory') AND name=N'UX_StudyHistory_Checkpoint')
        DROP INDEX UX_StudyHistory_Checkpoint ON dbo.StudyHistory;

    -- Dynamic SQL compiles after the columns exist, including when this entire script is one batch.
    IF OBJECT_ID(N'dbo.StudyCheckpoint', N'U') IS NOT NULL
        EXEC sys.sp_executesql N'
            IF EXISTS (
                SELECT 1 FROM dbo.StudyCheckpoint c
                WHERE (SELECT COUNT_BIG(*) FROM dbo.StudyHistory h
                       WHERE h.StudyColId=c.StudyColId AND h.RawTime=c.RawTime) <> 1)
                THROW 51011, ''A legacy checkpoint has no unique matching StudyHistory row. Resolve it before migration.'', 1;

            IF EXISTS (
                SELECT 1 FROM dbo.StudyCheckpoint c
                JOIN dbo.StudyHistory h ON h.StudyColId=c.StudyColId AND h.RawTime=c.RawTime
                WHERE h.CheckpointState IS NOT NULL AND
                    CONVERT(varbinary(max),h.CheckpointState)<>CONVERT(varbinary(max),c.Payload))
                THROW 51012, ''Existing StudyHistory checkpoint conflicts with a legacy checkpoint.'', 1;

            UPDATE h SET CheckpointState=c.Payload, CheckpointSavedAt=c.SavedAt
            FROM dbo.StudyHistory h JOIN dbo.StudyCheckpoint c
                ON h.StudyColId=c.StudyColId AND h.RawTime=c.RawTime
            WHERE h.CheckpointState IS NULL;';

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE parent_object_id=OBJECT_ID(N'dbo.StudyHistory') AND name=N'CK_StudyHistory_Checkpoint')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.StudyHistory WITH CHECK ADD CONSTRAINT CK_StudyHistory_Checkpoint
            CHECK ((CheckpointState IS NULL AND CheckpointSavedAt IS NULL)
                OR (CheckpointState IS NOT NULL AND CheckpointSavedAt IS NOT NULL AND ISJSON(CheckpointState)=1));';

    -- One snapshot per bar, with newest-first lookup for restoration after deleting a history tail.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE object_id=OBJECT_ID(N'dbo.StudyHistory') AND name=N'UX_StudyHistory_CheckpointBar')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_StudyHistory_CheckpointBar ON dbo.StudyHistory(StudyColId, RawTime DESC)
            WHERE CheckpointSavedAt IS NOT NULL;';

    IF OBJECT_ID(N'dbo.StudyCheckpoint', N'U') IS NOT NULL
        DROP TABLE dbo.StudyCheckpoint;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
