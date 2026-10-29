param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$LadxProject,
    [string]$MSBuild,
    [switch]$NoPdb
)
$ErrorActionPreference = 'Stop'
$dotnetRoot = Split-Path (Get-Command dotnet -ErrorAction Stop).Source
$pack = Get-ChildItem (Join-Path $dotnetRoot 'packs\Microsoft.NETCore.App.Host.win-x64') -Directory |
    Where-Object { $_.Name -match '^10\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$pack) { throw 'Install the x64 .NET 10 SDK (native hosting pack required).' }
$native = Join-Path $pack.FullName 'runtimes\win-x64\native'
if (!$MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $MSBuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (!$MSBuild) { throw 'Install Visual Studio with the v145 C++ toolset, or pass -MSBuild.' }
$argsNative = @("$PSScriptRoot\FIGStudyCollection.DotNetLADX.vcxproj", "/p:Configuration=$Configuration", '/p:Platform=x64', "/p:DotNetHostNativeDir=$native", '/v:minimal', '/nologo')
if ($LadxProject) { $argsNative += "-p:LadxProject=$LadxProject" }
if ($NoPdb) { $argsNative += "/p:ForceImportBeforeCppTargets=$PSScriptRoot\build\DotNetBridgeNoPdb.props" }
& $MSBuild @argsNative
if ($LASTEXITCODE) { throw 'Native LADX bridge build failed.' }
Write-Host "Built $PSScriptRoot\x64\$Configuration\FIGStudyCollection.DotNetLADX.dll"

