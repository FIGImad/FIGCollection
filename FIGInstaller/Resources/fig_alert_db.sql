USE FIGAlert
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION [dbo].[fn_ConvertLocalDateTimeToUTCRawTime]
(
    @localTime      DATETIME2(3),
    @LocalTimeZone  NVARCHAR(50)
)
RETURNS BIGINT
AS
BEGIN
    DECLARE @UtcDateTimeOffset DATETIMEOFFSET;

    -- local -> UTC (handles DST)
    SET @UtcDateTimeOffset =
        ( @localTime AT TIME ZONE @LocalTimeZone ) AT TIME ZONE 'UTC';

    RETURN DATEDIFF_BIG(SECOND, CONVERT(DATETIME2(0), '19700101'), CONVERT(DATETIME2(0), @UtcDateTimeOffset));
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION [dbo].[fn_ConvertUTCRawTimeToLocalDateTime]
(
    @EpochTime BIGINT,
    @TimeZone  NVARCHAR(50)
)
RETURNS DATETIME2(3)
AS
BEGIN
    DECLARE @UtcDateTime2 DATETIME2(3) = DATEADD(SECOND, @EpochTime, CONVERT(DATETIME2(0), '19700101'));
    DECLARE @LocalDto DATETIMEOFFSET;

    SET @LocalDto = (@UtcDateTime2 AT TIME ZONE 'UTC') AT TIME ZONE @TimeZone;

    RETURN CONVERT(DATETIME2(3), @LocalDto);
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE  FUNCTION [dbo].[fn_GetServerTimeZone] ()
RETURNS NVARCHAR(50)
AS
BEGIN
	DECLARE @TimeZone NVARCHAR(50);
	EXEC MASTER.dbo.xp_regread 'HKEY_LOCAL_MACHINE', 
							   'SYSTEM\CurrentControlSet\Control\TimeZoneInformation', 
							   'TimeZoneKeyName', 
							   @TimeZone OUTPUT;

	RETURN @TimeZone;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Alert](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RawTime] [int] NOT NULL,
	[MSec] [int] NOT NULL,
	[ServiceId] [varchar](50) NOT NULL,
	[ServiceName] [varchar](256) NOT NULL,
	[ServiceRole] [int] NOT NULL,
	[ServiceAddress] [varchar](256) NOT NULL,
	[LogName] [varchar](256) NOT NULL,
	[Source] [varchar](50) NOT NULL,
	[Level] [varchar](20) NOT NULL,
	[Message] [varchar](max) NOT NULL,
	[ExceptionText] [varchar](max) NULL,
	[MatchedRuleId] [int] NULL,
	[FriendlyMessage] [varchar](max) NULL,
	[SentAt] [datetime2](7) NULL,
	[IgnoredAt] [datetime2](7) NULL,
	[IgnoreReason] [varchar](200) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE VIEW [dbo].[VAlert]
AS
SELECT [Id]
      ,[RawTime]
      ,[MSec]
      ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([RawTime], 'UTC') AS [AlertTimeUTC]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([RawTime], 'Eastern Standard Time') AS [AlertTimeEST]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([RawTime], 'Arabian Standard Time') AS [AlertTimeDubai]
      ,[ServiceId]
      ,[ServiceName]
      ,[ServiceRole]
      ,[ServiceAddress]
      ,[LogName]
      ,[Source]
      ,[Level]
      ,[Message]
      ,[ExceptionText]
      ,[MatchedRuleId]
      ,[FriendlyMessage]
      ,[SentAt]
  FROM [dbo].[Alert]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlertMethod](
	[Id] [int] NOT NULL,
	[Method] [varchar](20) NOT NULL,
	[Description] [varchar](100) NOT NULL,
 CONSTRAINT [PK_AlertMethod] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlertRule](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleKey] [varchar](100) NOT NULL,
	[Severity] [int] NOT NULL,
	[Enabled] [bit] NOT NULL,
	[RuleJson] [nvarchar](max) NOT NULL,
	[FriendlyMessage] [varchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AlertRule] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlertRuleOperator](
	[OperatorCode] [varchar](25) NOT NULL,
	[OperatorType] [varchar](25) NOT NULL,
	[Description] [varchar](256) NOT NULL,
 CONSTRAINT [PK_AlertRuleOperator] PRIMARY KEY CLUSTERED 
(
	[OperatorCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlertType](
	[Type] [char](3) NOT NULL,
	[Description] [varchar](100) NULL,
 CONSTRAINT [PK_AlertType] PRIMARY KEY CLUSTERED 
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Event](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventType] [varchar](50) NOT NULL,
	[EventId] [varchar](100) NOT NULL,
	[Message] [varchar](256) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
 CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_Event_Name] UNIQUE NONCLUSTERED 
(
	[EventType] ASC,
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Recipient](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Alias] [varchar](256) NOT NULL,
	[Name] [varchar](256) NOT NULL,
	[Email] [varchar](256) NOT NULL,
	[PushoverKey] [varchar](256) NULL,
 CONSTRAINT [PK_Recipient] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RecipientSubscription](
	[RecipientId] [int] NOT NULL,
	[AlertRuleId] [int] NOT NULL,
	[AlertMethods] [int] NOT NULL
) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Alert_DispatchPending] ON [dbo].[Alert]
(
	[RawTime] ASC,
	[Id] ASC,
	[MatchedRuleId] ASC
)
WHERE ([SentAt] IS NULL AND [IgnoredAt] IS NULL AND [MatchedRuleId] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AlertRule] ADD  CONSTRAINT [DF__AlertClas__Enabl__44FF419A]  DEFAULT ((1)) FOR [Enabled]
GO
ALTER TABLE [dbo].[AlertRule] ADD  CONSTRAINT [DF__AlertClas__Creat__45F365D3]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[AlertRuleOperator]  WITH CHECK ADD  CONSTRAINT [CK_RuleOperator_OperatorType] CHECK  (([OperatorType]='COMPARE' OR [OperatorType]='GROUP'))
GO
ALTER TABLE [dbo].[AlertRuleOperator] CHECK CONSTRAINT [CK_RuleOperator_OperatorType]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alert_delete]
    @Id int
AS
BEGIN
    DELETE [dbo].[Alert] WHERE [Id] = @Id;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_alert_get_pending]
(
	@LookbackMinutes int = 60
)
AS
BEGIN
	SET NOCOUNT ON;

	IF (@LookbackMinutes IS NULL OR @LookbackMinutes <= 0)
		SET @LookbackMinutes = 60;

	DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();
	DECLARE @CutoffRawTime int = DATEDIFF(SECOND, CONVERT(datetime2(0), '19700101'), DATEADD(MINUTE, -@LookbackMinutes, @NowUtc));

	UPDATE [dbo].[Alert]
	SET    [IgnoredAt]    = @NowUtc,
	       [IgnoreReason] = CONCAT('Older than pending alert lookback window of ', @LookbackMinutes, ' minutes')
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
		a.[MatchedRuleId]    AS RuleId
	FROM  [dbo].[Alert]                  a
	INNER JOIN [dbo].[RecipientSubscription] rs ON rs.[AlertRuleId] = a.[MatchedRuleId]
	INNER JOIN [dbo].[Recipient]             r  ON r.[Id]           = rs.[RecipientId]
	WHERE a.[SentAt]          IS NULL
	  AND a.[IgnoredAt]       IS NULL
	  AND a.[RawTime]         >= @CutoffRawTime
	  AND a.[MatchedRuleId]   IS NOT NULL
	  AND a.[FriendlyMessage] IS NOT NULL
	  AND (
			(rs.[AlertMethods] & 1 = 1 AND r.[Email]       <> '')
		 OR (rs.[AlertMethods] & 2 = 2 AND r.[PushoverKey] IS NOT NULL AND r.[PushoverKey] <> '')
		  )
	ORDER BY a.[Id];
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_alert_mark_sent]
(
	@Id int
)
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [dbo].[Alert]
	SET    [SentAt] = SYSUTCDATETIME()
	WHERE  [Id] = @Id
	  AND  [IgnoredAt] IS NULL;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE PROCEDURE [dbo].[usp_alert_query_by_serviceid]
    @ServiceId VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calculate Unix timestamp for 24 hours ago
    DECLARE @TwentyFourHoursAgo INT;
    SET @TwentyFourHoursAgo = DATEDIFF(SECOND, '1970-01-01', DATEADD(HOUR, -24, GETUTCDATE()));
    
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
        ExceptionText
    FROM dbo.Alert
    WHERE ServiceId = @ServiceId
        AND RawTime >= @TwentyFourHoursAgo
    ORDER BY RawTime DESC, MSec DESC;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alert_select]
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
    FROM [dbo].[Alert]
    WHERE @Id = -1 OR [Id] = @Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alert_select_since]
(
    @Duration int
)
AS
BEGIN
    SET NOCOUNT ON;
    -- Get current time in EPOCH
    DECLARE @curRawTime INT = DATEDIFF(SECOND, '1970-01-01', GETUTCDATE());


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
    FROM [dbo].[Alert]
    WHERE [RawTime] >= (@curRawTime - @Duration);
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_alert_update_match]
(
	@Id              int,
	@MatchedRuleId   int,
	@FriendlyMessage varchar(max)
)
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [dbo].[Alert]
	SET    [MatchedRuleId]   = @MatchedRuleId,
		   [FriendlyMessage] = @FriendlyMessage
	WHERE  [Id] = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alert_upsert]
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
    @ExceptionText  varchar(max) = NULL
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
            ,[ExceptionText] = @ExceptionText
        WHERE [Id] = @IdNew;
    END
    RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alertmethod_select]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id],[Method],[Description] FROM [dbo].[AlertMethod]
    
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alertrule_delete]
    @Id int
AS
BEGIN
    DELETE [dbo].[AlertRule] WHERE [Id] = @Id;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alertrule_select]
(
    @Id int
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [Id]
          ,[RuleKey]
          ,[Severity]
          ,[Enabled]
          ,[RuleJson]
          ,[FriendlyMessage]
          ,[CreatedAt]
    FROM [dbo].[AlertRule]
    WHERE @Id = -1 OR [Id] = @Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alertrule_upsert]
(
    @IdNew    int OUTPUT,
    @Id       int,
    @RuleKey  varchar(100),
    @Severity int,
    @Enabled  bit,
    @RuleJson nvarchar(max),
    @FriendlyMessage nvarchar(max),
    @CreatedAt datetime2(7)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @IdNew = [Id] FROM [dbo].[AlertRule] WHERE [Id] = @Id;
    IF (@@ROWCOUNT = 0)
    BEGIN
        INSERT INTO [dbo].[AlertRule] (
             [RuleKey]
            ,[Severity]
            ,[Enabled]
            ,[RuleJson]
            ,[FriendlyMessage]
            ,[CreatedAt]
        )
        VALUES (
             @RuleKey
            ,@Severity
            ,@Enabled
            ,@RuleJson
            ,@FriendlyMessage
            ,@CreatedAt
        )
        SET @IdNew = SCOPE_IDENTITY();
    END ELSE BEGIN
        UPDATE [dbo].[AlertRule] SET
             [RuleKey]   = @RuleKey
            ,[Severity]  = @Severity
            ,[Enabled]   = @Enabled
            ,[RuleJson]  = @RuleJson
            ,[FriendlyMessage] = @FriendlyMessage
            ,[CreatedAt] = @CreatedAt
        WHERE [Id] = @IdNew;
    END
    RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alertruleoperator_select]
(
    @OperatorCode varchar(25)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [OperatorCode]
          ,[OperatorType]
          ,[Description]
    FROM [dbo].[AlertRuleOperator]
    WHERE @OperatorCode = '' OR [OperatorCode] = @OperatorCode
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_alerttype_select]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Type],[Description] FROM [dbo].[AlertType]
    
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_event_cleanup]
(
	@EventType varchar(50),
	@DurationInMin int
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @RefTime datetime;
    SET @RefTime = DATEADD(MINUTE, -@DurationInMin, GETDATE());
	DELETE FROM [dbo].[Event] WHERE [EventType] = @EventType AND [TimeStamp] < @RefTime;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_query]
(
	@EventType varchar(50),
	@EventId varchar(100)
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[EventType]
		  ,[EventId]
		  ,[Message]
		  ,[TimeStamp]
		FROM [dbo].[Event]
		WHERE [EventType] = @EventType AND [EventId] = @EventId;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_select]
(
	@Id int
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[EventType]
		  ,[EventId]
		  ,[Message]
		  ,[TimeStamp]
		FROM [dbo].[Event]
		WHERE @Id = -1 OR @Id = [Id]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_select_active]
(
	@Seconds int
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @TimeSince datetime = DATEADD(SECOND, -@Seconds, GETDATE());

	SELECT [Id]
		  ,[EventType]
		  ,[EventId]
		  ,[Message]
		  ,[TimeStamp]
		FROM [dbo].[Event]
		WHERE [TimeStamp] > @TimeSince;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_event_touch]
(
	@EventType varchar(50),
	@EventId varchar(100)
)

AS
BEGIN
	SET NOCOUNT ON;
	IF (EXISTS (SELECT 1 FROM [Event] WHERE [EventType] = @EventType AND [EventId] = @EventId))
	BEGIN
		UPDATE [Event] SET [TimeStamp] = getdate() WHERE [EventType] = @EventType AND [EventId] = @EventId;
	END ELSE BEGIN
		INSERT INTO [Event] ([EventType], [EventId], [Message], [TimeStamp]) 
				VALUES(@EventType, @EventId, 'Added By usp_event_touch procedure', getdate());
	END

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipient_delete]
	@Id int
AS
BEGIN
	Delete [dbo].[Recipient] Where Id = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipient_method_delete]
(
	@RecipientId int,
	@AlertMethod varchar(20)
)
AS
BEGIN
	DELETE FROM [RecipientMethod] 
		WHERE [RecipientId] = @RecipientId AND (@AlertMethod = '' OR @AlertMethod = [AlertMethod]);
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipient_method_select]
(
	@RecipientId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [RecipientId]
		  ,[AlertMethod]
          ,[Config]
        FROM [dbo].[RecipientMethod]
        WHERE @RecipientId = -1 OR [RecipientId] = @RecipientId
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipient_method_upsert]
(
	@IdNew int OUTPUT,
	@RecipientId int,
	@AlertMethod varchar(20),
	@Config varchar(4000)
)
AS
BEGIN
	SET NOCOUNT ON;
	SET @AlertMethod = upper(trim(@AlertMethod));
	IF (NOT EXISTS(SELECT 1 FROM [RecipientMethod] WHERE @RecipientId = [RecipientId] AND @AlertMethod = [AlertMethod]))
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [RecipientMethod] (
			 [RecipientId]
			,[AlertMethod]
			,[Config]
			)
		VALUES (
			 @RecipientId
			,@AlertMethod
			,@Config
			)

	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [RecipientMethod] SET [Config] = @Config WHERE @RecipientId = [RecipientId] AND @AlertMethod = [AlertMethod];
	END
	RETURN 1;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipient_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [Id]
		  ,[Alias]
          ,[Name]
		  ,[Email]
		  ,[PushoverKey]
        FROM [dbo].[Recipient]
        WHERE @Id = -1 OR Id = @Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipient_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@Alias varchar(256),
	@Name varchar(256),
    @Email varchar(256),
    @PushoverKey varchar(256)
)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT @IdNew = [Id] FROM [Recipient] WHERE [Id] = @Id;
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Recipient] (
			 [Alias]
			,[Name]
            ,[Email]
            ,[PushoverKey]
			)
		VALUES (
			 @Alias
			,@Name
            ,@Email
            ,@PushoverKey
			)
		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [Recipient] 
            SET [Name] = @Name, 
                [Alias] = @Alias,
                [Email] = @Email,
                [PushoverKey] = @PushoverKey
            WHERE [Id] = @IdNew;
	END
	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipientsubscription_delete]
	@RecipientId int,
	@AlertRuleId int
AS
BEGIN
	Delete [dbo].[RecipientSubscription] Where [RecipientId] = @RecipientId AND [AlertRuleId] = @AlertRuleId;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipientsubscription_query]
(
	@RecipientId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [RecipientId]
		  ,[AlertRuleId]
		  ,[AlertMethods]
		FROM [dbo].[RecipientSubscription]
        WHERE @RecipientId = -1 OR [RecipientId] = @RecipientId
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_recipientsubscription_upsert]
(
	@IdNew int OUTPUT,
	@RecipientId int,
	@AlertRuleId int,
	@AlertMethods int
)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 1 FROM [RecipientSubscription] WHERE [RecipientId] = @RecipientId AND [AlertRuleId] = @AlertRuleId;
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [RecipientSubscription] (
			 [RecipientId]
			,[AlertRuleId]
            ,[AlertMethods]
			)
		VALUES (
			 @RecipientId
			,@AlertRuleId
            ,@AlertMethods
			)
		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [RecipientSubscription] 
            SET [AlertMethods] = @AlertMethods
            WHERE [RecipientId] = @RecipientId AND [AlertRuleId] = @AlertRuleId;
	END
	SET @IdNew = 1;
END

GO
INSERT [dbo].[AlertMethod] ([Id], [Method], [Description]) VALUES (0, N'NONE', N'No Method Configured')
GO
INSERT [dbo].[AlertMethod] ([Id], [Method], [Description]) VALUES (1, N'EMAIL', N'Send to Email Account')
GO
INSERT [dbo].[AlertMethod] ([Id], [Method], [Description]) VALUES (2, N'PUSHOVER', N'Send to PushOver account')
GO
SET IDENTITY_INSERT [dbo].[AlertRule] ON 
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (1, N'BROKER_IBKR_CONN', 8, 1, N'{"operator":"AND","conditions":[{"field":"ServiceName","operator":"LIKE","value":"%Broker%","caseSensitive":false},{"operator":"OR","conditions":[{"field":"Source","operator":"LIKE","value":"%IBKRProvider.Service.Provider%","caseSensitive":false},{"field":"Source","operator":"LIKE","value":"%PositionTrackerService%","caseSensitive":false},{"field":"Message","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%BrokerConnection%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false}]}]}', N'BOT/Broker Service at {{service_name}} lost connection with IBKR Trade Station. Manual intervention is required ', CAST(N'2026-06-09T05:37:42.1369081' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (2, N'PING_ERR', 8, 1, N'{"operator":"AND","conditions":[{"field":"ServiceName","operator":"LIKE","value":"%Admin%","caseSensitive":false},{"field":"Message","operator":"LIKE","value":"%Ping failed for service%","caseSensitive":false}]}', N'Could not Ping "{{service_name_from_message}}" - ServiceId: "{{service_id_from_message}}"', CAST(N'2026-06-09T15:21:06.7044008' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (3, N'PRICESYNC_IBKR_CONN', 8, 1, N'{"operator":"AND","conditions":[{"field":"ServiceName","operator":"LIKE","value":"%Price Sync%","caseSensitive":false},{"operator":"OR","conditions":[{"field":"Source","operator":"LIKE","value":"%PriceSyncService%","caseSensitive":false},{"field":"Message","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%BrokerConnection%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false}]}]}', N'Price Sync Service at {{service_name}} lost connection with IBKR Trade Station. Manual intervention is required ', CAST(N'2026-06-09T05:37:42.1369081' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (4, N'CRITICAL_ERROR', 10, 1, N'{"operator":"OR","conditions":[{"field":"Level","operator":"LIKE","value":"%Critical%","caseSensitive":false},{"field":"Level","operator":"LIKE","value":"%FTL%","caseSensitive":false}]}', N'Critical Error: {{message}}', CAST(N'2026-06-10T05:42:51.6415508' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (5, N'ALERT', 1, 1, N'{"operator":"OR","conditions":[{"field":"Level","operator":"LIKE","value":"%ALERT%","caseSensitive":false},{"field":"Level","operator":"LIKE","value":"%ALT%","caseSensitive":false}]}', N'Alert: {{message}}', CAST(N'2026-06-10T05:44:50.6942033' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (6, N'BROKER_IBKR_CONN2', 8, 1, N'{"operator":"AND","conditions":[{"field":"Source","operator":"LIKE","value":"%IBKR%","caseSensitive":false},{"operator":"OR","conditions":[{"field":"Message","operator":"LIKE","value":"%connection error%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%BrokerConnectionException%","caseSensitive":false}]}]}', N'BOT/Broker Service at {{service_name}} lost connection with IBKR Trade Station. Manual intervention is required ', CAST(N'2026-06-11T07:13:52.7203575' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[AlertRule] OFF
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'AND', N'GROUP', N'All child conditions must match')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'CONTAINS', N'COMPARE', N'Field value contains the supplied text')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'ENDS_WITH', N'COMPARE', N'Field value ends with the supplied text')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'EQUALS', N'COMPARE', N'Field value equals the supplied value')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'GREATER_OR_EQUAL', N'COMPARE', N'Field value is greater than or equal to the supplied value')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'GREATER_THAN', N'COMPARE', N'Field value is greater than the supplied value')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'IN', N'COMPARE', N'Field value exists in the supplied list')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'IS_NOT_NULL', N'COMPARE', N'Field value is not null')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'IS_NULL', N'COMPARE', N'Field value is null')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'LESS_OR_EQUAL', N'COMPARE', N'Field value is less than or equal to the supplied value')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'LESS_THAN', N'COMPARE', N'Field value is less than the supplied value')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'LIKE', N'COMPARE', N'Field value matches a SQL LIKE pattern')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'NOT', N'GROUP', N'Negates a condition or group')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'NOT_EQUALS', N'COMPARE', N'Field value does not equal the supplied value')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'NOT_IN', N'COMPARE', N'Field value does not exist in the supplied list')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'NOT_LIKE', N'COMPARE', N'Field value does not match a SQL LIKE pattern')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'OR', N'GROUP', N'At least one child condition must match')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'REGEX', N'COMPARE', N'Field value matches a regular expression')
GO
INSERT [dbo].[AlertRuleOperator] ([OperatorCode], [OperatorType], [Description]) VALUES (N'STARTS_WITH', N'COMPARE', N'Field value starts with the supplied text')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'ALT', N'Alert')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'DBG', N'Debug')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'ERR', N'Error')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'FTL', N'Critical Error')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'INF', N'Information')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'VRN', N'Verbose')
GO
INSERT [dbo].[AlertType] ([Type], [Description]) VALUES (N'WRN', N'Warning')
GO
SET IDENTITY_INSERT [dbo].[Recipient] ON 
GO
INSERT [dbo].[Recipient] ([Id], [Alias], [Name], [Email], [PushoverKey]) VALUES (1, N'Imad', N'Imad Ansari', N'imad.ansari@gmail.com', N'ux67uaouowtjgx1vcwjvwexck4wnnu')
GO
INSERT [dbo].[Recipient] ([Id], [Alias], [Name], [Email], [PushoverKey]) VALUES (3, N'Sultan', N'Sultan AlTaher', N'', N'')
GO
SET IDENTITY_INSERT [dbo].[Recipient] OFF
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (3, 1, 1)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (3, 3, 1)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (3, 6, 1)
GO
