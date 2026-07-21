-- Extracting data from remote database

SET NOCOUNT ON; 

DECLARE @lastRawTime int;
DECLARE @DataSetId int;
DECLARE @TickerId int;
DECLARE @TimeZoneAdj int;
SET @lastRawTime = 0;
SET @DataSetId = 6;
SET @TickerId = 1;
SET @TimeZoneAdj = -1;


SELECT MAX(TF.[Id]) AS [Id], @DataSetId AS [DataSetId], TF.[RawTime], TF.[PriceDate]
	,MAX(TF.[Open]) AS [Open], MAX(TF.[High]) AS [High], MAX(TF.[Low]) AS [Low], MAX(TF.[Close]) AS [Close], MAX(TF.[Volume]) AS [Volume]
FROM (
	SELECT TB.[Id]
		  ,datediff(S, '1970-01-01', DATEADD(hour, @TimeZoneAdj, TB.[PriceDate])) AS [RawTime]
		  ,TB.[PriceDate]
		  ,TB.OrigTime
		  ,TB.[Open]
		  ,TB.[High]
		  ,TB.[Low]
		  ,TB.[Close]
		  ,TB.[Volume]
		  FROM
	(
	SELECT [ticker_price_id] AS [Id]
		  ,[ticker_price_datet] + ' ' + [ticker_price_time] as OrigTime
		  ,DATEADD(hour,DATEDIFF (hour, GETDATE(), GETUTCDATE()),Convert(datetime, convert(varchar(100), convert(datetime, [ticker_price_datet], 103), 106) + ' ' + [ticker_price_time], 113)) AS [PriceDate]
		  ,cast([ticker_open] as Decimal(18,4)) AS [Open]
		  ,cast([ticker_high] as Decimal(18,4)) AS [High]
		  ,cast([ticker_low] as Decimal(18,4)) AS [Low]
		  ,cast([ticker_close] as Decimal(18,4)) AS [Close]
		  ,cast([ticker_Volume] as int) AS [Volume]
	  FROM [IBDATA].[dbo].[One_M_Bar_Table]
	  WHERE [ticker_id] = @TickerId 
	  ) AS TB
  ) AS TF
  WHERE TF.[RawTime] >= @lastRawTime
  GROUP BY TF.[RawTime], TF.[PriceDate]
  ORDER BY [Id] ASC


  --SELECT TOP (1000) [Id]
  --    ,[DataSetId]
  --    ,[RawTime]
  --	,datediff(S, '1970-01-01', DATEADD(hour,DATEDIFF (hour, GETDATE(), GETUTCDATE()), [PriceDate])) AS [RawTime2]
  --    ,[PriceDate]
  --    ,[Open]
  --    ,[High]
  --    ,[Low]
  --    ,[Close]
  --    ,[Volume]
  --FROM [ROOTS_IGS].[dbo].[PriceData] Where RawTime = 1715022420
