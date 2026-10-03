param(
    [string]$Executable = "$PSScriptRoot\..\x64\Release\FIGSignalCpp.exe",
    [string]$Settings = "$PSScriptRoot\..\signal-settings.json"
)
$ErrorActionPreference = 'Stop'
$root = Join-Path "$PSScriptRoot\..\optimizer-output" ('cache-test-' + [guid]::NewGuid().ToString('N'))
$parameters = Join-Path $root 'parameters'
$cache = Join-Path $root 'cache'
New-Item -ItemType Directory -Path $parameters | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot\..\adx-base-parameters.json" -Destination (Join-Path $parameters 'baseline.json')
function Run([string]$Name, [string]$Mode, [int]$Interval = 5, [string]$End = '') {
    $report = Join-Path $root $Name
    $nativeArgs = @('--backtest-batch','--dataset','3','--max-bars','12000','--jobs','1',
        '--parameters-dir',$parameters,'--output-dir',$report,'--settings',$Settings,
        '--interval-minutes',"$Interval",'--price-cache-dir',$cache,'--price-cache-mode',$Mode)
    if ($End) { $nativeArgs += @('--end',$End) }
    & $Executable @nativeArgs
    if ($LASTEXITCODE -ne 0) { throw "$Name failed" }
    return $report
}
function Same([string]$Left,[string]$Right) {
    foreach ($file in @('prices.csv','results.csv','run-0001-monthly.csv','run-0001-trades.csv')) {
        $a = (Get-FileHash -LiteralPath (Join-Path $root "$Left\$file")).Hash
        $b = (Get-FileHash -LiteralPath (Join-Path $root "$Right\$file")).Hash
        if ($a -ne $b) { throw "$file differs: $Left vs $Right" }
    }
}
function Status([string]$Name,[string]$Expected) {
    if (!(Select-String -LiteralPath (Join-Path $root "$Name\batch-settings.txt") -SimpleMatch "PriceCache=$Expected" -Quiet)) {
        throw "$Name did not use $Expected"
    }
}
Run 'off' 'off'
Run 'miss' 'use'
Run 'hit' 'use'
Same 'off' 'miss'; Same 'off' 'hit'; Status 'miss' 'database-snapshot'; Status 'hit' 'aggregate-hit'
Run 'ten-cached' 'use' 10
Run 'ten-direct' 'off' 10
Same 'ten-cached' 'ten-direct'; Status 'ten-cached' 'raw-hit'
Run 'end-cached' 'use' 5 '2010-01-05'
Run 'end-direct' 'off' 5 '2010-01-05'
Same 'end-cached' 'end-direct'; Status 'end-cached' 'raw-hit'
Run 'refresh' 'refresh'
Same 'off' 'refresh'; Status 'refresh' 'database-snapshot'
$aggregate = Get-ChildItem -LiteralPath $cache -Filter '*-i5-e9223372036854775807-aggregate-v1.bin' | Select-Object -First 1
$bytes = [IO.File]::ReadAllBytes($aggregate.FullName)
$bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 1
[IO.File]::WriteAllBytes($aggregate.FullName, $bytes)
$failed = $false
try { Run 'corrupt' 'use' } catch { $failed = $true }
if (!$failed) { throw 'Corrupt cache was accepted' }
Run 'repaired' 'refresh'
Same 'off' 'repaired'
Write-Output "Cache integration checks passed: $root"
