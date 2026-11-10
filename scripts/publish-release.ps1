# Erzeugt ein vollständiges Release-Paket unter .\dist\FrischStart-<version>\
# Enthält: Self-Contained FrischStart.exe (.NET inklusive), Docs, Installer
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = "0.2.3"
$rid = "win-x64"
$outApp = Join-Path $root "dist\publish\$rid"
$stage = Join-Path $root "dist\FrischStart-$version"
$zip = Join-Path $root "dist\FrischStart-$version-win-x64.zip"

Write-Host "== FrischStart Release $version ==" -ForegroundColor Cyan

if (Test-Path (Join-Path $root "dist\publish")) {
    Remove-Item -Recurse -Force (Join-Path $root "dist\publish")
}
if (Test-Path $stage) {
    Remove-Item -Recurse -Force $stage
}

Write-Host "Publishing self-contained single-file..."
dotnet publish (Join-Path $root "src\FrischStart.App\FrischStart.App.csproj") `
    -c Release `
    -r $rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$version `
    -o $outApp

New-Item -ItemType Directory -Force -Path $stage | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $stage "app") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $stage "docs") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $stage "installer") | Out-Null

Copy-Item (Join-Path $outApp "FrischStart.exe") (Join-Path $stage "app\FrischStart.exe") -Force
Copy-Item (Join-Path $root "docs\*") (Join-Path $stage "docs\") -Force
Copy-Item (Join-Path $root "LICENSE") (Join-Path $stage "LICENSE") -Force
Copy-Item (Join-Path $root "NOTICE") (Join-Path $stage "NOTICE") -Force
Copy-Item (Join-Path $root "README.md") (Join-Path $stage "README.md") -Force
Copy-Item (Join-Path $root "scripts\Install-FrischStart.ps1") (Join-Path $stage "installer\") -Force
Copy-Item (Join-Path $root "scripts\Uninstall-FrischStart.ps1") (Join-Path $stage "installer\") -Force
Copy-Item (Join-Path $root "scripts\Start-Installation.bat") (Join-Path $stage "Start-Installation.bat") -Force
Copy-Item (Join-Path $root "setup\LESEN.txt") (Join-Path $stage "LESEN.txt") -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stage -DestinationPath $zip -Force

Write-Host ""
Write-Host "Fertig:" -ForegroundColor Green
Write-Host "  Ordner: $stage"
Write-Host "  ZIP:    $zip"
Write-Host ""
Write-Host "Auf dem Ziel-PC: ZIP entpacken → Rechtsklick Start-Installation.bat → als Admin."
