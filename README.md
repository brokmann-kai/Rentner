# FrischStart

Einfacher Neustart-Schutz für Windows — gedacht für **Angehörige**, die den PC
von Eltern/Großeltern absichern wollen. Nach jedem Neustart ist das System
wieder im sauberen Zustand. Viren und unerwünschte Installationen verschwinden.

**Kerntechnik:** [SysRestoreDriver](https://github.com/qhencoder/SysRestoreDriver)
(Apache-2.0) — Volume-Filter mit Copy-on-Write / Restart-Restore.

## Für wen?

| Rolle | Aufgabe |
|-------|---------|
| **Angehörige** | Einmalig einrichten (Assistent, ~10 Minuten) |
| **Seniorin / Senior** | Bei Problemen: PC neu starten — fertig |

## Prinzip

1. Systemplatte (meist `C:`) wird geschützt.
2. Fotos, Briefe, Downloads liegen auf einer **ungeschützten** Platte (z. B. externe HDD).
3. Online-Spiele (Facebook o. Ä.) speichern Fortschritt in der Cloud — bleiben erhalten.
4. Nach dem Neustart: Windows wieder „frisch“.

## Repo-Struktur

```
windows-neu/
  src/FrischStart.App/     ← deutsche WPF-Oberfläche (Assistent)
  src/FrischStart.Core/    ← Treiber-Anbindung (Install, Schutz an/aus)
  vendor/SysRestoreDriver/ ← Upstream-Treiber + Original-UI
  drivers/                 ← gebaute .sys/.inf hier ablegen
  docs/                    ← Anleitungen
```

## Status

Früher Prototyp. Treiber muss mit Visual Studio + WDK gebaut werden.
**Nur in einer VM / auf einem Test-PC ausprobieren** — nie zuerst auf dem einzigen Familien-PC ohne Backup.

## Voraussetzungen (Entwicklung)

- Windows 10/11, NTFS
- Visual Studio 2022 + Windows SDK + WDK (für den Treiber)
- .NET 8 SDK (für FrischStart-UI)
- Testmodus oder signierter Kernel-Treiber (`bcdedit /set testsigning on`)

## Schnellstart (Entwicklung)

```powershell
# 1. SysRestoreDriver in VS öffnen und Release|x64 bauen
# 2. SysRestoreDriver.sys + .inf nach .\drivers\ kopieren
# 3. UI bauen
dotnet build src\FrischStart.sln -c Release
# 4. Als Administrator starten
```

## Sicherheitshinweise

- Kernel-Treiber — Bluescreens und Datenverlust sind möglich.
- Schutz aus = immer erst im Assistenten deaktivieren, dann neu starten.
- Nicht bei aktivem Schutz hart ausschalten.
- BitLocker vor Tests abschalten bzw. separat prüfen.
- Upstream-Warnungen in `vendor/SysRestoreDriver/README_EN.md` beachten.

## Lizenz

FrischStart-Code: Apache License 2.0.  
Upstream SysRestoreDriver: Apache License 2.0 (Copyright Xuhui Jiang) — siehe `NOTICE`.
