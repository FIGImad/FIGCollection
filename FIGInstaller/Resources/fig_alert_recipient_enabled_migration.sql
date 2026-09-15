-- Recipient.Enabled migration for the attached/current FIGAlert schema.
-- Select the Alerts database in SSMS, then run this entire script before the new binaries.
-- SQL Server 2016 SP1+ (CREATE OR ALTER). No database name is hard-coded.
-- Repeatable: existing Enabled=0 values are preserved, missing values default to 1.
-- All schema/procedure changes are committed together or rolled back together.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Recipient', N'U') IS NULL
        THROW 50001, 'Recipient table is missing. Select the Alerts database.', 1;
    IF OBJECT_ID(N'dbo.RecipientSubscription', N'U') IS NULL
       OR OBJECT_ID(N'dbo.AlertImmediateSource', N'U') IS NULL
       OR COL_LENGTH(N'dbo.Alert', N'IgnoredAt') IS NULL
       OR COL_LENGTH(N'dbo.Alert', N'IgnoreReason') IS NULL
        THROW 50002, 'Apply the current Alerts schema (including immediate dispatch and ignore tracking) first.', 1;

    IF COL_LENGTH(N'dbo.Recipient', N'Enabled') IS NULL
        ALTER TABLE dbo.Recipient ADD Enabled bit NOT NULL
            CONSTRAINT DF_Recipient_Enabled DEFAULT (1) WITH VALUES;
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Recipient')
                   AND name = N'Enabled' AND (system_type_id <> TYPE_ID(N'bit') OR is_computed = 1))
            THROW 50003, 'Recipient.Enabled already exists but is not a writable bit column.', 1;

        -- Dynamic SQL compiles after a missing column has been added.
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Recipient')
                   AND name = N'Enabled' AND is_nullable = 1)
        BEGIN
            EXEC sys.sp_executesql N'UPDATE dbo.Recipient SET Enabled = 1 WHERE Enabled IS NULL;
                                    ALTER TABLE dbo.Recipient ALTER COLUMN Enabled bit NOT NULL;';
        END;
    END;

    DECLARE @DefaultName sysname, @DefaultDefinition nvarchar(max), @Sql nvarchar(max);
    SELECT @DefaultName = dc.name, @DefaultDefinition = dc.definition
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Recipient') AND c.name = N'Enabled';

    -- Repair an existing default of 0 without changing any stored disabled values.
    IF @DefaultName IS NOT NULL
       AND REPLACE(REPLACE(REPLACE(@DefaultDefinition, N'(', N''), N')', N''), N' ', N'') <> N'1'
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.Recipient DROP CONSTRAINT ' + QUOTENAME(@DefaultName) + N';';
        EXEC sys.sp_executesql @Sql;
        SET @DefaultName = NULL;
    END;
    IF @DefaultName IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recipient ADD CONSTRAINT DF_Recipient_Enabled DEFAULT (1) FOR Enabled;';

    -- CREATE OR ALTER preserves procedure permissions. Separate dynamic batches
    -- allow the column and all five procedures to share the same transaction.

    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[usp_recipient_select]
    @Id int
AS
BEGIN
    SET NOCOUNT ON;
    -- Keep disabled recipients visible so administrators can re-enable them.
    SELECT [Id], [Alias], [Name], [Email], [PushoverKey], [Enabled]
    FROM [dbo].[Recipient]
    WHERE @Id = -1 OR [Id] = @Id;
END;';

    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[usp_recipient_upsert]
    @IdNew int OUTPUT,
    @Id int,
    @Alias varchar(256),
    @Name varchar(256),
    @Email varchar(256),
    @PushoverKey varchar(256),
    @Enabled bit = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Old callers may omit Enabled: enable inserts, preserve existing updates.
    SET @IdNew = NULL;
    SELECT @IdNew = [Id] FROM [dbo].[Recipient] WHERE [Id] = @Id;
    IF @IdNew IS NULL
    BEGIN
        INSERT INTO [dbo].[Recipient] ([Alias], [Name], [Email], [PushoverKey], [Enabled])
        VALUES (@Alias, @Name, @Email, @PushoverKey, COALESCE(@Enabled, CONVERT(bit, 1)));
        SET @IdNew = CONVERT(int, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE [dbo].[Recipient]
        SET [Alias] = @Alias,
            [Name] = @Name,
            [Email] = @Email,
            [PushoverKey] = @PushoverKey,
            [Enabled] = COALESCE(@Enabled, [Enabled])
        WHERE [Id] = @IdNew;
    END;
    RETURN @IdNew;
END;';

    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[usp_recipient_delete]
	@Id int
AS
BEGIN
	Delete [dbo].[Recipient] Where Id = @Id;
END';

    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_get_pending]
(
	@LookbackMinutes int = 60
)
AS
BEGIN
	SET NOCOUNT ON;

	IF (@LookbackMinutes IS NULL OR @LookbackMinutes <= 0)
		SET @LookbackMinutes = 60;

	DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();
	DECLARE @CutoffRawTime int = DATEDIFF(SECOND, CONVERT(datetime2(0), ''19700101''), DATEADD(MINUTE, -@LookbackMinutes, @NowUtc));

	UPDATE [dbo].[Alert]
	SET    [IgnoredAt]    = @NowUtc,
	       [IgnoreReason] = CONCAT(''Older than pending alert lookback window of '', @LookbackMinutes, '' minutes'')
	WHERE  [SentAt]          IS NULL
	  AND  [IgnoredAt]       IS NULL
	  AND  [RawTime]         < @CutoffRawTime
	  AND  [MatchedRuleId]   IS NOT NULL
	  AND  [FriendlyMessage] IS NOT NULL;

	SELECT
		a.[Id]               AS AlertId,
		a.[FriendlyMessage],
		rs.[AlertMethods],
		r.[Id]               AS RecipientId,
		r.[Name]             AS RecipientName,
		r.[Email],
		r.[PushoverKey],
		a.[MatchedRuleId]    AS RuleId,
		r.[Enabled] AS RecipientEnabled
	FROM  [dbo].[Alert]                  a
	INNER JOIN [dbo].[RecipientSubscription] rs ON rs.[AlertRuleId] = a.[MatchedRuleId]
	INNER JOIN [dbo].[Recipient]             r  ON r.[Id]           = rs.[RecipientId]
	WHERE r.[Enabled] = 1 AND a.[SentAt]          IS NULL
	  AND a.[IgnoredAt]       IS NULL
	  AND a.[RawTime]         >= @CutoffRawTime
	  AND a.[MatchedRuleId]   IS NOT NULL
	  AND a.[FriendlyMessage] IS NOT NULL
	  AND (
			(rs.[AlertMethods] & 1 = 1 AND r.[Email]       <> '''')
		 OR (rs.[AlertMethods] & 2 = 2 AND r.[PushoverKey] IS NOT NULL AND r.[PushoverKey] <> '''')
		  )
	ORDER BY a.[Id];
END';

    EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_get_pending_immediate]
(
    @MaxRec int = 100
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxRec IS NULL OR @MaxRec <= 0
        SET @MaxRec = 100;


    ;WITH PendingAlerts AS
    (
        SELECT TOP (@MaxRec)
               a.[Id]
        FROM dbo.[Alert] AS a

        INNER JOIN dbo.AlertImmediateSource AS src
            ON src.[Source] = a.[Source]
           AND src.[Enabled] = 1

        WHERE a.[SentAt] IS NULL
          AND a.[IgnoredAt] IS NULL
          AND a.[MatchedRuleId] IS NOT NULL
          AND a.[FriendlyMessage] IS NOT NULL
          -- Apply eligibility BEFORE TOP: disabled-only alerts must not starve the queue.
          AND EXISTS
          (
              SELECT 1
              FROM dbo.RecipientSubscription AS eligibleSubscription
              INNER JOIN dbo.Recipient AS eligibleRecipient
                  ON eligibleRecipient.Id = eligibleSubscription.RecipientId
                 AND eligibleRecipient.Enabled = 1
              WHERE eligibleSubscription.AlertRuleId = a.MatchedRuleId
                AND ((eligibleSubscription.AlertMethods & 1 = 1 AND eligibleRecipient.Email <> '''')
                  OR (eligibleSubscription.AlertMethods & 2 = 2 AND eligibleRecipient.PushoverKey IS NOT NULL
                      AND eligibleRecipient.PushoverKey <> ''''))
          )

        ORDER BY
            a.[RawTime],
            a.[MSec],
            a.[Id]
    )

    SELECT
         a.[Id]                AS AlertId
        ,a.[Source]
        ,a.[Level]
        ,a.[FriendlyMessage]

        ,rs.[AlertMethods]

        ,r.[Id]                AS RecipientId
        ,r.[Name]              AS RecipientName
        ,r.[Email]
        ,r.[PushoverKey]

        ,a.[MatchedRuleId]     AS RuleId
        ,r.[Enabled] AS RecipientEnabled

    FROM PendingAlerts AS p

    INNER JOIN dbo.[Alert] AS a
        ON a.[Id] = p.[Id]

    INNER JOIN dbo.[RecipientSubscription] AS rs
        ON rs.[AlertRuleId] = a.[MatchedRuleId]

    INNER JOIN dbo.[Recipient] AS r
        ON r.[Id] = rs.[RecipientId] AND r.[Enabled] = 1

    WHERE
           (
                (rs.[AlertMethods] & 1 = 1
                    AND r.[Email] <> '''')

             OR (rs.[AlertMethods] & 2 = 2
                    AND r.[PushoverKey] IS NOT NULL
                    AND r.[PushoverKey] <> '''')
           )

    ORDER BY
        a.[RawTime],
        a.[MSec],
        a.[Id],
        r.[Id];
END;';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;