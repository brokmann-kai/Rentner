# Deinstalliert FrischStart (App + Verknüpfungen). VHD-Daten bleiben unberührt.
[CmdletBinding()]
param(
    [string]$InstallDir = "$env:ProgramFiles\FrischStart"
)

$ErrorActionPreference = "Stop"

$id = [Security.Principal.WindowsIdentity]::GetCurrent()
$p = New-Object Security.Principal.WindowsPrincipal($id)
if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Bitte als Administrator starten."
}

Write-Host "Deinstalliere FrischStart..." -ForegroundColor Cyan

$programs = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\FrischStart"
if (Test-Path $programs) { Remove-Item -Recurse -Force $programs }
$desktop = Join-Path $env:PUBLIC "Desktop\FrischStart.lnk"
if (Test-Path $desktop) { Remove-Item -Force $desktop }

$regPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FrischStart"
if (Test-Path $regPath) { Remove-Item -Recurse -Force $regPath }

if (Test-Path $InstallDir) {
    Remove-Item -Recurse -Force $InstallDir
}

Write-Host "FrischStart wurde entfernt." -ForegroundColor Green
Write-Host "Hinweis: VHD-Ordner (z.B. D:\FrischStart) und Boot-Einträge bleiben bestehen."
Write-Host "Boot-Einträge ggf. manuell mit bcdedit bereinigen."
