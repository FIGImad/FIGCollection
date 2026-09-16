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

-- ================================================================================
-- Author:		Imad Ansari
-- Create date: Oct 26, 2024
-- Description:	This function returns DateTime in local time. To see all supported
--              Timezones, run the following query:
--              SELECT * FROM sys.time_zone_info;
-- ================================================================================
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
	[Source] [varchar](256) NOT NULL,
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
CREATE TABLE [dbo].[AlertImmediateSource](
	[Source] [varchar](50) NOT NULL,
	[Enabled] [bit] NOT NULL,
 CONSTRAINT [PK_AlertImmediateSource] PRIMARY KEY CLUSTERED 
(
	[Source] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
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
	[Enabled] [bit] NOT NULL,
	[TimeZoneId] [nvarchar](128) NOT NULL,
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
INSERT [dbo].[AlertImmediateSource] ([Source], [Enabled]) VALUES (N'SIGNAL', 1)
GO
INSERT [dbo].[AlertMethod] ([Id], [Method], [Description]) VALUES (0, N'NONE', N'No Method Configured')
GO
INSERT [dbo].[AlertMethod] ([Id], [Method], [Description]) VALUES (1, N'EMAIL', N'Send to Email Account')
GO
INSERT [dbo].[AlertMethod] ([Id], [Method], [Description]) VALUES (2, N'PUSHOVER', N'Send to PushOver account')
GO
SET IDENTITY_INSERT [dbo].[AlertRule] ON 
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (1, N'BROKER_IBKR_CONN', 8, 1, N'{"operator":"AND","conditions":[{"field":"ServiceName","operator":"LIKE","value":"%Broker%","caseSensitive":false},{"operator":"OR","conditions":[{"field":"Source","operator":"LIKE","value":"%IBKRProvider.Service.Provider%","caseSensitive":false},{"field":"Source","operator":"LIKE","value":"%PositionTrackerService%","caseSensitive":false},{"field":"Message","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%BrokerConnection%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false}]}]}', N'<b><i>LOCAL SERVER</i> - {{service_name}}</b><font color="#FF0000"><b>IBKR CONNECTION ISSUE</b></font>Broker connectivity requires attention. Check the IBKR connection and trading session.', CAST(N'2026-06-09T05:37:42.1366667' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (2, N'PING_ERR', 8, 1, N'{"operator":"AND","conditions":[{"field":"ServiceName","operator":"LIKE","value":"%Admin%","caseSensitive":false},{"field":"Message","operator":"LIKE","value":"%Ping failed for service%","caseSensitive":false}]}', N'<b><i>LOCAL SERVER</i> - {{service_name_from_message}}</b><font color="#FF0000"><b>SERVICE PING FAILED</b></font>The health-check ping failed. Check whether this service is running and reachable.', CAST(N'2026-06-09T15:21:06.7033333' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (3, N'PRICESYNC_IBKR_CONN', 8, 1, N'{"operator":"AND","conditions":[{"field":"ServiceName","operator":"LIKE","value":"%Price Sync%","caseSensitive":false},{"operator":"OR","conditions":[{"field":"Source","operator":"LIKE","value":"%PriceSyncService%","caseSensitive":false},{"field":"Message","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%BrokerConnection%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%Failed to connect%","caseSensitive":false}]}]}', N'<b><i>LOCAL SERVER</i> - {{service_name}}</b><font color="#FF0000"><b>PRICE SYNC - IBKR CONNECTION ISSUE</b></font>Price synchronization may be interrupted. Check the IBKR connection and trading session.', CAST(N'2026-06-09T05:37:42.1366667' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (4, N'CRITICAL_ERROR', 10, 1, N'{"operator":"OR","conditions":[{"field":"Level","operator":"LIKE","value":"%Critical%","caseSensitive":false},{"field":"Level","operator":"LIKE","value":"%FTL%","caseSensitive":false}]}', N'<b><i>LOCAL SERVER</i> - {{service_name}}</b><font color="#FF0000"><b>CRITICAL ERROR</b></font>Immediate review required.{{message}}', CAST(N'2026-06-10T05:42:51.6400000' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (5, N'ALERT', 1, 1, N'{"operator":"OR","conditions":[{"field":"Level","operator":"LIKE","value":"%ALERT%","caseSensitive":false},{"field":"Level","operator":"LIKE","value":"%ALT%","caseSensitive":false}]}', N'<b><i>LOCAL SERVER</i> - {{service_name}}</b><b>SERVICE ALERT</b>{{message}}', CAST(N'2026-06-10T05:44:50.6933333' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (6, N'BROKER_IBKR_CONN2', 8, 1, N'{"operator":"AND","conditions":[{"field":"Source","operator":"LIKE","value":"%IBKR%","caseSensitive":false},{"operator":"OR","conditions":[{"field":"Message","operator":"LIKE","value":"%connection error%","caseSensitive":false},{"field":"ExceptionText","operator":"LIKE","value":"%BrokerConnectionException%","caseSensitive":false}]}]}', N'<b><i>LOCAL SERVER</i> - {{service_name}}</b><font color="#FF0000"><b>IBKR CONNECTION ISSUE</b></font>Broker connectivity requires attention. Check the IBKR connection and trading session.', CAST(N'2026-06-11T07:13:52.7200000' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (7, N'IBKR Response timeout', 10, 1, N'{"operator":"AND","conditions":[{"field":"ExceptionText","operator":"LIKE","value":"%ResponseWaitException%IBKRProvider%","caseSensitive":false}]}', N'<b><i>LOCAL SERVER</i> - {{service_name}}</b><font color="#FF0000"><b>IBKR RESPONSE TIMEOUT</b></font>An IBKR request did not receive a response within the expected time. Check connectivity and IBKR responsiveness.{{message}}', CAST(N'2026-08-26T19:27:20.1433333' AS DateTime2))
GO
INSERT [dbo].[AlertRule] ([Id], [RuleKey], [Severity], [Enabled], [RuleJson], [FriendlyMessage], [CreatedAt]) VALUES (8, N'SIGNAL ALERT', 5, 1, N'{"operator":"OR","conditions":[{"field":"Level","operator":"LIKE","value":"%ALERT%","caseSensitive":false},{"field":"Level","operator":"LIKE","value":"%ALT%","caseSensitive":false},{"field":"Source","operator":"EQUALS","value":"SIGNAL","caseSensitive":false}]}', N'<b><i>LOCAL SERVER</i> - SIGNAL ALERT</b>{{message}}', CAST(N'2026-09-15T15:56:06.8266667' AS DateTime2))
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
SET IDENTITY_INSERT [dbo].[Event] ON 
GO
INSERT [dbo].[Event] ([Id], [EventType], [EventId], [Message], [TimeStamp]) VALUES (1, N'ALERT_IMMEDIATE', N'-1', N'Immediate alert available', CAST(N'2026-09-16T15:29:59.027' AS DateTime))
GO
SET IDENTITY_INSERT [dbo].[Event] OFF
GO
SET IDENTITY_INSERT [dbo].[Recipient] ON 
GO
INSERT [dbo].[Recipient] ([Id], [Alias], [Name], [Email], [PushoverKey], [Enabled], [TimeZoneId]) VALUES (1, N'Imad', N'Imad Ansari', N'imad.ansari@gmail.com', N'ux67uaouowtjgx1vcwjvwexck4wnnu', 1, N'UTC')
GO
SET IDENTITY_INSERT [dbo].[Recipient] OFF
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (3, 1, 1)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (3, 3, 1)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (3, 6, 1)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 1, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 3, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 2, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 4, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (4, 8, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 6, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 7, 2)
GO
INSERT [dbo].[RecipientSubscription] ([RecipientId], [AlertRuleId], [AlertMethods]) VALUES (1, 8, 2)
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
SET ANSI_PADDING ON
GO
ALTER TABLE [dbo].[Event] ADD  CONSTRAINT [IX_Event_Name] UNIQUE NONCLUSTERED 
(
	[EventType] ASC,
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AlertImmediateSource] ADD  CONSTRAINT [DF_AlertImmediateSource_Enabled]  DEFAULT ((1)) FOR [Enabled]
GO
ALTER TABLE [dbo].[AlertRule] ADD  CONSTRAINT [DF__AlertClas__Enabl__44FF419A]  DEFAULT ((1)) FOR [Enabled]
GO
ALTER TABLE [dbo].[AlertRule] ADD  CONSTRAINT [DF__AlertClas__Creat__45F365D3]  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Recipient] ADD  CONSTRAINT [DF_Recipient_Enabled]  DEFAULT ((1)) FOR [Enabled]
GO
ALTER TABLE [dbo].[Recipient] ADD  CONSTRAINT [DF_Recipient_TimeZoneId]  DEFAULT (N'UTC') FOR [TimeZoneId]
GO
ALTER TABLE [dbo].[AlertRuleOperator]  WITH CHECK ADD  CONSTRAINT [CK_RuleOperator_OperatorType] CHECK  (([OperatorType]='COMPARE' OR [OperatorType]='GROUP'))
GO
ALTER TABLE [dbo].[AlertRuleOperator] CHECK CONSTRAINT [CK_RuleOperator_OperatorType]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-0d7fa3ee-035c-470d-82f3-3440715c3cd8] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-0d7fa3ee-035c-470d-82f3-3440715c3cd8]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-0d7fa3ee-035c-470d-82f3-3440715c3cd8] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-0d7fa3ee-035c-470d-82f3-3440715c3cd8') > 0)   DROP SERVICE [SqlQueryNotificationService-0d7fa3ee-035c-470d-82f3-3440715c3cd8]; if (OBJECT_ID('SqlQueryNotificationService-0d7fa3ee-035c-470d-82f3-3440715c3cd8', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-0d7fa3ee-035c-470d-82f3-3440715c3cd8]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-0d7fa3ee-035c-470d-82f3-3440715c3cd8]; END COMMIT TRANSACTION; END
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
CREATE   PROCEDURE [dbo].[usp_alert_get_pending]
(
	@LookbackMinutes int = 30
)
AS
BEGIN
	SET NOCOUNT ON;

	IF (@LookbackMinutes IS NULL OR @LookbackMinutes <= 0)
		SET @LookbackMinutes = 30;

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
		a.[MatchedRuleId]    AS RuleId,
		r.[Enabled] AS RecipientEnabled,
        a.[RawTime], a.[MSec], r.[TimeZoneId],
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AlertImmediateSource src WHERE src.[Source] = a.[Source] AND src.[Enabled] = 1) THEN 1 ELSE 0 END AS bit) AS IsImmediate
	FROM  [dbo].[Alert]                  a
	INNER JOIN [dbo].[RecipientSubscription] rs ON rs.[AlertRuleId] = a.[MatchedRuleId]
	INNER JOIN [dbo].[Recipient]             r  ON r.[Id]           = rs.[RecipientId]
	WHERE r.[Enabled] = 1 AND a.[SentAt]          IS NULL
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

CREATE   PROCEDURE [dbo].[usp_alert_get_pending_immediate]
(
    @MaxRec int = 100,
    @LookbackMinutes int = 30
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxRec IS NULL OR @MaxRec <= 0
        SET @MaxRec = 100;


	IF (@LookbackMinutes IS NULL OR @LookbackMinutes <= 0)
		SET @LookbackMinutes = 30;

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
                AND ((eligibleSubscription.AlertMethods & 1 = 1 AND eligibleRecipient.Email <> '')
                  OR (eligibleSubscription.AlertMethods & 2 = 2 AND eligibleRecipient.PushoverKey IS NOT NULL
                      AND eligibleRecipient.PushoverKey <> ''))
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
                    AND r.[Email] <> '')

             OR (rs.[AlertMethods] & 2 = 2
                    AND r.[PushoverKey] IS NOT NULL
                    AND r.[PushoverKey] <> '')
           )

    ORDER BY
        a.[RawTime],
        a.[MSec],
        a.[Id],
        r.[Id];
END;
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
CREATE   PROCEDURE [dbo].[usp_recipient_delete]
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

CREATE   PROCEDURE [dbo].[usp_recipient_select]
    @Id int
AS
BEGIN
    SET NOCOUNT ON;
    -- Keep disabled recipients visible so administrators can re-enable them.
    SELECT [Id], [Alias], [Name], [Email], [PushoverKey], [Enabled], [TimeZoneId]
    FROM [dbo].[Recipient]
    WHERE @Id = -1 OR [Id] = @Id;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   PROCEDURE [dbo].[usp_recipient_upsert]
    @IdNew int OUTPUT,
    @Id int,
    @Alias varchar(256),
    @Name varchar(256),
    @Email varchar(256),
    @PushoverKey varchar(256),
    @Enabled bit = NULL,
    @TimeZoneId nvarchar(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Old callers may omit Enabled: enable inserts, preserve existing updates.
    SET @IdNew = NULL;
    SELECT @IdNew = [Id] FROM [dbo].[Recipient] WHERE [Id] = @Id;
    IF @IdNew IS NULL
    BEGIN
        INSERT INTO [dbo].[Recipient] ([Alias], [Name], [Email], [PushoverKey], [Enabled], [TimeZoneId])
        VALUES (@Alias, @Name, @Email, @PushoverKey, COALESCE(@Enabled, CONVERT(bit, 1)), COALESCE(@TimeZoneId, N'UTC'));
        SET @IdNew = CONVERT(int, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE [dbo].[Recipient]
        SET [Alias] = @Alias,
            [Name] = @Name,
            [Email] = @Email,
            [PushoverKey] = @PushoverKey,
            [Enabled] = COALESCE(@Enabled, [Enabled]),
            [TimeZoneId] = COALESCE(@TimeZoneId, [TimeZoneId])
        WHERE [Id] = @IdNew;
    END;
    RETURN @IdNew;
END;
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE   TRIGGER [dbo].[trigAlertImmediateOnChange]
ON [dbo].[Alert]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /*
        Wake FIGAlertSvc only if at least one affected Alert is:

          1. From an immediate source
          2. Matched to a rule
          3. Has a FriendlyMessage
          4. Has not already been sent
          5. Has not been ignored
    */

    IF NOT EXISTS
    (
        SELECT 1
        FROM inserted AS i
        INNER JOIN dbo.AlertImmediateSource AS s
            ON s.[Source] = i.[Source]
           AND s.[Enabled] = 1
        WHERE i.[MatchedRuleId] IS NOT NULL
          AND i.[FriendlyMessage] IS NOT NULL
          AND i.[SentAt] IS NULL
          AND i.[IgnoredAt] IS NULL
    )
        RETURN;


    DECLARE @Now datetime = GETDATE();

    UPDATE dbo.[Event]
       SET [TimeStamp] = @Now,
           [Message]   = 'Immediate alert available'
     WHERE [EventType] = 'ALERT_IMMEDIATE'
       AND [EventId]   = '-1';


    IF @@ROWCOUNT = 0
    BEGIN
        BEGIN TRY

            INSERT INTO dbo.[Event]
            (
                [EventType],
                [EventId],
                [Message],
                [TimeStamp]
            )
            VALUES
            (
                'ALERT_IMMEDIATE',
                '-1',
                'Immediate alert available',
                @Now
            );

        END TRY
        BEGIN CATCH

            /*
                Another transaction may have inserted the Event
                between UPDATE and INSERT.

                Since EventType/EventId is unique, simply touch it.
            */

            IF ERROR_NUMBER() IN (2601, 2627)
            BEGIN

                UPDATE dbo.[Event]
                   SET [TimeStamp] = @Now,
                       [Message]   = 'Immediate alert available'
                 WHERE [EventType] = 'ALERT_IMMEDIATE'
                   AND [EventId]   = '-1';

            END
            ELSE
                THROW;

        END CATCH;
    END;
END;
GO
ALTER TABLE [dbo].[Alert] ENABLE TRIGGER [trigAlertImmediateOnChange]
GO
