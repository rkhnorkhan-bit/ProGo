# ProGo

ProGo — лёгкое Windows tray-приложение для быстрого управления SSH SOCKS-прокси и локального зашифрованного хранилища credentials.

## Статус

Версия: `0.1.0` bootstrap MVP.

Проект намеренно маленький: без облачного backend, без телеметрии, без хранения секретов на сервере.

## Платформа

- Windows 10/11 x64
- .NET Framework 4.8 runtime
- Windows OpenSSH client (`ssh.exe`)
- Windows PowerShell 5.1 или PowerShell 7.x для скриптов

## Возможности

- Запуск, остановка и перезапуск SSH SOCKS-туннеля из tray.
- Настраиваемые SOCKS host, port и SSH profile.
- User-level proxy environment variables: `ALL_PROXY`, `HTTPS_PROXY`, `HTTP_PROXY` и lowercase aliases.
- Проверка маршрута через `curl.exe --socks5-hostname`.
- Локальный encrypted vault: `%LOCALAPPDATA%\ProGo\vault.enc.json`.
- Типы записей: `api_key`, `password`, `token`, `ssh`, `note`, `custom`.
- Копирование секрета только по явному действию.
- Автоочистка clipboard, если там всё ещё находится скопированное ProGo значение.
- Русский UI.
- Скрипты build/install/uninstall/test.
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

## Сборка

```powershell
.\scripts\Build-ProGo.ps1
```

Сборка использует:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

## Установка

```powershell
.\scripts\Install-ProGo.ps1
```

Installer копирует `ProGo.exe` в:

```text
%LOCALAPPDATA%\ProGo
```

Startup shortcut создаётся по умолчанию. Чтобы отключить:

```powershell
.\scripts\Install-ProGo.ps1 -NoStartup
```

## Обновление

Запустите installer повторно. `settings.json`, `vault.enc.json` и logs сохраняются.

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
