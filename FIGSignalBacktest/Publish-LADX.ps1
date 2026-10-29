param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$pluginProject=Join-Path $PSScriptRoot '..\..\FIGSignalCollection\FIG.StudyCollections.LADX\FIG.StudyCollections.LADX.csproj'
$output=Join-Path $PSScriptRoot "bin\$Configuration\net10.0\publish"
& dotnet publish "$PSScriptRoot\FIGSignalBacktest.csproj" -c $Configuration -o $output -m:1 -p:RestoreDisableParallel=true
if($LASTEXITCODE) { throw 'Host publish failed.' }
& dotnet build $pluginProject -c $Configuration -m:1 -p:RestoreDisableParallel=true
if($LASTEXITCODE) { throw 'LADX build failed.' }
$pluginRoot=Split-Path $pluginProject
$destination=Join-Path $output 'Plugins\LADX'
New-Item -ItemType Directory -Force $destination | Out-Null
foreach($extension in @('dll','pdb')) {
    $source=Join-Path $pluginRoot "bin\$Configuration\net10.0\FIG.StudyCollections.LADX.$extension"
    if(Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $destination -Force }
}
Copy-Item -LiteralPath (Join-Path $pluginRoot 'plugin.json') -Destination $destination -Force
Write-Host "Ready to deploy: $output"
