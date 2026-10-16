# SSH-agent PGA-02B: bounded waiting и владение системным запросом

Baseline `618fd8e203159760c8d3274b7208ba6c3e3f808f`, VERSION0.2.2.
16.10.2026 UTC, следующая разрешённая владельцем микропорция.
[Контракт](SSH_AGENT_AUTOSTART_SPEC.md). Самостоятельный source review выполнен, native CI пока
**PENDING**. Это часть существующего F31-R11, не новый ID55.

## Поведение и границы

Один process-wide `SshAgentServiceProbe.Shared` допускает только один SCM read
вместе с его cleanup. Отмена/3s deadline завершают ожидание вызывающего кода,
а не принудительно выполняющийся Windows P/Invoke. Cooperative token передаётся
прежнему typed reader; после возврата OS вызова он прекращает последующие queries
и освобождает собственные handles/buffer. Slot остаётся занятым до завершения
raw worker Task. Повтор и другое диагностическое окно получают Busy и не создают
дополнительный native worker. Завершившийся старый snapshot не кешируется для
нового запроса; после полного settlement выполняется свежий read.

Отдельный background worker с Default scheduler не зависит от UI context.
Timer и cancellation registration принадлежат waiter и освобождаются после
ожидания. Native CTS принадлежит worker и освобождается только после его cleanup.
Непредвиденная ошибка/отсутствующий snapshot дают безопасный Unavailable без
raw exception/выдуманного Win32-кода. Результаты Completed/Busy/TimedOut/
Unavailable отделены от фактического runtime/startup snapshot PGA-02A.

`SshProfileDiagnostics.Check` использует общий probe (3s default). При отсутствии
подтверждённого snapshot сохраняет результат выбранного SSH config и откладывает
inventory ключей; это не запрещает другие рабочие методы SSH-аутентификации.
Shared argv/native config parser/подключение/fallback/SOCKS не меняются.
Старый text Read wrapper тоже использует общий bounded probe.

Окно проверки выполняет Check в worker, публикует результат только своему
живому UI owner. Close/Dispose завершают ожидание UI delivery без живого message
pump после фактического settlement CheckWork. Сам raw SCM может ещё выполняться
и остаётся в общем probe. Cancel ДО queued publication подавляет готовый результат;
CTS формы сохраняется до публикации либо закрытия, затем освобождается без
зависимости от UI pump. По окончании старого Run нельзя очистить CTS нового retry.
Текст различает лимиты SSH7s, ожидания службы3s и key inventory3s; физическая
остановка принадлежащих диагностике процессов имеет прежний отдельный budget.

Это read-only диагностика. Startup hook и попытка запуска службы по имеющимся
правам относятся к следующему PGA-03. Matching нужного ключа, interactive
ssh-add, новые agent UI действия и installer opt-in ещё впереди. Служба,
её policy, ключи, ACL, owner PC/VPS/телефон не меняются и не проверяются.
Не заявляется real-time гарантия при остановке планировщика/всего процесса ОС.

## Проверки

Новый Desktop partial использует настоящий typed reader и прежнюю native
boundary fixture; в нужном месте удерживается QueryStatus. Ожидаются33 checks:
валидация budget/pre-cancel, actual typed result/cleanup/background/context,
безопасный failure/null, timeout с raw pending,12 concurrent Busy, cooperative
cleanup и fresh retry, отмена без потери ownership, независимость Busy caller,
UI heartbeat/timeout/два окна/retry, queued cancellation, Close и Dispose без
UI pump, отсутствие поздней публикации. Два новых PNG показывают timeout/Busy.
Native oracle PGA-02A и все старые проверки/budgets сохранены. Check/compiler
получают по одной явной вставке. Ожидаемые Desktop4061=4028+33 не являются PASS
до фактического CI. Локального Windows runtime здесь нет; native результат
принимается только на изолированном CI, без действий на устройствах владельца.

Исходные55 IDs=43 до установщика+12 зависимых приёмок сохранены. Приняты только
F17-R02/R03 и F31-R01/R02/R03/R04: **6/55**, остаток49=37+12, общий23/31.
F31/F17 OPEN, F31-R05 PARTIAL; Codex ownership/disabled caption/неизвестный
ssh-G отказ CI556 остаются открытыми findings. Package PENDING, Live NOT_CHECKED.
PR draft/unmerged; merge/release не выполняются.

## Review перед native CI

Самостоятельно проверены: отсутствие mutation API; слот удерживается до raw Task
settlement, включая reader finally; waiter cancel не смешивается с native return;
failed/null worker не оставляет fault; native CTS не освобождается waiter;
Default scheduler/ConfigureAwait(false); cancellation registration/timer cleanup;
нет повторной выдачи abandoned snapshot; CTS нового Retry не очищается старым Run.
UI publication выполняется через BeginInvoke с owner gate и повторной проверкой
токена на UI; закрытие не зависит от message pump, но ждёт реального CheckWork.
Независимый review другим агентом в этой микропорции не выполнялся.

Локально public273/0 и whitespace PASS; inventory55unique=43+12/exact6accepted
сохранён. Проверка main/compiler после удаления по одной новой вставке даёт
полную эквивалентность baseline. Production SCM adapter, config parser,
DiagnosticProcess, ProxyService, Program, installer и все прежние test files
(кроме одной wiring строки main) не меняются.
