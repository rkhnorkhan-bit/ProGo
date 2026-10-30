# PGA-06A — отдельная управляемая консоль для добавления выбранного ключа

**17.10.2026 UTC. Узкий PGA-06A Code/Dev PASS по completed CI569.**
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

## Первый native run: CI565 FAIL до новых SSH checks

Source `7a0ba6efbad50c09f2628545f89d9c5f1c8a95ce`, tree
`eefb507c2489227e22e4ae064c2d80106c36dfb6`, merge
`249b1253641b285edcdfcbc1fc078b26860fa19f` в прежний main4bb3365.
[CI565](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39041721074)
completed FAILURE: Windows117422397614 упал в старом ScopedCodex до новых
PGA-06A checks; Ubuntu117422397962 SUCCESS. Сборка, прежние ранние тесты и
UpdateCheck72 прошли; Desktop старт14:47:15.8862709UTC, отказ14:47:25.9127847UTC:
`Codex launcher stalled`, десятисекундный WaitForExit. Новых SSH checks не было,
поэтому их результат NOT_RUN, не PASS/FAIL. Relay/public/package steps SKIPPED.

В старом ScopedCodex фактически унаследованы общий PATH/PATHEXT и cwd runner.
Исправлена изоляция fixture: явный SystemDirectory/cmd.exe, cwd своего fixture,
PATH только fixture+SystemDirectory, фиксированный PATHEXT. Fake codex.cmd,
production LauncherContent, оба assertions, timeout10000ms и watchdog прежние.
При timeout добавлен безопасный факт достижения локального fixture без raw
output/environment. Это устранение видимой зависимости fixture от окружения;
**точная причина отказа565 UNKNOWN**, прежний лог не содержит subprocess trace.
Последующий SUCCESS не будет объявлен доказательством этой причины.

Полные нормализованные CI565 логи сохранены: Windows2759lines/241747bytes,
SHA256 `41079e91c8a0283c2a851b1e4f50df6cdf63409c4d74cb60339d7aba57a84a4e`;
Ubuntu573lines/56209bytes,
SHA256 `b463ef609b0929bda92e59ab1ab9dd5277080b9f0f9a9745a96c130df308a0bd`.
Windows failure log и Ubuntu checkout подтверждают тот же merge/source/base.
CI565 не перезапускался и не замаскирован. Исправление требует нового source/run.

Source review следующего source c68b8ec обнаружил cancellation race в новом
backend: поздний Win32Exception/IOException мог публиковать Failed после уже
запрошенной отмены. Приоритет Cancelled распространён на эти catch boundaries;
безопасный numeric native error при этом сохраняется. Добавлены2 boundary checks,
всего45 новых. CI566/source c68b8ec superseded новым source до приёмки; его
Ubuntu SUCCESS не доказывает новый Windows механизм, неполный run не принят.

## CI568: FAIL тестового подключения к консоли после36 новых PASS

Source `c456bb2d58902407e14f5eb3dd08ba8980e8e567`, tree
`0999a13c419f22bfd52f7fff1a32878764318655`, merge572c581 в прежний main4bb3365.
[CI568](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39042744160):
Windows117425394004 FAILURE, Ubuntu117425394189 SUCCESS. ScopedCodex и все
предшествующие agent blocks прошли; это не объясняет UNKNOWN причину CI565.
Новый block BEGIN15:05:12.1296934UTC,36 PASS до15:05:12.3639701UTC:
guards/lease/late cancellation и настоящая separate console с3 handles/askpass
sanitation. Затем `Console input fixture failed; exit=21`15:05:27.3826095UTC.
Exit21 однозначно относится к AttachConsole helper; native Win32 error в первом
варианте helper не был сохранён, поэтому конкретная причина отказа UNKNOWN.
Ввод, keygen encryption и ssh-add unlock не подтверждены, не объявлены PASS.

По [Microsoft AttachConsole](https://learn.microsoft.com/en-us/windows/console/attachconsole)
уже прикреплённый процесс получает ERROR_ACCESS_DENIED5; требуется FreeConsole
этого процесса. Helper теперь обрабатывает5 через собственный FreeConsole,
6/87 через bounded3s startup retry и пишет только числовую attach диагностику.
При неуспехе остаётся exit21, guards не ослаблены. Parent держит handle test PID
до завершения helper, исключая PID reuse. Production console owner/backend,
45 assertions, прежние timeouts/watchdog не меняются. Это коррекция setup
тестового драйвера, а не переопределение FAIL568 или доказательство его неизвестного
native кода. Новый source должен пройти всю матрицу.

CI566/39042398179/source c68b8ec и CI567/39042670316/source feb4358 completed
CANCELLED при supersession исправлениями source; ни один не принят как PASS.
CI565/568 не перезапускались. Полные нормализованные CI568 логи:
Windows6907lines/751940bytes,
SHA256 `25a3308179d8c493a130d54c26f16f52e2fa10c2464536b34925599f748bcfe3`;
Ubuntu573lines/53081bytes,
SHA256 `e25ef37bb0ccae00f4f4fd98605a8fa955f9ca6e79bae270cf5f1780f49b51fc`.

Локально нет Windows/.NET runtime; приёмка основана на completed native CI569
ниже. Source/native review самостоятельный; независимого agent review не было.

PGA-05/06/F31-R11/F31/F17 OPEN, F31-R05 PARTIAL; Package PENDING, Live NOT_CHECKED.
Все55 IDs/10 PGA rows/11 acceptance scenarios сохранены. Старые FAIL555/556/562
и UNKNOWN причины556 не переобъявлены. Codex shortcut ownership/disabled caption
findings открыты. ПК/VPS/телефон/установленная программа/ключи/ACL/платежи владельца
не проверялись и не менялись. PR88 draft/unmerged, VERSION0.2.2, без release.

## Native Code/Dev приёмка: CI569 SUCCESS

Tested source `9db506714bb26320246e4519aaf2927934dde14f`, tree
`41b27204cf540000a82f8fcb74b60a6c3ce1f81c`, merge
`9baa66717b46aaf9b55dd51c6c9cd911b6c22cf7` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI569](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39043799101)
completed SUCCESS: Windows117428400034, Ubuntu117428400264, весь run SUCCESS.
Оба checkout log подтверждают merge/source/base, main — ancestor source.
Build identity0.2.2+merge9baa667. Это новый run на исправленном source, без rerun.
В Windows только прежний Prune old CI artifacts SKIPPED; тестовые steps SUCCESS.

Новый block BEGIN15:21:29.1694201UTC;45 фактических PASS до marker
`SSH interactive key addition checks PASS: 45`15:21:34.2793584UTC.
Desktop4387=baseline4342+45. Native console+3 handles/no askpass —15:21:29.4650437;
dummy echo-off input15:21:29.7531062; callback/cancel/owned descendants/foreign
process/normal-exit cleanup прошли, timeout case15:21:33.0647062. Настоящий
selected sibling ssh-keygen создал disposable encrypted Ed25519 key с2 console
entries15:21:33.8259551; настоящий ssh-add разблокировал один selected key с1
entry и послал ADD_IDENTITY только в isolated pipe15:21:34.2740904. Private
packet не логировался и очищен; temporary keys удалены. Системный агент не
получал ключ, служба/существующие ACL не менялись. Signing/server-auth не выполнялись.

Все3 helper attachments в этом run дали
`ATTACHED=True;FIRST_ERROR=5;LAST_ERROR=6;DETACHED=True`:
helper отделён от своей консоли после5, startup6 пережит bounded retry. Это
наблюдение **CI569**, не восстановленный задним числом Win32 error CI568.
Native creation/IO/owned cleanup подтверждены на runner; ручная UX/клавиатура/
Narrator/DPI и установленная программа этим не проверены.

Прежние service/probe/start/startup/inventory/matching/policy blocks прошли;
неизменные37 SCM/32 probe/66 start/40 startup/67 inventory/52 matching/57 policy
checks сохранены. Actual SCM oracle runtime1/Stopped, startup4/Disabled,
statusError0/configError0. SOCKS41/403, Instance28, Maintenance95, Shutdown47,
updater5 и полный Test-ProGo SUCCESS. HomeVPN975, portable90, public293source/
44release; Windows Python24=22PASS+2 прежних Linux-only skips, все7 HTTP cases.
Ubuntu16/9/46 без skips, actual independent QR и recovery/TLS/session tail PASS.
Новый source не менял Core/Program/ProxyService/DiagnosticProcess/SCM/probe/
startup/прежние SSH partials/server/workflows/installer; у старого DesktopTests
только новые test roles/workflow call и описанная изоляция ScopedCodex fixture.

Полные нормализованные UTF-8 логи:

| Job | Lines | Bytes | SHA-256 |
| --- | ---: | ---: | --- |
| Windows117428400034 | 8884 | 964491 | `3231d2bbeb298ce3462e86bf0ad752cc5526cd4e907409d0a11b3b52f7d751e0` |
| Ubuntu117428400264 | 573 | 52777 | `84a587344e1448e7ad9e683cd8ec7d2252a7956df72b2ce0c0c978967ed9d22e` |

Все4 artifact workflow_run/source/ID/size/digest сверены с API и upload log:

| Artifact | ID | Bytes | SHA-256 внешнего artifact ZIP |
| --- | ---: | ---: | --- |
| ProGo-wizard-screenshots | 12105461865 | 6541136 | `0402afd4e2cdb5a68ec291900c52ced3c3ab14e670fa76088e585166876c8db5` |
| ProGo-release-zip | 12104938945 | 539395 | `dc390875833eda35a92b6e45740748bc0b6246cd773daf610c6fc0d819450aab` |
| ProGo-ci-diagnostics | 12104604663 | 94357 | `824004044c6be4c6383a16cf3cf09c0806d9389116bb0c68f1dcf26048c42fec` |
| ProGo-release | 12104569812 | 546727 | `dba29cf3b5e9b8ee4dad6da9cc2df7528c474e4f83c71aa9569ed33ee0dcd4e8` |

Архивы не скачивались, нового visual/DPI/contrast PASS нет.293 screenshot files —
число runner upload, не проверенное содержимое ZIP. Внутренний ProGo-release.zip
542846bytes отличается от внешнего artifact ZIP; это не итоговый installer31/31.

Принят только console owner/explicit selected-key backend в указанной матрице.
UI «Добавить ключ», общая SCM/startup координация, private-key/signing/constraints/
cert/FIDO/custom endpoint/auth readiness остаются OPEN. Весь PGA-05/06/F31-R11
не PASS. Следующий пакет — общий lifecycle/coordination и явное UI действие.
Счёт6/55,49=37+12 и23/31 сохранён, Package PENDING/Live NOT_CHECKED.
