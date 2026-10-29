param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$build = Join-Path $root "x64\$Configuration"
$test = Join-Path $build ('package-test-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($test) | Out-Null
foreach ($name in @('FIGSignalCpp.exe','FIGStudyCollection.DotNetLADX.dll')) {
    Copy-Item -LiteralPath (Join-Path $build $name) -Destination $test
}
$oldCache = $env:FIG_LADX_CACHE
$env:FIG_LADX_CACHE = Join-Path $test 'cache'
try {
    $workers = @()
    for ($i = 0; $i -lt 4; ++$i) {
        $workers += Start-Process -FilePath "$test\FIGSignalCpp.exe" -WorkingDirectory $test -WindowStyle Hidden -PassThru `
            -ArgumentList '--test-collection --collection-dll FIGStudyCollection.DotNetLADX.dll' `
            -RedirectStandardOutput "$test\worker-$i.out" -RedirectStandardError "$test\worker-$i.err"
    }
    foreach ($worker in $workers) {
        if (!$worker.WaitForExit(60000)) { throw 'Concurrent package worker timed out.' }
        $worker.Refresh()
        if ($worker.ExitCode -ne 0) { throw "Package worker failed; inspect $test" }
    }
    $versions = @(Get-ChildItem -LiteralPath $env:FIG_LADX_CACHE -Directory)
    if ($versions.Count -ne 1) { throw 'Expected one shared content-addressed cache.' }
    $config = Join-Path $versions[0].FullName 'FIG.LADXBridge.runtimeconfig.json'
    [IO.File]::WriteAllText($config, 'damaged-cache')
    Remove-Item -LiteralPath (Join-Path $versions[0].FullName 'nethost.dll')
    Push-Location $test
    try {
        & .\FIGSignalCpp.exe --test-collection --collection-dll .\FIGStudyCollection.DotNetLADX.dll
        if ($LASTEXITCODE) { throw 'Cache repair failed.' }
    } finally { Pop-Location }
    Get-Content -LiteralPath $config -Raw | ConvertFrom-Json | Out-Null
    Write-Host "PASS: standalone DLL, four concurrent workers, warm cache, missing-file and corruption repair. Artifacts: $test"
} finally { $env:FIG_LADX_CACHE = $oldCache }
