# Proxy setup

## Connection

In **Настройки → Подключение → Добавить**, choose **По адресу сервера**.
Enter the VPS IP/domain (without `https://`, login or port), SSH login, SSH port
(default 22), and optionally select a private key file. Leave the key field empty
to use standard Windows OpenSSH keys/agent. ProGo stores only the file path;
it does not upload the private key or save an SSH password.

Use **Первый вход** to open a visible SSH console with these same settings.
Verify the host fingerprint independently before accepting it. An encrypted key
must be unlocked through your SSH agent for unattended use. A password login
alone does not authorize a key for background connections.

Existing connections remain in **Из SSH config (для опытных)** mode. The alias
(e.g. `my-vps`) or legacy `user@vpn.example.org` retains OpenSSH resolution; ProGo
does not rewrite the SSH config. **Проверить** uses `ssh.exe -G` with the same
server/login/port/key as the tunnel, and reports the resolved SSH port.

Connect from the main window. SOCKS readiness and a verified internet response
are separate states. Changing the selected server, login, SSH port or key restarts
an active desktop tunnel and discards stale readiness; renaming it does not.
The SSH port is independent of the SOCKS and local HTTP proxy ports.

## Automatic and manual controls

**Настройки → Автоматика** exposes recovery, terminal environment, Windows proxy,
and normal Codex launch as independent startup options. Codex and terminal setup use
the same user proxy environment; manual off cancels both pending environment actions. Startup actions wait for SOCKS
readiness and run once. Manual off cancels an outstanding action for the session;
it is not undone on the next timer tick. Unchecking disables future automatic work;
use the adjacent button to disable a currently active feature. New Windows and Codex
options default to false. Existing recovery and environment preferences are retained.

Recovery checks the owned SSH process and SOCKS handshake every five seconds,
uses a 20-second startup grace period, and retries with capped exponential backoff.
It never adopts or kills a process belonging to someone else. Manual stop cancels
retries. Automatic recovery also applies to the independent iPhone tunnel.

## Application port

Use **Настройки → Порт приложений**. Automatic selection defaults to on for new and
upgraded settings. The bridge tries the saved port (initially 1881). If Windows reports
it busy/reserved, ProGo binds port zero and uses the port assigned by the OS. The
socket remains bound throughout setup, avoiding a check-then-bind race.

For fixed mode, uncheck **Выбирать свободный порт автоматически** and enter a port
from 1 to 65535. A conflict reports an actionable error and retains the old listener
and settings. **Подобрать свободный** reserves a new port at Save, without changing
mode. An unrelated Save or repeated Start keeps a healthy listener on the same port.
Automatic selection happens at proxy startup; it is not a background port rotation.

Changes move only ProGo-owned environment values, Windows proxy endpoint and Codex
wrapper, retaining original restore backups. Failure to save settings rolls back those
integrations. Manually disabled integrations stay disabled. No other listener is stopped.
The bridge remains loopback-only. SSH, SOCKS, router and IKEv2 ports are independent.

## Command line

**Запустить CLI** on the dashboard, or **Прокси для приложений → Запустить CLI (Codex и терминалы)** applies the selected port.
For example, when the dashboard shows port 1881:

```text
HTTP_PROXY=http://127.0.0.1:1881
HTTPS_PROXY=http://127.0.0.1:1881
ALL_PROXY=http://127.0.0.1:1881
NO_PROXY=localhost,127.0.0.1,::1
```

The listener is loopback-only and supports plain HTTP plus HTTPS CONNECT over the
selected SOCKS transport. DNS destination names are passed to SOCKS. Established
streams have no idle read timeout; request and SOCKS handshakes are bounded.

Existing terminal processes retain their environment. Open a new terminal, or use
**Открыть терминал с прокси**. Environment changes are for the current Windows user;
previous values are backed up and restored only if still owned by ProGo. Turning off
automation alone does not change those values. Disconnect/exit restores owned settings.

## Codex CLI

**Запустить CLI** starts the HTTP bridge and applies its actual port to the current
Windows user's environment. Run `codex` normally afterwards. The automatic setting
**Включать прокси для обычного запуска Codex** and **Codex — включить прокси** use
the same environment setup. No special shortcut is required.

Completely close and reopen an already running terminal, IDE or Codex app once so it
inherits the updated environment. Opening another tab in an existing terminal may
reuse its old environment. **Открыть Codex через ProGo** remains a convenient direct
launch with an explicit process environment. Codex installation is separate.

The scoped Start Menu shortcut is optional under **Дополнительно: ярлык Codex**.
Creating/removing it does not enable/disable the shared user proxy environment.
Manual CLI/Codex off restores only ProGo-owned user values; it cannot change the
environment of processes that are already running. Windows system proxy is separate.

## Windows applications

**Windows — включить** sets current-user WinINet proxy values to the same selected
port. For example, when it is 1881:

```text
http=127.0.0.1:1881;https=127.0.0.1:1881
```

The previous proxy configuration is stored in `system-proxy-backup.json`.
**Windows — выключить** restores that configuration. Disconnect/exit restores it
only while the applied proxy still belongs to ProGo. Other apps' later proxy changes
are not overwritten on exit. Machine-wide WinHTTP is not modified.

Apps that ignore system settings can connect directly; this feature is not a full
Windows VPN. The [iPhone wizard](HOME_IKEV2.md) is a separate native IKEv2 route.

## Restoring Windows settings

**Windows — выключить** restores only values still owned by ProGo. Later changes
to the endpoint, bypass list or PAC are preserved. If another application replaced
the proxy endpoint, its associated Windows flags are preserved as well.

If a registry field or recovery-journal write fails, ProGo reports incomplete
cleanup and keeps the journal for retry. Manual desktop stop and normal Quit keep
the local service running on that failure. Correct the reported write/access
problem, then retry **Windows — выключить** or desktop stop. A successful retry
does not revisit fields already preserved as external changes.
Uninstall requests this cleanup from the running app before removing files or
shortcuts. If cleanup is refused or cannot be confirmed, removal is cancelled.
Update/restore also require successful cleanup before replacing files. If a crash
left a recovery journal, reopen ProGo and retry Windows/CLI off before maintenance.
Older running builds without cleanup IPC must be closed through their own Quit
command; uninstall does not force-kill them. A failed updater handoff after
successful cleanup leaves the app open; reconnect manually if needed.

## Backup retention

App and updater share one manifest-aware policy: keep all manual and unknown
folders, ten latest known automatic backups, the latest baseline and pre-update,
and the backup currently being created. Legacy folders with missing or ambiguous
metadata are preserved. Automatic cleanup never infers deletion eligibility
from a folder name. Generated timestamped names determine newest-first order.

**Удалить старые автоматические копии…** shows every candidate path before an
explicit confirmation. Cancel keeps all copies. A changed manifest or junction
is skipped during revalidation; newly eligible folders wait for the next review.
