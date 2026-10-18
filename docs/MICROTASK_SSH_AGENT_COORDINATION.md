# PGA-06B — общая координация службы и операций с ключами

**18.10.2026 UTC. Узкий PGA-06B Code/Dev PASS по completed CI574.**
Родитель `c7f335e58c13a9bc9401f7e7cb4411add130b28a`, предыдущий принятый
runtime source `9db506714bb26320246e4519aaf2927934dde14f` / completed CI569.
Два микрошага внутри F31-R11: общий владелец операции и проверки его lifetime.
Счёт **6/55**, остаток49=37+12, верхний уровень23/31. Новых ID/PASS55 нет.

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

## Проверки

В существующий Desktop partial включён отдельный coordination workflow после
45 проверок интерактивного backend. Новые **43 coordination checks** и
Desktop4431=4387+43+1 native fixture setup подтверждены CI574; assertions/watchdog
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

## CI573: FAIL добавленного Codex fixture assertion до SSH checks

Source `7d28498f6020d908ac5c9bdfa9d12cde0831e272`, tree
`651e71d4d905f4efc1937c253a82678a02991ccf`, merge
`11a97dbe449699279ceb9c00e0124681eb1e2a44` в прежний main4bb3365.
[CI573](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39164488312)
completed FAILURE: Windows117784468010 FAILURE, Ubuntu117784467763 SUCCESS.
20:48:09.7800734UTC AsyncSettingsFormCancel отказал на усиленном combined
rollback assertion, после успешного OwnIntegrations и pending-cancellation check.
Measurement24/helper8 PASS, новые SSH checks NOT_RUN.

В добавленном тесте ошибочно использован CodexProxyService.IsConfigured: это
наличие launcher **и Start Menu shortcut**, тогда как OwnIntegrations создаёт
только launcher. Fixture не устанавливает shortcut и не должен требовать его.
Это выявленная ошибка нового assertion; отдельные флаги combined assertion
в573 не записаны, другие ложные условия не исключены по тому логу. Проверка
заменена точным сравнением launcher content с прежним портом — именно файла,
который fixture создаёт и transaction должна вернуть. Дополнительно выводятся
только четыре booleans running/cli/windows/launcher. Прежний actual HTTP400,
remaining rollback assertions, production и все budgets остаются прежними.
Это исправление фикстуры, не доказательство причины UNKNOWN572.
CI573 не перезапускался; весь PGA-06B остаётся PENDING до нового completed CI.

Полный нормализованный Windows log573:3137lines/282997bytes,
SHA256 `bcfd9f55d69619fe763a77582ca32c3ac04e87cbe05a15db981351f246a02e92`.
Ubuntu573lines/53115bytes,
SHA256 `986d6de32243647e364ca612763db8e1529d1649bffef92df2aefa71d53f3801`.

## Native Code/Dev приёмка: CI574 SUCCESS

Tested source `1000e1c057ce529909ab1ee791b838eb0395c2ff`, tree
`eaf22f636fad5e16106b6420c023fbe42254edf6`, merge
`4ac7900fa1ba171a9ec6609caad18d8b770f233d` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI574](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39165129385)
completed SUCCESS: Windows117786357423/Ubuntu117786357149, весь run SUCCESS.
Оба checkout log подтверждают source/merge/base; build identity0.2.2+merge4ac7900.
Production SCM/key code идентичен первому source c862920; между source менялись
только отмеченные test fixtures/failure diagnostics и Markdown. Rerun не делался.
В Windows только прежний Prune old CI artifacts SKIPPED, все тестовые steps SUCCESS.

Coordination BEGIN21:02:27.9599865UTC →43 отдельных PASS →marker43
21:02:30.0775469UTC. Все три key owner, четыре SCM read/start×timeout/cancel,
separate probe/startup competitors, failure release и mixed12 contention прошли.
Mixed race: один native owner,11 Busy, освобождение слота после settlement.
Прежние66 start/40 startup/67 inventory/52 matching/57 policy/45 console checks
PASS; старые37 SCM/32 probe checks сохранены в полном Desktop workflow.
Actual read-only SCM oracle21:02:14.3743013UTC: runtime1/Stopped, startup4/Disabled,
statusError0/configError0. Нормальный StartService проверяется на прежних native
boundary fixtures; служба runner и реальный пользовательский агент не меняются.
Native selected ssh-keygen создаёт temporary encrypted Ed25519, настоящий ssh-add
получает1 echo-off console entry и отправляет ADD_IDENTITY только isolated pipe;
signing/server-auth не выполнялись. Все3 console attachment metadata first5→
detach→startup6→retry PASS; production console owner не изменён этим пакетом.

Старый settings cancellation fixture20:57:44.5716833UTC:
running/cli/windows/launcher=True. Exact settings/revision,
old port, launcher content и actual HTTP400 PASS. Setup добавляет1 check, поэтому
Desktop4431=4387+43+1 в21:03:12.3719298UTC. Measurement24/helper8 PASS, native
stop=completed/samples6/working/private/io=True. Это не доказывает UNKNOWN причины
старых570/572. Ошибки тестовых assertions571/573 исправлены; все FAIL сохранены.

Весь Test-ProGo PASS21:05:53.3268397UTC: прежние SOCKS41/403, Instance28,
Maintenance95/Shutdown47/updater5 сохранены. HomeVPN975 — 21:09:22.8145791UTC,
portable90 — 21:11:10.5612247UTC, public294source/44release — 21:11:13.5579148UTC PASS.
Windows Python24 =22PASS+2 прежних Linux-only skips; Ubuntu16/9 и полный46-check
provision smoke без skips, independent QR и TLS/recovery/revocation/session tail.
Source/native review самостоятельный; независимого agent review не было.
Нового visual/DPI/contrast/Narrator review не выполнялось и PASS не заявлен.

Полные нормализованные логи574: Windows8931lines/969897bytes,
SHA256 `9f4a24c1d2aa7798132b2d5216ee81f5ec6168348778e22a31ac95a5b3b1ab61`;
Ubuntu573lines/53129bytes,
SHA256 `447c82d5fc498b3dbcf5c12fce9407b7a32951f7ddfa4d7e636bd49680d26b2f`.

Все4 artifact metadata сверены с API и upload log: run39165129385/source1000e1c,
name/ID/outer ZIP bytes/SHA256. Архивы не скачивались; содержимое ZIP и скриншотов
не проверено. Runner сообщает293 screenshot files; это не проверенный count ZIP.

| Artifact | ID | Outer ZIP bytes | SHA256 |
| --- | ---: | ---: | --- |
| ProGo-release | 12151251112 | 546723 | a243446bee8248e19d096bd3323021a9ed7ef582fa00070cb147cb34a786f11b |
| ProGo-wizard-screenshots | 12150967085 | 6540742 | f20289d827a30bf6afdf88da61fa9e7ca19b2636a3c184550019e2d99462fd15 |
| ProGo-release-zip | 12150748029 | 539478 | 35ce20e36d65b382d53482887be0dead8391a49358aa7c7a95ff1d5c744779b0 |
| ProGo-ci-diagnostics | 12150733039 | 95132 | 037ba73b3e350e623a3e64ba52a327d8b54373e00197f5d98b8f520d15f01fd5 |

Inner ProGo-release.zip542967bytes отличается от outer artifact ZIP.
Это CI artifact, не итоговый31/31 установщик. Evidence после tested source
содержит только4 Markdown; runtime/tests/scripts/server/workflows/VERSION
не меняются и повторный native PASS на другой runtime не приписывается.

## Следующий микрошаг и ограничения

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
