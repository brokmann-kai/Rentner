# FrischStart

Open-Source-Neustart-Schutz für Windows — für Angehörige, die den PC von
Eltern/Großeltern absichern wollen. **Nach jedem Neustart ist das System wieder sauber.**

## Kernidee (ohne Zertifikat, ohne Testmodus)

Differencing-VHDs + Assistent — Details: [docs/ARCHITEKTUR.md](docs/ARCHITEKTUR.md)

## Vollständige Installation (für Ziel-PCs)

### Release bauen (Entwickler-PC mit .NET 8 SDK)

```powershell
.\scripts\publish-release.ps1
```

Erzeugt:

- `dist\FrischStart-0.2.3\` — fertiges Paket
- `dist\FrischStart-0.2.3-win-x64.zip`

### Auf dem Senioren-PC installieren

1. ZIP entpacken  
2. Rechtsklick **`Start-Installation.bat`** → **Als Administrator**  
3. FrischStart startet → **„Installation starten (Assistent)“**

Enthalten / mitgeliefert:

| Komponente | Status |
|------------|--------|
| FrischStart.exe | Self-Contained inkl. **.NET 8** |
| Anleitungen | `docs\` |
| Installer / Uninstaller | ja |
| diskpart, bcdedit, robocopy, schtasks | Windows-Bordmittel (werden geprüft) |
| Windows-ISO / Installstick | **nicht** enthalten (nur bei Neuinstallations-Weg nötig) |

Optional mit [Inno Setup](https://jrsoftware.org/isinfo.php): `setup\FrischStart.iss` → `FrischStart-*-Setup.exe`

## Entwicklung

```powershell
dotnet build src\FrischStart.sln -c Release
```

## Lizenz

Apache License 2.0 — siehe `LICENSE` und `NOTICE`.
