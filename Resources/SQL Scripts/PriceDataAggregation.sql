-- To Aggregate Price Data from 1 min data to 5 min data use this script
DECLARE @SrcInterval int = 1;
DECLARE @TargetInterval int = 5;
DECLARE @SrcDataSetId int = 6;
DECLARE @TargetDataSetId int = 14;

WITH PriceDataWithWindows AS (
		SELECT
			FIRST_VALUE([RawTime]) OVER (PARTITION BY DATEDIFF(MINUTE, 0, [PriceDate]) / @TargetInterval ORDER BY [PriceDate]) AS [RawTime],
			[PriceDate],
			FIRST_VALUE([Open]) OVER (PARTITION BY DATEDIFF(MINUTE, 0, [PriceDate]) / @TargetInterval ORDER BY [PriceDate]) AS [Open],
			[High],
			[Low],
			LAST_VALUE([Close]) OVER (PARTITION BY DATEDIFF(MINUTE, 0, [PriceDate]) / @TargetInterval ORDER BY [PriceDate] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS [Close],
			[Volume],
			DATEDIFF(MINUTE, 0, [PriceDate]) / @TargetInterval AS PriceInterval
		FROM
			PriceData
		WHERE
			DataSetId = @SrcDataSetId
	),
PriceDataFiveMin AS (
	SELECT
		@TargetDataSetId AS [DataSetId],
		MAX(RawTime) AS [RawTime],
		[dbo].[fn_ConvertUTCRawTimeToLocalDateTime](Max([RawTime]), 'UTC') AS [PriceDate],
		MIN([Open]) AS [Open],
		MAX([High]) AS [High],
		MIN([Low]) AS [Low],
		MIN([Close]) AS [Close],
		SUM(Volume) AS [Volume]
	FROM
		PriceDataWithWindows
	GROUP BY
		PriceInterval
)
SELECT * FROM PriceDataFiveMin ORDER BY RawTime 

-- OR Comment the SELECT ABOVE and UNCOMMENT BELOW To compare results
--SELECT A.* FROM 
--(
--	SELECT
--       P5D.[RawTime]
--      ,P5D.[PriceDate]
--      ,P5D.[Open] - P5C.[Open] AS OpenDiff
--      ,P5D.[High] - P5C.[High] AS HighDiff
--      ,P5D.[Low] - P5C.[Low] AS LowDiff
--      ,P5D.[Close] - P5C.[Close] AS CloseDiff
--  FROM [ROOTS_IGS].[dbo].[PriceData] P5D
--	FULL OUTER JOIN PriceDataFiveMin P5C ON P5D.RawTime = P5C.RawTime
--  WHERE P5D.DataSetId = @TargetDataSetId
--) A
--WHERE OpenDiff != 0 OR HighDiff != 0 OR LowDiff != 0 OR CloseDiff != 0
