; Optional: Inno Setup 6 — erzeugt Setup.exe aus dem publish-Paket
; Zuerst:  .\scripts\publish-release.ps1
; Dann:    Inno Setup Compiler → diese Datei öffnen → Compile
#define MyAppName "FrischStart"
#define MyAppVersion "0.2.3"
#define MyAppPublisher "FrischStart Open Source"
#define MyAppExeName "FrischStart.exe"
#define StageDir "..\dist\FrischStart-0.2.3"

[Setup]
AppId={{A7C3E2F1-9B4D-4E8A-9C21-8F0E1D2C3B4A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\FrischStart
DefaultGroupName=FrischStart
DisableProgramGroupPage=yes
LicenseFile={#StageDir}\LICENSE
OutputDir=..\dist
OutputBaseFilename=FrischStart-{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung anlegen"; GroupDescription: "Zusätzlich:"

[Files]
Source: "{#StageDir}\app\FrischStart.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageDir}\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs
Source: "{#StageDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageDir}\NOTICE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageDir}\LESEN.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageDir}\installer\Uninstall-FrischStart.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FrischStart"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\FrischStart"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "FrischStart jetzt starten"; Flags: nowait postinstall skipifsilent shellexec
