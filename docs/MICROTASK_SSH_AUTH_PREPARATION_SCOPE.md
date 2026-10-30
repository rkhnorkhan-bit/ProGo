# F31-R11: оставшаяся подготовка метода входа перед SSH

Сверено на source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`.
Это восстановленный контракт и точка продолжения; launch integration ниже TODO.
Агрегатор описан отдельно в текущем блоке ниже. Проверка подписи реализована микрошагом,
но не считается готовностью всех способов аутентификации.

Источник: [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md), разделы
«SSH-профиль и ключ» и «SSH/SOCKS и восстановление»;
[подпись выбранной identity](MICROTASK_SSH_SELECTED_KEY_SIGNING.md).

| Условие | Согласованное поведение |
| --- | --- |
| Нужная effective public identity подписывает через агент | Без повторного ввода фразы перейти к реальному SSH/SOCKS; подпись не доказывает доступ на сервер. |
| Зашифрованный ключ отсутствует в агенте | Предложить явное «Добавить ключ» через уже проверенную отдельную консоль OpenSSH; затем fresh профиль/ключ и явный повтор. Startup/recovery не должны сами открывать mutating ssh-add. |
| Прямой незашифрованный ключ | Агент не обязателен; отсутствие подписи через агент не запрещает прямой вход. |
| IdentityAgent none | Не обращаться к default agent как к обязательному условию. |
| Custom IdentityAgent | Нынешний backend сообщает UnsupportedEndpoint, не подменяет endpoint и не запрещает штатную попытку OpenSSH. |
| Publickey отключён или исключён | Agent/key proof не нужен; остальные разрешённые методы проверяются реальным SSH. |
| Неизвестная policy, cert/FIDO/constraints, несколько identities | Неподтверждённость с понятной причиной; не выбирать первую identity автоматически и не объявлять весь способ готовым. |
| Уже работающий настроенный SOCKS для CLI/Windows | Проверить SOCKS; новый собственный SSH не создаётся и новая подготовка ключа не требуется. |

`CommandFailed` от ssh-add -T не означает автоматически «зашифрованный ключ
не загружен»: возможны отказ, constraints, неподдержанный механизм или другой
ключ. Success по .pub не доказывает пригодность private bytes. Принятые источники
не задают общего запрета SSH по любому неуспеху подписи. Новый hard gate не вводится.

## Три следующих связанных блока

1. Агрегатор fresh CheckConfiguration и локальной подписи: immutable candidate,
   executable/endpoint, effective identity list, typed результат, общий бюджет,
   cancel/freshness; unknown/custom/other-auth допускают штатную попытку.
2. Одна preparation/reservation/epoch перед созданием собственного SSH в общем
   `ProxyService.StartSingleTunnel`: его вызывают StartTunnelCore и PollRecovery.
   Подготовка идёт вне UI/долгого gate; перед spawn повторно проверяются intent,
   настройки, endpoint и отмена. Context-only правка оставила бы recovery обход.
   Проверки: manual/auto/settings/recovery/fallback, borrowed SOCKS, Stop/Close,
   отсутствие late spawn и дублей. Это отдельный lifecycle блок, не одна вставка.
3. Раздельные profile/agent/key/SSH/SOCKS причины и существующие Check/Add/Retry;
   после Add fresh check, без повтора мутации. Bad-permissions одного ключа не
   запрещает другие identities. ACL исправляется только по отдельному подтверждению.

Выбор Combo диагностики сейчас не сохраняется в SshProfileSetting и не меняет
SSH argv: это выбранная reference текущей проверки, не новая identity туннеля.
Alias сохраняет effective список IdentityFile; direct explicit key задаёт -i
и IdentitiesOnly=yes. Изменение этой семантики не подразумевается новым UI.

## Текущий блок: advisory aggregate

`SshAuthenticationPreparation` получает immutable clone выбранного candidate,
точный локальный executable/config и captured endpoint. В worker объединяет
fresh `CheckConfiguration` и `SshKeyReadiness.Check` в общий бюджет7000мс;
сама подпись сохраняет прежний cap3000мс. После signer повторно проверяет
effective configuration в оставшемся бюджете, без автоматического выбора первого
из нескольких ключей. Результат содержит immutable effective список и отдельные
typed причины; raw stderr/secret в отображение не попадают.

`LocalSigningObserved` означает локальное наблюдение в фазе подписи. Второй -G
сверяет effective scope, но не создаёт вечной гарантии private/public bytes перед
spawn. Caller обязан вновь проверить revision/intent/endpoint и владение запуском;
текущий результат не является разрешением на SSH и не вводит hard gate.
Никаких запусков службы, mutating ssh-add, сохранения passphrase или live SSH нет.

Native suite `SshAuthenticationPreparationWorkflow` зарегистрирован в общем
Desktop harness: настоящий ssh-G с отдельным config, sibling ssh-add и test-owned
RSA agent pipe; signing/refusal/corruption, config changes, cancellation/deadline,
multiple/none и deterministic total-budget/snapshot vectors. Его выполнение на
новом точном SHA пока NOT_RUN. `ProxyService` этот агрегатор ещё не вызывает;
F31-R11 остаётся PARTIAL. Этот блок не заменяет launch integration ниже.

Когда можно подавить первую штатную SSH-попытку, требует проверяемого определения
«единственный разрешённый метод — именно этот зашифрованный ключ». Такого
состояния backend пока не устанавливает. До его реализации сохраняется advisory
наблюдение и классификация настоящего SSH; это не новый внешний блокер выбора
владельца и не основание закрыть F31-R11.

## Launch integration: проверенный технический остаток

Read-only сверка `656b3d65e03d540be859ee69635c492631093446` подтвердила:
`StartTunnelCore` и `PollRecovery` удерживают `ProxyService.gate` на всём вызове
`StartSingleTunnel`. Поэтому агрегатор нельзя вставить внутрь текущего lock.
Следующий блок охватывает `ProxyService`, узкий revision lease в `SettingsService`
и реальные lifecycle fixtures; Context-only hook оставил бы recovery обход.

1. Под gate резервировать ровно один launch: immutable epoch/settings/candidate/
   executable/endpoint, linked cancellation и Work, опубликованный до queue.
   Освободить gate обычным разделением фаз, выполнить preparation на worker.
   Timer видит reservation и не создаёт второй worker; ручной запуск ждёт старого
   владельца вне gate. Каждый fallback candidate получает свою fresh preparation.
2. Перед spawn повторить intent/epoch/token/settings/target/endpoint/occupied-port
   guards. Короткий nonblocking `SettingsService` lease должен исключать commit
   между final revision check и `Process.Start`. Не держать lease во время
   ssh-G/-T, await, UI, логирования или Save; при busy writer повторять admission
   вне process gate в существующем caller budget. Закрепить фактически проверенные
   executable и SSH_AUTH_SOCK в `ProcessStartInfo` (null endpoint удалять из env).
   Медленный native spawn остаётся owned до возврата; его нельзя объявить отменённым
   заранее. Snapshot check без lease не является доказательством атомарности с Save.
3. Stop/Dispose сразу инвалидируют intent/epoch и cancel reservation; tracked stop
   ждёт startup/recovery/preparation settlement вне gates, затем завершает только
   собственный child. Старый finally очищает лишь свою reservation/attempt и не
   может затронуть новый PID. Backoff/permanent refusal считаются по реальным
   SSH attempts, а advisory Busy/TimedOut/CommandFailed не становятся hard gates.

Production constructor передаёт реальный preparer явно. Независимый test constructor
принимает injected preparer и по умолчанию не выполняет дополнительный I/O:
`SocksRecoveryTests` использует собственный EXE как fake ssh с поддержкой только -N;
безусловный ssh-G запустил бы весь harness рекурсивно. Новый native aggregate test
должен отдельно использовать настоящий OpenSSH и test-owned agent pipe.

Обязательные проверки блока: held before/after preparation и final admission;
manual/auto/timer single flight; primary/fallback/recovery freshness; Stop, Close,
caller cancel, changed profile/key/SOCKS/endpoint и занятой revision lease без late
spawn/publication; старый finally против нового PID; borrowed valid SOCKS без
preparation/own child; direct/none/custom/other-auth/multiple/unknown/sign-fail
с сохранением обычной попытки. Сохранить существующие AsyncStopSerialization,
FallbackCancellation, TimerFallbackStop, FallbackEditCancellation, ForeignReadySocks
и PermanentRecoveryFallback. Реализация и выполнение этого блока пока TODO/NOT_RUN.

## Local lifecycle этап3 — source PARTIAL, отдельный от CI588

Main Program явно передаёт preparer; общий StartSingleTunnel теперь резервирует
один epoch/Work до worker preparation вне process gate и применяет final
SettingsService revision lease непосредственно перед native spawn. Captured
endpoint закреплён в ProcessStartInfo; Stop/Dispose invalidation и settlement,
manual/recovery/fallback freshness описаны в текущем PROJECT_STATE.
9 новых bounded native lifecycle scenarios добавлены к41 существующему recovery
suite; compilation/native NOT_RUN. Полный F31-R11 не закрыт. Это source main
pipeline, bootstrap/HomeVPN optional constructor остаётся отдельной областью.
Нужны новый exact-SHA CI, combined real-preparer/native tunnel integration и
раздельная typed UI presentation; owner encrypted-agent/VPS/другой ПК отдельно.
Прежний раздел TODO выше сохраняет критерии, а не отменяет этот local source
checkpoint. CI588 b32ac4ec не проверяет эти новые lifecycle изменения.
