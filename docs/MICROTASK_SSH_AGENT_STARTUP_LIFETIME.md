# SSH-agent PGA-03B: фоновый startup и lifetime

Baseline `d6a18d2914609eefd29b5428124a40658f3ea45e`, VERSION0.2.2.
Подготовка16.10.2026 UTC; приёмка17.10.2026 UTC. Узкая микрозадача внутри
прежнего F31-R11; новых ID55 нет. Самостоятельный source/native review и
полный CI560 SUCCESS: узкий PGA-03B **Code/Dev PASS**.

## Изменение поведения

Program после instance.Attach вызывает StartSshAgentAsync только нормальному
основному экземпляру. Self-check и secondary activation возвращаются раньше;
у maintenance/update helper entry points этого вызова нет. Запуск обычного
приложения после успешного update — новый нормальный owner, а не работа helper.
В constructor context SCM не вызывается. Метод планирования доступен только
UI owner; операция сначала резервируется в очереди, затем начинается Task.Run.

SshAgentStartupSession хранит один Task на lifetime: queued dispatch, bounded
probe waiter, best-effort background log и CTS cleanup. Повторный Schedule/
StartSshAgentAsync возвращает прежний Task до и после завершения, после timeout,
отменённого shutdown или Dispose. Двойной dispatch не создаёт второй worker.
Linked native cancellation/минимальные права/ошибки/slot/cleanup уже приняты в
PGA-03A; эти три production SCM/probe files не меняются.

SCM и журнал выполняются вне UI. NotStarted/Queued/Checking/Completed/Cancelled
отдельны от typed probe result и его runtime/startup policy. Completed означает
завершение startup workflow, а не Running/готовность ключа. Результат под gate
публикуется до записи журнала: медленный log не скрывает наблюдённое состояние.
Контекст предоставляет startup Work/state/result для будущего agent UI.
Это snapshot старта, не непрерывно актуальный статус или доказательство auth.

Подготовка выхода/обслуживания и Dispose вызывают Close. Если запуск ещё в
очереди, reservation завершается без worker/SCM и без живого UI pump. При
in-flight native call отменяется waiter, новая публикация после Close запрещена.
Сам native call остаётся owned в общем probe до физического return/cleanup;
после наблюдения отмены native reader/start прекращает следующие calls;
поздняя публикация результата после Close запрещена под gate.
Startup Work не называется полным settlement raw SCM. Штатное завершение
приложения не ждёт непрерываемую общую службу и не посылает ей Stop/rollback.
Уже вошедший StartService может иметь системный эффект несмотря на отмену.
Пока процесс жив, raw task остаётся в probe; при завершении процесса Windows
освобождает его process-owned handles, общая служба не удаляется/не останавливается.

Если результат был опубликован до Close, он сохраняется как исторический факт.
Уже начавшаяся background log I/O может завершиться позже; она не обновляет UI,
не открывает owner и не запускает новую операцию. Work остаётся pending до конца
логирования/CTS cleanup. Файловая I/O не держит gate, Close не ждёт её. Logger
failure не меняет service result и не становится unobserved exception. SafeLog
besteffort: при занятом log record может быть пропущен, это не обязательный IPC
ack и не доказательство отсутствия/повторного запуска. Dispatch failure даёт
безопасный Unavailable, не оставляет вечный queued Work и не раскрывает exception.

Трёхсекундный default относится к ожиданию probe после dispatch/запуска worker;
startup UI queue/планировщик и уже идущий P/Invoke не получают real-time гарантии.
AutoStartSocks/обычная SSH-auth не ждут обязательного агента. Сейчас Program
запускает AutoStartSocks до фонового agent check. Связь selected auth readiness с
началом/повтором auto-connect остаётся открытой в PGA-05/06 и F31-R11; encrypted
agent-required сценарий не объявляется готовым. Новые agent UI действия/отображение
startup причины — PGA-08; installer Automatic opt-in — PGA-04. UI не меняется.

## Проверки

Новый Desktop partial использует production startup session/context/probe/start
с controlled native boundary PGA-03A, без замены state machine. Проверяются:
construction/no UI window, budget/owner-thread guard, closed/lost dispatch,
12 parallel reservations, duplicate dispatch, queue и UI heartbeat, background
native/log и settlement без UI pump, Running/already-running/Disabled/Missing/
AccessDenied и one-shot retry policy, timeout/raw Busy/fresh cleanup, queued и
in-flight Dispose, подтверждённый shutdown и его отмена без automatic retry,
чужой диагностический read (не отменяется/не присваивается), held/failed log и
сохранение наблюдённого результата. Фактические40 checks подтверждены native log CI560:27 direct Check sites и
дополнительные выполнения outcome/queued/in-flight loops.
Все старые tests/conditions/budgets сохранены; main/compiler имеют по одной вставке.
InstanceTests не переписаны: реальные owner/secondary/self-check/IPC/proxy/window
регрессии прежних28 проверок выполняются на новом executable. Точное число
actual Program startup calls не выводится из best-effort log; placement/role
branches проверены source review, one-shot lifetime — production context tests.

Самостоятельно проверены: hook после Attach и вне self-check/secondary ветвей,
минимальная область Program diff, UI owner scheduling, reserve-before-dispatch,
один worker, closed gate, ownership queued/awaiter/log/CTS и отдельный raw probe,
no UI continuation для settlement, logger вне gate, safe failures, сохранение
прочих SSH methods. Независимого agent review в этой микропорции нет.
Локального Windows compiler/runtime нет; source review не подменяет native PASS.
Реальные службы/ключи/ACL владельца не меняются, owner PC/VPS/телефон не проверяются.

Исходные55unique=43+12; accepted только F17-R02/R03 и F31-R01/R02/R03/R04.
Счёт6/55, осталось49=37+12,23/31. F31/F17/F31-R11 OPEN, F31-R05 PARTIAL;
Codex shortcut ownership/disabled caption/UNKNOWN ssh-G556 остаются findings.
Package PENDING, Live NOT_CHECKED; PR draft/unmerged, без merge/release.

## Frozen source и completed CI560

Tested source `270765520d567b25f7b358946ed6b5b27f1684d6`, tree
`d5905e5cc10bc77234b00b5dd27a22ea991ccccf`, parent baseline d6a18d29.
Локальный эквивалент `5b4d86706a81f3145f808272d2dc43f9430e78ab` имеет тот же tree.
PR-triggered merge `aee662cffa8bf449663b94a519f6e19089381988` в неизменный
main `4bb336575bb1216d3047bd2b294aeecad9fdd65a` подтверждён обоими checkout logs.
Main является ancestor source, runtime merge не добавляет другой код.
[CI #560](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38985306318)
completed **SUCCESS**, весь run/оба jobs: Windows117255275406/Ubuntu117255275372.
Без повторного запуска CI; код не менялся во время выполнения.

| Файл | Frozen Git blob |
| --- | --- |
| `scripts/Test-ProGo.ps1` | `9c78a419322d10357910201aa73c1ff82f2b754a` |
| `src/Program.cs` | `b6f30499065c00ec5e639f485657cf96a423d844` |
| `src/SshAgentStartupSession.cs` | `c58acaa54408c5dd1597378575604fd25afb31ed` |
| `src/UpdateAwareTrayApplicationContext.cs` | `9326cf9a7b31156af41371060318d46ec1971f78` |
| `tests/DesktopSshAgentStartupTests.cs` | `141d579cb304282d824aab1c3922dedd256700eb` |
| `tests/DesktopTests.cs` | `f14bdf440a68fba8b9121d822564f43a597e6ea2` |

### Фактическая приёмка Windows

Новые40 Check PASS00:22:19.4559816–00:22:21.2634172 UTC; итоговый marker
`SSH-agent startup checks PASS: 40`00:22:21.2663282. Desktop4166=4126+40
PASS00:22:55.5470082. Подтверждены12 parallel reservations/duplicate dispatch,
queued/native ownership, heartbeat, startup result/log без UI pump, все5 outcome
вариантов и отсутствие автоматических retries. Timeout сохранил raw Busy до
cleanup и не был заменён поздним snapshot. Queue/held-native Dispose и оба
shutdown/cancel cases не сделали late StartService/publication. Foreign read
не был отменён или присвоен. Held logger не блокировал Close/native slot, его
реальный Task остался pending до release; failed logger не потерял service result.
Завершённый до Close result сохранён как исторический факт, не как auth proof.

Прежние37 SCM,32 probe/UI и66 start checks сохранены и прошли. Read-only oracle
00:22:14.6301635: runtime1/Stopped, startup4/Disabled, statusError0/configError0;
PGA-03A marker66 PASS00:22:19.4275654. Три SCM/probe production files в этой
порции не меняются. Stage/start/cancellation/cleanup решения остаются production,
только native boundary fixtures управляемы. Настоящие normal-owner child launches
в прежних Instance28 выполнялись на новом executable; сам факт раннего skip в
self-check/secondary и единственный hook после Attach также проверены source
review. Точный actual call count не выводится из допускающего пропуск SafeLog.

Native fixture.dotted config oracle PASS00:22:21.2908925, failure-only INFO не
появился; историческая причина CI556 остаётся UNKNOWN и finding не закрывается.
SOCKS41/403 PASS00:17:15.6256; Instance28 PASS00:23:01.5920933,
Maintenance95 PASS00:24:43.7612363, Shutdown47 PASS00:25:06.7589063,
updater5 PASS00:25:07.8381490; полный Test-ProGo PASS00:25:07.8385996.
Старые tests/conditions/budgets сохранены. Main/compiler после удаления одной
вставки каждый эквивалентны baseline. Program имеет только одну hook строку;
InstanceTests/ProxyService/auth/config/installer/workflows не меняются.

HomeVPN975 PASS00:27:43.1169650, portable90 PASS00:28:46.8312848;
Python24=22 PASS+2 прежних Linux-only skips, OK00:28:49.7249564. Все7 HTTP
profile-share cases выполнены ok. Public279 source/44 release PASS00:28:50.1780139.

### Ubuntu

16 recovery tests PASS00:12:51.0700535,9 sharing/sync PASS00:12:58.3964261,
46 isolated provision checks PASS,0 skips. Actual independent QR decode с
numpy2.2.6/zxing-cpp2.3.0; полный recovery/revoke/reissue/TLS/session tail достигнут.
Последний restart/session check PASS00:14:14.3723543. Только изолированное CI
окружение, без устройств/служб/ключей/ACL владельца.

### Logs и artifacts

Полные job logs сохранены UTF-8/LF для самостоятельного review:

| Job | Bytes | Chars | Lines | SHA-256 |
| --- | --- | --- | --- | --- |
| Windows | 938064 | 911015 | 8648 | `e231365f1066260591a6cc33e79372cd717634c23eebd0c80211fdfa6f4f8479` |
| Ubuntu | 54959 | 54705 | 573 | `a6b07434cdacf572c8593fbc51e09c0af5a3af16afc4c9227ee79df41a3b3539` |

Все4 artifact heads равны source27076552. ID/size/digest сопоставлены с actual
runner upload log и API metadata:

| Artifact | ID | Wrapper ZIP bytes | SHA-256 |
| --- | --- | --- | --- |
| ProGo-ci-diagnostics | 12082938774 | 89531 | `3da04cab80b91c6ba7cf0d9a3d36c13499fa58b089a4d524d9ff981abee3a431` |
| ProGo-release | 12082998644 | 539952 | `084fc6dddd0755c2fbd9358315d22cf4b77b5112988680cb9668400a4d01bf15` |
| ProGo-release-zip | 12083257719 | 531671 | `3028f79acf62ead570bc7091a3f28136594c9677c05a1dfc50a696b01fdebfef` |
| ProGo-wizard-screenshots | 12082948673 | 6538925 | `7bb41151be0b42b615c19ee9ec86be60a7ac0e2d7bf0f67f7308d6ee5a661db4` |

В этом пакете архивы не скачивались: нет локального root hash/ZIP entry inspection
или новой visual/DPI/contrast/Narrator приёмки. Runner сообщает293 screenshot
files, не локально проверенное число ZIP entries. UI layout не менялся.
Inner CI ZIP535116 bytes по runner, не итоговый installer31/31/installed update.

## Статус и следующий шаг

Узкий PGA-03B Code/Dev PASS. Кодовая часть службы/проверки/startup PGA-02/03
принята с указанными scoped доказательствами; это не end-to-end selected-key/
auth/UI/live приёмка. AutoStartSocks остаётся до agent startup: привязка к
готовности выбранного метода и корректный retry — PGA-05/06, полный agent UI
и действия — PGA-08, installer Automatic opt-in — PGA-04. Следующий небольшой
пакет — выбранный SSH профиль/его ключ и readiness без обязательного агента
для других разрешённых методов. Эти gates не скрыты в последующей live приёмке:
код и dev-проверки нужны до установщика в прежнем F31-R11.

Самостоятельный source/native review completed, независимого agent review нет.
55unique=43+12, accepted только F17-R02/R03 и F31-R01/R02/R03/R04:6/55,
осталось49=37+12,23/31. F31/F17/F31-R11 OPEN, R05 PARTIAL; Codex shortcut
ownership/disabled caption/UNKNOWN ssh-G556 открыты. Package PENDING,
Live NOT_CHECKED. Все10 PGA IDs и11 исходных acceptance scenarios сохранены.

После tested source — только4 Markdown evidence/status files. Runtime/tests/
scripts/server/workflows идентичны tested source; [skip ci] относится только к
этому evidence. PR draft/unmerged, без merge/release; VERSION0.2.2.
