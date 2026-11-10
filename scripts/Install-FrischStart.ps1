# Vollständige Installation von FrischStart (Self-Contained, ohne extra .NET-Installer)
# Aufruf (Admin): powershell -ExecutionPolicy Bypass -File Install-FrischStart.ps1
#Optional: -PackageRoot "C:\Pfad\zum\FrischStart-0.2.2"
[CmdletBinding()]
param(
    [string]$PackageRoot = "",
    [string]$InstallDir = "$env:ProgramFiles\FrischStart",
    [switch]$LaunchWizard
)

$ErrorActionPreference = "Stop"

function Assert-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Bitte als Administrator starten."
    }
}

function Test-Prereqs {
    $os = [Environment]::OSVersion.Version
    if ($os.Major -lt 10) {
        throw "Windows 10 oder 11 erforderlich."
    }
    foreach ($tool in @("diskpart.exe", "bcdedit.exe", "robocopy.exe", "schtasks.exe", "powershell.exe")) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "Benötigtes Windows-Programm fehlt: $tool"
        }
    }
}

Assert-Admin
Test-Prereqs

if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    # installer\ liegt eine Ebene unter Paketwurzel, oder Skript liegt in scripts\
    $here = Split-Path -Parent $MyInvocation.MyCommand.Path
    if (Test-Path (Join-Path $here "..\app\FrischStart.exe")) {
        $PackageRoot = Resolve-Path (Join-Path $here "..")
    }
    elseif (Test-Path (Join-Path $here "..\dist")) {
        # Dev: neuestes dist-Paket suchen
        $cand = Get-ChildItem (Join-Path $here "..\dist") -Directory -Filter "FrischStart-*" |
            Sort-Object Name -Descending |
            Select-Object -First 1
        if ($cand) { $PackageRoot = $cand.FullName }
    }
}

$exeSrc = Join-Path $PackageRoot "app\FrischStart.exe"
if (-not (Test-Path $exeSrc)) {
    throw "FrischStart.exe nicht gefunden. PackageRoot=$PackageRoot"
}

Write-Host "Installiere nach $InstallDir ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $InstallDir "docs") | Out-Null

Copy-Item $exeSrc (Join-Path $InstallDir "FrischStart.exe") -Force
if (Test-Path (Join-Path $PackageRoot "docs")) {
    Copy-Item (Join-Path $PackageRoot "docs\*") (Join-Path $InstallDir "docs\") -Force
}
foreach ($f in @("LICENSE", "NOTICE", "README.md", "LESEN.txt")) {
    $p = Join-Path $PackageRoot $f
    if (Test-Path $p) { Copy-Item $p (Join-Path $InstallDir $f) -Force }
}

# Startmenü + Desktop (All Users)
$programs = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\FrischStart"
New-Item -ItemType Directory -Force -Path $programs | Out-Null

$ws = New-Object -ComObject WScript.Shell
$exe = Join-Path $InstallDir "FrischStart.exe"

$sc1 = $ws.CreateShortcut((Join-Path $programs "FrischStart.lnk"))
$sc1.TargetPath = $exe
$sc1.WorkingDirectory = $InstallDir
$sc1.Description = "FrischStart — Neustart-Schutz"
$sc1.Save()

$sc2 = $ws.CreateShortcut((Join-Path $programs "FrischStart Installation (Assistent).lnk"))
$sc2.TargetPath = $exe
$sc2.WorkingDirectory = $InstallDir
$sc2.Description = "FrischStart Assistent"
$sc2.Save()

$desktop = Join-Path $env:PUBLIC "Desktop\FrischStart.lnk"
$sc3 = $ws.CreateShortcut($desktop)
$sc3.TargetPath = $exe
$sc3.WorkingDirectory = $InstallDir
$sc3.Save()

# Uninstall-Hilfsskript mitkopieren
$un = Join-Path $PackageRoot "installer\Uninstall-FrischStart.ps1"
if (Test-Path $un) {
    Copy-Item $un (Join-Path $InstallDir "Uninstall-FrischStart.ps1") -Force
}

# Einfacher Uninstall-Registry-Eintrag (ohne MSI)
$regPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FrischStart"
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty $regPath -Name "DisplayName" -Value "FrischStart"
Set-ItemProperty $regPath -Name "DisplayVersion" -Value "0.2.3"
Set-ItemProperty $regPath -Name "Publisher" -Value "FrischStart (Open Source)"
Set-ItemProperty $regPath -Name "InstallLocation" -Value $InstallDir
Set-ItemProperty $regPath -Name "DisplayIcon" -Value $exe
$uninstallCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$InstallDir\Uninstall-FrischStart.ps1`""
Set-ItemProperty $regPath -Name "UninstallString" -Value $uninstallCmd
Set-ItemProperty $regPath -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty $regPath -Name "NoRepair" -Value 1 -Type DWord

Write-Host ""
Write-Host "Installation fertig." -ForegroundColor Green
Write-Host "  App:      $exe"
Write-Host "  Startmenü: FrischStart"
Write-Host ""
Write-Host "Hinweis: Windows selbst und ein Installationsstick werden NICHT mitgeliefert."
Write-Host "Bereits vorhanden und geprüft: diskpart, bcdedit, robocopy, Aufgabenplanung."
Write-Host ""
Write-Host "Nächster Schritt: FrischStart als Admin starten → „Installation starten“."

if ($LaunchWizard) {
    Start-Process -FilePath $exe -Verb RunAs
}
