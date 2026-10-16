#requires -Version 5.1
param([string]$Directory = 'C:\Roots\OptimizationV2')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($Directory)
$files = @((Join-Path $root 'search-settings.json'))
$batches = @()
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Filter batch.json |
    Where-Object { $_.FullName -notmatch '[\\/]\.ladx-backup-' }) {
    $m = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    if ($m.Configuration.Collection -ne 'LADX' -or $m.Configuration.Signal -notin @('LADX_SIDE','STrend_LADX_LongSide')) { continue }
    if ($m.Status -eq 'Running') { throw "Stop the running batch before correcting it: $($file.FullName)" }
    if ($m.Status -notin @('Prepared','Failed','Cancelled')) { continue }
    $files += $file.FullName
    $snapshot = Join-Path $file.DirectoryName 'search-settings.json'
    if (Test-Path -LiteralPath $snapshot) { $files += $snapshot }
    $batches += $file.DirectoryName
}
$edits = @()
foreach ($file in $files | Select-Object -Unique) {
    $text = [IO.File]::ReadAllText($file)
    $parsed = $text | ConvertFrom-Json
    $c = if ($parsed.PSObject.Properties.Name -contains 'Configuration') { $parsed.Configuration } else { $parsed }
    if ($c.Collection -ne 'LADX') { throw "Expected collection LADX in $file" }
    if ($c.Signal -eq 'LADX_LongSide') { continue }
    if ($c.Signal -notin @('LADX_SIDE','STrend_LADX_LongSide')) { throw "Unexpected signal in $file" }
    $updated = [regex]::Replace($text, '("Signal"\s*:\s*")(LADX_SIDE|STrend_LADX_LongSide)(")', '${1}LADX_LongSide${3}')
    $null = $updated | ConvertFrom-Json
    $edits += [pscustomobject]@{File=$file;Before=$text;After=$updated}
}
$suffix = '.before-signal-fix-' + [guid]::NewGuid().ToString('N') + '.bak'
# Complete all backups before changing any configuration.
foreach ($edit in $edits) { [IO.File]::WriteAllText($edit.File + $suffix, $edit.Before, [Text.UTF8Encoding]::new($false)) }
foreach ($edit in $edits) {
    [IO.File]::WriteAllText($edit.File, $edit.After, [Text.UTF8Encoding]::new($false))
    Write-Output "Fixed: $($edit.File)"
}
Write-Output 'Signal=LADX_LongSide. Candidate files, checksums, cached prices, and prior reports were not changed.'
foreach ($batch in $batches) { Write-Output ('Retry: FIGOptimizationRunner.exe execute "' + $batch + '"') }
