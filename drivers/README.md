# Treiberdateien

Hier müssen liegen:

- `SysRestoreDriver.sys` (gebaut aus `vendor/SysRestoreDriver`)
- `SysRestoreDriver.inf` (liegt bereits vor)

Nach dem Build in Visual Studio:

```powershell
.\scripts\copy-driver-artifacts.ps1
```
