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
CREATE FUNCTION [dbo].[fn_split_string]
(
    @in_string VARCHAR(MAX),
    @delimeter VARCHAR(1)
)
RETURNS @list TABLE(field VARCHAR(100))
AS
BEGIN
        WHILE LEN(@in_string) > 0
        BEGIN
            INSERT INTO @list(field)
            SELECT left(@in_string, charindex(@delimeter, @in_string+',') -1) as tuple
    
            SET @in_string = stuff(@in_string, 1, charindex(@delimeter, @in_string + @delimeter), '')
        end
    RETURN 
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Trade](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AutoTradeId] [int] NOT NULL,
	[BotId] [int] NOT NULL,
	[TickerId] [int] NOT NULL,
	[StartRawTime] [int] NOT NULL,
	[EndRawTime] [int] NOT NULL,
	[Strategy] [varchar](100) NOT NULL,
	[SignalId] [varchar](50) NOT NULL,
	[Side] [decimal](18, 4) NOT NULL,
	[StrategyParam] [varchar](100) NOT NULL,
	[OrderRef] [varchar](36) NOT NULL,
	[OrderTag] [varchar](50) NOT NULL,
	[OrderType] [varchar](50) NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [decimal](18, 4) NULL,
	[TradeStatus] [int] NOT NULL,
	[FillQty] [int] NOT NULL,
	[FillPrice] [decimal](18, 4) NULL,
	[LastRawTime] [int] NOT NULL,
 CONSTRAINT [PK_Trade] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TrackData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AutoTradeId] [int] NOT NULL,
	[RawTime] [int] NOT NULL,
	[Open] [decimal](18, 4) NOT NULL,
	[High] [decimal](18, 4) NOT NULL,
	[Low] [decimal](18, 4) NOT NULL,
	[Close] [decimal](18, 4) NOT NULL,
	[Params] [varchar](max) NOT NULL,
	[ActiveTrades] [varchar](max) NOT NULL,
	[StrategyData] [varchar](max) NOT NULL,
	[CurrentSides] [varchar](max) NOT NULL,
	[TradeQty] [int] NOT NULL,
	[TradeFillPrice] [decimal](18, 4) NOT NULL,
	[TradeValue] [decimal](18, 4) NOT NULL,
	[TradeCommission] [decimal](18, 4) NOT NULL,
	[TradeCumulativeData] [varchar](max) NOT NULL,
 CONSTRAINT [PK_TrackData] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutoTrade](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](256) NOT NULL,
	[Ver] [int] NOT NULL,
	[TickerId] [int] NOT NULL,
	[IntervalId] [varchar](10) NOT NULL,
	[BotId] [int] NOT NULL,
	[IsLive] [bit] NOT NULL,
	[StartTime] [int] NOT NULL,
	[LiveStatus] [int] NOT NULL,
	[AUM] [money] NOT NULL,
	[Commission] [money] NOT NULL,
	[Params] [varchar](max) NOT NULL,
 CONSTRAINT [PK_AutoTrade] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO





CREATE VIEW [dbo].[VTrackDataLive]
AS
SELECT	 TD.[Id]
		,TD.[AutoTradeId]
		,TD.[RawTime]
		,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](TD.[RawTime], 'Arabian Standard Time') AS [PriceDateDubai]
		,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](TD.[RawTime], 'Eastern Standard Time') AS [PriceDateEST]
		,CAST(TD.[Open] AS decimal(18,2)) AS [Open]
		,CAST(TD.[High] AS decimal(18,2)) AS [High]
		,CAST(TD.[Low] AS decimal(18,2)) AS [Low]
		,CAST(TD.[Close] AS decimal(18,2)) AS [Close]
		,TD.[StrategyData]
		,TD.[CurrentSides]
		,ISNULL(T.[Strategy], '') AS [TStrategy]
		,T.[Side] AS [TSide]
		,ISNULL(T.[OrderTag], '') AS [TOrderTag]
		,ISNULL(T.[Price], 0) AS [Price]
		,ISNULL(T.[FillQty], 0) AS [FillQty]
		,ISNULL(T.[FillPrice], 0) AS [FillPrice]
		,ISNULL(T.[TradeStatus], 0) AS [TradeStatus]
		
	FROM [TrackData] AS TD
		INNER JOIN [AutoTrade] ON TD.[AutoTradeId] = [AutoTrade].[Id]
		LEFT JOIN [Trade] AS T ON T.[AutoTradeId] = TD.[AutoTradeId] AND T.[StartRawTime] = TD.[RawTime]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE VIEW [dbo].[VTrackData]
AS
SELECT	 TD.[Id]
		,TD.[AutoTradeId]
		,TD.[RawTime]
		,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](TD.[RawTime], 'Arabian Standard Time') AS [PriceDateDubai]
		,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](TD.[RawTime], 'Eastern Standard Time') AS [PriceDateEST]
		,CAST(TD.[Open] AS decimal(18,2)) AS [Open]
		,CAST(TD.[High] AS decimal(18,2)) AS [High]
		,CAST(TD.[Low] AS decimal(18,2)) AS [Low]
		,CAST(TD.[Close] AS decimal(18,2)) AS [Close]
		,TD.Params
		,TD.ActiveTrades
		,TD.StrategyData
		,TD.CurrentSides
		,TD.[TradeQty]
		,TD.[TradeFillPrice]
   		,TD.[TradeValue]
   		,TD.[TradeCommission]
		,TD.[TradeCumulativeData]
		,CData.[DealQty] AS [AcumPos]
		,CData.[DealVal] AS [AcumVal]
		,CData.[DealCommission] AS [AcumCommission]
		,CData.[DealClosePL] AS [NetClosePL]
		,CData.[DealOpenPL] AS [NetOpenPL]
		,CAST((ISNULL(CData.[DealClosePL], 0) + ISNULL(CData.[DealOpenPL], 0) + ISNULL(CData.[DealCommission], 0)) AS decimal(18,2)) AS [Net]
		,CAST((([AutoTrade].AUM + ISNULL(CData.[DealClosePL], 0) + ISNULL(CData.[DealOpenPL], 0) + ISNULL(CData.[DealCommission], 0))  * 100.0 / [AutoTrade].AUM) AS decimal(18,2))  AS [NAV]
	FROM [TrackData] AS TD
		INNER JOIN [AutoTrade] ON TD.[AutoTradeId] = [AutoTrade].[Id]
		OUTER APPLY OPENJSON(TD.[TradeCumulativeData]) 
			WITH ( [DealQty] DECIMAL(18,4) 'strict $.DealQty'
			      ,[DealVal] DECIMAL(18,4) 'strict $.DealVal'
			      ,[DealCommission] DECIMAL(18,4) 'strict $.DealCommission'
			      ,[DealClosePL] DECIMAL(18,4) 'strict $.DealClosePL'
			      ,[DealOpenPL] DECIMAL(18,4) 'strict $.DealOpenPL'
				 ) AS CData 
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DataSet](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TickerId] [int] NOT NULL,
	[IntervalId] [varchar](10) NOT NULL,
 CONSTRAINT [PK_DataSet] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PriceData](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DataSetId] [int] NOT NULL,
	[RawTime] [int] NOT NULL,
	[PriceDate] [datetime] NOT NULL,
	[Open] [decimal](18, 4) NOT NULL,
	[High] [decimal](18, 4) NOT NULL,
	[Low] [decimal](18, 4) NOT NULL,
	[Close] [decimal](18, 4) NOT NULL,
	[Volume] [int] NOT NULL,
 CONSTRAINT [PK_PriceData] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_PriceData] UNIQUE NONCLUSTERED 
(
	[DataSetId] ASC,
	[RawTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Ticker](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Symbol] [varchar](50) NOT NULL,
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









CREATE VIEW [dbo].[VPriceData]
AS

SELECT pricedata.[Id]
      ,[DataSetId]
	  ,Ticker.Symbol
      ,[RawTime]
      ,[PriceDate] 
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([RawTime], 'UTC') AS [PriceDateUTC]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([RawTime], 'Eastern Standard Time') AS [PriceDateEST]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([RawTime], 'Arabian Standard Time') AS [PriceDateDubai]
      ,[Open]
      ,[High]
      ,[Low]
      ,[Close]
      ,[Volume]
  FROM [PriceData]
	  INNER JOIN DataSet ON DataSet.Id = PriceData.DataSetId
	  INNER JOIN Ticker ON DataSet.TickerId = Ticker.Id


GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StudyHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StudyColId] [int] NOT NULL,
	[RawTime] [int] NOT NULL,
	[Open] [decimal](18, 4) NOT NULL,
	[High] [decimal](18, 4) NOT NULL,
	[Low] [decimal](18, 4) NOT NULL,
	[Close] [decimal](18, 4) NOT NULL,
	[Volume] [int] NOT NULL,
	[Studies] [varchar](max) NOT NULL,
 CONSTRAINT [PK_StudyHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




CREATE VIEW [dbo].[VStudyHistory]
AS

SELECT H.[Id]
      ,H.[StudyColId]
      ,H.[RawTime]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](H.[RawTime], 'UTC') AS [PriceDateUTC]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](H.[RawTime], 'Eastern Standard Time') AS [PriceDateEST]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](H.[RawTime], 'Arabian Standard Time') AS [PriceDateDubai]
      ,H.[Open]
      ,H.[High]
      ,H.[Low]
      ,H.[Close]
      ,H.[Volume]
	  ,CData.SEMC5
	  ,CData.UBC5
      ,H.[Studies]
  FROM [StudyHistory] AS H
	  CROSS APPLY OPENJSON(H.[Studies]) 
				WITH ( [SEMC5] DECIMAL(18,4) 'strict $.SEMC5',
					   [UBC5] DECIMAL(18,4) 'strict $.UBC5'
					 ) AS CData 


GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutoTradeStrategy](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AutoTradeId] [int] NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Config] [varchar](max) NOT NULL,
	[Qty] [int] NOT NULL,
	[QtyPct] [float] NOT NULL,
	[AwayAdj] [decimal](18, 4) NOT NULL,
	[Enabled] [bit] NOT NULL,
 CONSTRAINT [PK_AutoTradeStrategy] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bot](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[RefId] [varchar](50) NOT NULL,
	[Config] [varchar](max) NOT NULL,
 CONSTRAINT [PK_Bot] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DefaultParameter](
	[Tag] [varchar](50) NOT NULL,
	[Desc] [varchar](256) NOT NULL,
	[DefaultValue] [varchar](256) NOT NULL,
	[ValueType] [varchar](10) NOT NULL,
	[Fixed] [bit] NOT NULL,
 CONSTRAINT [PK_DefaultParameters] PRIMARY KEY CLUSTERED 
(
	[Tag] ASC
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
CREATE TABLE [dbo].[IBKRInterval](
	[IntervalId] [varchar](10) NOT NULL,
	[IBKRInterval] [varchar](50) NOT NULL,
 CONSTRAINT [PK_IBInterval] PRIMARY KEY CLUSTERED 
(
	[IntervalId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Interval](
	[Id] [varchar](10) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[Seq] [int] NOT NULL,
	[IntervalLen] [int] NOT NULL,
 CONSTRAINT [PK_Interval] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PriceSrc](
	[Id] [int] NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Value] [varchar](20) NOT NULL,
 CONSTRAINT [PK_PriceSrc] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Setting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Group] [varchar](50) NOT NULL,
	[Tag] [varchar](100) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Setting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Strategy](
	[Name] [varchar](100) NOT NULL,
	[Desc] [varchar](1024) NOT NULL,
 CONSTRAINT [PK_Strategy] PRIMARY KEY CLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StudyCol](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TickerId] [int] NOT NULL,
	[IntervalId] [varchar](10) NOT NULL,
	[C21] [int] NOT NULL,
	[StdDev1] [decimal](18, 4) NOT NULL,
	[StdDev2] [decimal](18, 4) NOT NULL,
	[StdDev3] [decimal](18, 4) NOT NULL,
	[IsLive] [bit] NOT NULL,
	[Enable] [bit] NOT NULL,
	[CompileFull] [bit] NOT NULL,
 CONSTRAINT [PK_StudyCol] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TradeOrder](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TradeId] [int] NOT NULL,
	[RawTime] [int] NOT NULL,
	[Action] [varchar](100) NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [decimal](18, 4) NULL,
	[FillQty] [int] NOT NULL,
	[FillPrice] [decimal](18, 4) NULL,
	[Status] [int] NOT NULL,
	[SyncId] [int] NOT NULL,
	[BrokerRef] [varchar](100) NULL,
	[LastRawTime] [int] NOT NULL,
 CONSTRAINT [PK_TradeOrder] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AutoTrade_UniqueName] ON [dbo].[AutoTrade]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_DataSet_TickerId_IntervalId] ON [dbo].[DataSet]
(
	[TickerId] ASC,
	[IntervalId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_PriceData_DataSetId] ON [dbo].[PriceData]
(
	[DataSetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_PriceData_RawTime] ON [dbo].[PriceData]
(
	[RawTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_StudyCol_Params] ON [dbo].[StudyCol]
(
	[TickerId] ASC,
	[IntervalId] ASC,
	[C21] ASC,
	[StdDev1] ASC,
	[StdDev2] ASC,
	[StdDev3] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_StudyHistory_StudyColId_RawTime] ON [dbo].[StudyHistory]
(
	[StudyColId] ASC,
	[RawTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_Ver]  DEFAULT ((1)) FOR [Ver]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_BotId]  DEFAULT ((-1)) FOR [BotId]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_StartTime]  DEFAULT ((0)) FOR [StartTime]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_Status]  DEFAULT ((0)) FOR [LiveStatus]
GO
ALTER TABLE [dbo].[DefaultParameter] ADD  CONSTRAINT [DF_DefaultParameters_Desc]  DEFAULT ('') FOR [Desc]
GO
ALTER TABLE [dbo].[DefaultParameter] ADD  CONSTRAINT [DF_DefaultParameters_DefaultValue]  DEFAULT ('') FOR [DefaultValue]
GO
ALTER TABLE [dbo].[DefaultParameter] ADD  CONSTRAINT [DF_DefaultParameters_ValueType]  DEFAULT ('STRING') FOR [ValueType]
GO
ALTER TABLE [dbo].[DefaultParameter] ADD  CONSTRAINT [DF_DefaultParameters_Fixed]  DEFAULT ((0)) FOR [Fixed]
GO
ALTER TABLE [dbo].[TrackData] ADD  CONSTRAINT [DF_TrackData_Params]  DEFAULT ('{}') FOR [Params]
GO
ALTER TABLE [dbo].[TrackData] ADD  CONSTRAINT [DF_TrackData_ActiveTrades]  DEFAULT ('{}') FOR [ActiveTrades]
GO
ALTER TABLE [dbo].[TrackData] ADD  CONSTRAINT [DF_TrackData_StrategyData]  DEFAULT ('{}') FOR [StrategyData]
GO
ALTER TABLE [dbo].[TrackData] ADD  CONSTRAINT [DF_TrackData_CurrentSides]  DEFAULT ('{}') FOR [CurrentSides]
GO
ALTER TABLE [dbo].[TrackData] ADD  CONSTRAINT [DF_TrackData_TradeFillPrice]  DEFAULT ((0)) FOR [TradeFillPrice]
GO
ALTER TABLE [dbo].[TrackData] ADD  CONSTRAINT [DF_TrackData_CumulativeParams]  DEFAULT ('{}') FOR [TradeCumulativeData]
GO
ALTER TABLE [dbo].[Trade] ADD  CONSTRAINT [DF_Trade_EndRawTime]  DEFAULT ((0)) FOR [EndRawTime]
GO
ALTER TABLE [dbo].[Trade] ADD  CONSTRAINT [DF_Trade_side]  DEFAULT ((0.0)) FOR [Side]
GO
ALTER TABLE [dbo].[AutoTrade]  WITH CHECK ADD  CONSTRAINT [FK_AutoTrade_Bot] FOREIGN KEY([BotId])
REFERENCES [dbo].[Bot] ([Id])
GO
ALTER TABLE [dbo].[AutoTrade] CHECK CONSTRAINT [FK_AutoTrade_Bot]
GO
ALTER TABLE [dbo].[AutoTrade]  WITH CHECK ADD  CONSTRAINT [FK_AutoTrade_Interval] FOREIGN KEY([IntervalId])
REFERENCES [dbo].[Interval] ([Id])
GO
ALTER TABLE [dbo].[AutoTrade] CHECK CONSTRAINT [FK_AutoTrade_Interval]
GO
ALTER TABLE [dbo].[AutoTrade]  WITH CHECK ADD  CONSTRAINT [FK_AutoTrade_Ticker] FOREIGN KEY([TickerId])
REFERENCES [dbo].[Ticker] ([Id])
GO
ALTER TABLE [dbo].[AutoTrade] CHECK CONSTRAINT [FK_AutoTrade_Ticker]
GO
ALTER TABLE [dbo].[AutoTradeStrategy]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeStrategy_AutoTrade] FOREIGN KEY([AutoTradeId])
REFERENCES [dbo].[AutoTrade] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AutoTradeStrategy] CHECK CONSTRAINT [FK_AutoTradeStrategy_AutoTrade]
GO
ALTER TABLE [dbo].[AutoTradeStrategy]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeStrategy_Strategy] FOREIGN KEY([Name])
REFERENCES [dbo].[Strategy] ([Name])
GO
ALTER TABLE [dbo].[AutoTradeStrategy] CHECK CONSTRAINT [FK_AutoTradeStrategy_Strategy]
GO
ALTER TABLE [dbo].[DataSet]  WITH CHECK ADD  CONSTRAINT [FK_DataSet_Interval] FOREIGN KEY([IntervalId])
REFERENCES [dbo].[Interval] ([Id])
GO
ALTER TABLE [dbo].[DataSet] CHECK CONSTRAINT [FK_DataSet_Interval]
GO
ALTER TABLE [dbo].[DataSet]  WITH CHECK ADD  CONSTRAINT [FK_DataSet_Ticker] FOREIGN KEY([TickerId])
REFERENCES [dbo].[Ticker] ([Id])
GO
ALTER TABLE [dbo].[DataSet] CHECK CONSTRAINT [FK_DataSet_Ticker]
GO
ALTER TABLE [dbo].[PriceData]  WITH NOCHECK ADD  CONSTRAINT [FK_PriceData_DataSet] FOREIGN KEY([DataSetId])
REFERENCES [dbo].[DataSet] ([Id])
GO
ALTER TABLE [dbo].[PriceData] CHECK CONSTRAINT [FK_PriceData_DataSet]
GO
ALTER TABLE [dbo].[Trade]  WITH NOCHECK ADD  CONSTRAINT [FK_Trade_AutoTrade] FOREIGN KEY([AutoTradeId])
REFERENCES [dbo].[AutoTrade] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Trade] CHECK CONSTRAINT [FK_Trade_AutoTrade]
GO
ALTER TABLE [dbo].[TradeOrder]  WITH CHECK ADD  CONSTRAINT [FK_TradeOrder_Trade] FOREIGN KEY([TradeId])
REFERENCES [dbo].[Trade] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TradeOrder] CHECK CONSTRAINT [FK_TradeOrder_Trade]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-38d50e45-3c21-47b5-803d-161ebdb272c4] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-38d50e45-3c21-47b5-803d-161ebdb272c4]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-38d50e45-3c21-47b5-803d-161ebdb272c4] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-38d50e45-3c21-47b5-803d-161ebdb272c4') > 0)   DROP SERVICE [SqlQueryNotificationService-38d50e45-3c21-47b5-803d-161ebdb272c4]; if (OBJECT_ID('SqlQueryNotificationService-38d50e45-3c21-47b5-803d-161ebdb272c4', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-38d50e45-3c21-47b5-803d-161ebdb272c4]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-38d50e45-3c21-47b5-803d-161ebdb272c4]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_AcumData]
(
	@AutoTradeId int,
	@MaxRow int
)

AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @ContractSize decimal(18, 4);
	DECLARE @AUM decimal(18,4);
	DECLARE @MinTick decimal(18,4);
	DECLARE @Commission decimal(18,4);

	SELECT @AUM = AutoT.AUM, @Commission = AutoT.Commission, @ContractSize = Tik.[ContractSize], @MinTick = Tik.[MinTick]
		FROM [AutoTrade] AS AutoT
			INNER JOIN [Ticker] AS Tik ON Tik.[Id] = AutoT.[TickerId]
		WHERE AutoT.[Id] = @AutoTradeId;


	DECLARE @tbl TABLE (
		[Id] [int],
		[RawTime] [int] NOT NULL,
		[Close] [decimal](18, 4) NOT NULL,
		[DealQty] [int] NOT NULL,
		[DealPrice] [decimal](18, 4) NOT NULL,
		[DealVal] [decimal](18, 4) NOT NULL,
		[DealCommission] [decimal](18, 4) NOT NULL,
		[CumulativeCommission] [decimal](18, 4) NOT NULL,
		[CumulativeTradeVal] [decimal](18, 4) NOT NULL,
		[CumulativePos] [int] NOT NULL,
		[ClosePL] decimal(18,4)  NULL,
		[OpenPL] decimal(18,4)  NULL,
		[DealNet] decimal(18,4)  NULL,
		[NAV] decimal(18,4)  NULL
		);


	WITH CTE_Aggreg AS (
		SELECT
			 TD.[Id]
			,TD.[RawTime]
			,MAX(TD.[Close]) AS [Close]
			,ISNULL(SUM(T.[FillQty]), 0) AS [DealQty]
			,ISNULL(SUM(-1 * T.[FillPrice] * @ContractSize * T.[FillQty]), 0) AS [DealVal]
			,ISNULL(SUM(-1 * ABS(@ContractSize * T.[FillQty] * @Commission)), 0) AS [DealCommission]
		FROM [TrackData] AS TD 
			LEFT JOIN [Trade] AS T ON T.[AutoTradeId] = TD.[AutoTradeId] AND TD.RawTime = T.EndRawTime AND T.[TradeStatus] = 3
		WHERE TD.[AutoTradeId] = @AutoTradeId
		GROUP BY TD.[Id], TD.[AutoTradeId], TD.[RawTime]
	),
	CTE_Calc AS (
		SELECT
			 [Id]
			,[RawTime]
			,[Close]
			,SUM([DealQty]) OVER (ORDER BY [Id]) AS [CumulativePos]
			,SUM([DealVal]) OVER (ORDER BY [Id]) AS [CumulativeTradeVal]
			,SUM([DealCommission]) OVER (ORDER BY [Id]) AS [CumulativeCommission]
			,[DealQty]
			,[DealVal]
			,[DealCommission]
			,ROUND(CASE WHEN ISNULL([DealQty], 0) = 0 THEN 0 ELSE ABS([DealVal] / [DealQty] / @ContractSize) END / @MinTick, 0) * @MinTick AS [DealPrice]
		FROM CTE_Aggreg
	),
	CTE_Calc2 AS (
		SELECT
		 [Id]
		,[RawTime]
		,[Close]
		,[DealQty]
		,[DealPrice]
		,[DealVal]
		,[DealCommission]
		,[CumulativeCommission]
		,[CumulativeTradeVal]
		,[CumulativePos]
		,CASE WHEN [CumulativePos] = 0 THEN [CumulativeTradeVal] ELSE NULL END AS ClosePL
	FROM 
		CTE_Calc
	)
	INSERT INTO @tbl(
		 [Id]
		,[RawTime]
		,[Close]
		,[DealQty]
		,[DealPrice]
		,[DealVal]
		,[DealCommission]
		,[CumulativeCommission]
		,[CumulativeTradeVal]
		,[CumulativePos]
		,[ClosePL]
		,[OpenPL]
		,[DealNet]
		,[NAV]
	)
	SELECT 	 [Id]
		,[RawTime]
		,[Close]
		,[DealQty]
		,[DealPrice]
		,[DealVal]
		,[DealCommission]
		,[CumulativeCommission]
		,[CumulativeTradeVal]
		,[CumulativePos]
		,[ClosePL]
		,NULL AS [OpenPL]
		,NULL AS [DealNet]
		,NULL AS [NAV]
	 FROM CTE_Calc2;

	 DECLARE track_cursor CURSOR FOR
		SELECT [Id]
			,[RawTime]
			,[Close]
			,[DealQty]
			,[DealPrice]
			,[DealVal]
			,[DealCommission]
			,[CumulativeCommission]
			,[CumulativeTradeVal]
			,[CumulativePos]
			,[ClosePL]
			,[OpenPL]
			,[DealNet]
			,[NAV]
		FROM @tbl
		ORDER BY [Id];

	OPEN track_cursor;

	DECLARE @Id int, @RawTime int, @Close decimal(18, 4), @DealQty int, @DealPrice decimal(18, 4), @DealVal decimal(18, 4), 
			@DealCommission decimal(18, 4), @CumulativeCommission decimal(18, 4), @CumulativeTradeVal decimal(18, 4), 
			@CumulativePos int, @ClosePL decimal(18,4), @OpenPL decimal(18,4), @DealNet decimal(18,4), @NAV decimal(18,4);

	DECLARE @ActiveClosePL decimal(18,4) = 0;

	-- Fetch the first row from the cursor
	FETCH NEXT FROM track_cursor INTO @Id, @RawTime, @Close, @DealQty, @DealPrice, @DealVal, @DealCommission, 
									  @CumulativeCommission, @CumulativeTradeVal, @CumulativePos, @ClosePL,
									  @OpenPL, @DealNet, @NAV;


	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF (@ClosePL IS NULL)
		BEGIN
			SET @ClosePL = @ActiveClosePL;
		END 
		ELSE 
		BEGIN
			SET @ActiveClosePL = @ClosePL;
		END

		-- Calculate OpenPL
		IF (@CumulativePos != 0)
		BEGIN
			SET @OpenPL = (@Close * @CumulativePos * @ContractSize) + (@CumulativeTradeVal - @ClosePL);
		END

		SET @DealNet = ISNULL(@ClosePL, 0) + ISNULL(@OpenPL, 0) + ISNULL(@CumulativeCommission, 0);

		UPDATE @tbl 
			SET 
				[ClosePL] = @ClosePL,
				[OpenPL] = ISNULL(@OpenPL, 0),
				[DealNet] = @DealNet,
				[NAV] = (@AUM + @DealNet) * 100 / @AUM
			WHERE [Id] = @Id;

		-- Fetch the next row
		FETCH NEXT FROM track_cursor INTO @Id, @RawTime, @Close, @DealQty, @DealPrice, @DealVal, @DealCommission, 
										  @CumulativeCommission, @CumulativeTradeVal, @CumulativePos, @ClosePL,
										  @OpenPL, @DealNet, @NAV;

	END;

	-- Close and deallocate the cursor
	CLOSE track_cursor;
	DEALLOCATE track_cursor;

	SELECT TOP (@MaxRow) * FROM @tbl
		ORDER BY [Id] DESC;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_auto_trade_delete]
	@Id int
AS
BEGIN
	Delete [AutoTrade] Where Id = @Id;
	Delete [TrackData] Where AutoTradeId = @Id
	Delete [Trade] Where AutoTradeId = @Id
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_auto_trade_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Name]
		  ,[Ver]
		  ,[TickerId]
		  ,[IntervalId]
		  ,[BotId]
		  ,[IsLive]
		  ,[StartTime]
		  ,[LiveStatus]
		  ,[AUM]
		  ,[Commission]
		  ,[Params]
		FROM [AutoTrade]
		WHERE @Id = -1 OR @Id = Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_auto_trade_strategy_delete]
(
	@Id int
)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE [AutoTradeStrategy]  WHERE [Id] = @Id;	
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_auto_trade_strategy_select] 
	@Id int = -1,
	@AutoTradeId int = -1
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT [Id]
		  ,[AutoTradeId]
		  ,[Name]
		  ,[Config]
		  ,[Qty]
		  ,[QtyPct]
		  ,[AwayAdj]
		  ,[Enabled]
		FROM [dbo].[AutoTradeStrategy]
		WHERE (@Id = -1 OR [Id] = @Id) AND (@AutoTradeId = -1 OR [AutoTradeId] = @AutoTradeId);

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_auto_trade_strategy_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@AutoTradeId int,
	@Name varchar(255),
	@Config varchar(max),
	@Qty int,
	@QtyPct float,
	@AwayAdj decimal(18,4),
	@Enabled bit
)

AS
BEGIN
	SET NOCOUNT ON;

	-- Check if strategy exists
	SELECT	 @IdNew = [Id] 
		FROM [AutoTradeStrategy] 
		WHERE @AutoTradeId = [AutoTradeId] AND @Name = [Name];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [AutoTradeStrategy] (
			 [AutoTradeId]
			,[Name]
			,[Config]
			,[Qty]
			,[QtyPct]
			,[AwayAdj]
			,[Enabled]
			)
		VALUES (
			 @AutoTradeId
			,@Name
			,@Config
			,@Qty
			,@QtyPct
			,@AwayAdj
			,@Enabled
			)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [AutoTradeStrategy] SET 
				 [AutoTradeId] = @AutoTradeId
				,[Name] = @Name
				,[Config] = @Config
				,[Qty] = @Qty
				,[QtyPct] = @QtyPct
				,[AwayAdj] = @AwayAdj
				,[Enabled] = @Enabled
		WHERE Id = @IdNew
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_auto_trade_update_live_status]
(
	@Id int,
	@LiveStatus int
)

AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [AutoTrade] SET [LiveStatus] = @LiveStatus WHERE Id = @Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_auto_trade_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@Name varchar(256),
	@Ver int,
	@TickerId int,
	@IntervalId varchar(10),
	@BotId int,
	@IsLive bit,
	@StartTime int,
	@LiveStatus int,
	@AUM decimal(18,6),
	@Commission decimal(18,4),
	@Params varchar(max)
)

AS
BEGIN

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [AutoTrade] WHERE @Id = [Id];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [AutoTrade] (
			   [Name]
			  ,[Ver]
			  ,[TickerId]
			  ,[IntervalId]
			  ,[BotId]
			  ,[IsLive]
			  ,[StartTime]
			  ,[LiveStatus]
			  ,[AUM]
			  ,[Commission]
			  ,[Params]
		)
		VALUES (
			  @Name
			, @Ver
			, @TickerId
			, @IntervalId
			, @BotId
			, @IsLive
			, @StartTime
			, @LiveStatus
			, @AUM
			, @Commission
			, @Params
		)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [AutoTrade] SET 
			  [Name] = @Name
			, [Ver] = @Ver
			, [TickerId] = @TickerId
			, [IntervalId] = @IntervalId
			, [BotId] = @BotId
			, [IsLive] = @IsLive
			, [StartTime] = @StartTime
			, [LiveStatus] = @LiveStatus
			, [AUM] = @AUM
			, [Commission] = @Commission
			, [Params] = @Params
		WHERE Id = @IdNew
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_bot_delete]
(
	@Id int
)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE [Bot] WHERE Id = @Id

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_bot_query]
(
	@RefId varchar(50)
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Name]
		  ,[RefId]
		  ,[Config]
		FROM [dbo].[Bot]
		WHERE @RefId = [RefId]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_bot_select]
(
	@Id int
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Name]
		  ,[RefId]
		  ,[Config]
		FROM [dbo].[Bot]
		WHERE @Id = -1 OR @Id = [Id]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_bot_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@Name varchar(50),
	@RefId varchar(50),
	@Config varchar(max)
)

AS
BEGIN

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [Bot] WHERE [Id] = @Id;
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Bot] (
			  [Name]
			, [RefId]
			, [Config]
		)
		VALUES (
			  @Name
			, @RefId
			, @Config
		)
		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [Bot]  SET
			  [Name] = @Name
			, [RefId] = @RefId
			, [Config] = @Config
		WHERE Id = @IdNew

	END

	RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_create_study_history_table]
(
	@StudyColId int,
	@BasicStudiesHeaders varchar(MAX),
	@CompoundStudiesHeaders varchar(MAX),
	@SignalsHeaders varchar(MAX)
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	DECLARE @headers TABLE([name] VARCHAR(100));

	INSERT INTO @headers SELECT LTRIM(RTRIM([field])) FROM [dbo].[fn_split_string](@BasicStudiesHeaders, ',');
	INSERT INTO @headers SELECT LTRIM(RTRIM([field])) FROM [dbo].[fn_split_string](@CompoundStudiesHeaders, ',');
	INSERT INTO @headers SELECT LTRIM(RTRIM([field])) FROM [dbo].[fn_split_string](@SignalsHeaders, ',');

	DECLARE @sql NVARCHAR(MAX);
    DECLARE @tableName NVARCHAR(128) = 'StudyHistory';

	-- Check if table already exists
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName)
    BEGIN
        DECLARE @dropSql NVARCHAR(MAX) = 'DROP TABLE [dbo].[' + @tableName + ']';
        EXEC sp_executesql @dropSql;
    END

	-- Add Studies headers
    DECLARE @headerName NVARCHAR(100);
    DECLARE header_cursor CURSOR FOR 
    SELECT [name] FROM @headers;
    
    OPEN header_cursor;
    FETCH NEXT FROM header_cursor INTO @headerName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = @sql + CHAR(13) + CHAR(10) + '[' + @headerName + '] [decimal](10, 4) NULL,';
        FETCH NEXT FROM header_cursor INTO @headerName;
    END
    CLOSE header_cursor;
    DEALLOCATE header_cursor;

	-- Add primary key constraint and close the statement
    SET @sql = LEFT(@sql, LEN(@sql) - 1) + -- Remove trailing comma
        CHAR(13) + CHAR(10) + 'CONSTRAINT [PK_' + @tableName + '] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]';

	-- Execute the dynamic SQL
    EXEC sp_executesql @sql;

	-- Create index on StudyColId and RawTime for performance
    SET @sql = 'CREATE UNIQUE INDEX [IX_' + @tableName + '_StudyColId_RawTime] ON [dbo].[' + @tableName + ']
    (
        [StudyColId] ASC,
        [RawTime] ASC
    )';
    EXEC sp_executesql @sql;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_dataset_query]
(
	@TickerId int,
	@IntervalId varchar(10)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @IntervalId = upper(trim(@IntervalId));

	SELECT [Id]
		  ,[TickerId]
		  ,[IntervalId]
		FROM [DataSet]
		WHERE @TickerId = [TickerId] AND @IntervalId = [IntervalId]; 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_dataset_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[TickerId]
		  ,[IntervalId]
		FROM [DataSet]
		WHERE @Id = -1 OR @Id = Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_dataset_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@TickerId int,
	@IntervalId varchar(10)
)

AS
BEGIN

	DECLARE @CommissionOld decimal(18,4);
	DECLARE @AUMOld decimal(18,6);

	SET @IntervalId = upper(trim(@IntervalId));

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [DataSet] WHERE @TickerId = [TickerId] AND @IntervalId = [IntervalId];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [DataSet] (
			  [TickerId]
			, [IntervalId]
		)
		VALUES (
			  @TickerId
			, @IntervalId
		)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [DataSet] SET 
			  [TickerId] = @TickerId
			, [IntervalId] = @IntervalId
		WHERE Id = @IdNew

	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_default_parameter_select]
(
	@Tag varchar(50)
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Tag]
		  ,[Desc]
		  ,[DefaultValue]
		  ,[ValueType]
		  ,[Fixed]
		FROM [dbo].[DefaultParameter]
		WHERE @Tag = '' OR @Tag = [Tag]
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
CREATE PROCEDURE [dbo].[usp_ib_interval_select]
(
	@IntervalId varchar(10)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @IntervalId = upper(trim(@IntervalId));

	SELECT [Interval].[Id]
		  ,[Interval].[Name]
		  ,[Interval].[Seq]
		  ,[Interval].[IntervalLen]		  
		  ,[IBKRInterval].[IBKRInterval]
		FROM [IBKRInterval]
			 INNER JOIN [Interval] ON [Interval].[Id] = [IBKRInterval].[IntervalId]
		WHERE @IntervalId = '' OR @IntervalId = [IntervalId]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_interval_select]
(
	@Id varchar(10)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @Id = upper(trim(@Id));

	SELECT [Id]
		  ,[Name]
		  ,[Seq]
		  ,[IntervalLen]
		FROM [Interval]
		WHERE @Id = '' OR @Id = Id
		ORDER BY [Seq]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_price_data_select] 
	@DataSetId int,
	@StartTime int,
	@MaxRec int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT	TOP (@MaxRec)
				[Id]
			,[DataSetId]
			,[RawTime]
			,[PriceDate]
			,[Open]
			,[High]
			,[Low]
			,[Close]
			,[Volume]
		FROM [dbo].[PriceData]
		WHERE DataSetId = @DataSetId AND [RawTime] >= @StartTime
		ORDER BY [DataSetId], [RawTime] ASC
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_price_data_select_last] 
	@DataSetId int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT	TOP 1
			 [Id]
			,[DataSetId]
			,[RawTime]
			,[PriceDate]
			,[Open]
			,[High]
			,[Low]
			,[Close]
			,[Volume]
		FROM [dbo].[PriceData]
		WHERE DataSetId = @DataSetId 
		ORDER BY [RawTime] DESC
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_price_data_select_until] 
	@DataSetId int,
	@UntilTime int,
	@MaxRec int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	WITH FilteredData AS (
			SELECT 
				[Id],
				[DataSetId],
				[RawTime],
				[PriceDate],
				[Open],
				[High],
				[Low],
				[Close],
				[Volume],
				ROW_NUMBER() 
			OVER (ORDER BY [RawTime] DESC) as RowNum
			FROM [dbo].[PriceData]
			WHERE DataSetId = @DataSetId AND [RawTime] <= @UntilTime
		)
		SELECT 
			[Id],
			[DataSetId],
			[RawTime],
			[PriceDate],
			[Open],
			[High],
			[Low],
			[Close],
			[Volume]
		FROM FilteredData
		WHERE RowNum <= @MaxRec
		ORDER BY [RawTime] ASC -- Final output ordered ascending
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_price_data_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@DataSetId int,
	@RawTime int,
	@PriceDate datetime,
	@Open decimal(18,4),
	@High decimal(18,4),
	@Low decimal(18,4),
	@Close decimal(18,4),
	@Volume int
)

AS
BEGIN

	DECLARE @CurOpen decimal(18,4);
	DECLARE @CurHigh decimal(18,4);
	DECLARE @CurLow decimal(18,4);
	DECLARE @CurClose decimal(18,4);
	DECLARE @CurVolume int;

	-- Check if Price exists
	SELECT @IdNew = [Id],
		   	@CurOpen = [Open],
		   	@CurHigh = [High],
		   	@CurLow = [Low],
		   	@CurClose = [Close],
		   	@CurVolume = [Volume]
		FROM [PriceData] WHERE @DataSetId = [DataSetId] AND @RawTime = [RawTime];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [PriceData]
				   ([DataSetId]
				   ,[RawTime]
				   ,[PriceDate]
				   ,[Open]
				   ,[High]
				   ,[Low]
				   ,[Close]
				   ,[Volume])
			 VALUES
			   (@DataSetId
			   ,@RawTime
			   ,@PriceDate
			   ,@Open
			   ,@High
			   ,@Low
			   ,@Close
			   ,@Volume)
		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values if any value has changed
		IF (@Open != @CurOpen 
			 OR @High != @CurHigh
			 OR @Low != @CurLow
			 OR @Close != @CurClose
			 OR @Volume != @CurVolume)
		BEGIN
			UPDATE [PriceData] SET 
					[DataSetId] = @DataSetId
				   ,[RawTime] = @RawTime
				   ,[PriceDate] = @PriceDate
				   ,[Open] = @Open
				   ,[High] = @High
				   ,[Low] = @Low
				   ,[Close] = @Close
				   ,[Volume] = @Volume
			WHERE Id = @IdNew
		END
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_price_src_select]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Name]
		  ,[Value]
		FROM [PriceSrc]
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_price_truncate]
(
	@DataSetId int,
	@StartTime int
)
AS
BEGIN
	DELETE [PriceData] WHERE [DataSetId] = @DataSetId AND [RawTime] >= @StartTime; 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_setting_delete]
	@Id int,
	@Group varchar(50),
	@Tag varchar(100)
AS
BEGIN
	IF (@Id != -1) 
	BEGIN
		Delete [Setting] Where Id = @Id;
	END ELSE BEGIN
		Delete [Setting] Where [Group] = @Group AND [Tag] = @Tag;
	END
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_setting_select]
(
	@Group varchar(50),
	@Tag varchar(100)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Group]
		  ,[Tag]
		  ,[Value]
		FROM [Setting]
		WHERE (@Group = '' OR @Group = [Group]) AND (@Tag = '' OR @Tag = [Tag])
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_setting_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@Group varchar(50),
	@Tag varchar(100),
	@value nvarchar(max)

)

AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @CurId int;

	SELECT @CurId = Id FROM [Setting] WHERE @Group = [Group] AND @Tag = [Tag];

	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Setting] (
			 [Group]
			,[Tag]
			,[Value]
			)
		VALUES (
			 @Group
			,@Tag
			,@Value
			)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		SET @IdNew = @CurId;
		-- Exists, just update values
		UPDATE [Setting] SET [Value] = @Value WHERE Id = @CurId;
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_strategy_delete]
(
	@name varchar(100)
)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE [Strategy] WHERE [Name] = @name;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_strategy_select] 
	@Name varchar(100)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	/****** Script for SelectTopNRows command from SSMS  ******/
	SELECT [Name]
		  ,[Desc]
		FROM [Strategy]
		WHERE @Name = '' OR [Name] = @Name;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_strategy_upsert]
(
	@IdNew int OUTPUT,  -- need to be there for compatability with other procedures
	@Name varchar(100),
	@Desc varchar(1024)
)

AS
BEGIN

	DECLARE @UpperName varchar(100) = TRIM(UPPER(@Name));
	DECLARE @NameNew varchar(100);
	SET @IdNew = 0;

	-- Check if name exists
	SELECT @NameNew = [Name] FROM [Strategy] WHERE TRIM(UPPER([Name])) = @UpperName;
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Strategy] ([Name], [Desc]) VALUES (Trim(@Name), Trim(@Desc))
	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [Strategy] SET [Desc] = @Desc WHERE [Name] = @NameNew;
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_col_delete]
	@Id int
AS
BEGIN
	Delete [StudyCol] Where Id = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_col_query]
(
	@TickerId int,
	@IntervalId varchar(10),
	@C21 int,
	@StdDev1 decimal(18,4),
	@StdDev2 decimal(18,4),
	@StdDev3 decimal(18,4)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[TickerId]
		  ,[IntervalId]
		  ,[C21]
		  ,[StdDev1]
		  ,[StdDev2]
		  ,[StdDev3]
		  ,[IsLive]
		  ,[Enable]
		  ,[CompileFull]
		FROM [StudyCol]
		WHERE	@TickerId = [TickerId] 
			AND @IntervalId = [IntervalId]
			AND @C21 = [C21]
			AND @StdDev1 = [StdDev1]
			AND @StdDev2 = [StdDev2]
			AND @StdDev3 = [StdDev3]

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_col_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[TickerId]
		  ,[IntervalId]
		  ,[C21]
		  ,[StdDev1]
		  ,[StdDev2]
		  ,[StdDev3]
		  ,[IsLive]
		  ,[Enable]
		  ,[CompileFull]
		FROM [StudyCol]
		WHERE @Id = -1 OR [Id] = @Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_col_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@TickerId int,
	@IntervalId varchar(10),
	@C21 int,
	@StdDev1 decimal(18,4),
	@StdDev2 decimal(18,4),
	@StdDev3 decimal(18,4),
	@IsLive bit,
	@Enable bit,
	@CompileFull bit
)

AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @CurId int;

	SELECT @IdNew = Id FROM [StudyCol] WHERE [Id] = @Id
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [dbo].[StudyCol]
				   ([TickerId]
				   ,[IntervalId]
				   ,[C21]
				   ,[StdDev1]
				   ,[StdDev2]
				   ,[StdDev3]
				   ,[IsLive]
				   ,[Enable]
				   ,[CompileFull])
			 VALUES
				   (@TickerId,
				   @IntervalId,
				   @C21,
				   @StdDev1,
				   @StdDev2,
				   @StdDev3,
				   @IsLive,
				   @Enable,
				   @CompileFull);

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		UPDATE [dbo].[StudyCol]
			SET [TickerId] = @TickerId
				,[IntervalId] = @IntervalId
				,[C21] = @C21
				,[StdDev1] = @StdDev1
				,[StdDev2] = @StdDev2
				,[StdDev3] = @StdDev3
				,[IsLive] = @IsLive
				,[Enable] = @Enable
				,[CompileFull] = @CompileFull
			WHERE [Id] = @Id
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_history_count]
(
	@StudyColId int,
	@StartTime int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @TotalCount int = 0;
	SELECT @TotalCount = COUNT(1)
		FROM [StudyHistory]
		WHERE [StudyColId] = @StudyColId AND [RawTime] >= @StartTime

	RETURN @TotalCount;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_history_select]
(
	@StudyColId int,
	@StartTime int,
	@MaxRec int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF @MaxRec <= 0
    BEGIN
        SET @MaxRec = 1;
    END 
	
	SELECT TOP (@MaxRec) 
            [Id],
            [StudyColId],
            [RawTime],
            [Open],
            [High],
            [Low],
            [Close],
            [Volume],
            [Studies]
		FROM [StudyHistory]
		WHERE [StudyColId] = @StudyColId AND [RawTime] >= @StartTime
		ORDER BY [Id] ASC
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_history_select_top]
(
	@StudyColId int,
	@MaxRec int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	IF @MaxRec <= 0
    BEGIN
        SET @MaxRec = 1;
    END
	
	-- Add semicolon here to avoid syntax error before CTE
	;

	-- Get top records in descending order but return in ascending order
	WITH TopRecords AS (
        SELECT TOP (@MaxRec) 
               [Id],
               [StudyColId],
               [RawTime],
               [Open],
               [High],
               [Low],
               [Close],
               [Volume],
               [Studies]
        FROM [StudyHistory]
        WHERE @StudyColId = -1 OR [StudyColId] = @StudyColId
        ORDER BY [Id] DESC
    )
    SELECT * 
    FROM TopRecords
    ORDER BY [Id] ASC;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_study_history_update]
(
	@Id int,
	@StudyColId int,
	@RawTime int,
	@Open decimal(18,4),
	@High decimal(18,4),
	@Low decimal(18,4),
	@Close decimal(18,4),
	@Volume int,
	@Studies varchar(max)
)

AS
BEGIN

	-- Exists, just update values
	UPDATE [Studyhistory] SET 
			 [StudyColId] = @StudyColId
			,[RawTime] = @RawTime
			,[Open] = @Open
			,[High] = @High
			,[Low] = @Low
			,[Close] = @Close
			,[Volume] = @Volume
			,[Studies] = @Studies
	WHERE [Id] = @Id
		 OR (@Id = -1 AND [StudyColId] = @StudyColId AND [RawTime] = @RawTime);
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_study_history_view]
(
    @StudyColId int,
    @RawTimeStart int,
    @RawTimeEnd int,
    @ParamNames varchar(max)
)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Create a table variable to hold the parameter names
    DECLARE @ParamTable TABLE (ParamName NVARCHAR(100));
    
    -- Split the comma-separated parameter names into rows
    INSERT INTO @ParamTable
    SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@ParamNames, ',') WHERE LTRIM(RTRIM(value)) <> '';
    
    -- Build dynamic WITH clause for OPENJSON
    DECLARE @WithClause NVARCHAR(MAX) = '';
    
    SELECT @WithClause = @WithClause + 
           '[' + ParamName + '] DECIMAL(18,4) ''strict $.' + ParamName + ''',' + CHAR(13) + CHAR(10)
    FROM @ParamTable;
    
    -- Remove trailing comma and newline if parameters exist
    IF LEN(@WithClause) > 0
    BEGIN
        SET @WithClause = LEFT(@WithClause, LEN(@WithClause) - 3); -- Remove last comma and CRLF
    END
    
    -- Build the full dynamic SQL
    DECLARE @SQL NVARCHAR(MAX) = '
    SELECT H.[Id]
          ,H.[StudyColId]
          ,H.[RawTime]
          ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](H.[RawTime], ''UTC'') AS [PriceDateUTC]
          ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](H.[RawTime], ''Eastern Standard Time'') AS [PriceDateEST]
          ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](H.[RawTime], ''Arabian Standard Time'') AS [PriceDateDubai]
          ,H.[Open]
          ,H.[High]
          ,H.[Low]
          ,H.[Close]
          ,H.[Volume]
          ,H.[Studies]';
    
    -- Add the dynamic columns if parameters exist
    IF EXISTS (SELECT 1 FROM @ParamTable)
    BEGIN
        SET @SQL = @SQL + CHAR(13) + CHAR(10);
        
        SELECT @SQL = @SQL + ',CData.[' + ParamName + ']' + CHAR(13) + CHAR(10)
        FROM @ParamTable;
        
        SET @SQL = @SQL + 'FROM [StudyHistory] AS H
            CROSS APPLY OPENJSON(H.[Studies]) 
                WITH (' + @WithClause + ') AS CData';
    END
    ELSE
    BEGIN
        SET @SQL = @SQL + CHAR(13) + CHAR(10) + 'FROM [StudyHistory] AS H';
    END
    
    -- Add the WHERE clause
    SET @SQL = @SQL + '
     WHERE (@StudyColId = -1 OR [StudyColId] = @StudyColId)
            AND (@RawTimeStart <= 0 OR [RawTime] >= @RawTimeStart)
            AND (@RawTimeEnd <= 0 OR [RawTime] <= @RawTimeEnd)
    ORDER BY Id ASC';
    
    -- Execute the dynamic SQL
    EXEC sp_executesql @SQL, 
         N'@StudyColId int, @RawTimeStart int, @RawTimeEnd int', 
         @StudyColId, @RawTimeStart, @RawTimeEnd;
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

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [Ticker] WHERE @Symbol = [Symbol];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Ticker] (
			  [Symbol]
			, [Name]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trackdata_insert]
(
	@IdNew int OUTPUT,
	@Id int,
	@AutoTradeId int,
	@RawTime int,
	@Open decimal(18,4),
	@High decimal(18,4),
	@Low decimal(18,4),
	@Close decimal(18,4),

	@Params varchar(max),
	@ActiveTrades varchar(max),
	@StrategyData varchar(max),
	@CurrentSides varchar(max),

	@TradeQty int,
	@TradeValue decimal(18,4),
	@TradeCommission decimal(18,4),
	@TradeCumulativeData varchar(max)
)

AS
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [dbo].[TrackData]
           (
		    [AutoTradeId]
           ,[RawTime]
           ,[Open]
           ,[High]
           ,[Low]
           ,[Close]
           ,[Params]
           ,[ActiveTrades]
           ,[StrategyData]
		   ,[CurrentSides]
           ,[TradeQty]
           ,[TradeValue]
           ,[TradeCommission]
           ,[TradeCumulativeData]
		   )
     VALUES
           (
			 @AutoTradeId
			,@RawTime
			,@Open
			,@High
			,@Low
			,@Close
            ,@Params
            ,@ActiveTrades
            ,@StrategyData
			,@CurrentSides
            ,@TradeQty
            ,@TradeValue
            ,@TradeCommission
            ,@TradeCumulativeData
		   )

	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_trackdata_select] 
	@AutoTradeId int,
	@RawTime int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT	[Id]
		   ,[AutoTradeId]
           ,[RawTime]
           ,[Open]
           ,[High]
           ,[Low]
           ,[Close]
           ,[Params]
           ,[ActiveTrades]
           ,[StrategyData]
		   ,[CurrentSides]
           ,[TradeQty]
		   ,[TradeFillPrice]
           ,[TradeValue]
           ,[TradeCommission]
           ,[TradeCumulativeData]
		FROM [dbo].[TrackData]
		WHERE [AutoTradeId] = @AutoTradeId AND [RawTime] >= @RawTime
		ORDER BY [RawTime]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_trackdata_select_last] 
	@AutoTradeId int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT	TOP 1
			[Id]
		   ,[AutoTradeId]
           ,[RawTime]
           ,[Open]
           ,[High]
           ,[Low]
           ,[Close]
           ,[Params]
           ,[ActiveTrades]
           ,[StrategyData]
		   ,[CurrentSides]
           ,[TradeQty]
		   ,[TradeFillPrice]
           ,[TradeValue]
           ,[TradeCommission]
           ,[TradeCumulativeData]
		FROM [dbo].[TrackData]
		WHERE [AutoTradeId] = @AutoTradeId
		ORDER BY [RawTime] DESC
END

SET ANSI_NULLS ON
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trackdata_truncate]
(
	@AutoTradeId int,
	@StartTime int
)
AS
BEGIN
	DELETE [TrackData] WHERE [AutoTradeId] = @AutoTradeId AND [RawTime] >= @StartTime; 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trade_insert]
(
	@IdNew int OUTPUT,
    @Id int,
    @AutoTradeId int,
    @BotId int,
    @TickerId int,
	@StartRawTime int,
	@EndRawTime int,
	@Strategy varchar(100),
	@SignalId varchar(50),
	@Side decimal(18,4),
    @StrategyParam varchar(100),
	@OrderRef varchar(36),
    @OrderTag varchar(50),
    @OrderType varchar(50),
    @Qty int,
	@Price decimal(18,4),
	@TradeStatus int,
	@FillQty int,
	@FillPrice decimal(18,4),
	@LastRawTime int
)

AS
BEGIN
	DECLARE @NowRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());

	-- OVERRIDE @StartRawTime, @EndRawTime, and @LastRawTime  (Automatically added and Updated)
	--SET @StartRawTime = @NowRawTime;
	--SET @EndRawTime = -1;
	SET @LastRawTime = @NowRawTime;
	
	INSERT INTO [dbo].[Trade]
				(
				 [AutoTradeId]
				,[BotId]
				,[TickerId]
				,[StartRawTime]
				,[EndRawTime]
				,[Strategy]
				,[SignalId]
				,[Side]
				,[StrategyParam]
				,[OrderRef]
				,[OrderTag]
				,[OrderType]
				,[Qty]
				,[Price]
				,[TradeStatus]
				,[FillQty]
				,[FillPrice]
				,[LastRawTime]
				)
			VALUES
				(
				 @AutoTradeId
				,@BotId
				,@TickerId
				,@StartRawTime
				,@EndRawTime
				,@Strategy
				,@SignalId
				,@Side
				,@StrategyParam
				,@OrderRef
				,@OrderTag
				,@OrderType
				,@Qty
				,@Price
				,@TradeStatus
				,@FillQty
				,@FillPrice
				,@LastRawTime
				)
	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END
SET ANSI_NULLS ON
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trade_opened]
(
	@AutoTradeId int,
	@Strategy varchar(100)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @SignalId varchar(50);
	DECLARE @OrderRef varchar(36);
	DECLARE @IdOpen int;

	-- FIND The last signal that was opened for this strategy and autoTradeId
	SELECT TOP 1 @SignalId = [SignalId], @OrderRef = [OrderRef], @IdOpen = [Id]
		FROM  [Trade] 
		WHERE [AutoTradeId] = @AutoTradeId 
			AND [Strategy] = @Strategy
			AND [OrderTag] = 'OPEN'
		ORDER BY [Id] DESC

	IF @@ROWCOUNT = 0
	BEGIN
		RETURN;
	END

	-- Check if there is a close transaction 
	IF EXISTS(SELECT 1 FROM [Trade] WHERE [AutoTradeId] = @AutoTradeId AND [Strategy] = @Strategy AND [OrderTag] = 'CLOSE' AND [OrderRef] = @OrderRef) 
	BEGIN
		-- ALREDY there is a CLOSE, ignore
		return;
	END

	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
	FROM [Trade]
	WHERE [Id] = @IdOpen;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--USED
CREATE PROCEDURE [dbo].[usp_trade_order_add]
(
    @TradeId int,
	@Qty int,
	@Price decimal(18,4),
	@Action varchar(100)
)

AS
BEGIN
	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	DECLARE @IdNew int = -1;
	INSERT INTO [dbo].[TradeOrder]
		(
				[TradeId]
			,[RawTime]
			,[Action]
			,[Qty]
			,[Price]
			,[FillQty]
			,[FillPrice]
			,[Status]
			,[SyncId]
			,[BrokerRef]
			,[LastRawTime]
		)
		VALUES
		(
				@TradeId
			,@LastRawTime
			,@Action
			,@Qty
			,@Price
			,0 -- FillQty
			,0 -- FillPrice
			,0  --Status
			,-1 -- @SyncId
			,null  -- @BrokerRef
			,@LastRawTime
		)
	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_order_select]
(
	@TradeId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT   [Id]
			,[TradeId]
			,[RawTime]
			,[Action]
			,[Qty]
			,[Price]
			,[FillQty]
			,[FillPrice]
			,[Status]
			,[SyncId]
			,[BrokerRef]
			,[LastRawTime]
	  FROM [dbo].[TradeOrder]
	  WHERE [TradeId] = @TradeId
	  ORDER BY [Id] ASC;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--USED
CREATE PROCEDURE [dbo].[usp_trade_order_update_price]
(
	@TradeOrderId int,
	@Price int
)

AS
BEGIN
	-- @LastRawTime  (Automatically added and Updated)
	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	DECLARE @TradeId int;
	SELECT @TradeId = [TradeId] FROM [TradeOrder] WHERE [Id] = @TradeOrderId;

	DECLARE @Success bit = 0;
	BEGIN TRY
	  BEGIN TRANSACTION UPDATETRADEORDER

		DECLARE @TradeQty int;
		DECLARE @TradeFillQty int;
		DECLARE @TradeFillPrice int;
		DECLARE @TradeStatus int;
		DECLARE @TradePrice decimal(18,4);
		SELECT @TradeStatus = [TradeStatus], @TradePrice = [Price], @TradeQty = [Qty], @TradeFillQty = [FillQty], @TradeFillPrice = [FillPrice] FROM [Trade] WHERE [Id] = @TradeId;
		IF (@@ROWCOUNT > 0 AND @TradeStatus in (0, 1, 2))  -- Update only of trade is not yet completed
		BEGIN
			UPDATE [TradeOrder] SET 
					   [SyncId] = CASE WHEN [SyncId] = -2 AND @Price > 0 THEN -1 ELSE [SyncId] END
					  ,[Price] = @Price
					  ,[LastRawTime] = @LastRawTime
				WHERE [Id] = @TradeOrderId;
			IF @@ROWCOUNT > 0
			BEGIN
				-- Update succeeded
				IF (@TradePrice IS NULL OR @TradePrice != @Price) 
				BEGIN
					UPDATE [Trade] SET [Price] = @Price, [EndRawTime] = @LastRawTime, [LastRawTime] = @LastRawTime WHERE [Id] = @TradeId;
				END
				SET @Success = 1;
			END
		END

	  COMMIT TRANSACTION UPDATETRADEORDER
	END TRY
	BEGIN CATCH 
	  IF (@@TRANCOUNT > 0)
	   BEGIN
		  ROLLBACK TRANSACTION UPDATETRADEORDER
	   END 
	END CATCH

	RETURN @Success

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_order_update_status]
(
    @Id int,
    @SyncId int,
	@OrderStatus int,
	@FillQty int,
	@FillPrice decimal(18,4),
	@BrokerRef varchar(100)
)

AS
BEGIN

	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	DECLARE @CurBrokerRef varchar(100);
	DECLARE @TradeId int;
	DECLARE @Qty int;
	DECLARE @TradeStatus int;
	SELECT @TradeId = [TradeId] FROM [TradeOrder] WHERE [Id] = @Id AND [Status] != 3;
	IF (@@ROWCOUNT > 0) 
	BEGIN
		SELECT @Qty = [Qty], @TradeStatus = [TradeStatus] FROM [Trade]  WHERE [Id] = @TradeId;
		IF (@@ROWCOUNT > 0)
		BEGIN
			UPDATE [TradeOrder] 
				SET  [Status] = @OrderStatus 
					,[FillQty] = @FillQty
					,[FillPrice] = @FillPrice
					,[SyncId] = CASE WHEN [SyncId] <= 0 THEN @SyncId ELSE [SyncId] END
					,[BrokerRef] = CASE WHEN ([BrokerRef] IS NULL OR [BrokerRef] = '') THEN @BrokerRef ELSE [BrokerRef] END
					,[LastRawTime] = @LastRawTime 
				WHERE [Id] = @Id;

			SET @TradeStatus = CASE WHEN @OrderStatus > 0 AND @FillQty = 0 THEN 1 
									WHEN @OrderStatus > 0 AND ABS(@FillQty) < ABS(@Qty) THEN 2 
									WHEN @OrderStatus = 3 THEN 3
									ELSE @TradeStatus END;
			UPDATE [Trade] 
				SET [FillQty] = @FillQty,  -- fillqty is the total filled qty coming from broker
					[FillPrice] = @FillPrice,  -- price is alread avg price from broker
					[TradeStatus] = @TradeStatus,
					[EndRawTime] = CASE WHEN @TradeStatus >= 3 OR @TradeStatus < 0 THEN @LastRawTime ELSE [EndRawTime] END,
					[LastRawTime] = @LastRawTime
				WHERE [Id] = @TradeId;

		END

	END
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_order_update_syncid]
(
    @Id int,
	@SyncId int
)

AS
BEGIN
	DECLARE @RawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	UPDATE [TradeOrder] SET [SyncId] = @SyncId , [LastRawTime] = @RawTime  WHERE [Id] = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_query]
(
	@AutoTradeId int,
	@Strategy varchar(100),
	@SignalId varchar(50),
	@OrderTag  varchar(50)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
	FROM [Trade]
	WHERE [AutoTradeId] = @AutoTradeId 
			AND [Strategy] = @Strategy 
			AND (@SignalId = '' OR [SignalId] = @SignalId)
			AND [OrderTag] = @OrderTag;

END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--USED
CREATE PROCEDURE [dbo].[usp_trade_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
	  FROM [dbo].[Trade]
	  WHERE [Id] = @Id;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_select_active]
(
	@AutoTradeId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
	FROM [Trade]
	WHERE [AutoTradeId] = @AutoTradeId AND [TradeStatus] in (0, 1, 2);
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_select_by_autotrade]
(
	@AutoTradeId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
	  FROM [dbo].[Trade]
	  WHERE [AutoTradeId] = @AutoTradeId;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- USED
CREATE PROCEDURE [dbo].[usp_trade_select_completed]
(
	@AutoTradeId int,
	@SinceRawTime int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
	FROM [Trade]
	WHERE [AutoTradeId] = @AutoTradeId 
			AND [TradeStatus] = 3 
			AND [LastRawTime] >= @SinceRawTime 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trade_select_unfilled]
(
    @AutoTradeId int,
	@BotId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT	 [Id]
			,[AutoTradeId]
			,[BotId]
			,[TickerId]
			,[StartRawTime]
			,[EndRawTime]
			,[Strategy]
			,[SignalId]
			,[Side]
			,[StrategyParam]
			,[OrderRef]
			,[OrderTag]
			,[OrderType]
			,[Qty]
			,[Price]
			,[TradeStatus]
			,[FillQty]
			,[FillPrice]
			,[LastRawTime]
		FROM [Trade]
		WHERE (@BotId = -1 OR @BotId = [BotId]) AND (@AutoTradeId = -1 OR @AutoTradeId = [AutoTradeId]) AND [TradeStatus] >= 0 AND [TradeStatus] < 3
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
--USED
CREATE PROCEDURE [dbo].[usp_trade_truncate]
(
    @AutoTradeId int
)

AS
BEGIN

	DELETE [dbo].[Trade] WHERE (@AutoTradeId = -1 OR [AutoTradeId] = @AutoTradeId)

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trade_update_status]
(
    @Id int,
	@TradeStatus int,
	@FillQty int,
	@FillPrice decimal(18,4)
)
AS
BEGIN
	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	UPDATE [Trade] 
		SET  [TradeStatus] = @TradeStatus 
			,[FillQty] = @FillQty
			,[FillPrice] = @FillPrice
			,[EndRawTime] = CASE WHEN (@TradeStatus < 0 OR @TradeStatus >= 2) THEN @LastRawTime ELSE [EndRawTime] END
			,[LastRawTime] =  @LastRawTime
		WHERE [Id] = @Id AND [TradeStatus] != 3;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_trade_updatestatus]
(
    @Id int,
	@TradeStatus int,
	@FillQty int,
	@FillPrice decimal(18,4)
)
AS
BEGIN
	UPDATE [Trade] 
		SET  [TradeStatus] = @TradeStatus 
			,[FillQty] = @FillQty
			,[FillPrice] = @FillPrice
			,[LastRawTime] =  DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE())
		WHERE [Id] = @Id AND [TradeStatus] != 3;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tradeorder_add]
(
    @TradeId int,
	@Qty int,
	@Price decimal(18,4),
	@Action varchar(100)
)

AS
BEGIN
	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	DECLARE @IdNew int = -1;
	INSERT INTO [dbo].[TradeOrder]
		(
			 [TradeId]
			,[RawTime]
			,[Action]
			,[Qty]
			,[Price]
			,[FillQty]
			,[FillPrice]
			,[Status]
			,[SyncId]
			,[BrokerRef]
			,[LastRawTime]
		)
		VALUES
		(
			 @TradeId
			,@LastRawTime
			,@Action
			,@Qty
			,@Price
			,0 -- FillQty
			,0 -- FillPrice
			,0  --Status
			,-1 -- @SyncId
			,null  -- @BrokerRef
			,@LastRawTime
		)
	SET @IdNew = SCOPE_IDENTITY();  
	RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tradeorder_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT   [Id]
			,[TradeId]
			,[RawTime]
			,[Action]
			,[Qty]
			,[Price]
			,[FillQty]
			,[FillPrice]
			,[Status]
			,[SyncId]
			,[BrokerRef]
			,[LastRawTime]
	  FROM [dbo].[TradeOrder]
	  WHERE [Id] = @Id;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tradeorder_select_all]
(
	@TradeId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT   [Id]
			,[TradeId]
			,[RawTime]
			,[Action]
			,[Qty]
			,[Price]
			,[FillQty]
			,[FillPrice]
			,[Status]
			,[SyncId]
			,[BrokerRef]
			,[LastRawTime]
	  FROM [dbo].[TradeOrder]
	  WHERE [TradeId] = @TradeId
	  ORDER BY [Id] ASC;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tradeorder_select_by_tradeid]
(
	@TradeId int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT   [Id]
			,[TradeId]
			,[RawTime]
			,[Action]
			,[Qty]
			,[Price]
			,[FillQty]
			,[FillPrice]
			,[Status]
			,[SyncId]
			,[BrokerRef]
			,[LastRawTime]
	  FROM [dbo].[TradeOrder]
	  WHERE [TradeId] = @TradeId
	  ORDER BY [Id] ASC;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tradeorder_update_status]
(
    @Id int,
    @SyncId int,
	@OrderStatus int,
	@FillQty int,
	@FillPrice decimal(18,4),
	@BrokerRef varchar(100)
)

AS
BEGIN
	DECLARE @LastRawTime int = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE());
	UPDATE [TradeOrder] 
		SET  [Status] = @OrderStatus 
			,[FillQty] = @FillQty
			,[FillPrice] = @FillPrice
			,[SyncId] = CASE WHEN [SyncId] <= 0 THEN @SyncId ELSE [SyncId] END
			,[BrokerRef] = CASE WHEN ([BrokerRef] IS NULL OR [BrokerRef] = '' OR [BrokerRef] = '0') THEN @BrokerRef ELSE [BrokerRef] END
			,[LastRawTime] = @LastRawTime 
		WHERE [Id] = @Id AND [Status] != 3;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_tradeorder_update_syncid]
(
    @Id int,
	@SyncId int
)

AS
BEGIN
	UPDATE [TradeOrder] 
		SET [SyncId] = @SyncId , 
			[LastRawTime] = DATEDIFF(SECOND, '1970-01-01 00:00:00', GETUTCDATE())
		WHERE [Id] = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigBotOnChange]
   ON  [dbo].[Bot]
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
			@EventType = 'BOT',
			@EventId = CAST(i.Id AS VARCHAR(10)),
			@Message = 'Inserted Automatically from Bot Trigger' 
		FROM inserted i;

	IF @EventType = 'BOT' 
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
ALTER TABLE [dbo].[Bot] ENABLE TRIGGER [trigBotOnChange]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigPriceDataOnChange]
   ON  [dbo].[PriceData]
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
			@EventType = 'PRICEDATA',
			@EventId = CAST(i.DataSetId AS VARCHAR(10)),
			@Message = 'Inserted Automatically from PriceData Trigger' 
		FROM inserted i;

	IF @EventType = 'PRICEDATA' 
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
ALTER TABLE [dbo].[PriceData] ENABLE TRIGGER [trigPriceDataOnChange]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigPriceDataOnDelete]
   ON  [dbo].[PriceData]
   AFTER DELETE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Exit early if no rows were deleted
    IF NOT EXISTS (SELECT 1 FROM deleted)
        RETURN;

    -- Handle all deleted rows
    MERGE INTO [Event] AS target
    USING (
        SELECT 
            'PRICEDATA_DEL' AS EventType,
            CAST(DataSetId AS VARCHAR(10)) AS EventId,
            'Inserted Automatically from PriceData Trigger' AS Message,
            MAX(GETDATE()) AS TimeStamp
        FROM deleted
		GROUP BY DataSetId  -- Group by the ID to ensure one row per EventId
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
ALTER TABLE [dbo].[PriceData] ENABLE TRIGGER [trigPriceDataOnDelete]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigStudyHistoryOnChange]
   ON  [dbo].[StudyHistory]
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
			@EventType = 'STUDYHISTORY',
			@EventId = CAST(i.StudyColId AS VARCHAR(10)),
			@Message = 'Inserted Automatically from StudyHistory Trigger' 
		FROM inserted i;

	IF @EventType = 'STUDYHISTORY' 
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
ALTER TABLE [dbo].[StudyHistory] ENABLE TRIGGER [trigStudyHistoryOnChange]
GO


SET IDENTITY_INSERT [dbo].[Ticker] ON 
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange]) VALUES (1, N'NQ', N'E-mini NASDAQ 100', N'FUT', N'USD', 20250919, CAST(0.250000 AS Decimal(18, 6)), 20, N'CME')
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange]) VALUES (2, N'MNQ', N'Micro E-Mini Nasdaq-100 Index', N'FUT', N'USD', 20250919, CAST(0.250000 AS Decimal(18, 6)), 2, N'CME')
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange]) VALUES (3, N'NQTEST', N'NQ', N'FUT', N'USD', 0, CAST(0.250000 AS Decimal(18, 6)), 20, N'CME')
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange]) VALUES (4, N'NQHIST', N'NQ Historical', N'FUT', N'USD', 20250919, CAST(0.250000 AS Decimal(18, 6)), 20, N'CME')
GO
SET IDENTITY_INSERT [dbo].[Ticker] OFF
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'1', N'1 Min', 1001, 60)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'10', N'10 Min', 1010, 600)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'10D', N'10 Days', 10010, 864000)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'10S', N'10 Sec', 10, 10)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'120', N'2 Hr', 1120, 7200)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'15', N'15 Min', 1015, 900)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'15S', N'15 Sec', 15, 15)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'1S', N'1 Sec', 1, 1)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'2', N'2 Min', 1002, 120)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'20', N'20 Min', 1020, 1200)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'20S', N'20 Sec', 20, 20)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'240', N'4 Hr', 1240, 14400)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'2D', N'2 Days', 10002, 172800)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'2M', N'2 Months', 1000002, 5184000)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'2S', N'2 Sec', 2, 2)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'2W', N'2 Weeks', 100002, 1209600)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'3', N'3 Min', 1003, 180)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'30', N'30 Min', 1030, 1800)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'30S', N'30 Sec', 30, 30)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'3D', N'3 Days', 10003, 259200)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'3M', N'3 Months', 1000003, 7776000)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'3S', N'3 Sec', 3, 3)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'3W', N'3 Weeks', 100003, 1814400)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'4', N'4 Min', 1004, 240)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'45', N'45 Min', 1045, 2700)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'45S', N'45 Sec', 45, 45)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'4D', N'4 Days', 10004, 345600)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'4S', N'4 Sec', 4, 4)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'5', N'5 Min', 1005, 300)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'5S', N'5 Sec', 5, 5)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'60', N'1 Hr', 1060, 3600)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'D', N'1 Day', 10001, 86400)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'M', N'1 Month', 1000001, 2592000)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'W', N'1 Week', 100001, 604800)
GO
INSERT [dbo].[Interval] ([Id], [Name], [Seq], [IntervalLen]) VALUES (N'Y', N'1 Year', 10000001, 31536000)
GO
SET IDENTITY_INSERT [dbo].[DataSet] ON 
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (1, 1, N'1')
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (2, 2, N'1')
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (3, 3, N'1')
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (4, 4, N'1')
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (5, 5, N'1')
GO
SET IDENTITY_INSERT [dbo].[DataSet] OFF
GO
INSERT [dbo].[DefaultParameter] ([Tag], [Desc], [DefaultValue], [ValueType], [Fixed]) VALUES (N'C21', N'Cycle Length', N'1932', N'INTEGER', 1)
GO
INSERT [dbo].[DefaultParameter] ([Tag], [Desc], [DefaultValue], [ValueType], [Fixed]) VALUES (N'StdDev1', N'Standard Deviation 1', N'1.0', N'DECIMAL', 1)
GO
INSERT [dbo].[DefaultParameter] ([Tag], [Desc], [DefaultValue], [ValueType], [Fixed]) VALUES (N'StdDev2', N'Standard Deviation 2', N'2.0', N'DECIMAL', 1)
GO
INSERT [dbo].[DefaultParameter] ([Tag], [Desc], [DefaultValue], [ValueType], [Fixed]) VALUES (N'StdDev3', N'Standard Deviation 3', N'0.6', N'DECIMAL', 1)
GO
INSERT [dbo].[DefaultParameter] ([Tag], [Desc], [DefaultValue], [ValueType], [Fixed]) VALUES (N'StudyInterval', N'Study Interval - if not set, will use AutoTrade interval', N'', N'STRING', 1)
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'1', N'1 min')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'10', N'10 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'10D', N'10 days')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'10S', N'10 secs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'120', N'2 hrs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'15', N'15 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'15S', N'15 secs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'1S', N'1 secs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'2', N'2 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'20', N'20 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'20S', N'20 secs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'240', N'4 hrs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'2D', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'2M', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'2S', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'2W', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'3', N'3 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'30', N'30 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'30S', N'30 secs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'3D', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'3M', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'3S', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'3W', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'4', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'45', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'45S', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'4D', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'4S', N'')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'5', N'5 mins')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'5S', N'5 secs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'60', N'1 hrs')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'D', N'1 days')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'M', N'1 months')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'W', N'1 weeks')
GO
INSERT [dbo].[IBKRInterval] ([IntervalId], [IBKRInterval]) VALUES (N'Y', N'')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (1, N'Open', N'open')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (2, N'Hig', N'high')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (3, N'Low', N'low')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (4, N'Close', N'close')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (5, N'(High+Low)/2', N'hl2')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (6, N'(Open+Close)/2', N'oc2')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (7, N'(High+Low+Close)/3', N'hlc3')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (8, N'(Open+High+Low+Close)/4', N'ohlc4')
GO
INSERT [dbo].[PriceSrc] ([Id], [Name], [Value]) VALUES (9, N'(High+Low+2*Close)/4', N'hlcc4')
GO
INSERT [dbo].[Strategy] ([Name], [Desc]) VALUES (N'L2dot1', N'Long Strategy 2.1')
GO
INSERT [dbo].[Strategy] ([Name], [Desc]) VALUES (N'LC5dot1', N'Long Strategy LC1')
GO
INSERT [dbo].[Strategy] ([Name], [Desc]) VALUES (N'LL8L6', N'Fast Long Strategy')
GO
INSERT [dbo].[Strategy] ([Name], [Desc]) VALUES (N'S100dot3', N'Short Strategy 100.3')
GO
