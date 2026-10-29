param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$output = Join-Path $root "x64\$Configuration\dotnet-ladx-test"
& dotnet build "$PSScriptRoot\DotNetLADXReference\DotNetLADXReference.csproj" -c $Configuration -m:1 -p:RestoreDisableParallel=true
if ($LASTEXITCODE) { throw 'Reference build failed.' }
& dotnet run --no-build --project "$PSScriptRoot\DotNetLADXReference\DotNetLADXReference.csproj" -c $Configuration -- $output
if ($LASTEXITCODE) { throw 'Reference generation failed.' }
& "$root\x64\$Configuration\FIGCollectionProbe.exe" "$root\x64\$Configuration\FIGStudyCollection.DotNetLADX.dll" LADX "$output\parameters.json" "$output\prices.csv" "$output\actual.csv" 120
if ($LASTEXITCODE) { throw 'Native bridge replay failed.' }
$expected = @(Import-Csv "$output\expected.csv")
$actual = @(Import-Csv "$output\actual.csv")
if ($expected.Count -ne $actual.Count) { throw 'Row counts differ.' }
$columns = @($expected[0].PSObject.Properties.Name)
$actualColumns = @($actual[0].PSObject.Properties.Name)
if (@(Compare-Object $columns $actualColumns).Count) { throw 'Output columns differ.' }
$culture = [Globalization.CultureInfo]::InvariantCulture
for ($i = 0; $i -lt $expected.Count; ++$i) {
    foreach ($key in $columns) {
        $a = $actual[$i].$key; $e = $expected[$i].$key
        if ($a -ceq $e) { continue }
        [double]$an = 0; [double]$en = 0
        if (![double]::TryParse($a, [Globalization.NumberStyles]::Float, $culture, [ref]$an) -or
            ![double]::TryParse($e, [Globalization.NumberStyles]::Float, $culture, [ref]$en) -or
            ![double]::IsFinite($an) -or ![double]::IsFinite($en) -or
            [Math]::Abs($an - $en) -gt 1e-12 * [Math]::Max(1, [Math]::Abs($en))) {
            throw "Mismatch row $i column ${key}: expected '$e', actual '$a'."
        }
    }
}
Write-Host "PASS: $($expected.Count) bars, $($columns.Count - 1) study outputs match the .NET collection factory."
