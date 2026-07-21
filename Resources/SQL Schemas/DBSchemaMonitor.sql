SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ================================================================================
-- Author:		Imad Ansari
-- Create date: Oct 26, 2024
-- Description:	This function returns DateTime in UTC unix format in seconds
--              From Local datetime
-- Arguments:   - @localTime DATETIME   Local time (datetime)
--              - @TimeZone NVARCHAR(50) time zone string. See the function
--                                       [fn_GetServerTimeZone] to get the 
--                                       timezone from this server
-- ================================================================================
CREATE FUNCTION [dbo].[fn_ConvertLocalDateTimeToUTCRawTime] (@localTime DATETIME, @LocalTimeZone NVARCHAR(50))
RETURNS BIGINT
AS
BEGIN
	-- Convert datetime in @LocalTimeZone to UTC DateTime (adjusts for DST)
	DECLARE @UtcDateTime datetime;
	SET @UtcDateTime = @localTime AT TIME ZONE @LocalTimeZone AT TIME ZONE 'UTC';
	RETURN DATEDIFF(SECOND, '1970-01-01 00:00:00', @UtcDateTime);
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
  CREATE FUNCTION [dbo].[fn_ConvertToDateTime] (@Datetime BIGINT)
RETURNS DATETIME
AS
BEGIN
    DECLARE @LocalTimeOffset BIGINT
           ,@AdjustedLocalDatetime BIGINT;
    SET @LocalTimeOffset = DATEDIFF(second,GETDATE(),GETUTCDATE())
    SET @AdjustedLocalDatetime = @Datetime - @LocalTimeOffset
    RETURN (SELECT DATEADD(second,@AdjustedLocalDatetime, CAST('1970-01-01 00:00:00' AS datetime)))
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ================================================================================
-- Author:		Imad Ansari
-- Create date: Oct 26, 2024
-- Description:	This function returns DateTime in local time 
-- Arguments:   - @EpochTime BIGINT   UTC time in Unix Format in Seconds
--              - @TimeZone NVARCHAR(50) time zone string. See the function
--                                       [fn_GetServerTimeZone] to get the 
--                                       timezone from this server
-- ================================================================================
CREATE FUNCTION [dbo].[fn_ConvertUTCRawTimeToLocalDateTime] (@EpochTime BIGINT, @TimeZone NVARCHAR(50))
RETURNS DATETIME
AS
BEGIN
	DECLARE @UtcDateTime DATETIME;

	-- Step 1: Convert Epoch Time to UTC DateTime
	-- If the value is in milliseconds (typical for current timestamps), it will be much larger
    -- For example, as of 2023, milliseconds since 1970 is around 1.6 trillion
    -- If it's in seconds, it would be around 1.6 billion
    IF @EpochTime > 100000000000 -- Arbitrary threshold (November 20, 5138 in seconds)
    BEGIN
        -- Treat as milliseconds
		-- Break into seconds + milliseconds to avoid overflow
        DECLARE @Seconds BIGINT = @EpochTime / 1000;
        DECLARE @RemainingMs INT = @EpochTime % 1000;
        
        -- Add seconds first, then milliseconds
        SET @UtcDateTime = DATEADD(SECOND, @Seconds, '1970-01-01');
        SET @UtcDateTime = DATEADD(MILLISECOND, @RemainingMs, @UtcDateTime);
    END
    ELSE
    BEGIN
        -- Treat as seconds
        SET @UtcDateTime = DATEADD(SECOND, @EpochTime, '1970-01-01');
    END

	-- Step 2: Convert UTC DateTime to Central Time (adjusts for DST)
	return @UtcDateTime AT TIME ZONE 'UTC' AT TIME ZONE @TimeZone;
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
CREATE TABLE [dbo].[File](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DeviceId] [varchar](256) NOT NULL,
	[FileType] [varchar](50) NOT NULL,
	[FileName] [nvarchar](256) NOT NULL,
	[FileNameNormalized] [nvarchar](256) NOT NULL,
	[FileDate] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
 CONSTRAINT [PK_File] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LogHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FileId] [int] NOT NULL,
	[RawTime] [int] NOT NULL,
	[MSec] [int] NOT NULL,
	[Level] [varchar](5) NOT NULL,
	[DeviceId] [varchar](50) NOT NULL,
	[ThreadId] [int] NOT NULL,
	[Source] [varchar](256) NOT NULL,
	[Message] [varchar](max) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
 CONSTRAINT [PK_LogHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE VIEW [dbo].[VLogHistory]
AS


	SELECT H.[Id]
		  ,H.[FileId]
		  ,F.[FileType]
		  ,F.[FileDate]
		  ,H.[RawTime]
		  ,H.[MSec]
		  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](cast(H.[RawTime] AS BIGINT) * 1000 + H.[MSec], 'UTC') AS [LogDateUTC]
		  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](cast(H.[RawTime] AS BIGINT) * 1000 + H.[MSec], 'Eastern Standard Time') AS [LogDateEST]
		  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](cast(H.[RawTime] AS BIGINT) * 1000 + H.[MSec], 'Arabian Standard Time') AS [LogDateDubai]
		  ,H.[Level]
		  ,H.[DeviceId]
		  ,H.[ThreadId]
		  ,H.[Source]
		  ,H.[Message]
		  ,H.[TimeStamp]
	  FROM [dbo].[LogHistory] AS H
		INNER JOIN [dbo].[File] AS F ON H.[FileId] = F.[Id] 

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
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UniqueFile] ON [dbo].[File]
(
	[DeviceId] ASC,
	[FileType] ASC,
	[FileName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_LogHistoryFileIdRawTime] ON [dbo].[LogHistory]
(
	[FileId] ASC,
	[RawTime] ASC,
	[MSec] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[LogHistory]  WITH NOCHECK ADD  CONSTRAINT [FK_LogHistory_File] FOREIGN KEY([FileId])
REFERENCES [dbo].[File] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LogHistory] CHECK CONSTRAINT [FK_LogHistory_File]
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
CREATE PROCEDURE [dbo].[usp_file_delete]
	@Id int
AS
BEGIN
	Delete [dbo].[File] Where Id = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_file_delete_cutoff]
	@DeviceId varchar(256),
	@FileType varchar(50),
	@CutoffDate int
AS
BEGIN
	SET @DeviceId = trim(upper(@DeviceId));
	SET @FileType = trim(upper(@FileType));
	DELETE [dbo].[File] 
		WHERE (@DeviceId = '' OR [DeviceId] = @DeviceId)
			AND (@FileType = '' OR [FileType] = @FileType)
			AND [FileDate] <= @CutoffDate;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_file_insert]
(
	@IdNew int OUTPUT,
	@Id int,
	@DeviceId varchar(256),
	@FileType varchar(50),
	@FileName nvarchar(256),
	@FileDate int
)

AS
BEGIN
	DECLARE @FileNameNormalized nvarchar(256) =  trim(upper(@FileName));
	SET @FileName = trim(@FileName);
	SET @DeviceId = trim(upper(@DeviceId));
	SET @FileType = trim(upper(@FileType));

	INSERT INTO [dbo].[File]
			   ([DeviceId]
			   ,[FileType]
			   ,[FileName]
			   ,[FileNameNormalized]
			   ,[FileDate]
			   ,[TimeStamp])
		 VALUES
			   (@DeviceId
			   ,@FileType
			   ,@FileName
			   ,@FileNameNormalized
			   ,@FileDate
			   ,getdate())
	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_file_query]
(
	@DeviceId varchar(256),
	@FileType varchar(50),
	@FileName nvarchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @FileName = trim(upper(@FileName));
	SET @DeviceId = trim(upper(@DeviceId));
	SET @FileType = trim(upper(@FileType));

	SELECT [Id]
		  ,[DeviceId]
		  ,[FileType]
		  ,[FileName]
		  ,[FileNameNormalized]
		  ,[FileDate]
		  ,[TimeStamp]
		FROM [dbo].[File]
		WHERE [DeviceId] = @DeviceId AND [FileType] = @FileType AND [FileNameNormalized] = @FileName;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_file_query_last]
(
	@DeviceId varchar(256),
	@FileType varchar(50)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @DeviceId = trim(upper(@DeviceId));
	SET @FileType = trim(upper(@FileType));

	SELECT TOP 1 
			   [Id]
			  ,[DeviceId]
			  ,[FileType]
			  ,[FileName]
			  ,[FileNameNormalized]
			  ,[FileDate]
			  ,[TimeStamp]
		FROM [dbo].[File]
		WHERE [DeviceId] = @DeviceId AND [FileType] = @FileType
		ORDER BY [FileDate] DESC;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_file_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[DeviceId]
		  ,[FileType]
		  ,[FileName]
		  ,[FileNameNormalized]
		  ,[FileDate]
		  ,[TimeStamp]
		FROM [dbo].[File]
		WHERE @Id = -1 OR [Id] = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_loghistory_query_Last]
(
	@FileId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT TOP 1
	         [Id]
			,[FileId]
			,[RawTime]
			,[MSec]
			,[Level]
			,[DeviceId]
			,[ThreadId]
			,[Source]
			,[Message]
			,[TimeStamp]
		FROM [dbo].[LogHistory]
		WHERE [FileId] = @FileId
		ORDER BY [FileId], [RawTime] DESC
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_loghistory_query_recent] 
	@FileId int,
	@MinId int,
	@MaxRec int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	WITH FilteredData AS (
			SELECT 
				 [Id]
				,[FileId]
				,[RawTime]
				,[MSec]
				,[Level]
				,[DeviceId]
				,[ThreadId]
				,[Source]
				,[Message]
				,[TimeStamp]
				,ROW_NUMBER() 
			OVER (ORDER BY [Id] DESC) as RowNum
			FROM [dbo].[LogHistory]
			WHERE (@FileId = -1 OR [FileId] = @FileId) AND [Id] > @MinId
		)
		SELECT 
			 [Id]
			,[FileId]
			,[RawTime]
			,[MSec]
			,[Level]
			,[DeviceId]
			,[ThreadId]
			,[Source]
			,[Message]
			,[TimeStamp]
		FROM FilteredData
		WHERE RowNum <= @MaxRec
		ORDER BY [Id] ASC -- Final output ordered ascending
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_sys_databases_select]
(
	@DBName varchar(255)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT	 [name]
			,[database_id]
			,[is_broker_enabled] 
		FROM sys.databases 
		WHERE [name] = @DBName;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigLogHistoryOnChange]
   ON  [dbo].[LogHistory]
   AFTER INSERT, UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

	-- Only proceed if rows were actually affected
    IF NOT EXISTS (SELECT 1 FROM inserted)
        RETURN;

	DECLARE @EventType varchar(50);
	DECLARE @EventId varchar(100);
	DECLARE @Message varchar(256);
    SELECT  TOP 1 
			@EventType = 'LOGHISTORY',
			@EventId = CAST(i.FileId AS VARCHAR(10)),
			@Message = 'Inserted Automatically from LogHistory Trigger' 
		FROM inserted i;

	IF @EventType = 'LOGHISTORY' 
	BEGIN
		UPDATE [Event]
			SET TimeStamp = GETDATE()
			WHERE [EventType] = @EventType AND [EventId] = @EventId;

		IF @@ROWCOUNT = 0
		BEGIN
			-- If no row was updated, perform an INSERT
			INSERT INTO [Event] ([EventType], [EventId], [Message], [TimeStamp])
			VALUES (@EventType, @EventId, @Message, GETDATE());
		END
	END

END


GO
ALTER TABLE [dbo].[LogHistory] ENABLE TRIGGER [trigLogHistoryOnChange]
GO
