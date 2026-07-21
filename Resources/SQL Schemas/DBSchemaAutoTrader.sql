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
CREATE TABLE [dbo].[StudyCol](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TickerId] [int] NOT NULL,
	[IntervalId] [varchar](10) NOT NULL,
	[ColType] [varchar](100) NOT NULL,
	[OrderSeq] [int] NULL,
	[ColParams] [varchar](max) NOT NULL,
	[Enabled] [bit] NOT NULL,
 CONSTRAINT [PK_StudyCol] PRIMARY KEY CLUSTERED 
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
	[PeriodMultiplier] [int] NOT NULL,
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
CREATE TABLE [dbo].[Signal](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StrategyConfigId] [int] NOT NULL,
	[Tag] [varchar](50) NOT NULL,
	[Status] [varchar](10) NOT NULL,
	[Side] [decimal](18, 4) NOT NULL,
	[StartTime] [int] NULL,
	[StopTime] [int] NULL,
	[StartPrice] [decimal](18, 4) NULL,
	[StopPrice] [decimal](18, 4) NULL,
	[Canceled] [bit] NOT NULL,
	[LastUpdated] [int] NOT NULL,
 CONSTRAINT [PK_Signal] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StrategyConfig](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StrategyName] [varchar](100) NOT NULL,
	[StudyColId] [int] NOT NULL,
 CONSTRAINT [PK_StrategyConfig] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO






CREATE VIEW [dbo].[VSignal]
AS
SELECT S.[Id] AS [SignalId]
      ,S.[StrategyConfigId]
      ,SC.[StudyColId]
      ,SC.[StrategyName]
      ,Col.[IntervalId]
      ,I.[Name] AS [Interval]
      ,I.[IntervalLen]
      ,T.[LocalSymbol] AS [Ticker]
      ,T.[ContractSize] AS [TickerContractSize]
      ,S.[Tag]
      ,S.[Status]
      ,S.[Side]
      ,S.[StartTime]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[StartTime], 'UTC') AS [StartTimeUTC]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[StartTime], 'Eastern Standard Time') AS [StartTimeEST]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[StartTime], 'Arabian Standard Time') AS [StartTimeDubai]
      ,S.[StartPrice]
      ,S.[StopTime]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[StopTime], 'UTC') AS [StopTimeUTC]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[StopTime], 'Eastern Standard Time') AS [StopTimeEST]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[StopTime], 'Arabian Standard Time') AS [StopTimeDubai]
      ,S.[StopPrice]
      ,S.[Canceled]
      ,S.[LastUpdated]
	  ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](S.[LastUpdated], 'Eastern Standard Time') AS [LastUpdatedEST]
  FROM [dbo].[Signal] AS S
        INNER JOIN [dbo].[StrategyConfig] AS SC ON S.[StrategyConfigId] = SC.[Id]
        INNER JOIN [dbo].[StudyCol] AS Col ON SC.[StudyColId] = Col.[Id]
        INNER JOIN [dbo].[Ticker] AS T ON Col.[TickerId] = T.[Id]
        INNER JOIN [dbo].[Interval] AS I ON Col.[IntervalId] = I.[Id]

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





CREATE VIEW [dbo].[VStudyHistoryEx]
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
	  --,CData.SEMC5
	  --,CData.UBC5
      ,H.[Studies]
  FROM [StudyHistory] AS H
	  --CROSS APPLY OPENJSON(H.[Studies]) 
			--	WITH ( [SEMC5] DECIMAL(18,4) 'strict $.SEMC5',
			--		   [UBC5] DECIMAL(18,4) 'strict $.UBC5'
			--		 ) AS CData 


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
	  --,CData.SEMC1
	  --,CData.SEMC2
	  --,CData.SEMC3
	  ,CData.SEMC5
	  ,CData.SEMC8
	  ,CData.SEMC13
	  ,CData.SEMC21
	  ,CData.SEMC34
	  ,CData.SEMC55
	  ,CData.SEMC89
	  ,CData.C21_SEMTrend
	  ,CData.L3_SEMMATrigger
	  --,CData.MAC1
	  --,CData.MAC2
	  --,CData.MAC3
	  ,CData.MAC5
	  ,CData.LBC5
	  --,CData.C3_SEMT
	  --,CData.C5_C21_Trend
	  ,CData.trigger_LC5dot1
	  ,CData.LC5dot1_SIDE
	  ,CData.S100dot3_TRIGGER
	  ,CData.S100dot3_SIDE
	  ,CData.LC5dot1_TP_STAGE
	  ,CData.firstTime
      ,H.[Studies]
  FROM [StudyHistory] AS H
	  CROSS APPLY OPENJSON(H.[Studies]) 
				WITH ( 
					--[SEMC1] DECIMAL(18,4) 'lax $.SEMC1',
					--[SEMC2] DECIMAL(18,4) 'lax $.SEMC2',
					--[SEMC3] DECIMAL(18,4) 'lax $.SEMC3',
					[SEMC5] DECIMAL(18,4) 'lax  $.SEMC5',
					[SEMC8] DECIMAL(18,4) 'lax  $.SEMC8',
					[SEMC13] DECIMAL(18,4) 'lax  $.SEMC13',
					[SEMC21] DECIMAL(18,4) 'lax  $.SEMC21',
					[SEMC34] DECIMAL(18,4) 'lax  $.SEMC34',
					[SEMC55] DECIMAL(18,4) 'lax  $.SEMC55',
					[SEMC89] DECIMAL(18,4) 'lax  $.SEMC89',
					[C21_SEMTrend] DECIMAL(18,4) 'lax  $.C21_SEMTrend',
					[L3_SEMMATrigger] DECIMAL(18,4) 'lax  $.L3_SEMMATrigger',

					--[MAC1] DECIMAL(18,4) 'lax $.MAC1',
					--[MAC2] DECIMAL(18,4) 'lax $.MAC2',
					--[MAC3] DECIMAL(18,4) 'lax $.MAC3',
					[MAC5] DECIMAL(18,4) 'lax $.MAC5',
					[LBC5] DECIMAL(18,4) 'lax $.LBC5',
					--[C3_SEMT] DECIMAL(18,4) 'lax $.C3_SEMT',
					--[C5_C21_Trend] DECIMAL(18,4) 'lax $.C5_C21_Trend',
					[trigger_LC5dot1] DECIMAL(18,4) 'lax $.trigger_LC5dot1',
					[LC5dot1_SIDE] DECIMAL(18,4) 'lax $.LC5dot1_SIDE'
					,[S100dot3_TRIGGER] DECIMAL(18,4) 'lax $.S100dot3_TRIGGER'
					,[S100dot3_SIDE] DECIMAL(18,4) 'lax $.S100dot3_SIDE'
					,[LC5dot1_TP_STAGE] DECIMAL(18,4) 'lax $.LC5dot1_TP_STAGE'
					,[firstTime] DECIMAL(18,4) 'lax $.firstTime'
				) AS CData 


GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO









CREATE VIEW [dbo].[VStudyHistoryL20]
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
	  ,CData.[BaseL1_SIDE]
	  ,CData.[LBaseL1_SIDE]
	  ,CData.[SBaseL1_SIDE]
	  ,CData.[BaseL1_basicTrigger_DL1]
	  ,CData.[BaseL1_trigger_DL1]
	  ,CData.[BaseL1_trigger_L]
	  ,CData.[BaseL1_L2_MAC_SupRes]
	  ,CData.[BaseL1_firsttime]
	  ,CData.[C1_C5_S_Cont_LB1]
	  ,CData.[C1_C5_S_Cont_LB2]
	  ,CData.[C1UBLBDir]
      ,H.[Studies]
  FROM [StudyHistory] AS H
	  CROSS APPLY OPENJSON(H.[Studies]) 
				WITH ( 
					[BaseL1_SIDE] DECIMAL(18,4) 'lax  $.BaseL1_SIDE',
					[LBaseL1_SIDE] DECIMAL(18,4) 'lax  $.LBaseL1_SIDE',
					[SBaseL1_SIDE] DECIMAL(18,4) 'lax  $.SBaseL1_SIDE',
					[BaseL1_basicTrigger_DL1] DECIMAL(18,4) 'lax $.BaseL1_basicTrigger_DL1',
					[BaseL1_trigger_DL1] DECIMAL(18,4) 'lax $.BaseL1_trigger_DL1',
					[BaseL1_trigger_L] DECIMAL(18,4) 'lax  $.BaseL1_trigger_L',
					[BaseL1_L2_MAC_SupRes] DECIMAL(18,4) 'lax  $.BaseL1_L2_MAC_SupRes',
					[BaseL1_firsttime] DECIMAL(18,4) 'lax  $.BaseL1_firsttime',
					[C1_C5_S_Cont_LB1] DECIMAL(18,4) 'lax  $.C1_C5_S_Cont_LB1',
					[C1_C5_S_Cont_LB2] DECIMAL(18,4) 'lax  $.C1_C5_S_Cont_LB2',
					[C1UBLBDir] DECIMAL(18,4) 'lax  $.C1UBLBDirDir'
					
				) AS CData 

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO







CREATE VIEW [dbo].[VStudyHistoryADX]
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
	  ,CData.[LSIDE]
	  ,CData.[LTRIGGER]
	  ,cData.[LPRICEMA]
	  ,cData.[LGUIDEMA]
	  ,cData.[LGUIDEUB]
	  ,cData.[LGUIDELB]
	  ,cData.[ADX]
	  ,cData.[PDI]
	  ,cData.[NDI]
      ,H.[Studies]
  FROM [StudyHistory] AS H
	  CROSS APPLY OPENJSON(H.[Studies]) 
				WITH ( 
				--,LADXTREND_PRICEMA":28700.8438,"LADXTREND_GUIDEMA":28697.8594,"LADXTREND_LEVELMA":28690.8852}
					[LSIDE] DECIMAL(18,4) 'lax $.LBASEADX_SIDE',
					[LTRIGGER] DECIMAL(18,4) 'lax  $.LBASEADX_TRIGGER',
					[LPRICEMA] DECIMAL(18,4) 'lax  $.MAPrice',
					[LGUIDEMA] DECIMAL(18,4) 'lax  $.MIDGuide',
					[LGUIDEUB] DECIMAL(18,4) 'lax  $.UBGuide',
					[LGUIDELB] DECIMAL(18,4) 'lax  $.LBGuide',
					[ADX] DECIMAL(18,4) 'lax  $.LADX_ADX',
					[PDI] DECIMAL(18,4) 'lax  $.LADX_PDI',
					[NDI] DECIMAL(18,4) 'lax  $.LADX_NDI'
				) AS CData 
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO





CREATE VIEW [dbo].[VStudyHistoryL8L6]
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
	  ,CData.[MAL8]
	  ,CData.[MAL6]
	  ,CData.[LL8L6_SIDE]
      ,H.[Studies]
  FROM [StudyHistory] AS H
	  CROSS APPLY OPENJSON(H.[Studies]) 
				WITH ( 
					[MAL8] DECIMAL(18,4) 'lax  $.MAL8',
					[MAL6] DECIMAL(18,4) 'lax  $.MAL6',
					[LL8L6_SIDE] DECIMAL(18,4) 'lax $.LL8L6_SIDE'
				) AS CData 

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
	[BotId] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[AUM] [decimal](18, 4) NOT NULL,
	[Commission] [decimal](18, 4) NOT NULL,
 CONSTRAINT [PK_AutoTrade] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutoTradeSignal](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AutoTradeId] [int] NOT NULL,
	[SignalId] [int] NOT NULL,
	[Tag] [varchar](50) NOT NULL,
	[BotId] [int] NOT NULL,
	[TickerId] [int] NOT NULL,
	[OpenStatus] [varchar](20) NOT NULL,
	[OpenStatusCode] [int] NOT NULL,
	[OpenAvgPrice] [decimal](18, 4) NULL,
	[CloseStatus] [varchar](20) NOT NULL,
	[CloseStatusCode] [int] NOT NULL,
	[CloseAvgPrice] [decimal](18, 4) NULL,
	[OrigQty] [int] NOT NULL,
	[FilledQty] [int] NOT NULL,
	[ManualQty] [int] NOT NULL,
	[LastUpdated] [int] NOT NULL,
 CONSTRAINT [PK_AutoTradeSignal] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutoTradeSignalOrder](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AutoTradeSignalId] [int] NOT NULL,
	[OrderTag] [varchar](200) NOT NULL,
	[OrderTime] [int] NOT NULL,
	[Type] [varchar](20) NOT NULL,
	[LimitPrice] [decimal](18, 4) NOT NULL,
	[Qty] [int] NOT NULL,
	[QtyFilled] [int] NOT NULL,
	[AvgFillPrice] [decimal](18, 4) NOT NULL,
	[Status] [varchar](20) NOT NULL,
	[StatusCode] [int] NOT NULL,
	[RequestRef] [varchar](100) NOT NULL,
	[BrokerRef] [varchar](100) NULL,
	[CancelRequest] [bit] NOT NULL,
	[LastUpdated] [int] NOT NULL,
 CONSTRAINT [PK_AutoTradeSignalOrder] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AutoTradeStrategy](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AutoTradeId] [int] NOT NULL,
	[StrategyConfigId] [int] NOT NULL,
	[Config] [varchar](max) NOT NULL,
	[Qty] [int] NOT NULL,
	[QtyPct] [float] NOT NULL,
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
	[Group] [varchar](50) NOT NULL,
	[AccountId] [varchar](50) NOT NULL,
	[BrokerServiceId] [varchar](50) NOT NULL,
 CONSTRAINT [PK_Bot] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
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
CREATE TABLE [dbo].[OrderStatus](
	[Status] [varchar](20) NOT NULL,
	[Description] [varchar](256) NOT NULL,
 CONSTRAINT [PK_OrderStatus] PRIMARY KEY CLUSTERED 
(
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderStatusCode](
	[Code] [int] NOT NULL,
	[Status] [varchar](50) NOT NULL,
	[Desc] [varchar](1024) NOT NULL,
 CONSTRAINT [PK_OrderStatusCode] PRIMARY KEY CLUSTERED 
(
	[Code] ASC
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
CREATE TABLE [dbo].[SignalStatus](
	[Status] [varchar](10) NOT NULL,
	[Description] [varchar](256) NOT NULL,
 CONSTRAINT [PK_SignalStatus] PRIMARY KEY CLUSTERED 
(
	[Status] ASC
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
CREATE NONCLUSTERED INDEX [IX_AutoTradeSignalOrder_AutoTradeSignalId] ON [dbo].[AutoTradeSignalOrder]
(
	[AutoTradeSignalId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_AutoTradeSignalOrder_OrderTime] ON [dbo].[AutoTradeSignalOrder]
(
	[OrderTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AutoTradeSignalOrder_UQ_RequestRef] ON [dbo].[AutoTradeSignalOrder]
(
	[RequestRef] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_AutoTradeStrategy_UniqueKey] ON [dbo].[AutoTradeStrategy]
(
	[AutoTradeId] ASC,
	[StrategyConfigId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Bot_UNQ] ON [dbo].[Bot]
(
	[AccountId] ASC
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
CREATE UNIQUE NONCLUSTERED INDEX [IX_StrategyConfig_UNQ] ON [dbo].[StrategyConfig]
(
	[StrategyName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_StudyCol] ON [dbo].[StudyCol]
(
	[TickerId] ASC,
	[IntervalId] ASC,
	[ColType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_StudyHistory_StudyColId_RawTime] ON [dbo].[StudyHistory]
(
	[StudyColId] ASC,
	[RawTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_Ticker_LocalSymbol] ON [dbo].[Ticker]
(
	[LocalSymbol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Ticker_Symbol] ON [dbo].[Ticker]
(
	[Symbol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_Ver]  DEFAULT ((1)) FOR [Ver]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_BotId]  DEFAULT ((-1)) FOR [BotId]
GO
ALTER TABLE [dbo].[AutoTrade] ADD  CONSTRAINT [DF_AutoTrade_Status]  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[Signal] ADD  CONSTRAINT [DF_Signal_Canceled]  DEFAULT ((0)) FOR [Canceled]
GO
ALTER TABLE [dbo].[StudyCol] ADD  DEFAULT ((1)) FOR [Enabled]
GO
ALTER TABLE [dbo].[AutoTrade]  WITH CHECK ADD  CONSTRAINT [FK_AutoTrade_Bot] FOREIGN KEY([BotId])
REFERENCES [dbo].[Bot] ([Id])
GO
ALTER TABLE [dbo].[AutoTrade] CHECK CONSTRAINT [FK_AutoTrade_Bot]
GO
ALTER TABLE [dbo].[AutoTrade]  WITH CHECK ADD  CONSTRAINT [FK_AutoTrade_Ticker] FOREIGN KEY([TickerId])
REFERENCES [dbo].[Ticker] ([Id])
GO
ALTER TABLE [dbo].[AutoTrade] CHECK CONSTRAINT [FK_AutoTrade_Ticker]
GO
ALTER TABLE [dbo].[AutoTradeSignal]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeSignal_AutoTrade] FOREIGN KEY([AutoTradeId])
REFERENCES [dbo].[AutoTrade] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AutoTradeSignal] CHECK CONSTRAINT [FK_AutoTradeSignal_AutoTrade]
GO
ALTER TABLE [dbo].[AutoTradeSignal]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeSignal_OrderStatus] FOREIGN KEY([OpenStatus])
REFERENCES [dbo].[OrderStatus] ([Status])
GO
ALTER TABLE [dbo].[AutoTradeSignal] CHECK CONSTRAINT [FK_AutoTradeSignal_OrderStatus]
GO
ALTER TABLE [dbo].[AutoTradeSignal]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeSignal_OrderStatus1] FOREIGN KEY([CloseStatus])
REFERENCES [dbo].[OrderStatus] ([Status])
GO
ALTER TABLE [dbo].[AutoTradeSignal] CHECK CONSTRAINT [FK_AutoTradeSignal_OrderStatus1]
GO
ALTER TABLE [dbo].[AutoTradeSignal]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeSignal_Signal] FOREIGN KEY([SignalId])
REFERENCES [dbo].[Signal] ([Id])
GO
ALTER TABLE [dbo].[AutoTradeSignal] CHECK CONSTRAINT [FK_AutoTradeSignal_Signal]
GO
ALTER TABLE [dbo].[AutoTradeSignalOrder]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeSignalOrder_AutoTradeSignal] FOREIGN KEY([AutoTradeSignalId])
REFERENCES [dbo].[AutoTradeSignal] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AutoTradeSignalOrder] CHECK CONSTRAINT [FK_AutoTradeSignalOrder_AutoTradeSignal]
GO
ALTER TABLE [dbo].[AutoTradeSignalOrder]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeSignalOrder_OrderStatus] FOREIGN KEY([Status])
REFERENCES [dbo].[OrderStatus] ([Status])
GO
ALTER TABLE [dbo].[AutoTradeSignalOrder] CHECK CONSTRAINT [FK_AutoTradeSignalOrder_OrderStatus]
GO
ALTER TABLE [dbo].[AutoTradeStrategy]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeStrategy_AutoTrade] FOREIGN KEY([AutoTradeId])
REFERENCES [dbo].[AutoTrade] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AutoTradeStrategy] CHECK CONSTRAINT [FK_AutoTradeStrategy_AutoTrade]
GO
ALTER TABLE [dbo].[AutoTradeStrategy]  WITH CHECK ADD  CONSTRAINT [FK_AutoTradeStrategy_StrategyConfig] FOREIGN KEY([StrategyConfigId])
REFERENCES [dbo].[StrategyConfig] ([Id])
GO
ALTER TABLE [dbo].[AutoTradeStrategy] CHECK CONSTRAINT [FK_AutoTradeStrategy_StrategyConfig]
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
ALTER TABLE [dbo].[Signal]  WITH CHECK ADD  CONSTRAINT [FK_Signal_SignalStatus] FOREIGN KEY([Status])
REFERENCES [dbo].[SignalStatus] ([Status])
GO
ALTER TABLE [dbo].[Signal] CHECK CONSTRAINT [FK_Signal_SignalStatus]
GO
ALTER TABLE [dbo].[Signal]  WITH CHECK ADD  CONSTRAINT [FK_Signal_StrategyConfig] FOREIGN KEY([StrategyConfigId])
REFERENCES [dbo].[StrategyConfig] ([Id])
GO
ALTER TABLE [dbo].[Signal] CHECK CONSTRAINT [FK_Signal_StrategyConfig]
GO
ALTER TABLE [dbo].[StrategyConfig]  WITH CHECK ADD  CONSTRAINT [FK_StrategyConfig_StudyCol] FOREIGN KEY([StudyColId])
REFERENCES [dbo].[StudyCol] ([Id])
GO
ALTER TABLE [dbo].[StrategyConfig] CHECK CONSTRAINT [FK_StrategyConfig_StudyCol]
GO
ALTER TABLE [dbo].[StudyCol]  WITH CHECK ADD  CONSTRAINT [FK_StudyCol_Interval] FOREIGN KEY([IntervalId])
REFERENCES [dbo].[Interval] ([Id])
GO
ALTER TABLE [dbo].[StudyCol] CHECK CONSTRAINT [FK_StudyCol_Interval]
GO
ALTER TABLE [dbo].[StudyCol]  WITH CHECK ADD  CONSTRAINT [FK_StudyCol_Ticker] FOREIGN KEY([TickerId])
REFERENCES [dbo].[Ticker] ([Id])
GO
ALTER TABLE [dbo].[StudyCol] CHECK CONSTRAINT [FK_StudyCol_Ticker]
GO
ALTER TABLE [dbo].[StudyHistory]  WITH NOCHECK ADD  CONSTRAINT [FK_StudyHistory_StudyCol] FOREIGN KEY([StudyColId])
REFERENCES [dbo].[StudyCol] ([Id])
GO
ALTER TABLE [dbo].[StudyHistory] CHECK CONSTRAINT [FK_StudyHistory_StudyCol]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-41244eaf-cd5e-4f40-87cb-8d6a069add82] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-41244eaf-cd5e-4f40-87cb-8d6a069add82]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-41244eaf-cd5e-4f40-87cb-8d6a069add82] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-41244eaf-cd5e-4f40-87cb-8d6a069add82') > 0)   DROP SERVICE [SqlQueryNotificationService-41244eaf-cd5e-4f40-87cb-8d6a069add82]; if (OBJECT_ID('SqlQueryNotificationService-41244eaf-cd5e-4f40-87cb-8d6a069add82', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-41244eaf-cd5e-4f40-87cb-8d6a069add82]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-41244eaf-cd5e-4f40-87cb-8d6a069add82]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-af514c57-493f-4b4f-a605-4077db4a3688] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-af514c57-493f-4b4f-a605-4077db4a3688]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-af514c57-493f-4b4f-a605-4077db4a3688] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-af514c57-493f-4b4f-a605-4077db4a3688') > 0)   DROP SERVICE [SqlQueryNotificationService-af514c57-493f-4b4f-a605-4077db4a3688]; if (OBJECT_ID('SqlQueryNotificationService-af514c57-493f-4b4f-a605-4077db4a3688', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-af514c57-493f-4b4f-a605-4077db4a3688]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-af514c57-493f-4b4f-a605-4077db4a3688]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SqlQueryNotificationStoredProcedure-dd616aa5-fb4d-4e2d-a542-3abde9e837cd] AS BEGIN BEGIN TRANSACTION; RECEIVE TOP(0) conversation_handle FROM [SqlQueryNotificationService-dd616aa5-fb4d-4e2d-a542-3abde9e837cd]; IF (SELECT COUNT(*) FROM [SqlQueryNotificationService-dd616aa5-fb4d-4e2d-a542-3abde9e837cd] WHERE message_type_name = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer') > 0 BEGIN if ((SELECT COUNT(*) FROM sys.services WHERE name = 'SqlQueryNotificationService-dd616aa5-fb4d-4e2d-a542-3abde9e837cd') > 0)   DROP SERVICE [SqlQueryNotificationService-dd616aa5-fb4d-4e2d-a542-3abde9e837cd]; if (OBJECT_ID('SqlQueryNotificationService-dd616aa5-fb4d-4e2d-a542-3abde9e837cd', 'SQ') IS NOT NULL)   DROP QUEUE [SqlQueryNotificationService-dd616aa5-fb4d-4e2d-a542-3abde9e837cd]; DROP PROCEDURE [SqlQueryNotificationStoredProcedure-dd616aa5-fb4d-4e2d-a542-3abde9e837cd]; END COMMIT TRANSACTION; END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_admin_check_login_membership]
(
    @LoginName sysname,
    @DbListCsv nvarchar(max)   -- e.g. 'FIGAutoTrade,FIGMonitor,FIGUser'
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DBs TABLE (DbName sysname PRIMARY KEY);

    ;WITH s AS
    (
        SELECT LTRIM(RTRIM(value)) AS DbName
        FROM string_split(@DbListCsv, ',')
        WHERE LTRIM(RTRIM(value)) <> ''
    )
    INSERT INTO @DBs (DbName)
    SELECT DISTINCT DbName
    FROM s;

    CREATE TABLE #Results
    (
        DatabaseName sysname NOT NULL,
        LoginName sysname NOT NULL,
        DBExists bit NOT NULL,
        UserReg bit NOT NULL,
        IsMappedToLoginSID bit NOT NULL,
        IsOwner bit NOT NULL,
        IsPublic bit NOT NULL
    );

    DECLARE @db sysname;
    DECLARE @sql nvarchar(max);

    DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DbName FROM @DBs;

    OPEN db_cursor;
    FETCH NEXT FROM db_cursor INTO @db;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF DB_ID(@db) IS NULL
        BEGIN
            INSERT INTO #Results
            (DatabaseName, LoginName, DBExists, UserReg, IsMappedToLoginSID, IsOwner, IsPublic)
            VALUES
            (@db, @LoginName, 0, 0, 0, 0, 0);
        END
        ELSE
        BEGIN
            SET @sql = N'
INSERT INTO #Results
(DatabaseName, LoginName, DBExists, UserReg, IsMappedToLoginSID, IsOwner, IsPublic)
SELECT
    @DbName AS DatabaseName,
    @LoginName AS LoginName,
    CAST(1 AS bit) AS DBExists,

    CAST(CASE WHEN EXISTS
    (
        SELECT 1
        FROM ' + QUOTENAME(@db) + N'.sys.database_principals dp
        WHERE dp.name = @LoginName
    ) THEN 1 ELSE 0 END AS bit) AS UserReg,

    CAST(CASE WHEN EXISTS
    (
        SELECT 1
        FROM ' + QUOTENAME(@db) + N'.sys.database_principals dp
        JOIN sys.server_principals sp
            ON sp.name = @LoginName
        WHERE dp.name = @LoginName
          AND dp.sid = sp.sid
    ) THEN 1 ELSE 0 END AS bit) AS IsMappedToLoginSID,

    CAST(CASE WHEN EXISTS
    (
        SELECT 1
        FROM ' + QUOTENAME(@db) + N'.sys.database_role_members drm
        JOIN ' + QUOTENAME(@db) + N'.sys.database_principals r
            ON r.principal_id = drm.role_principal_id
        JOIN ' + QUOTENAME(@db) + N'.sys.database_principals u
            ON u.principal_id = drm.member_principal_id
        WHERE u.name = @LoginName
          AND r.name = N''db_owner''
    ) THEN 1 ELSE 0 END AS bit) AS IsOwner,

    CAST(CASE WHEN EXISTS
    (
        SELECT 1
        FROM ' + QUOTENAME(@db) + N'.sys.database_role_members drm
        JOIN ' + QUOTENAME(@db) + N'.sys.database_principals r
            ON r.principal_id = drm.role_principal_id
        JOIN ' + QUOTENAME(@db) + N'.sys.database_principals u
            ON u.principal_id = drm.member_principal_id
        WHERE u.name = @LoginName
          AND r.name = N''public''
    ) THEN 1 ELSE 0 END AS bit) AS IsPublic;
';

            EXEC sys.sp_executesql
                @sql,
                N'@DbName sysname, @LoginName sysname',
                @DbName = @db,
                @LoginName = @LoginName;
        END

        FETCH NEXT FROM db_cursor INTO @db;
    END

    CLOSE db_cursor;
    DEALLOCATE db_cursor;

    SELECT *
    FROM #Results
    ORDER BY DatabaseName;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_admin_databases_select]
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
CREATE   PROCEDURE [dbo].[usp_admin_get_query_notification_status]
(
    @LoginName  sysname,
    @DbName     sysname
)
AS
BEGIN
    SET NOCOUNT ON;

    -- If DB doesn't exist, return one row
    IF DB_ID(@DbName) IS NULL
    BEGIN
        SELECT
            @DbName AS DatabaseName,
            @LoginName AS LoginName,
            CAST(0 AS bit) AS [Exists],
            ISNULL(CAST(NULL AS bit), 0) AS [Enabled],
            CAST(0 AS bit) AS Permitted;
        RETURN;
    END;

    DECLARE @sql nvarchar(max) = N'
SELECT
    d.name AS DatabaseName,
    @LoginName AS LoginName,
    CAST(1 AS bit) AS [Exists],
    d.is_broker_enabled AS [Enabled],
    CASE WHEN COALESCE(p.PermissionState, ''REVOKE'') = ''GRANT'' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Permitted
FROM (SELECT name, is_broker_enabled
      FROM master.sys.databases
      WHERE name = @DbName) AS d
LEFT JOIN
(
    SELECT TOP (1)
        dp.state_desc AS PermissionState,
        dp.permission_name AS Permission
    FROM ' + QUOTENAME(@DbName) + N'.sys.database_permissions dp
    WHERE dp.permission_name = N''SUBSCRIBE QUERY NOTIFICATIONS''
      AND dp.class_desc = N''DATABASE''
      AND USER_NAME(dp.grantee_principal_id) = @LoginName
) AS p
ON 1 = 1;
';

    EXEC sys.sp_executesql
        @sql,
        N'@DbName sysname, @LoginName sysname',
        @DbName = @DbName,
        @LoginName = @LoginName;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_delete]
	@Id int
AS
BEGIN
	Delete [AutoTrade] Where Id = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_select]
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
		  ,[BotId]
		  ,[Status]
		  ,[AUM]
		  ,[Commission]
		FROM [AutoTrade]
		WHERE @Id = -1 OR @Id = Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_order_cancel]
(
    @Id int
)
AS
BEGIN
	UPDATE [dbo].[AutoTradeSignalOrder] SET 
			[CancelRequest] = 1,
			[LastUpdated] = DATEDIFF(SECOND, '19700101', GETUTCDATE())
		WHERE [Id] = @Id;
	RETURN @@ROWCOUNT;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_order_query_by_autotrade_signal_id]
  @AutoTradeSignalId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
           [Id]
          ,[AutoTradeSignalId]
          ,[OrderTag]
          ,[OrderTime]
          ,[Type]
          ,[LimitPrice]
          ,[Qty]
          ,[QtyFilled]
          ,[AvgFillPrice]
          ,[Status]
          ,[StatusCode]
          ,[RequestRef]
          ,[BrokerRef]
          ,[CancelRequest]
          ,[LastUpdated]
      FROM [dbo].[AutoTradeSignalOrder]
      WHERE [AutoTradeSignalId] = @AutoTradeSignalId
      ORDER BY [OrderTime] ASC


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_order_query_by_requestref]
  @RequestRef varchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 
           [Id]
          ,[AutoTradeSignalId]
          ,[OrderTag]
          ,[OrderTime]
          ,[Type]
          ,[LimitPrice]
          ,[Qty]
          ,[QtyFilled]
          ,[AvgFillPrice]
          ,[Status]
          ,[StatusCode]
          ,[RequestRef]
          ,[BrokerRef]
          ,[CancelRequest]
          ,[LastUpdated]
      FROM [dbo].[AutoTradeSignalOrder]
      WHERE [RequestRef] = @RequestRef
      ORDER BY [OrderTime] DESC


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_order_query_last]
  @AutoTradeSignalId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 
           [Id]
          ,[AutoTradeSignalId]
          ,[OrderTag]
          ,[OrderTime]
          ,[Type]
          ,[LimitPrice]
          ,[Qty]
          ,[QtyFilled]
          ,[AvgFillPrice]
          ,[Status]
          ,[StatusCode]
          ,[RequestRef]
          ,[BrokerRef]
          ,[CancelRequest]
          ,[LastUpdated]
      FROM [dbo].[AutoTradeSignalOrder]
      WHERE [AutoTradeSignalId] = @AutoTradeSignalId
      ORDER BY [OrderTime] DESC


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_order_select]
  @Id int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 
           [Id]
          ,[AutoTradeSignalId]
          ,[OrderTag]
          ,[OrderTime]
          ,[Type]
          ,[LimitPrice]
          ,[Qty]
          ,[QtyFilled]
          ,[AvgFillPrice]
          ,[Status]
          ,[StatusCode]
          ,[RequestRef]
          ,[BrokerRef]
          ,[CancelRequest]
          ,[LastUpdated]
      FROM [dbo].[AutoTradeSignalOrder]
      WHERE [Id] = @Id
      ORDER BY [OrderTime] DESC


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_order_upsert]
(
	@IdNew int OUTPUT,
    @Id int,
    @AutoTradeSignalId int,
	@OrderTag varchar(20),
    @OrderTime int,
    @Type varchar(20),
	@LimitPrice decimal(18,4),
    @Qty int,
    @QtyFilled int,
	@AvgFillPrice decimal(18, 4),
    @Status varchar(20),
    @StatusCode int,
    @RequestRef varchar(100),
    @BrokerRef varchar(100),
	@CancelRequest bit,
    @LastUpdated int
)
AS
BEGIN
    IF @LastUpdated = -1 
    BEGIN
     SET @LastUpdated = DATEDIFF(SECOND, '19700101', GETUTCDATE());
    END

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [AutoTradeSignalOrder] WHERE @Id = [Id];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [AutoTradeSignalOrder] (
			   [AutoTradeSignalId]
			  ,[OrderTag]
			  ,[OrderTime]
			  ,[Type]
			  ,[LimitPrice]
			  ,[Qty]
			  ,[QtyFilled]
			  ,[AvgFillPrice]
			  ,[Status]
			  ,[StatusCode]
			  ,[RequestRef]
			  ,[BrokerRef]
			  ,[CancelRequest]
			  ,[LastUpdated]
			)
		VALUES (
				@AutoTradeSignalId,
				@OrderTag,
				@OrderTime,
				@Type,
				@LimitPrice,
				@Qty,
				@QtyFilled,
				@AvgFillPrice,
				@Status,
				@StatusCode,
				@RequestRef,
				@BrokerRef,
				@CancelRequest,
				@LastUpdated
			)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [dbo].[AutoTradeSignalOrder]
		   SET  [AutoTradeSignalId] = @AutoTradeSignalId,
				[OrderTag]			= @OrderTag,
				[OrderTime]         = @OrderTime,
				[Type]				= @Type,
				[LimitPrice]		= @LimitPrice,
				[Qty]               = @Qty,
				[QtyFilled]         = @QtyFilled,
				[AvgFillPrice]      = @AvgFillPrice,
				[Status]            = @Status,
				[StatusCode]        = @StatusCode,
				[RequestRef]        = @RequestRef,
				[BrokerRef]         = @BrokerRef,
				[CancelRequest]		= @CancelRequest,
				[LastUpdated]       = @LastUpdated
		  WHERE [Id] = @IdNew

	END

	RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_query_signalid]
  @SignalId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
         [Id]
        ,[AutoTradeId]
        ,[SignalId]
        ,[Tag]
        ,[BotId]
        ,[TickerId]
        ,[OpenStatus]
        ,[OpenStatusCode]
        ,[OpenAvgPrice]
        ,[CloseStatus]
        ,[CloseStatusCode]
        ,[CloseAvgPrice]
        ,[OrigQty]
        ,[FilledQty]
        ,[ManualQty]
        ,[LastUpdated]
      FROM [dbo].[AutoTradeSignal]
      WHERE [SignalId] = @SignalId


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_query_top]
  @AutoTradeId int
AS
BEGIN
    SET NOCOUNT ON;

    -- RETURNS top records for each StrategyConfigId ordered by Signal.StartTime DESC order
    ;WITH Ranked AS
    (
        SELECT
             A.[Id]
            ,A.[AutoTradeId]
            ,A.[SignalId]
            ,A.[Tag]
            ,A.[BotId]
            ,A.[TickerId]
            ,A.[OpenStatus]
            ,A.[OpenStatusCode]
            ,A.[OpenAvgPrice]
            ,A.[CloseStatus]
            ,A.[CloseStatusCode]
            ,A.[CloseAvgPrice]
            ,A.[OrigQty]
            ,A.[FilledQty]
            ,A.[ManualQty]
            ,A.[LastUpdated]
            ,S.[StrategyConfigId]
            ,S.[StartTime]
            ,ROW_NUMBER() OVER
              (
                PARTITION BY S.[StrategyConfigId]
                ORDER BY S.[StartTime] DESC, A.[Id] DESC
              ) AS rn
        FROM [dbo].[AutoTradeSignal] AS A WITH (UPDLOCK, READPAST, ROWLOCK)
            INNER JOIN [dbo].[AutoTradeStrategy] AS STRA ON A.[AutoTradeId] = STRA.[AutoTradeId] AND STRA.[Enabled] = 1
            INNER JOIN [dbo].[Signal] AS S ON S.[StrategyConfigId] = STRA.[StrategyConfigId]
        WHERE @AutoTradeId = -1 OR A.[AutoTradeId] = @AutoTradeId
    )
    SELECT
         [Id]
        ,[AutoTradeId]
        ,[SignalId]
        ,[Tag]
        ,[BotId]
        ,[TickerId]
        ,[OpenStatus]
        ,[OpenStatusCode]
        ,[OpenAvgPrice]
        ,[CloseStatus]
        ,[CloseStatusCode]
        ,[CloseAvgPrice]
        ,[OrigQty]
        ,[FilledQty]
        ,[ManualQty]
        ,[LastUpdated]
    FROM Ranked
    WHERE rn = 1
    ORDER BY [StartTime] DESC;

END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_select]
  @Id int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 
         [Id]
        ,[AutoTradeId]
        ,[SignalId]
        ,[Tag]
        ,[BotId]
        ,[TickerId]
        ,[OpenStatus]
        ,[OpenStatusCode]
        ,[OpenAvgPrice]
        ,[CloseStatus]
        ,[CloseStatusCode]
        ,[CloseAvgPrice]
        ,[OrigQty]
        ,[FilledQty]
        ,[ManualQty]
        ,[LastUpdated]
      FROM [dbo].[AutoTradeSignal]
      WHERE [Id] = @Id


END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_autotrade_signal_set_manual_qty]
(
    @Id int,
    @ManualQty int
)
AS
BEGIN
    DECLARE @LastUpdated int = DATEDIFF(SECOND, '19700101', GETUTCDATE());

    UPDATE [dbo].[AutoTradeSignal]
       SET [OpenStatus] = 'FILLED_MANUALLY',
           [ManualQty] = @ManualQty,
           [LastUpdated] = @LastUpdated
     WHERE [Id] = @Id
       AND [CloseStatus] NOT IN ('FILLED', 'FAILED', 'CANCELED', 'FILLED_PARTIALLY_FIN')
       AND (@ManualQty = 0 OR (@ManualQty > 0 AND [OrigQty] > 0) OR (@ManualQty < 0 AND [OrigQty] < 0));

    RETURN @@ROWCOUNT;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_signal_upsert]
(
	@IdNew int OUTPUT,
    @Id int,
    @AutoTradeId int,
    @SignalId int,
    @Tag varchar(50),
    @BotId int,
    @TickerId int,
    @OpenStatus varchar(20),
    @OpenStatusCode int,
	@OpenAvgPrice decimal(18,4),
    @CloseStatus varchar(20),
    @CloseStatusCode int,
	@CloseAvgPrice decimal(18,4),
    @OrigQty int,
    @FilledQty int,
    @ManualQty int,
    @LastUpdated int
)

AS
BEGIN
    IF @LastUpdated = -1 
    BEGIN
     SET @LastUpdated = DATEDIFF(SECOND, '19700101', GETUTCDATE());
    END

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [AutoTradeSignal] WHERE @Id = [Id];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [AutoTradeSignal] (
			   [AutoTradeId]
			  ,[SignalId]
			  ,[Tag]
			  ,[BotId]
			  ,[TickerId]
			  ,[OpenStatus]
			  ,[OpenStatusCode]
			  ,[OpenAvgPrice]
			  ,[CloseStatus]
			  ,[CloseStatusCode]
			  ,[CloseAvgPrice]
			  ,[OrigQty]
			  ,[FilledQty]
			  ,[ManualQty]
			  ,[LastUpdated]
		)
		VALUES (
			  @AutoTradeId
			, @SignalId
			, @Tag
			, @BotId
			, @TickerId
			, @OpenStatus
			, @OpenStatusCode
			, @OpenAvgPrice
			, @CloseStatus
			, @CloseStatusCode
			, @CloseAvgPrice
			, @OrigQty
			, @FilledQty
			, @ManualQty
			, @LastUpdated
		)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [dbo].[AutoTradeSignal]
		   SET [AutoTradeId] = @AutoTradeId
			  ,[SignalId] = @SignalId
			  ,[Tag] = @Tag
			  ,[BotId] = @BotId
			  ,[TickerId] = @TickerId
			  ,[OpenStatus] = @OpenStatus
			  ,[OpenStatusCode] = @OpenStatusCode
			  ,[OpenAvgPrice] = @OpenAvgPrice
			  ,[CloseStatus] = @CloseStatus
			  ,[CloseStatusCode] = @CloseStatusCode
			  ,[CloseAvgPrice] = @CloseAvgPrice
			  ,[OrigQty] = @OrigQty
			  ,[FilledQty] = @FilledQty
			  ,[ManualQty] = @ManualQty
			  ,[LastUpdated] = @LastUpdated
		 WHERE Id = @IdNew

	END

	RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_strategy_delete]
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

CREATE PROCEDURE [dbo].[usp_autotrade_strategy_select] 
	@Id int = -1,
	@AutoTradeId int = -1
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT [Id]
		  ,[AutoTradeId]
		  ,[StrategyConfigId]
		  ,[Config]
		  ,[Qty]
		  ,[QtyPct]
		  ,[Enabled]
		FROM [dbo].[AutoTradeStrategy]
		WHERE (@Id = -1 OR [Id] = @Id) AND (@AutoTradeId = -1 OR [AutoTradeId] = @AutoTradeId);
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_strategy_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@AutoTradeId int,
	@StrategyConfigId int,
	@Config varchar(max),
	@Qty int,
	@QtyPct float,
	@Enabled bit
)

AS
BEGIN
	SET NOCOUNT ON;

	-- Check if strategy exists
	SELECT	 @IdNew = [Id] 
		FROM [AutoTradeStrategy] 
		WHERE @AutoTradeId = [AutoTradeId] AND @StrategyConfigId = [StrategyConfigId];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [AutoTradeStrategy] (
			 [AutoTradeId]
			,[StrategyConfigId]
			,[Config]
			,[Qty]
			,[QtyPct]
			,[Enabled]
			)
		VALUES (
			 @AutoTradeId
			,@StrategyConfigId
			,@Config
			,@Qty
			,@QtyPct
			,@Enabled
			)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [AutoTradeStrategy] SET 
				 [AutoTradeId] = @AutoTradeId
				,[StrategyConfigId] = @StrategyConfigId
				,[Config] = @Config
				,[Qty] = @Qty
				,[QtyPct] = @QtyPct
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
CREATE PROCEDURE [dbo].[usp_autotrade_update_status]
(
	@Id int,
	@Status int
)

AS
BEGIN
	SET NOCOUNT ON;
	UPDATE [AutoTrade] SET [Status] = @Status WHERE Id = @Id
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_autotrade_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@Name varchar(256),
	@Ver int,
	@TickerId int,
	@BotId int,
	@Status int,
	@AUM decimal(18,6),
	@Commission decimal(18,4)
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
			  ,[BotId]
			  ,[Status]
			  ,[AUM]
			  ,[Commission]
		)
		VALUES (
			  @Name
			, @Ver
			, @TickerId
			, @BotId
			, @Status
			, @AUM
			, @Commission
		)

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [AutoTrade] SET 
			  [Name] = @Name
			, [Ver] = @Ver
			, [TickerId] = @TickerId
			, [BotId] = @BotId
			, [Status] = @Status
			, [AUM] = @AUM
			, [Commission] = @Commission
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
	@AccountId varchar(50)
)
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[Group]
		  ,[AccountId]
		  ,[BrokerServiceId]
		FROM [dbo].[Bot]
		WHERE @AccountId = [AccountId]
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
		  ,[Group]
		  ,[AccountId]
		  ,[BrokerServiceId]
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
	@Group varchar(50),
	@AccountId varchar(50),
	@BrokerServiceId varchar(50)
)

AS
BEGIN

	-- Check if Symbol exists
	SELECT @IdNew = [Id] FROM [Bot] WHERE [Id] = @Id;
	IF (@@ROWCOUNT = 0 OR @IdNew = -1)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [Bot] (
			  [Group]
			, [AccountId]
			, [BrokerServiceId]
		)
		VALUES (
			  @Group
			, @AccountId
			, @BrokerServiceId
		)
		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values

		-- now update Backtest
		UPDATE [Bot]  SET
			  [Group] = @Group
			, [AccountId] = @AccountId
			, [BrokerServiceId] = @BrokerServiceId
		WHERE Id = @IdNew

	END

	RETURN @IdNew;
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

CREATE PROCEDURE [dbo].[usp_price_data_select_exact] 
	@DataSetId int,
	@StartTime int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT	 [Id]
			,[DataSetId]
			,[RawTime]
			,[PriceDate]
			,[Open]
			,[High]
			,[Low]
			,[Close]
			,[Volume]
		FROM [dbo].[PriceData]
		WHERE DataSetId = @DataSetId AND [RawTime] = @StartTime;
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
CREATE PROCEDURE [dbo].[usp_price_data_truncate]
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
CREATE PROCEDURE [dbo].[usp_report_strategy_perf]
AS

DECLARE @StartingNAV decimal(18, 4) = 14000000;
DECLARE @Multiplier decimal(18, 4) = 25 * 20;

WITH Trades AS
(
    SELECT
        StrategyName,
        YEAR(StopTimeEST) AS TradeYear,
        CASE 
            WHEN Side > 0 THEN (StopPrice - StartPrice) * @Multiplier
            WHEN Side < 0 THEN (StartPrice - StopPrice) * @Multiplier
            ELSE 0
        END AS Profit
    FROM [FIGAutoTrader].[dbo].[VSignal]
    WHERE StopTimeEST IS NOT NULL
      AND StartPrice IS NOT NULL
      AND StopPrice IS NOT NULL
),
Yearly AS
(
    SELECT
        StrategyName,
        TradeYear,
        COUNT(*) AS SignalCount,
        SUM(Profit) AS NetProfit
    FROM Trades
    GROUP BY StrategyName, TradeYear
),
Cumulative AS
(
    SELECT
        StrategyName,
        TradeYear,
        SignalCount,
        NetProfit,

        SUM(NetProfit) OVER (
            ORDER BY TradeYear
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS CumulativeNetProfit,

        SUM(SignalCount) OVER (
            ORDER BY TradeYear
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS CumulativeSignalCount
    FROM Yearly
)
SELECT
    StrategyName,
    TradeYear,

    SignalCount,
    CumulativeSignalCount,

    CAST(NetProfit AS decimal(18, 2)) AS NetProfit,

    CAST(NetProfit / @StartingNAV AS decimal(18, 6)) AS NetProfitPct,

    CAST(100.0 * NetProfit / @StartingNAV AS decimal(18, 2)) AS NetProfitPercent,

    CAST(1 + NetProfit / @StartingNAV AS decimal(18, 6)) AS YearNAV,

    CAST(CumulativeNetProfit AS decimal(18, 2)) AS CumulativeNetProfit,

    CAST(CumulativeNetProfit / @StartingNAV AS decimal(18, 6)) AS CumulativeNetProfitPct,

    CAST(100.0 * CumulativeNetProfit / @StartingNAV AS decimal(18, 2)) AS CumulativeNetProfitPercent,

    CAST(1 + CumulativeNetProfit / @StartingNAV AS decimal(18, 6)) AS CumulativeNAV

FROM Cumulative
ORDER BY TradeYear;
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
CREATE PROCEDURE [dbo].[usp_signal_query_last]
(
    @StrategyName varchar(100),
    @StudyColId int
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Q AS
    (
        SELECT
             S.[Id]
            ,S.[StrategyConfigId]
            ,S.[Tag]
            ,S.[Status]
            ,S.[Side]
            ,S.[StartTime]
            ,S.[StopTime]
            ,S.[StartPrice]
            ,S.[StopPrice]
            ,S.[Canceled]
            ,S.[LastUpdated]
            ,SConfig.[StrategyName]
            ,SConfig.[StudyColId]
            ,ROW_NUMBER() OVER
            (
                PARTITION BY
                    CASE WHEN @StrategyName = '' THEN SConfig.[StrategyName] ELSE '__ALL_STRATEGIES__' END,
                    CASE WHEN @StudyColId = -1 THEN CONVERT(varchar(50), SConfig.[StudyColId]) ELSE '__ALL_STUDYCOLS__' END
                ORDER BY S.[StartTime] DESC, S.[Id] DESC
            ) AS RN
        FROM [Signal] AS S
        INNER JOIN [StrategyConfig] AS SConfig
            ON S.[StrategyConfigId] = SConfig.[Id]
        WHERE
            (@StrategyName = '' OR SConfig.[StrategyName] = @StrategyName)
            AND
            (@StudyColId = -1 OR SConfig.[StudyColId] = @StudyColId)
    )
    SELECT
         [Id]
        ,[StrategyConfigId]
        ,[Tag]
        ,[Status]
        ,[Side]
        ,[StartTime]
        ,[StopTime]
        ,[StartPrice]
        ,[StopPrice]
        ,[Canceled]
        ,[LastUpdated]
        ,[StrategyName]
        ,[StudyColId]
    FROM Q
    WHERE RN = 1
    ORDER BY [StrategyName], [StudyColId], [StartTime] DESC;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_signal_select]
(
	@Id int
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [Id]
          ,[StrategyConfigId]
          ,[Tag]
          ,[Status]
          ,[Side]
          ,[StartTime]
          ,[StopTime]
          ,[StartPrice]
          ,[StopPrice]
          ,[Canceled]
          ,[LastUpdated]
        FROM [Signal] 
        WHERE (@Id = -1 OR @Id = [Id])
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_signal_upsert]
(
    @IdNew int OUTPUT,
    @Id int,
    @StrategyConfigId int,
    @Tag varchar(50),
    @Status varchar(10),
    @Side decimal(18,4),
    @StartTime int,
    @StopTime int,
    @Canceled bit,
    @StartPrice decimal(18,4),
    @StopPrice decimal(18,4),
    @LastUpdated int
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @LastUpdated = -1 
    BEGIN
     SET @LastUpdated = DATEDIFF(SECOND, '19700101', GETUTCDATE());
    END

    -- Check if Id exists
    IF @Id = -1 OR NOT EXISTS (SELECT 1 FROM [Signal] WHERE [Id] = @Id)
    BEGIN
        -- Does not exist, add new
        INSERT INTO [Signal] (
              [StrategyConfigId]
            , [Tag]
            , [Status]
            , [Side]
            , [StartTime]
            , [StopTime]
            , [StartPrice]
            , [StopPrice]
            , [Canceled]
            , [LastUpdated]
        )
        VALUES (
              @StrategyConfigId
            , @Tag
            , @Status
            , @Side
            , @StartTime
            , @StopTime
            , @StartPrice
            , @StopPrice
            , @Canceled
            , @LastUpdated
        );
        SET @IdNew = SCOPE_IDENTITY();  
    END
    ELSE
    BEGIN
        -- Exists, update
        UPDATE [Signal]
        SET
              [StrategyConfigId] = @StrategyConfigId
            , [Tag] = @Tag
            , [Status] = @Status
            , [Side] = @Side
            , [StartTime] = @StartTime
            , [StopTime] = @StopTime
            , [StartPrice] = @StartPrice
            , [StopPrice] = @StopPrice
            , [Canceled] = @Canceled
            , [LastUpdated] = @LastUpdated
        WHERE [Id] = @Id;
        SET @IdNew = @Id;
    END
    -- set @IdNew to a number 
    RETURN @IdNew;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_statuscode_select]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT [Code]
		  ,[Status]
		  ,[Desc]
        FROM [dbo].[OrderStatusCode]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_strategy_config_delete]
(
	@Id int
)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DELETE [StrategyConfig]  WHERE [Id] = @Id;	
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_strategy_config_query] 
	@StudyColId int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT [Id]
		  ,[StrategyName]
		  ,[StudyColId]
		FROM [dbo].[StrategyConfig]
		WHERE [StudyColId] = @StudyColId;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_strategy_config_select] 
	@Id int = -1
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	SELECT [Id]
		  ,[StrategyName]
		  ,[StudyColId]
		FROM [dbo].[StrategyConfig]
		WHERE @Id = -1 OR [Id] = @Id;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_strategy_config_upsert]
(
	@IdNew int OUTPUT,
	@Id int,
	@StrategyName varchar(100),
	@StudyColId int
)

AS
BEGIN
	SET NOCOUNT ON;

	-- Check if strategy exists
	SELECT	 @IdNew = [Id] FROM [StrategyConfig] WHERE @StrategyName = [StrategyName] AND @StudyColId = [StudyColId];
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- Does not exists, add new 
		INSERT INTO [StrategyConfig] ([StrategyName] ,[StudyColId])
		VALUES (@StrategyName ,@StudyColId)
		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		-- Exists, just update values
		UPDATE [StrategyConfig] SET 
				 [StrategyName] = @StrategyName
				,[StudyColId] = @StudyColId
			WHERE Id = @IdNew
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
	@ColType varchar(100)
)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT [Id]
		  ,[TickerId]
		  ,[IntervalId]
		  ,[ColType]
		  ,[OrderSeq]
		  ,[ColParams]
		  ,[Enabled]
		FROM [StudyCol]
		WHERE	@TickerId = [TickerId] 
			AND @IntervalId = [IntervalId]
			AND @ColType = [ColType]

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
		  ,[ColType]
		  ,[OrderSeq]
		  ,[ColParams]
		  ,[Enabled]
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
	@ColType varchar(100),
	@OrderSeq int,
	@ColParams varchar(max),
	@Enabled bit
)

AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @CurId int;

	SELECT @IdNew = Id FROM [StudyCol] WHERE [Id] = @Id
	IF (@@ROWCOUNT = 0)
	BEGIN
		-- get max sequence
		IF (@OrderSeq <= 0) 
		BEGIN
			DECLARE @maxSeq int;
			SELECT @maxSeq = max(OrderSeq) FROM [StudyCol];
			IF @@ROWCOUNT > 0 SET @OrderSeq = @maxSeq + 1;
			ELSE SET @OrderSeq = 1;
		END

		-- Does not exists, add new 
		INSERT INTO [dbo].[StudyCol]
				   ([TickerId]
				   ,[IntervalId]
				   ,[ColType]
				   ,[OrderSeq]
				   ,[ColParams]
				   ,[Enabled])
			 VALUES
				   (@TickerId,
				   @IntervalId,
				   @ColType,
				   @OrderSeq,
				   @ColParams,
				   @Enabled);

		SET @IdNew = SCOPE_IDENTITY();  

	END ELSE BEGIN
		UPDATE [dbo].[StudyCol]
			SET [TickerId] = @TickerId
				,[IntervalId] = @IntervalId
				,[ColType] = @ColType
				,[OrderSeq] = @OrderSeq
				,[ColParams] = @ColParams
				,[Enabled] = @Enabled
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
		  ,[PeriodMultiplier]
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
		  ,[PeriodMultiplier]
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
	@Exchange varchar(100),
	@PeriodMultiplier int
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
			, [LocalSymbol]
			, [Name]
			, [SecurityType]
			, [Currency]
			, [ExpiryDate]
			, [MinTick]
			, [ContractSize]
			, [Exchange]
		    ,[PeriodMultiplier]
		)
		VALUES (
			  @Symbol
			, @LocalSymbol
			, @Name
			, @SecurityType
			, @Currency
			, @ExpiryDate
			, @MinTick
			, @ContractSize
			, @Exchange
			, @PeriodMultiplier
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
			, [PeriodMultiplier] = @PeriodMultiplier
		WHERE Id = @IdNew;
	END

	RETURN @IdNew;
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trigAutoTradeSignalOnChange]
   ON  [dbo].[AutoTradeSignal]
   AFTER INSERT, UPDATE
AS 
BEGIN
	SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted)
        RETURN;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @EventType varchar(50) = 'AUTOTRADESIGNAL';

    -- Store all per-BotId changes here so we can use them in multiple statements
    DECLARE @Changes TABLE
    (
        EventType  varchar(50)   NOT NULL,
        EventId    varchar(100)  NOT NULL,
        [Message]  varchar(256)  NOT NULL,
        [TimeStamp] datetime2(3) NOT NULL,
        PRIMARY KEY (EventType, EventId)
    );

    /*
      INSERT actions (rows in inserted not matched in deleted by BotId)
    */
    INSERT INTO @Changes (EventType, EventId, [Message], [TimeStamp])
    SELECT DISTINCT
        @EventType AS EventType,
        CAST(i.AutoTradeId AS varchar(100)) AS EventId,
        'Inserted Automatically from [AutoTradeSignal] Trigger' AS [Message],
        @Now
        FROM inserted i
        LEFT JOIN deleted d ON d.Id = i.Id
        WHERE d.Id IS NULL AND i.AutoTradeId IS NOT NULL;

    /*
      UPDATE actions (rows present in both inserted and deleted by BotId)
      If same EventId already inserted above, keep INSERT message (or overwrite if you prefer).
    */
    INSERT INTO @Changes (EventType, EventId, [Message], [TimeStamp])
    SELECT DISTINCT
        @EventType AS EventType,
        CAST(i.AutoTradeId AS varchar(100)) AS EventId,
        'Updated Automatically from [AutoTradeSignal] Trigger' AS [Message],
        @Now
    FROM inserted i
    INNER JOIN deleted d ON d.Id = i.Id
    WHERE i.AutoTradeId IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM @Changes c
          WHERE c.EventType = @EventType
            AND c.EventId = CAST(i.AutoTradeId AS varchar(100))
      );

    -- 1) Update existing Event rows
    UPDATE E
       SET E.[TimeStamp] = C.[TimeStamp],
           E.[Message]   = C.[Message]
    FROM dbo.[Event] E
    INNER JOIN @Changes C
            ON E.[EventType] = C.[EventType]
           AND E.[EventId]   = C.[EventId];

    -- 2) Insert missing Event rows
    INSERT INTO dbo.[Event] ([EventType], [EventId], [Message], [TimeStamp])
    SELECT C.[EventType], C.[EventId], C.[Message], C.[TimeStamp]
    FROM @Changes C
    LEFT JOIN dbo.[Event] E
           ON E.[EventType] = C.[EventType]
          AND E.[EventId]   = C.[EventId]
    WHERE E.[EventId] IS NULL;


END
GO
ALTER TABLE [dbo].[AutoTradeSignal] ENABLE TRIGGER [trigAutoTradeSignalOnChange]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dbo].[trigAutoTradeSignalOrderOnChange]
ON [dbo].[AutoTradeSignalOrder]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted)
        RETURN;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @EventType varchar(50) = 'AUTOTRADESIGNALORDER';

    -- Store all per-BotId changes here so we can use them in multiple statements
    DECLARE @Changes TABLE
    (
        EventType  varchar(50)   NOT NULL,
        EventId    varchar(100)  NOT NULL,
        [Message]  varchar(256)  NOT NULL,
        [TimeStamp] datetime2(3) NOT NULL,
        PRIMARY KEY (EventType, EventId)
    );

    /*
      INSERT actions (rows in inserted not matched in deleted by BotId)
    */
    INSERT INTO @Changes (EventType, EventId, [Message], [TimeStamp])
    SELECT DISTINCT
        @EventType AS EventType,
        CAST(ATS.AutoTradeId AS varchar(100)) AS EventId,
        'Inserted Automatically from AutoTradeSignalOrder Trigger' AS [Message],
        @Now
        FROM inserted i
        LEFT JOIN deleted d ON d.Id = i.Id
        INNER JOIN AutoTradeSignal AS ATS ON i.AutoTradeSignalId = ATS.Id
        WHERE d.Id IS NULL AND ATS.AutoTradeId IS NOT NULL;

    /*
      UPDATE actions (rows present in both inserted and deleted by BotId)
      If same EventId already inserted above, keep INSERT message (or overwrite if you prefer).
    */
    INSERT INTO @Changes (EventType, EventId, [Message], [TimeStamp])
    SELECT DISTINCT
        @EventType AS EventType,
        CAST(ATS.AutoTradeId AS varchar(100)) AS EventId,
        'Updated Automatically from AutoTradeSignalOrder Trigger' AS [Message],
        @Now
    FROM inserted i
    INNER JOIN deleted d ON d.Id = i.Id
    INNER JOIN AutoTradeSignal AS ATS ON ATS.Id = i.AutoTradeSignalId  
    WHERE ATS.AutoTradeId IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM @Changes c
          WHERE c.EventType = @EventType
            AND c.EventId = CAST(ATS.AutoTradeId AS varchar(100))
      );

    -- 1) Update existing Event rows
    UPDATE E
       SET E.[TimeStamp] = C.[TimeStamp],
           E.[Message]   = C.[Message]
    FROM dbo.[Event] E
    INNER JOIN @Changes C
            ON E.[EventType] = C.[EventType]
           AND E.[EventId]   = C.[EventId];

    -- 2) Insert missing Event rows
    INSERT INTO dbo.[Event] ([EventType], [EventId], [Message], [TimeStamp])
    SELECT C.[EventType], C.[EventId], C.[Message], C.[TimeStamp]
    FROM @Changes C
    LEFT JOIN dbo.[Event] E
           ON E.[EventType] = C.[EventType]
          AND E.[EventId]   = C.[EventId]
    WHERE E.[EventId] IS NULL;
END
GO
ALTER TABLE [dbo].[AutoTradeSignalOrder] ENABLE TRIGGER [trigAutoTradeSignalOrderOnChange]
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
CREATE TRIGGER [dbo].[trigSignalOnChange]
ON [dbo].[Signal]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME = GETDATE();
    DECLARE @EventType varchar(50) = 'SIGNAL';

    -- Update existing Event rows
    UPDATE e
    SET
        e.[TimeStamp] = @Now,
        e.[Message] = 'Updated for StrategyConfigId: ' + CONVERT(varchar(100), i.StrategyConfigId)
    FROM dbo.[Event] e
    INNER JOIN inserted i
        ON e.EventType = @EventType
       AND e.EventId = CONVERT(varchar(100), i.StrategyConfigId);

    -- Insert missing Event rows
    INSERT INTO dbo.[Event] (EventType, EventId, [Message], [TimeStamp])
    SELECT
        @EventType,
        CONVERT(varchar(100), i.StrategyConfigId),
        'Inserted automatically for StrategyConfigId: ' + CONVERT(varchar(100), i.StrategyConfigId),
        @Now
    FROM inserted i
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.[Event] e
        WHERE e.EventType = @EventType
          AND e.EventId = CONVERT(varchar(100), i.StrategyConfigId)
    );
END
GO
ALTER TABLE [dbo].[Signal] ENABLE TRIGGER [trigSignalOnChange]
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
INSERT [dbo].[Ticker] ([Id], [Symbol], [LocalSymbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange], [PeriodMultiplier]) VALUES (1, N'NQ', N'NQ', N'E-mini NASDAQ 100', N'FUT', N'USD', 20260618, CAST(0.250000 AS Decimal(18, 6)), 20, N'CME', 92)
GO
INSERT [dbo].[Ticker] ([Id], [Symbol], [LocalSymbol], [Name], [SecurityType], [Currency], [ExpiryDate], [MinTick], [ContractSize], [Exchange], [PeriodMultiplier]) VALUES (2, N'MNQ', N'MNQ', N'Micro E-Mini Nasdaq-100 Index', N'FUT', N'USD', 20260618, CAST(0.250000 AS Decimal(18, 6)), 2, N'CME', 92)
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
SET IDENTITY_INSERT [dbo].[StudyCol] ON 
GO
INSERT [dbo].[StudyCol] ([Id], [TickerId], [IntervalId], [ColType], [OrderSeq], [ColParams], [Enabled]) VALUES (1, 1, N'15', N'', 1, N'{"C21": 1932, "StdDev1": 1, "StdDev2": 2, "StdDev3": 0.6}', 1)
GO
INSERT [dbo].[StudyCol] ([Id], [TickerId], [IntervalId], [ColType], [OrderSeq], [ColParams], [Enabled]) VALUES (2, 1, N'5', N'L20', 2, N'{"MinPeriod": 276, "MaxPeriod": 5796, "PeriodInc": 276, "StdDev1": 1, "StdDev2": 2}', 1)
GO
SET IDENTITY_INSERT [dbo].[StudyCol] OFF
GO
SET IDENTITY_INSERT [dbo].[StrategyConfig] ON 
GO
INSERT [dbo].[StrategyConfig] ([Id], [StrategyName], [StudyColId]) VALUES (1, N'LC5dot1', 1)
GO
INSERT [dbo].[StrategyConfig] ([Id], [StrategyName], [StudyColId]) VALUES (2, N'S100dot3', 1)
GO
INSERT [dbo].[StrategyConfig] ([Id], [StrategyName], [StudyColId]) VALUES (3, N'LBaseL1', 2)
GO
INSERT [dbo].[StrategyConfig] ([Id], [StrategyName], [StudyColId]) VALUES (4, N'SBaseL1', 2)
GO
SET IDENTITY_INSERT [dbo].[StrategyConfig] OFF
GO
SET IDENTITY_INSERT [dbo].[DataSet] ON 
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (1, 1, N'1')
GO
INSERT [dbo].[DataSet] ([Id], [TickerId], [IntervalId]) VALUES (2, 2, N'1')
GO
SET IDENTITY_INSERT [dbo].[DataSet] OFF
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'CANCELED', N'Canceled')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'FAILED', N'Failed to process order')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'FILLED', N'Fully Filled')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'FILLED_MANUALLY', N'Manually Filled')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'FILLED_PARTIALLY', N'Partially Filled')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'FILLED_PARTIALLY_FIN', N'Filled partially and completed (partial amount canceled or expired)')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'NEW', N'New Order')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'NONE', N'Not initialized yet')
GO
INSERT [dbo].[OrderStatus] ([Status], [Description]) VALUES (N'PROCESSING', N'Order is in progress')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-30, N'IGNORED_DUPLICATE_ORDER', N'Order is duplicate based on Ref and Tag')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-29, N'IGNORED_DATA_INVALID', N'Invalid Data')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-28, N'IGNORED_COMM_ERR', N'Communication error')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-27, N'IGNORED_TICKER_NOT_ALLOWED', N'Not permitted to trade ticker')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-26, N'IGNORED_DATABASE_ERROR', N'Ignored: database error')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-25, N'IGNORED_ORDER_ID_ERROR', N'Ignored: order id error')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-24, N'IGNORED_BROKER_NOT_CONFIG', N'Ignored: broker not configured')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-23, N'IGNORED_TICKER_NOT_CONFIG', N'Ignored: ticker not configured')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-22, N'IGNORED_ACCOUNT_NOT_CONFIG', N'Ignored: account not configured')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-21, N'IGNORED', N'Ignored: not processed')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-20, N'FAIL_OTHER', N'Failed: Unknown reason')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-15, N'IGNORED_SHORT_POS_LIMIT', N'Reached Short Position Limit')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-14, N'IGNORED_LONG_POS_LIMIT', N'Reached Long Position Limit')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-13, N'FAIL_AUTHORIZATION', N'Failed: Unauthorized')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-12, N'FAIL_NO_OPEN_POS', N'Failed: No Open Positions to Close')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-11, N'FAIL_CLOSED_WHILE_SUBMISSION', N'Failed: Close operation failed due to incompleted open submission')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-10, N'FAIL_CLOSED_BEFORE_SUBMISSION', N'Failed: Close operation failed due to unsubmitted open order')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-9, N'FAIL_CLOSING_INCOMPLETE_ORDER', N'Fail: Closing incompleted open order')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-8, N'FAIL_DUPLICATE_ID', N'Duplicate Order Id')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-7, N'FAIL_CONNECTION', N'Connection to broker error')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-6, N'FAIL_TIMEOUT', N'Failed: operation timed out')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-5, N'FAIL_REJECTED', N'Failed: order rejected')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-4, N'FAIL_CANCELED', N'Failed: order canceled')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-3, N'FAIL_CANCELED_NOT_SUBMITTED', N'Failed: Canceled before submission')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-2, N'FAIL_EXPIRED', N'Failed: order expired')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (-1, N'UNDEFINED', N'Not defined yet')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (0, N'NEW', N'New order created (not yet processed)')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (1, N'INPROGRESS', N'In progress')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (2, N'INPROGRESS_TIMEOUT', N'In progress: timeout pending/approaching')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (3, N'INPROGRESS_SUBMITTED', N'In progress: submitted to broker')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (4, N'INPROGRESS_INACTIVE', N'indicates that the order was received by the system but is no longer active because it was rejected or canceled.')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (5, N'INPROGRESS_CANCEL', N'Cancelation is in-progress')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (9, N'INPROGRESS_OTHER', N'In progress: general')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (10, N'SUCCESS', N'Success (completed without fill-specific outcome)')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (11, N'PARTIALLY_FILLED', N'Partially filled')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (12, N'PARTIALLY_FILLED_FINAL', N'Partially filled (final state)')
GO
INSERT [dbo].[OrderStatusCode] ([Code], [Status], [Desc]) VALUES (13, N'FILLED', N'Filled')
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
INSERT [dbo].[SignalStatus] ([Status], [Description]) VALUES (N'START', N'Start Signal')
GO
INSERT [dbo].[SignalStatus] ([Status], [Description]) VALUES (N'STOP', N'Signal Completed')
GO
