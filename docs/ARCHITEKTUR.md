# FrischStart — Architektur

## Warum kein eigener Kernel-Treiber als Hauptweg?

Unter Windows 10/11 (x64) laden **unsignierte** Kernel-Treiber nur im Testmodus
(Wasserzeichen) oder mit teurer EV-Signatur. Das widerspricht dem Ziel:
kostenlos, Open Source, alltagstauglich für Senioren-PCs.

## Hauptweg: Differencing-VHD (eigene Engine)

FrischStart orchestriert **von Microsoft signierte** Windows-Bordmittel:

1. Windows startet von einer **Differencing-VHD** (Kind), die auf eine
   unveränderliche **Parent-/Golden-VHD** zeigt.
2. Beim Start löscht FrischStart die *andere* Diff-VHD und erzeugt sie neu (leer).
3. Der nächste Boot zeigt auf diese frische Diff-VHD.
4. Ergebnis: **Jeder Neustart = jungfräulicher Zustand** der Golden-VHD.

Kein eigener `.sys`-Treiber → **kein EV, kein Testmodus-Wasserzeichen**.

```
[Golden Parent.vhdx]  (nur lesen / Wartung)
        ↑
   Diff-A.vhdx  ←→  Diff-B.vhdx   (abwechselnd, jede Session eine frische)
        ↑
   Windows bootet hier
```

Daten, die bleiben sollen: **externe Festplatte** (nicht in der VHD).

## Optional / Experimentell

`vendor/SysRestoreDriver` bleibt im Repo als experimenteller Kernel-Weg
(Testmodus nötig). Nicht der Standard für Familien-PCs.

## Open Source

FrischStart-Code: Apache-2.0.  
Nutzte Windows-APIs/`diskpart`/`bcdedit` — keine proprietäre Restore-Engine.
