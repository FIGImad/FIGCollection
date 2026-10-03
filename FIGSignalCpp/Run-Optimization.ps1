# Requires PowerShell 7+. Generates reproducible candidates, then invokes one native batch.
param(
    [string]$Config = "$PSScriptRoot/optimizer-search.json",
    [string]$Executable = "$PSScriptRoot/x64/Release/FIGSignalCpp.exe",
    [int]$Samples = -1,
    [int]$MaxSourceBars = -1,
    [string]$OutputDirectory = '',
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$configPath = (Resolve-Path -LiteralPath $Config).Path
$configRoot = Split-Path -Parent $configPath
$c = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json -AsHashtable
function Resolve-ConfigPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return $Path }
    return [IO.Path]::GetFullPath((Join-Path $configRoot $Path))
}
$base = Get-Content -LiteralPath (Resolve-ConfigPath $c.BaseParameters) -Raw | ConvertFrom-Json -AsHashtable
$count = if ($Samples -ge 0) { $Samples } else { [int]$c.Samples }
if ($count -lt 0 -or $count -gt 100000) { throw 'Samples must be between 0 and 100000 (plus the baseline).' }
$maximum = if ($MaxSourceBars -ge 0) { $MaxSourceBars } else { [int]$c.MaxSourceBars }
$allowed = @('AdxLen','FastMALen','GuideMALen','GuideStdDev','StartBias','StopBias','ReEntryCooldownBars','ShockATRLen','ShockDropATR','ShockArmedDropATR','ShockArmUpATR','ShockRangeATR','ChopMinGuideWidthATR','ChopMaxGuideSlopeATR','ContinuationEntryBars')
$keys = @($c.Ranges.Keys | Sort-Object)
$space = [decimal]1
foreach ($key in $keys) {
    if ($key -notin $allowed -or -not $base.Contains($key)) { throw "Unsupported/inactive long-strategy search parameter: $key" }
    $values = @($c.Ranges[$key])
    if ($values.Count -eq 0) { throw "Empty range: $key" }
    foreach ($v in $values) {
        if ($null -eq $v -or $v -is [string] -or $v -is [bool] -or -not [double]::IsFinite([double]$v)) { throw "Range values must be finite numbers: $key" }
    }
    if (@($values | Select-Object -Unique).Count -ne $values.Count) { throw "Duplicate range values: $key" }
    $space = [math]::Min([decimal]100001, $space * $values.Count)
}
$random = [Random]::new([int]$c.Seed)
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
function Candidate-Key($p) {
    $ordered = [ordered]@{}
    foreach ($key in @($p.Keys | Sort-Object)) { $ordered[$key] = $p[$key] }
    return ConvertTo-Json -InputObject $ordered -Compress -Depth 8
}
$candidates = [Collections.Generic.List[string]]::new()
$baseline = Candidate-Key $base
[void]$seen.Add($baseline)
$candidates.Add($baseline)
$baselineInside = $true
foreach ($key in $keys) { if ($base[$key] -notin $c.Ranges[$key]) { $baselineInside = $false } }
$available = [int]$space - [int]$baselineInside
if ($count -gt $available) { throw "Requested $count distinct samples but only $available combinations remain besides the baseline." }
# Small spaces are shuffled without replacement. Large spaces use seeded rejection sampling.
if ($space -le 100000) {
    $indices = [Collections.Generic.List[int]]::new()
    for ($i=0; $i -lt $space; $i++) { $indices.Add($i) }
    for ($i=$indices.Count-1; $i -gt 0; $i--) {
        $j=$random.Next($i+1); $tmp=$indices[$i]; $indices[$i]=$indices[$j]; $indices[$j]=$tmp
    }
    foreach ($index in $indices) {
        if ($candidates.Count -ge $count+1) { break }
        $p = $base.Clone(); $value = $index
        foreach ($key in $keys) { $v=@($c.Ranges[$key]); $p[$key]=$v[$value % $v.Count]; $value=[int][math]::Floor($value / $v.Count) }
        $json=Candidate-Key $p
        if ($seen.Add($json)) { $candidates.Add($json) }
    }
} else {
    while ($candidates.Count -lt $count+1) {
        $p=$base.Clone()
        foreach ($key in $keys) { $v=@($c.Ranges[$key]); $p[$key]=$v[$random.Next($v.Count)] }
        $json=Candidate-Key $p
        if ($seen.Add($json)) { $candidates.Add($json) }
    }
}
if (-not $OutputDirectory) {
    $root = Resolve-ConfigPath $c.OutputRoot
    $OutputDirectory = Join-Path $root ((Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new output directory for each optimization batch.' }
$parameterDir = Join-Path $OutputDirectory 'parameters'
$reportDir = Join-Path $OutputDirectory 'reports'
[void][IO.Directory]::CreateDirectory($parameterDir)
for ($i=0; $i -lt $candidates.Count; $i++) {
    $name = 'run-{0:D4}.json' -f ($i+1)
    $pretty = $candidates[$i] | ConvertFrom-Json -AsHashtable | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText((Join-Path $parameterDir $name), $pretty)
}
$c.Samples=$count; $c.MaxSourceBars=$maximum
$c.BaseParameters=Resolve-ConfigPath $c.BaseParameters
$c.Settings=Resolve-ConfigPath $c.Settings
if (Test-Path -LiteralPath $Executable) {
    $c['ExecutableSHA256'] = (Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash
}
$c | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'search-settings.json') -Encoding utf8NoBOM
Write-Host "Prepared $($candidates.Count) candidates (baseline + $count samples): $parameterDir"
if ($PrepareOnly) { return }
if (-not (Test-Path -LiteralPath $Executable)) { throw 'Build the Release executable or specify -Executable.' }
$invariant = [Globalization.CultureInfo]::InvariantCulture
function Num($v) { return [Convert]::ToString($v, $invariant) }
$arguments = @('--backtest-batch','--dataset',"$($c.DataSetId)",'--interval-minutes',"$($c.IntervalMinutes)",
    '--parameters-dir',$parameterDir,'--output-dir',$reportDir,'--settings',$c.Settings,
    '--jobs',"$($c.Jobs)",'--max-bars',"$maximum",'--capital',(Num $c.InitialCapital),'--quantity',"$($c.Quantity)",
    '--multiplier',(Num $c.Multiplier),'--commission',(Num $c.CommissionPerContractPerSide),
    '--slippage-points',(Num $c.SlippagePointsPerSide),'--force-close',"$([int][bool]$c.ForceCloseAtEnd)",
    '--start',$c.StartDate,'--train-end',$c.TrainEndExclusive,'--validation-end',$c.ValidationEndExclusive)
if ($c.EndDateExclusive) { $arguments += @('--end',$c.EndDateExclusive) }
if ($c.Quantity -eq 0) {
    $allocation = if ($c.Contains('QuantityPct')) { $c.QuantityPct } else { 1 }
    $arguments += @('--quantity-pct', (Num $allocation))
}
& $Executable @arguments
if ($LASTEXITCODE -ne 0) { throw "Backtest batch failed (exit $LASTEXITCODE). Completed reports, if any, remain in $reportDir" }
Write-Host "Results: $(Join-Path $reportDir 'results.csv')"
