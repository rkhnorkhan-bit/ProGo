# ProGo planned update sequence

This is historical release history. **0.2.0 supersedes the 0.1.x updater transport**:
only installed updater files execute; version-specific release packages require the
GitHub SHA-256 digest. In-memory script evaluation and source-build fallback have
been removed. No antivirus trust or detection clearance is claimed.

## Rules

- Do not change vault format, encryption, KDF, PIN behavior, or stored secrets without an explicit owner decision.
- Keep updates transactional: backup first, then staging, then validation, then main replacement.
- Keep user-visible failures actionable: every update failure must expose logs without forcing the user to hunt for files.
- Avoid shipping bootstrap-only scripts inside the installed runtime package.
- Prefer small release steps with GitHub Actions green before recommending an update.

## 0.1.12 — Backup UX / manifest / retention

Status: done.

Scope:

- Rich backup manifest metadata.
- Rollback picker with version, target version, type, status, reason, and creation time.
- Backup retention action in tray menu.
- Retention policy:
  - keep manual backups;
  - keep latest baseline;
  - keep latest pre-update backup;
  - keep up to 10 latest automatic backups;
  - delete only older automatic backups.

Out of scope:

- Vault format changes.
- Updater transport changes.

## 0.1.13 — Release-package updater bridge

Status: done.

Scope:

- Prefer a prepared release package over source-build updates.
- Default release package URL: GitHub Releases `latest/download/ProGo-release.zip`.
- Verify package contents and version before staging.
- Keep source-build updater as fallback until a stable GitHub Release asset is published.
- Add CI artifact `ProGo-release-zip` so the package format is produced on every green build.

Out of scope:

- Mandatory code signing.
- Removing source-build fallback before a release asset exists.

## 0.1.14 — SSH profile manager hardening

Status: done.

Scope:

- Add explicit `Проверить SSH-профиль` action.
- Show whether the selected SSH profile exists in `~/.ssh/config` or is a direct `user@host` target.
- Resolve the profile with `ssh.exe -G <target>` without connecting to the server.
- Show resolved `hostname`, `user`, and `identityfile`.
- Improve automatic profile switching diagnostics in a later pass if runtime connection failures remain unclear.

## 0.1.15 — Installer / startup / recovery polish

Status: done.

Scope:

- Add explicit app start helper: `Start-ProGo.ps1`.
- Add repair action for missing scripts/folders/runtime files: `Repair-ProGo.ps1`.
- Add Start Menu shortcuts through the installer.
- Remove Start Menu shortcuts through the uninstaller.
- Keep logs and backup actions accessible from normal UI.

Out of scope:

- Installer MSI/MSIX.
- Code signing.
- Changing vault format, encryption, KDF, PIN behavior, or stored secrets.


## 0.1.16 — Updater launch recovery

Status: done.

Scope:

- Stop using ShellExecute to launch Windows PowerShell from the application.
- Log the updater child PID and native Win32 launch error codes.
- Keep `Update-ProGo.ps1` small enough to act as a compatibility bootstrap for already-installed builds.
- Move transactional update logic into `Update-ProGo.Core.ps1`.
- Ship, repair, stage, and validate both updater scripts.
- Preserve transactional backup, validation, rollback, vault format, encryption, KDF, PIN behavior, and stored secrets.


## 0.1.17 — Updater failure recovery

Status: done.

Scope:

- Execute the downloaded updater core in memory to avoid a second downloaded script execution boundary.
- Relaunch the installed ProGo automatically if bootstrap/core startup fails.
- Keep the current application open when the updater child exits before handoff.
- Log recovery, early-exit, and handoff events.
- Preserve transactional backup, validation, rollback, vault format, encryption, KDF, PIN behavior, and stored secrets.


## 0.1.18 — Antivirus-safe updater transport

Status: done.

Scope:

- Avoid direct PowerShell access to raw GitHub script/version URLs.
- Fetch updater core through GitHub Contents API as JSON/base64 and decode it in memory.
- Use the GitHub source archive as fallback transport.
- Fetch remote VERSION through GitHub Contents API.
- Preserve automatic recovery and the transactional update pipeline.


## 0.1.19 — Updater transaction state scope fix

Status: done.

Scope:

- Remove `$script:` mutable updater state that breaks under in-memory ScriptBlock execution.
- Keep remote/local version, update mode, and main-change state in one shared object.
- Ensure post-install version verification uses the same remote version value produced by the version check.
- Ensure rollback eligibility reflects whether the main application directory was modified.


## 0.1.20 — Connection metrics in status

Status: done.

Scope:

- Show continuous SOCKS-route latency in the Status window.
- Poll latency every 2 seconds without overlapping measurements.
- Add an explicit `Измерить скорость` action.
- Run a download throughput test only on user request.
- Show the latest speed result in Mbit/s.
- Rename the proxy row to `Прокси окружения`.


## 0.1.21 — Updater state log formatting

Status: done.

Scope:

- Correct PowerShell interpolation of shared updater state in logs and manifests.
- Log actual local/remote versions.
- Persist actual target version and update mode in backup manifests.
- Keep transaction behavior unchanged.


## 0.1.22 — Published release pipeline and update UX

Status: done.

Scope:

- Publish `ProGo-release.zip` as a real GitHub Release asset for every version change on `main`.
- Tag releases as `vX.Y.Z`.
- Use the latest published GitHub Release as the source of update availability.
- Show an informational dialog when the installed version is current.
- Show current/available versions and ask for confirmation when an update exists.
- Keep source-build fallback for recovery.


## 0.1.23 — Release publishing reliability

Status: done.

Scope:

- Make the release-existence check non-terminating on Windows PowerShell 5.1.
- Allow first-time release creation to proceed when no prior release exists.
- Verify the published `ProGo-release.zip` asset after creation.
