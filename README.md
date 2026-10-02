# ProGo 0.2.1

A lightweight native Windows app for SSH proxy connections, a home-entry iPhone VPN,
and an encrypted local vault. Russian interface, no telemetry, no vendor-operated backend.

**[Инструкция на русском](https://github.com/rkhnorkhan-bit/ProGo/blob/main/README.ru.md)** ·
[Latest release](https://github.com/rkhnorkhan-bit/ProGo/releases/latest)

## Desktop experience

Open ProGo from the tray for the connection dashboard. Settings separates automation,
connections, application ports, vault preferences and diagnostics. All application dialogs use the same
native dark theme, keyboard controls and multi-resolution icon. Windows native security
and file dialogs retain their standard appearance. No embedded browser or VM is required.

## Four independent automatic options

| Option in Settings | Behavior |
| --- | --- |
| Восстанавливать подключение при обрыве | Recovers an owned SSH/SOCKS connection after failure. Manual stop cancels retries. Applies to the home VPN channel too. |
| Включать прокси для командной строки | Applies proxy environment variables for newly launched terminals when the connection is ready. |
| Включать прокси для приложений Windows | Applies current-user Windows HTTP/HTTPS proxy settings when ready. |
| Подготавливать Codex к работе через прокси | Creates a **Codex через ProGo** Start Menu shortcut that gives only the launched CLI a scoped proxy environment. |

Unchecked means manual control; adjacent buttons remain available. Unchecking does not
silently undo a manually active feature. Manual off suppresses pending automation for
that session. The next launch or an explicit preference off/on re-arms it. Existing
preferences migrate; new Windows/Codex automation defaults to off. CLI installation
itself is separate. **Подключаться к серверу при запуске ProGo** remains a separate setting.

Proxy-aware Windows apps and terminal programs use a shared loopback HTTP endpoint, which
supports HTTP requests and HTTPS CONNECT over SSH/SOCKS. This does not capture all PC
traffic. Previous user settings are restored where still owned by ProGo on disconnect
or exit; existing processes must be restarted to refresh their environment.

## Application proxy port

**Настройки → Порт приложений** defaults to automatic selection. ProGo first tries the
last saved port (1881 on upgrade); if unavailable, it binds a free loopback port and
updates its active integrations. Uncheck **Выбирать свободный порт автоматически**
for a fixed port. **Подобрать свободный** chooses a new port on Save while retaining
the selected mode. A fixed-port conflict leaves the existing connection unchanged.
The current address appears on the dashboard and can be copied in settings.
Restart already open terminals and Codex after changing it.
[Detailed behavior](docs/PROXY_SETUP.md#application-port).

## Home VPN

Open **iPhone через домашний ПК…**. Choose an existing VPS token or add an Ubuntu VPS
with SSH. The wizard provisions the VPN service, shows router forwarding rules,
exports an iPhone profile and reports transport counters. Owners can issue and revoke
separate invitations. [Setup and limits](https://github.com/rkhnorkhan-bit/ProGo/blob/main/docs/HOME_IKEV2.md).

0.2.0 fixes iOS rejecting profiles with `Invalid DH group (0)`. Export a new profile
from the wizard and reinstall it; the server does not require reprovisioning.
The home route remains experimental and must be verified with the actual phone/provider.

## Updates and antivirus

**Обновить ProGo** installs the latest published release. Starting with 0.2, the installed
updater file resolves a version-specific release asset and verifies GitHub's SHA-256
digest before extraction. Archive paths and staging are checked before replacement;
backup and rollback are retained. Settings, vault data and VPN credentials are preserved.
The first upgrade from an older release still uses that release's package validation.

The updater no longer downloads/evaluates PowerShell text in memory or compiles
remote source as a fallback. It respects execution policy. Missing digests and failed
validation cancel the update rather than weakening checks. No antivirus exclusions,
security disabling or detection-evasion behavior is included.

The executable is currently **unsigned**. SHA-256 is an integrity check, not an
Authenticode publisher signature or antivirus approval. Use **Помощь и журналы →
Антивирус и обновления…** to reach logs, the official release, and Kaspersky OpenTIP.
Record the exact detection name before assuming a false positive.

If an older updater is blocked, use the official release via a browser after resolving
the detection with the antivirus vendor. Close ProGo, back up its folder, and extract
the archive into `%LOCALAPPDATA%\ProGo`. Keep settings, vault and private VPN data.

## Platform and development

Windows 10/11 x64, .NET Framework 4.8, Windows OpenSSH and PowerShell 5.1.

```powershell
.\scripts\Build-ProGo.ps1
.\scripts\Test-ProGo.ps1
.\scripts\Test-HomeVpnRelay.ps1
```

For a source checkout, `scripts/Install-ProGo.ps1` builds and installs locally.
[Development documentation](https://github.com/rkhnorkhan-bit/ProGo/blob/main/docs/DEVELOPMENT.md).

## Vault and license

The vault uses AES-256-CBC, HMAC-SHA256 and PBKDF2-HMAC-SHA256. Its four-digit PIN
has only 10,000 combinations and is not a strong master password. Never publish
credentials, phone profiles, tokens, private keys, decrypted data or personal logs.

Source-available for personal/noncommercial use. Commercial use requires separate
written permission; see LICENSE.md and COMMERCIAL_USE.md in the repository.
