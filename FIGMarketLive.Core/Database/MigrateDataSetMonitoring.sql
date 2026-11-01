SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource='FIGMarketLive.DataSetSchema',@LockMode='Exclusive',@LockOwner='Transaction';
IF @Lock<0 THROW 51070,'Cannot lock DataSet schema.',1;
IF OBJECT_ID('dbo.DataSet','U') IS NULL OR OBJECT_ID('dbo.PriceData','U') IS NULL
    THROW 51071,'Configure the base database schema first.',1;
IF COL_LENGTH('dbo.DataSet','Monitor') IS NULL
    ALTER TABLE dbo.DataSet ADD Monitor bit NOT NULL CONSTRAINT DF_DataSet_Monitor DEFAULT(0) WITH VALUES;
IF COL_LENGTH('dbo.PriceData','PriceDate') IS NOT NULL
BEGIN
    -- Do not silently break legacy/shared procedures or views. Audit and migrate those explicitly.
    IF EXISTS(SELECT 1 FROM sys.sql_modules WHERE definition LIKE '%PriceDate%' AND definition LIKE '%PriceData%')
        THROW 51073,'Legacy SQL modules reference PriceDate. Audit and update those modules before this migration.',1;
    DECLARE @Default sysname;
    SELECT @Default=d.name FROM sys.default_constraints d JOIN sys.columns c
        ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
        WHERE c.object_id=OBJECT_ID('dbo.PriceData') AND c.name='PriceDate';
    IF @Default IS NOT NULL
    BEGIN
        DECLARE @DropDefault nvarchar(max)='ALTER TABLE dbo.PriceData DROP CONSTRAINT '+QUOTENAME(@Default);
        EXEC sys.sp_executesql @DropDefault;
    END;
    ALTER TABLE dbo.PriceData DROP COLUMN PriceDate;
END;
COMMIT;
