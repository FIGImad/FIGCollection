#requires -Version 7.0
param([string]$DeploymentDirectory = 'C:\Roots\OptimizationV2')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$repository = Split-Path -Parent $workspace
$patch = Join-Path $PSScriptRoot 'FIGOptimizationRunner-LADX.patch'
& git -c "safe.directory=$($repository.Replace('\','/'))" -C $repository apply --check $patch
if ($LASTEXITCODE -eq 0) {
    & git -c "safe.directory=$($repository.Replace('\','/'))" -C $repository apply $patch
    if ($LASTEXITCODE -ne 0) { throw 'Could not update the runner source project.' }
} else {
    & git -c "safe.directory=$($repository.Replace('\','/'))" -C $repository apply --reverse --check $patch
    if ($LASTEXITCODE -ne 0) { throw 'Runner sources changed; the rename patch needs review before applying.' }
    Write-Output 'Runner source rename is already applied.'
}
& (Join-Path $PSScriptRoot 'Update-LADX-Deployment.ps1') -Directory $DeploymentDirectory
