# Run from an elevated PowerShell after applying the database migration.
# Installs two stopped instances; configure producers before starting them.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$PublishDirectory)
$ErrorActionPreference = 'Stop'
$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path
$executable = Join-Path $publishRoot 'FIGAlertSvc.exe'
if (!(Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Missing executable: $executable" }
$instances = @(
    @{ Name = 'FIG_SystemAlertSvc'; DisplayName = 'FIG_SystemAlertSvc'; Description = 'Processes and delivers FIG signal alerts.'; Settings = 'alerts-settings.system.json'; Mode = 'SystemOnly' },
    @{ Name = 'FIG_AlertSvc'; DisplayName = 'FIG_AlertSvc'; Description = 'Processes and delivers FIG application and operational alerts other than system alerts.'; Settings = 'alerts-settings.other.json'; Mode = 'OtherOnly' }
)
# Check every input before changing service registrations.
foreach ($instance in $instances) {
    $settingsPath = Join-Path $publishRoot $instance.Settings
    $config = Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json
    if ($config.AlertProcessing.Mode -ne $instance.Mode) { throw "Unexpected mode in $settingsPath" }
    if (Get-Service -Name $instance.Name -ErrorAction SilentlyContinue) {
        throw "Service $($instance.Name) already exists. Stop and remove the old registration before installing the split instances."
    }
}
foreach ($instance in $instances) {
    $settingsPath = Join-Path $publishRoot $instance.Settings
    $binaryPath = '"{0}" --windows-service-name "{1}" --settings-file "{2}"' -f $executable, $instance.Name, $settingsPath
    New-Service -Name $instance.Name -DisplayName $instance.DisplayName -Description $instance.Description -BinaryPathName $binaryPath -StartupType Automatic

    # Restart after 60 seconds on the first, second, and all subsequent failures.
    # Windows repeats the final recovery action for subsequent failures.
    & "$env:SystemRoot\System32\sc.exe" failure $instance.Name reset= 86400 actions= restart/60000/restart/60000/restart/60000
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to configure recovery actions for $($instance.Name) (exit code $LASTEXITCODE). The service was installed but remains stopped."
    }
    # Also recover when the service reports a nonzero exit code on stopping.
    & "$env:SystemRoot\System32\sc.exe" failureflag $instance.Name 1
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to enable recovery on non-crash failures for $($instance.Name) (exit code $LASTEXITCODE). The service was installed but remains stopped."
    }
}
Write-Host 'Installed both services without starting them. Finish the producer routing configuration, then start both services.'
