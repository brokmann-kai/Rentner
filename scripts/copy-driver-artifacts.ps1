# Kopiert gebaute SysRestoreDriver-Artefakte nach .\drivers\
param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$vendor = Join-Path $root "vendor\SysRestoreDriver"
$dest = Join-Path $root "drivers"

$candidates = @(
    Join-Path $vendor "$Platform\$Configuration\SysRestoreDriver.sys",
    Join-Path $vendor "SysRestoreDriver\$Platform\$Configuration\SysRestoreDriver.sys",
    Join-Path $vendor "QHEngineUI\$Platform\$Configuration\SysRestoreDriver.sys"
)

$sys = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $sys) {
    Write-Host "Kein SysRestoreDriver.sys gefunden. Bitte zuerst die Upstream-Solution in Visual Studio bauen."
    Write-Host "Erwartete Pfade z.B.:"
    $candidates | ForEach-Object { Write-Host "  $_" }
    exit 1
}

$infSrc = Join-Path $vendor "SysRestoreDriver\SysRestoreDriver.inf"
if (-not (Test-Path $infSrc)) {
    Write-Error "INF fehlt: $infSrc"
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item $sys (Join-Path $dest "SysRestoreDriver.sys") -Force
Copy-Item $infSrc (Join-Path $dest "SysRestoreDriver.inf") -Force
Write-Host "Kopiert nach $dest"
Get-ChildItem $dest | Format-Table Name, Length
