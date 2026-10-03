#requires -Version 7.4
param(
    [string]$UserName = 'roots',
    [string]$Settings = "$PSScriptRoot/signal-settings.json"
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($UserName)) { throw 'A SQL username is required.' }
$configuration = Get-Content -LiteralPath $Settings -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($configuration.ODBCName)) { throw 'Set ODBCName in the settings file first.' }

# Same lookup order and payload as FIGCommon.Utilities.ProtectedDataUtil.Protect.
$encodedKey = $null
if (Test-Path -LiteralPath /run/secrets/FIG_MASTER_KEY) {
    $encodedKey = (Get-Content -LiteralPath /run/secrets/FIG_MASTER_KEY -Raw).Trim()
}
if ([string]::IsNullOrWhiteSpace($encodedKey)) {
    $encodedKey = [Environment]::GetEnvironmentVariable('FIG_MASTER_KEY', 'Machine')
    if ($null -eq $encodedKey) { $encodedKey = [Environment]::GetEnvironmentVariable('FIG_MASTER_KEY', 'User') }
    if ($null -eq $encodedKey) { $encodedKey = [Environment]::GetEnvironmentVariable('FIG_MASTER_KEY', 'Process') }
}
if ([string]::IsNullOrWhiteSpace($encodedKey)) { throw 'FIG_MASTER_KEY is not configured.' }
$key = [Convert]::FromBase64String($encodedKey)
$encodedKey = $null
$plain = $null
$password = $null
$aes = $null
try {
    if ($key.Length -ne 32) { throw 'FIG_MASTER_KEY must decode to exactly 32 bytes.' }
    $password = Read-Host "SQL password for $UserName on ODBC source $($configuration.ODBCName)" -AsSecureString
    if ($password.Length -eq 0) { throw 'A nonempty SQL password is required.' }
    $credential = [System.Net.NetworkCredential]::new($UserName, $password)
    $plain = [Text.Encoding]::UTF8.GetBytes($credential.Password)
    $credential = $null
    $nonce = [byte[]]::new(12)
    [Security.Cryptography.RandomNumberGenerator]::Fill($nonce)
    $ciphertext = [byte[]]::new($plain.Length)
    $tag = [byte[]]::new(16)
    $aes = [Security.Cryptography.AesGcm]::new($key, 16)
    $aes.Encrypt($nonce, $plain, $ciphertext, $tag)
    $encrypted = [Convert]::ToBase64String([byte[]]($nonce + $ciphertext + $tag))
    $configuration | Add-Member -NotePropertyName ODBCUser -NotePropertyValue $UserName -Force
    $configuration | Add-Member -NotePropertyName ODBCPassword -NotePropertyValue $encrypted -Force
    $configuration | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $Settings -Encoding utf8NoBOM
    Write-Host 'Saved ODBCUser and encrypted ODBCPassword. The master key was not changed.'
}
finally {
    if ($aes) { $aes.Dispose() }
    if ($password) { $password.Dispose() }
    if ($plain) { [Security.Cryptography.CryptographicOperations]::ZeroMemory($plain) }
    [Security.Cryptography.CryptographicOperations]::ZeroMemory($key)
}
