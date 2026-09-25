# Changelog

## 0.1.8 - SSH profile management

- Added SSH profile selector in settings.
- Added local profile list stored in `settings.json`.
- Added `+`, `-`, and `?` profile actions for adding, deleting, and editing the selected profile.
- Added in-app explanation of what an SSH profile is and why ProGo needs it.
- Added automatic SSH profile switching when the selected profile cannot bring the SOCKS tunnel up.
- Preserved backward compatibility with the old `SshProfile` text field: existing values are migrated into the profile list automatically.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.7 - Visible launch fallback

- Added `ProGo.exe --show` to open the status window even when the tray icon is hidden.
- Added `scripts/Show-ProGo.ps1` visible launcher.

## 0.1.6 - Startup diagnostics

- Added startup crash guard and fatal logging.
- Added installer-created backups folder.

## 0.1.5 - Backup and rollback safety

- Added `VERSION`-based update checks.
- Added encoding-safe updater messages for Windows PowerShell 5.1.
- Added full installed-state backups before real version changes.
- Added startup baseline backup: one backup per installed version when none exists yet.
- Added tray actions:
  - `Создать резервную копию`
  - `Откатить из резервной копии...`
  - `Открыть папку резервных копий`
- Added restore script `scripts/Restore-ProGoBackup.ps1`.
- Backups now may include `ProGo.exe`, `ProGo.ico`, `VERSION`, `scripts`, `vault.enc.json`, `settings.json`, and logs.
- Kept vault format, encryption, KDF, and PIN behavior unchanged.

## 0.1.4 - Safer updater

- Added `VERSION` file.
- Added update check before downloading source archive.
- Added backup attempt before updates.
- Reduced aggressive process handling in updater.
- Improved Windows PowerShell 5.1 parser compatibility.

## 0.1.0 - Bootstrap MVP

- Initialized clean ProGo repository.
- Added Windows WinForms tray application.
- Added generated ProGo brand icon for tray and executable metadata.
- Added configurable SSH SOCKS controller.
- Added user-level proxy environment management.
- Added local encrypted vault MVP.
- Added clipboard auto-clear.
- Added Russian UI.
- Added direct install from GitHub through `scripts/Install-FromGitHub.ps1`.
- Added tray update action `Обновить ProGo`.
- Added self-updater script `scripts/Update-ProGo.ps1` that downloads latest `main`, rebuilds locally, preserves user data, and restarts ProGo.
- Added build, install, update, uninstall, and test scripts.
- Added repository hygiene and Windows build CI.
- Added source-available license notice, security policy, and documentation.
