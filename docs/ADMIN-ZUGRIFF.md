# Admin-Zugang / Dump ändern

## Gibt es eine Tastenkombination?

**Keine eigene FrischStart-F-Taste.** Unter modernem UEFI/Windows ist klassisches F8 oft deaktiviert.

### Wenn im Assistenten ein Boot-Menü gewählt wurde (3 / 5 / 10 Sek.)

1. PC einschalten  
2. Kurz erscheint das **Windows-Startmenü**  
3. Mit **Pfeiltasten** „**FrischStart Wartung**“ wählen → **Enter**  

Dort bootet die **Parent-VHD** (`golden.vhdx`). Änderungen bleiben (neuer Dump).  
Danach Diffs erneuern bzw. einmal neu starten, damit der Schutz wieder greift.

### Wenn „Nur geschützt (automatisch)“ gewählt wurde (Timeout 0)

Kein Menü beim Start. Alternativen:

1. **Shift** gedrückt halten und auf **Neu starten** klicken  
   → Problembehandlung → Erweiterte Optionen → Starreinstellungen / anderes System  
2. In der App FrischStart (als Admin) Timeout/Menü später ändern (geplant)  
3. Einmalig per `bcdedit /timeout 5` wieder Menü aktivieren  

## Was ist was?

| Eintrag | Bedeutung |
|---------|-----------|
| FrischStart (geschützt) | Alltag / Rentner — Reset bei Neustart |
| FrischStart Wartung | Parent bearbeiten — Updates, Programme dauerhaft |
| Windows Notfall (vorher) | Kopie des früheren Boot-Eintrags (falls angelegt) |

## Nach der Wartung

1. Gewünschte Änderungen in **Wartung** machen  
2. Herunterfahren  
3. Beim nächsten Mal wieder **geschützt** starten (oder Standard im Assistenten)  
4. Diffs werden beim geschützten Betrieb wieder verworfen  

Empfehlung für Familien-PCs: **Menü 5 Sekunden** — Rentner trifft meist Enter/Timeout auf „geschützt“, Angehörige können Wartung wählen.
