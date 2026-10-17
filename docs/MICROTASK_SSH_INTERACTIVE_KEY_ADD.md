# PGA-06A — отдельная управляемая консоль для добавления выбранного ключа

**17.10.2026 UTC. Реализация и fixtures подготовлены; native CI PENDING.**
Родитель `5fb3874f24b3b6cf778a8b9318db287449e89bcb` (PGA-05C/CI564).
Три небольших шага внутри F31-R11: console owner; immutable selected-key request
и backend ssh-add; native input/lifetime/encrypted-key fixtures. Счёт **6/55**,
остаток49=37+12, верхний уровень23/31. Новых ID или принятых критериев нет.

## Выбранный механизм

Прямой CreateProcessW выбранного OpenSSH, CREATE_NEW_CONSOLE + CREATE_SUSPENDED +
CREATE_UNICODE_ENVIRONMENT. Прежде чем запустить код ребёнка, ProGo помещает его
в собственный JobObject с KILL_ON_JOB_CLOSE без breakaway. Cancel, timeout,
ошибка callback и обычное завершение очищают только этот job; cleanup отдельно
ограничен2s и подтверждает ActiveProcesses=0. Нет shell/PowerShell/UAC,
перенаправления потоков или наследования handles приложения. Консольные
stdin/stdout/stderr создаёт Windows. ProGo не читает и не пишет эту консоль.

Парольную фразу принимает непосредственно OpenSSH с выключенным echo. В backend
нет параметра для неё, password textbox, stdin pipe, captured stderr/stdout,
settings/log/telemetry записи. Child-only environment удаляет SSH_ASKPASS,
DISPLAY/WAYLAND_DISPLAY, ставит SSH_ASKPASS_REQUIRE=never. Избранный при подготовке
SSH_AUTH_SOCK фиксируется, включая отсутствие override; environment родителя
не меняется. Это предотвращает случайную смену агента между подготовкой и запуском,
но не является проверкой прав доступа/владения endpoint.

Основания выбора: [Microsoft: отдельная консоль](https://learn.microsoft.com/en-us/windows/console/creation-of-a-console),
[CreateProcessW](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw),
[JobObject](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects),
[OpenSSH ssh-add: tty, askpass, -k, exit status](https://man.openbsd.org/ssh-add).
Документация задаёт механизм; работоспособность установленного в runner Windows
OpenSSH должна быть подтверждена отдельным native oracle ниже, не предположением
о текущей версии upstream.

## Backend и границы

- Только effective resolved профиль с явно разрешённым publickey и выбранным
  членом IdentityFiles с подтверждённым полным IdentityPath. Unknown policy,
  IdentityAgent none и пока не поддержанный custom IdentityAgent не допускают
  скрытой подмены стандартным агентом. Отсутствующий sibling ssh-add различается.
- Immutable request содержит выбранный sibling ssh-add.exe, один путь и endpoint.
  Команда `-k -- <quoted path>` не подхватывает default identities/certificates;
  CRT quoting, без shell interpolation. Ключ не открывается кодом подготовки.
- LongRunning worker освобождает вызывающий UI поток. Общий прежний key lease
  сериализует inventory/fingerprint/add до фактического cleanup, включая отмену.
  Имя SshReadOnlyLease сохранено для старых callers, новый scope описан в коде.
- Typed completed/failed/unavailable/busy/timeout/cancelled/native numeric error.
  Raw exception/output/путь/отпечаток не попадают в display. CommandCompleted не
  доказывает private-key binding, подпись или server authentication.
- Cancel не откатывает уже принятый агентом ключ. ProGo не удаляет ключи из общего
  агента, не останавливает службу и не меняет ACL. Ошибки приватного ключа пока
  классифицирует сам OpenSSH; отдельный bad permissions UX остаётся PGA-07/08.
- Это backend для будущего явного «Добавить ключ», ещё не подключённый к UI или
  startup. SCM/startup coordination, selected-key proof/constraints/cert/FIDO,
  custom endpoint, свежесть UI результата и SSH auth readiness остаются открытыми.
  Существующий «Первый вход» не переименован в добавление ключа.

## Проверки

Новый Desktop partial явно добавлен в compiler и workflow. Pure/boundary checks:
effective policy/identity/endpoint guards, безопасные пути, immutable snapshot,
exit mapping, redaction, cancellation, lifetime validation, shared lease и late
success после cancel. Старые проверки и watchdog budgets не меняются.

Native Windows fixtures проверяют реальную отдельную консоль и три console
handles, child-only environment, ввод фиксированной синтетической строки при
echo off, callback failure, cancel/timeout с дочерним процессом, normal-exit
cleanup и сохранение отдельно запущенного процесса. Только тестовый helper
использует AttachConsole/WriteConsoleInput; production не включает этот helper.

Настоящий выбранный ssh-keygen создаёт временный зашифрованный Ed25519 ключ:
две console entry, без passphrase argv/environment. Затем настоящий ssh-add
должен получить одну console entry для этого ключа и послать ADD_IDENTITY в
одноразовый test-owned named pipe. Служба Windows ssh-agent не запускается и не
меняется, реальный пользовательский агент не получает ключ. Fixture pipe только
принимает ADD_IDENTITY/возвращает SUCCESS; не подписывает и не имитирует полный
auth proof. Тестовая строка публичная и постоянная; ключи временные, private
packet очищается в памяти; приватный материал не печатается и не сохраняется
в диагностике. Временные файлы удаляются.

Локально нет Windows/.NET runtime; native результата пока нет. Source review
самостоятельный; независимого agent review не было. CI результаты, точный source,
логи и artifact metadata будут внесены после completed run.

PGA-05/06/F31-R11/F31/F17 OPEN, F31-R05 PARTIAL; Package PENDING, Live NOT_CHECKED.
Все55 IDs/10 PGA rows/11 acceptance scenarios сохранены. Старые FAIL555/556/562
и UNKNOWN причины556 не переобъявлены. Codex shortcut ownership/disabled caption
findings открыты. ПК/VPS/телефон/установленная программа/ключи/ACL/платежи владельца
не проверялись и не менялись. PR88 draft/unmerged, VERSION0.2.2, без release.
