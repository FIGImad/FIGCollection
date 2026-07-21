-- 0- Stop PriceSync and Signal Services

-- 1- Determine the RawTime that to cover couple of hours before the switch
DECLARE @SwitchRawTime int = 1773352800; -- Example RawTime for the switch  (2026-03-12 18:00:00.000 EST)
DECLARE @KeepRawTime int = @SwitchRawTime - 7200; -- Keep two hours before the switch, you can adjust this value as needed (7200 seconds = 2 hours)

-- 2- Make a copy of the current prices table starting with @@KeepRawTime
SELECT * INTO TPriceData FROM PriceData Where RawTime >=@KeepRawTime

-- 3- now we have a copy of the data starting from the switch time, we can delete PriceData from @SwitchRawTime and onwards
DELETE FROM PriceData Where RawTime >= @SwitchRawTime

-- 4- Change Tickers and update the NQ and MNQ contract expiry dates

-- 5- Make a comparison, you will find that there few (3 bars) changed because of the switch
SELECT * 
	FROM 
	(
	SELECT
		   ESIG.[RawTime]
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
			  FROM [PriceData]
			  
			  ) ESIG
			  INNER JOIN 
			  (SELECT [Id]
				  ,[DataSetId]
				  ,[RawTime]
				  ,[PriceDate]
				  ,[Open]
				  ,[High]
				  ,[Low]
				  ,[Close]
				  ,[Volume]
			  FROM [TPriceData]
			  ) SULT ON SULT.RawTime = ESIG.RawTime AND SULT.DataSetId = ESIG.DataSetId
		) A
		Where A.OpenDiff != 0 OR A.HighDiff != 0 OR A.LowDiff != 0 OR A.CloseDiff != 0

-- 6- delete all records from TPriceData that are unchanged and keep the 3 records that changed because of the switch
  DECLARE @UpdateRawtime int = @SwitchRawTime - (70 * 60); -- Update 10 minutes before the switch (note there is 60 minutes extra due to break from 5PM to 6PM)


-- 7 - Update the # records that changed because of the switch, you can adjust the values as needed
UPDATE P
SET
    P.[Open]   = T.[Open],
    P.[High]   = T.[High],
    P.[Low]    = T.[Low],
    P.[Close]  = T.[Close],
    P.[Volume] = T.[Volume]
FROM PriceData AS P
INNER JOIN TPriceData AS T
    ON P.RawTime = T.RawTime
   AND P.DataSetId = T.DataSetId;
WHERE P.RawTime >= @UpdateRawtime
