# FrischStart VHD-Setup (Angehörige)

## Empfohlen: Assistent in der App

1. `FrischStart.exe` **als Administrator** starten  
2. **„Installation starten (Assistent)“**  
3. Weg wählen:
   - **Bestehendes Windows übernehmen** (kein Neuaufsetzen, Klon per Schattenkopie)
   - **Neue Windows-Installation in VHD** (Installationsstick nötig)
4. Speicherort mit genug Platz wählen  
5. **Startreihenfolge** wählen (automatisch / Menü 3–10 s / Wartung)  
6. Assistenten zu Ende führen → neu starten  

Admin/Dump ändern: siehe [ADMIN-ZUGRIFF.md](ADMIN-ZUGRIFF.md) (Boot-Menü → „FrischStart Wartung“).

## Ziel

Windows läuft aus einer VHD. Nach jedem Neustart sind Änderungen weg.

## Voraussetzungen

- Windows 10/11 (Pro reicht)
- Genug Platz: ca. **2×** die Größe der Systeminstallation frei
  (Parent + Diffs), ideal auf einer schnellen internen SSD
- Externe HDD für Fotos/Dokumente (bleibt ungeschützt)
- Backup vorher!

## Manuell (falls ohne Assistent)

1. Sauberes Windows als **Parent** `golden.vhdx` bereitstellen  
   (Neuinstallation in VHD, oder Übernahme)
2. Zwei Differencing-VHDs anlegen: `diff-a.vhdx`, `diff-b.vhdx`
3. Von Diff-A booten
4. In FrischStart **„VHD-Schutz aktivieren“** → geplante Aufgabe setzt Diffs zurück

## Parent erzeugen (Variante Neuinstallation)

1. Auf D: Ordner `D:\FrischStart\` anlegen
2. Leere VHD erzeugen (Diskverwaltung oder `diskpart`):

```
create vdisk file=D:\FrischStart\golden.vhdx maximum=80000 type=expandable
attach vdisk
create partition primary
format fs=ntfs quick label=FrischStart
assign letter=V
exit
```

3. Windows-Setup von USB starten, auf Laufwerk `V:` installieren
4. Windows einrichten (Browser, Office-Viewer, …), **keine** privaten Fotos auf C:
5. Herunterfahren, VHD trennen
6. Diffs erzeugen:

```
create vdisk file=D:\FrischStart\diff-a.vhdx parent=D:\FrischStart\golden.vhdx
create vdisk file=D:\FrischStart\diff-b.vhdx parent=D:\FrischStart\golden.vhdx
```

7. Boot-Eintrag für Diff-A setzen (`bcdboot` von gemounteter Diff-A)
8. FrischStart starten → Schutz aktivieren

## Wartung (Updates dauerhaft machen)

1. In FrischStart Schutz **aus**
2. In die Parent-VHD booten bzw. Diff mergen (Wartungsmodus)
3. Updates installieren
4. Wieder Diffs frisch erzeugen und Schutz an

## Senior-Regel

> Neu starten = wieder sauber.  
> Wichtige Dateien nur auf die externe Platte.
