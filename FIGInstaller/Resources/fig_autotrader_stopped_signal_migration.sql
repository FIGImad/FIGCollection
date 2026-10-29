-- Run against FIGAutoTrader before deploying the updated AutoTrader service.
-- The existing usp_autotrade_signal_upsert suspect-transition rule remains in use.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM dbo.OrderStatusCode WITH (UPDLOCK, HOLDLOCK)
           WHERE Code = -16 AND Status <> 'FAIL_AUTOTRADE_STOPPED')
    THROW 51000, 'Order status code -16 is already assigned to another meaning.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.OrderStatusCode WITH (UPDLOCK, HOLDLOCK) WHERE Code = -16)
    INSERT dbo.OrderStatusCode (Code, Status, [Desc])
    VALUES (-16, 'FAIL_AUTOTRADE_STOPPED', 'Failed: Autotrade instance was stopped before completing the signal');
COMMIT;
