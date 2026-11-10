# Changelog (English)

## [0.2.3] — 2026-03-10

### Added
- **Full install package**: self-contained publish, `Start-Installation.bat`,
  install/uninstall scripts, optional Inno Setup (`setup/FrischStart.iss`).

## [0.2.2] — 2026-03-10

### Added
- Wizard step for **boot order** (timeout 0/3/5/10, maintenance default).
- Boot entry **FrischStart Wartung** (parent/dump) and `ADMIN-ZUGRIFF.md`.

## [0.2.1] — 2026-03-10

### Added
- **Setup wizard** with two paths:
  - take over existing Windows (VSS clone)
  - fresh Windows install into VHD
- Main UI button “Installation starten”.

## [0.2.0] — 2026-03-10

### Added
- **Native differencing-VHD engine** (dual-slot reset) as the primary protect path.
- Startup scheduled task + worker script.
- UI switched to VHD protect (no test-signing required).
- Docs: `ARCHITEKTUR.md`, `VHD-SETUP.md`.

### Changed
- SysRestoreDriver is experimental / optional only.

## [0.1.0] — 2026-03-10

### Added
- Repository scaffold for **FrischStart** (senior-friendly reboot restore).
- Vendored upstream [SysRestoreDriver](https://github.com/qhencoder/SysRestoreDriver) under `vendor/`.
- WPF setup assistant (`FrischStart.App`) and driver bridge library (`FrischStart.Core`).
- German caregiver documentation under `docs/`.
