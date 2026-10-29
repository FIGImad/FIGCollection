param([Parameter(Mandatory)][string]$PublishDirectory,
      [Parameter(Mandatory)][string]$NetHost,
      [Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PublishDirectory).Path.TrimEnd('\')
$out = [IO.Path]::GetFullPath($OutputDirectory)
[IO.Directory]::CreateDirectory($out) | Out-Null
$files = @(Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object Extension -ne '.pdb' | Sort-Object FullName)
foreach ($required in @('FIG.LADXBridge.dll','FIG.LADXBridge.runtimeconfig.json','FIG.LADXBridge.deps.json','FIG.StudyCollections.LADX.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $root $required))) { throw "Missing publish file: $required" }
}
$stream = [IO.MemoryStream]::new()
$writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint32]0x31474446) # FDG1, package format 1
    $writer.Write([uint32]($files.Count + 1))
    foreach ($file in @($files) + @(Get-Item -LiteralPath $NetHost)) {
        $name = if ($file.FullName -eq (Get-Item -LiteralPath $NetHost).FullName) { 'nethost.dll' } else { $file.FullName.Substring($root.Length + 1).Replace('\','/') }
        $nameBytes = [Text.Encoding]::UTF8.GetBytes($name)
        $data = [IO.File]::ReadAllBytes($file.FullName)
        $writer.Write([uint32]$nameBytes.Length); $writer.Write($nameBytes)
        $writer.Write([uint32]$data.Length); $writer.Write($data)
    }
    $writer.Flush()
    $bytes = $stream.ToArray()
    $payload = Join-Path $out 'dotnet-ladx.payload'
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $changed = !(Test-Path -LiteralPath $payload)
        if (!$changed) { $changed = [Convert]::ToBase64String($sha.ComputeHash($bytes)) -ne [Convert]::ToBase64String($sha.ComputeHash([IO.File]::ReadAllBytes($payload))) }
        if ($changed) { [IO.File]::WriteAllBytes($payload, $bytes) }
    } finally { $sha.Dispose() }
    $rc = '101 RCDATA "' + $payload.Replace('\','\\') + '"' + "`r`n"
    $rcPath = Join-Path $out 'dotnet-ladx.rc'
    if (!(Test-Path -LiteralPath $rcPath) -or [IO.File]::ReadAllText($rcPath) -ne $rc) { [IO.File]::WriteAllText($rcPath, $rc, [Text.Encoding]::Unicode) }
} finally { $writer.Dispose(); $stream.Dispose() }
