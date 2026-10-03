param([string]$Server = 'localhost')
$ErrorActionPreference = 'Stop'

# Exercise the real deployment procedure using session-local tables/procedures only.
# No application database is read or modified.
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$migration = Get-Content (Join-Path $root 'FIGInstaller/Resources/fig_autotrader_suspect_transition_migration.sql') -Raw
$procedure = [regex]::Match($migration, '(?s)ALTER PROCEDURE.*?(?=\r?\nGO)').Value
if (!$procedure) { throw 'Migration procedure not found' }
$procedure = $procedure.Replace('ALTER PROCEDURE [dbo].[usp_autotrade_signal_upsert]', 'CREATE PROCEDURE #upsert')
$procedure = $procedure.Replace('[dbo].[AutoTradeSignal]', '#AutoTradeSignal').Replace('[dbo].[Bot]', '#Bot').Replace('[dbo].[usp_systemalert_add]', '#alert')
$setup = @'
SET NOCOUNT ON;
CREATE TABLE #Bot (Id int, AccountId varchar(50), BrokerServiceId varchar(50), Status varchar(20), LastUpdated int);
CREATE TABLE #Alerts (Message nvarchar(max));
CREATE TABLE #AutoTradeSignal (
    Id int IDENTITY PRIMARY KEY, AutoTradeId int, SignalId int, Tag varchar(50), BotId int, TickerId int,
    OpenStatus varchar(20), OpenStatusCode int, OpenAvgPrice decimal(18,4), CloseStatus varchar(20),
    CloseStatusCode int, CloseAvgPrice decimal(18,4), OrigQty int, FilledQty int, ManualQty int, LastUpdated int
);
INSERT #Bot VALUES (1, 'TEST', 'TEST', 'ACTIVE', 0);
GO
CREATE PROCEDURE #alert @Source varchar(50), @Level varchar(20), @Message nvarchar(max)
AS INSERT #Alerts VALUES (@Message);
GO
'@
$cases = @'
DECLARE @Cases TABLE (
    Name varchar(100), PreviousStatus varchar(20), NextStatus varchar(20),
    BotStatus varchar(20), OpenStatus varchar(20), FilledQty int, ManualQty int, ExpectedAlert bit
);
INSERT @Cases VALUES
('New failed close', 'PROCESSING', 'FAILED', 'ACTIVE', 'FILLED', 8, 0, 1),
('Repeated failure after manual reactivation', 'FAILED', 'FAILED', 'ACTIVE', 'FILLED', 8, 0, 0),
('Final partial close', 'PROCESSING', 'FILLED_PARTIALLY_FIN', 'ACTIVE', 'FILLED', 8, 0, 1),
('Repeated partial close', 'FILLED_PARTIALLY_FIN', 'FILLED_PARTIALLY_FIN', 'ACTIVE', 'FILLED', 8, 0, 0),
('Failure to partial is same episode', 'FAILED', 'FILLED_PARTIALLY_FIN', 'ACTIVE', 'FILLED', 8, 0, 0),
('Partial to failure is same episode', 'FILLED_PARTIALLY_FIN', 'FAILED', 'ACTIVE', 'FILLED', 8, 0, 0),
('Successful close', 'PROCESSING', 'FILLED', 'ACTIVE', 'FILLED', 8, 0, 0),
('Zero exposure', 'PROCESSING', 'FAILED', 'ACTIVE', 'FILLED', 0, 0, 0),
('Manual fill exposure', 'PROCESSING', 'FAILED', 'ACTIVE', 'FILLED_MANUALLY', 0, 8, 1),
('Manual zero quantity overrides fill', 'PROCESSING', 'FAILED', 'ACTIVE', 'FILLED_MANUALLY', 8, 0, 0),
('Short position', 'PROCESSING', 'FAILED', 'ACTIVE', 'FILLED', -8, 0, 1),
('Already suspect', 'PROCESSING', 'FAILED', 'SUSPECT', 'FILLED', 8, 0, 0),
('Inactive preserved', 'PROCESSING', 'FAILED', 'INACTIVE', 'FILLED', 8, 0, 0),
('Null previous status', NULL, 'FAILED', 'ACTIVE', 'FILLED', 8, 0, 1);
DECLARE @Name varchar(100), @Previous varchar(20), @Next varchar(20), @BotStatus varchar(20),
    @Open varchar(20), @Filled int, @Manual int, @Alert bit, @Id int, @IdNew int;
DECLARE cases CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM @Cases;
OPEN cases;
FETCH NEXT FROM cases INTO @Name, @Previous, @Next, @BotStatus, @Open, @Filled, @Manual, @Alert;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE #AutoTradeSignal;
    DELETE #Alerts;
    UPDATE #Bot SET Status = @BotStatus;
    INSERT #AutoTradeSignal (CloseStatus) VALUES (@Previous);
    SET @Id = SCOPE_IDENTITY();
    EXEC #upsert @IdNew OUTPUT, @Id, 1, 1, 'TEST', 1, 1, @Open, 13, 0, @Next, -7, 0, 8, @Filled, @Manual, -1;
    IF (SELECT COUNT(*) FROM #Alerts) <> CONVERT(int, @Alert)
        THROW 51000, @Name, 1;
    IF (SELECT Status FROM #Bot) <> CASE WHEN @Alert = 1 THEN 'SUSPECT' ELSE @BotStatus END
        THROW 51001, @Name, 1;
    IF @IdNew <> @Id OR NOT EXISTS (SELECT 1 FROM #AutoTradeSignal WHERE Id = @Id AND CloseStatus = @Next)
        THROW 51002, @Name, 1;
    PRINT 'PASS: ' + @Name;
    FETCH NEXT FROM cases INTO @Name, @Previous, @Next, @BotStatus, @Open, @Filled, @Manual, @Alert;
END;
CLOSE cases;
DEALLOCATE cases;

-- A retry returning to PROCESSING must re-arm failure detection.
DELETE #Alerts;
UPDATE #Bot SET Status = 'ACTIVE';
EXEC #upsert @IdNew OUTPUT, @Id, 1, 1, 'TEST', 1, 1, 'FILLED', 13, 0, 'PROCESSING', 1, 0, 8, 8, 0, -1;
EXEC #upsert @IdNew OUTPUT, @Id, 1, 1, 'TEST', 1, 1, 'FILLED', 13, 0, 'FAILED', -7, 0, 8, 8, 0, -1;
IF (SELECT COUNT(*) FROM #Alerts) <> 1 OR (SELECT Status FROM #Bot) <> 'SUSPECT'
    THROW 51003, 'Retry failure must mark suspect again', 1;
PRINT 'PASS: Retry failure';
GO
'@
$testFile = Join-Path ([IO.Path]::GetTempPath()) ('autotrade-suspect-' + [guid]::NewGuid() + '.sql')
try {
    [IO.File]::WriteAllText($testFile, $setup + "`r`n" + $procedure + "`r`nGO`r`n" + $cases)
    & sqlcmd -S $Server -E -d tempdb -b -l 10 -i $testFile
    if ($LASTEXITCODE -ne 0) { throw 'SQL regression checks failed' }
} finally {
    Remove-Item -LiteralPath $testFile -ErrorAction SilentlyContinue
}
