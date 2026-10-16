# SSH-agent PGA-03A: запуск службы с текущими правами

Baseline `df50e2ce8d44d71f6755def1be7ff4a5f09841b1`, VERSION0.2.2.
16.10.2026 UTC. Это узкая первая часть PGA-03 внутри прежнего F31-R11,
не новый ID55. Самостоятельный source review; native CI PENDING.

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

## Проверки до CI

Новый Desktop partial проверяет production reader/start/coordinator через
контролируемую native boundary: skip states/policy, минимальные rights,
same-handle races, accepted/Pending/Stopped, rejected/native failures,
unknown/exception, отмену на каждом значимом checkpoint, read-vs-start и
12 конкурирующих read/start после timeout, raw ownership/cleanup/fresh retry,
запрет позднего StartService при отмене в read, успешное завершение без caller
synchronization context. Количество checks принимается только по native log.

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
