# ProGo planned update sequence

This document tracks the short-term update chain agreed during the 0.1.x stabilization phase.

## Rules

- Do not change vault format, encryption, KDF, PIN behavior, or stored secrets without an explicit owner decision.
- Keep updates transactional: backup first, then staging, then validation, then main replacement.
- Keep user-visible failures actionable: every update failure must expose logs without forcing the user to hunt for files.
- Avoid shipping bootstrap-only scripts inside the installed runtime package.
- Prefer small release steps with GitHub Actions green before recommending an update.

## 0.1.12 — Backup UX / manifest / retention

Status: in progress.

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

## 0.1.13 — Release-package updater

Status: planned.

Scope:

- Stop building from GitHub source during end-user update.
- Download a prepared release package from GitHub Actions or GitHub Releases.
- Verify package version before staging.
- Keep source-build updater only as developer fallback.

## 0.1.14 — SSH profile manager hardening

Status: planned.

Scope:

- Add explicit `Проверить SSH-профиль` action.
- Show whether the selected SSH profile exists in `~/.ssh/config` or is a direct `user@host` target.
- Improve automatic profile switching diagnostics.
- Make SSH failures explain whether the issue is profile missing, key/permission, host unreachable, or port-forward failure.

## 0.1.15 — Installer / startup / recovery polish

Status: planned.

Scope:

- Add explicit app shortcut/start action UX.
- Add repair action for missing scripts.
- Improve first-run screen and status access if tray icon is hidden.
- Keep logs and backup actions accessible from normal UI.
