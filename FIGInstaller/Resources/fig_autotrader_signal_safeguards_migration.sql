USE [fig_autotrader]
GO

SET XACT_ABORT ON;
GO

-- Do not guess which duplicate record is authoritative. Report duplicates and
-- stop so they can be reconciled before the uniqueness safeguard is installed.
IF EXISTS
(
    SELECT 1
      FROM [dbo].[AutoTradeSignal]
     GROUP BY [AutoTradeId], [SignalId]
    HAVING COUNT_BIG(*) > 1
)
BEGIN
    SELECT [AutoTradeId], [SignalId], COUNT_BIG(*) AS [DuplicateCount]
      FROM [dbo].[AutoTradeSignal]
     GROUP BY [AutoTradeId], [SignalId]
    HAVING COUNT_BIG(*) > 1
     ORDER BY [AutoTradeId], [SignalId];

    THROW 51000, 'Duplicate AutoTradeSignal rows must be reconciled before UX_AutoTradeSignal_AutoTradeId_SignalId can be created.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
      FROM [sys].[indexes]
     WHERE [object_id] = OBJECT_ID(N'[dbo].[AutoTradeSignal]')
       AND [name] = N'UX_AutoTradeSignal_AutoTradeId_SignalId'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_AutoTradeSignal_AutoTradeId_SignalId]
        ON [dbo].[AutoTradeSignal] ([AutoTradeId], [SignalId]);
END
GO

-- OPEN and CLOSE are logical singleton orders for a signal. Stop rather than
-- deleting data when an existing database contains ambiguous duplicates.
IF EXISTS
(
    SELECT 1
      FROM [dbo].[AutoTradeSignalOrder]
     GROUP BY [AutoTradeSignalId], [OrderTag]
    HAVING COUNT_BIG(*) > 1
)
BEGIN
    SELECT [AutoTradeSignalId], [OrderTag], COUNT_BIG(*) AS [DuplicateCount]
      FROM [dbo].[AutoTradeSignalOrder]
     GROUP BY [AutoTradeSignalId], [OrderTag]
    HAVING COUNT_BIG(*) > 1
     ORDER BY [AutoTradeSignalId], [OrderTag];

    THROW 51001, 'Duplicate AutoTradeSignalOrder rows must be reconciled before UX_AutoTradeSignalOrder_Signal_Tag can be created.', 1;
END
GO

IF NOT EXISTS
(
    SELECT 1
      FROM [sys].[indexes]
     WHERE [object_id] = OBJECT_ID(N'[dbo].[AutoTradeSignalOrder]')
       AND [name] = N'UX_AutoTradeSignalOrder_Signal_Tag'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UX_AutoTradeSignalOrder_Signal_Tag]
        ON [dbo].[AutoTradeSignalOrder] ([AutoTradeSignalId], [OrderTag]);
END
GO
