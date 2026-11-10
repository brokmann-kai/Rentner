# Changelog (Deutsch)

## [0.2.2] — 2026-03-10

### Hinzugefügt
- Assistent-Schritt **Startreihenfolge** (Timeout 0/3/5/10, Wartung als Standard).
- Boot-Eintrag **FrischStart Wartung** (Parent/Dump) + Doku `ADMIN-ZUGRIFF.md`.

## [0.2.1] — 2026-03-10

### Hinzugefügt
- **Installations-Assistent** mit zwei Wegen:
  - bestehendes Windows übernehmen (VSS-Klon)
  - neue Windows-Installation in VHD
- Button „Installation starten“ in der Haupt-UI.

## [0.2.0] — 2026-03-10

### Hinzugefügt
- **Eigene VHD-Engine** (Differencing-VHD / Dual-Slot-Reset) als Hauptweg.
- Geplante Aufgabe + Worker-Skript für Reset bei jedem Start.
- UI auf VHD-Schutz umgestellt (kein Testmodus nötig).
- Docs: `ARCHITEKTUR.md`, `VHD-SETUP.md`.

### Geändert
- SysRestoreDriver nur noch experimentell / optional.

## [0.1.0] — 2026-03-10

### Hinzugefügt
- Projektgerüst **FrischStart** (Neustart-Schutz für Senioren-PCs).
- Upstream [SysRestoreDriver](https://github.com/qhencoder/SysRestoreDriver) unter `vendor/`.
- WPF-Einrichtungsassistent (`FrischStart.App`) und Treiber-Brücke (`FrischStart.Core`).
- Angehörigen-Dokumentation unter `docs/`.
