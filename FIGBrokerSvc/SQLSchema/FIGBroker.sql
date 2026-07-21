SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ================================================================================
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
	-- Step 1: Convert Epoch Time to UTC DateTime
	DECLARE @UtcDateTime DATETIME = DATEADD(SECOND, @EpochTime, '1970-01-01');

	-- Step 2: Convert UTC DateTime to Central Time (adjusts for DST)
	return @UtcDateTime AT TIME ZONE 'UTC' AT TIME ZONE @TimeZone
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BrokerAccount](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AdapterType] [varchar](30) NOT NULL,
	[AccountId] [varchar](256) NOT NULL,
	[AccountName] [varchar](256) NOT NULL,
	[AccountDesc] [varchar](256) NOT NULL,
 CONSTRAINT [PK_BrokerAccount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderInstruction](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BrokerAccountId] [int] NOT NULL,
	[OrderRefId] [varchar](36) NOT NULL,
	[RawTime] [int] NOT NULL,
	[Ticker] [varchar](25) NOT NULL,
	[Action] [varchar](50) NOT NULL,
	[OrderTag] [varchar](50) NOT NULL,
	[OrderType] [varchar](50) NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[OrderStatus] [int] NOT NULL,
	[FillQty] [int] NOT NULL,
	[FillPrice] [decimal](18, 4) NULL,
	[BrokerOrderId] [varchar](100) NULL,
	[CreationRawTime] [int] NOT NULL,
	[LastRawTime] [int] NOT NULL,
 CONSTRAINT [PK_OrderInstruction] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







CREATE VIEW [dbo].[VOrderInstruction]
AS
	SELECT 
			 OI.[Id]
			,OI.[BrokerAccountId]
			,ISNULL(BA.[AdapterType], 'UNDEF_ADAPTER') AS [Adapter]
			,ISNULL(BA.[AccountName], 'UNDEF_ACCT') AS [AccountName]
			,ISNULL(BA.[AccountId], 'UNDEF_ACCTID') AS [AccountId]
			,OI.[OrderRefId]
			,OI.[RawTime]
			,OI.[Ticker]
			,OI.[Action]
			,OI.[OrderTag]
			,OI.[OrderType]
			,OI.[Qty]
			,OI.[Price]
			,OI.[OrderStatus]
			,OI.[FillQty]
			,OI.[FillPrice]
			,OI.[Broker_Order_id]
			,OI.[CreationRawTime]
			,OI.[LastRawTime]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([CreationRawTime], 'UTC') AS [CreationTimeUTC]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([CreationRawTime], 'Eastern Standard Time') AS [CreationTimeEST]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([CreationRawTime], 'Arabian Standard Time') AS [CreationTimeDubai]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([LastRawTime], 'UTC') AS [LastTimeUTC]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([LastRawTime], 'Eastern Standard Time') AS [LastTimeEST]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([LastRawTime], 'Arabian Standard Time') AS [LastTimeDubai]
		FROM [OrderInstruction] AS OI
			 LEFT JOIN [BrokerAccount] BA ON OI.[BrokerAccountId] = BA.[Id]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BrokerTicker](
	[BrokerAccountId] [int] NOT NULL,
	[TickerDefId] [int] NOT NULL,
 CONSTRAINT [PK_BrokerTicker] PRIMARY KEY CLUSTERED 
(
	[BrokerAccountId] ASC,
	[TickerDefId] ASC
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
CREATE TABLE [dbo].[TickerDef](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Symbol] [varchar](50) NOT NULL,
	[LocalSymbol] [varchar](50) NOT NULL,
	[Name] [varchar](255) NOT NULL,
	[SecurityType] [varchar](50) NOT NULL,
	[Currency] [varchar](50) NOT NULL,
	[ExpiryDate] [int] NOT NULL,
	[MinTick] [decimal](18, 6) NOT NULL,
	[ContractSize] [int] NOT NULL,
	[Exchange] [varchar](100) NOT NULL,
	[BrokerRef] [varchar](100) NOT NULL,
 CONSTRAINT [PK_TickerDef] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_BrokerAccount_Unique] ON [dbo].[BrokerAccount]
(
	[AdapterType] ASC,
	[AccountId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_OrderInstruction_BrokerAccount] ON [dbo].[OrderInstruction]
(
	[BrokerAccountId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_OrderInstruction_BrokerAccount_Ticker] ON [dbo].[OrderInstruction]
(
	[BrokerAccountId] ASC,
	[Ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_OrderInstruction_Opt1] ON [dbo].[OrderInstruction]
(
	[BrokerAccountId] ASC,
	[OrderStatus] ASC,
	[CreationRawTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_OrderInstruction_Opt2] ON [dbo].[OrderInstruction]
(
	[BrokerAccountId] ASC,
	[OrderRefId] ASC,
	[OrderTag] ASC,
	[BrokerOrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_TickerDef_Symbol] ON [dbo].[TickerDef]
(
	[Symbol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OrderInstruction] ADD  CONSTRAINT [DF_OrderInstruction_OrderStatus]  DEFAULT ((0)) FOR [OrderStatus]
GO
ALTER TABLE [dbo].[OrderInstruction] ADD  CONSTRAINT [DF_OrderInstruction_FillQty]  DEFAULT ((0)) FOR [FillQty]
GO
ALTER TABLE [dbo].[OrderInstruction] ADD  CONSTRAINT [DF_OrderInstruction_LastRawTime]  DEFAULT ((0)) FOR [LastRawTime]
GO
ALTER TABLE [dbo].[BrokerTicker]  WITH CHECK ADD  CONSTRAINT [FK_BrokerTicker_BrokerAccount] FOREIGN KEY([BrokerAccountId])
REFERENCES [dbo].[BrokerAccount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BrokerTicker] CHECK CONSTRAINT [FK_BrokerTicker_BrokerAccount]
GO
ALTER TABLE [dbo].[BrokerTicker]  WITH CHECK ADD  CONSTRAINT [FK_BrokerTicker_TickerDef] FOREIGN KEY([TickerDefId])
REFERENCES [dbo].[TickerDef] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BrokerTicker] CHECK CONSTRAINT [FK_BrokerTicker_TickerDef]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccount_delete]
	@Id int
AS
BEGIN
	Delete [BrokerAccount] Where Id = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccount_query]
(
	@AdapterType varchar(30),
	@AccountId varchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @AdapterType = TRIM(UPPER(@AdapterType));
	SET @AccountId = TRIM(UPPER(@AccountId));

	SELECT [Id]
		  ,[AdapterType]
		  ,[AccountId]
		  ,[AccountName]
		  ,[AccountDesc]
		FROM [BrokerAccount]
		WHERE @AdapterType = [AdapterType] AND @AccountId = TRIM(UPPER([AccountId]))
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccount_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[AdapterType]
		  ,[AccountId]
		  ,[AccountName]
		  ,[AccountDesc]
		FROM [BrokerAccount]
		WHERE @Id = -1 OR @Id = Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccount_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@AdapterType varchar(30),
	@AccountId varchar(256),
	@AccountName varchar(256),
	@AccountDesc varchar(256)
)

AS
BEGIN

	SET @AdapterType = TRIM(UPPER(@AdapterType));
	SET @AccountId = TRIM(@AccountId);
	SET @AccountName = TRIM(@AccountName);
	SET @AccountDesc = TRIM(@AccountDesc);

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [BrokerAccount] WHERE @AdapterType = [AdapterType] AND UPPER(@AccountId) = Upper([AccountId]);
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [BrokerAccount]
			   ([AdapterType]
			   ,[AccountId]
			   ,[AccountName]
			   ,[AccountDesc])
		 VALUES
			   (@AdapterType
			   ,@AccountId
			   ,@AccountName
			   ,@AccountDesc)
		   
		SET @IdNew = SCOPE_IDENTITY();  
	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [BrokerAccount] SET 
			  [AdapterType] = @AdapterType
			, [AccountId] = @AccountId
			, [AccountName] = @AccountName
			, [AccountDesc] = @AccountDesc
		WHERE Id = @IdNew;
	END

	RETURN @IdNew;
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
CREATE PROCEDURE [dbo].[usp_orderinstruction_insert]
(
	@IdNew int OUTPUT,
	@Id int,
	@BrokerAccountId int,
	@OrderRefId varchar(36),
	@RawTime int,
	@Ticker varchar(25),
	@Action varchar(50),
	@OrderTag varchar(50),
	@OrderType varchar(50),
	@Qty int,
	@Price decimal(18,4),
	@OrderStatus int,
	@FillQty int,
	@FillPrice decimal(18,4),
	@BrokerOrderId varchar(100),
	@CreationRawTime int,
	@LastRawTime int
)

AS
BEGIN
	
	SET @CreationRawTime = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	SET @LastRawTime = @CreationRawTime;
	SET @Ticker = Trim(Upper(@Ticker));
	SET @Action = Trim(Upper(@Action));
	SET @OrderTag = Trim(Upper(@OrderTag));
	SET @OrderType = Trim(Upper(@OrderType));

	
	INSERT INTO [OrderInstruction]
           ([BrokerAccountId]
           ,[OrderRefId]
           ,[RawTime]
           ,[Ticker]
           ,[Action]
           ,[OrderTag]
           ,[OrderType]
           ,[Qty]
           ,[Price]
           ,[OrderStatus]
           ,[FillQty]
           ,[FillPrice]
           ,[BrokerOrderId]
           ,[CreationRawTime]
           ,[LastRawTime])
     VALUES
           (@BrokerAccountId
           ,@OrderRefId
           ,@RawTime
           ,@Ticker
           ,@Action
           ,@OrderTag
           ,@OrderType
           ,@Qty
           ,@Price
           ,@OrderStatus
           ,@FillQty
           ,@FillPrice
           ,@BrokerOrderId
           ,@CreationRawTime
           ,@LastRawTime);

	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_orderinstruction_pull]
	@BrokerAccountId int,
	@ExpiryTimeSec int
AS
BEGIN

	-- Declare the table variable with the required columns
	DECLARE @TblLastInstructions TABLE (
		[Id] INT,
		[Ticker] VARCHAR(25),
		[OrderRefId] VARCHAR(36),
		[OrderTag] VARCHAR(50),
		[Action] VARCHAR(50),
		[BrokerOrderId] VARCHAR(100),
		[Keep] int
	);

	BEGIN TRY
		-- Mark all records that are older than (@curRawTime - @ExpiryTimeSec) as Expired
		-----------------------------------------------------------------------------------------
		DECLARE @curRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
		UPDATE [OrderInstruction] 
			SET [OrderStatus] = -2     -- EXPIRED
				,[LastRawTime] = @curRawTime
			WHERE [BrokerAccountId] = @BrokerAccountId
					AND [OrderStatus] = 0
					AND [CreationRawTime] < (@curRawTime - @ExpiryTimeSec);


		-- Select Last top records for each Ticker and OrderRefId
		-----------------------------------------------------------------------------------------
		WITH RankedOrders AS (
		SELECT 
			[Id],
			[Ticker],
			[OrderRefId],
			[OrderTag],
			[Action],
			ROW_NUMBER() OVER (PARTITION BY [Ticker], [OrderRefId] ORDER BY [Id] DESC) AS RowNum
		FROM [OrderInstruction]
			WHERE [BrokerAccountId] = @BrokerAccountId
				  AND [OrderStatus] = 0    -- new records only
		)
		INSERT INTO @TblLastInstructions ([Id], [Ticker], [OrderRefId], [OrderTag], [Action], [BrokerOrderId], [Keep])
		SELECT [Id], [Ticker], [OrderRefId], [OrderTag], [Action], '', 1
			FROM RankedOrders
			WHERE RowNum = 1;

		-- Mark all other new requests as ignored (OrderStatus = -3)
		-----------------------------------------------------------------------------------------
		UPDATE [OrderInstruction] 
			SET [OrderStatus] = -3, [LastRawTime] = @curRawTime 
			WHERE [BrokerAccountId] = @BrokerAccountId
				  AND [OrderStatus] = 0
				  AND [Id] not in (SELECT [Id] FROM @TblLastInstructions);


		-- Check if the record has the OrderTag 'CLOSE' without corresponding 'OPEN'. 
		-- Update the temp table first
		-----------------------------------------------------------------------------------
		UPDATE t
			SET t.[Keep] = 0
			FROM @TblLastInstructions t
			WHERE t.[OrderTag] = 'CLOSE' 
				AND NOT EXISTS(
				SELECT 1 
					FROM [OrderInstruction] o 
					WHERE o.[BrokerAccountId] = @BrokerAccountId 
						AND o.[OrderRefId] = t.[OrderRefId] 
						AND o.[OrderTag] = 'OPEN' 
						AND o.[OrderStatus] = 3
				);

		-- Find existing BrokerOrderIds if any and update temp table with the BrokerId
		-- Update the temp table first
		-----------------------------------------------------------------------------------------
		UPDATE t
			SET t.[BrokerOrderId] = latest.[BrokerOrderId]
			FROM @TblLastInstructions t
				CROSS APPLY (
					SELECT TOP 1 oi.[BrokerOrderId]
						FROM [OrderInstruction] oi
						WHERE oi.[BrokerAccountId] = @BrokerAccountId 
							AND oi.[OrderRefId] = t.[OrderRefId] 
							AND oi.[OrderTag] = t.[OrderTag] 
							AND oi.[BrokerOrderId] > ''
						ORDER BY oi.[Id] DESC
					) latest;


		-- Final update of OrderInstruction records
		-----------------------------------------------------------------------------------------
		UPDATE oi
			SET [OrderStatus] = CASE WHEN t.[Keep] = 1 THEN 1 ELSE -3 END,
				[LastRawTime] = @curRawTime,
				[BrokerOrderId] = CASE WHEN t.[BrokerOrderId] = '' THEN NULL ELSE t.[BrokerOrderId] END,
				[OrderTag] = CASE WHEN oi.[OrderTag] = 'UPDATE' AND t.[BrokerOrderId] = '' THEN 'NEW' ELSE oi.[OrderTag] END
			FROM [OrderInstruction] oi
				 INNER JOIN @TblLastInstructions t ON oi.[Id] = t.[Id];
	END TRY
	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
		DECLARE @ErrorState INT = ERROR_STATE();
    
		RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
		RETURN -1;
	END CATCH


	-- IF all good, return the records that are picked up 
	SELECT [Id]
		  ,[BrokerAccountId]
		  ,[OrderRefId]
		  ,[RawTime]
		  ,[Ticker]
		  ,[Action]
		  ,[OrderTag]
		  ,[OrderType]
		  ,[Qty]
		  ,[Price]
		  ,[OrderStatus]
		  ,[FillQty]
		  ,[FillPrice]
		  ,[BrokerOrderId]
		  ,[CreationRawTime]
		  ,[LastRawTime]
		FROM [OrderInstruction]
		WHERE [Id] in (SELECT [Id] FROM @TblLastInstructions WHERE [Keep] = 1);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_orderinstruction_update_status]
(
	@Id int,
	@OrderStatus int
)

AS
BEGIN
	
	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	
	UPDATE [OrderInstruction] 
		SET  [OrderStatus] = @OrderStatus
			,[LastRawTime] = @LastRawTime
		WHERE [Id] = @Id AND [OrderStatus] != 3;
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
CREATE PROCEDURE [dbo].[usp_tickerdef_query_by_brokeraccount]
(
	@BrokerAccountId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT T.[Id]
		  ,T.[Symbol]
		  ,T.[LocalSymbol]
		  ,T.[Name]
		  ,T.[SecurityType]
		  ,T.[Currency]
		  ,T.[ExpiryDate]
		  ,T.[MinTick]
		  ,T.[ContractSize]
		  ,T.[Exchange]
		  ,T.[BrokerRef]
		FROM [TickerDef] AS T 
			INNER JOIN [BrokerTicker] AS BT ON T.[Id] = BT.[TickerDefId]
		WHERE @BrokerAccountId = BT.[BrokerAccountId]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tickerdef_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Symbol]
		  ,[LocalSymbol]
		  ,[Name]
		  ,[SecurityType]
		  ,[Currency]
		  ,[ExpiryDate]
		  ,[MinTick]
		  ,[ContractSize]
		  ,[Exchange]
		  ,[BrokerRef]
		FROM [Ticker]
		WHERE @Id = -1 OR @Id = Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tickerdef_select_by_Symbol]
(
	@Symbol varchar(50)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @Symbol = TRIM(UPPER(@Symbol));

	SELECT [Id]
		  ,[Symbol]
		  ,[LocalSymbol]
		  ,[Name]
		  ,[SecurityType]
		  ,[Currency]
		  ,[ExpiryDate]
		  ,[MinTick]
		  ,[ContractSize]
		  ,[Exchange]
		  ,[BrokerRef]
		FROM [TickerDef]
		WHERE @Symbol = [Symbol]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tickerdef_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@Symbol varchar(50),
	@LocalSymbol varchar(50),
	@Name varchar(256),
	@SecurityType varchar(50),
	@Currency varchar(50),
	@ExpiryDate int,
	@MinTick decimal(18,6),
	@ContractSize int,
	@Exchange varchar(100),
	@BrokerRef varchar(100)
)

AS
BEGIN

	SET @Symbol = TRIM(UPPER(@Symbol));

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [TickerDef] WHERE @Symbol = [Symbol];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [TickerDef] (
			  [Symbol]
			, [LocalSymbol]
			, [Name]
			, [SecurityType]
			, [Currency]
			, [ExpiryDate]
			, [MinTick]
			, [ContractSize]
			, [Exchange]
			, [BrokerRef]
		)
		VALUES (
			  @Symbol
			, @Name
			, @LocalSymbol
			, @SecurityType
			, @Currency
			, @ExpiryDate
			, @MinTick
			, @ContractSize
			, @Exchange
			, @BrokerRef
		)
		SET @IdNew = SCOPE_IDENTITY();  
	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [TickerDef] SET 
			  [Symbol] = @Symbol
			, [LocalSymbol] = @LocalSymbol
			, [Name] = @Name
			, [SecurityType] = @SecurityType
			, [Currency] = @Currency
			, [ExpiryDate] = @ExpiryDate
			, [MinTick] = @MinTick
			, [ContractSize] = @ContractSize
			, [Exchange] = @Exchange
			, [BrokerRef] = @BrokerRef
		WHERE Id = @IdNew;
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigOrderInstructionOnChange]
   ON  [dbo].[OrderInstruction]
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
			@EventType = 'ORDERINSTRUCTION',
			@EventId = CAST(i.[BrokerAccountId] AS VARCHAR(256)),
			@Message = 'Inserted Automatically from OrderInstruction Trigger' 
		FROM inserted i;

	IF @EventType = 'ORDERINSTRUCTION' 
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
ALTER TABLE [dbo].[OrderInstruction] ENABLE TRIGGER [trigOrderInstructionOnChange]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigOrderInstructionOnDelete]
   ON  [dbo].[OrderInstruction]
   AFTER DELETE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Handle all deleted rows
    MERGE INTO [Event] AS target
    USING (
        SELECT 
            'ORDERINSTRUCTION_DEL' AS EventType,
            CAST([BrokerAccountId] AS VARCHAR(256)) AS EventId,
            'Inserted Automatically from OrderInstruction Trigger' AS Message,
            MAX(GETDATE()) AS TimeStamp
        FROM deleted
		GROUP BY [BrokerAccountId]  -- Group by the ID to ensure one row per EventId
    ) AS source
    ON (target.[EventType] = source.[EventType] AND target.[EventId] = source.[EventId])
    WHEN MATCHED THEN
        UPDATE SET 
            TimeStamp = source.TimeStamp,
            Message = source.Message
    WHEN NOT MATCHED THEN
        INSERT ([EventType], [EventId], [Message], [TimeStamp])
        VALUES (source.EventType, source.EventId, source.Message, source.TimeStamp);

END
GO
ALTER TABLE [dbo].[OrderInstruction] ENABLE TRIGGER [trigOrderInstructionOnDelete]
GO
