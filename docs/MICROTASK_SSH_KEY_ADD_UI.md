# PGA-06C — явное добавление выбранного ключа в диагностике SSH

**19.10.2026 UTC. Реализация подготовлена, Code/Dev PENDING до completed CI.**
Родитель `6dcb41b0d09924ac36711a1b83bbd18347b4bb2f`, предыдущий принятый
runtime source `1000e1c057ce529909ab1ee791b838eb0395c2ff` / completed CI574.
Небольшой пакет внутри F31-R11: выбор identity и действие Add Key; lifetime/
cancellation/result publication UI. Счёт6/55, остаток49=37+12, общий23/31.

## Поведение

В SshDiagnosticForm добавлены native DropDownList выбора одного effective
IdentityFile, кнопка «Добавить ключ» и объяснение доступности. До завершения
диагностики действие недоступно. Если несколько ключей, требуется явный выбор;
если один, он показан выбранным, но добавляется только нажатием кнопки. Enter
не становится неявным default action. Не более32 уникальных identities.

Immutable request каждого показанного IdentityFile подготавливается в фоне
после диагностики, включая проверку sibling ssh-add; UI не выполняет File.Exists
или чтение ключа. Используется проверенный профиль окна, скопированный при
открытии. Позднейшее изменение исходного profile/result object не подменяет
путь/исполняемый файл выбранного request. Labels ограничены240 символами и
очищены от управляющих символов; полные пути остаются в отчёте диагностики.

Unknown/disabled/excluded publickey policy, IdentityAgent none/custom endpoint,
неподтверждённый путь и отсутствующий sibling utility дают отдельную безопасную
причину. Если нет пригодных кандидатов, combo и Add недоступны. Конкретная
доступность агента устанавливается самим ssh-add, не из service Running.

Нажатие запускает существующий PGA-06A backend в dedicated background worker.
Вся мутация/console cleanup сохраняет общий PGA-06B lease с SCM/startup/inventory/
fingerprints. Пока операция выполняется, повтор/выбор/Add недоступны; Cancel и
Escape отменяют принадлежащую операцию. Native timeout backend120s/cleanup2s
прежние. Парольная фраза вводится только в отдельной консоли OpenSSH; в приложении
нет поля пароля, stdin forwarding или capture console. Уже принятый агентом
ключ при отмене не удаляется. Startup не вызывает Add.

Typed outcome отображается без raw exception/output, с доступными числовыми
exit/native кодами. После **любой попытки**, включая Busy/error/cancel, отчёт и
key choices сбрасываются: нужно явно повторить диагностику перед следующим Add.
CommandCompleted не объявляет private-key possession/signing/server login.
Автоматического retry/повторной мутации/переключения на чужой агент нет.

Close/Dispose запрашивают отмену без ожидания native cleanup на UI. Work/KeyWork
отслеживают background lifetime до его фактического завершения; поздний callback
не обращается к закрытой форме. Cancel перед queued publication имеет приоритет
над уже завершённым success/готовым диагностическим result. Прежние CTS и
ownerClosed guards сохранены. Неканцелируемый OS file probe при подготовке
не выдаётся за принудительно остановленный: он остаётся своим background work,
без публикации в закрытый UI.

## Проверки

Новый workflow в уже подключённом Desktop partial, ожидается **40 checks**,
Desktop4471=4431+40. Старые assertions/watchdog/workflows не ослаблены.

- 9 policy/path/helper/no-key вариантов с specific reason/no mutation.
- Multi-key explicit selection, immutable request, background work и heartbeat.
- Native keyboard Tab/Shift+Tab, accessible names/descriptions/Cancel; minimum
  layout и отсутствие default Enter mutation.
- Pending cancellation держит настоящий production key lease против Shared SCM;
  duplicate Add/Retry не запускаются; late success подавлен, нужен fresh check.
- 6 typed outcomes, числовые коды, invalidation прежней evidence.
- Close/Dispose, release после физического cleanup, отсутствие late UI writes и
  settlement без UI pump; queued Add/diagnostic publication cancellation.

UI fixtures используют fake sibling files и production SshAgentKeyAdd.Run с
управляемым runner boundary; реальные ключи и системный агент не используются.
Реальный OpenSSH encrypted-key/console oracle остаётся в прежних45 backend checks
полной CI матрицы. Одно составное UI→настоящий системный агент→server login
не заявляется. Подготовлены6 UI screenshots: unavailable, ready-minimum, pending,
cancelled, completed, agent-error. Native/visual PASS до CI/просмотра не заявлен.
Локального Windows/.NET runtime нет. Source review самостоятельный;
независимого agent review не было.

## CI575: FAIL старого measurement до новых UI checks

Source `6d915c8a59e4f58a51c36f439a4bf0e1de040922`, tree
`b1fabe11eb9cebf0dd46367c033ecbb70877d4e9`, merge
`547edf14a8bc59fd95055b375b058b237ebaa1b8` в прежний main4bb3365.
[CI575](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39218163456)
completed FAILURE: Windows117946847857 FAILURE, Ubuntu117946848235 SUCCESS.
07:51:43.1875043UTC прежний native measurement assertion: sample-timeout,
samples6, working/private/io=True. Новые40 UI checks NOT_RUN, не PASS/FAIL.
Безопасная диагностика, добавленная после570, теперь установила failing condition:
stop не completed. Конкретный native вызов и оставшийся budget в575 не записаны;
они UNKNOWN, предыдущие570/572 причины не переопределяются.

В Collect выявлен отдельный дефект планирования: цикл допускал очередной sample
в частичном последнем interval, даже с остатком1ms. Исправлено admission правило:
если общего времени осталось меньше interval, завершаем окно с отменяемым
ожиданием остатка без нового native probe. Уже начатый заблокированный sample
по-прежнему даёт sample-timeout; inventory timeout/cancel/failure не маскируются.
Нет retry, увеличения budgets или ослабления native assertion. Ноль/недостаток
образцов по-прежнему NOT_CHECKED в отчёте, не подтверждённые метрики.

Public Collect сохраняет прежний контракт и настоящий Stopwatch/WaitHandle.
Internal core принимает часы/ожидание для2 детерминированных regression cases:
350ms/100ms,3 samples, последний wait оставляет1ms либо запрашивает отмену;
четвёртый native sample запрещён fixture. Проверяются completed/cancelled без
таймингов host. Measurement ожидается26=24+2; Desktop по-прежнему4471=4431+40.
Это воспроизведение исправленного алгоритмического дефекта, **не доказательство**,
что именно он вызвал575. UI/SSH source не меняется этим исправлением; нужен новый CI.
CI575 не перезапускается, его FAIL сохранён.

Полные нормализованные логи575: Windows2020lines/174796bytes,
SHA256 `235ba89fa865cf4d5a5b7ef9a27b5e2d1f19575814c52ab5bc6b0b944fa5bf96`;
Ubuntu573lines/53276bytes,
SHA256 `5e6d5b75d775178cbd38801316581aa965c2484a03b12ba92f380d48b274b453`.

## Оставшиеся границы

Selected-key binding/signing/constraints/cert/FIDO/custom endpoint, service Start
action в UI, auth readiness/AutoStartSocks ordering, ACL diagnostics/repair и
полная PGA-08/PGA-10 приёмка остаются впереди. Старый «Первый вход» не заменён.
PGA-05/06/F31-R11/F31/F17 OPEN, R05 PARTIAL, Package PENDING/Live NOT_CHECKED.
Все55 IDs/10 PGA rows/11 acceptance scenarios сохраняются. Истории FAIL555/556/
562/565/568/570–573 и UNKNOWN причины не переопределены. ПК/VPS/телефон/установленная
программа/реальные ключи/ACL/платежи владельца не проверялись и не менялись.
PR88 draft/unmerged, VERSION0.2.2, без merge/release.
