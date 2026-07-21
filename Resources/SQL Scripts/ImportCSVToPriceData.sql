SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DataImport](
	[Date] [varchar](100) NULL,
	[Time] [varchar](100) NULL,
	[BarStr] [varchar](50) NULL,
	[BarNum] [int] NULL,
	[TickRange] [int] NULL,
	[Open] [decimal](18, 4) NULL,
	[High] [decimal](18, 4) NULL,
	[Low] [decimal](18, 4) NULL,
	[Close] [decimal](18, 4) NULL,
	[Vol] [int] NULL
) ON [PRIMARY]
GO


---- 2- Import CSV to DataImport table
--------------------------------------------
BULK INSERT DataImport
FROM 'D:\Temp\NQ #F Chart 2026-05-05-17-46.csv'
WITH (
    FIRSTROW = 2,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    TABLOCK
);


--Finally insert data into PriceData
-----------------------------------------

DECLARE @SourceTimeZone varchar(100) = 'Eastern Standard Time';
--DECLARE @SourceTimeZone varchar(100) = 'Arabian Standard Time';
DECLARE @DataSetId int = 5;

DELETE PriceData Where DataSetId = @DataSetId

INSERT INTO [PriceData]
	(
		 [DataSetId]
		,[RawTime]
		,[PriceDate]
		,[Open]
		,[High]
		,[Low]
		,[Close]
		,[Volume]
	)
	SELECT 
			 @DataSetId AS [DataSetId]
			,dbo.[fn_ConvertLocalDateTimeToUTCRawTime] (CONVERT(DATETIME, ([Date] + ' ' + [Time]), 101), @SourceTimeZone) as RawTime
			,dbo.fn_ConvertUTCRawTimeToLocalDateTime (dbo.[fn_ConvertLocalDateTimeToUTCRawTime] (CONVERT(DATETIME, ([Date] + ' ' + [Time]), 101), @SourceTimeZone), 'UTC') AS PriceDate
			,[Open]
			,[High]
			,[Low]
			,[Close]
			,[Vol] AS [Volume]
		FROM [dbo].[DataImport] ORDER BY BarNum ASC


-- Delete temp Table
Drop Table [DataImport]
