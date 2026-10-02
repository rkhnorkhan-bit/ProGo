# Proxy Setup

## Automatic SOCKS recovery

Starting with 0.1.29, the tray toggle `Автовосстановление SOCKS` is enabled by
default. It applies after ProGo starts an SSH tunnel; it does not enable the
separate `Запускать SOCKS вместе с ProGo` setting.

ProGo checks its SSH process and the local SOCKS greeting every five seconds.
An unexpected process exit schedules a restart. A new process has a 20-second
startup grace period; three consecutive failed listener checks after that grace
period cause a restart. SSH keepalives detect a lost remote connection separately.
A website outage alone is not treated as a local tunnel failure.

Retries wait 5, 10, 20, 40, then at most 60 seconds. After one minute of healthy
listener checks the backoff resets. Automatic attempts use `BatchMode=yes`, so
SSH keys or an already unlocked agent must work without a password prompt.
Host-key validation remains enabled according to the SSH configuration.

`Остановить SOCKS` cancels retries until the next explicit start. Turning the
recovery toggle off leaves a running tunnel alone. Exiting or updating ProGo
disposes the monitor and stops the owned process. Another process occupying the
SOCKS port is never killed or adopted by the watchdog.

The `Состояние` window displays recovery status. Existing connections can break
during a restart; client applications must reconnect. The local CLI proxy and
optional iPhone relay are not stopped by automatic SOCKS recovery.

## 1. Create an OpenSSH profile

Edit your user OpenSSH config:

```text
%USERPROFILE%\.ssh\config
```

Example:

```sshconfig
Host my-vps
  HostName example.com
  User deploy
  IdentityFile ~/.ssh/id_ed25519
```

Check it manually:

```powershell
ssh my-vps
```

## 2. Configure ProGo

Open ProGo settings and set:

- SOCKS host: `127.0.0.1`
- SOCKS port: `1080` or another free local port
- SSH profile: `my-vps`

For automatic CLI routing, enable:

```text
Автоматически запускать HTTP proxy и применять env
```

## 3. Start SOCKS

Tray menu:

```text
Запустить SOCKS
```

ProGo starts a command equivalent to:

```powershell
ssh.exe -N -D 127.0.0.1:1080 -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 my-vps
```

The SOCKS endpoint is the internal/explicit transport endpoint. It is not published as `HTTP_PROXY`, `HTTPS_PROXY`, or `ALL_PROXY`.

## 4. CLI/Codex HTTP CONNECT bridge

ProGo exposes a loopback-only HTTP CONNECT proxy:

```text
Codex CLI
  -> http://127.0.0.1:1881
  -> SOCKS 127.0.0.1:<SOCKS-port>
  -> SSH tunnel
  -> remote VPS
  -> destination
```

Use:

```text
Запустить CLI/Codex proxy
```

The bridge listens only on `127.0.0.1:1881`. Long-lived CONNECT streams, including WebSocket traffic, do not use a ProGo read/write idle timeout.

## 5. Apply proxy environment

Tray menu:

```text
Применить proxy environment
```

ProGo starts the local HTTP bridge if needed and writes user-level environment variables:

```text
ALL_PROXY=http://127.0.0.1:1881
HTTPS_PROXY=http://127.0.0.1:1881
HTTP_PROXY=http://127.0.0.1:1881
all_proxy=http://127.0.0.1:1881
https_proxy=http://127.0.0.1:1881
http_proxy=http://127.0.0.1:1881
NO_PROXY=localhost,127.0.0.1,::1
no_proxy=localhost,127.0.0.1,::1
```

This is the canonical environment route for Codex and other HTTP-proxy-aware CLI tools.

Already running processes keep their inherited environment. Open a new terminal, or use:

```text
Открыть PowerShell с CLI proxy
```

If user-level proxy variables already point to `http://127.0.0.1:1881`, ProGo restores the bridge listener on application startup even when the variables were applied in an earlier session.

Explicitly stopping the CLI/Codex proxy or choosing `Выход` clears ProGo-owned user-level proxy variables so new processes do not inherit a dead loopback proxy.

## 6. Windows system proxy

`Включить системный прокси Windows` remains a separate current-user WinINet/browser feature. It points supported Windows applications at the SOCKS endpoint and does not replace the HTTP CONNECT bridge used by Codex CLI.

## 7. Check the route

Check the HTTP bridge:

```powershell
curl.exe -v -x http://127.0.0.1:1881 https://api.ipify.org
```

The returned public IP should be the VPS egress IP.

Check Codex:

```powershell
codex doctor
codex exec "Reply only with WORKS"
```

For ChatGPT subscription authentication, `codex doctor` should report ChatGPT auth, reachable provider endpoints, and a successful WebSocket handshake when the current Codex version and network path support it.
