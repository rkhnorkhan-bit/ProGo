# Changelog

## 0.1.23 - Release publishing reliability

- Fixed the GitHub Release workflow so a missing release is detected without Windows PowerShell 5.1 treating `gh release view` stderr as a terminating error.
- Release publishing now proceeds to create the version tag and upload `ProGo-release.zip` on the first release for a version.
- Kept update availability UX, release-based version checks, source-build fallback, connection metrics, transaction safety, and vault behavior unchanged.

## 0.1.22 - Published releases and update UX

- Added an automatic GitHub Release workflow triggered when `VERSION` changes on `main`.
- Release workflow builds/tests ProGo, creates tag `vX.Y.Z`, publishes a GitHub Release, and uploads `ProGo-release.zip`.
- Updater version checks now use the latest published GitHub Release instead of `main/VERSION`, preventing update prompts before a ready package exists.
- Added in-app update availability check before closing ProGo.
- When no update is available, ProGo shows the installed version and confirms that it is current.
- When an update is available, ProGo shows current and available versions and asks for explicit confirmation before starting the updater.
- Kept source-build fallback for recovery if a release asset cannot be downloaded.
- Kept connection metrics, transaction safety, vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.21 - Updater state log formatting

- Fixed PowerShell string interpolation for updater shared-state properties.
- Version checks now log real local/remote version values instead of Hashtable property names.
- Backup manifests now persist real `target_version` and `update_mode` values.
- Version-mismatch diagnostics now include the actual remote version.
- Added regression tests against direct `$State.Property` interpolation inside updater strings.
- No changes to transaction semantics, connection metrics, vault format, encryption, KDF, PIN behavior, or stored secrets.

## 0.1.20 - Connection metrics in status

- Added continuous SOCKS-route latency measurement in the Status window.
- Latency is measured through the actual local SOCKS5 tunnel to the configured test endpoint, including remote DNS/TCP route establishment.
- Added on-demand `Измерить скорость` button.
- Speed test measures download throughput through SOCKS using a 10 MB Cloudflare test payload and reports Mbit/s.
- Renamed the status row from `Системный прокси` to `Прокси окружения` to reflect the actual environment-variable proxy configuration.
- Ping polling runs every 2 seconds without overlapping measurements; speed tests never run automatically.
- Kept updater, vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.19 - Updater transaction state scope fix

- Replaced updater `$script:` mutable variables with one shared state object so in-memory ScriptBlock execution keeps a single transaction state.
- Fixed remote version persistence after the version check.
- Fixed `MainWasChanged` tracking so rollback decisions reflect whether the main install was actually modified.
- Added CI guards against reintroducing script-scoped updater state.
- Kept GitHub API transport, recovery, backup, staging, self-check, rollback, vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.18 - Antivirus-safe updater transport

- Replaced direct PowerShell downloads from `raw.githubusercontent.com` for updater core/version with GitHub API content endpoints.
- Updater core is received as GitHub JSON/base64 and decoded in memory.
- Added source archive transport as a fallback if GitHub API access fails.
- Kept automatic ProGo recovery if all updater transports fail.
- Kept transactional backup, staging, self-check, rollback, vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.17 - Updater failure recovery

- Run the downloaded transactional updater core in memory instead of launching a second downloaded PowerShell script from disk.
- On bootstrap failure, wait for the old ProGo process to exit and relaunch the installed ProGo executable automatically.
- The application now verifies updater handoff for 1.2 seconds and stays open if the updater process exits immediately.
- Added recovery/handoff coverage to Windows CI.
- Kept transactional backup, staging, self-check, rollback, vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

## 0.1.16 - Updater launch recovery

- Changed the in-app updater launcher to start Windows PowerShell directly with `UseShellExecute = false` and `CreateNoWindow = true`.
- Added Win32 native error-code and updater PID logging around updater process startup.
- Split updater delivery into a lightweight `Update-ProGo.ps1` bootstrap and `Update-ProGo.Core.ps1` transactional core so older installed builds can refresh a smaller compatibility script before self-update.
- Installer, repair helper, release package, staging validation, and tests now include the updater core.
- Kept backup/staging/self-check/rollback behavior and user vault data unchanged.

## 0.1.15 - Startup and recovery polish

- Added `scripts/Start-ProGo.ps1` for normal launch without manually locating `ProGo.exe`.
- Added `scripts/Repair-ProGo.ps1` to recreate the installed folder structure, scripts folder, backups folder, and copy available runtime files back into `%LOCALAPPDATA%\ProGo`.
- Installer now deploys start/repair helpers.
- Installer now creates Start Menu shortcuts:
  - `ProGo`
  - `ProGo Status`
- Uninstaller now removes Start Menu shortcuts as well as startup shortcut.
- Release package now includes `Start-ProGo.ps1` and `Repair-ProGo.ps1`.
- Kept vault format, encryption, KDF, PIN behavior, and stored secrets unchanged.

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
