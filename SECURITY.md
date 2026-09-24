# Security Policy

## Supported versions

| Version | Supported |
| --- | --- |
| 0.1.x | Yes, bootstrap MVP |

## Reporting a vulnerability

Report vulnerabilities privately to the repository owner through GitHub.

Do not publish real vault files, API keys, tokens, passwords, SSH private keys, `.env` files, screenshots containing secrets, logs containing sensitive values, or production credentials in public issues or pull requests.

## Scope

Security reports may include:

- vault encryption or integrity issues;
- clipboard handling issues;
- unsafe logging;
- proxy command injection risks;
- installer/uninstaller data-loss risks;
- secret leakage in UI, logs, tests, docs, or release artifacts.

## Current limitations

The current MVP uses a 4-digit PIN. This is not a strong master password. Offline vault attack resistance is limited by the weakness of the PIN.

## Responsible disclosure

Provide enough detail to reproduce the issue without exposing real credentials. Use synthetic test data whenever possible.
