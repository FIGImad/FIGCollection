DECLARE @Date2022 datetime;
SET @Date2022 = '2022-01-01 00:00:00'


SELECT * 
	FROM 
	(
	SELECT
		   ESIG.[RawTime]
		  ,DateDiff(S, ESIG.[PriceDate], ISNULL(SULT.[PriceDate], @Date2022)) AS DiffSeconds
		  ,(ESIG.[Open] - ISNULL(SULT.[Open], 0)) AS OpenDiff
		  ,(ESIG.[High] - ISNULL(SULT.[High], 0)) AS HighDiff
		  ,(ESIG.[Low] - ISNULL(SULT.[Low], 0)) AS LowDiff
		  ,(ESIG.[Close] - ISNULL(SULT.[Close], 0)) AS CloseDiff
		FROM
			(SELECT [Id]
				  ,[DataSetId]
				  ,[RawTime]
				  ,[PriceDate]
				  ,[Open]
				  ,[High]
				  ,[Low]
				  ,[Close]
				  ,[Volume]
			  FROM [ROOTS_IGS].[dbo].[PriceData]
			  Where  DataSetId = 4  -- MNQ #F
			  ) ESIG
			  LEFT JOIN 
			  (SELECT [Id]
				  ,[DataSetId]
				  ,[RawTime]
				  ,[PriceDate]
				  ,[Open]
				  ,[High]
				  ,[Low]
				  ,[Close]
				  ,[Volume]
			  FROM [ROOTS_IGS].[dbo].[PriceData]
			  Where  DataSetId = 4  -- MNQ #F
			  ) SULT ON SULT.RawTime = ESIG.RawTime
		) A
		Where A.DiffSeconds != 0 OR A.OpenDiff != 0 OR A.HighDiff != 0 OR A.LowDiff != 0 OR A.CloseDiff != 0







		-- OR

DECLARE @DataSetId int = 2;
DECLARE @TickerId int = 2;
--DECLARE @TimezoneInfo nvarchar(50) = 'Jordan Standard Time';
DECLARE @TimezoneInfo nvarchar(50) = 'Arabian Standard Time';
--EXEC MASTER.dbo.xp_regread 'HKEY_LOCAL_MACHINE', 
--                           'SYSTEM\CurrentControlSet\Control\TimeZoneInformation', 
--                           'TimeZoneKeyName', 
--                           @TimezoneInfo OUTPUT;

SELECT TF.[Id] AS [Id], @DataSetId AS [DataSetId], TF.[RawTime], TF.[PriceDateJordan], TF.[PriceDateUTC]
	,TF.[Open] AS [Open], TF.[High] AS [High], TF.[Low] AS [Low], TF.[Close] AS [Close], TF.[Volume] AS [Volume]
	INTO COMPTABLE
	FROM (
	SELECT TB.[Id]
		  ,dbo.fn_ConvertLocalDateTimeToUTCRawTime([PriceDateLocal], @TimezoneInfo) AS [RawTime]
		  ,TB.[PriceDateLocal] AS [PriceDateJordan]
		  ,TB.[PriceDateLocal] AT TIME ZONE @TimezoneInfo AT TIME ZONE 'UTC' AS [PriceDateUTC]
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
		  ,Convert(datetime, convert(varchar(100), convert(datetime, [ticker_price_datet], 103), 106) + ' ' + [ticker_price_time], 113) AS [PriceDateLocal]
		  ,cast([ticker_open] as Decimal(18,4)) AS [Open]
		  ,cast([ticker_high] as Decimal(18,4)) AS [High]
		  ,cast([ticker_low] as Decimal(18,4)) AS [Low]
		  ,cast([ticker_close] as Decimal(18,4)) AS [Close]
		  ,cast([ticker_Volume] as int) AS [Volume]
	  FROM RemoteDataServer.[IBDATA].[dbo].[One_M_Bar_Table]
	  WHERE [ticker_id] = @TickerId 
	  ) AS TB
  ) AS TF
  ORDER BY [Id] ASC


-- delete last 10 entries from COMPTABLE, re-insert them, 
--DECLARE @LastId int
--SELECT TOP 20 @LastId = [Id] FROM COMPTABLE ORDER BY [Id] DESC
--DELETE COMPTABLE WHERE [Id] >= @LastId

--INSERT INTO COMPTABLE([Id], [DataSetId], [RawTime], [PriceDateJordan], [PriceDateUTC]
--	,[Open], [High], [Low], [Close], [Volume])

--SELECT TF.[Id] AS [Id], @DataSetId AS [DataSetId], TF.[RawTime], TF.[PriceDateJordan], TF.[PriceDateUTC]
--	,TF.[Open] AS [Open], TF.[High] AS [High], TF.[Low] AS [Low], TF.[Close] AS [Close], TF.[Volume] AS [Volume]
--	FROM (
--	SELECT TB.[Id]
--		  ,dbo.fn_ConvertLocalDateTimeToUTCRawTime([PriceDateLocal], @TimezoneInfo) AS [RawTime]
--		  ,TB.[PriceDateLocal] AS [PriceDateJordan]
--		  ,TB.[PriceDateLocal] AT TIME ZONE @TimezoneInfo AT TIME ZONE 'UTC' AS [PriceDateUTC]
--		  ,TB.OrigTime
--		  ,TB.[Open]
--		  ,TB.[High]
--		  ,TB.[Low]
--		  ,TB.[Close]
--		  ,TB.[Volume]
--		  FROM
--	(
--	SELECT [ticker_price_id] AS [Id]
--		  ,[ticker_price_datet] + ' ' + [ticker_price_time] as OrigTime
--		  ,Convert(datetime, convert(varchar(100), convert(datetime, [ticker_price_datet], 103), 106) + ' ' + [ticker_price_time], 113) AS [PriceDateLocal]
--		  ,cast([ticker_open] as Decimal(18,4)) AS [Open]
--		  ,cast([ticker_high] as Decimal(18,4)) AS [High]
--		  ,cast([ticker_low] as Decimal(18,4)) AS [Low]
--		  ,cast([ticker_close] as Decimal(18,4)) AS [Close]
--		  ,cast([ticker_Volume] as int) AS [Volume]
--	  FROM RemoteDataServer.[IBDATA].[dbo].[One_M_Bar_Table]
--	  WHERE [ticker_id] = @TickerId AND ticker_price_id >= @LastId
--	  ) AS TB
--  ) AS TF
--  ORDER BY [Id] ASC


-- and then compare
SELECT ComTbl.* FROM 
(
  SELECT ISNULL(A.[RawTime], B.[RawTime]) AS RawTime
		,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](ISNULL(A.[RawTime], B.[RawTime]), 'Eastern Standard Time') AS [PriceDateEST]
		,(A.[Open] - B.[Open]) AS OpenDiff
		,(A.[High] - B.[High]) AS HighDiff
		,(A.[Low] - B.[Low]) AS LowDiff
		,(A.[Close] - B.[Close]) AS CloseDiff
		,(A.Volume - B.Volume) AS VolDiff
		,A.Volume
		,A.[Open]
		,[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](A.[RawTime], @TimezoneInfo) AS ESTDate
		,A.PriceDateJordan
		,A.PriceDateUTC
	FROM [COMPTABLE] AS A
	 FULL OUTER JOIN [PriceData] AS B ON A.[DataSetId] = B.[DataSetId] AND A.[RawTime] = B.[RawTime]
	WHERE B.[DataSetId] =@DataSetId AND A.RawTime >= 1707347400
) ComTbl
Where (OpenDiff + HighDiff + LowDiff + CloseDiff) != 0 --AND RawTime >= 1732903200
ORDER BY ComTbl.[RawTime]





	--OR OR OR OR OR OR FROM The same table
	-------------------------------------------
	SELECT 
    ISNULL(A.[RawTime], B.[RawTime]) AS [RawTime],
	[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](ISNULL(A.[RawTime], B.[RawTime]), 'Eastern Standard Time') AS [PriceDateEST],
    A.[Open] AS Open_32,
    B.[Open] AS Open_6,
	(A.[Open] - B.[Open]) AS OpenDiff,
    A.High AS High_32,
    B.High AS High_6,
	(A.High - B.High) AS HighDiff,
    A.Low AS Low_32,
    B.Low AS Low_6,
	(A.Low - B.Low) AS LowDiff,
    A.[Close] AS Close_32,
    B.[Close] AS Close_6,
	(A.[Close] - B.[Close]) AS CloseDiff
FROM 
    (SELECT [RawTime], [DataSetId], [Open], [High], [Low], [Close]
     FROM PriceData WHERE [DataSetId] = 32 AND [RawTime] >= 1727949540) A
FULL OUTER JOIN
    (SELECT [RawTime], [DataSetId], [Open], [High], [Low], [Close]
     FROM PriceData WHERE [DataSetId] = 6 AND [RawTime] >= 1727949540) B
ON A.[RawTime] = B.[RawTime]
WHERE 
    A.[Open] != B.[Open] OR
    A.High != B.High OR
    A.Low  != B.Low OR
    A.[Close] != B.[Close]
    OR A.[RawTime] IS NULL
    OR B.[RawTime] IS NULL