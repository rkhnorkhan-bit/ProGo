# Changelog

## 0.1.14 - SSH profile diagnostics

- Added `Проверить SSH-профиль` action in settings.
- Added `SshProfileDiagnostics`:
  - detects direct `user@host` targets;
  - checks whether an alias is present in `%USERPROFILE%\.ssh\config`;
  - runs `ssh.exe -G <target>` without connecting to the server;
  - reports resolved `hostname`, `user`, and `identityfile`.
- Added visible SSH config path in settings.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.13 - Release-package updater bridge

- Added release-package update mode:
  - updater first tries `ReleasePackageUrl`;
  - default package URL is `https://github.com/rkhnorkhan-bit/ProGo/releases/latest/download/ProGo-release.zip`;
  - package must contain `ProGo.exe`, `VERSION`, and `scripts/Update-ProGo.ps1`;
  - package version must match remote `VERSION`;
  - accepted package is still installed through the transactional staging/self-check/main-commit flow.
- Kept `source-build-fallback` mode when no release package exists yet.
- Added `update_mode=release-package` and `update_mode=source-build-fallback` logging in `update.log`.
- Added CI packaging step for `ProGo-release.zip` artifact.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.12 - Backup UX, manifest, and retention

- Added richer backup manifest metadata:
  - `target_version`;
  - `created_by`;
  - `update_result`;
  - `backup_kind`.
- Improved rollback picker display with version, target version, backup kind, update result, reason, and creation time.
- Added tray action `Удалить старые резервные копии...`.
- Added backup retention behavior:
  - manual backups are preserved;
  - latest baseline is preserved;
  - latest pre-update backup is preserved;
  - up to 10 latest automatic backups are preserved;
  - only older automatic backups are deleted.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.11 - Runtime release without bootstrap installer

- Removed `Install-FromGitHub.ps1` from runtime `release/scripts` and installed `%LOCALAPPDATA%\ProGo\scripts`.
- Kept `Install-FromGitHub.ps1` in the repository as a first-time bootstrap helper only.
- Fixed self-update builds that could fail when endpoint protection blocks the downloaded bootstrap installer script in the temporary source tree.
- Added CI coverage to ensure the bootstrap installer is not included in runtime release scripts.
- Kept transactional updater behavior from `0.1.9` and log-access UX from `0.1.10`.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.10 - Update log access UX

- Added updater result dialog actions:
  - `Открыть update.log`
  - `Скопировать log`
  - `Открыть папку ProGo`
- Added ProGo tray menu section `Открыть логи` with direct access to:
  - `progo.log`
  - `update.log`
  - legacy `progo-update.log`
  - the `%LOCALAPPDATA%\ProGo` folder.
- Kept transactional updater behavior from `0.1.9`.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.9 - Transactional updater

- Reworked updater into a transactional flow:
  - create installed-state backup before any update work;
  - create an intermediate staging copy;
  - download and build the new version in a temporary workspace;
  - apply the new version to staging first;
  - validate staging with `ProGo.exe --self-check`;
  - update the main application only after staging passes;
  - rollback from backup if a post-commit failure occurs;
  - write the full process to `update.log` and legacy `progo-update.log`;
  - clean temporary update files in `finally`.
- Added `ProGo.exe --self-check` for non-interactive updater validation.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

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
