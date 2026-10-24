# PGA-06D — явный запуск ssh-agent в диагностике SSH

**20.10.2026 UTC. Узкий Code/Dev PASS:** source `b02b0e9606fb57518ed18d17de42e5f82fca8fa8`, tree `7647304da881577f96a32dc1046aa6dbc95b0082`, merge `cf2e19691057c143b4f66d93c0041e2e1c5681a8` в прежний main `4bb336575bb1216d3047bd2b294aeecad9fdd65a`. [CI582](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39481464526) completed SUCCESS, Windows/Ubuntu SUCCESS. Счёт **6/55**,49=37+12, верхний **23/31** не меняется. PGA-06 и F31-R11 остаются OPEN.

## Что делает действие

В том же окне проверки SSH появилась кнопка «Запустить агент». Она доступна только после законченной локальной диагностики, если результат проверки службы подтверждает `Stopped`, startup mode не `Disabled` и нет ошибки чтения состояния. `Running`, `Missing`, `StartPending`, `Disabled`, Busy/timeout/Unknown дают безопасную причину и недоступное действие; неизвестный режим запуска при подтверждённом `Stopped` допускает попытку, поскольку права на запуск отдельно проверяет Windows.

Только нажатие вызывает существующий `SshAgentServiceProbe.StartAsync` с 3-секундным сроком ожидания. Он использует уже принятые StartService API и общий lease со startup, ключами и SCM-проверкой. Нет изменения startup policy, ACL, elevation/UAC, StopService, автоматического повтора или неявного запуска по Enter. Подтверждённое Windows принятие команды не объявляется готовностью ключа либо входом на сервер.

Во время операции остальные действия отключены, окно остаётся отзывчивым, Cancel/Escape прекращает ожидание; принятый Windows запуск отменой не откатывается. После timeout/cancel native вызов может продолжиться и **удерживает общий lease до фактического cleanup**. Close/Dispose не ждёт SCM на UI и не публикует результат в закрытое окно. После любой попытки отчёт показывает, почему прежние наблюдения сброшены, ключи и кнопка запуска недоступны до явной повторной проверки; результат и безопасный Win32-код остаются в статусе. Отказ в правах виден без raw exception и повторного запроса UAC.

## Проверки и визуальная ревизия

46 новых UI checks в существующем Desktop partial: семь комбинаций service runtime/startup/probe с недоступными кнопками; explicit keyboard path и minimum-size bounds; Running/AccessDenied Win32 5/Unavailable; отсутствие повторного Start без fresh check; heartbeat при зависшем SCM, timeout/cancel и удержание общего lease; close/dispose без UI pump и late writes; Cancel до queued публикации. Старые 40 Add Key checks сохранены. Desktop **4517=4471+46**. Production SCM не запускался в тестах: применён injected native boundary, а старый настоящий StartService oracle по-прежнему только проверяет invalid handle. Реальных ключей, агента владельца и сервера тесты не трогали.

В CI582: SOCKS recovery **41/403**, старый Add Key UI **40**, новый Start UI **46**, Desktop **4517**, Home VPN **975**, portable **90**; Windows и Ubuntu job SUCCESS, остальные шаги workflow SUCCESS. Публичный release artifact является промежуточной CI-сборкой, **не итоговым установщиком 31/31**.

Сверены имена, размеры, SHA256 и run ID четырёх final artifacts с API и upload log:

| Артефакт | ID | ZIP bytes | SHA256 |
| --- | ---: | ---: | --- |
| ProGo-release-zip | 12293436206 | 544373 | `84bd112a852eb6f526ca32d2c8a42df73a21645a46d38b291dc4f3f36703bfda` |
| ProGo-release | 12292977164 | 551663 | `491354a322dffdff5f44022a40afb00d5944a1c35ed9fe3496d9e5a29b21a6b6` |
| ProGo-ci-diagnostics | 12292297843 | 96792 | `6f6baac0b4bedd7c33a3c92b181a44953220086657374f848db55421592d9d0c` |
| ProGo-wizard-screenshots | 12292647592 | 6840844 | `d27240003fc42231fa796078d95f4f204fa3c161eeccfa8979f31d8812dde378` |

Скачан **только** screenshot ZIP; actual SHA256 совпал, в архиве305 записей. Все6 свежих `ssh-agent-start-{ready-minimum,pending,disabled,completed,denied,cancelled}.png` просмотрены: читаемые подписи/статусы, видимая кнопка при минимальном окне666×599, отсутствие заметного clipping или наложения. Остальные3 ZIP не скачивались и их содержимое не заявляется проверенным. Скриншоты создаёт synthetic harness с version0.0.0.0; настоящая build identity в логе `0.2.2+cf2e196...`. Независимого agent review не было; source/native/visual review самостоятельный. High DPI, contrast ratio, Narrator и реальное использование службы владельца NOT_CHECKED.

## Сохранённая история неуспехов

- [CI580](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39474744305), source `b98f9e7f83f19fb59ec55579198a617d5d3d5960`: attempt1 Windows FAIL до новых UI tests — старый `SocksRecoveryTests` ожидал fallback fixture более10 секунд, точная причина UNKNOWN; Ubuntu SUCCESS. Attempt2 Windows FAIL до новых UI tests — старый `AsyncSettingsColdStartHeartbeat`: немедленный rebind реального порта после Stop вернул AddressAlreadyInUse, ещё виден listener. Attempt3 Windows/Ubuntu SUCCESS, новый блок46, Desktop4517,6 PNG; это подтверждает первый вариант кнопки, но не исправляет две предыдущие ошибки.
- [CI581](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39480078167), source `565e1924ea45ebb216a95737ef796a96675f5a02`: после UX-пояснения Windows вновь FAIL в том же старом immediate rebind до SSH UI, Ubuntu SUCCESS. Этот run не принимается.
- В узком test-only изменении CI582 immediate rebind заменён ожиданием **до1 секунды с реальной exclusive bind проверкой** того же порта. Ни production Stop, ни native deadlines, ни startup/auth guards не менялись; при непрерывно занятом порте тест FAIL и печатает прежнюю диагностику. CI582 PASS подтверждает освобождение в пределах нового срока, но не устанавливает, какой именно поток или компонент удерживал listener в CI580/581. Эти причины остаются UNKNOWN; наблюдение фиксируется для последующей проверки устойчивости.

## Открытые границы

Selected-key possession/signing, certificate/FIDO constraints, auth readiness и порядок AutoStartSocks ещё не связаны end-to-end; ACL/permission policy и постоянные против временных SSH-отказов впереди. Подлинная служба, ключи и установка владельца не проверяются до готового пакета. F31-R11/F31/F17 OPEN, F31-R05 PARTIAL, Package PENDING, Live NOT_CHECKED; все исходные55 ID и11 сценариев сохранены. Следующая микрозадача — связать проверенное состояние выбранного ключа с дальнейшим SSH startup без ложного объявления аутентификации.
