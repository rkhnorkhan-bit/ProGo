# Proxy Setup

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

## 3. Start SOCKS

Tray menu:

```text
Запустить SOCKS
```

ProGo starts:

```powershell
ssh.exe -N -D 127.0.0.1:1080 -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 my-vps
```

## 4. Apply proxy environment

Tray menu:

```text
Применить настройки прокси
```

ProGo sets user-level environment variables:

```text
ALL_PROXY=socks5h://127.0.0.1:1080
HTTPS_PROXY=socks5h://127.0.0.1:1080
HTTP_PROXY=socks5h://127.0.0.1:1080
all_proxy=socks5h://127.0.0.1:1080
https_proxy=socks5h://127.0.0.1:1080
http_proxy=socks5h://127.0.0.1:1080
NO_PROXY=localhost,127.0.0.1,::1
```

Already running processes may need restart to read updated environment variables.

## 5. Check route

Tray menu:

```text
Проверить соединение
```

The default endpoint may return HTTP 401 without authorization. In ProGo UI this is treated as a working unauthenticated route, not as a failure.
