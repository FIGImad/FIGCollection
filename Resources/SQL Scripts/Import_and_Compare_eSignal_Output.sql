-- 1- Import the eSignal output file (eSignal_Output.csv) into a table called eSignal_Output
SET QUOTED_IDENTIFIER ON
GO
Drop Table [DataImport]
GO 

CREATE TABLE [dbo].[DataImport](
	[OpenDateStr] [varchar](100) NULL,
	[OpenTimeStr] [varchar](100) NULL,
	[OpenPrice] [decimal](18, 4) NULL,
	[CloseDateStr] [varchar](100) NULL,
	[CloseTimeStr] [varchar](100) NULL,
	[ClosePrice] [decimal](18, 4) NULL,
	[Side] [decimal](18, 4) NULL,
	[Qty] [int] NULL,
	[TimeInDeal] [int] NULL,
	[TradePL] [decimal] NULL,
	[TotalPL] [decimal] NULL,
	[NAV] [decimal] NULL,
	[NAVMax] [decimal] NULL,
	[NAVDrawdown] [decimal] NULL,
	[NAVMaxDrawdown] [decimal] NULL,
	[NAVTrade] [decimal] NULL,
	[NAVTradeMax] [decimal] NULL,
	[NAVTradeDrawdown] [decimal] NULL,
	[NAVTradeMaxDrawdown] [decimal] NULL,
) ON [PRIMARY]
GO

BULK INSERT DataImport
FROM 'D:\Temp\L2_V2_24June26.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK
);
ALTER TABLE [DataImport] Add [StartTime] int NULL
GO
ALTER TABLE [DataImport] Add [StopTime] int NULL
GO

--Finally insert data into PriceData
-----------------------------------------

DECLARE @SourceTimeZone varchar(100) = 'Eastern Standard Time';
--DECLARE @SourceTimeZone varchar(100) = 'Arabian Standard Time';

UPDATE di
SET
    StartTime = dbo.fn_ConvertLocalDateTimeToUTCRawTime(x.OpenDateTime, @SourceTimeZone),
    StopTime  = dbo.fn_ConvertLocalDateTimeToUTCRawTime(x.CloseDateTime, @SourceTimeZone)
FROM dbo.DataImport di
CROSS APPLY (
    SELECT
        OpenDate = TRY_CONVERT(date, LTRIM(RTRIM(di.OpenDateStr)), 23),
        CloseDate = TRY_CONVERT(date, LTRIM(RTRIM(di.CloseDateStr)), 23),

        OpenTime = TRY_CONVERT(
            time(0),
            REPLACE(
                REPLACE(LOWER(LTRIM(RTRIM(di.OpenTimeStr))), 'a.m.', 'AM'),
                'p.m.',
                'PM'
            )
        ),

        CloseTime = TRY_CONVERT(
            time(0),
            REPLACE(
                REPLACE(LOWER(LTRIM(RTRIM(di.CloseTimeStr))), 'a.m.', 'AM'),
                'p.m.',
                'PM'
            )
        )
) p
CROSS APPLY (
    SELECT
        OpenDateTime = DATETIME2FROMPARTS(
            YEAR(p.OpenDate), MONTH(p.OpenDate), DAY(p.OpenDate),
            DATEPART(HOUR, p.OpenTime),
            DATEPART(MINUTE, p.OpenTime),
            DATEPART(SECOND, p.OpenTime),
            0,
            0
        ),

        CloseDateTime = DATETIME2FROMPARTS(
            YEAR(p.CloseDate), MONTH(p.CloseDate), DAY(p.CloseDate),
            DATEPART(HOUR, p.CloseTime),
            DATEPART(MINUTE, p.CloseTime),
            DATEPART(SECOND, p.CloseTime),
            0,
            0
        )
) x
WHERE p.OpenDate IS NOT NULL
  AND p.OpenTime IS NOT NULL
  AND p.CloseDate IS NOT NULL
  AND p.CloseTime IS NOT NULL;




	SELECT *
    ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([StartTime], 'Eastern Standard Time') AS [StartTimeEST] 
    ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime]([StopTime], 'Eastern Standard Time') AS [StopTimeEST] 
    FROM DataImport











-- 2- Compare the eSignal output with the output of the strategy
SELECT 
        COALESCE(T.[ESignalStartTimeEST], T.[SolStartTimeEST]) AS StartTimeEST, 
        T.[SolStartTimeEST],
        T.[SolStartTime],
        T.SolStartPrice,
        T.[ESignalStartTimeEST],
        T.ESignalStartPrice,
        T.[SolStopTimeEST],
        T.[SolStopTime],
        T.SolStopPrice,
        T.[ESignalStopTimeEST],
        T.ESignalStopPrice,
        T.SolSide,
        T.ESignalSide

        FROM 
        (

SELECT 
      -- [Id]
      --,[StudyColId]
      --,[Strategy]
      --,[Status]
      [dbo].[fn_ConvertUTCRawTimeToLocalDateTime](A.[StartTime] + 300, 'Arabian Standard Time') AS [SolStartTimeEST]
      ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](D.[StartTime], 'Arabian Standard Time') AS [ESignalStartTimeEST]
      ,A.[StartPrice] as SolStartPrice
      ,D.[OpenPrice] as ESignalStartPrice
      ,A.[StartTime] as SolStartTime
      --,[LiveTime]
      ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](A.[StopTime], 'Arabian Standard Time') AS [SolStopTimeEST]
      ,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](D.[StopTime], 'Arabian Standard Time') AS [ESignalStopTimeEST]
      ,A.[StopTime] as SolStopTime
      --,[LivePrice]
      ,A.[StopPrice] AS SolStopPrice
      ,D.[ClosePrice] as ESignalStopPrice
      --,[StudyHistoryStopId]
      --,[LastUpdated]
      ,A.[Side] AS SolSide
      ,D.[Side] AS ESignalSide
  FROM (SELECT * FROM [FIGAutoTrader].[dbo].[Signal] WHERE StrategyConfigId = 10) AS A
      FULL OUTER  JOIN DataImport AS D ON D.StartTime = (A.StartTime + 300)
    

    ) T
  WHERE COALESCE(T.[ESignalStartTimeEST], T.[SolStartTimeEST]) >= cast('2016-06-17 04:45:00.000' as datetime)
  ORDER BY 1


  --2 Alternative Compare the eSignal output with the output of the strategy, 
SELECT * FROM (
SELECT 
    COALESCE(T.[ESignalStartTimeEST], T.[SolStartTimeEST]) AS StartTimeEST, 

    T.[SolStartTimeEST],
    T.[SolStartTime],
    T.SolStartPrice,

    T.[ESignalStartTimeEST],
    T.ESignalStartPrice,

    T.[SolStopTimeEST],
    T.[SolStopTime],
    T.SolStopPrice,

    T.[ESignalStopTimeEST],
    T.ESignalStopPrice,

    T.SolSide,
    T.ESignalSide,

    CASE 
        WHEN T.SolStopTimeEST IS NULL AND T.ESignalStopTimeEST IS NULL THEN 0
        WHEN T.SolStopTimeEST IS NULL OR T.ESignalStopTimeEST IS NULL THEN 1
        WHEN T.SolStopTimeEST <> T.ESignalStopTimeEST THEN 1
        ELSE 0
    END AS IsStopTimeDifferent,

    CASE 
        WHEN T.SolSide IS NULL AND T.ESignalSide IS NULL THEN 0
        WHEN T.SolSide IS NULL OR T.ESignalSide IS NULL THEN 1
        WHEN T.SolSide <> T.ESignalSide THEN 1
        ELSE 0
    END AS IsSideDifferent,

    CASE 
        WHEN 
            (
                T.SolStopTimeEST IS NULL AND T.ESignalStopTimeEST IS NOT NULL
            )
            OR
            (
                T.SolStopTimeEST IS NOT NULL AND T.ESignalStopTimeEST IS NULL
            )
            OR
            (
                T.SolStopTimeEST <> T.ESignalStopTimeEST
            )
            OR
            (
                T.SolSide IS NULL AND T.ESignalSide IS NOT NULL
            )
            OR
            (
                T.SolSide IS NOT NULL AND T.ESignalSide IS NULL
            )
            OR
            (
                T.SolSide <> T.ESignalSide
            )
        THEN 1
        ELSE 0
    END AS HasDifference

FROM 
(
    SELECT 
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime](A.[StartTime] + 300, 'Eastern Standard Time') AS [SolStartTimeEST],
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime](D.[StartTime], 'Eastern Standard Time') AS [ESignalStartTimeEST],

        A.[StartPrice] AS SolStartPrice,
        D.[OpenPrice] AS ESignalStartPrice,

        A.[StartTime] AS SolStartTime,

        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime](A.[StopTime]+ 300, 'Eastern Standard Time') AS [SolStopTimeEST],
        [dbo].[fn_ConvertUTCRawTimeToLocalDateTime](D.[StopTime], 'Eastern Standard Time') AS [ESignalStopTimeEST],

        A.[StopTime] AS SolStopTime,

        A.[StopPrice] AS SolStopPrice,
        D.[ClosePrice] AS ESignalStopPrice,

        A.[Side] AS SolSide,
        D.[Side] AS ESignalSide
    FROM 
        (
            SELECT * 
            FROM [FIGAutoTrader].[dbo].[Signal] 
            WHERE StrategyConfigId = 10
        ) AS A
    FULL OUTER JOIN DataImport AS D 
        ON D.StartTime = A.StartTime + 300
) T
WHERE COALESCE(T.[ESignalStartTimeEST], T.[SolStartTimeEST]) >= CAST('2016-06-17 04:45:00.000' AS datetime) ) Final
WHERE HasDifference = 1
ORDER BY 1;