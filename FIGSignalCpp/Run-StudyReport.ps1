param(
    [int]$DataSetId = 1,
    [int]$Period = 128,
    [int]$AdxSmoothing = 0,
    [string]$Source = 'close',
    [string]$Output = 'study-values.csv',
    [int]$MaxBars = 0,
    [ValidateRange(1, 35791394)]
    [int]$IntervalMinutes = 1,
    [string]$Collection = '',
    [string]$Parameters = '',
    [string]$Settings = "$PSScriptRoot/signal-settings.json",
    [string]$Executable = ''
)

$ErrorActionPreference = 'Stop'
if (-not $Executable) {
    $Executable = "$PSScriptRoot/x64/Release/FIGSignalCpp.exe"
    if (-not (Test-Path -LiteralPath $Executable)) { $Executable = "$PSScriptRoot/x64/Debug/FIGSignalCpp.exe" }
}
if (-not (Test-Path -LiteralPath $Executable)) { throw 'Build FIGSignalCpp before running this script.' }
if ($DataSetId -le 0 -or $Period -lt 2 -or $AdxSmoothing -lt 0 -or $MaxBars -lt 0) {
    throw 'Use a positive dataset ID, period >= 2, and nonnegative smoothing/max-bars.'
}
if ($AdxSmoothing -eq 0) { $AdxSmoothing = $Period }
$configuration = Get-Content -LiteralPath $Settings -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($configuration.ODBCName)) { throw 'Set ODBCName in the settings file.' }
$collectionArguments = @()
if ($Collection) { $collectionArguments += @('--collection', $Collection) }
if ($Parameters) { $collectionArguments += @('--parameters', $Parameters) }
& $Executable --calculate-studies --dataset $DataSetId --period $Period --adx-smoothing $AdxSmoothing --source $Source --max-bars $MaxBars --interval-minutes $IntervalMinutes --output $Output --settings $Settings @collectionArguments
if ($LASTEXITCODE -ne 0) { throw "Study report failed (exit $LASTEXITCODE)." }
