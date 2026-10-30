# SSH-agent PGA-03A: запуск службы с текущими правами

Baseline `df50e2ce8d44d71f6755def1be7ff4a5f09841b1`, VERSION0.2.2.
16.10.2026 UTC. Это узкая первая часть PGA-03 внутри прежнего F31-R11,
не новый ID55. Самостоятельный source/native review и полный CI559 SUCCESS:
узкий PGA-03A **Code/Dev PASS**. Весь PGA-03 остаётся PARTIAL до PGA-03B.

## Поведение

Отдельная start-capability поверх прежнего read-only native boundary. Первый
snapshot сохраняет независимые runtime/startup policy/read errors. Running,
StartPending, другие переходные состояния, Paused, Missing, неизвестный status
и Disabled/Stopped не получают вызов StartService. Неизвестный режим запуска
из-за запрета QUERY_CONFIG не равен отсутствию SERVICE_START: решение принимает
Windows без обхода policy.

Для известного Stopped открываются только SC_MANAGER_CONNECT и
SERVICE_START|SERVICE_QUERY_STATUS (`1`/`0x14`). Перед запуском выполняется
повторный query на том же handle: внешний start или переход состояния не
порождает дубликат. Однократный StartServiceW без аргументов не меняет режим
запуска, ACL, системный SSH config или ключи, не вызывает PowerShell/UAC.

StartInvoked, StartAccepted и свежий runtime state — отдельные факты. Успешный
StartService не доказывает Running: выполняется один post-query, Pending и
Stopped остаются явными результатами. 1056 также требует свежего query и не
выдаётся за готовность. 5/1058/1060/1722/1053 и прочие ошибки сохраняют точный
этап/Win32; непредвиденная exception не раскрывает raw message и не получает
выдуманный native code. Все handles освобождаются в finally, отмена проверяется
между native calls. Запуск общего агента никогда не откатывается/не останавливается.

Shared probe допускает только один read ИЛИ start, включая cleanup. Timeout/
cancel не освобождает slot до фактического settlement OS worker; конкурирующие
read/start возвращают Busy. Owner cancellation связывается до scheduling,
чтобы не пропустить закрытие между запуском worker и регистрацией waiter.
Start получает независимый CancelAfter token до scheduling; прежний bounded
wait/monotonic check остаётся. Отмена не может прервать уже вошедший StartService
или отменить его системный эффект; новый query после наблюдения отмены не выполняется.
Windows scheduling/непрерываемый native call не имеют real-time гарантии.

Result formatter содержит причину и следующее действие, не считает службу
доказательством выбранного ключа/SSH-аутентификации. Это подготовленный internal
API: Program/context/auto-connect и существующее диагностическое окно его ещё
не вызывают. Startup hook/его lifetime — PGA-03B, полный agent UI — PGA-08,
matching/interactive ssh-add — PGA-06, Automatic installer opt-in — PGA-04.

## Проверки

Новый Desktop partial проверяет production reader/start/coordinator через
контролируемую native boundary: skip states/policy, минимальные rights,
same-handle races, accepted/Pending/Stopped, rejected/native failures,
unknown/exception, отмену на каждом значимом checkpoint, read-vs-start и
12 повторных read/start при удерживаемом native worker после timeout, raw ownership/cleanup/fresh retry,
запрет позднего StartService при отмене в read, успешное завершение без caller
synchronization context. Фактические66 checks подтверждены native block CI559:
35 direct Check sites с дополнительными выполнениями loop/двух hold cases.

Один реальный Win32 oracle вызывает StartServiceW с null handle: ожидается
ERROR_INVALID_HANDLE6, без открытия/изменения реальной службы. Успешные start
и запреты policy проверяются на boundary fixtures; реальная служба CI читается
прежним oracle, её режим/состояние/ключи не меняются. Это не installed/live приёмка.
Старые tests/conditions/budgets сохранены; main/compiler имеют по одной вставке.
UI не меняется, новые визуальные claims не заявляются.

Самостоятельно проверены call ordering, точность native error capture,
независимость read/start permissions, отсутствие stop/configure/ACL/elevation,
finally cleanup, linked token до schedule, slot до raw completion и отсутствие
исключений заброшенного worker. Независимого agent review в этой микропорции нет.
Локально Windows compiler/runtime отсутствует; native PASS не подменяется source review.

Исходные55unique=43+12; приняты только F17-R02/R03 и F31-R01/R02/R03/R04.
Счёт6/55, остаток49=37+12, общий23/31. F31-R05 PARTIAL, F31-R11/F31/F17 OPEN;
Codex ownership/disabled caption/UNKNOWN ssh-G556 остаются findings.
Package PENDING, Live NOT_CHECKED. Owner PC/VPS/телефон не проверяются,
реальные службы/ключи/ACL владельца не меняются. PR draft/unmerged, без merge/release.

## Первичные источники

- [StartServiceW](https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-startservicew): SERVICE_START, нулевые arguments, accepted не означает Running, QueryServiceStatus и ошибки/непрерываемое ожидание SCM.
- [Service access rights](https://learn.microsoft.com/en-us/windows/win32/services/service-security-and-access-rights): START и QUERY_STATUS отделены от CHANGE_CONFIG/STOP.

## Frozen source и completed native CI559

Tested source `4a7a6a9fec80a17321f6ffc0ac4fa5fdf6fedd6a`, tree
`2576062986df57ef6b5a3d1368afa186435e212a`, parent baseline df50e2ce.
Локальный эквивалент `d182348b05a3bc9287ea53808649162c77d77251` имеет тот же tree.
PR-triggered merge `9486787c536aa18d59cc9fb95ec1a3948787c43e` в неизменный
main `4bb336575bb1216d3047bd2b294aeecad9fdd65a` подтверждён обоими checkout logs.
[CI #559](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38971701412)
completed **SUCCESS**, весь run и оба jobs: Windows117211358601/Ubuntu117211359027.
Без повторного запуска CI.

| Файл | Frozen Git blob |
| --- | --- |
| `scripts/Test-ProGo.ps1` | `f64de1d24be9f01868899b1c28737e225b06769a` |
| `src/SshAgentServiceProbe.cs` | `ea513608fc59e268c9f8246660fd537957783649` |
| `src/SshAgentServiceStart.cs` | `a510deef1cd020fbd87d8e69438f352ea5552a74` |
| `src/SshAgentServiceStatus.cs` | `daa882adf2540fcdfde8ed2341b17aff820692b0` |
| `tests/DesktopSshAgentStartTests.cs` | `86c06225cee304c7c259102021a3c0c426138a54` |
| `tests/DesktopTests.cs` | `16fe411b7b67f0694af602de477fa1610da43ad0` |

### Фактическая проверка Windows

Новый block66 PASS:21:59:17.5628967–21:59:19.6849601 UTC, итоговый marker
`SSH-agent start checks PASS: 66` в21:59:19.6850827. Desktop4126=4060+66
PASS22:00:02.1271066. Подтверждены все skip/rights/policy/state/error cases,
same-handle race и отдельные invoked/accepted/observed facts. Held before-query
и held StartService дали bounded waiter timeout при всё ещё pending raw Task;
12 mixed read/start повторов в каждом случае получили Busy до settlement.
Эти12 caller requests подаются последовательно во время удерживаемого native
worker; отдельные12 caller threads здесь не заявляются. Прежний PGA-02B содержит
свой отдельный12 parallel read test и также прошёл в полном Desktop наборе.
После cleanup новый start вернул свежий Running. Отмена в read синхронно
достигла linked native token и после release не допустила StartService.
Непрерываемый уже вошедший start был честно оставлен owned до cleanup, без
post-cancel query. Background/no caller context и отсутствие unobserved faults PASS.

Реальный Win32 oracle21:59:19.6849601 вернул Win326 на null handle; служба не
открывалась и не запускалась. Успешный start, permissions/policy и внешний race
проверены production state machine на controlled native boundary, не на настоящей
системной службе. Прежние37 SCM checks и independent read-only oracle PASS:
21:59:14.5339607 runtime1/Stopped, startup4/Disabled, statusError0/configError0.
Прежние32 probe/UI checks также PASS; старый fixture.dotted native config oracle
PASS21:59:19.7201639, failure-only diagnostic INFO не сработал. Причина исторического
CI556 остаётся UNKNOWN: успешный559 её не объясняет и finding не закрывает.

SOCKS41/403 PASS21:52:22; Instance28 PASS22:00:09.6311114,
Maintenance95 PASS22:02:16.7641444, Shutdown47 PASS22:02:45.6002954,
updater5 PASS22:02:47.4752887; полный Test-ProGo PASS22:02:47.4760711.
Все прежние test conditions/budgets сохранены. Main/compiler after removal
одной новой wiring вставки каждый эквивалентны baseline; implementation typed
reader идентичен baseline, добавлены только enum stages и visibility native adapter.
Program/context/UI/ProxyService/config parser/installer/workflows не меняются.

HomeVPN975 PASS22:06:08.4974642, portable90 PASS22:07:57.1463166;
Windows Python24=22 PASS+2 прежних Linux-only skips,22:07:59.3220647.
Все7 HTTP profile-share cases выполнены ok; public276 source/44 release
PASS22:08:00.2445192.

### Ubuntu

16 operation recovery +9 sharing/sync +46 isolated provision checks PASS,
0 skips. Actual independent QR decode в venv с numpy2.2.6/zxing-cpp2.3.0;
достигнут весь recovery/revoke/reissue/TLS/session tail. Последний provision
check restart/session PASS21:48:34.2394839. Owner devices/servers не затрагивались.

### Logs и artifacts

Полные job logs сохранены локально UTF-8/LF для самостоятельной сверки:

| Job | Bytes | Chars | Lines | SHA-256 |
| --- | --- | --- | --- | --- |
| Windows | 933220 | 906171 | 8606 | `ba130339d3ba32384947189250cb8c0e053d39015fe648d38f43b5263c459af0` |
| Ubuntu | 52795 | 52541 | 573 | `416f3eca4884f377e71e36914535ad2d7ad5a5272efb14ce25d49171d8f8ad0c` |

Все4 artifact metadata heads равны tested source4a7a6a9f. ID/size/digest
сопоставлены с actual runner upload log, не только с API summary:

| Artifact | ID | Wrapper ZIP bytes | SHA-256 |
| --- | --- | --- | --- |
| ProGo-ci-diagnostics | 12078475402 | 88604 | `b7d6c56448409f2d7868fdd5398065917659842e837a3cc18cf74ce99428f128` |
| ProGo-release | 12078165720 | 538323 | `d096165e2356178f0cb450c3af8d2828e21ce32aaaf7a7ebd6b5faaa1c96cacf` |
| ProGo-release-zip | 12078320546 | 530045 | `efe125915707053175660c7740d37566c28bee1e045979599eac5f922208383a` |
| ProGo-wizard-screenshots | 12077673618 | 6539576 | `1b9e84edc179cc21dfd26ca0edfd27f6ad0ec50173827d3bd58fe72de00b1e83` |

В этом пакете ни один artifact ZIP не скачивался: нет root hash по локальному
архиву, ZIP entry inspection или нового visual/DPI/contrast/Narrator acceptance.
Runner сообщает293 screenshot files; это не локально проверенное число entries.
UI не менялся. Inner `ProGo-release.zip`533500 bytes по runner; это CI build,
не итоговый полный установщик всех31 задач и не installed/live подтверждение.

## Итоговый узкий статус и следующий шаг

Самостоятельный source/native review completed; независимого agent review нет.
PGA-03A Code/Dev PASS, весь PGA-03/PGA-02 PARTIAL и F31-R11 OPEN. F31-R05
PARTIAL, F31/F17 OPEN. Exact accepted IDs только F17-R02/R03 и F31-R01/R02/R03/R04:
6/55, осталось49=37+12,23/31;55unique=43+12 сохранены. Package PENDING,
Live NOT_CHECKED. Codex shortcut ownership/disabled caption/UNKNOWN ssh-G556
остаются findings. Все11 исходных acceptance scenarios сохранены.

PGA-03B подключит Shared.StartAsync после instance.Attach только нормальному
owner startup, один раз за lifetime, в фоне с cancellation/close ownership и
без SCM-ожидания на UI. Self-check/secondary/update helpers не запускают
службу. Результат startup не станет доказательством выбранного ключа; другие
рабочие SSH auth methods не требуют агента. Это следующий пакет, не скрыто
принятая часть текущего. Полный agent UI/interactive/installer opt-in ещё впереди.

После tested source добавлены только4 Markdown evidence/status files. Runtime,
tests, scripts, server и workflows идентичны tested source; [skip ci] относится
только к этому evidence. PR draft/unmerged, без merge/release; VERSION0.2.2.
