-- Select FIGAlert in SSMS. Apply before deploying the new binaries.
-- Requires the current schema including Recipient.Enabled/TimeZoneId and immediate delivery.
-- Repeatable and transactional; existing alert categories are preserved on rerun.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT <> 0 THROW 50000, 'Run without an existing transaction.', 1;
IF OBJECT_ID(N'dbo.AlertImmediateSource', N'U') IS NULL
   OR COL_LENGTH(N'dbo.Recipient', N'TimeZoneId') IS NULL
   OR COL_LENGTH(N'dbo.Recipient', N'Enabled') IS NULL
   OR COL_LENGTH(N'dbo.Alert', N'IgnoredAt') IS NULL
   OR COL_LENGTH(N'dbo.Alert', N'IgnoreReason') IS NULL
    THROW 50001, 'Apply the current delivery/timezone and recipient-enabled migrations first.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.Alert', N'IsSystemAlert') IS NULL
    BEGIN
        ALTER TABLE dbo.Alert ADD IsSystemAlert bit NOT NULL
            CONSTRAINT DF_Alert_IsSystemAlert DEFAULT (0) WITH VALUES;
        -- Legacy ingestion unwrapped the producer source. Backfill only known
        -- ALT/ALERT rows with the structured database-alert header; review these
        -- legacy matches if other producers used the same message format.
        EXEC(N'UPDATE dbo.Alert SET IsSystemAlert = 1
               WHERE [Level] IN (''ALT'', ''ALERT'')
                 AND ([Source] = ''FIGAutoTradeExSvc.Services.SystemAlertMonitorService''
                      OR [Message] LIKE ''Source: %, Time: %''
                      OR [Message] LIKE ''Alert: Source: %, Time: %'');');
    END;
EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_get_pending]
(
	@LookbackMinutes int = 30,
    @CategoryFilter int = -1
)
AS
BEGIN
	SET NOCOUNT ON;
    IF @CategoryFilter IS NULL OR @CategoryFilter NOT IN (-1, 0, 1)
        THROW 50003, ''Invalid category filter: use -1, 0 or 1.'', 1;

	IF (@LookbackMinutes IS NULL OR @LookbackMinutes <= 0)
		SET @LookbackMinutes = 30;

	DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();
	DECLARE @CutoffRawTime int = DATEDIFF(SECOND, CONVERT(datetime2(0), ''19700101''), DATEADD(MINUTE, -@LookbackMinutes, @NowUtc));

	UPDATE [dbo].[Alert]
	SET    [IgnoredAt]    = @NowUtc,
	       [IgnoreReason] = CONCAT(''Older than pending alert lookback window of '', @LookbackMinutes, '' minutes'')
	WHERE (@CategoryFilter = -1 OR [IsSystemAlert] = @CategoryFilter)
      AND [SentAt]          IS NULL
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
		r.[Enabled] AS RecipientEnabled,
        a.[RawTime], a.[MSec], r.[TimeZoneId],
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AlertImmediateSource src WHERE src.[Source] = a.[Source] AND src.[Enabled] = 1) THEN 1 ELSE 0 END AS bit) AS IsImmediate
	FROM  [dbo].[Alert]                  a
	INNER JOIN [dbo].[RecipientSubscription] rs ON rs.[AlertRuleId] = a.[MatchedRuleId]
	INNER JOIN [dbo].[Recipient]             r  ON r.[Id]           = rs.[RecipientId]
	WHERE (@CategoryFilter = -1 OR a.[IsSystemAlert] = @CategoryFilter)
      AND r.[Enabled] = 1 AND a.[SentAt]          IS NULL
	  AND a.[IgnoredAt]       IS NULL
	  AND a.[RawTime]         >= @CutoffRawTime
	  AND a.[MatchedRuleId]   IS NOT NULL
	  AND a.[FriendlyMessage] IS NOT NULL
	  AND (
			(rs.[AlertMethods] & 1 = 1 AND r.[Email]       <> '''')
		 OR (rs.[AlertMethods] & 2 = 2 AND r.[PushoverKey] IS NOT NULL AND r.[PushoverKey] <> '''')
		  )
	ORDER BY a.[Id];
END');
EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_get_pending_immediate]
(
    @MaxRec int = 100,
    @LookbackMinutes int = 30,
    @CategoryFilter int = -1
)
AS
BEGIN
    SET NOCOUNT ON;
    IF @CategoryFilter IS NULL OR @CategoryFilter NOT IN (-1, 0, 1)
        THROW 50003, ''Invalid category filter: use -1, 0 or 1.'', 1;

    IF @MaxRec IS NULL OR @MaxRec <= 0
        SET @MaxRec = 100;


	IF (@LookbackMinutes IS NULL OR @LookbackMinutes <= 0)
		SET @LookbackMinutes = 30;

	DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();
	DECLARE @CutoffRawTime int = DATEDIFF(SECOND, CONVERT(datetime2(0), ''19700101''), DATEADD(MINUTE, -@LookbackMinutes, @NowUtc));

	UPDATE [dbo].[Alert]
	SET    [IgnoredAt]    = @NowUtc,
	       [IgnoreReason] = CONCAT(''Older than pending alert lookback window of '', @LookbackMinutes, '' minutes'')
	WHERE (@CategoryFilter = -1 OR [IsSystemAlert] = @CategoryFilter)
      AND [SentAt]          IS NULL
	  AND  [IgnoredAt]       IS NULL
	  AND  [RawTime]         < @CutoffRawTime
	  AND  [MatchedRuleId]   IS NOT NULL
	  AND  [FriendlyMessage] IS NOT NULL;


    ;WITH PendingAlerts AS
    (
        SELECT TOP (@MaxRec)
               a.[Id]
        FROM dbo.[Alert] AS a

        INNER JOIN dbo.AlertImmediateSource AS src
            ON src.[Source] = a.[Source]
           AND src.[Enabled] = 1

        WHERE (@CategoryFilter = -1 OR a.[IsSystemAlert] = @CategoryFilter)
          AND a.[SentAt] IS NULL
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
        ,r.[Enabled] AS RecipientEnabled,
        a.[RawTime], a.[MSec], r.[TimeZoneId],
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AlertImmediateSource src WHERE src.[Source] = a.[Source] AND src.[Enabled] = 1) THEN 1 ELSE 0 END AS bit) AS IsImmediate

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
END;');
EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_upsert]
(
    @IdNew    int OUTPUT,
    @Id       int,
    @RawTime  int,
    @MSec     int,
    @ServiceId      varchar(50),
    @ServiceName    varchar(256),
    @ServiceRole    int,
    @ServiceAddress varchar(256),
    @LogName        varchar(256),
    @Source         varchar(50),
    @Level          varchar(20),
    @Message        varchar(max),
    @ExceptionText  varchar(max) = NULL,
    @IsSystemAlert bit = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @IdNew = [Id] FROM [dbo].[Alert] WHERE [Id] = @Id;
    IF (@@ROWCOUNT = 0)
    BEGIN
        -- Does not exist, insert new
        INSERT INTO [dbo].[Alert] (
             [RawTime]
            ,[MSec]
            ,[ServiceId]
            ,[ServiceName]
            ,[ServiceRole]
            ,[ServiceAddress]
            ,[LogName]
            ,[Source]
            ,[Level]
            ,[Message]
            ,[ExceptionText]
            ,[IsSystemAlert]
        )
        VALUES (
             @RawTime
            ,@MSec
            ,@ServiceId
            ,@ServiceName
            ,@ServiceRole
            ,@ServiceAddress
            ,@LogName
            ,@Source
            ,@Level
            ,@Message
            ,@ExceptionText
            ,@IsSystemAlert
        )
        SET @IdNew = SCOPE_IDENTITY();
    END ELSE BEGIN
        -- Exists, update values
        UPDATE [dbo].[Alert] SET
             [RawTime]       = @RawTime
            ,[MSec]          = @MSec
            ,[ServiceId]     = @ServiceId
            ,[ServiceName]   = @ServiceName
            ,[ServiceRole]   = @ServiceRole
            ,[ServiceAddress]= @ServiceAddress
            ,[LogName]       = @LogName
            ,[Source]        = @Source
            ,[Level]         = @Level
            ,[Message]       = @Message
            ,[IsSystemAlert] = @IsSystemAlert
            ,[ExceptionText] = @ExceptionText
        WHERE [Id] = @IdNew;
    END
    RETURN @IdNew;
END');
EXEC(N'-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE OR ALTER PROCEDURE [dbo].[usp_alert_query_by_serviceid]
    @ServiceId VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calculate Unix timestamp for 24 hours ago
    DECLARE @TwentyFourHoursAgo INT;
    SET @TwentyFourHoursAgo = DATEDIFF(SECOND, ''1970-01-01'', DATEADD(HOUR, -24, GETUTCDATE()));
    
    -- Query alerts from last 24 hours
    SELECT 
        Id,
        RawTime,
        MSec,
        ServiceId,
        ServiceName,
        ServiceRole,
        ServiceAddress,
        LogName,
        Source,
        Level,
        Message,
        ExceptionText,
        IsSystemAlert
    FROM dbo.Alert
    WHERE ServiceId = @ServiceId
        AND RawTime >= @TwentyFourHoursAgo
    ORDER BY RawTime DESC, MSec DESC;
END;');
EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_select]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  [Id]
           ,[RawTime]
           ,[MSec]
           ,[ServiceId]
           ,[ServiceName]
           ,[ServiceRole]
           ,[ServiceAddress]
           ,[LogName]
           ,[Source]
           ,[Level]
           ,[Message]
           ,[ExceptionText]
           ,[IsSystemAlert]
    FROM [dbo].[Alert]
    WHERE @Id = -1 OR [Id] = @Id
END');
EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[usp_alert_select_since]
(
    @Duration int
)
AS
BEGIN
    SET NOCOUNT ON;
    -- Get current time in EPOCH
    DECLARE @curRawTime INT = DATEDIFF(SECOND, ''1970-01-01'', GETUTCDATE());


    SELECT  [Id]
           ,[RawTime]
           ,[MSec]
           ,[ServiceId]
           ,[ServiceName]
           ,[ServiceRole]
           ,[ServiceAddress]
           ,[LogName]
           ,[Source]
           ,[Level]
           ,[Message]
           ,[ExceptionText]
           ,[IsSystemAlert]
    FROM [dbo].[Alert]
    WHERE [RawTime] >= (@curRawTime - @Duration);
END');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
