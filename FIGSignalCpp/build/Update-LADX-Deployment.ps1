#requires -Version 7.0
param([string]$Directory = 'C:\Roots\OptimizationV2')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$workspace = Split-Path -Parent $PSScriptRoot
$root = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/')
if (!(Test-Path -LiteralPath $root -PathType Container)) { throw "Deployment folder not found: $root" }
function Inside([string]$relative) {
    $full = [IO.Path]::GetFullPath((Join-Path $root $relative))
    if (!$full.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path leaves deployment folder: $relative"
    }
    return $full
}
function RenameText([string]$text) {
    return $text.Replace('LDAX', 'LADX').Replace('ldax', 'ladx').Replace('Ldax', 'Ladx')
}
function WriteJson([string]$file, $value) {
    [IO.File]::WriteAllText($file, ($value | ConvertTo-Json -Depth 30) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}
function FixConfig($c) {
    $c = (RenameText ($c | ConvertTo-Json -Depth 30)) | ConvertFrom-Json
    $c.Collection = 'LADX'
    $c.CollectionDll = Inside 'FIGStudyCollection.LADX.dll'
    $c.Signal = 'LADX_LongSide'
    $c.Executable = Inside 'FIGSignalCpp.exe'
    $c.BaseParameters = Inside 'ladx-parameters.json'
    $c.Settings = Inside 'signal-settings.json'
    return $c
}
$running = @(Get-Process -Name FIGSignalCpp,FIGOptimizationRunner -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase) })
if ($running.Count) { throw 'Stop the optimization processes before updating the deployment.' }
$dllSource = Join-Path $workspace 'x64/Release/FIGStudyCollection.LADX.dll'
$runnerSource = Join-Path $workspace 'x64/Release/optimizer'
$runnerFiles = @('FIGOptimizationRunner.exe','FIGOptimizationRunner.dll','FIGOptimizationRunner.deps.json','FIGOptimizationRunner.runtimeconfig.json')
foreach ($file in @($dllSource) + @($runnerFiles | ForEach-Object { Join-Path $runnerSource $_ })) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Build output missing: $file" }
}
$dllHash = (Get-FileHash -LiteralPath $dllSource -Algorithm SHA256).Hash
$exeHash = (Get-FileHash -LiteralPath (Inside 'FIGSignalCpp.exe') -Algorithm SHA256).Hash
$configPath = Inside 'search-settings.json'
$config = FixConfig (Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json)
$oldBase = Inside 'ldax-parameters.json'
$newBase = Inside 'ladx-parameters.json'
if (!(Test-Path -LiteralPath $oldBase) -and !(Test-Path -LiteralPath $newBase)) { throw 'Base parameter file is missing.' }
if ((Test-Path -LiteralPath $oldBase) -and (Test-Path -LiteralPath $newBase) -and
    (Get-FileHash -LiteralPath $oldBase).Hash -ne (Get-FileHash -LiteralPath $newBase).Hash) {
    throw 'Both base filenames exist with different contents; resolve before migrating.'
}
$oldOutput = Inside 'optimizer-output/ldax-3m'
$newOutput = Inside 'optimizer-output/ladx-3m'
if ((Test-Path -LiteralPath $oldOutput) -and (Test-Path -LiteralPath $newOutput)) {
    throw 'Both output folders exist; refusing to merge or overwrite them.'
}
$plans = @()
$savedConfigs = @()
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Filter 'batch.json' |
    Where-Object { $_.FullName -notmatch '[\\/]\.ladx-backup-' }) {
    $m = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    if ($m.Configuration.Collection -notin @('LDAX','LADX')) { continue }
    if ($m.Status -eq 'Running') { throw "Batch still marked Running: $($file.FullName)" }
    $parameters = Join-Path $file.DirectoryName 'parameters'
    $orphan = !(Test-Path -LiteralPath $parameters -PathType Container)
    if (!$orphan) {
        if ($m.Status -eq 'Completed') { throw "Completed batch must stay archived with its original build: $($file.FullName)" }
        if ($m.ExecutableSHA256 -and $m.ExecutableSHA256 -ne $exeHash) { throw "Native host differs from prepared batch: $($file.FullName)" }
        foreach ($p in $m.Candidates) {
            if ([IO.Path]::GetFileName($p.FileName) -ne $p.FileName) { throw 'Invalid candidate path.' }
            if ((Get-FileHash -LiteralPath (Join-Path $parameters $p.FileName) -Algorithm SHA256).Hash -ne $p.SHA256) {
                throw "Candidate changed: $($p.FileName)"
            }
        }
        $m.Configuration = FixConfig $m.Configuration
        $m.CollectionDllSHA256 = $dllHash
    }
    $plans += [pscustomobject]@{Path=$file.FullName;Value=$m;Orphan=$orphan}
}
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Filter 'search-settings.json' |
    Where-Object { $_.FullName -ne $configPath -and $_.FullName -notmatch '[\\/]\.ladx-backup-' }) {
    $c = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    if ($c.Collection -in @('LDAX','LADX')) { $savedConfigs += [pscustomobject]@{Path=$file.FullName;Value=(FixConfig $c)} }
}
# All checks precede writes. Keep originals and historical reports for inspection.
$backup = Inside ('.ladx-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
[void][IO.Directory]::CreateDirectory($backup)
function BackupFile([string]$file) {
    if (Test-Path -LiteralPath $file -PathType Leaf) {
        $relative = [IO.Path]::GetRelativePath($root, $file)
        $destination = Join-Path $backup $relative
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath $file -Destination $destination
    }
}
foreach ($file in @($configPath, $oldBase, $newBase, (Inside 'FIGStudyCollection.LADX.dll')) +
    @($runnerFiles | ForEach-Object { Inside $_ }) + @($plans | ForEach-Object { $_.Path }) + @($savedConfigs | ForEach-Object { $_.Path })) {
    BackupFile $file
}
Copy-Item -LiteralPath $dllSource -Destination (Inside 'FIGStudyCollection.LADX.dll') -Force
foreach ($file in $runnerFiles) { Copy-Item -LiteralPath (Join-Path $runnerSource $file) -Destination (Inside $file) -Force }
if (Test-Path -LiteralPath $oldBase) {
    if (Test-Path -LiteralPath $newBase) { Move-Item -LiteralPath $oldBase -Destination (Join-Path $backup 'duplicate-base-original.json') }
    else { Move-Item -LiteralPath $oldBase -Destination $newBase }
}
WriteJson $configPath $config
foreach ($p in $plans) {
    if ($p.Orphan) {
        # A manifest without its candidates cannot be executed; retain it only in the backup.
        Move-Item -LiteralPath $p.Path -Destination (Join-Path $backup ('orphan-' + [guid]::NewGuid().ToString('N') + '.json'))
    } else { WriteJson $p.Path $p.Value }
}
foreach ($c in $savedConfigs) { WriteJson $c.Path $c.Value }
if (Test-Path -LiteralPath $oldOutput) {
    # Inside() resolved and checked both absolute directory paths above; destination is absent.
    Move-Item -LiteralPath $oldOutput -Destination $newOutput
}
$oldDll = Inside 'FIGStudyCollection.LDAX.dll'
if (Test-Path -LiteralPath $oldDll) { Move-Item -LiteralPath $oldDll -Destination (Join-Path $backup 'FIGStudyCollection.LDAX.dll') }
Write-Output "Updated deployment: $root"
Write-Output "Original files: $backup"
Write-Output 'Collection=LADX; DLL=FIGStudyCollection.LADX.dll; Signal=LADX_LongSide'
Write-Output 'No backtests were started. Run FIGOptimizationRunner.exe run --config search-settings.json --samples 199 to start a fresh 200-case batch.'
