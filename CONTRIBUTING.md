# Contributing

ProGo accepts small, controlled changes.

## Rules

- Do not commit real secrets, vault files, logs, private keys, `.env` files, or credentials.
- Keep the app small and Windows-focused.
- Do not add telemetry, backend sync, cloud storage, or auto-update without owner approval.
- Do not change vault file format, encryption, KDF, PIN policy, or decoy semantics without a separate security design.
- Keep UI text Russian and internal stable values English.
- Run `scripts/Test-ProGo.ps1` before opening a pull request.

## Commit style

Examples:

- `feat: add vault search filters`
- `fix: prevent secret logging`
- `docs: document proxy setup`
- `security: harden clipboard cleanup`
