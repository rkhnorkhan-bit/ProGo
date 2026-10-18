# PGA-06B — общая координация службы и операций с ключами

**18.10.2026 UTC. Реализация подготовлена; Code/Dev PENDING до итогового CI.**
Родитель `c7f335e58c13a9bc9401f7e7cb4411add130b28a`, предыдущий принятый
runtime source `9db506714bb26320246e4519aaf2927934dde14f` / completed CI569.
Два микрошага внутри F31-R11: общий владелец операции и проверки его lifetime.
Счёт **6/55**, остаток49=37+12, верхний уровень23/31. Новых ID/PASS нет.

## Изменение

Раньше SCM read/start/startup и inventory/fingerprint/add имели независимые
блокировки и могли выполняться одновременно. Теперь SshAgentServiceProbe
резервирует тот же process-wide SshReadOnlyLease, который уже используют
операции с ключами. Историческое имя класса сохранено. Резервирование происходит
до scheduling dedicated worker, без ожидания свободного слота. Конкурирующий
запрос получает typed Busy без native/child work, скрытой очереди и повторов.
Разные экземпляры probe и normal-owner startup используют тот же lease.

Lease передаётся worker и освобождается в его finally после возврата native
callback, очистки принадлежащих ему ресурсов и CTS. Если создать/запланировать
worker не удалось, резервирование освобождает вызывающий код. Таймаут или отмена
ожидающего caller не освобождают lease незавершённого native запроса. Существующие
SCM timeout/cancellation semantics, bounded owned process cleanup и порядок
StartService не меняются. Busy тексты больше не называют конкурента исключительно
проверкой ключей/read-only: слот может быть занят SCM или явным добавлением.

Нет нового native API, shell, UAC, изменения политики службы, остановки агента,
удаления ключей или ACL repair. Добавление по-прежнему только backend будущего
явного действия; оно не вызывается startup. Парольную фразу получает OpenSSH.

## Проверки и ожидаемая матрица

В существующий Desktop partial включён отдельный coordination workflow после
45 проверок интерактивного backend. Новые **43 coordination checks**, ожидаемый
Desktop4431=4387+43+1 native fixture setup ниже; существующие assertions/watchdog
budgets и workflows не ослаблены.

- Три реальных production entry points на injected child boundary — inventory,
  fingerprint phase и add — каждый удерживает слот. SCM read/start другого probe
  возвращают Busy без native callback; отмена отклонённого caller не затрагивает
  владельца. One-shot startup также получает Busy и не создаёт скрытый retry.
- Отменённая операция с ключами удерживает слот до возврата runner; поздний успех
  не заменяет отмену. После settlement SCM read/start вновь доступны.
- Native-boundary SCM read/start fixtures удерживают настоящий production SCM
  код до/после caller timeout/cancel. Все три операции с ключами и отдельный probe
  не обходят владельца; после release закрыты fixture handles, позднего StartService
  нет, новый SCM/key запрос может выполняться.
- Исключение, null snapshot и probe без start capability завершаются safe
  Unavailable и не оставляют занятый lease.
- Двенадцать одновременно выпущенных смешанных SCM/key запросов: ровно один
  native callback, один владелец, одиннадцать Busy; после завершения слот свободен.

Используются только временные fake sibling paths и прежние SCM/child boundary
fixtures. Нового системного StartService или работы с реальным агентом нет.
Предыдущие actual native OpenSSH/console fixtures остаются в полной CI матрице.
Локально Windows/.NET runtime отсутствует; source review не объявляется native PASS.
Самостоятельный source review; независимого agent review не было.

## Первый run: CI570 FAIL до новых SSH checks

Source `c862920236b4467d10878a4f7afa3954bdce16ae`, tree
`c61c43a55ec877338e476dba96dd3da5a86877a0`, merge
`b2cb7b39e3d8e1de73301deabfba291172c111e4` в прежний main4bb3365.
[CI570](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39162194563)
completed FAILURE: Windows117777645086 FAILURE, Ubuntu117777645179 SUCCESS.
Windows20:16:45.1288754UTC: прежний RescueMeasurement native assertion
`real current-process sampling reads Win32 CPU/RAM/I/O`. До него15 прежних
measurement checks PASS. Новые Desktop/SSH coordination checks **NOT_RUN**;
их native PASS/FAIL не установлен. Последующие relay/public/package steps SKIPPED.

Assertion объединяет completed stop, минимум2 samples и positive working/private
memory/I/O availability. В CI570 не выведено, какое условие нарушено; **точная
причина UNKNOWN**. Production measurement, assertions, duration1500/interval250,
прочие budgets и workflow не меняются. Добавлена только безопасная диагностика
typed stop, sample count и трёх availability booleans в прежнее сообщение теста.
Это позволяет разобрать следующий отказ; последующий SUCCESS не будет считаться
доказательством причины570. Нет автоматического rerun или ослабления проверки.

Полные нормализованные логи CI570: Windows2018lines/174599bytes,
SHA256 `eb8989fbd4da50ecd7424c85a9196be2ae525d081040baef71a1715ece6a65f9`;
Ubuntu573lines/56265bytes,
SHA256 `dcbd7e18e5c6af55f2caa6ef0532a8275fe2a36907703b883df9fd56da05f84c`.

## CI571: FAIL нового cancellation assertion после25 новых PASS

Source `eb25c66259be933b9015bffaa5655c8be7368247`, tree
`6240ff5b071413a118ea7bf64fd961b1bdffb376`, merge
`4bb9777b3545e46869aef225623c4fed6f1fffe7` в прежний main4bb3365.
[CI571](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39162695886)
completed FAILURE: Windows117779138466 FAILURE, Ubuntu117779138583 SUCCESS.
Прежний measurement24/helper8 и agent blocks66/40/67/52/57/45 PASS.
Успех measurement не устанавливает UNKNOWN причину FAIL570. Native fixture
интерактивного добавления и encrypted-key flow сохранены без изменения.

Новый coordination block BEGIN20:30:38.2259008UTC,25 PASS до отказа
20:30:39.3149884UTC: scenario starting=False/cancel=True. В тесте было
`!cancel && waiter.GetAwaiter().GetResult().State == TimedOut`. При cancel=True
short circuit не вызывал GetResult, поэтому catch отмены не выполнялся и expected
всегда оставался false. Причина именно этого тестового отказа установлена по
исходнику; это не доказательство отказа production lease. Исправлен порядок:
GetResult вызывается безусловно, затем оценивается timeout или ловится cancel.
Production code,43 checks, assertions, native boundaries и budgets не меняются.
Остальные18 новых checks не приняты; весь PGA-06B остаётся PENDING до нового CI.
CI571 не перезапускается и не объявляется PASS.

Полные нормализованные логи571: Windows6946lines/756415bytes,
SHA256 `1074b74d33c9cbbc5e3b0d5de7c48b925142895b2282d20132f3e4771d3ab3d7`;
Ubuntu573lines/53270bytes,
SHA256 `b8e9a21a2871d11578b7ce4ea9b0396215ddea56e993948bb309939a2db80eb7`.

## CI572: FAIL старого async-settings fixture до SSH checks

Source `40143a572352f04b8a8d402fa090d4b79e9366e0`, tree
`4ceeba78d3a4081d42833f8c82021dc0d4ece78d`, merge
`18f5d8e3af91407ebaa4b32dee4f5a8070f9fb11` в прежний main4bb3365.
[CI572](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39163721326)
completed FAILURE: Windows117782201589 FAILURE, Ubuntu117782201414 SUCCESS.
Прежние measurement24/helper8 PASS; native facts completed/samples7/working,
private,io=True. Причина570 этим не устанавливается. Новые SSH checks NOT_RUN.

20:39:19.4876511UTC AssertBridge в AsyncSettingsFormCancel получил ConnectionRefused
на test-owned loopback port61208. До него exact settings/revision/old-port
assertion прошёл, но он не проверял наличие активного listener. Fixture запускал
реальный application owner и bridge, не создавая потребителя этого bridge.
Штатный AppProxyConsumers.ReleaseIfUnused вправе остановить такой listener
после фонового наблюдения Empty; его timer работает во время UI pump теста.
В первом failure log нет trace конкретного Stop caller, поэтому **точная причина
остановки572 UNKNOWN**; выявлено несоответствие setup ожидаемому живому endpoint.

Fixture теперь вызывает уже существующий OwnIntegrations для своих временных
Windows/CLI/Codex consumers перед переходом порта. Это даёт1 прежний setup check
и делает rollback условие осмысленным: дополнительно проверяются IsRunning,
старый CLI endpoint, Windows proxy и Codex ownership; прежний actual HTTP400
assertion сохранён. Тест не отключает observer, не удерживает искусственный lock,
не добавляет retry, задержку или больший budget. Raw snapshot cleanup прежний;
все HKCU/files/listeners принадлежат изолированному CI fixture. Production bridge,
transactions/consumer policy и SSH code не меняются. Новый source требует
completed CI; итог ожидается Desktop4431=4387+43+1. CI572 не перезапускается.

Полные нормализованные логи572: Windows3141lines/283464bytes,
SHA256 `b1a7a509452e8a20b25bfcc8804f1d90886a1b79c45313634fc7fb7520053977`;
Ubuntu573lines/53993bytes,
SHA256 `7b0438e19dde66328d4dd0cf976f1390148b00688b24a24e6f11c9e8a5a8270a`.

## Оставшиеся работы

Это взаимное исключение отдельных SCM/key операций, а не атомарный snapshot
целого диагностического pipeline и не готовность SSH-аутентификации. Кнопка
«Добавить ключ», выбор identity, UI freshness/lifecycle, selected-key possession/
signing/constraints/cert/FIDO/custom endpoint, AutoStartSocks/auth ordering и
классификация ошибок остаются последующими микрошагами. Старый «Первый вход»
не заменяется этим backend. Совпадение public fingerprint и service Running
по-прежнему не означают подтверждённый вход.

PGA-05/06/F31-R11/F31/F17 OPEN, F31-R05 PARTIAL, Package PENDING,
Live NOT_CHECKED. Все55 IDs/10 PGA rows/11 acceptance scenarios сохраняются.
Истории FAIL555/556/562/565/568 и UNKNOWN причины не переопределены.
ПК/VPS/телефон/установленная программа/реальные ключи/ACL/платежи владельца
не проверялись и не менялись. PR88 draft/unmerged, VERSION0.2.2, без merge/release.
