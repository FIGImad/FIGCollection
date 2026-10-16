#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$fixture = Join-Path $workspace ('optimizer-output/ladx-migration-check-' + [guid]::NewGuid().ToString('N'))
$sourceBatch = Join-Path $workspace 'optimizer-candidates/ladx-3m-200'
$oldBatch = Join-Path $fixture 'optimizer-output/ldax-3m/failed'
$newBatch = Join-Path $fixture 'optimizer-output/ladx-3m/failed'
[void][IO.Directory]::CreateDirectory((Join-Path $oldBatch 'parameters'))
[void][IO.Directory]::CreateDirectory((Join-Path $oldBatch 'reports'))
Copy-Item -LiteralPath (Join-Path $workspace 'x64/Release/FIGSignalCpp.exe') -Destination $fixture
[IO.File]::WriteAllText((Join-Path $fixture 'FIGStudyCollection.LDAX.dll'), 'old collection fixture')
Copy-Item -LiteralPath (Join-Path $workspace 'ladx-parameters.json') -Destination (Join-Path $fixture 'ldax-parameters.json')
[IO.File]::WriteAllText((Join-Path $fixture 'FIGOptimizationRunner.exe'), 'old runner fixture')
[IO.File]::WriteAllText((Join-Path $fixture 'signal-settings.json'), '{}')
[IO.File]::WriteAllText((Join-Path $oldBatch 'reports/evidence.txt'), 'preserve prior attempt')
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceBatch 'parameters') -File) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $oldBatch 'parameters')
}
$m = Get-Content -LiteralPath (Join-Path $sourceBatch 'batch.json') -Raw | ConvertFrom-Json
$m.Status = 'Failed'; $m.Attempts = 1; $m.Error = 'old signal name'
$m.Configuration.Collection = 'LDAX'; $m.Configuration.Signal = 'LADX_SIDE'
$m.Configuration.CollectionDll = Join-Path $fixture 'FIGStudyCollection.LDAX.dll'
$m.Configuration.BaseParameters = Join-Path $fixture 'ldax-parameters.json'
$m.Configuration.Executable = Join-Path $fixture 'FIGSignalCpp.exe'
$m.Configuration.Settings = Join-Path $fixture 'signal-settings.json'
$m.Configuration.OutputRoot = Join-Path $fixture 'optimizer-output/ldax-3m'
$m.CollectionDllSHA256 = (Get-FileHash -LiteralPath $m.Configuration.CollectionDll).Hash
$originalDllHash = $m.CollectionDllSHA256
$m | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $oldBatch 'batch.json')
$m | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $fixture 'batch.json')
$m.Configuration | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $fixture 'search-settings.json')
$m.Configuration | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $oldBatch 'search-settings.json')
& (Join-Path $workspace 'build/Update-LADX-Deployment.ps1') -Directory $fixture
$after = Get-Content -LiteralPath (Join-Path $newBatch 'batch.json') -Raw | ConvertFrom-Json
if ($after.Configuration.Collection -ne 'LADX' -or $after.Configuration.Signal -ne 'LADX_LongSide') { throw 'Selection migration failed.' }
if ($after.Status -ne 'Failed' -or $after.Attempts -ne 1) { throw 'Attempt history changed.' }
if ($after.CollectionDllSHA256 -eq $originalDllHash -or $after.CollectionDllSHA256 -ne
    (Get-FileHash -LiteralPath (Join-Path $fixture 'FIGStudyCollection.LADX.dll')).Hash) { throw 'Incorrect new DLL hash.' }
if ((Get-Content -LiteralPath (Join-Path $newBatch 'reports/evidence.txt') -Raw) -ne 'preserve prior attempt') { throw 'Historical output changed.' }
foreach ($p in $after.Candidates) {
    if ((Get-FileHash -LiteralPath (Join-Path $newBatch "parameters/$($p.FileName)")).Hash -ne $p.SHA256) { throw 'Candidate modified.' }
}
foreach ($old in @('ldax-parameters.json','FIGStudyCollection.LDAX.dll','batch.json','optimizer-output/ldax-3m')) {
    if (Test-Path -LiteralPath (Join-Path $fixture $old)) { throw "Obsolete active path remains: $old" }
}
& (Join-Path $fixture 'FIGSignalCpp.exe') --list-collections --collection-dll (Join-Path $fixture 'FIGStudyCollection.LADX.dll')
if ($LASTEXITCODE -ne 0) { throw 'Renamed DLL does not load.' }
& (Join-Path $fixture 'FIGOptimizationRunner.exe') prepare --config (Join-Path $fixture 'search-settings.json') --output-dir (Join-Path $fixture 'prepared')
if ($LASTEXITCODE -ne 0) { throw 'Migrated runner/config preparation failed.' }
$fresh = Get-Content -LiteralPath (Join-Path $fixture 'prepared/batch.json') -Raw | ConvertFrom-Json
if (($fresh.Candidates | ConvertTo-Json -Compress) -ne ($after.Candidates | ConvertTo-Json -Compress)) { throw 'Candidate values changed after migration.' }
Write-Output "LADX deployment migration passed, including all 200 candidate hashes: $fixture"
