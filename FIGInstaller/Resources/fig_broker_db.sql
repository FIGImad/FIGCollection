USE FIGBroker
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
CREATE   FUNCTION [dbo].[fn_split_string]
(
    @in_string VARCHAR(MAX),
    @delimiter VARCHAR(1) = ','
)
RETURNS @list TABLE (field VARCHAR(100))
AS
BEGIN
    DECLARE @pos INT, @token VARCHAR(100);

    IF @in_string IS NULL OR @in_string = '' RETURN;

    -- Treat NULL/empty delimiter as default comma
    SET @delimiter = COALESCE(NULLIF(@delimiter, ''), ',');

    WHILE 1 = 1
    BEGIN
        SET @pos = CHARINDEX(@delimiter, @in_string);

        IF @pos = 0
        BEGIN
            SET @token = LEFT(@in_string, 100);
            INSERT INTO @list(field) VALUES (@token);
            BREAK;
        END

        SET @token = LEFT(@in_string, @pos - 1);
        INSERT INTO @list(field) VALUES (LEFT(@token, 100));

        SET @in_string = STUFF(@in_string, 1, @pos, '');
        IF @in_string = '' BREAK;
    END

    RETURN;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BrokerAccount](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [varchar](50) NOT NULL,
	[AdapterType] [varchar](30) NOT NULL,
	[AccountName] [varchar](256) NOT NULL,
	[AccountServiceId] [varchar](50) NOT NULL,
	[URL] [varchar](256) NOT NULL,
	[BrokerAccountId] [varchar](256) NOT NULL,
	[BrokerAccountParams] [varchar](max) NOT NULL,
	[Enable] [bit] NOT NULL,
 CONSTRAINT [PK_BrokerAccount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ticker](
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
 CONSTRAINT [PK_Ticker] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Order](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BrokerAccountId] [int] NOT NULL,
	[OrderRefId] [varchar](256) NOT NULL,
	[TickerId] [int] NOT NULL,
	[OrderTagRef] [varchar](50) NOT NULL,
	[OrderTag] [varchar](50) NOT NULL,
	[OrderType] [varchar](50) NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[StopPrice] [decimal](18, 4) NOT NULL,
	[OrderStatus] [int] NOT NULL,
	[FillQty] [int] NOT NULL,
	[FillPrice] [decimal](18, 4) NOT NULL,
	[CancelRequested] [bit] NOT NULL,
	[BrokerRef] [varchar](256) NOT NULL,
	[BrokerOrderId] [varchar](256) NOT NULL,
	[BrokerParams] [varchar](max) NOT NULL,
	[OrderTime] [int] NOT NULL,
	[CompletionTime] [int] NOT NULL,
	[LastUpdateTime] [int] NOT NULL,
 CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO



CREATE VIEW [dbo].[VOrder]
AS
	SELECT 
			 OI.[Id]
			,OI.[BrokerAccountId]
			,ISNULL(BA.[AdapterType], 'UNDEF_ADAPTER') AS [Adapter]
			,ISNULL(BA.[AccountName], 'UNDEF_ACCT') AS [AccountName]
			,ISNULL(BA.[AccountId], 'UNDEF_ACCTID') AS [AccountId]
			,OI.[OrderRefId]
			,OI.[TickerId] 
            ,T.[LocalSymbol] AS Ticker
            ,OI.[OrderTag]
            ,OI.[OrderType]
            ,OI.[Qty]
            ,OI.[Price]
            ,OI.[StopPrice]
            ,OI.[OrderStatus]
            ,SC.[Status] AS OrderStatusDesc
            ,OI.[FillQty]
            ,OI.[FillPrice]
            ,OI.[CancelRequested]
            ,OI.[BrokerRef]
            ,OI.[BrokerOrderId]
            ,OI.[BrokerParams]
            ,OI.[OrderTime]
            ,OI.[CompletionTime]
            ,OI.[LastUpdateTime]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([OrderTime], 'UTC') AS [OrderTimeUTC]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([OrderTime], 'Eastern Standard Time') AS [OrderTimeEST]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([OrderTime], 'Arabian Standard Time') AS [OrderTimeDubai]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([CompletionTime], 'UTC') AS [CompletionTimeUTC]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([CompletionTime], 'Eastern Standard Time') AS [CompletionTimeEST]
			,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([CompletionTime], 'Arabian Standard Time') AS [CompletionTimeDubai]
		FROM [Order] AS OI
			 LEFT JOIN [BrokerAccount] BA ON OI.[BrokerAccountId] = BA.[Id]
             LEFT JOIN [FigAutoTrader].dbo.[OrderStatusCode] AS SC ON OI.OrderStatus = SC.Code
             LEFT  JOIN [Ticker] AS T ON OI.[TickerId] = T.Id
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BrokerAccountTicker](
	[BrokerAccountId] [int] NOT NULL,
	[TickerId] [int] NOT NULL,
	[Allowed] [bit] NOT NULL,
	[LongLimit] [int] NOT NULL,
	[ShortLimit] [int] NOT NULL,
 CONSTRAINT [PK_BrokerAccountTicker] PRIMARY KEY CLUSTERED 
(
	[BrokerAccountId] ASC,
	[TickerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BrokerAdapter](
	[Type] [varchar](30) NOT NULL,
	[Description] [varchar](256) NOT NULL,
 CONSTRAINT [PK_BrokerAdapter] PRIMARY KEY CLUSTERED 
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_BrokerAccount_AccountId] ON [dbo].[BrokerAccount]
(
	[AccountId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Order_BrokerAccount] ON [dbo].[Order]
(
	[BrokerAccountId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_Order_BrokerOrderId] ON [dbo].[Order]
(
	[BrokerOrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_Order_BrokerRef] ON [dbo].[Order]
(
	[BrokerRef] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Order_Opt1] ON [dbo].[Order]
(
	[BrokerAccountId] ASC,
	[OrderStatus] ASC,
	[OrderTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_Ticker_Symbol] ON [dbo].[Ticker]
(
	[Symbol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Order] ADD  CONSTRAINT [DF_Order_OrderStatus]  DEFAULT ((0)) FOR [OrderStatus]
GO
ALTER TABLE [dbo].[Order] ADD  CONSTRAINT [DF_Order_FillQty]  DEFAULT ((0)) FOR [FillQty]
GO
ALTER TABLE [dbo].[Order] ADD  CONSTRAINT [DF_Order_LastUpdateTime]  DEFAULT ((0)) FOR [LastUpdateTime]
GO
ALTER TABLE [dbo].[BrokerAccount]  WITH CHECK ADD  CONSTRAINT [FK_BrokerAccount_BrokerAdapter] FOREIGN KEY([AdapterType])
REFERENCES [dbo].[BrokerAdapter] ([Type])
GO
ALTER TABLE [dbo].[BrokerAccount] CHECK CONSTRAINT [FK_BrokerAccount_BrokerAdapter]
GO
ALTER TABLE [dbo].[BrokerAccountTicker]  WITH CHECK ADD  CONSTRAINT [FK_BrokerAccountTicker_BrokerAccount] FOREIGN KEY([BrokerAccountId])
REFERENCES [dbo].[BrokerAccount] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BrokerAccountTicker] CHECK CONSTRAINT [FK_BrokerAccountTicker_BrokerAccount]
GO
ALTER TABLE [dbo].[BrokerAccountTicker]  WITH CHECK ADD  CONSTRAINT [FK_BrokerAccountTicker_Ticker] FOREIGN KEY([TickerId])
REFERENCES [dbo].[Ticker] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[BrokerAccountTicker] CHECK CONSTRAINT [FK_BrokerAccountTicker_Ticker]
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_BrokerAccount] FOREIGN KEY([BrokerAccountId])
REFERENCES [dbo].[BrokerAccount] ([Id])
GO
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_BrokerAccount]
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_Ticker] FOREIGN KEY([TickerId])
REFERENCES [dbo].[Ticker] ([Id])
GO
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_Ticker]
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
          ,[AccountId]
          ,[AdapterType]
          ,[AccountName]
          ,[AccountServiceId]
          ,[URL]
          ,[BrokerAccountId]
          ,[BrokerAccountParams]
          ,[Enable]
		FROM [BrokerAccount]
		WHERE @Id = -1 OR @Id = Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccount_select_by_AccountId]
(
	@AccountId varchar(50)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    SET @AccountId = trim(upper(@AccountId));
	SELECT [Id]
          ,[AccountId]
          ,[AdapterType]
          ,[AccountName]
          ,[AccountServiceId]
          ,[URL]
          ,[BrokerAccountId]
          ,[BrokerAccountParams]
          ,[Enable]
		FROM [BrokerAccount]
		WHERE @AccountId =trim(upper([AccountId]))
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccount_select_by_ServiceId]
(
	@ServiceId varchar(50)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SET @ServiceId = trim(upper(@ServiceId));

	SELECT [Id]
          ,[AccountId]
          ,[AdapterType]
          ,[AccountName]
          ,[AccountServiceId]
          ,[URL]
          ,[BrokerAccountId]
          ,[BrokerAccountParams]
          ,[Enable]
		FROM [BrokerAccount]
		WHERE @ServiceId = trim(upper([AccountServiceId]));
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
	@AccountId varchar(50),
	@AdapterType varchar(30),
	@AccountName varchar(256),
	@AccountServiceId varchar(50),
	@URL varchar(256),
	@BrokerAccountId varchar(256),
	@BrokerAccountParams varchar(max),
	@Enable bit
)

AS
BEGIN

	SET @AdapterType = TRIM(UPPER(@AdapterType));
	SET @AccountId = TRIM(@AccountId);
	SET @AccountName = TRIM(@AccountName);
	SET @AccountServiceId = UPPER(TRIM(@AccountServiceId));

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [BrokerAccount] WHERE UPPER(@AccountId) = Upper([AccountId]);
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [BrokerAccount]
			   ([AdapterType]
			   ,[AccountId]
			   ,[AccountName]
			   ,[AccountServiceId]
			   ,[URL]
			   ,[BrokerAccountId]
			   ,[BrokerAccountParams]
			   ,[Enable])
		 VALUES
			   (@AdapterType
			   ,@AccountId
			   ,@AccountName
			   ,@AccountServiceId
			   ,@URL
			   ,@BrokerAccountId
			   ,@BrokerAccountParams
			   ,@Enable)

		   
		SET @IdNew = SCOPE_IDENTITY();  
	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [BrokerAccount] SET 
			  [AdapterType] = @AdapterType
			, [AccountId] = @AccountId
			, [AccountName] = @AccountName
			, [AccountServiceId] = @AccountServiceId
			, [URL] = @URL
			, [BrokerAccountId] = @BrokerAccountId
			, [BrokerAccountParams] = @BrokerAccountParams
			, [Enable] = @Enable
		WHERE Id = @IdNew;
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_BrokerAccount_UpsertFromJson]
(
    @ConfigJsn varchar(max)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @ConfigJsn IS NULL OR LTRIM(RTRIM(@ConfigJsn)) = ''
        BEGIN
            THROW 50001, 'Config JSON is empty.', 1;
        END

        IF ISJSON(@ConfigJsn) <> 1
        BEGIN
            THROW 50002, 'Config JSON is not valid.', 1;
        END

        BEGIN TRAN;

        ----------------------------------------------------------------------
        -- 1) Parse top-level JSON rows
        ----------------------------------------------------------------------
        IF OBJECT_ID('tempdb..#Config') IS NOT NULL DROP TABLE #Config;
        CREATE TABLE #Config
        (
            AccountId         varchar(50)   NOT NULL,
            AccountServiceId  varchar(50)   NOT NULL,
            URL               varchar(256)  NOT NULL,
            BrokerAccountId   varchar(256)  NOT NULL,
            ClientId          int           NOT NULL,
            TrackingClientId  int           NOT NULL,
            AllowedTickersJsn nvarchar(max) NULL
        );

        INSERT INTO #Config
        (
            AccountId,
            AccountServiceId,
            URL,
            BrokerAccountId,
            ClientId,
            TrackingClientId,
            AllowedTickersJsn
        )
        SELECT
            J.AccountId,
            J.AccountServiceId,
            J.URL,
            J.BrokerAccountId,
            J.ClientId,
            J.TrackingClientId,
            J.AllowedTickers
        FROM OPENJSON(@ConfigJsn)
        WITH
        (
            AccountId        varchar(50)   '$.AccountId',
            AccountServiceId varchar(50)   '$.AccountServiceId',
            URL              varchar(256)  '$.URL',
            BrokerAccountId  varchar(256)  '$.BrokerAccountId',
            ClientId         int           '$.ClientId',
            TrackingClientId int           '$.TrackingClientId', 
            AllowedTickers   nvarchar(max) '$.AllowedTickers' AS JSON
        ) AS J;

        IF NOT EXISTS (SELECT 1 FROM #Config)
        BEGIN
            THROW 50003, 'No valid configuration rows were found in the JSON.', 1;
        END

        ----------------------------------------------------------------------
        -- 2) Expand each config row into 2 BrokerAccount rows:
        --    a) Trading
        ----------------------------------------------------------------------
        IF OBJECT_ID('tempdb..#DesiredAccounts') IS NOT NULL DROP TABLE #DesiredAccounts;
        CREATE TABLE #DesiredAccounts
        (
            JsonAccountId       varchar(50)   NOT NULL,   -- original config AccountId
            AccountId           varchar(50)   NOT NULL,   -- actual BrokerAccount.AccountId
            AdapterType         varchar(30)   NOT NULL,
            AccountName         varchar(256)  NOT NULL,
            AccountServiceId    varchar(50)  NOT NULL,
            URL                 varchar(256)  NOT NULL,
            BrokerAccountId     varchar(256)  NOT NULL,
            BrokerAccountParams varchar(max)  NOT NULL,
            Enable              bit           NOT NULL
        );

        INSERT INTO #DesiredAccounts
        (
            JsonAccountId,
            AccountId,
            AdapterType,
            AccountName,
            AccountServiceId,
            URL,
            BrokerAccountId,
            BrokerAccountParams,
            Enable
        )
        SELECT
            C.AccountId AS JsonAccountId,
            C.AccountId AS AccountId,
            'IBKR' AS AdapterType,
            C.AccountId + '-PROD' AS AccountName,
            C.AccountServiceId AS AccountServiceId,
            C.URL,
            C.BrokerAccountId,
            CONCAT('{"client_id":', C.ClientId, ',"tracking_client_id":', C.TrackingClientId, '}') AS BrokerAccountParams,
            CAST(1 AS bit) AS Enable
        FROM #Config C;

        ----------------------------------------------------------------------
        -- 3) Upsert BrokerAccount
        --    Update existing rows by AccountId
        ----------------------------------------------------------------------
        UPDATE BA
           SET BA.AdapterType         = DA.AdapterType,
               BA.AccountName         = DA.AccountName,
               BA.AccountServiceId    = DA.AccountServiceId,
               BA.URL                 = DA.URL,
               BA.BrokerAccountId     = DA.BrokerAccountId,
               BA.BrokerAccountParams = DA.BrokerAccountParams,
               BA.Enable              = DA.Enable
        FROM dbo.BrokerAccount BA
        INNER JOIN #DesiredAccounts DA
            ON BA.AccountId = DA.AccountId;

        INSERT INTO dbo.BrokerAccount
        (
            AccountId,
            AdapterType,
            AccountName,
            AccountServiceId,
            URL,
            BrokerAccountId,
            BrokerAccountParams,
            Enable
        )
        SELECT
            DA.AccountId,
            DA.AdapterType,
            DA.AccountName,
            DA.AccountServiceId,
            DA.URL,
            DA.BrokerAccountId,
            DA.BrokerAccountParams,
            DA.Enable
        FROM #DesiredAccounts DA
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.BrokerAccount BA
            WHERE BA.AccountId = DA.AccountId
        );

        ----------------------------------------------------------------------
        -- 4) Parse AllowedTickers and map to both account rows
        --    Ignore ticker symbols that do not exist in dbo.Ticker
        ----------------------------------------------------------------------
        IF OBJECT_ID('tempdb..#DesiredAccountTicker') IS NOT NULL DROP TABLE #DesiredAccountTicker;
        CREATE TABLE #DesiredAccountTicker
        (
            BrokerAccountId int NOT NULL,
            TickerId        int NOT NULL,
            Allowed         bit NOT NULL,
            LongLimit       int NOT NULL,
            ShortLimit       int NOT NULL,
            PRIMARY KEY (BrokerAccountId, TickerId)
        );

        ;WITH AllowedSymbols AS
        (
            SELECT
                C.AccountId AS JsonAccountId,
                LTRIM(RTRIM(J.[value])) AS Symbol
            FROM #Config C
            CROSS APPLY OPENJSON(C.AllowedTickersJsn) J
            WHERE J.[type] IN (1, 2) -- string / number; symbol expected as string
        ),
        DesiredSymbolsPerAccount AS
        (
            -- Trading account
            SELECT
                BA.Id AS BrokerAccountId,
                T.Id  AS TickerId
            FROM AllowedSymbols S
            INNER JOIN dbo.BrokerAccount BA
                ON BA.AccountId = S.JsonAccountId
            INNER JOIN dbo.Ticker T
                ON T.Symbol = S.Symbol

            UNION

            -- Tracking account
            SELECT
                BA.Id AS BrokerAccountId,
                T.Id  AS TickerId
            FROM AllowedSymbols S
            INNER JOIN dbo.BrokerAccount BA
                ON BA.AccountId = S.JsonAccountId + '_TRK'
            INNER JOIN dbo.Ticker T
                ON T.Symbol = S.Symbol
        )
        INSERT INTO #DesiredAccountTicker
        (
            BrokerAccountId,
            TickerId,
            Allowed, 
            LongLimit,
            ShortLimit
        )
        SELECT
            DSA.BrokerAccountId,
            DSA.TickerId,
            CAST(1 AS bit) AS Allowed,
            0, 0
        FROM DesiredSymbolsPerAccount DSA;

        ----------------------------------------------------------------------
        -- 5) Delete ticker mappings that are no longer allowed
        --    but only for BrokerAccount rows managed by this JSON input
        ----------------------------------------------------------------------
        IF OBJECT_ID('tempdb..#ManagedBrokerAccounts') IS NOT NULL DROP TABLE #ManagedBrokerAccounts;
        CREATE TABLE #ManagedBrokerAccounts
        (
            BrokerAccountId int NOT NULL PRIMARY KEY
        );

        INSERT INTO #ManagedBrokerAccounts (BrokerAccountId)
        SELECT BA.Id
        FROM dbo.BrokerAccount BA
        INNER JOIN #DesiredAccounts DA
            ON BA.AccountId = DA.AccountId;

        DELETE BAT
        FROM dbo.BrokerAccountTicker BAT
        INNER JOIN #ManagedBrokerAccounts MBA
            ON MBA.BrokerAccountId = BAT.BrokerAccountId
        LEFT JOIN #DesiredAccountTicker DAT
            ON DAT.BrokerAccountId = BAT.BrokerAccountId
           AND DAT.TickerId = BAT.TickerId
        WHERE DAT.BrokerAccountId IS NULL;

        ----------------------------------------------------------------------
        -- 6) Upsert desired ticker mappings
        ----------------------------------------------------------------------
        UPDATE BAT
           SET BAT.Allowed = DAT.Allowed
        FROM dbo.BrokerAccountTicker BAT
        INNER JOIN #DesiredAccountTicker DAT
            ON DAT.BrokerAccountId = BAT.BrokerAccountId
           AND DAT.TickerId = BAT.TickerId;

        INSERT INTO dbo.BrokerAccountTicker
        (
            BrokerAccountId,
            TickerId,
            Allowed, 
            LongLimit, 
            ShortLimit
        )
        SELECT
            DAT.BrokerAccountId,
            DAT.TickerId,
            DAT.Allowed,
            DAT.LongLimit,
            DAT.ShortLimit
        FROM #DesiredAccountTicker DAT
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.BrokerAccountTicker BAT
            WHERE BAT.BrokerAccountId = DAT.BrokerAccountId
              AND BAT.TickerId = DAT.TickerId
        );

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        THROW;
    END CATCH
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccountticker_delete]
(
	@BrokerAccountId int,
	@TickerId int
)

AS
BEGIN
	DELETE FROM [BrokerAccountTicker] WHERE @BrokerAccountId = [BrokerAccountId] AND @TickerId = [TickerId]
END


GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccountticker_select]
(
	@BrokerAccountId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [BrokerAccountId]
		  ,[TickerId]
		  ,[Allowed]
		  ,[LongLimit]
		  ,[ShortLimit]
		FROM [BrokerAccountTicker]
		WHERE @BrokerAccountId = -1 OR @BrokerAccountId = [BrokerAccountId]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_brokeraccountticker_upsert]
(
	@IdNew int OUTPUT,
	@BrokerAccountId int,
	@TickerId int,
	@Allowed bit,
	@LongLimit int,
	@ShortLimit int
)

AS
BEGIN

	-- Check if Symbol exists
	IF (NOT EXISTS(SELECT 1 FROM [BrokerAccountTicker] WHERE @BrokerAccountId = [BrokerAccountId] AND @TickerId = [TickerId] ))
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [dbo].[BrokerAccountTicker]
           ([BrokerAccountId]
           ,[TickerId]
           ,[Allowed]
           ,[LongLimit]
           ,[ShortLimit])
		 VALUES
			   (@BrokerAccountId
			   ,@TickerId
			   ,@Allowed
			   ,@LongLimit
			   ,@ShortLimit)
	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [BrokerAccountTicker] SET 
			  [Allowed] = @Allowed
			, [LongLimit] = @LongLimit
			, [ShortLimit] = @ShortLimit
		WHERE @BrokerAccountId = [BrokerAccountId] AND @TickerId = [TickerId];
	END

	SET @IdNew = 1;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_order_insert]
(
	@IdNew int OUTPUT,
	@Id int,
	@BrokerAccountId int,
	@OrderRefId varchar(256),
	@TickerId int,
	@OrderTagRef varchar(50),
	@OrderTag varchar(50),
	@OrderType varchar(50),
	@Qty int,
	@Price decimal(18,4),
	@StopPrice decimal(18,4),
	@OrderStatus int,
	@FillQty int,
	@FillPrice decimal(18,4),
	@CancelRequested bit,
	@BrokerRef varchar(256),
	@BrokerOrderId varchar(256),
	@BrokerParams varchar(max),
	@OrderTime int,
	@CompletionTime int,
	@LastUpdateTime int
)

AS
BEGIN
	DECLARE @CurrentTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	SET @OrderTime = CASE WHEN @OrderTime = -1 THEN @CurrentTime ELSE @OrderTime END;
	SET @CompletionTime = CASE WHEN @CompletionTime = -1 THEN @CurrentTime ELSE @CompletionTime END;
	SET @LastUpdateTime = @CurrentTime;
	SET @OrderTagRef = Trim(Upper(@OrderTagRef));
	SET @OrderTag = Trim(Upper(@OrderTag));
	SET @OrderType = Trim(Upper(@OrderType));

	INSERT INTO [dbo].[Order]
    (
         [BrokerAccountId]
		,[OrderRefId]
		,[TickerId]
		,[OrderTagRef]
		,[OrderTag]
		,[OrderType]
		,[Qty]
		,[Price]
		,[StopPrice]
		,[OrderStatus]
		,[FillQty]
		,[FillPrice]
		,[CancelRequested]
		,[BrokerRef]
		,[BrokerOrderId]
		,[BrokerParams]
		,[OrderTime]
		,[CompletionTime]
		,[LastUpdateTime]
    )
    VALUES
    (
         @BrokerAccountId
		,@OrderRefId
		,@TickerId
		,@OrderTagRef
		,@OrderTag
		,@OrderType
		,@Qty
		,@Price
		,@StopPrice
		,@OrderStatus
		,@FillQty
		,@FillPrice
		,@CancelRequested
		,@BrokerRef
		,@BrokerOrderId
		,@BrokerParams
		,@OrderTime
		,@CompletionTime
		,@LastUpdateTime
    );

	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_active]
(
    @BrokerAccountId varchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT O.[Id]
          ,O.[BrokerAccountId]
          ,O.[OrderRefId]
          ,O.[TickerId]
          ,O.[OrderTagRef]
          ,O.[OrderTag]
          ,O.[OrderType]
          ,O.[Qty]
          ,O.[Price]
          ,O.[StopPrice]
          ,O.[OrderStatus]
          ,O.[FillQty]
          ,O.[FillPrice]
          ,O.[CancelRequested]
          ,O.[BrokerRef]
          ,O.[BrokerOrderId]
          ,O.[BrokerParams]
          ,O.[OrderTime]
          ,O.[CompletionTime]
          ,O.[LastUpdateTime]
        FROM [dbo].[Order] AS O
            INNER JOIN [dbo].[BrokerAccount] AS BA ON O.[BrokerAccountId] = BA.[Id]
        -- query INPROGRESS(1), INPROGRESS_TIMEOUT(2), INPROGRESS_SUBMITTED(3) and PARTIALLY_FILLED(4)
	    WHERE O.[OrderStatus] in (1, 2, 3, 4, 11) AND len(O.[BrokerRef]) > 0
                AND (len(@BrokerAccountId) = 0 OR @BrokerAccountId = BA.[AccountId])
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_active_by_service_id]
(
    @ServiceId varchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SET @ServiceId = upper(trim(@ServiceId))

    SELECT O.[Id]
          ,O.[BrokerAccountId]
          ,O.[OrderRefId]
          ,O.[TickerId]
          ,O.[OrderTagRef]
          ,O.[OrderTag]
          ,O.[OrderType]
          ,O.[Qty]
          ,O.[Price]
          ,O.[StopPrice]
          ,O.[OrderStatus]
          ,O.[FillQty]
          ,O.[FillPrice]
          ,O.[CancelRequested]
          ,O.[BrokerRef]
          ,O.[BrokerOrderId]
          ,O.[BrokerParams]
          ,O.[OrderTime]
          ,O.[CompletionTime]
          ,O.[LastUpdateTime]
        FROM [dbo].[Order] AS O
            INNER JOIN [dbo].[BrokerAccount] AS BA ON O.[BrokerAccountId] = BA.[Id]
        -- query INPROGRESS(1), INPROGRESS_TIMEOUT(2), INPROGRESS_SUBMITTED(3) and PARTIALLY_FILLED(4)
	    WHERE O.[OrderStatus] in (1, 2, 3, 4, 11) -- AND len(O.[BrokerRef]) > 0
                AND (len(@ServiceId) = 0 OR @ServiceId = upper(trim(BA.[AccountServiceId])))
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_by_brokeraccount]
(
    @BrokerAccountId int,
    @FromTime int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

SELECT [Id]
      ,[BrokerAccountId]
      ,[OrderRefId]
      ,[TickerId]
      ,[OrderTagRef]
      ,[OrderTag]
      ,[OrderType]
      ,[Qty]
      ,[Price]
      ,[StopPrice]
      ,[OrderStatus]
      ,[FillQty]
      ,[FillPrice]
      ,[CancelRequested]
      ,[BrokerRef]
      ,[BrokerOrderId]
      ,[BrokerParams]
      ,[OrderTime]
      ,[CompletionTime]
      ,[LastUpdateTime]
  FROM [Order]
  WHERE [BrokerAccountId] = @BrokerAccountId AND [OrderTime] >= @FromTime;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_by_brokerorderid]
(
    @BrokerOrderId varchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT TOP 1
           [Id]
          ,[BrokerAccountId]
          ,[OrderRefId]
          ,[TickerId]
          ,[OrderTagRef]
          ,[OrderTag]
          ,[OrderType]
          ,[Qty]
          ,[Price]
          ,[StopPrice]
          ,[OrderStatus]
          ,[FillQty]
          ,[FillPrice]
          ,[CancelRequested]
          ,[BrokerRef]
          ,[BrokerOrderId]
          ,[BrokerParams]
          ,[OrderTime]
          ,[CompletionTime]
          ,[LastUpdateTime]
        FROM [dbo].[Order]
	    WHERE [BrokerOrderId] = @BrokerOrderId
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_by_brokerref]
(
    @BrokerRef varchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT TOP 1
           [Id]
          ,[BrokerAccountId]
          ,[OrderRefId]
          ,[TickerId]
          ,[OrderTagRef]
          ,[OrderTag]
          ,[OrderType]
          ,[Qty]
          ,[Price]
          ,[StopPrice]
          ,[OrderStatus]
          ,[FillQty]
          ,[FillPrice]
          ,[CancelRequested]
          ,[BrokerRef]
          ,[BrokerOrderId]
          ,[BrokerParams]
          ,[OrderTime]
          ,[CompletionTime]
          ,[LastUpdateTime]
        FROM [dbo].[Order]
	    WHERE [BrokerRef] = @BrokerRef
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_by_order_tag]
(
    @BrokerAccountId int,
    @OrderTagRef varchar(50),
    @OrderTag varchar(50)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

SET @OrderTagRef = trim(upper(@OrderTagRef));
SET @OrderTag = trim(upper(@OrderTag));

SELECT [Id]
      ,[BrokerAccountId]
      ,[OrderRefId]
      ,[TickerId]
      ,[OrderTagRef]
      ,[OrderTag]
      ,[OrderType]
      ,[Qty]
      ,[Price]
      ,[StopPrice]
      ,[OrderStatus]
      ,[FillQty]
      ,[FillPrice]
      ,[CancelRequested]
      ,[BrokerRef]
      ,[BrokerOrderId]
      ,[BrokerParams]
      ,[OrderTime]
      ,[CompletionTime]
      ,[LastUpdateTime]
  FROM [Order]
  WHERE [BrokerAccountId] = @BrokerAccountId 
        AND [OrderTagRef] = @OrderTagRef
        AND [OrderTag] = @OrderTag;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_query_by_orderref]
(
    @BrokerAccountId int,
    @OrderRefId varchar(256)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

SELECT [Id]
      ,[BrokerAccountId]
      ,[OrderRefId]
      ,[TickerId]
      ,[OrderTagRef]
      ,[OrderTag]
      ,[OrderType]
      ,[Qty]
      ,[Price]
      ,[StopPrice]
      ,[OrderStatus]
      ,[FillQty]
      ,[FillPrice]
      ,[CancelRequested]
      ,[BrokerRef]
      ,[BrokerOrderId]
      ,[BrokerParams]
      ,[OrderTime]
      ,[CompletionTime]
      ,[LastUpdateTime]
  FROM [Order]
  WHERE [BrokerAccountId] = @BrokerAccountId AND [OrderRefId] = @OrderRefId;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_order_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [Id]
          ,[BrokerAccountId]
          ,[OrderRefId]
          ,[TickerId]
          ,[OrderTagRef]
          ,[OrderTag]
          ,[OrderType]
          ,[Qty]
          ,[Price]
          ,[StopPrice]
          ,[OrderStatus]
          ,[FillQty]
          ,[FillPrice]
          ,[CancelRequested]
          ,[BrokerRef]
          ,[BrokerOrderId]
          ,[BrokerParams]
          ,[OrderTime]
          ,[CompletionTime]
          ,[LastUpdateTime]
        FROM [dbo].[Order]
	    WHERE @Id = -1 OR @Id = Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_order_update_cancel_status]
(
    @Id            int,
    @OrderStatus   int
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CurrentTime int = DATEDIFF(SECOND, '1970-01-01T00:00:00', GETUTCDATE());
    DECLARE @CurStatus int;
    SELECT @CurStatus = [OrderStatus] FROM [Order] WHERE [Id] = @Id;

    IF (@CurStatus between 0 and 9)   -- still in progress 
    BEGIN
        IF (@OrderStatus = 10 OR @OrderStatus = -4) -- success or FAIL_CANCELED
        BEGIN
            SET @OrderStatus = -4;  -- FAIL_CANCELED
        END
        ELSE IF (@OrderStatus between 0 and 9 ) -- order status is in-progress
        BEGIN
            SET @OrderStatus = 5;  -- INPROGRESS_CANCEL
        END
        ELSE SET @OrderStatus = @CurStatus;
    END
    ELSE IF (@CurStatus = 11)   -- partially-filled
    BEGIN
        IF (@OrderStatus = 10 OR @OrderStatus = -4) -- success
        BEGIN
            SET @OrderStatus = 12;  -- PARTIALLY_FILLED_FINAL
        END
        ELSE SET @OrderStatus = @CurStatus;
    END
    ELSE SET @OrderStatus = @CurStatus;


    UPDATE dbo.[Order]
        SET
            [OrderStatus]     = @OrderStatus,
            [CompletionTime]  = CASE WHEN @OrderStatus in (10, 12, 13) OR @OrderStatus < 0  -- Failures or Filled
                                        THEN @CurrentTime ELSE [CompletionTime] END,
            [LastUpdateTime]  = @CurrentTime,
            [CancelRequested] = 1
        WHERE [Id] = @Id;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_order_update_status]
(
    @Id            int,
    @OrderStatus   int,
    @FilledQty     int,
    @FilledPrice   decimal(18,4),
    @BrokerRef     varchar(256),
    @BrokerOrderId varchar(256),
    @BrokerParams  varchar(max)
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CurrentTime int = DATEDIFF(SECOND, '1970-01-01T00:00:00', GETUTCDATE());

    UPDATE dbo.[Order]
        SET
            [OrderStatus]     = @OrderStatus,
            [BrokerRef]      = CASE WHEN 
                                    (([BrokerRef] = '' OR [BrokerRef] = '0') AND len(@BrokerRef) > 0)
                                    THEN @BrokerRef ELSE [BrokerRef] END,
            [BrokerOrderId]      = CASE WHEN 
                                    (([BrokerOrderId] = '' OR [BrokerOrderId] = '0') AND len(@BrokerOrderId) > 0)
                                    THEN @BrokerOrderId ELSE [BrokerOrderId] END,
            [BrokerParams]      = @BrokerParams,
            [CompletionTime]  = CASE WHEN @OrderStatus IN (11,12,13) OR @OrderStatus < 0  -- Failures or Filled
                                        THEN @CurrentTime ELSE [CompletionTime] END,
            [LastUpdateTime]  = @CurrentTime,
            [FillQty]         = CASE WHEN abs(@FilledQty) > abs([FillQty]) THEN @FilledQty ELSE [FillQty] END,
            [FillPrice]       = CASE WHEN @FilledPrice > 0 THEN @FilledPrice ELSE [FillPrice] END
        WHERE [Id] = @Id;

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
CREATE PROCEDURE [dbo].[usp_ticker_query]
(
	@Symbol varchar(50),
	@Exchange varchar(100),
	@ContractExpiry int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @Symbol = TRIM(UPPER(@Symbol));
	SET @Exchange = TRIM(UPPER(@Exchange));

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
		FROM [Ticker]
		WHERE @Symbol = [Symbol] AND @Exchange = [Exchange] AND @ContractExpiry = [ExpiryDate]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_ticker_select]
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
		FROM [Ticker]
		WHERE @Id = -1 OR @Id = Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_ticker_select_by_Symbol]
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
		FROM [Ticker]
		WHERE @Symbol = [Symbol]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_ticker_upsert]
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
	@Exchange varchar(100)
)

AS
BEGIN

	SET @Symbol = TRIM(UPPER(@Symbol));
	SET @Exchange = TRIM(UPPER(@Exchange));

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [Ticker] WHERE @Symbol = [Symbol];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Ticker] (
			  [Symbol]
			, [Name]
			, [LocalSymbol]
			, [SecurityType]
			, [Currency]
			, [ExpiryDate]
			, [MinTick]
			, [ContractSize]
			, [Exchange]
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
		)
		SET @IdNew = SCOPE_IDENTITY();  
	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [Ticker] SET 
			  [Symbol] = @Symbol
			, [LocalSymbol] = @LocalSymbol
			, [Name] = @Name
			, [SecurityType] = @SecurityType
			, [Currency] = @Currency
			, [ExpiryDate] = @ExpiryDate
			, [MinTick] = @MinTick
			, [ContractSize] = @ContractSize
			, [Exchange] = @Exchange
		WHERE Id = @IdNew;
	END

	RETURN @IdNew;
END

GO
INSERT [dbo].[BrokerAdapter] ([Type], [Description]) VALUES (N'IBKR', N'Interactive Broker')
GO
SET IDENTITY_INSERT [dbo].[BrokerAccount] ON 
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (1, N'ROOTS', N'IBKR', N'ROOTS IBKR Account', N'07C09868-BC3F-4130-BF56-E7C4B71F4ED4', N'127.0.0.1:7496', N'U3416760', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (2, N'ROOTS_CLASS_D', N'IBKR', N'ROOTS-ClassD IBKR Account', N'8D8C0E86-DE31-4EC2-A3FD-7FD242A1B5C3', N'127.0.0.1:7496', N'U4490376', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (3, N'SEEDS', N'IBKR', N'SEEDS IBKR Account', N'035E9A70-AAE9-4629-A634-4A26AF832237', N'127.0.0.1:7496', N'U4490376', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (4, N'ALALFA', N'IBKR', N'AL_ALFA IBKR Account', N'F1476D7B-CD35-4B51-BC7B-F8FBF766AB8B', N'127.0.0.1:7496', N'U4890118', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (5, N'MARGIN1', N'IBKR', N'MARGIN1 IBKR Account', N'C4E09239-8998-4553-B545-0B9B02F875BA', N'127.0.0.1:7496', N'U24865288', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (6, N'MARGIN2', N'IBKR', N'MARGIN2 IBKR Account', N'7A2AC41E-5010-4ACF-8ECA-AE03353438CB', N'127.0.0.1:7496', N'U23146913', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (7, N'FIRST', N'IBKR', N'FIRST IBKR Account', N'F40C1BB9-1BAA-4FE6-A495-5BB226F0B951', N'127.0.0.1:7496', N'U9533211', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (9, N'DUBAI', N'IBKR', N'DUBAI IBKR Account', N'91A38075-8623-46E3-95E5-1E00B12D7CCA', N'127.0.0.1:7496', N'U10565325', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (10, N'TAREQ', N'IBKR', N'TAREQ IBKR Account', N'3C43E7FF-2B6D-42C3-87DD-2322FD42A3DE', N'127.0.0.1:7496', N'U23117421', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
INSERT [dbo].[BrokerAccount] ([Id], [AccountId], [AdapterType], [AccountName], [AccountServiceId], [URL], [BrokerAccountId], [BrokerAccountParams], [Enable]) VALUES (11, N'SEEDS-NEW', N'IBKR', N'SEEDS-NEW IBKR Account', N'518A4CFB-D751-4586-B732-14CABBF1CA7D', N'127.0.0.1:7496', N'U6723474', N'{"client_id": 601, "tracking_client_id":605}', 1)
GO
SET IDENTITY_INSERT [dbo].[BrokerAccount] OFF
GO
SET IDENTITY_INSERT [dbo].[Ticker] ON 
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [LocalSymbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange]) VALUES (1, N'NQ', N'NQM6', N'E-mini NASDAQ 100 ', N'FUT', N'USD', 20260918, CAST(0.250000 AS Decimal(18, 6)), 20, N'CME')
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [LocalSymbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange]) VALUES (2, N'MNQ', N'MNQH6', N'Micro E-Mini Nasdaq-100 Index', N'FUT', N'USD', 20260918, CAST(0.250000 AS Decimal(18, 6)), 2, N'CME')
GO
SET IDENTITY_INSERT [dbo].[Ticker] OFF
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (1, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (1, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (2, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (2, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (3, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (3, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (4, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (4, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (5, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (5, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (6, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (6, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (7, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (7, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (9, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (9, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (10, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (10, 2, 1, 100, -100)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (11, 1, 1, 30, -30)
GO
INSERT [dbo].[BrokerAccountTicker] ([BrokerAccountId], [TickerId], [Allowed], [LongLimit], [ShortLimit]) VALUES (11, 2, 1, 100, -100)
GO
