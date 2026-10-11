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
