# ProGo

ProGo — лёгкое Windows tray-приложение для быстрого управления SSH SOCKS-прокси и локального зашифрованного хранилища credentials.

## Статус

Версия: `0.1.27`.

Проект намеренно маленький: без облачного backend, без телеметрии, без хранения секретов на сервере.

## Платформа

- Windows 10/11 x64
- .NET Framework 4.8 runtime
- Windows OpenSSH client (`ssh.exe`)
- Windows PowerShell 5.1 или PowerShell 7.x для скриптов

## Возможности

- Запуск, остановка и перезапуск SSH SOCKS-туннеля из tray.
- Настраиваемые SOCKS host, port и SSH profile.
- User-level `ALL_PROXY` / `HTTPS_PROXY` / `HTTP_PROXY` через loopback HTTP CONNECT bridge `http://127.0.0.1:1881`.
- Current-user системный прокси Windows для browser/login-сценариев.
- CLI/Codex HTTP CONNECT proxy bridge для инструментов, которым нужен обычный `http://` proxy вместо SOCKS.
- Проверка маршрута через `curl.exe --socks5-hostname`.
- Локальный encrypted vault: `%LOCALAPPDATA%\ProGo\vault.enc.json`.
- Типы записей: `api_key`, `password`, `token`, `ssh`, `note`, `custom`.
- Копирование секрета только по явному действию.
- Автоочистка clipboard, если там всё ещё находится скопированное ProGo значение.
- Русский UI.
- Установка напрямую из GitHub.
- Пункт tray `Обновить ProGo`: скачивает свежий `main` из GitHub, пересобирает и заменяет установленный `ProGo.exe`.
- Скрипты build/install/update/uninstall/test.
- GitHub Actions Windows build.

## Ограничения безопасности

Текущий MVP использует 4-значный PIN. Это удобно, но слабо: всего 10 000 комбинаций. Не считайте его сильным master password.

Vault шифруется на диске с использованием:

- AES-256-CBC
- HMAC-SHA256
- PBKDF2-HMAC-SHA256
- encrypt-then-MAC структуры

Логи не должны содержать PIN, секреты, токены, API keys, Authorization headers, расшифрованный vault или значения clipboard.

## Настройка SSH

Создайте SSH alias в стандартном OpenSSH config:

```sshconfig
Host my-vps
  HostName example.com
  User deploy
  IdentityFile ~/.ssh/id_ed25519
```

После этого укажите `my-vps` в настройках ProGo как SSH-профиль.

ProGo запускает команду уровня:

```powershell
ssh.exe -N -D 127.0.0.1:1080 -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 my-vps
```

ProGo не хранит SSH private keys или SSH passwords в source code.

## Системный прокси Windows

ProGo умеет включать current-user системный прокси Windows из tray menu:

```text
Включить системный прокси Windows
```

Этот режим записывает WinINet-настройки текущего пользователя в HKCU и направляет proxy-aware приложения Windows на локальный SSH SOCKS endpoint:

```text
socks=127.0.0.1:1080
```

Перед изменением Windows proxy settings ProGo сохраняет предыдущие значения текущего пользователя в `%LOCALAPPDATA%\ProGo\system-proxy-backup.json`. Для восстановления используйте tray action:

```text
Отключить системный прокси Windows
```

Режим рассчитан на browser/login-сценарии. Если браузер уже был открыт, перезапустите браузер — старые процессы могут держать старые proxy-настройки.

Границы режима:

- только текущий пользователь;
- права администратора не нужны;
- machine-wide WinHTTP не меняется;
- VPN/TUN/WFP-перехвата всего трафика нет;
- приложения, которые игнорируют Windows proxy settings, могут подключаться напрямую.

## CLI/Codex proxy

Некоторые CLI-инструменты не используют Windows system proxy и нестабильно работают с `socks5h://` в environment variables. Для таких случаев ProGo может запустить локальный HTTP CONNECT proxy bridge:

```text
Codex CLI → http://127.0.0.1:1881 → SOCKS 127.0.0.1:<SOCKS-port> → SSH tunnel
```

Tray actions:

```text
Запустить CLI/Codex proxy
Применить CLI proxy env
Открыть PowerShell с CLI proxy
Остановить CLI/Codex proxy
```

`Запустить CLI/Codex proxy` поднимает loopback-only listener `127.0.0.1:1881`. Он принимает только HTTP `CONNECT` и прокидывает поток через текущий SOCKS-туннель.

`Применить CLI proxy env` записывает user-level переменные:

```text
ALL_PROXY=http://127.0.0.1:1881
HTTPS_PROXY=http://127.0.0.1:1881
HTTP_PROXY=http://127.0.0.1:1881
NO_PROXY=localhost,127.0.0.1,::1
```

Уже открытые терминалы не получают новые user-level environment variables автоматически. Для текущей работы используйте `Открыть PowerShell с CLI proxy`, затем запускайте `codex login` в новом окне. Если user-level env уже указывает на `127.0.0.1:1881`, ProGo восстанавливает listener при следующем запуске; явная остановка CLI proxy или выход из ProGo очищает ProGo-owned env, чтобы новые процессы не наследовали мёртвый loopback-порт.

## Установка напрямую из GitHub

Самый простой standalone-вариант для новой Windows-машины:

```powershell
$script = Join-Path $env:TEMP "Install-FromGitHub.ps1"
Invoke-WebRequest "https://raw.githubusercontent.com/rkhnorkhan-bit/ProGo/main/scripts/Install-FromGitHub.ps1" -OutFile $script -UseBasicParsing
powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script
```

Скрипт скачает текущий `main` с GitHub, соберёт `ProGo.exe` через штатный .NET Framework compiler и установит приложение в:

```text
%LOCALAPPDATA%\ProGo
```

Startup shortcut создаётся по умолчанию. Чтобы отключить:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File $script -NoStartup
```

## Установка из локального clone

```powershell
git clone https://github.com/rkhnorkhan-bit/ProGo.git
cd ProGo
.\scripts\Install-ProGo.ps1
```

## Сборка

```powershell
.\scripts\Build-ProGo.ps1
```

Сборка использует:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

## Обновление

В tray menu нажмите:

```text
Обновить ProGo
```

Update-контур делает следующее:

1. запускает `%LOCALAPPDATA%\ProGo\scripts\Update-ProGo.ps1`;
2. закрывает текущий ProGo;
3. скачивает свежий `main` из GitHub;
4. пересобирает приложение локально;
5. заменяет `%LOCALAPPDATA%\ProGo\ProGo.exe`;
6. запускает обновлённый ProGo.

`settings.json`, `vault.enc.json` и logs сохраняются.

CLI-вариант:

```powershell
%LOCALAPPDATA%\ProGo\scripts\Update-ProGo.ps1
```

## Удаление

```powershell
.\scripts\Uninstall-ProGo.ps1
```

По умолчанию user data остаются. Для удаления пользовательских данных:

```powershell
.\scripts\Uninstall-ProGo.ps1 -RemoveUserData
```

Скрипт отдельно попросит ввести `DELETE`.

## Тесты

```powershell
.\scripts\Test-ProGo.ps1
```

Скрипт выполняет hygiene checks и smoke build на Windows.

## Лицензия

ProGo — source-available проект для personal/noncommercial use. Commercial use требует отдельного письменного разрешения владельца. См. `LICENSE.md` и `COMMERCIAL_USE.md`.

## Responsible disclosure

Не публикуйте реальные secrets, vault files, tokens, keys или credentials в issues/PR. См. `SECURITY.md`.
