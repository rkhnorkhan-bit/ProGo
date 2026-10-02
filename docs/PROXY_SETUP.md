# Proxy setup

## Connection

In **Настройки → Подключение**, add an SSH alias from the standard OpenSSH config
or a target such as `user@vpn.example.org`. Connect from the main window. A live
SOCKS listener means the tunnel is ready; route diagnostics verify actual reachability.

## Automatic and manual controls

**Настройки → Автоматика** exposes recovery, terminal environment, Windows proxy,
and a scoped Codex launcher as independent settings. Startup actions wait for SOCKS
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

**Прокси для приложений → Командная строка — включить** applies the selected port.
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

**Codex — настроить ярлык** creates **Codex через ProGo** in the Start Menu.
The command wrapper uses `setlocal`; its HTTP proxy variables apply only to the
launched Codex process. It does not alter Codex configuration, API keys, PATH or
other terminal environments. Install Codex CLI separately and keep ProGo connected.
**Открыть Codex через ProGo** starts it directly with the same scoped environment.
**Codex — убрать ярлык** removes the ProGo-owned launcher; it does not uninstall Codex.

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
