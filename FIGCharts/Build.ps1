param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$locator = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $locator -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'Visual Studio C++ build tools were not found.' }
& $msbuild (Join-Path $PSScriptRoot 'FIGCharts.vcxproj') "/p:Configuration=$Configuration" /p:Platform=x64 /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "FIGCharts build failed ($LASTEXITCODE)." }
