# Матрица реализации и доказательств ProGo F01–F31

## Baseline до новых исправлений утверждённого плана

Дата фиксации: **11.10.2026**, после утверждения
[APPROVED_PLAN_31.md](APPROVED_PLAN_31.md), до свежих изменений интеграции.
Точное основание: **`a41bbcf41ab17a79245f4f5ae14cfd4c0b52f690`**,
[PR #87](https://github.com/rkhnorkhan-bit/ProGo/pull/87).
[Проверенный запуск 38083031654](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38083031654):
**Windows CI — FAIL; серверный CI — PASS**. Часть Windows suites после раннего
отказа не запущена. Частичный PASS не подтверждает непройденные/пропущенные suites.

Опубликованный продукт: **v0.2.2**, main/tag
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`. Сохранённая цепочка аудиторских
исправлений ещё не включена в этот опубликованный выпуск. Установленная у
конкретного пользователя версия этим документом не определяется.

История: **22/31**; последним исторически принят F29 после CI и снимков.
Открытые девять: **F09, F16, F17, F19, F22, F23, F27, F30, F31**.
Это не свежий PASS общего кандидата и не счёт принятой пользовательской установки.
Прежние сообщения о локальной готовности после очистки временных рабочих копий
не считаются сохранённым кодом, пока не сопоставлены с коммитами/составом кандидата.

## Значение слоёв

| Слой | Значение |
| --- | --- |
| Code | Наличие исходного кода baseline и объём незавершённого утверждённого изменения; наличие файла не равно работоспособности. |
| Dev | Свежие обязательные проверки пункта на **одном точном итоговом commit**, включая Windows/native и необходимые интеграции. Исторический CI учитывается отдельно. |
| Package | Включение в готовый цельный setup/updater одной версии/commit и проверка чистой установки, обоих путей обновления и отката на стенде. |
| Live | Проверка после установки владельцем и подтверждение нужных внешних зависимостей; прежде этой установки такие проверки не требуются от владельца. |

Обозначения кода: **ЕСТЬ** — сохранённая историческая реализация требует
повторной проверки; **ЧАСТИЧНО** — есть предыдущие подэтапы, но утверждённые
изменения/исправления не завершены; **ПРЕДСТОИТ** — согласованный новый контур
не реализован. Ни одно из обозначений не является PASS.

Обозначения проверок: **NOT_CHECKED** — полный критерий свежей общей сборки
не подтверждён; **FAIL** — известный непройденный критерий baseline;
**PENDING** — цельный новый пакет ещё не готов. Успех серверного запуска выше
не распространяется на Windows, оплату, почту, подпись или пользовательскую среду.

## Матрица baseline

| Пункт | История | Code на baseline | Dev | Package | Live |
| --- | --- | --- | --- | --- | --- |
| **F01 — Общий CLI/Codex** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F02 — Автоматика и повторы** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F03 — Один экземпляр** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F04 — Честный статус связи** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F05 — Один клик и SSH-вход** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F06 — Возврат proxy-настроек** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F07 — Сохранность копий** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F08 — Транзакционный restore** | Принят | ЧАСТИЧНО | FAIL: отмена/очистка restore | PENDING | NOT_CHECKED |
| **F09 — Настоящий и ложный vault** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F10 — Жизненный цикл bridge** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F11 — Навигация и команды** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F12 — Область отключения** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F13 — Проверка маршрута** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F14 — Фоновая диагностика** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F15 — Save и немедленные действия** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F16 — Адаптивность и DPI** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F17 — Доступность** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F18 — Выдача и установка** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F19 — Условия подключения** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F20 — Управление друзьями** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F21 — Очистка clipboard** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F22 — Копии и перенос** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F23 — Обновление и installer** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F24 — Помощь и версия** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F25 — Журналы и экспорт** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F26 — Автозапуск** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F27 — Подпись и антивирусы** | Открыт | ПРЕДСТОИТ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F28 — Команды и поддержка** | Принят | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F29 — Визуальный стиль** | Принят | ЕСТЬ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F30 — Почта, кабинет, биллинг** | Открыт | ПРЕДСТОИТ | NOT_CHECKED | PENDING | NOT_CHECKED |
| **F31 — SSH, переносимость, скорость** | Открыт | ЧАСТИЧНО | NOT_CHECKED | PENDING | NOT_CHECKED |

Классификация Code относится к утверждённому объёму, а не к отсутствию всех
подэтапов. Например, F23j и F31a/F31b имеют прежние отдельные доказательства,
но весь F23/F31 остаётся незавершённым; F30 ранее имел приглашения, а не полный
почтовый/платёжный/агентский контур. F27 имеет прежнюю проверку хеша, но это не
новый signing pipeline и не подписанный выпуск.

## Доказательства, которые надо получить

| Область | Требуемое доказательство |
| --- | --- |
| Общая сборка | Устранён текущий Windows FAIL; каждая suite имеет результат, а непроверенный хвост не скрывается итоговым сообщением. Scenario counts и assertion counts различаются. |
| F06/F08/F10/F12/F15/F21 | Новые регрессии владения, чужих правок, отмены, relay half-close, cleanup, конкурентной записи и занятого буфера на общей сборке. |
| F09/F23/F30 | Полные утверждённые сценарии, включая рестарт/полночь, готовый installer и проверенные серверные платежные события/возвраты/активацию. |
| F16/F17/F29 | Native снимки новой общей сборки, настоящие dev Windows DPI/контраст/Narrator где доступны; причина каждого недоступного окружения записана. |
| F22/F31 | Чистый защищённый перенос между пользователями/ПК, slow disk/network fixtures, реальные замеры там, где они возможны; реальные SSH-agent/VPS/Rescue владельца — после установки. |
| F27 | Signing pipeline отдельно; реальный сертификат после тестов, проверяемые подписи и отдельные Defender/Kaspersky результаты. |
| Поставка | Setup и updater одной версии/commit, полный runtime, чистая установка, фиксированные текущие пути обновления, self-check, сохранность личных данных, подтверждённый rollback. |

## Обновление матрицы

Каждое изменение статуса должно иметь:

1. Точный commit SHA и пункт/подкритерий утверждённого плана.
2. Команду либо CI run/artifact с проверенным результатом; причины FAIL, SKIP и
   NOT_CHECKED сохраняются.
3. Для UI — ссылку на просмотренные native снимки; для пакета — версию, состав,
   digest и результаты установки/обновления/отката.
4. Для Live — конкретную среду и проверенный сценарий без публикации секретов.

Ранние записи [AUDIT_PROGRESS.md](AUDIT_PROGRESS.md) сохраняют исторические
результаты. Их «accepted/closed» не переносится автоматически на новую матрицу.
После нового commit статусы baseline не редактируются задним числом: ниже
добавляется новая секция с новым SHA и доказательствами. Документационная правка
сама по себе не закрывает ни один функциональный критерий.


## Контрольная точка 01 — отмена подготовки восстановления

Код: **`7440ec5eda97bf37708583af079be3694ea0039c`**,
[черновик PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[Запуск 38146890018](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38146890018):

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Сборка C# приложения и desktop harness на Windows | PASS | Native job `114492030367` |
| Отмена linked-token, однократная очистка после worker, отсутствие зависимости от UI pump | PASS | Новые строгие fixtures `DesktopAsyncRestoreTests` |
| Настоящая блокировка временного файла, видимая ошибка очистки, сохранность источника | PASS | Native locked-stage fixture |
| Принятая передача restore helper и поздняя отмена | PASS | Native helper acknowledgement fixture |
| Общий Windows CI | FAIL | Последующий `UiControls -> ServiceCommandDialogs`: ожидаемый диалог `RestoreBackup` не обнаружен |
| Хвост suites после этого отказа | NOT_CHECKED | Текущий sequential harness прерывается на ошибке |
| Изолированный VPN server CI | PASS | Job `114492030575` |
| Новый installer / пользовательская установка | PENDING / NOT_CHECKED | Цельный пакет всех31 ещё не готов |

Эта микрозадача не закрывает F08/F31 целиком и не меняет исторические **22/31**.
Следующий небольшой шаг — отдельно разобрать ожидание асинхронного
`RestoreBackup` из меню и затем независимые результаты suites. Уже подготовленные
правки других модулей и незавершённый F09 сохранены вне данного checkpoint.

## Контрольная точка 02 — асинхронная команда восстановления из меню

Код: **`a2746268ca1ec74b82839e9ab4268b68bc7eb05d`**,
[черновик PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[Запуск 38148211645](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38148211645):

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Windows C# compilation | PASS | Native desktop harness выполнялся на проверяемом commit |
| Настоящий RestoreBackup из tray, появление асинхронного chooser | PASS | Job `114495929814`, 14:46:06 UTC |
| Отмена, завершение tracked task, снятие ownership, отсутствие подготовки/запуска helper | PASS | Native checks `ServiceCommandDialogs`; прежний предел ожидания сохранён |
| Сохранность settings и backup; закрытие completed progress; следующая ExportDiagnostics | PASS | Native checks, 14:46:06–14:46:07 UTC |
| Общий desktop suite | FAIL | Watchdog: 90 секунд без успешной assertion; последний attempted check `actual settings save completes while background ownership is busy: False`, `AsyncCliStartup -> SettingsSaveConnectionIntent`, `DesktopStartupTests.cs` |
| Общий Windows CI | CANCELLED / TIMEOUT | Desktop сообщил отказ в 14:49:32 UTC, enclosing step не завершился до job limit 40 минут; отмена в 15:21:52 UTC |
| Последующие suites | NOT_CHECKED | Нет доказательства успешного завершения; cleanup зафиксировал orphan `SocksRecoveryTests`, причина удержания CI пока не установлена |
| Изолированный VPN server CI | PASS | Job `114495929607` |
| Новый installer / пользовательская установка | PENDING / NOT_CHECKED | Полный пакет всех 31 ещё не готов; пользовательская среда не проверялась |

Микрозадача меняет только тест ожидания tray dialog и его cleanup, без изменения
application code. Она прошла свои native checks, но не закрывает F01–F31 и не
меняет исторические **22/31**. Следующий отдельный шаг — проверка сохранения
настроек при занятом фоновом владельце и корректный выход modal fixture при
отказе. Завершение CI после failure требует отдельного ограниченного исправления.

## Контрольная точка 03 — асинхронное сохранение и выход modal fixture

Код: **`be52b781938c14a6a5b748ea74a89ef7b3393ec1`**,
[черновик PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[Запуск 38153684615](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38153684615):
CI проверял synthetic PR merge **`406a08b33c2a17730ad8ef374a127061d9382b74`**,
объединяющий указанный head с baseline `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Windows C# compilation | PASS | Native job `114512152421` |
| Ошибка inspection при pending Save: отмена, task/UI settlement, повторное закрытие, исходное исключение | PASS | Native regression, 16:07:29 UTC |
| Намеренно незавершающийся synthetic Save: пятисекундный timeout, сохранность обеих ошибок, bounded Dispose modal, последующее settlement | PASS | Native regression, 16:07:30–16:07:35 UTC |
| Неизменность settings bytes/revision и снятие route subscription после обоих отказов | PASS | Native failure-path checks |
| Save dispatch <400 ms; async acceptance при удерживаемом исходном proxy gate | PASS | Оба случая `stopped=false/true`, 16:07:37–16:07:40 UTC; UI deadline и gate wait не увеличены |
| Новый PID/порт после reconnect; explicit Stop не восстанавливается сохранением | PASS | Native checks, 16:07:39–16:07:41 UTC |
| Весь DesktopTests | PASS | 3485 checks, 16:08:00 UTC |
| Весь Test-ProGo.ps1, включая Instance/Maintenance/Shutdown/UpdatePackage | PASS | Итоговый `ProGo tests PASS`, 16:10:57 UTC; после desktop больше нет прежнего NOT_CHECKED |
| Общий Windows CI | FAIL | Поздний `Test-HomeVpnRelay.ps1 -> HomeVpnOwnerWaitingTests`, сценарий `Outputs(): oversized/revoke`: `Owner native UI controller failed`, 16:13:59 UTC |
| Оставшийся хвост wizard после owner fixture, portable harness, Windows Python relay integration | NOT_CHECKED | Не достигнуты после указанного отказа |
| Финальный public release-content scan, pack/upload | SKIPPED | Не выполнялись после позднего отказа; local source scan отдельно PASS |
| Изолированный VPN server CI | PASS | Job `114512152566` |
| Новый installer / пользовательская установка | PENDING / NOT_CHECKED | Полный пакет всех 31 ещё не готов; пользовательская среда не проверялась |

Микрозадача меняет только ожидание async Save и cleanup тестового modal, без
изменения application code. Прежние исторические **22/31** не пересчитываются:
native PASS этих fixtures и desktop suite не закрывает целиком F01–F31.
Следующий отдельный шаг — установить причину отказа `oversized/revoke` и состояние
его task/process cleanup. Уже подтверждено пересечение бюджетов: controller
запускает свои 20 секунд раньше операции, которой тоже предоставлены 20 секунд.
Возможный дефект обработки большого stdout приложением пока не доказан.
Общее ограничение CI-процессов остаётся отдельным follow-up.

## Контрольная точка 04 — ограничение SSH stdout до завершения команды

Код: **`f3ad1c4458d86b4a96f6807ed94be75fd7ae8b43`**,
[черновик PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[Запуск 38156158147](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38156158147)
проверил synthetic PR merge `27fba56ac9c2d9b84326deda8ca84d12ff18acbd`,
включающий этот head и baseline `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Windows C# compilation | PASS | Native job `114519455884` |
| Точные 32/64 КиБ без newline, исходные UTF-8 bytes | PASS | Две native boundary cases, 16:47:00–16:47:01 UTC |
| Превышение на один байт, ограниченный файл, завершение live descendant | PASS | Обе boundary cases, 16:47:00–16:47:02 UTC; прямой transport timeout не может пройти assertion типа ошибки |
| Настоящие console stdin/stderr handles и UI heartbeat | PASS | `GetConsoleCP`, `GetConsoleMode`, native ticks; реальный интерактивный VPS-вход этим не проверяется |
| Миллион символов: revoke/repair/share | PASS | 16:47:06–16:47:07 UTC; отказ раньше десятисекундного SSH deadline, без его расширения |
| AcceptedCancellation и итоговые owner checks | PASS | 16:47:07 UTC; unrelated process сохранён, private dispatch work очищен |
| Весь DesktopTests / весь Test-ProGo.ps1 | PASS / PASS | 3485 checks в 16:42:37 UTC; итоговый `ProGo tests PASS` в 16:45:03 UTC |
| Общий Windows CI | FAIL | Поздний `HomeVpnShareRecoveryTests.RecoverForm("complete", false, false)`: `HTTPS native fixture failed`, 16:47:52 UTC; inner exception в логе скрыт |
| Остаток HTTPS/wizard, portable harness, Windows Python relay integration | NOT_CHECKED | Не достигнуты после этого отказа |
| Финальный public release-content scan, pack/upload | SKIPPED | Local public-source scan отдельно PASS; цельного принятого installer нет |
| Изолированный VPN server CI | PASS | Job `114519456030` |
| Native screenshots / diagnostics | UPLOADED | Artifacts `11709315308` / `11708374771`; новая визуальная приёмка не выполнялась |
| Пользовательская установка, ПК/VPS | PENDING / NOT_CHECKED | Отложены до цельной сборки |

Микрозадача устраняет доказанное отсутствие раннего output-cap. Точная причина
прежних 20 секунд ожидания не заявляется установленной. Исторический **22/31**
остаётся неизменным; весь F20/F31 и другие полные пункты этим не закрываются.
Следующий отдельный блокер — завершение успешного HTTPS recovery-диалога и
предикат его fixture. Два предыдущих сценария с ошибками проверки/сохранения
прошли; `ConfirmedCleanupFailure`, `DisposedVerification` и `ProofAndStorage`
после успешного recovery ещё не выполнялись.

Ближайший путь к двум дополнительным завершённым Code/Dev-пунктам — **F09**
(совместимость настоящего vault, честные ошибки, суточное ложное состояние,
атомарное сохранение и expiry вне UI) и **F22** (полный runtime repair и
защищённый перенос на dev-стенде между разными Windows-пользователями).
Существующий portable child использует тот же пользовательский token и сам
по себе не доказывает перенос между пользователями. Repair пока не восстанавливает
`InstalledUpdateTransport.cs`, `UpdateInstallSession.cs` и `scripts/home-vpn/*`;
пропущенные источники и проверка одного exe не подтверждают полный repair.
Эти остатки требуют самостоятельных микрозадач и проверок; **24/31 пока нет**.

## Контрольная точка 05 — ожидание фактического результата HTTPS recovery

Код: **`778fff7a2948ad75d0f13c0c86ab63fdef74671c`**,
[черновик PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[Запуск 38158069018](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38158069018)
проверил synthetic PR merge `21d3efa17622e50c636f38fa3f7b58aefcd46f24`,
включающий этот head и baseline `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Два независимых source review, whitespace, local public-source scan | PASS | Публичных исходников 206; release-файлы этим запуском scanner не проверялись |
| Windows C# compilation | PASS | Native job `114525051269` |
| Весь DesktopTests / весь Test-ProGo.ps1 | PASS / PASS | 3485 checks, 17:10:44 UTC; итоговый `ProGo tests PASS`, 17:13:41 UTC |
| Предыдущие SSH output-cap и итоговые owner checks | PASS | Включая oversized revoke/repair/share, 17:15:58–17:16:00 UTC |
| Исправленный RecoverForm, включая complete success | NOT_CHECKED | Не достигнут: все RecoverForm идут после PendingGate; в логе нет ни одной новой terminal-state assertion |
| Общий Windows CI | FAIL | Более ранний PendingGate: `HTTPS native fixture failed [unlabelled]: NullReferenceException`, 17:16:12 UTC |
| Остаток HTTPS/wizard, portable harness, Windows Python relay integration | NOT_CHECKED | Не достигнуты после указанного отказа |
| Финальный public release-content scan, pack/upload | SKIPPED | Local source PASS не подменяет проверку release |
| Изолированный VPN server CI | PASS | Job `114525051002` |
| Native screenshots / diagnostics | UPLOADED | Artifacts `11709528169` / `11709453402`; визуальная приёмка не заявляется |
| Новый installer / пользовательская установка и ПК/VPS | PENDING / NOT_CHECKED | Отложены до цельной сборки |

В fixture успех теперь определяется по `DialogResult.OK`, ошибки — по возврату
доступного поля. Ожидание lease и три последующих timer ticks сохраняют UI pump;
все прежние постусловия и исходный 20-секундный deadline сохранены. Application
code не изменялся. Native PASS этой правки пока **не подтверждён**.

В PendingGate трижды повторилась одна assertion за 33 мс. Settled callback
вызывает `Application.DoEvents`, а Loop останавливает timer только после него.
Это указывает на повторный вход завершения и закрытие формы вложенным tick;
точная строка NullReferenceException не подтверждена stack trace. Следующая
отдельная микрозадача — однократное завершение fixture, сохранённый failure
cleanup и regression с намеренной вложенной обработкой сообщений. Подробности:
[MICROTASK_HTTPS_COMPLETION.md](MICROTASK_HTTPS_COMPLETION.md).

Исторические **22/31** не меняются. Новые полные F-пункты не закрыты;
**24/31 пока нет**. F09 и F22 остаются отдельной реализацией после этого блокера.

## Контрольная точка 06 — однократное завершение HTTPS fixture

Первая попытка: код `5c943c67d98fdba67efb36ed35e565a088d689a5`,
[CI 38160835465](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38160835465),
synthetic merge `faa4c6460c187c6d8b1c2c7ba7890e692f03f200`.
Основной Windows-набор и server прошли. Первый sentinel case прошёл, но следующий
success case упал через примерно 10 мс: регрессия обнаружила проблему нового
message loop после abort. PendingGate и все RecoverForm не были достигнуты.
Этот результат не скрывается успешным повторным targeted прогоном ниже.

Исправленный код: **`a66a25ed079240f3d73ff34ca5c39466f451eb82`**,
[черновик PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
[Повторный запуск 38161800807](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38161800807)
проверил synthetic PR merge `acb2c84777346dcf99f6285ee0e305c259691ccf`,
включающий этот head и baseline `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Два независимых source review, whitespace, local public-source scan | PASS | 207 исходников, zero release files; application code не изменён |
| Windows C# compilation | PASS | Native job `114536093148` |
| Вложенный UI pump, sentinel failure и следующий success loop | PASS | 18:09:59 UTC; в обоих cases 1 inspection, 3 heartbeat pulses, live controls, disposed form; 157/156 мс, исходный sentinel сохранён |
| Actual PendingGate, однократная layout-inspection | PASS | 18:10:09 UTC; journal/binding/draft сохранены, новый share не запускался |
| Все 12 RecoverForm scenarios микрозадачи 05 | PASS | 18:10:11–18:10:31 UTC; 12 acknowledgement + 12 terminal-state + 12 status/result-only assertions; число assertions не является числом scenarios |
| Успешный complete/verify=False/save=False | PASS | 18:10:31 UTC; exact receipt/origin/HTTPS/save consume only matching request |
| Весь DesktopTests / весь Test-ProGo.ps1 | PASS / PASS | 3485 checks, 18:04:22 UTC; итоговый `ProGo tests PASS`, 18:07:28 UTC |
| Предыдущие SSH output-cap и итоговые owner checks | PASS | Включая oversized revoke/repair/share; завершение owner work 18:09:58 UTC |
| Общий Windows CI | FAIL | Позже: combined assertion `ConfirmedCleanupFailure`, 18:10:33 UTC |
| DisposedVerification/ProofAndStorage, HTTP/wizard/setup-recovery tail, portable и Windows Python integration | NOT_CHECKED | Не достигнуты после указанного отказа |
| Финальный public release-content scan, pack/upload | SKIPPED | Local source PASS не подменяет release-проверку |
| Изолированный VPN server CI | PASS | Job `114536093329` |
| Native screenshots / diagnostics | UPLOADED | Artifacts `11710928715` / `11710933727`; визуальная приёмка не заявляется |
| Новый installer / пользовательская установка и ПК/VPS | PENDING / NOT_CHECKED | Отложены до цельной сборки |

Контроллер запрещает повторный вход, останавливает timer до settled inspection,
немедленно очищает ресурсы при ошибке и сохраняет исходное исключение. Он завершает
собственный ApplicationContext, если main-form disposal ещё не завершил его;
дублирующий ambient ExitThread устранён. Deadline 20 секунд и строгие регрессии
2/5 секунд сохранены. Точный механизм native quit queue напрямую не наблюдался.

Следующий отдельный блокер содержит доказанное устаревшее ожидание fixture:
ConfirmedCleanupFailure требует `previous.example.org` после того, как успешный
RecoverForm сохранил `qr.example.org`. Reset не восстанавливает persisted origin.
Остальные условия combined assertion пока не установлены. Нужны изолированная
подготовка прежнего origin, отличного от целевого, до snapshot, его фиксация
и именованные безопасные diagnostics с сохранением
всех cleanup/proof/journal/no-replay проверок. Application cleanup defect этим
отказом не подтверждён. Подробнее:
[MICROTASK_HTTPS_FINALIZATION.md](MICROTASK_HTTPS_FINALIZATION.md).

## Контрольная точка 07–08 — очистка HTTPS и безопасное чтение vault

Пакет из двух микрозадач: код **`00915014df63213524ee793daa2d0a0eec8c852c`**,
включает micro07 **`3c24eaccf5020bfd3450a001710c3b5e2277a713`**.
[CI 38164605392](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38164605392)
проверил merge **`e4098fdbc1d70cb823c5f4b2d79d61af020b3636`** с main
**`4bb336575bb1216d3047bd2b294aeecad9fdd65a`**. Результаты независимо сверены
по завершённым журналам и состоянию jobs.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Windows compilation и отдельный vault harness | PASS | 165 checks, job `114544444410`, 18:38:14 UTC; прежние ASCII/Unicode PIN и v1, повреждения/лимиты/ошибки, реальные read/replace locks, эксклюзивный Create и неизменность файлов |
| Desktop и полный `Test-ProGo.ps1` | PASS | 3485 desktop checks в 18:43:48; полный script PASS в 18:46:26, включая Instance/Maintenance/Shutdown/UpdatePackage |
| Controller, PendingGate и 12 RecoverForm | PASS | Обе вложенные UI-pump регрессии, exact-once и все 12 recovery scenarios на этом же кандидате |
| Micro07: locked HTTPS cleanup | PASS | Все четыре строгие проверки в 18:49:01; `pendingTicks=32`, `settledMs=1034`, исходный `checkedMs=1039<2500`; proof/journal/inputs/helper/origin сохранены, verify/save=0, noReplay и safe UI |
| Хвост HTTPS/HTTP/wizard/SetupRecovery | PASS | Disposed verification в двух вариантах, proof/storage и остальные сценарии; весь Home VPN harness — 975 checks, 18:49:17 |
| Portable | FAIL после 14 PASS | 18:49:32: составная проверка импорта отдельным процессом с паролем через stdin; нарушенный операнд и причина по журналу не установлены |
| Хвост portable и Windows Python relay discovery | NOT_CHECKED | Child-data comparison, installed reload, tamper/length/password checks и последующие portable suites не выполнены после отказа |
| Общий Windows CI / изолированный server CI | FAIL / PASS | Jobs `114544444410` / `114544444301`; частичные успехи не означают PASS общего кандидата |
| Public release scan, упаковка, release uploads | SKIPPED | Windows остановился до этих шагов; отдельный локальный source scan PASS: 210 sources, 0 release files |
| Снимки и диагностика | Загружены | 277 screenshots, artifact `11711099065`; diagnostics `11711971245`. Новая визуальная приёмка не заявляется |
| Полный installer / пользовательские ПК/VPS | PENDING / NOT_CHECKED | Пакет всех 31 ещё не готов; среда владельца не проверялась |

Micro07 исправляет независимый baseline теста и его безопасную диагностику,
сохраняя все исходные условия и сроки. Micro08 отделяет mismatch MAC от I/O,
JSON, структуры и неподдерживаемых параметров, ограничивает чтение до KDF,
исключает implicit Create при чтении и перезапись существующего файла при
явном Create. Writer/KDF/Unicode PIN сохранены; casing aliases и ошибочные
Unicode escapes проверены отдельными authenticated fixtures.

Это первый участок F09: суточный ложный PIN, отдельные ложные данные,
рестарт/полночь и асинхронный UI ещё предстоят. Перенос под другим Windows
пользователем/на другой ПК тоже не подтверждён. Целевые micro07/micro08 имеют
native PASS, но полные F09/F22 не закрыты; история **22/31** неизменна.
Следующий небольшой пакет должен разобрать фактический отказ child-import
без предположений по составной assertion и продолжить отдельные части F09.

Подробности: [MICROTASK_HTTPS_CLEANUP_BASELINE.md](MICROTASK_HTTPS_CLEANUP_BASELINE.md)
и [MICROTASK_VAULT_READ.md](MICROTASK_VAULT_READ.md).

Targeted native micro05/06 теперь **PASS**, общий Windows-кандидат остаётся
**FAIL**. Исторические **22/31** неизменны; **24/31 пока нет**. Полные F-пункты
и цельный installer не закрыты. F09/F22 по-прежнему требуют собственной реализации.

## Контрольная точка 09–10 — stdin импорта и суточная политика PIN

Пакет из двух ограниченных микрозадач. Micro09 diagnostic
`1c5285b5793576d30fbdcf1a7890fd57335a2d41` и micro10
`8453e9e97d50fce71c4553e63371577210b91a07` проверены в
[первом CI 38167482711](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38167482711),
merge `ed2bab8d395ec2a2ffcad8bb90f7e8e3bd50acb4`.
165 reader, 134 daily policy, 3485 desktop, весь Test-ProGo и 975 Home VPN
прошли. Новый EOF child case упал в 19:30:34 UTC: парольная строка не
передавалась, но процесс перешёл к `prepare/portable` вместо `stdin/empty-input`.
Exit=1, stdout=0, stderr=53 символа, PASS отсутствует, truncation=False.
Это подтверждённая ошибка границы stdin в fixture; точные runtime кодировки
и переданные байты не наблюдались. Windows `114552949650` — FAIL,
server `114552949771` — PASS. Исходный отказ сохранён в отчёте.

Исправление **`f7473015cecf10677e26596bc03c6e12207cf2de`** использует отдельный
BOM-aware reader stdin в тестовом процессе и восемь memory vectors.
[Повторный CI 38168506823](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38168506823)
проверил merge **`7da51d27216df625f9c8779a45d040e8539ff0b3`** с прежним main
**`4bb336575bb1216d3047bd2b294aeecad9fdd65a`**. Production archive code,
критерии успешного импорта и исходные сроки сохранены.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Native compilation / reader / daily policy | PASS / 165 / 134 | Windows `114555971100`, 19:34:03 UTC; отдельные guards исключают реальные app paths и logging |
| Desktop / весь Test-ProGo | 3485 / PASS | 19:40:33 и 19:43:36 UTC; Instance/Maintenance/Shutdown/UpdatePackage выполнены |
| Controller, PendingGate, RecoverForm и locked cleanup | PASS | Все прежние строгие регрессии; pendingTicks=32, settledMs=1062, checkedMs=1068<2500; proof/journal/origin/helper/inputs/noReplay сохранены |
| Весь Home VPN harness | PASS, 975 | 19:46:47 UTC, включая ранее достигнутый recovery/HTTP tail |
| Micro09: 8 transport vectors и 4 child cases | PASS | EOF, wrong password, existing target и success; сохранность файлов и отсутствие secret output проверены |
| Реальный отдельный child import + DPAPI comparison | PASS | 19:47:16 UTC; exit=0, stdout=37 символов, stderr=0, PASS присутствует, truncation=False |
| Весь portable harness | PASS, 90 | 19:48:34 UTC; installed reload, malformed/resource/authenticated payload, NativeWorkflow и ContextWorkflow выполнены |
| Windows Python discovery | FAIL | 18 items: 15 PASS, 2 platform skips, 1 loader error; `test_profile_share` импортирует Linux-only `fcntl` |
| HTTP cases неимпортированного Python module на Windows | NOT_CHECKED | Эти 7 scenarios не выполнены; пропуск всего HTTP module не будет подходящим исправлением |
| Общий Windows / изолированный server | FAIL / PASS | Jobs `114555971100` / `114555970949`; server operations16, sharing9, isolated provisioning46 |
| Public release scan, pack и release uploads | SKIPPED | Локальный source scan отдельно PASS: 214 sources, 0 release files |
| Screenshots / diagnostics | Загружены | Artifacts `11714010910` / `11713591795`; новая визуальная приёмка не заявляется |
| Другой Windows account/ПК, полный installer и среда владельца | NOT_CHECKED / PENDING | Отложены до цельной сборки; установленное приложение и ПК/VPS владельца не проверялись |

Micro10 реализует только детерминированные переходы: real success имеет
приоритет, один exact ложный PIN сохраняет identity записей/правок за день,
другой mismatch даёт «Неверный пин-код». Смена local date в любую сторону
возвращает пустой expiry baseline даже при ошибке. Новый ложный binding
остаётся provisional, `CanOpen=false`, до будущего атомарного commit.
Пустой expiry baseline не удерживает прежние записи или историю; real PIN
в суточном состоянии не сохраняется.
Хранение, рестарт, очистка активной сессии в полночь, генерация данных,
конфликты сохранения и асинхронный UI ещё требуют отдельных микрозадач.

Следующий небольшой пакет: заменить зависимость portable HTTP fixture от
Linux setup writer собственным атомарным тестовым publisher, сохранив все
HTTP assertions и настоящий Linux sync/permission/locking набор; затем
начать отдельный protected daily store F09. Targeted micro09/micro10 —
**native PASS**, весь Windows CI — **FAIL**. Исторические **22/31** неизменны,
полные F09/F22 и остальные открытые F-пункты не закрыты; **24/31 пока нет**.

Подробности: [MICROTASK_PORTABLE_CHILD_IMPORT.md](MICROTASK_PORTABLE_CHILD_IMPORT.md)
и [MICROTASK_VAULT_DAILY_POLICY.md](MICROTASK_VAULT_DAILY_POLICY.md).

## Контрольная точка 11–12 — portable HTTP и защищённая суточная запись

Пакет из двух микрозадач: micro11
`0e2a8c033b665c5e5cba5cb803a4a5d3fa1f833e`, итоговый functional head
**`42c7ab60c9a3e035a712196765fa74a565f41f1a`**.
[CI 38171560167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38171560167)
проверил merge **`cd12b11e921477c85f068eeb8580f259a8eed742`** с main
**`4bb336575bb1216d3047bd2b294aeecad9fdd65a`**. Оба jobs и общий run
завершены с **SUCCESS**; завершённые журналы независимо сверены.
Прежние отказы контрольных точек выше сохраняются как история.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Source review / whitespace / local public-source scan | PASS | 218 sources, zero release files; отдельно HTTP7 с заблокированными fcntl/pwd без skips |
| Native reader / daily policy / новый protected record | 165 / 134 / 325 PASS | Windows `114565055972`, 20:18:01–20:18:03 UTC; guarded C#5 и настоящий CurrentUser DPAPI |
| Desktop / весь Test-ProGo | 3485 / PASS | 20:24:41 и 20:27:42 UTC; Instance26, Maintenance95, Shutdown47 и прежние suites выполнены |
| Весь Home VPN / portable harness | 975 / 90 PASS | 20:30:56 и 20:32:41 UTC; отдельный child-import и сравнение DPAPI данных выполнены |
| Micro11: Windows HTTP / вся Python discovery | 7/7 / PASS | 24 items: 22 passed, только 2 прежних Linux ownership/flock module skips; HTTP module не пропущен, 20:32:51 UTC |
| Общий Windows / изолированный server / общий run | PASS / PASS / PASS | Jobs `114565055972` / `114565056126`, run #528; Ubuntu operations16, sharing9, provisioning46 |
| Independent QR decode на Ubuntu | PASS | numpy2.2.6 и zxing-cpp2.3.0 установлены перед sharing suite; Windows availability отдельного decoder не заявляется |
| Public release-content scan / CI ZIP | PASS / PACKED | 218 sources, 44 release files, 20:32:52 UTC; ZIP 488291 bytes, 20:32:55 UTC |
| CI artifacts | Загружены | Screenshots `11714591746` (278 files), diagnostics `11714746417`, folder `11714268197`, ZIP `11714287987`; pruning для PR skipped |
| Новый цельный installer / visual и owner acceptance / другой account или ПК | PENDING / NOT_CHECKED / NOT_CHECKED | CI artifacts не являются опубликованным выпуском или полной приёмкой всех31; среда владельца не проверялась |

Micro11 использует unique staging в той же директории: закрывает stream,
атомарно публикует настройки через os.replace, затем в finally удаляет только
свой оставшийся staging. Сохранены все
семь HTTP scenarios/assertions и прежние сроки; production server и настоящий
Linux sync/permission/flock набор не изменены. Windows публикация прошла;
отсутствие всех возможных reader-sharing races этим запуском не доказано.

Micro12 защищает всю запись целиком: local calendar day, generation GUID,
revision, binding flag, fake PIN и ten fields. Строгое framing/UTF8 и пределы
проверяются до принятия; собственные byte buffers очищаются, input/dataset
caller не изменяются. Пустой bound dataset сохраняет PIN и отличается от
unbound tombstone. 325 checks — число assertions; 98 negative decode fixtures
подтвердили статические ошибки и неизменность caller bytes. Формат memory-only:
file persistence, атомарный commit, CAS, expiry/session revocation, генерация ложных наборов данных
и UI ещё не добавлены.

Строка ERROR с installed=broken-fixture в maintenance-журнале относится к
намеренной отрицательной update fixture: после неё rollback завершён и весь
Maintenance95 прошёл. Это не отказ общего кандидата.

Следующий небольшой пакет — отдельное атомарное суточное хранилище и отказ
устаревшего сохранения по generation/revision; adoption и active-session cleanup
подключаются после него. Targeted micro11/micro12 и весь текущий Windows CI —
**native PASS**. Исторические **22/31** неизменны; новых полных закрытых F-пунктов
нет, **24/31 пока нет**. F09 остаётся открытым; полный installer всех31 и owner
acceptance ещё впереди. Пользовательские ПК/VPS и установленное приложение
не проверялись.

Подробности: [MICROTASK_HTTP_FIXTURE_PORTABILITY.md](MICROTASK_HTTP_FIXTURE_PORTABILITY.md)
и [MICROTASK_VAULT_DAILY_RECORD.md](MICROTASK_VAULT_DAILY_RECORD.md).

## Контрольная точка 13–14 — чтение файла и атомарный суточный CAS

Пакет из двух ограниченных микрозадач. Reader micro13:
`43c1b63d1648bf7ee3facd259c730c74a5ef2936`; production micro14:
`387c5bcbab23f94dd2270e5c716f0b37ed73d513`. Итоговый test-correction head:
**`3eaec3ab46a064fd3072c91cb96c80b416533a66`**.
[CI #532](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38227655739)
проверил merge **`6bd2ef1764cae9429c626261e17632ad353ea8ab`** с неизменным main
**`4bb336575bb1216d3047bd2b294aeecad9fdd65a`**. Windows, isolated server и
общий run завершены с **SUCCESS**; результаты сверены по completed logs и jobs.

Три ранних запуска #529/#530/#531 завершены Windows FAIL / server PASS;
File-reader assertions до отказа: 75/75/76. Первый fixture не помечал Access
descriptor изменённым для native восстановления. Явный импорт Access SDDL
восстановил читаемость; #531 подтвердил remaining failure именно raw SDDL
equality. Финальная проверка строго сравнивает все упорядоченные access rules,
точные права, SID, inheritance, protection/canonical flags и отдельно native
readability; четыре memory vectors проверяют сам comparer. Все ранние head,
merge, run и job IDs сохранены в
[MICROTASK_VAULT_DAILY_FILE.md](MICROTASK_VAULT_DAILY_FILE.md).
Application build в ранних runs компилировал production Store, но отдельный
Store harness и позже расположенные наборы не достигались. Последующие
исправления меняли только File tests, production reader/store сохранены.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Source review / whitespace / local public scan | PASS | 224 sources, zero release files; отдельный просмотр reader/store и final evidence |
| Native прежний reader / policy / protected record | 165 / 134 / 325 PASS | Windows `114739023300`, 09:06:16–09:06:17 UTC; guards и CurrentUser DPAPI |
| Micro13: bounded file reader / immutable stamp | **110 PASS assertions** | 09:06:18.1525531 UTC; actual ACL denial/restored read/ordered rules, read locks, NTFS junctions, limits, marker/orphan backup; не 110 отдельных scenarios |
| Micro14: atomic compare-exchange | **149 PASS assertions** | 09:06:21.4596637 UTC; durable snapshots, PIN/day/generation/revision/digest, реальный locked Replace, witnesses и два настоящих child writers |
| Gate Busy / abandoned owner / broad ACL | PASS | Один SID gate для разных каталогов; bounded wait, fresh read, небезопасный existing ACL не меняется и не допускает публикацию |
| Desktop / весь Test-ProGo | 3485 / PASS | 09:11:57 и 09:14:59 UTC; Instance26, Maintenance95, Shutdown47 и UpdatePackage5 |
| Весь Home VPN / portable | 975 / 90 PASS | 09:17:55 и 09:19:14 UTC; прежние строгие recovery/lifecycle и child-import regressions выполнены |
| Windows HTTP / Python discovery | 7/7 / PASS | 24 reported items: 22 PASS +2 прежних Linux ownership/flock module skips; 09:19:17 UTC |
| Windows / server / общий run | PASS / PASS / PASS | Jobs `114739023300` / `114739024077`; server operations16, sharing9, provisioning46, independent QR decode с numpy2.2.6/zxing-cpp2.3.0 |
| Native public release scan / CI ZIP | PASS / PACKED | 224 sources, 44 release files, 09:19:18 UTC; ZIP 491904 bytes, 09:19:22 UTC |
| CI artifacts | Загружены | Screenshots `11738340469` (278 files), diagnostics `11737232829`, folder `11737683356`, ZIP `11737678429`; pruning для PR skipped |
| Open/UI, реальный crash/power loss, другой account/ПК и owner acceptance | NOT_CHECKED | Не подключены/не проверены; пользовательская среда не затрагивалась |
| Целый installer всех31 / published release / merge | PENDING / NONE / NONE | CI artifacts не подменяют завершение плана и owner acceptance |

Reader не создаёт отсутствующие файлы/каталоги, не чинит данные и не выдаёт
Missing при ошибках доступа, повреждении или оставшемся recovery artifact.
Stamp фиксирует scope/day/generation/revision и digest всей прочитанной image,
независимо от правок возвращённого dataset.

Store под проверенным SID mutex делает свежий read и CAS, подготавливает owned
detached result до публикации, закрывает WriteThrough staging с Flush(true) и
публикует через exclusive Move либо Replace. Стабильный `.recovery` отделён от
native backup `.recovery.previous`. Ошибка после marker блокирует Read/Write до
явного recovery; нет copy/delete/rollback fallback и ложного Success.
Удаляется только own staging, а при успешной публикации — own backup и marker
последним. Сохранность прежних bytes установлена в конкретных lock/prepublish
scenarios; общий hardware/crash guarantee, malicious-user rollback prevention
и handle-relative sandbox не заявляются.

ERROR `installed=broken-fixture` в maintenance log — намеренный negative update
case; rollback затем завершён и Maintenance95 прошёл. Общий кандидат PASS.

Следующий небольшой пакет — явный recovery protocol и владение оставшимся
staging **до подключения Open/UI**; затем durable adoption, CRUD, startup expiry
и active-session cleanup. Текущие micro13/micro14 и весь CI — **native PASS**.
F09 остаётся открытым, исторические **22/31** неизменны; новых закрытых полных
F-пунктов нет, **24/31 пока нет**. Полный installer всех31 и пользовательские
тесты впереди. ПК/VPS/телефон владельца и установленное приложение не проверялись.

Подробности: [MICROTASK_VAULT_DAILY_FILE.md](MICROTASK_VAULT_DAILY_FILE.md)
и [MICROTASK_VAULT_DAILY_STORE.md](MICROTASK_VAULT_DAILY_STORE.md).

## Контрольная точка 15–16 — журнал записи и явное восстановление

Пакет из двух микрозадач: защищённый ownership journal и explicit recovery с
очисткой только подтверждённых transaction artifacts. Functional head
**`4e44f3124dcec0136a5461ecd79ff2193e4fc223`** проверен в
[CI #533](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38303786820),
merge **`5622ce1d028201ccab5affdf2d3cd69c76443ce9`**, прежний main
**`4bb336575bb1216d3047bd2b294aeecad9fdd65a`**. Оба jobs и общий run —
**SUCCESS**; source review и завершённые Journal/Recovery/server logs сверены
независимо. История прежних отказов и PASS checkpoints выше сохранена.

| Проверка | Результат | Доказательство/граница |
| --- | --- | --- |
| Source review / whitespace / local public scan | PASS | Полные source/tests/doc и отдельные root File/Store/harness review; 230 sources, zero release files |
| Native прежний reader / policy / protected record / daily file | 165 / 134 / 325 / 110 PASS | Windows `114992783732`, 20:23:08–20:23:10 UTC; guards и настоящий CurrentUser DPAPI |
| Micro15: protected journal codec | **211 PASS assertions** | 20:23:10.4506661 UTC; independent Unprotect/layout/EOF/fixture, scope/digest/metadata/limits/purpose/tamper; не 211 отдельных scenarios |
| Изменённый Store | **151 PASS assertions** | 20:23:13.9185339 UTC; CAS/PIN/day invariants, retained exact staging/journal/unique backup, два real writers, Busy/abandoned/ACL |
| Micro16: recovery и подтверждённый cleanup | **274 PASS assertions** | 20:23:16.5913534 UTC; Previous/Next, exact bytes, undefined Missing failclosed, initial recovery, legacy/unknown preservation и guards |
| Native interruption/ownership/lock fixtures | PASS | Шесть writer checkpoints, три changed-ownership edges, partial cleanup/retry и second preflight; реальные delete/read/Move locks, ordered ACL restore, три artifact junctions +root junction, shared/abandoned gate |
| Desktop / весь Test-ProGo | 3485 / PASS | 20:29:54 и 20:33:00 UTC; Instance26, Maintenance95, Shutdown47, UpdatePackage5 |
| Весь Home VPN / portable | 975 / 90 PASS | 20:36:19 и 20:38:06 UTC; прежние строгие lifecycle/recovery и child-import cases выполнены |
| Windows HTTP / Python discovery | 7/7 / PASS | 24 reported items: 22 PASS +2 прежних Linux ownership/flock module skips; 20:38:08 UTC |
| Windows / server / общий run | PASS / PASS / PASS | Jobs `114992783732` / `114992783483`; Ubuntu operations16, sharing9, provisioning46, independent QR decode с numpy2.2.6/zxing-cpp2.3.0 |
| Native public scan / CI ZIP | PASS / PACKED | 230 sources, 44 release files, 20:38:09 UTC; ZIP 495490 bytes, 20:38:15 UTC |
| CI artifacts | Загружены | Screenshots `11772920840` (278 files), diagnostics `11773225557`, folder `11773090703`, ZIP `11772810913`; pruning для PR skipped |
| Actual crash/power loss, другой account/ПК, hostile filesystem и owner acceptance | NOT_CHECKED | Persisted/exception checkpoints не подменяют hardware durability; environment владельца не проверялся |
| Open/UI/adoption/CRUD/expiry, целый installer всех31, merge/release | PENDING / PENDING / NONE | Новые seams ещё не подключены к приложению; CI artifacts не являются опубликованным полным выпуском |

Writer теперь атомарно публикует полный DPAPI journal после закрытого flushed
staging и exclusive reservation unique backup. Journal фиксирует canonical
scope, transaction GUID и immutable evidence старой/новой images, без PIN и
dataset. Новый backup — `vault.daily.dat.previous-<Operation:N>`; legacy fixed
`.recovery.previous` остаётся blocked unknown evidence. После попытки journal
rename ошибки сохраняют stage/backup: новый ciphertext может быть единственным
проверяемым кандидатом. Before publish проверяются exact stage, old CAS, journal
bytes и legacy отсутствие; before journal deletion — fresh exact target и
journal bytes. Изменённые артефакты не удаляются и не дают ложный Success.

Recovery под тем же verified SID gate сначала проверяет каждый present artifact.
Current Next принимается; intact Previous сохраняется с отдельным результатом
RecoveredPrevious. При отсутствующем target сначала восстанавливается exact
Previous backup, иначе публикуется exact Next stage через exclusive Move.
Если verified image нет, остаются Conflict и journal; Missing не создаётся.
Перед cleanup выполняется повторный полный preflight, затем удаляются только
проверенные auxiliaries и journal последним; returned data получены fresh read.
Никакой новой revision/PIN/clock state recovery не генерирует. Untracked
pre-journal stage/backup сохраняются; directory enumeration/glob delete отсутствует.

ERROR `installed=broken-fixture` — намеренная отрицательная maintenance fixture;
rollback затем завершён и Maintenance95 прошёл. Это не отказ кандидата #533.

Следующий bounded пакет — подключить эти foundations к асинхронному Open и
durable adoption, сохранив приоритет real authentication. Затем нужны CRUD,
startup/day expiry и очистка активной сессии в полночь. Текущие micro15/micro16
и полный CI — **native PASS**. F09 открыт; **22/31** остаётся историческим счётом,
новых полных закрытых F-пунктов нет, **24/31 пока нет**. Полный installer всех31,
visual/owner acceptance впереди. ПК/VPS/телефон владельца и установленное
приложение не проверялись.

Подробности: [MICROTASK_VAULT_DAILY_JOURNAL.md](MICROTASK_VAULT_DAILY_JOURNAL.md)
и [MICROTASK_VAULT_DAILY_RECOVERY.md](MICROTASK_VAULT_DAILY_RECOVERY.md).
