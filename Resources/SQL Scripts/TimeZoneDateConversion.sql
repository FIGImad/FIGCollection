-- DATE conversion to UT
DECLARE @dateInput datetime;
DECLARE @dateInputGMT datetime;

SET @dateInput = '2024-04-10 9:00';
SET @dateInputGMT = DATEADD(hour,DATEDIFF (hour, GETDATE(), GETUTCDATE()),@dateInput);

DECLARE @rawTimeInput int;
SET @rawTimeInput = datediff(S, '1970-01-01', @dateInputGMT);
print @rawTimeInput

SELECT DATEDIFF (hour, GETDATE(), GETUTCDATE()) AS [TIMEZONE]
SELECT CONVERT(datetime, SWITCHOFFSET(GETUTCDATE(), DATEPART(TZOFFSET, GETUTCDATE() AT TIME ZONE 'Arabian Standard Time'))) AS CreationTime



-- Get RawTime from Dubai to UTC rawtime
DECLARE @DubaiDate datetime, @UTCDate datetime;
set @DubaiDate = CONVERT(DATETIME, '2024-09-28 22:55:00.000', 121)
set @UTCDate = CONVERT(datetime, SWITCHOFFSET(@DubaiDate , -DATEPART(TZOFFSET, @DubaiDate AT TIME ZONE 'Arabian Standard Time'))) 
print 'DubaiDate: ' + cast (@DubaiDate  as varchar(100))
print 'UTCDate: ' + cast (@UTCDate  as varchar(100))
print 'UTCRawTime: ' + cast(DATEDIFF(SECOND, '1970-01-01 00:00:00', @UTCDate) as varchar(100))
