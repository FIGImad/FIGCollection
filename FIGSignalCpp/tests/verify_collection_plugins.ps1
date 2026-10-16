param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$IncompatibleDll = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$bin = Join-Path $root "x64/$Configuration"
$executable = Join-Path $bin 'FIGSignalCpp.exe'
$example = Join-Path $bin 'FIGStudyCollection.Example.dll'
$adx = Join-Path $bin 'FIGStudyCollection.ADX.dll'
if (!$IncompatibleDll) { $IncompatibleDll = Join-Path $bin 'FIGCollectionPluginFixture.dll' }
$IncompatibleDll = [IO.Path]::GetFullPath($IncompatibleDll)
foreach ($file in @($executable, $example, $adx, $IncompatibleDll)) {
    if (!(Test-Path -LiteralPath $file)) { throw "Build FIGSignalCpp.slnx first: missing $file" }
}
# A fresh isolated deployment also exercises paths containing spaces.
$testRoot = Join-Path $bin ('plugin tests ' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
Copy-Item -LiteralPath $executable -Destination $testRoot
Copy-Item -LiteralPath $example -Destination $testRoot
$isolatedHost = Join-Path $testRoot 'FIGSignalCpp.exe'
$isolatedDll = Join-Path $testRoot 'FIGStudyCollection.Example.dll'
function Check([string]$Exe, [string[]]$Arguments, [bool]$Success, [string[]]$Expected) {
    $result = & $Exe @Arguments 2>&1 | Out-String
    $code = $LASTEXITCODE
    if (($code -eq 0) -ne $Success) {
        throw "Unexpected exit $code for $($Arguments -join ' '): $result"
    }
    foreach ($message in $Expected) {
        if ($result -notlike "*$message*") { throw "Missing '$message' for $($Arguments -join ' '): $result" }
    }
}
Check $executable @('--self-test') $true 'self-test passed'
Push-Location $testRoot
try {
    Check $executable @('--list-collections') $true 'ADXBASE'
    Check $isolatedHost @('--test-collection', '--collection-dll', '.\FIGStudyCollection.Example.dll') $true 'self-test passed'
    Check $isolatedHost @('--list-collections', '--collection-dll', $isolatedDll) $true 'EXAMPLEMA'
    Check $isolatedHost @('--list-collections') $false 'Cannot load collection DLL'
    Check $isolatedHost @('--list-collections', '--collection-dll', '.\missing.dll') $false 'Cannot load collection DLL'
    Check $isolatedHost @('--list-collections', '--collection-dll', "$env:WINDIR\System32\version.dll") $false 'does not export'
    $output = Join-Path $testRoot 'must-not-be-created.csv'
    Check $isolatedHost @('--calculate-studies', '--dataset', '1', '--output', $output,
        '--settings', 'missing-settings.json', '--collection-dll', $isolatedDll, '--collection', 'ADXBase') $false 'Unsupported collection type'
    Check $isolatedHost @('--backtest-batch', '--collection-dll', $isolatedDll, '--collection', 'ADXBase',
        '--settings', 'missing-settings.json') $false 'Unsupported collection type'
    if (Test-Path -LiteralPath $output) { throw 'Invalid collection opened an output file' }
    if ($IncompatibleDll) {
        Check $isolatedHost @('--list-collections', '--collection-dll', $IncompatibleDll) $false 'Incompatible collection DLL API'
    }
    $other = if ($Configuration -eq 'Release') { 'Debug' } else { 'Release' }
    $otherDll = Join-Path $root "x64/$other/FIGStudyCollection.Example.dll"
    if (Test-Path -LiteralPath $otherDll) {
        $hostLevel = if ($Configuration -eq 'Debug') { 2 } else { 0 }
        $dllLevel = if ($other -eq 'Debug') { 2 } else { 0 }
        Check $isolatedHost @('--list-collections', '--collection-dll', $otherDll) $false @(
            'does not match the host', "Host: *_ITERATOR_DEBUG_LEVEL=$hostLevel",
            "DLL: *_ITERATOR_DEBUG_LEVEL=$dllLevel", 'omit --collection-dll')
    }
} finally {
    Pop-Location
}
Write-Output "Collection plugin checks passed ($Configuration). Test deployment: $testRoot"
