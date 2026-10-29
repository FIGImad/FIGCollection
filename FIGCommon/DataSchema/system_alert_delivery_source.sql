-- Run against the AutoTrader database before starting the updated monitor.
CREATE OR ALTER PROCEDURE dbo.usp_systemalert_read_pending @MaxRec int
AS
BEGIN
    SET NOCOUNT ON;
    IF @MaxRec <= 0 RETURN;
    SELECT TOP (@MaxRec) Id, RawTime, MSec, Source, Level, Message, DeliveryTime
    FROM dbo.SystemAlert
    WHERE DeliveryTime = 0
    ORDER BY Id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_systemalert_acknowledge @Id int
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SystemAlert
    SET DeliveryTime = DATEDIFF(SECOND, '19700101', GETUTCDATE())
    WHERE Id = @Id AND DeliveryTime = 0;
    SELECT @@ROWCOUNT;
END;
GO
