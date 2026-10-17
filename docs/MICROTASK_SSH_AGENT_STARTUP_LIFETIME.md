# SSH-agent PGA-03B: фоновый startup и lifetime

Baseline `d6a18d2914609eefd29b5428124a40658f3ea45e`, VERSION0.2.2.
16.10.2026 UTC. Узкая микрозадача внутри прежнего F31-R11; новых ID55 нет.
Самостоятельный source review; native CI PENDING. PASS пока не заявляется.

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
сохранение наблюдённого результата. Check count принимается только по native log.
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
