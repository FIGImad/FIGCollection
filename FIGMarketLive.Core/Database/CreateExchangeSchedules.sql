SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource='FIGMarketLive.ExchangeSchedules',@LockMode='Exclusive',@LockOwner='Transaction';
IF @Lock<0 THROW 51076,'Cannot lock exchange schedule schema.',1;
DECLARE @Created bit=0;
IF OBJECT_ID('dbo.ExchangePriceSchedule','U') IS NULL
BEGIN
    SET @Created=1;
    CREATE TABLE dbo.ExchangePriceSchedule(
        Exchange nvarchar(50) NOT NULL PRIMARY KEY,
        Enabled bit NOT NULL,
        TimeZoneId nvarchar(128) NOT NULL,
        WeeklyBreakCloseDay varchar(9) NOT NULL,
        WeeklyBreakCloseTime char(5) NOT NULL,
        WeeklyBreakOpenDay varchar(9) NOT NULL,
        WeeklyBreakOpenTime char(5) NOT NULL,
        DailyBreakCloseTime char(5) NOT NULL,
        DailyBreakOpenTime char(5) NOT NULL
    );
END;
-- Initial CME template copied from FIGPriceSyncSvc/price-sync-settings.json.
IF @Created=1 AND NOT EXISTS(SELECT 1 FROM dbo.ExchangePriceSchedule WHERE Exchange='CME')
    INSERT dbo.ExchangePriceSchedule VALUES('CME',1,'Central Standard Time','Friday','16:00','Sunday','17:00','16:00','17:00');
-- Do not guess trading hours for other exchanges. They must be configured and enabled.
INSERT dbo.ExchangePriceSchedule
    SELECT DISTINCT UPPER(LTRIM(RTRIM(t.Exchange))),0,'UTC','Friday','16:00','Sunday','17:00','16:00','17:00'
    FROM dbo.Ticker t WHERE LTRIM(RTRIM(t.Exchange))<>''
    AND NOT EXISTS(SELECT 1 FROM dbo.ExchangePriceSchedule s WHERE s.Exchange=UPPER(LTRIM(RTRIM(t.Exchange))));
COMMIT;
