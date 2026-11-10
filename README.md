# FrischStart

Open-Source-Neustart-Schutz für Windows — für Angehörige, die den PC von
Eltern/Großeltern absichern wollen. **Nach jedem Neustart ist das System wieder sauber.**

## Kernidee (ohne Zertifikat, ohne Testmodus)

Eigene Engine auf **Differencing-VHDs** (Steadier-State-Prinzip):

- Unveränderliche Parent-VHD (`golden.vhdx`)
- Zwei Diff-VHDs im Wechsel
- Beim Start wird die nächste Diff frisch erzeugt
- Windows-Bordmittel (`diskpart`, `bcdedit`) — **von Microsoft signiert**
- **Kein eigener Kernel-Treiber** → kein EV-Zertifikat, kein Wasserzeichen

Details: [docs/ARCHITEKTUR.md](docs/ARCHITEKTUR.md) · Setup: [docs/VHD-SETUP.md](docs/VHD-SETUP.md)

## Für wen?

| Rolle | Aufgabe |
|-------|---------|
| **Angehörige** | Einmal VHD-Setup + FrischStart aktivieren |
| **Seniorin / Senior** | Bei Problemen: PC neu starten |

Fotos/Dokumente → **externe Festplatte** (nicht in der System-VHD).

## Repo

```
src/FrischStart.App/      WPF-Assistent (Deutsch)
src/FrischStart.Core/     VHD-Engine + optional alter Treiber-Bridge
docs/                     Architektur & Setup
vendor/SysRestoreDriver/  nur experimentell (braucht Testmodus)
```

## Bauen

```powershell
dotnet build src\FrischStart.sln -c Release
# Start als Administrator:
.\src\FrischStart.App\bin\Release\net8.0-windows\FrischStart.exe
```

Voraussetzungen: .NET 8 SDK, Windows 10/11.  
**Kein Visual Studio / WDK** für den Hauptweg nötig.

## Status

VHD-Engine + UI sind im Prototyp. Zuerst in einer **VM** testen.
Erstes Setup der Golden-VHD ist der aufwendigste Schritt (danach einfach).

## Lizenz

Apache License 2.0 — siehe `LICENSE` und `NOTICE`.
