param([string]$Server = 'localhost')
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = Get-Content (Join-Path $root 'FIGCommon/DataAccess/MainRepo.StoppedAutoTrades.cs') -Raw
$selection = [regex]::Match($source, '(?s)SELECT a.Id.*?ORDER BY a.Id;').Value
if (!$selection) { throw 'Production pending-signal query not found' }
$selection = $selection.Replace('dbo.AutoTradeSignalOrder', '#AutoTradeSignalOrder').Replace('dbo.AutoTradeSignal', '#AutoTradeSignal').Replace('dbo.Signal', '#Signal')
$schema = Get-Content (Join-Path $root 'FIGCommon/DataSchema/FIGAutoTrader_Schema.sql') -Raw
$upsert = [regex]::Match($schema, '(?s)CREATE\s+PROCEDURE \[dbo\]\.\[usp_autotrade_signal_upsert\].*?(?=\r?\nGO)').Value
if (!$upsert) { throw 'Production upsert not found' }
$upsert = [regex]::Replace($upsert, 'CREATE\s+PROCEDURE \[dbo\]\.\[usp_autotrade_signal_upsert\]', 'CREATE PROCEDURE #upsert')
$upsert = $upsert.Replace('[dbo].[AutoTradeSignal]', '#AutoTradeSignal').Replace('[dbo].[Bot]', '#Bot').Replace('[dbo].[usp_systemalert_add]', '#alert')
$setup = @'
SET NOCOUNT ON;
CREATE TABLE #Bot (Id int, AccountId varchar(50), BrokerServiceId varchar(50), Status varchar(20), LastUpdated int);
CREATE TABLE #Alerts (Message nvarchar(max));
CREATE TABLE #Signal (Id int PRIMARY KEY, Status varchar(10));
CREATE TABLE #AutoTradeSignalOrder (AutoTradeSignalId int, OrderTag varchar(200));
CREATE TABLE #AutoTradeSignal (
 Id int IDENTITY PRIMARY KEY, AutoTradeId int, SignalId int, Tag varchar(50), BotId int, TickerId int,
 OpenStatus varchar(20), OpenStatusCode int, OpenAvgPrice decimal(18,4), CloseStatus varchar(20),
 CloseStatusCode int, CloseAvgPrice decimal(18,4), OrigQty int, FilledQty int, ManualQty int, LastUpdated int
);
INSERT #Bot VALUES (13, 'TEST', 'TEST', 'ACTIVE', 0);
INSERT #Signal VALUES (1,'STOP'), (2,'START');
-- Two old stopped signals must both be found, not merely the latest one.
INSERT #AutoTradeSignal (AutoTradeId,SignalId,BotId,OpenStatus,CloseStatus,FilledQty,ManualQty) VALUES
(58,1,13,'FILLED','NONE',1,0),
(58,1,13,'FILLED_MANUALLY','NONE',0,1),
(58,2,13,'FILLED','NONE',1,0), -- still START: leave alone
(58,1,13,'FILLED','PROCESSING',1,0), -- close in progress
(58,1,13,'FILLED','FILLED',1,0), -- completed close
(58,1,13,'FILLED','FAILED',1,0), -- already failed
(58,1,13,'FILLED','NONE',1,0), -- close order exists despite stale status
(48,1,13,'FILLED','NONE',1,0), -- other AutoTrade, same BOT
(58,1,13,'FAILED','NONE',0,0); -- no exposure: still finalize bookkeeping
INSERT #AutoTradeSignalOrder VALUES (7,'CLOSE');
GO
CREATE PROCEDURE #alert @Source varchar(50), @Level varchar(20), @Message nvarchar(max)
AS INSERT #Alerts VALUES (@Message);
GO
'@
$cases = @'
DECLARE @AutoTradeId int = 58;
DECLARE @Selected TABLE (Id int);
INSERT @Selected
__SELECTION__
IF (SELECT COUNT(*) FROM @Selected) <> 3
 OR EXISTS (SELECT 1 FROM @Selected WHERE Id NOT IN (1,2,9))
 THROW 51000, 'Incorrect pending-close selection', 1;
PRINT 'PASS: All outstanding stopped signals selected; START, completed, processing, existing close orders and other AutoTrades excluded';

DECLARE @Id int, @New int, @Open varchar(20), @Filled int, @Manual int;
DECLARE pending CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM @Selected ORDER BY Id;
OPEN pending;
FETCH NEXT FROM pending INTO @Id;
WHILE @@FETCH_STATUS = 0
BEGIN
 UPDATE #Bot SET Status = 'ACTIVE';
 DELETE #Alerts;
 SELECT @Open=OpenStatus,@Filled=FilledQty,@Manual=ManualQty FROM #AutoTradeSignal WHERE Id=@Id;
 EXEC #upsert @New OUTPUT,@Id,58,1,'TEST',13,1,@Open,13,0,'FAILED',-16,0,1,@Filled,@Manual,-1;
 IF NOT EXISTS (SELECT 1 FROM #AutoTradeSignal WHERE Id=@Id AND CloseStatus='FAILED' AND CloseStatusCode=-16)
  THROW 51001, 'Stopped failure was not saved', 1;
 IF (SELECT Status FROM #Bot) <> CASE WHEN @Id=9 THEN 'ACTIVE' ELSE 'SUSPECT' END
  THROW 51002, 'Incorrect suspect transition', 1;
 IF (SELECT COUNT(*) FROM #Alerts) <> CASE WHEN @Id=9 THEN 0 ELSE 1 END
  THROW 51003, 'Incorrect alert count', 1;
 FETCH NEXT FROM pending INTO @Id;
END;
CLOSE pending;
DEALLOCATE pending;
PRINT 'PASS: Filled and manual exposure mark BOT suspect; zero exposure does not';
DELETE @Selected;
INSERT @Selected
__SELECTION__
IF EXISTS (SELECT 1 FROM @Selected) THROW 51004, 'Repeated notifications must be idempotent', 1;
IF NOT EXISTS (SELECT 1 FROM #AutoTradeSignal WHERE Id=8 AND CloseStatus='NONE')
 THROW 51005, 'Other AutoTrade was modified', 1;
PRINT 'PASS: Repeated notification does not reprocess failures or change the other AutoTrade';
GO
'@
$cases = $cases.Replace('__SELECTION__', $selection)
$testFile = Join-Path ([IO.Path]::GetTempPath()) ('autotrade-stopped-' + [guid]::NewGuid() + '.sql')
try {
 [IO.File]::WriteAllText($testFile, $setup + "`r`n" + $upsert + "`r`nGO`r`n" + $cases)
 & sqlcmd -S $Server -E -d tempdb -b -l 10 -i $testFile
 if ($LASTEXITCODE -ne 0) { throw 'Stopped signal regression checks failed' }
} finally {
 Remove-Item -LiteralPath $testFile -ErrorAction SilentlyContinue
}
