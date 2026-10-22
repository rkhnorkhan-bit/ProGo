# ProGo: единый реестр фактического состояния

Обновлено22.10.2026 (МСК). Текущий опубликованный source `bf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc`, tree `8cf494f4ab7c2dfc247a2ad8f96689632f3cb983`; PR88 OPEN/DRAFT, интеграция в main NOT_RUN. Исходная аудируемая база — `7861ea919eed24c5fe9964fc35a23d3958ba5a9d`. Машиночитаемые поля и точные критерии: [project_state.json](project_state.json). Это текущий реестр; прежние checkpoint-файлы сохраняются как история. Утверждённые требования остаются в [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md) и [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md).

23/31 и 6/55 — разные исторические области приёмки разработки. Эти исторические счётчики не складываются и не переводятся в процент готовности. Отдельная взвешенная оценка v1.0 приведена ниже. В 55 входят 43 результата до установщика и 12 зависимых приёмок. Шесть исторических Code/Dev PASS: F17-R02, F17-R03, F31-R01, F31-R02, F31-R03, F31-R04. Их полная перепроверка нового кандидата пока NOT_RUN. Верхнеуровневый PASS требует всех применимых слоёв.

Слои каждого ID: реализация; реальный пользовательский маршрут; проверка/commit; интеграция; пакет; целевой ПК. Код находится в PR88, не в main. Общий финальный installer отсутствует, установка и приёмка на ПК владельца NOT_RUN. PARTIAL в таблице не отменяет прежний узкий PASS. FAIL обозначает подтверждённое нарушение критерия; BLOCKED — конкретную внешнюю зависимость.

CI582 независимо проверен через jobs и Windows log: Windows/Ubuntu SUCCESS, source `b02b0e9606fb57518ed18d17de42e5f82fca8fa8`, merge `cf2e19691057c143b4f66d93c0041e2e1c5681a8`. На базе `7861ea9` только четыре Markdown-файла поверх tested source; runtime/tests/scripts/server/workflows/VERSION идентичны. Новые изменения требуют нового CI.

## CI606 full SUCCESS — sourcebf1c / mergecd20, staged R09 ещё NOT_RUN

[CI606/39724309390](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39724309390)
Windows119644614336/Ubuntu119644614044 SUCCESS. Build identity
0.2.2+cd20daf85da786d42244e7ce17fc83726b81fbcc проверен по log;
source `bf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc`, full tree
`8cf494f4ab7c2dfc247a2ad8f96689632f3cb983` = local0152426bd4c6b8723e3232a0463fc8c224296cce.

Новые narrow native PASS: committed repair cleanup04:47:52.4180059 (реальный
locked old snapshot, -Recover только residue cleanup); coherent DPAPI reader
05:08:39.9645891 +cancel/conflict/lock/defaults; failed old cleanup refuses token
import before DPAPI/identity change05:09:30.2421268. Это не закрывает весь R09.
Main regression51/456; bounded43/report32/helper2; actual modal Cancel→Retry,
chooser3/Context ownership, signing55/UI50/prep72 +real direct/owned-agent
prepare→native SSH→SOCKS→HTTP→crash/fresh recovery PASS. Desktop5274/Instance28/
Maintenance126/Shutdown47/HomeVPN1051/Portable90; SSH contracts5; Python28=
26PASS2SKIP. Exact28 parent anchors обновлены, веса и уровни не повышены.

Runner ZIP0.2.2 SHA256 `198a7ed7d2e2839e3be023814a0273ef62bc20a08e2a3e4ea0510635b0f73822`.
All uploads SKIPPED. Bytes не получены/не переданы/не установлены; installer и
owner acceptance NOT_RUN, передача BLOCKED до разрешённого приватного канала.
I58–80, verified21.75→22, изменение0: D0.5/P0.5/A0 для тех же28 parents weight87.

Frozen next checking local `dfe70563a136a21edeae1b514a219a067a0a64a7` /tree
`7016b466f779fc02168e538626e6f610780325ea` содержит только staged API/optional
warnings/loading gates/owned share retry +tests и docs:9files661 additions,
scan0 secrets/PII/public320/0/syntax/C#5 structure/diff PASS. Windows NOT_RUN.
Перед publication evidence docs обновить до exact606, source files не менять.

Отдельный root local `5cce98404647a2dd4e0ea4d88f13daf9957a9018` подключает
owned cancellable share read к configure/QR actions wizard. Written actual UI
Cancel→Retry/queued close/Dispose cases с реальным protected read и own configure
modal, без SSH/HTTP действий. Syntax/C#5/source/public/diff PASS; compiler/native
NOT_RUN. Этот UI block исключён из frozen checkingdfe; Program/default ctor и
getter пока legacy. R09 private writes/recovery I/O остаются FAIL/open.

Следующее: обновить checking docs, exact scan/tree, expectedbf1c публикация одного
source в PR88 и один CI. Пока идёт его Windows — отдельно default startup/cache
integration; не приписывать ему606 SUCCESS. No binaries/release/main merge/owner
PC/VPS actions. Ниже все checkpoint-записи — история.

## R09 owned share read/retry — locale385513, routes ещё не переключены

Source `e38551307dd16be3410b3957ddd5a746c888725f`: RefreshShareOriginAsync
публикует retained task до I/O; duplicate join, immutable profile ID/fallback,
DPAPI/cohort reread off UI, очищение временных массивов. Generation guard не
публикует старый результат поверх явно сохранённого нового адреса; внешний
conflict — честный отказ. Stop отменяет admission memory-only до worker cancel
и join actual read; Dispose не даёт late cache update. Source/private writes
не выполняются reader. Синхронный setter остаётся отдельным R09 долгом.

Written native vectors clean/cancel/Stop/Dispose/newer-save/external-change,
real lock→unlock→retry, UI heartbeat/thread/duplicate ownership/source bytes;
каждый worker/Stop наблюдается до восстановления своих private fixtures.
Modern syntax/targeted C#5/diff/public320/0 PASS; Windows compilation/native
NOT_RUN. Program/default ctor/getter и QR/configure routes пока legacy; этот
API не объявлен подключённым. I58–80, verified22 exact605,0 новых баллов.

CI606 sourcebf1/mergecd20, run39724309390 не менялся: Windows119644614336
repository step in_progress, Ubuntu119644614044 SUCCESS. Не делать head-only
push/duplicate. Далее exact606 результат; отдельный checking API+availability+
loading gates+refresh на этой проверенной базе, затем actual UI/startup switch.
Ниже checkpoints — история.

## R09 loading gates — local7d9d80f, default startup ещё legacy

Source `7d9d80fbdf35309c10f13179cf2c0554281df830`: AppCommandState имеет отдельный
PhoneInitializing; actual Context dispatcher и tray не открывают мастер до конца
owned read. MainWindow показывает чтение/ошибку, обе точки входа телефона
согласованы; StopPhone/StopAll/settings/desktop продолжают работать. Добавлен
native Context+dashboard held-read test: heartbeat, pending menu/buttons/status,
rejected stale command, bounded own-modal guard, восстановление entry points.
Fixture освобождает barrier и наблюдает retained initialization/Stop до выхода.

4-file syntax/targeted C#5/diff/public320/0 и public-doc tests3 PASS; Windows
compilation/native NOT_RUN. Это staged route, Program/default ещё не активирован.
Новый block не входит в606. I58–80, verified22 exact605,0 новых баллов. Далее
owned share read/retry, затем default startup integration и один exact checking
после результата606. Private writes R09 остаются открыты.

## R09 optional metadata availability — local00a1831, production ещё legacy

Local source `00a183199da722a1d97441ad4b36838de3100aea` сохраняет valid Access,
если optional owner/home/share недоступны: отдельные warning flags, честный
отрицательный read outcome, без подмены share непроверенным fallback. Mandatory
access/cohort conflict остаются отказом. Добавлены native real-lock cases для
трёх optional файлов с запуском собственного process/local relay и проверкой
неизменности private bytes. Это staged API, не переключение Program.

Diff/public320/0/modern syntax/targeted C#5 structure PASS; Windows compiler/native
NOT_RUN. Source606 не содержит этот блок: Windows119644614336 in_progress,
Ubuntu119644614044 SUCCESS в run39724309390. Без нового push/duplicate CI.
I58–80, verified22/raw21.75 только exact605,0 новых баллов. Далее отдельные
Context/main loading gates и native held-read regression; перед production
activation нужен owned refresh/retry, затем startup switch. Sync private writes
и весь R09 остаются открыты. Ниже checkpoints — история.

## CI606 — finished blocks checking опубликован, один run

Sourcebf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc, merge candidatecd20daf85da786d42244e7ce17fc83726b81fbcc,
full tree8cf494f4ab7c2dfc247a2ad8f96689632f3cb983 = local0152426bd4c6b8723e3232a0463fc8c224296cce.
[CI606/39724309390](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39724309390)
in_progress. Expected163d→bf1c lease/actual PR88 head+merge подтверждены, body
updated.6files2048 additions secret/PII0/public320/0/syntax/diff/tree PASS;
workflow/VERSION unchanged/public uploads OFF. Guard/read primitive/committed
extra native vector впервые входят в source606, их Windows пока NOT_RUN.

Staged initializerad3/optional-metadata/Context/Program switch НЕ входят. Для него
до активации нужно сохранить basic access при ошибке optional metadata; это
следующая local микрозадача. Не приписывать source606 успех605, не запускать
duplicate/head-only docs CI. После обрыва сначала этот же run/head/log.
I58–80, verified22/raw21.75 только exact evaluated605,0 баллов за новый checking.
No binary/release/main merge/owner PC/VPS actions. Private delivery/setup/target
acceptance остаются blocked/NOT_RUN. После606 результата — exact native evidence,
а не новая интерпретация старых SUCCESS.

## Pending finished followup checking publication

Objectbf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc parent163d97436d68f5169a8a22ea9f17d38344f8ad88;
full tree8cf494f4ab7c2dfc247a2ad8f96689632f3cb983 = local0152426bd4c6b8723e3232a0463fc8c224296cce.
6files2048 additions secret/PII0/public320/0/source syntax/diff/tree PASS;
workflow/VERSION unchanged, uploads OFF,605 EOF fixture fix унаследован.
Только законченные source3 blocks (guard/read primitive/committed extra test) и
актуальные3 docs. initializerad3/optional metadata/UI integration excluded.

Ref ещё NOT_RUN в этот момент. После обрыва actual PR head/run проверить прежде
повтора.605 завершён, новый head его не отменит. New Windows compilation/native
NOT_RUN; read primitive всё ещё не подключён к startup. I58–80, verified22/raw21.75
только exact605;0 новых баллов за checking. Далее expected163d→bf1c и один CI,
без binary uploads/release/main merge/owner PC/VPS действий.

## CI605 full SUCCESS — exact163d/522ca, новые local blocks excluded

[run39721135629](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39721135629)
Windows119634576903/Ubuntu119634576608 SUCCESS. Build identity
0.2.2+522ca8616d014fec2ebafd176d9a4365e9281e1e совпадает; source163d97436d68f5169a8a22ea9f17d38344f8ad88,
treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d = local7b3330c35b49306be18740c17f10968532a95921.
Alive assertions PASS04:36:20.697 Waiting и04:37:11.047 Preparation. Own pipe EOF
lifetime исправил unreliable fixture, actual preservation теперь подтверждено.

Repair PASS: null1/preflight4/full41 +actual image/fresh Root/default known-folder
untouched04:13:32.442/real sharing rollback/contained hang/hard-kill recovery
without original payload/9 strict refusals. Regression51/456, bounded43/report32/
helper2/modal6/chooser3/context3/worker4/routes6, signing55/UI50/prep72 +real direct
и owned-agent prepare→native SSH→SOCKS→HTTP→crash recovery; Desktop5274/Instance28/
Maintenance126/Shutdown47/HomeVPN1022/Portable90. Native SSH server contracts5;
late Python28=26PASS2SKIP (SKIP не PASS).

Runner ZIP0.2.2 SHA256 `3f40efcfa8a109e166509e42eb75b0fceecd9dc87c5a959873a7c99ca69e071e`.
All uploads SKIPPED. Bytes не получены/не доставлены/не установлены. Private
пакетная передача BLOCKED до разрешённого владельцем канала; successful runner
archive не является предоставленным installer. Release/main/owner PC/VPS не было.

V1.0:28 parents weight87 с exact anchors, D0.5/P0.5/A0 дают21.75→22. +8.70 raw
к60413.05 — реально выполненный late tail/ZIP, без смены весов/двойного учёта ID.
I58–80 unchanged. Whole F22/23/R08/R09/R10/R11 и target acceptance остаются partial/
blocked. Local checking6353 (guard/read primitive/committed extra case) и staged
initializerad3 НЕ входят в605: их Windows NOT_RUN/баллы0, old SUCCESS не наследуют.

Далее checking6353 плюс актуальные evidence docs, exact diff/secret/workflow/tree
check и один новый source/run. initializerad3 пока отдельно: optional metadata
availability нужно исправить до UI/Program switch; нельзя публиковать случайно
неоконченные стадии вместе с checking. Ниже checkpoints — история.

## R09 stage1 service ownership API — local ad3fa3e, production NOT_RUN

Sourcead3fa3e037a9a8fb82ad31c9e753de1f09759657. Optional background constructor
публикует Task/CTS ДО worker, primitive читает real DPAPI/files; поле состояния
публикуется коротким memory gate только в действующем service scope. Start/Stop
join initial task; Dispose cancels read даже после ранее pending Stop и не даёт
позднего metadata update.1arg/5arg ctor API сохранены; Program/default route ещё
legacy sync. Share cache действует только в new mode, production не переведён.

Written native modes load/Stop/Dispose/Stop→Dispose +locked access: real read
barrier, UI heartbeat/off-thread, no partial identity/session/child, cancelled
initialization settles before cleanup, cached share getter under real file lock.
Все retained workers/starts/stops наблюдаются перед восстановлением private data.
C#syntax/C#5 structure/task-before-I/O/source/diff/public320/0 PASS; Windows
compiler/native NOT_RUN. Не входит в605 и не меняет frozen checking6353.

Review до переключения UI выявил prototype availability regression: full-cohort
fault по optional share/owner/home сейчас оставляет new-mode Access null, хотя
прежний constructor не требовал share для базового канала. Current production
legacy route этого нового поведения ещё не использует. Следующая отдельная
микрозадача — optional metadata warning/совместимая доступность +native test;
не включать новый путь в Program до исправления. После этого loading gates,
startup switch, explicit safe share refresh и private write workers отдельно.
Compatibility UseToken всё ещё sync, весь F31-R09 FAIL/не закрыт.

I58–80, verified13/raw13.05 exact604,0 баллов за new API/tests.605 run39721135629
Windows119634576903 repository step SUCCESS, late native relay in_progress;
Ubuntu119634576608 SUCCESS, full result/ZIP ещё не получены. Current source163d
не менялся, следующие Win checks нельзя приписать этому local API.

## Сохранённые checking drafts и следующий R09 initialization block

Pre605 draft2bbd52615795c5d5123987fc4b7e22d981fc8f4e сохранён отдельным local
commit в ProGo-ci604-followups; stale a859 base, не публиковать с возвратом fixture.
Актуальный checking6353f95cb872d15c3f8302c0d9de887fe0b3942b/tree01dc308e4d8b12a1c6c78739a3d5225104a85e0a
в ProGo-ci605-followups уже на base7b3330c, stdin lifetime fix унаследован. Только
3 finished source/test paths: token cleanup guard/read primitive/committed cleanup
extra native case. Source/syntax/secretPII0/public320/0/diff PASS, Windows NOT_RUN.
Current605 не менялся. Следующий этап не должен случайно попасть в этот draft.

R09 initialization крупнее одной микрозадачи, разбит заранее:
1. Service-owned init task публикуется до worker I/O; Stop/Dispose удерживают/
   join actual read, Dispose cancels/no late metadata. Native held-read cases.
2. Actual Context/main/wizard loading gates и honest pending/error; bounded
   shutdown через retained StopWork, никаких UI getters с ожиданием worker.
3. Program/default startup переключается на worker, legacy constructor/ShareOrigin
   reads удаляются, реальные native ctor expectations адаптируются; producers
   shared-origin cache не должны стать устаревшими. Private writes отдельно.

Stage1 — API/ownership, production ещё не переведён до следующих шагов; ни один
этап не закрывает весь F31-R09. Все новые Windows NOT_RUN/баллы0 до exact evidence.
I58–80, verified13/raw13.05 exact604.605 run39721135629 Window119634576903
in_progress, Ubuntu119634576608 SUCCESS; не отменять/не дублировать CI.

## CI605 — unrelated lifetime fix опубликован, один run

Source163d97436d68f5169a8a22ea9f17d38344f8ad88, merge candidate522ca8616d014fec2ebafd176d9a4365e9281e1e,
full treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d = local7b3330c35b49306be18740c17f10968532a95921.
[CI605/39721135629](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39721135629)
queued/in_progress. Expected46576→163d lease/actual PR88 head+merge подтверждены,
body updated.2files33 additions secret/PII0/public320/0/syntax/diff/tree PASS;
production/src/scripts/workflow/VERSION unchanged, public uploads OFF. New
Windows native/compilation NOT_RUN pending exact log. Не дублировать run/head.

Root token guard a75/7cc, committed repair extra83bcc8a/c39659f, stored-state
primitive501/886 исключены из605. После обрыва сначала этот же run/head. Пока он
идёт — preserve/refresh pre-fix dirty followup worktree перед будущей публикацией,
потом coherent R09 constructor/UI ownership: task до I/O, metadata publication
после scope check, pending action gates и retained bounded shutdown; не простое
Task.Run вокруг синхронного импортера и не getters, ожидающие worker на UI.
I58–80, verified13/raw13.05 exact evaluated604,0 новых баллов. Historical59822/
его ZIP не наследуются. Owner/private package/setup/real target остаются blocked.

## Pending unrelated fixture lifetime publication

Object163d97436d68f5169a8a22ea9f17d38344f8ad88 parent46576c73cf029b4ab0fc2d97ebbac07d15e91fa3;
treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d = local7b3330c35b49306be18740c17f10968532a95921.
2files33 additions secret/PII0/public320/0/syntax/diff/tree PASS. Production/src/
scripts/workflows/VERSION unchanged, uploads OFF. Ref ещё NOT_RUN здесь, после
обрыва actual PR head/run проверить до повтора.604 completed, не будет отменён.
Root pending3 independent blocks не включены. New native NOT_RUN, I58–80,
verified13/raw13.05 exact604, прирост за fixed fixture0. Далее expected46576→163
source и единственный новый CI; не rerun604, alive assertions/2s cleanup сохранены.

## CI604 completed FAILURE позднего fixture; repair/main scoped PASS

Source46576c73cf029b4ab0fc2d97ebbac07d15e91fa3 / merge9e2c214a22d11ee1efded8ec2832e62b0bb7e2dd,
run39717548727 Windows119623188818 FAILURE/Ubuntu119623188527 SUCCESS. Build identity
совпала; repository step SUCCESS. Repair native PASS: null1/preflight4/full41
copy+actual self-check1/real known-folder untouched03:34:35.411/sharing rollback1/
contained hang1/hard-kill then explicit recovery without payload1/strict refusals9.
New explicit Root и native canonical image validation теперь имеют exact evidence.

Main regression PASS:51/456, bounded43/report32/helper2/modal6/chooser3/context3/
worker4/route6, selected signing55/UI50/prep72 +actual direct/owned-agent native
SSH→SOCKS→HTTP→crash recovery, Desktop5274/Instance28/Maintenance126/Shutdown47.
13 phone cases завершились PASS, но в03:56:54.690 final unrelated-alive check FAIL.
Source fixture сам завершался после Thread.Sleep60000; группа длится больше60s,
потому итоговый check не может отличить self-expiration от неверного cleanup.
Actual сохранность с исправленным lifetime ещё NOT_RUN; обвинение production
cleanup не подтверждено. Later Home/Portable/Python/content/package NOT_RUN,
все uploads SKIPPED, installer/private delivery/target не выполнены.

Isolated7b3330c35b49306be18740c17f10968532a95921/treec8b4209c4faebfb42ff33774f9bc2db16f43cd7d
(см. точный tree JSON; root importbe8a1d1d8aa1b28fbb4f23e3477af9ae7ee76a8b) меняет
только2 fixture files: собственная redirected stdin pipe вместо elapsed lifetime,
EOF при cleanup/смерти parent, оба callers Waiting/Preparation используют одну
схему. Alive assertions остаются, cleanup500+1500ms=прежние2s; app deadlines/CI
не увеличены, проверки не отключены. Source syntax/ownership/public320/0/diff
PASS, new native NOT_RUN. Один initial source check ошибочно искал sleep во всём
fixture, исправлен на unrelated branch; separate fake-SSH waits не менялись.

V1.0:28 parents weight87×D0.5×30%=13.05→13, P/A0. Все exact timestamp anchors
в JSON. F22 использует actual durable repair recovery критерий вместо NOT_RUN
portable tail; его вес не менялся/не добавлялся дважды. +13.05 к evaluated6030 —
выполненные native, не написанные tests. Historical59822 с P0.5 остаётся только
source37f02/16bf1/того ZIP. I58–80 unchanged. F22-R03 PARTIAL: узкие native PASS,
extra committed test/final package/owner acceptance всё ещё NOT_RUN.

Далее publish только isolated unrelated fixture fix. ProGo-ci604-followups имеет
3 dirty copied root source/test paths со старой a859 base: это наш preserved draft,
не другой исполнитель. Перед его будущей публикацией обновить base/включить
исправление7b3330c, чтобы не вернуть прежний lifetime. Root token guard/read
primitive/committed cleanup test остаются local NOT_RUN и не смешиваются с fix.

## F31-R09 read preparation primitive — local, constructor/UI NOT_RUN

Source5017167a249256255259669f21f88747bda6021a +
8863ea9a9d440988ce1562fa3485a9c771c3ab8a. HomeVpnStoredState.LoadAsync читает
access/owner/home/share и DPAPI только в actual worker, проверяет повторно тот же
ciphertext cohort, отказывает при concurrent change/cancel, не пишет файлы/ACL.
Результат сохраняет immutable private state, выдаёт отдельные mutable consumer
copies; временные ciphertext/plain arrays очищаются. Это подготовка чтения,
старый service constructor/ShareOrigin/private writes ещё НЕ переведены на неё.
Full F31-R09 остаётся FAIL, фактический UI startup по-прежнему требует следующего
согласованного подключения,0 баллов за невыполненный/unwired code.

Native tests written в existing HomeWaiting suite: real Form/heartbeat + held
read verification, off-UI thread, protected byte preservation; cancel ownership,
concurrent DPAPI change refusal/no rollback, actual locked file, partial/empty
saved state defaults without new files. Все retained tasks наблюдаются после
release native locks до восстановления четырёх своих private fixture files;
свои Form/timer/stream/CTS/events освобождаются. Windows compilation/native
NOT_RUN; syntax/C#5 structure/read-only/coherence/public320/0/diff PASS.

НЕ входит в604, как и local token guard/committed cleanup test. Published source
46576c73cf029b4ab0fc2d97ebbac07d15e91fa3 / merge9e2c214a22d11ee1efded8ec2832e62b0bb7e2dd
не менялся. Run39717548727 Windows119623188818 in_progress, Ubuntu119623188527
SUCCESS. I58–80, verified0 exact evaluated603, прирост0. Далее actual604 final;
после него checking законченных независимых блоков и coherent constructor/UI
ownership. Сначала service single load/epoch/cancel, затем actual UI gating/
bounded shutdown; persistence отдельно, не простой Task.Run вокруг UseToken.

## Independent committed repair cleanup regression — local, not604

Source83bcc8a4bdb336bd47d6c42f473b98c5adbfe9f3 +
c39659f918db611ec06560bfc166ee4188df7e94 добавляет только test.
До actual repair старый installed VERSION отличается от подготовленного.
Свой Windows FileShare.Read на old VERSION snapshot разрешает чтение/самотест,
но запрещает cleanup deletion после durable committed. Проверяются failed
cleanup exit/no completed-success claim, committed+selfchecktrue journal,
неизменность whole pending tree при отказе повторного обычного repair.

После release своего handle explicit -Recover обязан только убрать остатки и
сохранить exact accepted candidate/private snapshot. Во время recovery fixture
держит настоящий application lifetime mutex: случайный self-check не смог бы
вернуть success; residue-only cleanup не требует повторного app startup или
stopped-app gate. Все свои Process/stream/mutex/identity handles bounded/finally
освобождаются, другие процессы/окна не завершаются. New native NOT_RUN, source/
ASCII/public320/0/diff PASS,0 баллов. Runtime code не менялся.

Не входит в604: его source46576/merge9e2c остаётся неизменным. Ubuntu119623188527
SUCCESS, Windows119623188818 всё ещё full repository step in_progress; точные
native604 результаты пока не получены. I58–80, verified0 exact evaluated603,
прирост0. Local import guard также excluded604. После604 final результат — один
checking source для этих небольших завершённых блоков, не отменять текущий run.

## CI604 — fresh self-check/native canonical fixture опубликован

Source46576c73cf029b4ab0fc2d97ebbac07d15e91fa3, merge candidate9e2c214a22d11ee1efded8ec2832e62b0bb7e2dd,
full treec5e54b1beef1cc37a942c5af6884e3181d284949 = locala8599c761280f332e9697b87113b48672070c4da.
[CI604/39717548727](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39717548727)
in_progress. Expected8da→46576 lease/actual PR88 head+merge подтверждены, body
updated.3files72 additions secret/PII0/public320/0/Core+helper syntax/diff/tree
PASS; workflow/VERSION unchanged, public uploads OFF. Новые Windows checks
NOT_RUN pending exact log. Root R09 guard a75df7b/7cc8159 исключён из source604.

Следующий шаг — этот же604, actual known-folder preservation/native path check,
затем mandatory timeout/hard-kill/recovery и поздние native/ZIP. При отказе только
изолированный cause fix. Не повторять push/CI и не отменять текущий run ради docs.
I58–80, verified0 exact evaluated603, прирост0. Historical59822 остаётся его
source37f02/merge16bf1/ZIP. Binary/release/main/owner PC/VPS действий не было.

## Pending fresh self-check/native canonical fixture source

Object46576c73cf029b4ab0fc2d97ebbac07d15e91fa3 parent8da78a6bc6c72709b9f21d43a041c65c66ec61a5;
treec5e54b1beef1cc37a942c5af6884e3181d284949 = locala8599c761280f332e9697b87113b48672070c4da.
3files72 additions secret/PII0/public320/0/Core+embedded C#syntax/diff/tree PASS;
workflow/VERSION unchanged, uploads OFF. Ref ещё NOT_RUN здесь. После обрыва
actual head/run проверить прежде повтора.603 completed; следующий run не отменит
его. New fresh Root/real known-folder preservation/native final-path checks
NOT_RUN. Root R09 guard excluded; I58–80, verified0 exact603, прирост0.

## CI603 FAILURE на следующем fixture; null/copy/rollback scoped PASS

Source8da78a6bc6c72709b9f21d43a041c65c66ec61a5 / merge4a0bac2b788708a51e5051e4802d0d7fa6698eb6,
[run39716559040](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39716559040):
Windows119619939733 FAILURE/Ubuntu119619939421 SUCCESS. PS5 parser/main compile
PASS. Native null-string bad-vs-good1 PASS подтвердил причину602: plain$null
даёт ArgumentException без изменений bytes, explicitNullString атомарно заменяет
без backup.4 preflight/41 manifest/full41 copy+actual installed self-check+
private fixture preservation1 и actual sharing failure rollback1 PASS.

В03:23:50.784 negative hanging-check fixture отказал на raw image-path comparison.
Alias short/long Windows path — hypothesis; значения не печатались. Local
native GetFinalPathNameByHandle compare e2c93f2648f5a8a328ac5762cedf00d9f32c28aa +
755fc3cea05c778606e150bf178df0b4e512d5c7 disposes failed/unadopted handles. Child
kill-on-close job и bounded cleanup остаются; новый native NOT_RUN.

Критичный source gap: старый LOCALAPPDATA-only override НЕ гарантировал isolated
AppPaths, который читает Windows known folder.603 private fixture PASS не
подтверждает неприкосновенность default Windows profile. Старые утверждения
изоляции в истории superseded этим выводом; на ПК владельца ничего не запускалось.
Locala8599c761280f332e9697b87113b48672070c4da/treec5e54b1beef1cc37a942c5af6884e3181d284949
задаёт fresh absolute PROGO_SELF_CHECK_ROOT только для3 прежних self-check args;
обычный app игнорирует env, readonly AppPaths API сохранён. Repair требует actual
созданный child data root. Native test дополнительно сравнит real Windows
known-folder tree existence/all file hashes до/после actual built-image check.

Все новые native NOT_RUN, source Core/helper syntax/contracts/ASCII/public320/0/
diff PASS.3 source paths. I58–80, verified0 exact603, прирост0: полного accepted
parent D/P нет, later recovery/native/ZIP/owner NOT_RUN. Historical59822 остаётся
только exact37f02/16bf1. Далее scanned isolated source и один новый CI, не rerun603;
local R09 token guard не включать. Binary/release/main/owner действий не было.

## Pending explicit-null fix publication

Object8da78a6bc6c72709b9f21d43a041c65c66ec61a5 parent5ecf7738e3f77832737caad3a43bccda34366e6a;
treee3365f1d14d49ea825b98c089686d54915a1789d = local87bcec48a5582a10add3045221525e0cacb4b5fb.
2files20 additions secret/PII0/public320/0/diff/tree PASS, workflow/VERSION unchanged,
upload guards4/4 intact. Ref ещё NOT_RUN здесь; после обрыва actual PR head/run
проверять перед повтором. Native interop/positive repair/остальные критерии NOT_RUN.
R09 local guard excluded. I58–80, verified0 exact602, прирост0.

## CI602 FAILURE локализован: journal File.Replace backup-string boundary

Source5ecf7738e3f77832737caad3a43bccda34366e6a / mergef232bfd91cd25d475a38b917cf9398d8f5128cd7,
run39716108725 Windows119618489243 FAILURE/Ubuntu119618489590 SUCCESS. Parser/
main compile/4 preflight/41-manifest PASS. Safe record03:18:59.814:
ArgumentException; stage=snapshot; script_line=121; hresult=-2147024809;
native=0; check_exit=-1. Подтверждён отказ File.Replace(temp,journal,$null)
при preparing→prepared, ДО runtime admission/self-check. Initial599 не имел
такого direct record; причинная связь того запуска восстанавливается по той же
неизменённой операции, не по вымышленному логированию599.

Local87bcec48a5582a10add3045221525e0cacb4b5fb/treee3365f1d14d49ea825b98c089686d54915a1789d:
оба journal/runtime File.Replace используют явный NullString.Value. Microsoft
описывает этот type как передачу настоящего null строковому .NET parameter;
API File.Replace требует null, когда backup не нужен. Преобразование plain$null
в пустую строку — пока inference из точного отказа/контракта, его отдельно
подтвердит native PS5 vector: прежний вызов обязан дать ArgumentException без
изменения bytes, правильный — заменить owned target/consume temp без extra file.
Primary URLs сохранены в JSON. Никакого восстановления через отключение проверки.

New native NOT_RUN, source/public320/0/diff PASS.41-file guard/таймауты/workflows
не менялись. Positive actual repair/rollback/hard-kill/recovery и поздний полный
native всё ещё обязательны. I58–80, verified0 exact602, прирост0. Далее isolated
null-boundary fix publication и один exact run; R09 guard не включать.

## CI602 — raw safe evidence опубликован, один exact run

Source5ecf7738e3f77832737caad3a43bccda34366e6a, merge candidatef232bfd91cd25d475a38b917cf9398d8f5128cd7,
tree01509d78d50912f975e6fada81202f5ccfc50e4d = localfa7c7ee4545c7d8230b1ea30e7e0188f18f2644f.
[CI602/39716108725](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39716108725)
queued/in_progress. Expected d239→5ecf lease/actual PR88 head/merge подтверждены,
body updated.2files10 additions secret/PII0/public320/0/diff/tree PASS;
workflow/VERSION unchanged, public uploads OFF. New Windows checks NOT_RUN.

Следующий шаг — тот же602 exact logs/raw fixed marker или numeric source line.
Сначала факты, потом изолированный cause fix; исходная599 причина всё ещё UNKNOWN.
Не повторять CI или head update ради docs. Root R09 guard a75df7b/7cc8159 excluded.
I58–80, verified0 exact evaluated601, прирост0; historical59822 сохранён отдельно.

## Pending raw repair evidence source

Object5ecf7738e3f77832737caad3a43bccda34366e6a parentd239bee8c69519086ddcfe611a637a7627a7ffb7;
tree01509d78d50912f975e6fada81202f5ccfc50e4d = localfa7c7ee4545c7d8230b1ea30e7e0188f18f2644f.
2files10 additions0 secrets/PII/public320/0/diff/tree PASS; workflow/VERSION unchanged,
uploads OFF. Ref ещё NOT_RUN здесь. После обрыва actual head/run проверить раньше
повтора. Цель нового source — raw safe phase/type/numeric marker/line, не rerun601,
не утверждение исправленной причины599. I58–80, verified0 exact601, прирост0.

## CI601 FAILURE: manifest41 прошёл, positive child exit1 без classified cause

Source d239bee8c69519086ddcfe611a637a7627a7ffb7 / mergea9600c5511e79f262e688da3d80284e58692119e,
run39715671880 Windows119617063751 FAILURE/Ubuntu119617063695 SUCCESS. PS parser/
main compilation/4 native preflight refusals PASS. Canonical41-runtime guard
теперь PASSED (достигнут следующий positive child); в03:14:02.040 child_exit1,
no classified repair failure. Исходная причина599 всё ещё UNKNOWN; последующие
native/ZIP NOT_RUN. Ошибка форматирования stderr — гипотеза, не подтверждённая
причина самого repair. Не повторять идентичный601.

Localfa7c7ee4545c7d8230b1ea30e7e0188f18f2644f/tree01509d78d50912f975e6fada81202f5ccfc50e4d:
Console.Error.WriteLine fixed PROGO_REPAIR_FAILURE record ДО rollback: type,
phase, script line, HResult/native/check exit только whitelist/numbers. Parent
также извлекает только numeric stderr source line при ошибке вне handler. Никаких
raw exception messages/путей/данных. Source/public/diff PASS, native NOT_RUN;
не ослаблены41 guard/таймауты/реальные проверки. После этого нужны actual codes,
а не очередное предположение о причине. Root R09 guard excluded.

I58–80, verified0 exact601, прирост0. Historical59822 остаётся exact37f02/16bf1.
Далее2-file safe marker checking source, expected d239 head и один новый CI.

## CI601 — canonical test fixture опубликован, один run

Sourced239bee8c69519086ddcfe611a637a7627a7ffb7, merge candidatea9600c5511e79f262e688da3d80284e58692119e,
full treef06412841ab9607333a40b8cf674ba8683cb2732 = local7299b84f647e6176f48638550a8fe64131cb8cb9.
[CI601/39715671880](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39715671880)
queued/in_progress. Actual PR88 head/merge и expected973→d239 lease подтверждены,
body обновлён.1file5 additions secret/PII0/public320/0/diff/tree PASS; unchanged
workflow/VERSION, uploads OFF. Новые Windows compilation/native NOT_RUN.

Далее этот же601: получить actual runtime_count либо sanitized repair phase/
codes. Прежний600 не дал выполнить positive child; первопричина599 всё ещё
UNKNOWN. Не повторять CI для того же SHA и не менять head ради checkpoint docs.
Local R09 guard a75df7b/7cc8159 исключён из601. I58–80, verified0 exact evaluated600,
новые баллы0; usable historical59822 сохранён только на его исходниках/пакете.
Binary/private delivery/owner setup/target acceptance BLOCKED/NOT_RUN, не PASS.

## Pending canonical fixture publication

Objectd239bee8c69519086ddcfe611a637a7627a7ffb7 / treef06412841ab9607333a40b8cf674ba8683cb2732
равен local7299b84f647e6176f48638550a8fe64131cb8cb9. Parent/expected973fb0a40cee9cbfeb745904e9204be9b418cc9b.
1file5 additions0 secrets/PII/public320/0/diff/tree PASS. Workflow/VERSION unchanged,
uploads OFF; exact41 criterion retained. Ref в этот момент NOT_RUN; after
interruption проверить actual head и associated run, не повторять публикацию.
Новое source изменение только test canonical path/count evidence; original599
repair cause остаётся UNKNOWN. I58–80, verified0 exact600, прирост0.

## CI600 FAILURE раньше positive child; первоначальная причина599 UNKNOWN

Source973fb0a40cee9cbfeb745904e9204be9b418cc9b / merge22a413abcf33d62e81d923ad258201728c69a4be,
[run39715222513](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39715222513):
Windows119615596947 FAILURE, Ubuntu119615596679 SUCCESS. PS5.1 parser/main
compilation PASS,4 missing/empty native payload refusals PASS. В03:08:59.248
новый test41-runtime count guard FAIL на98 строке ДО positive child. Безопасная
repair classification пока НЕ выполнена, причина599 всё ещё UNKNOWN. Не
объявлять исправленную сверку доказательством исправленного repair.

Local7299b84f647e6176f48638550a8fe64131cb8cb9/treef06412841ab9607333a40b8cf674ba8683cb2732
меняет только test fixture: canonical payload root, root-directory ownership+
readme/license names вместо slicing ненормализованной строки пути; runtime_count
в безопасном числовом error, exact41 guard остаётся. Это candidate, native NOT_RUN.
Source public320/0/diff PASS; no test disable/deadline increase. Один новый SHA
для новых данных, не identical rerun600. Root R09 guard по-прежнему исключён.

I58–80, verified0 exact600, изменение0. Historical59822/raw21.75 сохранён с его
SHA/ZIP; new package/поздние native/owner NOT_RUN. Далее проверить actual973 head,
один targeted delta source CI, по actual count/phase исправить подтверждённую
причину. Публикация binary/release/установка/реальные VPS не выполнялись.

## Pending safe-failure source перед ref update

GitHub object973fb0a40cee9cbfeb745904e9204be9b418cc9b parent0942445dd268f836c5001ec2bf82d27bafc6f3a5;
full tree1fbf063b4d684240ccb431f24f80d1289335e474 = local85771b3d764c7b36c488ea178b52e49dafa566c1.
2 files27 additions0 secrets/PII/public320/0/diff/tree PASS; workflow/VERSION
unchanged, uploads OFF. Ref ещё NOT_RUN в этот момент. После обрыва проверить
actual PR head, затем единственный associated CI. Новая проверка нужна для
safe child failure phase/codes, которые599 скрывал. Это не identical rerun и не
заявление об исправленной причине599. Local R09 guard excluded, Windows NOT_RUN.
I58–80, verified0 exact599; historical59822 сохранён. Затем exact CI/log cause.

## CI599 FAILURE — positive repair отказал, root cause пока UNKNOWN

Source0942445dd268f836c5001ec2bf82d27bafc6f3a5 / mergea5d6f7c9afcd6d671b6607447fff7648b109ee45,
[run39714516345](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39714516345):
Windows119613322368 FAILURE, Ubuntu119613322153 SUCCESS. Windows PS5.1 parser
preflight и app C#5 compilation PASS; Build identity совпал. Четыре реальных
incomplete/empty payload refusals PASS. В03:01:18.866 positive complete repair
FAIL; fixture скрыл child exit/phase/cause общим сообщением. Причина пока
UNKNOWN. Последующие runtime rollback/self-check/hard-kill/recovery/native suites
и пакет NOT_RUN, uploads SKIPPED. Не повторять идентичный599.

Изолированный470e33c6027e31a01754de4bb458d0ae9a229994 добавляет только безопасную
диагностику: фиксированный stage, root exception type, HResult/native/check exit
numbers; exception messages/private paths не печатаются.85771b3d764c7b36c488ea178b52e49dafa566c1
отдельно исправляет source-confirmed latent test mismatch: Build содержит41
runtime+3 root readme/license; repair test сверяет41 runtime, без пропуска иных
built leaves.599 упал ДО этой сверки — mismatch не объявляется его причиной.
Новые native NOT_RUN, таймауты/проверки не ослаблялись,0 баллов за диагностику.

V1.0 current exact599 verified0: четыре узких preflight guards не подтверждают
parent D при отказе основного repair; поздний Desktop/native transport/package
не выполнялись. Историческая usable59822/raw21.75 сохранена только source37f02/
merge16bf1/его ZIP. Это снижение evidence для нового SHA, не объявление всех
функций FAIL. I58–80 unchanged. Далее один новый exact instrumentation source,
не rerun599; root token guard не включать. Ниже прежний in_progress — история.

## CI599 — repair опубликован, один exact run

Source `0942445dd268f836c5001ec2bf82d27bafc6f3a5`, merge candidate `a5d6f7c9afcd6d671b6607447fff7648b109ee45`,
full tree `4297436ce6a3ad896783a9ea45e070c593aa0eac` = isolated local
`3f545dd2190b23344e200c56b0427ff8cc70e00f`. Expected-head CAS37f02→0942445 без
force подтверждён независимым PR read. [PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88)
OPEN/DRAFT, body updated. [CI599/39714516345](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39714516345)
in_progress, new repair parser/compiler/native NOT_RUN. Full41 preflight/copy/
rollback/self-check/journal/recovery/native failure vectors теперь входят в этот
source. Local token guard a75df7b/7cc8159 НЕ входит; он остаётся сохранён локально.

7 files2669 additions: secret/PII0/public320/0/diff/C#syntax/target C#5 structure/
full tree PASS; workflows/VERSION unchanged, uploads OFF. Новая публикация не
отменила598 — тот завершён SUCCESS, его score22/raw21.75/package только exact
source37f02/merge16bf1. I58–80, прирост за новый source0. Run599 не дублировался.

После обрыва сначала этот же run ID и actual PR head; не повторять push/CI. Не
менять head ради checkpoint docs, пока599 active. Далее независимый R09 worker
import с cancellation/ownership; если599 упадёт — изолированный exact fix без
смешивания незавершённого R09. Binary/private delivery/owner PC/installer blocked.

## Перед repair publication — pending object, ref ещё NOT_RUN

GitHub object0942445dd268f836c5001ec2bf82d27bafc6f3a5 создан с parent
37f02d2a0648fe2efebd7fc932b81668d26c44be; full tree4297436ce6a3ad896783a9ea45e070c593aa0eac
точно равен isolated local3f545dd2190b23344e200c56b0427ff8cc70e00f.
7 files:4 repair source/test +3 current evidence docs. Secret/PII0/public320/0/
diff/tree PASS; workflows/VERSION unchanged, public uploads OFF. New Windows
compilation/native NOT_RUN. Local token guarda75df7b/7cc8159 исключён.

После обрыва сначала actual PR head: ref мог быть обновлён после этого checkpoint.
Плановый CAS expected37f02→0942445 без force, затем сохранить actual merge/run ID;
не создавать duplicate run. CI598 завершён SUCCESS и не будет отменён новым head.
I58–80, verified22 exact598,0 баллов за candidate/создание object. Публичные binary/
installer/release/main merge/owner PC/VPS действия не выполняются.

## CI598 завершён SUCCESS — exact source37f02 / merge16bf1

[CI598/39711511948](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39711511948),
Windows119603692452 и Ubuntu119603692348 SUCCESS. Build identity
`0.2.2+16bf1e50b66baf1a724a0d2da642b2fe70bb3a46` совпадает. Source `37f02d2a0648fe2efebd7fc932b81668d26c44be`;
matching local85f33/treee6f. Точный native regression full StopAll PASS в
02:40:45.557 UTC: старый queued observer не поглощает новую physical stop operation.
13 новых phone cases PASS:5 Context,3 service preparation/cleanup,3 wizard,
2 relay lifecycle. Actual relay flow drain/EOF/ports tests PASS.

Regression: recovery51/456; bounded43/report32/helper2/modal6/chooser3/
diagnosticContext3/worker4/route6; signing55/UI50/prep72; Desktop5274/Instance28/
Maintenance126/Shutdown47/HomeVPN1022/Portable90. Actual direct и owned-agent
prepare→native OpenSSH→SOCKS→owned HTTP→child crash→fresh recovery PASS. Native
startup direct1966/prep37ms, recovery1957/prep37,42; agent2064/prep139 и
recovery2048/prep139,128. Это CI hardware, owner HDD/whole-app I/O NOT_RUN.
Поздний Python28=26PASS2SKIP; SKIP не принят за native PASS.

Runner ZIP0.2.2 SHA256 `576f6034b4768e148a0533ada7596136ef06950dd01ee8d5a19476fcac8d932b`.
Все uploads SKIPPED: bytes не получены/не доставлены/не установлены. Private
передача BLOCKED до согласования канала владельцем. Нельзя ссылаться на этот ZIP
как на доступный installer. Release/main merge/owner PC/VPS действий не было.

V1.0:28 parents weight87 с timestamp anchors в JSON; D0.5/P0.5/A0 дают21.75→22.
Рост raw+11.25 к partial59710.50 подтверждён полными native/runner package,
не13 написанными tests. I58–80 без изменения. F19/F27/F30/target acceptance0;
full F31-R10/R11 остаются PARTIAL. Локальные repair144c616…fa48f29 и token guard
a75df7b/7cc8159 НЕ входят в598, их Windows NOT_RUN/баллы0.

Далее — isolated repair checking sourceb342 плюс эта evidence документация,
проверка exact delta secrets/PII/workflow guards/tree и expected-head publication.
Один новый run, не rerun598. Независимый import guard остаётся local отдельно.
Ниже checkpoints сохранены как история; их in_progress/NOT_RUN отражали прежний
момент, актуальный итог598 находится в этом разделе и ci_history JSON.

## F31-R09 narrow import guard, local Windows NOT_RUN

Source a75df7b9c62ef1638019886821ecce4b45b76d62 и fixture cleanup followup
7cc8159629d2cc085c575fdd65687f136bceedb7. Подтверждён source defect: прежний
UseToken вызывал void Stop, терял false actual cleanup и затем сохранял новый
доступ поверх неочищенного session. Теперь требуется true physical StopAsync
result ДО первого Save/Access/Owner replacement, иначе явный отказ с retry.

В existing native FileShare.None cleanup fixture добавлен import другого owner:
прежние Access/Owner references и access/owner DPAPI bytes должны остаться точно
исходными, session ownership удерживается, новый relay/PID не появляется. Fixture
держит Task импорта и после выхода из lock scope ждёт его в bounded finally до
восстановления private test data. Source order/syntax/C#5 structure/public320/0/
diff PASS. Windows compilation/native NOT_RUN, код только local и НЕ входит ни
в598, ни в repair checkingb342. Синхронный UI import и multi-file persistence/
owner cancellation всё ещё F31-R09 FAIL; полной готовности не объявляем.

CI598 native relay step SUCCESS, public content step in_progress; итог/ZIP ещё
не подтверждены. I58–80, verified11 exact597, прирост0. Далее final598 logs,
потом отдельный repair checking publication; local import guard сохраняется.

## Repair checking candidate готов локально, публикация ещё NOT_RUN

Isolated local `b342dfc4c055de5b4e8e7d1733fdb39b0e65ada4`, tree
`2b9640811e30ccfc5e79c26e767b50830aa5712c`, worktree ProGo-ci598-repair.
На published matching85f33 включены только4 пути: Repair-ProGo.ps1,
Test-ProGo.ps1, MaintenanceOperation.cs и новый RepairPayloadTests.ps1.
4 files727 additions: secret/PII scan0; public320/0, modern C#syntax/targeted
C#5 structure/diff PASS. Workflow/VERSION unchanged, upload guards4/4 intact.

CI598 источник не меняется; repository build step уже SUCCESS, отдельный native
relay step in_progress. Новые compilation/native repair NOT_RUN. После final
jobs/log проверяется actual PR source37f02, затем candidate допускает одну новую
CI публикацию с full-tree equality. Сборка/доставка бинарей, main merge/release
не разрешались и не выполнялись. I58–80, verified11 exact597; баллы за подготовку0.

## F22-R03 этап7 — recovery отказ/cleanup, local fa48f29d1fb1b74563b83e9b1b58988499bfe4ed

Review выявил возможность пропустить Dispose lease/maintenance при ошибке
удаления своего temporary file. Nested finally теперь освобождает оба owner
независимо от этого отказа; snapshot/journal не объявляются завершёнными.
Non-object journal и занятый обычным файлом stage path имеют явный отказ.
Дополнены native vectors: string вместо bool, неправильный admission prefix,
несогласованный phase, повреждённый old snapshot. Все9 refusal scenarios должны
оставлять exact baseline установки/journal/snapshot bytes; реальное выполнение
Windows пока NOT_RUN. Source cleanup/validation contracts/public320/0/diff PASS.

Новые исходники остаются local, CI598 проверяет только source37f02/merge16bf1.
I58–80, verified11(raw10.50 exact597), прирост0. Следующий этап — isolated repair
checking tree, секреты/PII/diff/workflow guards, публикация только после завершения
того же598. Full F22-R03/приёмка владельца остаются открытыми.

## F22-R03 этап6 — explicit recovery, local source 017553607c1eac01f9eb382bb904c2f0a5a0818e

Отдельный `Repair-ProGo.ps1 -Recover` восстанавливает только известные admitted
runtime paths по проверенным snapshots. Schema1/32-hex GUID/41 canonical paths,
порядок prefix admission, boolean/hash types, bounded64KiB journal и hash тех же
прочитанных bytes проверяются до runtime writes. Lease защищает от второго owner;
redirected stage/runtime paths запрещены, tree проверяется до descent. Foreign
runtime или повреждённый old snapshot сохраняют journal/stage, не перезаписываются.

Applying recovery использует hash-verified staged Maintenance helper и общий
maintenance gate/application-stopped/proxy-cleanup checks. Original payload ему
не нужен. Committed outcome только очищается, без rollback/self-check/launch;
rolled-back cleanup допускает повторное завершение. Temporary atomic write files
теперь внутри owned stage — killed writer не оставляет их рядом с private data.

Добавлен Windows regression: настоящий repair parent принудительно завершается
на barrier двух собственных native check processes; оба должны закончиться через
kill-on-close job. Pending default repair/schema2/traversal/foreign runtime/repeat
recovery дают явный отказ, exact installation snapshots неизменны. Затем payload
удаляется и explicit recovery должен вернуть все исходные runtime/private bytes.
Все child waits ограничены, cleanup касается только observed own process handles.
Source contracts/public320/0/diff PASS; PS parser/C#5 shared helper/native repair
NOT_RUN. Это не Windows PASS и не проверка power-loss durability/целевого ПК.

Рабочее дерево после source commit чистое до этой checkpoint документации.
CI598/39711511948 всё ещё Windows in_progress, Ubuntu SUCCESS; published source
37f02d2a0648fe2efebd7fc932b81668d26c44be/merge16bf1e50b66baf1a724a0d2da642b2fe70bb3a46
не менялся. Jobs Windows119603692452/Ubuntu119603692348; stale older job/pending
fields в active JSON перенесены в historical checkpoint fields. Repair не входит
в598. Следующий этап: review phase/conflict interruption boundaries, затем exact
repair Windows candidate после завершения598. F22-R03 всё ещё открыт.
I58–80, verified11/raw10.50 exact597, прирост0. Публикации/нового CI не было.

## F22-R03 этап5 — durable admission journal, только local

Repair пишет relative schema1/GUID journal: preparing, проверенный prepared,
applying ДО каждой atomic OS replacement, committed только после actual
self-check/повторных hashes, rolled-back перед cleanup. Новый record сначала
Flush(true) в собственном temp, затем atomic move/replace; чужой transaction_id
не перезаписывается. Pending journal запрещает новый repair до payload
validation, установка не меняется повторно. В record нет secret/private values
или прежних Windows absolute paths, только runtime names/hashes/состояния.

При неподтверждённом durable cleanup snapshot/journal сохраняются, restored
bytes не объявляются готовым приложением. Существующий negative native check
наблюдает41 entries/41 admitted paths/phaseapplying/selfcheckfalse во время
своих живых root+child; после обычного отказа ожидается точный rollback без
journal residue. Source Flush/admission/order/guard/public320/0/diff PASS;
PS parser/native NOT_RUN, не входит в598. Hard-kill/recovery ещё не выполнен.

F22-R03 остаётся FAIL/partial local: явный recovery command со строгим schema/
path/hash ownership ещё не реализован. Следующий этап — recovery и настоящий
test termination родителя с его own job, затем malformed/foreign journal refusal
и exact Windows. Recovery не будет повторять self-check/запуск автоматически.
I58–80, verified11(exact597),0 баллов за написанный journal. CI598 в работе;
новый head или дублирующий CI не создавались.

## CI598 — exact physical StopAll fix опубликован, один run

Source `37f02d2a0648fe2efebd7fc932b81668d26c44be`, merge candidate
`16bf1e50b66baf1a724a0d2da642b2fe70bb3a46`, tree
`e6f261e2bdadc227eac3394dc83dc43e43294e05` = local
`85f33fdcae96df1231cfeb91af692eb7ad40f97e`.
[CI598/39711511948](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39711511948)
in_progress, new compilation/native NOT_RUN. Scan2files76additions0 secrets/
PII/public319/0/syntax/C#5structure/diff/full tree PASS. Expected-head lease
подтверждён, PR88 OPEN/DRAFT updated; workflows/VERSION unchanged, uploads OFF.
Repair stages144c616/5157f78/46fdd45/9d88165 excluded, их Windows NOT_RUN.

I58–80, verified11/raw10.50 принадлежит exact597 partial native, не598. Ни5
новых Context cases, ни написанный regression не дают отдельных parent баллов.
Public release/binary upload/main merge/owner PC/VPS/system settings не делались.
После обрыва проверить этот же598/actual PR head, не повторять push/CI. Далее
independent durable repair journal (до каждой atomic runtime записи), затем
восстановление/interrupt regression отдельным local этапом. Blocker full StopAll
остаётся до exact Windows; full F31-R10/R11 не закрыт.

## CI597 scoped native PASS, full StopAll FAIL — exact identity fix pending

Source `b1fad3c6a6804ab6055b146b5cf284f5136d4288`, tested merge
`699b50ee7db44cc6ec851d7fcc36069fa574611a`, run39708048332 FAILURE;
Windows119592612485/Ubuntu119592612681 FAILURE/SUCCESS. Actual build identity
matched. C#5 fix и main/Core/Desktop compilation PASS; later Home harness
compilation/native NOT_RUN. Native51/456/bounded43/report32/helper2/modal6/
chooser3/diagnosticContext3/logger+worker4/routes6 и5 новых phone Context PASS.
Settings budget phone shutdown действительно3s, не250ms:250ms был только
backup budget fixture (прежнее описание уточнено). Native log02:03:28→31.

В02:04:09 CheckStopScopes full-stop assertion FAIL. Подтверждённый user defect:
StopPhoneAsync объединял запросы по незавершённому UI observation, хотя его
физический Stop уже завершился и channel был заново запущен. StopAll возвращал
прежний task и не запускал остановку нового channel. Новый isolated candidate
сначала резервирует physical Home.StopAsync и объединяет UI observers только
для того же самого Task; stale callback не стирает новое состояние.

Isolated local85f33fdcae96df1231cfeb91af692eb7ad40f97e (первый local93ec714),
treee6f261e2bdadc227eac3394dc83dc43e43294e05 = GitHub object
37f02d2a0648fe2efebd7fc932b81668d26c44be. Earlier object2c7fee создан, но ref
не обновлялся; superseded only. Regression явно держит очередь UI: physical
cleanup awaited без pump, свежий real loopback relay, новая stop identity,
confirmed stop обоих каналов/нет timer revival. Fixture peer/startup/cleanup
ограничены5s и освобождают только свои sockets/session, проверки не отключены.
Syntax/target C#5 structural/public319/0/diff/tree PASS;2files76additions
secret/PII0, workflows/VERSION unchanged. New Windows compilation/native NOT_RUN.
Следующий шаг expectedb1fad→37f02 после проверки head; не rerun597. All repair
blocks до9d88165 excluded, source импортируется без потери local repair work.

V1.0 exact59719 timestamp-traced parents weight70×D0.5×30%=raw10.50→11;
P/A0, later full Desktop/selected-key combined/Home/relay/ZIP NOT_RUN. +10.50 к
compile-only596 отражает выполненные native критерии, не written tests. Новые
5 Context cases не дают отдельных дополнительных parent баллов. Historical
59522 сохранён только для его source/merge/package. I58–80 unchanged;
полная готовность F31-R10/R11 не объявляется.

## F22-R03 этап4 — actual installed self-check before commit, local NOT_RUN

Repair участвует в общем Maintenance gate и требует остановленное приложение/
завершённую proxy cleanup до target writes. Actual installed EXE --self-check
выполняется до committed/success/launch. Ему выделен fresh LOCALAPPDATA внутри
owned stage: smoke-check не мигрирует и не переписывает настоящие profiles/
settings/vault. Это не приёмка реального профиля/ПК. После child exit повторно
сверяются все runtime hashes; отказ вызывает прежний conditional rollback.

Shared MaintenanceOperation получил opt-in RunOwnedSelfCheck: suspended child
помещён в kill-on-close Job до исполнения, root wait8s, tree cleanup3s. При
смерти PowerShell owner kernel закрывает только этот job. Default accepted
handoff/старые cleanup budgets не менялись. Fresh prepared helper API требуется;
old-loaded-API transition не принят. Старые missing/locked-copy tests10s
сохранены; новый self-check budget20s обоснован8+3+staging/PS startup, не retry
ради зелёного результата.

Positive case использует настоящий built ProGo. Negative native PE создаёт
своего child и зависает; PID marker публикуется атомарно, parent наблюдает
проверенные process handles, ожидает failed result, оба exited и полный byte
rollback. Это failure/cleanup fixture, не подмена functional application test.
C#syntax/public320/0/self-check-before-commit/diff PASS; target C#5 compiler,
PS parser/native NOT_RUN, не входят в597. F22-R03 остаётся открытым: durable
interruption journal и exact Windows regression ещё отсутствуют.

CI597 теперь completed FAILURE в существующем full Stop scope после5 новых
Context scoped PASS; exact log сохранён, причина разбирается отдельно. Этот
repair block фиксируется независимо до переключения на изолированный StopAll
fix. I58–80, verified0(last evaluated596), новых баллов за repair0;597 evidence
будет оценено только с exact native anchors, не по написанным тестам.

## F22-R03 этап3 — owned atomic runtime rollback, local NOT_RUN

Repair резервирует per-install exclusive file lease, до первой замены готовит
проверенные snapshots старых и новых41 leaves. Каждый file commit атомарный
через собственный verified temp; путь записан как applied до OS call. При
ошибке rollback меняет только байты, совпавшие с этим кандидатом, либо видит
неизменённый baseline. Чужое изменение/неподтверждённый путь не перезаписывается:
owned snapshot сохраняется и выдаётся явная ошибка incomplete rollback.
Target links не используются для перенаправления владения на внешние файлы.
Свои temp files/lease и пустые созданные directories очищаются; existing
private/unknown файлы вне runtime list. Literal .NET directory creation
сохраняет корректность путей с пробелами/brackets.

Native regression удерживает VERSION FileShare.Read (чтение snapshot разрешено,
atomic replacement запрещён) после первой замены ProGo.exe. Проверяет failed
exit, возврат всего исходного runtime/private hash tree и отсутствие residues;
child10s/output+cleanup3s, NoLaunch, только свои процессы/папки. Source atomic/
ownership/order/public320/0/diff PASS, PS parser/native NOT_RUN. Не входит в597.

F22-R03 всё ещё FAIL: self-check перед success/launch и durable interruption
journal ещё не реализованы. Snapshot при конфликте не считается восстановленной
установкой. Следующий этап self-check/failure regression, затем journal/recovery.
I58–80, current verified0(compile-failed596), прирост0; historical59522 сохранён.
CI597 в работе, новый head/дублирующий CI не запускались.

## CI597 — C#5 fix опубликован, один exact-SHA run

Source `b1fad3c6a6804ab6055b146b5cf284f5136d4288`, merge candidate
`699b50ee7db44cc6ec851d7fcc36069fa574611a`, tree
`ca320af4f5a6df1b556101a56c0b7e011eddc018` = local
`272c42697ba7f0928626de2a11bcef6d114d913a`.
[CI597/39708048332](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39708048332)
in_progress; new Windows compilation/native NOT_RUN. Expected-head lease
подтверждён, PR88 OPEN/DRAFT updated. One-file scan29 additions0 secrets/PII,
syntax/targeted C#5 structural/public319/0/diff/tree PASS; workflows/VERSION
unchanged, public uploads OFF. No native test disabled or deadline increased.

Repair144c616/5157f78 и последующие local blocks исключены. Source596 native
NOT_RUN не подменяется историческим595 SUCCESS22. Current verified0 относится
к compile-failed596, I58–80 unchanged,597 ещё0 новых баллов. После обрыва читать
этот run/head, не повторять push/CI. Далее independent repair runtime rollback,
затем self-check и interruption recovery отдельными этапами. Public release/
binary upload/main merge/целевой ПК/VPS/system settings не выполнялись.

## CI596 compilation FAIL — C#5 compatibility fix готов, ref PENDING

Source654551f80feb485c1ca1f7889797d0cf362c0cf4 / merge3f67193a277574e128a8b06cb1a4ac502d873440,
[CI596/39707142123](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39707142123)
FAILURE. Windows119589733115 CS1985 at Ikev2RelayService.cs:116, symbol
StartOwnedAsync: установленный Framework C#5 compiler запрещает await в catch.
Ubuntu119589732810 SUCCESS. Native13 phone cases/relay regression/ZIP NOT_RUN,
uploads SKIPPED. Современный syntax parser этого target limitation не ловил;
его PASS не выдавался за compilation. Подтверждённый source defect, не flake.

Isolated local272c42697ba7f0928626de2a11bcef6d114d913a / tree
ca320af4f5a6df1b556101a56c0b7e011eddc018 = GitHub object
b1fad3c6a6804ab6055b146b5cf284f5136d4288. Catch лишь фиксирует exception;
await cleanup после него остаётся внутри outer CTS lifetime/finally. Прежняя
классификация cancel/cleanup failure, все tests/deadlines сохранены. Syntax и
целевой structural scan C#5 catch/finally/lock await во всех11 C# files PASS;
это ещё не target compiler. Scan1file29additions0 secrets/PII/public319/0/
diff/full tree equality PASS; workflows/VERSION unchanged. Windows fix NOT_RUN.

До публикации проверить actualhead PR88; один expected654551→b1fad без force,
новый SHA CI. Не rerun596. Local repair144c616/5157f78 исключён, импорт fix в
root не стирает его. Текущая exact596 verification0/100(raw0): native/P/A не
достигнуты; это не FAIL всех функций. Historical59522/raw21.75 сохранён только
для5f342/a285. I58–80 unchanged, прирост за code fix0. Главный blocker — exact
Windows compilation и все новые phone/relay cases после target-compatible fix.

## F22-R03 этап2 — полный single-payload runtime copy, local NOT_RUN

Repair больше не смешивает script files из PSScriptRoot с payload и не
пропускает отсутствующие компоненты. Сначала фиксирует SHA256 всех41 runtime
leaves, затем копирует только эти известные пути, сверяет каждый destination
и повторно весь набор. Включены ранее пропускаемые Install helper,2 update CS
и9 home-vpn resources. Settings/vault/keys/profiles/backups/unknown files не
входят в copy list. Окончание называется runtime restored+verified, не Repair OK.

Положительный native child case берёт настоящий Build payload, NoLaunch,
сверяет каждый built file и прежние private bytes. Сохранились4 negative
preflight cases; child10s/output+cleanup3s, только свои процессы/папки.
Static single-source/hash-order/41 parity/public320/0/diff PASS; PS parser/
native NOT_RUN, не входят в596 или phone654551. Native test counts пока0 PASS.

Это ещё не готовый repair: rollback при отказе посреди копирования и self-check
перед success/launch отсутствуют. F22-R03 остаётся FAIL до следующих блоков и
exact Windows. Следующая отдельная микрозадача — transaction/self-check и
failure regression, затем interruption recovery. I58–80, verified22(exact595),
прирост0 за этот local source. Основной596 продолжает только phone/relay scope.

## CI596 — phone/relay checking source опубликован, один run

Source `654551f80feb485c1ca1f7889797d0cf362c0cf4`, merge candidate
`3f67193a277574e128a8b06cb1a4ac502d873440`, tree
`daf59cf31a82ad253a9914f9b17fae8b6bf53034` = local
`3d3301a5f95ddfcb5ad60af5555a6de7ae1025b1`.
[CI596/39707142123](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39707142123)
in_progress; новые phone/relay Windows compilation/native NOT_RUN. Scan exact
16paths1835additions0 secrets/PII/full tree equality PASS, workflows/VERSION
unchanged, uploads guards подтверждены предыдущим595 SKIPPED. Expected-head
без force успешно проверен, PR88 OPEN/DRAFT updated. Root repair144c616 НЕ
входит в checking source, его Windows parser/native NOT_RUN.

Score22/raw21.75 принадлежит exact595 source5f342/mergea285, не596. Будущий
результат596 не распространять на repair, который продолжается независимо.
При восстановлении проверить PRhead и тот же run596, повтор push/CI не делать.
Следующий шаг: exact596 logs; пока он идёт full repair copy/verification,
transaction/self-check отдельными local commits. Release/binary upload/main
merge/ПК владельца/VPS/system settings не выполнялись. I58–80, прирост за push0.

## CI595 completed SUCCESS — exact R08 и selected-key pipeline proof

Source `5f3420d72e5f8c8ac282af6ca7ac57db1d89534c`, tested merge
`a2851df351a8ef86c7965761821f64a66c2dd960`, local matching tree
`ddb95e7ed68f1cf1ad3f27130575f2a4c923df97`.
[CI595/39704288681](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39704288681):
Windows119580544561/Ubuntu119580545361 SUCCESS, identity0.2.2+merge matched.
Exact Windows log прочитан: bounded43/report32/helper2,6 modal Cancel→Retry,
3 chooser/3 diagnostic Context,4 open workers/6 actual viewer routes, actual
SafeLog worker+3 epoch assertions+2 owner shutdown/Dispose cases PASS. Missing
fixture epoch fix native producer/entry/heartbeat/redaction/file PASS, all
later suites completed. Full R08 не закрыт: PS updater cold/helper/viewer I/O,
old-loaded-API transition и owner slow hardware/DPI/Narrator ещё открыты.

Fresh selected-key evidence: signing55/UI50/aggregate72/recovery51/456 PASS;
настоящие direct и owned-agent local preparation→OpenSSH→SOCKS→owned HTTP→
автоматический recovery PASS. Isolated metrics direct startup1954/prep33ms,
recovery1947/prep33,34ms; agent startup2012/prep90ms, recovery2007/prep90,93ms.
Own SSH CPU31.25/15.625ms, RAM~11MB, whole app I/O/owner ПК/VPS NOT_RUN.
Desktop5240/Instance28/Maintenance126/Shutdown47/HomeVPN976/Portable90 PASS.
Prepared native SSH server contracts5 и later relay Python28 (26PASS/2SKIP)
реально выполнены; discovery fix не отключил SSH cases и не требует ambient
Paramiko. NOT_RUN/skipped не включены как PASS.

Runner ZIP0.2.2 SHA256
`f1ba5b17e02d4cc0a36b8265851748b1b2529c4dee228b4279cd5ba5c7e5b27b`.
Uploads SKIPPED, bytes не получены/не переданы/не установлены. Private delivery
BLOCKED до согласованного владельцем канала; binary upload/release запрещены.
V1.0 unchanged:28 parents weight87,D0.5/P0.5/A0 =>raw21.75→22. +11.25 raw к594
из реально выполненных дополнительных native критериев и runner ZIP, не из
числа коммитов. Все28 timestamp anchors в JSON. I58–80 unchanged.

Phone object654551/source3d3301a и repair144c616 НЕ входят в595, их Windows
compilation/native NOT_RUN. Следующий шаг — проверить PR88 actualhead и один
expected5f342→654551 без force, новый exact checking SHA/merge/run. Source scan
16paths1835additions0 secrets/PII/full tree PASS; workflows/VERSION unchanged.
Repair остаётся независимым local block, не добавлять его в phone tree.

## F22-R03 этап1 — repair payload preflight, только local

Repair принимает опциональный PayloadDirectory готового payload (default source
repo/release сохранён). До любого New-Item/копирования в installation проверяет
41 обязательный непустой file. Native exe/helpers/home-vpn отсутствующий или
пустой компонент даёт явный отказ с relative names, не «Repair OK».
Состав статически совпал с независимыми Build outputs:3 root+17PS+12CS+9Home.
Это presence/length preflight, не signature/whole repair proof.

4 новых negative Windows child cases берут настоящий built payload и удаляют
exe/InstalledUpdateTransport.cs/home-vpn profile_share.py либо обнуляют updater.
Только собственный LOCALAPPDATA fixture, hash каждого private fixture file и
отсутствие нового scripts dir после отказа; NoLaunch. Process10s/output3s,
finally kill только созданного child при timeout и cleanup streams3s; timeout
не превращается в PASS. Suite явно wired после Build. PS testASCII и main BOM
сохранены. Source41 parity/order/public320/0/diff PASS; PS parser/native NOT_RUN.
Не входят в running595 или prepared phone object654551.

F22-R03 остаётся FAIL: legacy copy ещё неполон/mixed-source, self-check и
transaction rollback отсутствуют. Следующий отдельный блок — полный prepared
runtime copy+verification; затем transaction/self-check. Никакой установки на
ПК владельца, launch/release/upload не выполнялось. Score11(exact594), I58–80,
прирост0. CI595 repository build уже SUCCESS по jobs, relay in_progress;
точный полный log/итог/ZIP ещё не проверены, не объявлять полный SUCCESS.

## Phone checking object подготовлен — публикация PENDING completion595

Fixed local `3d3301a5f95ddfcb5ad60af5555a6de7ae1025b1`, GitHub object
`654551f80feb485c1ca1f7889797d0cf362c0cf4`, одинаковый полный tree
`daf59cf31a82ad253a9914f9b17fae8b6bf53034`. 16 paths/1835 additions,
secret/PII findings0; public319/0, diff/JSON/syntax11/wiring PASS. Canonical
committed blobs прочитаны полностью, GitHub full tree equality подтверждён;
GitHub commit metadata отличается от local SHA, это не разные runtime bytes.
Workflows/VERSION unchanged, public binary uploads OFF. Новый code Windows
compilation/native NOT_RUN, не входит в595 и не получает его будущий SUCCESS.

Branch пока5f3420d7, единственный CI595/39704288681 in_progress. После обрыва
сначала проверить этот run и PR88 head. Только после установленного completion
и exact logs один expected-head5f342→654551 без force, новый actual merge/run.
Старый595 не перезапускать и не отменять head update заранее. Если595 FAIL,
его причина приоритетнее публикации phone: fix держать изолированно, object
не обновлять вслепую (parent lease проверяется). Исходники/commits сохранены,
ничего не удалено/не откатано. Score11 принадлежит594, прирост за object0.

## Phone/relay final checking review —11 C# syntax PASS, native NOT_RUN

Все5 logical blocks сохранены отдельными commits: service owner df104d5,
wizard f5b2236, Context89dfcf6, Run drain6b4ec6f, public relay a34b0db.
11 изменённых C# files parse0errors; wiring новых Desktop tests и явного
console StopAsync await проверен, PowerShell UTF8 BOM сохранён. Backend Python4
PASS,3 C# cases skipped=NOT_RUN; public319/0 и diff PASS. Это self-review,
independent agent review и Windows compilation/native NOT_RUN. В595 не входят.

Небольшой callback guard предотвращает obsolete Stop failure balloon, если
другая попытка service Stop уже стала текущей; actual old task сохраняет свой
результат, UI/новая попытка не перезаписываются. ОстаткиR09 и аварийная/целевая
приёмка не скрыты. Следующий фиксированный checking tree включает только
phone/relay source/tests и checkpoints, после secret/PII/workflow scan. Ref
обновлять только после установленного completion595; нового CI на старом SHA
не создавать. Score11 относится к completed594, новые изменения дают0.
Реализация58–80; полная готовность R10/R11 не объявляется.

## R10 relay этап5 — public async startup/Stop/Dispose, local NOT_RUN

Start и Stop публикуют свои task до native worker. Дубликат реального pending
Start получает тот же task. Stop сразу закрывает admission, вне metadata gate
отменяет и ждёт startup probe, затем все retiring Run tasks; новый Start не
обходит pending/faulted retire. UDP/timer setup выполняется worker вне service
metadata gate, stale startup не публикуется после Stop/Dispose reservation.
Dispose idempotent, сохраняет StopWork и не ждёт UI. Home cleanup ждёт этот
настоящий relay lifetime на своём worker. Console harness явно ждёт StopAsync
перед exit: process exit не выдаётся за успешно завершённую очистку.

2 actual native Windows Forms cases держат настоящий loopback SOCKS probe:
Stop и Dispose, EOF своего peer до результата, один start/stop owner, новый
Start refused до settlement, heartbeat, оба UDP ports свободны. Barrier/peer/
cleanup waits3/4/5s; finally release, свои listener/client/task закрыты, ни один
чужой process/socket не затрагивается. C#syntax3/public319/0/diff PASS,
Python backend4 PASS, native harness3 skipped=NOT_RUN. New Windows compilation/
native NOT_RUN, не относится к595. Пять local phone/relay этапов не опубликованы.

Остаток: один exact-SHA checking run всех13 новых service/wizard/Context/probe
cases и усиленного real UDP/SOCKS flow drain; token import всё ещё sync Stop/
DPAPI на UI (R09). Forced process kill/restart recovery и целевой ПК/реальный
телефон NOT_RUN: retained managed task живёт лишь пока жив процесс; полная
аварийная приёмка не выдумывается. F31-R10 PARTIAL. Реализация58–80,
verified11(exact594), прирост0 за исходники и невыполненные native tests.
Далее финальный review/scan checking tree и тот же CI595 result.

## R10 relay этап4 — Run receive/flow tasks отслеживаются, local NOT_RUN

Run сохраняет оба receiver task и все незавершённые flow workers. Один stop
reservation ставится до worker, закрывает свои сокеты/flows и ждёт Task.WhenAll
прежде чем завершить результат. Self-stopping Run регистрируется в retiring
list до удаления current: concurrent cleanup больше не может потерять его.
Успешные завершения удаляются, неожиданный fault не превращается в success.
Serialized compatibility Stop ждёт этот drain на Home cleanup worker/console.

Существующий real Windows frontend test усилен без новых моков:4 потока по
обоим UDP ports через own SOCKS, STOPPED0 acknowledgment после task settlement,
EOF всех своих TCP handlers и rebind обоих UDP ports. Fixture teardown теперь
имеет bounded waits и finally убивает только свой subprocess при ошибке/
timeout, закрывает свои servers/tasks; timeout остаётся FAIL. Backend Python4
PASS,3 C# harness cases skipped=NOT_RUN. C#syntax2/public319/0/diff PASS,
Windows compilation/native NOT_RUN, в595 не входит.

Это отдельный private Run этап: public relay startup/StopAsync admission и
Dispose ещё требуют следующей микрозадачи. Полный R10 не закрыт; UseToken
UI Stop/DPAPI остаётся R09. Source не опубликован. Реализация58–80,
verified11(exact594),0 новых баллов. Далее public relay lifetime reservation,
затем checking tree после completion595.

## CI595 — isolated SafeLog fixture fix опубликован, один run

Source `5f3420d72e5f8c8ac282af6ca7ac57db1d89534c`, merge candidate
`a2851df351a8ef86c7965761821f64a66c2dd960`, tree
`ddb95e7ed68f1cf1ad3f27130575f2a4c923df97` = local
`a70943991c96907afd829f715bf4fa75a9ee6552`.
[CI595/39704288681](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39704288681)
in_progress. One-file fix9 additions, secrets/PII0, source syntax/public318/0/
diff/tree PASS, new Windows compilation/native NOT_RUN. Expected-head lease
подтверждён; PR88 OPEN/DRAFT обновлён. Phone stages остаются только local и в595
НЕ входят. Full CI594 не был SUCCESS, его узкие PASS относятся только к6f615/
5cf4. Score11/raw10.50 — completed594, не новая проверка595; I58–80 unchanged.
Никаких public binaries/release/main merge. После обрыва проверить595/head,
не повторять push/CI. Далее independent local R10 relay task ownership stage.

## CI594 — R08 compilation PASS, native PARTIAL; isolated fixture fix pending

[CI594/39702615908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39702615908)
completed FAILURE, source6f615c9fa7243f1365b721a6b78f17dcda6a6538 /
merge5cf4a131b653439396c79f87f84a3f4dc97544ec, exact build identity matched.
Windows119575163356 FAILURE, Ubuntu119575163598 SUCCESS. Parser/BOM fix PASS,
R08 C# compilation PASS; phone stages excluded. Native recovery51/456,
bounded log43/report32/installed helper2,6 actual modal Cancel→Retry and3
chooser, prepared real Windows SSH server contracts5 PASS. In01:06:17 first
SafeLogWorkerWorkflow elapsed/entered assertion FAIL; full Context/log-open,
selected-agent combined and later Desktop/tail/ZIP NOT_RUN, uploads SKIPPED.

Missing fixture precondition confirmed in source: standalone test never creates
its logger epoch after preceding Context.Dispose seals queue admission. The
exact paused/drop cause of failed combined assertion is an inference (old log
cannot distinguish elapsed vs entry); no false claim of product failure/fix.
Isolated a70943991c96907afd829f715bf4fa75a9ee6552 owns/drains/resumes one epoch,
finally suspends it; producer1000ms and native entry3000ms checked separately,
entry waits with real UI pump. Real sink/heartbeat/redaction/file checks remain.
No production changes or timeout increases,9 added lines. C#syntax1/public318/0/
diff/secrets0/full tree equality PASS; Windows new fix NOT_RUN. GitHub object
5f3420d72e5f8c8ac282af6ca7ac57db1d89534c, tree
 ddb95e7ed68f1cf1ad3f27130575f2a4c923df97, ref update PENDING expected6f615.
All root phone stages remain local and excluded from this checking object.

V1.0 exact59419 traced parents weight70×D0.5×30%, P/A0 =>raw10.50→11/100.
+10.50 raw vs593 preflight-only0 reflects actual native criteria, no new weight
or credit for fix code. Source/merge/timestamps in JSON; all whole IDs remain
open where their full criteria are unmet. Implementation58–80 unchanged.
Next: inspect PR head, publish isolated object once, preserve new run/merge;
no duplicate594. Workflows unchanged, no public binary/release/main merge.

## R10 checking review —9 C# syntax PASS, native остаётся NOT_RUN

Финальный self-review3 local phone этапов: syntax9files0errors, suite wiring
(explicit compile+entrypoint), PS UTF8 BOM, public319/0 и diff PASS. Добавлены
native assertions actual main phone card pending→stopped. Независимое agent
review NOT_RUN, Windows compiler/typecheck/native NOT_RUN, баллов0.

Уточнён остаток: sync compatibility Stop вызывается из UseToken на UI, это
не non-UI путь; его worker/DPAPI migration — F31-R09, пока не исправлен.
Relay.Stop закрывает сокеты, но не предоставляет tracked drain всех receive/
flow tasks; полную lifetime приёмку R10 до этой проверки не объявлять.
Новые native tests не подменяют real phone/VPS/owner integration. Следующий
checking source содержит только phone блоки и их tests/docs после completion
единственного CI594; никаких дубликатов или раннего head update.

## F31-R10 этап3 — actual tray/exit ownership, local Windows NOT_RUN

StopPhone/StopAll используют retained async observation; повтор Stop общий,
phone commands disabled до результата, StopDesktop остаётся независимым.
Result TCS завершается перед RefreshPendingRoutes, чтобы меню не зависло в
pending после cleanup; closed owner завершает observation без late UI.
Main phone card показывает «Остановка…»/«Запуск…»/«Нужна очистка» честно.

Shutdown/maintenance останавливают phone worker до изменений desktop proxy,
ждут реальную очистку в существующем settings budget и отказывают handoff при
timeout/false. Admission новых phone starts заранее paused; после отказа или
отмены prepared handoff явно resumed, после успешной подготовки остаётся sealed.
CompleteShutdown больше не выполняет sync Stop на UI. Forced Context.Dispose
вызывает idempotent Home.Dispose: метаданные резервируются до возврата, worker
сохраняется в StopWork без ожидания файлов/процессов на UI. UI Stop во всём
Context переведён; non-UI compatibility Stop остаётся для UseToken (R09).

5 actual Context fixtures: tray, StopAll, bounded refusal→retry→CancelShutdown,
ForceDispose, реальный locked-file refusal→retry. Реальные собственные session
files, native message loop, deadline250ms и fixture waits3/5s; всегда release и
await cleanup. Не выдаются за реальные SSH/phone integrations: те проверяются
отдельно. Новая suite явно включена в Test-ProGo/entrypoint; shared fixture
теперь ждёт Home.StopWork перед dispose Relay. Source syntax7/public319/0/diff
PASS, Windows compilation/native NOT_RUN; это не scope CI594.

Этапы1 df104d5 и2 f5b2236 сохранены отдельно; этот этап — отдельный local commit.
Далее финальный review/scan всех3 phone блоков и один exact Windows checking
SHA после completion594. R10 остаётся PARTIAL, R09 private IO остаётся открытым.
Реализация58–80, verified0(CI593),0 баллов за новые невыполненные тесты.

## F31-R10 этап2 — actual Wizard Stop использует async owner, local NOT_RUN

Кнопка «Остановить VPN для телефона» теперь идёт через RunStep/StopAsync,
явно сообщает pending/неполную очистку/подтверждённый результат и сбрасывает
только прежнее подтверждение phone internet. Конкурирующие действия disabled.
Title close ждёт successful cleanup, после ошибки окно остаётся для retry;
forced Dispose не прерывает owned task и подавляет late UI. Счётчики не
называют held/failed cleanup завершённой остановкой. Сохранённый доступ остаётся.

3 actual button native fixtures: stay+реальный locked-file failure/retry,
queued close и ForceDispose. Собственный child/session, heartbeat, ожидания
3/5/8s; finally release и await собственной очистки. C#syntax3/public318/0/
diff PASS, Windows compilation/native NOT_RUN (не входят в594). Source review
проверяет и gap между завершением StopChannel и outer RunStep: stopping flag
снимается только в общем finally, queued close не теряется в этом интервале.
Сервисный этап1 df104d5 сохранён отдельно. Полный F31-R10 остаётся открытым:
tray/StopAll/exit ещё sync compatibility, R09 private-load/save отдельно.
Следующий шаг — actual Context Stop/shutdown ownership и exact594 log.
Реализация58–80, verified0(CI593), прирост0 за невыполненные новые тесты.

## CI594 — BOM fix опубликован, один exact-SHA run

Source `6f615c9fa7243f1365b721a6b78f17dcda6a6538`, merge candidate
`5cf4a131b653439396c79f87f84a3f4dc97544ec`, tree
`bd0853c7ce82916a202a0b3c2495e24c824a854a` = local
`5fc228f144d6b91adb8a77f158a93785ae7c7db1`.
[CI594/39702615908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39702615908)
in_progress, compilation/native NOT_RUN. Точный diff:3 BOM bytes, никаких
изменений assertions/deadlines/guards. PR88 OPEN/DRAFT updated, expected-head
без force подтверждён; public uploads OFF, release/main merge не выполнялись.
Локальный phone lifecycle df104d5 в этот source НЕ входит. Проверить тот же
run после обрыва; второй CI/повтор push не создавать. Реализация58–80;
verified0 относится к completed593 preflight, historical59213 сохранён,
прирост за этот source0. Продолжить независимую local UI часть R10.

## CI593 parser FAILURE — BOM-only fix ready, ref PENDING

Sourcec3a67ebff9a6ac10e93059bc73dc2fadd8fef29a / merge30a39709b1144b99c493c49db0e10ada8f6cef2e,
run39701960142 completed FAILURE. Windows119573054034 остановился в00:49:43
на ParseFile tests/DiagnosticPreviewTests.ps1; Ubuntu119573054253 SUCCESS.
Новые C#/native R08, relay/new discovery/ZIP NOT_RUN; uploads SKIPPED.
Причина: добавленная русская header assertion в UTF8 без BOM. WindowsPS5.1
читает default ANSI; bytesD092 первой В становятся smart quote и ломают literal.
Локальный cp1252 decode воспроизводит smart quote; native parser ещё NOT_RUN.

Isolated local5fc228f144d6b91adb8a77f158a93785ae7c7db1 / treebd0853c7ce82916a202a0b3c2495e24c824a854a
= GitHub object6f615c9fa7243f1365b721a6b78f17dcda6a6538. Ровно3 BOM bytes,
все assertions/timeout/content сохранены (byte equality BOM+old PASS).
Public318/0/diff/tree equality PASS; semantic additions/secret/PII0; workflows
unchanged, public uploads OFF. Phone lifecycle df104d5 excluded, Windows NOT_RUN.
Следующий шаг: проверить actual PR head, один expectedc3a67→6f615 без force,
новый exact-SHA run. Не повторять593. Результат предыдущего source сохранён.

V1.0 current593 raw0→0/100: Windows проверки до критериев не дошли, P/A0.
Это отсутствие доказательств на новом SHA, не FAIL всех функций. CI59213/raw13.05
остаётся историей; его баллы новому source не приписываются. Реализация58–80,
прирост за BOM0. Главный ближайший blocker: exact R08 Windows execution.

## F31-R10 этап1 — owned phone lifecycle, local Windows NOT_RUN

HomeVpnService резервирует StartWork/StopWork до Task.Run. Повторный Start
получает тот же task; Stop/Dispose сразу резервируют один cleanup, отменяют
worker и ждут его фактического завершения перед удалением exact session.
Новый Start во время Stop отклоняется. ACL/key/known_hosts/config подготовка,
relay/native disposal/session delete теперь worker-only, короткий lifecycleGate
не держится на I/O. SSH inputs копируются на admission; отмена проверяется
между файловыми операциями и до child/relay publication. Stop false/fault не
теряет directory/process owner; повторная явная остановка пробует его снова.
Dispose не ждёт UI, StopWork остаётся доступным. RecoveryStatus читает один
snapshot proxy, избегая null-race между двумя чтениями.

3 bounded native fixture scenarios добавлены в существующий Windows harness:
held preparation→Stop, held preparation→Dispose, held cleanup + реальная
FileShare.None блокировка→false→retry. Проверяют native heartbeat, single
flight, отсутствие позднего child/config, saved DPAPI bytes и собственный
child cleanup. Каждый finally release+await cleanup; ожидания3/5/8s ограничены.
C#syntax2files/public318/0/diff PASS; Windows compilation/native NOT_RUN, не
входит в593. Это не end-to-end R10 PASS: tray/wizard/exit пока вызывают sync
compatibility Stop. R09 constructor/share/token/DPAPI I/O остаётся отдельно.
После сохранения этого локального блока приоритет — parser failure CI593,
отдельным isolated checking source без phone изменений. Баллов за source0.

## CI593 — R08 checking source опубликован, один run в работе

Source `c3a67ebff9a6ac10e93059bc73dc2fadd8fef29a`, merge candidate
`30a39709b1144b99c493c49db0e10ada8f6cef2e`, tree
`e3104d9fead1a4fff51d598b6099690cb72bcf4e` = local
`e62d21c9d84b110fd7f588c27c912cf48d861413`.
[CI593/39701960142](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39701960142)
in_progress; новые R08 Windows compilation/native NOT_RUN. Source scan17 paths/
929 additions0 secrets/PII; public318/0, diff/tree equality PASS, workflows/VERSION
unchanged, public binary uploads OFF. Expected-head4dd→c3a67 без force confirmed;
PR88 OPEN/DRAFT updated, main merge/release/installer upload не выполнялись.
Score13/raw13.05 принадлежит только предыдущему exact CI592 scoped native;
новый source даёт0 баллов до logs. Реализация58–80. После обрыва проверять
этот же593 и фактический head, не повторять push/CI. Пока он выполняется,
разрешена независимая локальная работа F31-R10; в593 она не входит.

## Текущее подтверждение CI592 — completed FAILURE, узкие Windows PASS

Source `4dd4987cf6f453847931820a6f9ba1298f99f802`, tested merge
`4cf49375895d64e79a951bfc926a01502f9a7352`.
[CI592/39698918707](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39698918707):
Windows119563052615 FAILURE, Ubuntu119563052956 SUCCESS. Build identity совпала.
Repository build/Test-ProGo SUCCESS: report32/helper2,4 modal Cancel→Retry,
3 chooser,3 diagnostic Context; recovery51/456/guidance10, signing55/UI50,
aggregate72, Desktop5132, Instance28/Maintenance126/Shutdown47 PASS.
Оба настоящих direct/owned-agent сценария прошли выбранный ключ→native
OpenSSH→SOCKS5→owned HTTP→прерывание child→повторная preparation/recovery.
Это локальный SOCKS HTTP payload, не полная проверка HTTP bridge/внешнего VPS.

Измерения из native log: direct startup2014/preparation29ms,
recovery1938/preparation29,28ms; agent startup1999/preparation87ms,
recovery1994/preparation87,82ms. CPU own SSH31.25/15.625ms, RAM~11MB.
Это изолированный runner, owner HDD/whole-process I/O/целевой ПК NOT_RUN.
HomeVPN975/Portable90 PASS; relay Python29 tests завершились с1 import error:
в00:43:53 старое test_ssh_loopback_fixture.py повторно обнаружено без Paramiko
после восстановления PYTHONPATH. Причина теперь подтверждена exact log;
new-name explicit prepared discovery уже исправлен отдельным local e62d21c.
Pack SKIPPED, новый ZIP/hash отсутствует; все uploads SKIPPED.

V1.0 прежние веса:28 родителей кромеF19/F27/F30, сумма87, D0.5/P0/A0.
Raw13.05→13/100; +2.55 raw к591 за реально выполненные native критерии,
не за fix/commit. Все28 timestamp anchors находятся в JSON current_readiness.
Реализация58–80 без изменения. R08 checking objectc3a67/treee3104d9
ещё НЕ опубликован, его Windows compilation/native NOT_RUN,0 новых баллов.
Следующий шаг — один expected-head update4dd→c3a67, сохранить новый run/merge;
пять mandatory SSH contracts не отключаются. F31-R11 остаётся PARTIAL:
R08/R09/R10, полная итоговая performance/owner приёмка не закрыты.

Ниже сохранены прежние checkpoints; их pending/in_progress — исторические
наблюдения, заменённые актуальным разделом и машинными полями выше.

## R08 checking object подготовлен — ref update PENDING completion592

Local `e62d21c9d84b110fd7f588c27c912cf48d861413`, GitHub commit object
`c3a67ebff9a6ac10e93059bc73dc2fadd8fef29a`, одинаковый tree
`e3104d9fead1a4fff51d598b6099690cb72bcf4e`. Scan17 tree paths (включая old/new
имя одного Git rename),929 added lines:0 secrets/PII; public318/0, diff/JSON,
C#syntax10files0errors PASS. Rename не отключает5 mandatory native SSH tests;
проверены новым именем локально5 PASS4.237с. Workflows/VERSION без изменений,
public uploads/release выключены прежними guards. Windows R08 NOT_RUN.

Branch пока остаётся4dd4987c:592 repository build SUCCESS, relay ещё в работе.
Не запускать второй CI/не обновлять head до completion592. Если прервано,
проверить head PR88 и run39698918707 перед любым ref update; затем один
expected-head update от полного4dd4987c без force, новый exact-SHA native run.
R08 object включает logger/open/folders/installed VERSION и discovery boundary;
PS updater cold-load/log/folder GUI I/O остаётся вне исправленного scope.
Evidence11(CI591),0 баллов за подготовленный object. Следующий шаг — exact592
logs и публикация только после установленного результата, не blind retry.

## Test-suite boundary fix — explicit SSH fixture discovery preserved

Пять SSH server contract tests переименованы в ssh_loopback_fixture_tests.py,
Test-ProGo.ps1 явно запускает новый pattern в том же подготовленном pinned
PYTHONPATH и с прежним90с budget. Число/содержание тестов не уменьшено; actual
native Linux new-name suite5 PASS за4.237с. Git rename сохраняет историю.

Причина: Test-HomeVpnRelay.ps1 позднее делает discover test_*.py уже после
restore environment и setup другого Python. Старое имя случайно импортировало
SSH test без его dependency environment. Clean local Python discovery old-name
дал1 import error при отсутствии глобального Paramiko (без запуска сценариев).
Новый filename не входит в relay pattern; mandatory prepared launch остаётся
в Test-ProGo. Это отдельный исправленный harness-boundary source defect, не
отключение проверки и не утверждение о результате ещё работающего CI592.

CI592 сейчас repository build SUCCESS, relay step in_progress; старое имя
входит в его source4dd4987c. До exact log не объявлять end-to-end SUCCESS.
Windows new discovery wiring NOT_RUN. Следующий checking tree включает этот
отдельный rename commit и R08 блок; source/secret/workflow scan перед push,
после completion592, без дубликата. Evidence11(CI591), прирост0 за rename.

## R08 checking scope — source syntax/dependencies reviewed, Windows pending

Текущий code checkpoint `f7ec7f2cc95bb723ee111f1ba58e2d5ce31fc0cd` содержит7
логических R08 этапов отдельными commits. Diff к isolated47f03e3:14 files,
827 additions/158 deletions (до этого checkpoint текста), без workflows/VERSION.
Tree-sitter0.25.2/C#grammar0.23.1: все10 изменённых C# files parse PASS,0 ERROR/
missing nodes. Это только syntax, не C# typecheck/compilation/native. Инструмент
установлен только в /tmp, не стал product/CI dependency. Public318/0, JSON и
diff PASS; shared BoundedLog.cs сохраняет Core/bootstrap/installed helper source
связи, новый LogOpenOperation включается существующим src wildcard desktop build.

Минимальный поддержанный Windows-прогон — scripts/Test-ProGo.ps1 на новом
точном source/merge (narrow Desktop selector отсутствует):
- BoundedLog existing rotation/privacy + new capacity/single-sink/pause/resume/
  refusal/exception lifetime vectors; Core bootstrap и installed Add-Type compile.
- Actual SafeLog held sink/heartbeat/redaction,3 epoch assertions,2 Context logger
  shutdown/Dispose scenarios;4 open worker cases,6 actual log/folder route cases.
-6 actual modal Cancel→Retry cases (4 прежних +2 installed VERSION),3 native
  chooser cases,3 Diagnostic Context ownership cases; fresh installed helper
  privacy/clipboard/header tests. Old loaded-API transition отдельно NOT_RUN.
- Existing full Desktop/instance/maintenance/shutdown/portable and real SSH
  direct/agent/recovery regression; runner package/hash только после success.

CI592 source4dd4987c НЕ содержит этот R08 code. Не отправлять новый head до
completion592; затем one checking tree с secrets/PII/workflow scan и один CI.
Новые tests ещё NOT_RUN, score11(CI591), прирост0 за parser/review/commits.
Остаток R08: installed updater cold dot-source/Add-Type и его PS Open-UpdateLog/
Open-ProGoFolder/Write-UpdateLog GUI I/O. Source confirmed; actual slow-I/O
Windows reproduction/acceptance NOT_RUN, не объявлять эти пути исправленными.

## F31-R08 этап7 — installed VERSION читается в preview worker, local NOT_RUN

Diagnostics-ProGo.ps1 больше не выполняет Test-Path/OpenText/Read VERSION в
Click. Новый ShowInstalled(root) сначала показывает пустой shared modal UI,
затем tracked Build worker читает максимум32 chars, проверяет отмену до/после
native read и передаёт snapshot существующему allowlisted builder. Missing/
denied/invalid version становится unknown; raw VERSION текст не экспортируется.
Cancel не объявляет незавершённый I/O завершённым, Retry вновь читает version.

2 дополнительных настоящих ShowDialog Cancel→Retry cases (click/Escape) держат
version-read boundary, сохраняют responsive loop/одного owner/cleanup; после
отмены меняют VERSION0.2.2→0.2.3, принимается только fresh header и fresh log.
Установленный fresh PowerShell helper test также требует header0.2.2 и прежнюю
privacy/copy гарантию. Все прежние deadlines3с/8с/cleanup5с сохранены.
Source/diff/public318/0 PASS; C#/PS/native Windows NOT_RUN, не входят в592.

Совместимость при update: уже загруженный old API может не иметь ShowInstalled,
хотя helper script уже заменён. Metadata feature detection вызывает старый
Show(root,'') с честной unknown версией без нового UI file read; actual old-API
transition native NOT_RUN, не объявляется принятой интеграцией.

Уточнение прежнего source finding: normal Initialize-UpdateWindow заранее
компилирует diagnostic types до error-dialog. Cold Add-Type на Click остаётся
возможен в early-error/fresh helper path, а dot-source/PS helper log/folder I/O
тоже отдельный остаток. Этот шаг устраняет только unconditional VERSION read,
не закрывает весь R08. Native root log/queue/folder/version блок ещё ждёт своего
checking SHA после592. Evidence11(CI591), прирост0 за source.

## F31-R08 этап6 — folder/backups actions на owned worker, local NOT_RUN

OpenProGoFolder/OpenBackups используют тот же tracked open workflow. Фиксированный
requested path вычисляется без I/O; Ensure/CreateDirectory и native Explorer
start выполняет worker. directoryOnly mode не проверяет/не создаёт log file.
Все4 viewer commands имеют общий pending owner и защищены от duplicate launch;
settings/routes не блокируются этим локальным просмотром. Error остаётся на
UI, пути корректно quoted, принятый пользовательский Explorer не завершается
при закрытии ProGo. Shutdown/Dispose с прежним budget сохраняют ownership и
отменяют admission до позднего launch; fixed error теперь относится к файлу/
папке, не ошибочно только к журналу.

4 дополнительных actual command-route cases (2 folders × shutdown/ForceDispose)
используют настоящий собственный native child вместо системного Explorer.
Проверяют подготовку выбранной папки, heartbeat, duplicate/cancel/retry, один
cleanup и unchanged installed bytes. finally всегда release до cleanup,
свои native child handles и read-only fixture attributes очищаются также при
ошибке/timeout предыдущего ожидания. Source/diff/public318/0 PASS; Windows
compilation/native NOT_RUN, не входят в592. Согласованные функции не расширены.

R08 source блок теперь охватывает logger +3 viewer paths, но ещё требует exact
Windows и installed updater cold diagnostics UI path. Последний не закрыт;
slow target disk/owner measurements также NOT_RUN. Evidence11(CI591), прирост0.
Далее — source acceptance checklist/secret scan R08 checking tree и текущий
CI592; после его completion отдельная проверка всех этих новых Native cases.

## CI592 — delimiter fix опубликован, один run в работе

Head PR88 `4dd4987cf6f453847931820a6f9ba1298f99f802`, merge
`4cf49375895d64e79a951bfc926a01502f9a7352`, tree
`71a53965acd9d4eb19b6fbe6a9a691693d6b56b6` = isolated local47f03e3bc8f054305d810095a3bfc6128c494cb1.
Единственный [CI592/39698918707](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39698918707)
in_progress. Source scan5files/347lines0 secrets/PII, public317/0/diff/tree PASS,
expected-head update без force. Новый Windows compilation/native NOT_RUN до
logs; прежний591 observed FAIL не превращается в592 PASS. Source-import root
`964bf83fb22d40f931d38de972475607a52baa19`; R08 log/queue code до5d30993 остаётся
отдельным неотправленным блоком. Следующий шаг: same run592 exact logs;
независимо довести folder/backups viewer через тот же owned worker, затем
один Windows checking tree R08 после592. Evidence11/raw10.50 остаётся у591;
0 баллов за source/push. Releases/binary uploads/main merge/целевой ПК/VPS/
system settings не выполнялись.

## Delimiter fix object создан, ref update PENDING

Local fix `47f03e3bc8f054305d810095a3bfc6128c494cb1`, GitHub object
`4dd4987cf6f453847931820a6f9ba1298f99f802`, одинаковый tree
`71a53965acd9d4eb19b6fbe6a9a691693d6b56b6`. Scan5files/347addedlines:
secret/PII0, public317/0, diff/tree equality PASS; workflows/VERSION/launcher
не менялись. Следующий шаг — expected-head693de8ab →4dd4987c без force и один
автоматический новый CI. При обрыве сначала проверить PR88/runs, не повторять
внешнюю операцию вслепую. Root импортировал только два code files+docs, R08
работа сохранена отдельными commits и в fixed checking tree отсутствует.
Windows этого fix NOT_RUN; evidence11 относится к591, прирост0 за commit.

## CI591 delimiter-fix candidate — source only, separate from R08

ProxyService constructor теперь отделяет непустой extraOptions от следующего
аргумента пробелом; caller не обязан добавлять trailing space. Пустой production
prefix остаётся пустым. Существующий native combined fixture намеренно передаёт
-F quoted(config) без trailing space и остаётся regression, проверки не ослаблены.
Failure branch печатает только completed/status/preparation count/owned/typed
local reason и уже classified safe SSH guidance, без raw stderr/argv/key paths.
Это устраняет недостаток доказательств старого log, не скрывает сам failure.

Native Linux ssh-G separator reproducer PASS: до255/config-N, после0/expected
host, без TCP/SSH соединения. Windows compilation/combined-native этого нового
source NOT_RUN. Diff/public317/0 PASS. Код isolated worktree содержит только
ProxyService и соответствующий fixture; R08 logger/open source по-прежнему
исключён. Следующий шаг — scan exact commit tree, expected-head693de8ab → новый
source, один CI. Реализация58–80, verified11(CI591), прирост0 за сам source fix.

## CI591 — launcher PASS, combined start FAIL; exact evidence11

[CI591/39696687583](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39696687583)
completed FAILURE: Windows119555663751 FAILURE, Ubuntu119555663596 SUCCESS.
Source `693de8abda98f48108cea97624cbf9cd165b376d`, merge
`774c77a6f94193ea91281c6adf72a08481db82db`, exact identity0.2.2+этого merge.
Launcher exit0/exit17/owned-timeout regressions PASS; real dependency install
прошёл,5 native OpenSSH server contract tests PASS за3.947с на Windows.
Recovery51/456/guidance10 PASS; report32/helper2/modal4/chooser3/Context3,
settings93, signing55 и aggregate prior43 vectors reached PASS. Новый full
aggregate summary и Desktop summary отсутствуют: остановка на первом direct
combined startup assertion. Agent combined/recovery/metrics и последующий
Instance/Shutdown/HomeVPN/ZIP NOT_RUN, не PASS; uploads SKIPPED. Собственные
SSH/server cleanup markers PASS, timeout не скрыт.

Подтверждённый code defect: ProxyService constructor присваивает raw extraOptions,
StartSingleTunnel склеивает его напрямую с -N. Combined fixture передаёт
-F quoted(config) без trailing space, argv получает config-N. Native Linux
isolated ssh-G reproducer: old prefix exit255/config path changed, separated
prefix exit0/expected host. Windows exact underlying StartupError прежний log
не печатал; причинная связь этого FAIL с delimiter — обоснованный вывод из
кода/reproducer, а не прочитанный stderr. Добавляется безопасная typed причина
и state/count/ownership только при failure; сырые stderr/argv/key paths не выводим.

Отдельный worktree ProGo-ci591-fix от localdd9b17a: нормализует boundary prefix
(пустой production default не меняется) и использует существующий real native
combined тест как regression. R08 source commits до5d30993 сохранены отдельно,
не включать их в эту узкую публикацию. New fix Windows compilation/native NOT_RUN.
Следующий шаг — scan new exact tree, expected-head693de8ab, один новый CI.

V1.0/W100/D-P-A30/20/50 прежние. Exact log591 подтверждает partial D0.5 у
19 родителей F01/F02/F04/F05/F06/F07/F08/F09/F10/F11/F12/F14/F15/F16/F17/
F23/F25/F26/F31 (weight70). К прежним590 добавились native CLI migration,
actual typed proxy rollback, bridge, tray settings, restore shutdown heartbeat,
settings persistence, layout и diagnostic keyboard Tab/Shift+Tab. Все anchors
проверены в raw591 log; F17 диагностическая keyboard доступность00:07:35.526.
P0/A0 (нет ZIP/приёмки). Raw70×0.30×0.5=10.50→11, изменение от590+4.35 raw,
потому что реально выполнен дополнительный Desktop scope до FAIL, не за commit
или полный CI SUCCESS. Реализация58–80 неизменна; F31-R11 всё ещё PARTIAL.

## F31-R08 этап5 — logger owner seal и shutdown/exit lifetime, local NOT_RUN

Context получает epoch logger owner. Begin/resume/suspend управляются коротким
metadata gate без I/O; старый Context не может приостановить или возобновить
нового owner. Shutdown немедленно закрывает admission logger и ждёт его
реальный drain совместно с log-open/diagnostic в прежнем settings budget,
до proxy cleanup/ACK. При отказе/отмене обслуживания admission возвращается
только ещё живому текущему owner; early false/exception тоже покрыт wrapper.
ForceDispose сохраняет LogWriterWork без UI wait. Нельзя вернуть success для
всё ещё held sink. Поздние subsystem log attempts во время seal best-effort
отклоняются (счётчик), не создают новые внешние записи после handoff.

Main после UI loop и terminal CLR handler имеют bounded500мс best-effort
финальный flush (timeout печатает NOT_RUN, не выдаёт durability PASS). Это не
замена строгому pre-handoff drain в Context; recoverable UI ThreadException
не ждёт disk. Необратимый process/OS kill по-прежнему может терять best-effort
logs; профили/секреты никогда не передаются в queue и ею не обновляются.

Native fixtures: obsolete owner resume/suspend, actual held global logger +
UI heartbeat, shutdown refusal до cleanup, explicit retry, ForceDispose без
UI pump и unchanged settings/vault/VERSION. Все ожидания/cleanup ограничены;
global test hooks восстанавливаются в finally даже при timeout/ошибке.
Source/diff/public318/0 PASS; C# compilation/native NOT_RUN в Linux. SafeLog
writer source этап4 `da9ad49901ba8eb27f4ac9c39a94023f131a2b1c`.
Не входит в CI591 source693de8ab; evidence6(CI590), прирост0.

Следующий шаг — exact Windows checking SHA всего связанного R08 блока после
completion591; отдельно остаются folder/backups/cold helper preparation I/O,
slow-disk/owner hardware измерения. F31-R08 ещё не PASS. Сначала проверить
native logger/helper/bootstrap regression, не предполагать её по source review.

## F31-R08 этап4 — SafeLog пишет через bounded worker, local native NOT_RUN

SafeLog больше не вызывает EnsureDirectories/BoundedLog.TryWrite на caller:
только worker sink разрешает AppPaths, создаёт каталоги/вращает/пишет файл.
До queue поступает уже redacted immutable string ≤4096chars; Exception/raw
message не захватываются worker. Oversized raw input не режется посреди
секрета, а целиком заменяется фиксированным omitted marker; после redaction
общий bounded record сохраняет корректную UTF16 границу и truncated marker.
Обычный формат времени/level/redaction сохранён. Queue capacity/dropped/failed
контракт этапа3; I/O failure не создаёт автоматического retry внешней записи.

FlushAsync — асинхронный snapshot lifetime для observability, не обещание
долговечности и не запрет новых producers. Новый actual SafeLog native fixture
держит собственный disk boundary, проверяет UI heartbeat, sanitization до sink,
pending drain и реальные bytes после release. Existing clipboard log check
теперь явно ждёт асинхронный sink (5с), не читает файл в ложный момент.
Source/diff/public318/0 PASS; C# compilation/native NOT_RUN в Linux и не входят
в CI591. Shutdown/process-exit admission/drain ещё TODO следующего отдельного
этапа: не считать этот source окончательной logger lifetime приёмкой.
Реализация58–80, evidence6(CI590), прирост0. Далее — owner-scoped pause/resume
перед shutdown/handoff и bounded post-UI exit flush; old actor не должен
возобновлять admission нового owner. Native проверять новым exact source.

## CI591 — launcher fix опубликован, run в работе

Head PR88 `693de8abda98f48108cea97624cbf9cd165b376d`, merge
`774c77a6f94193ea91281c6adf72a08481db82db`, tree
`789f87c6aaab9748a43be493ec175451b2f43e0e` = isolated localdd9b17afb01a0264e6f6a96d7a3f6f1f5ba81b44.
Единственный [CI591/39696687583](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39696687583)
in_progress. Никакого rerun590 или второго dispatch. Новый fix compilation/PS/
native NOT_RUN до exact logs; исходный failure590 сохранён ниже со своими SHA.
Текущая evidence база6/raw6.15 относится к590, не считается PASS591; прирост0.

Root local импортfixb2b510be37b152a1c514dde0a0847d7a6d927286 содержит также
ранее сохранённые R08 log-open/queue source blocks. Их code НЕ входит в591;
source queuecdadbf429aaac3db10ebb921e88a08e22770031f пока только foundation,
SafeLog всё ещё synchronous. Следующий шаг — отдельное wiring SafeLog с
redaction до queue, bounded data и shutdown/exit drain; параллельно читать тот
же run591. Main merge/public release/binary uploads/owner installation не делались.

## Launcher fix commit object создан — ref update PENDING

Local fix `dd9b17afb01a0264e6f6a96d7a3f6f1f5ba81b44`, GitHub object
`693de8abda98f48108cea97624cbf9cd165b376d`, одинаковый tree
`789f87c6aaab9748a43be493ec175451b2f43e0e`. Scan4files/485addedlines,
0 secrets/PII, public317/0, diff PASS; workflows/VERSION неизменны. Следующий
шаг — expected-head8487594e →693de8ab без force. Если прервано, проверить
фактический head PR88/runs и не повторять update/dispatch вслепую. Native fix
ещё NOT_RUN. Root безопасно импортировал только script+docs из isolated
worktree; R08 source commits сохранены и не входят в этот checking tree.

## CI590 fix candidate — isolated launcher source, native pending

Этот worktree основан на local60f5e92 (tree017b33cb = published8487594e).
В code diff только Test-ProGo.ps1 launcher: прямой System.Diagnostics.Process
создаёт и сохраняет native handle; одновременно дренирует stdout/stderr,
обязательно проверяет ненулевой/пустой ExitCode, сохраняет install60с/contract90с
и cleanup5с. Startup/error/timeout finally закрывает только свой процесс/streams.
Нет Start-Process -PassThru или cast null→0. Согласован с upstream PowerShell
https://github.com/PowerShell/PowerShell/issues/5421 и /issues/20400.

Перед pip выполняются3 native regressions: exit0 принимается; exit17 обязан
вернуть именно ошибку17; timeout200мс обязан завершить только своего sleeping
child и вернуть точную timeout ошибку. Missing code/неверный exception/оставшийся
child → FAIL. Negative vector не превращает timeout реальной установки в PASS.
Ни один прежний timeout не увеличен, проверки не выключались. Windows/PS этих
новых изменений NOT_RUN в Linux; diff/public317/0 PASS, source self review.

R08 log-open/queue commits29b98b34/e4dc5a7f/cdadbf42 сохранены в root branch,
их CODE не включён в этот узкий checking candidate. Документация их учитывает
как local PARTIAL/NOT_RUN. Следующий шаг — scan exact committed fix tree,
expected-head8487594e без force, один новый CI с прямыми launcher regressions.
Реализация58–80; проверенная готовность CI5906/raw6.15, прирост0 за fix source.

## CI590 — exact FAILURE launcher exit-code observation; новая evidence база6

[CI590/39694720495](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39694720495)
completed FAILURE: Win119549198567 FAILURE, Ubuntu119549199364 SUCCESS.
Source `8487594e6d7d4e4eb16df86aae27f2e980329aaa`, tested merge
`20df578c1c2d162f5f373e94aed8107bbfee7d19`, identity0.2.2+этого merge.
Actual log: recovery51/456 PASS, включая10 guidance assertions; report32/helper2,
BoundedLog32, retention29/integrity82/updater134/install145/check72 PASS.
Desktop compilation PASS, но весь Desktop native, Python server contracts,
combined direct/agent/recovery/metrics и subsequent Instance/Shutdown/ZIP
не запущены: NOT_RUN. Package/hash отсутствует; uploads SKIPPED.

Подтверждённый FAIL: Test-ProGo launcher выбросил
`Owned SSH fixture install failed with exit code ` с пустым значением.
Actual pip exit неизвестен. Причина получения ExitCode согласуется с известной
Start-Process -PassThru/redirect handle problem (PowerShell issues5421/20400);
проверки и install60с не выключаются/не увеличиваются. Отдельный fix worktree
ProGo-ci590-fix от local60f5e92 не включает unverified R08 code. Следующий шаг —
прямой System.Diagnostics.Process handle и native regressions exit0/exit17/
owned timeout; это новый source, не слепой retry старого CI.

V1.0/W100/D-P-A30/20/50 неизменны. Новый exact snapshot имеет конкретно
трассированные partial D0.5 для F02/F04/F05/F07/F08/F09/F23/F25/F26/F31,
суммарный вес41; остальным D0 до новой трассировки, не автоматически FAIL.
P0 (ZIP не создан), A0. Raw41×0.30×0.5=6.15 →6/100, изменение от589 raw−15.60.
Основания в590 log: fallback23:42:49.150; foreign-SOCKS23:41:34.418;
slow-start23:41:09.912; retention23:46:11.393; integrity23:46:20.222;
wrong-pin23:38:54.296; rollback-progress23:46:44.643; log32/report32;
quiet-start23:46:07.341; preparation51/45623:45:36.178. Не начисляем баллы
недостигнутому Desktop или compiled-only tests. Не трассированные assertions
сейчас не дают дополнительных баллов; это консервативная earned-evidence база.
CI58922/raw21.75 остаётся историческим полным снимком, не PASS source590.
Реализация58–80 прежняя; это изменение подтверждённости, не удаление функций.

## F31-R08 этап3 — bounded queue foundation, source only

BoundedLogQueue в общем BoundedLog.cs (сохраняет bootstrap/helper dependency
boundary): caller TryEnqueue не ждёт queue lock/I/O;128×4096-char bounded
backlog, single drain без queue gate на sink. Full/oversized/contended/paused
rows отклоняются с Dropped counter; false/throw sink — Failed counter, без
автоматических повторов. SuspendAsync закрывает admission сразу и берёт
worker snapshot после уже принятого producer; lifetime остаётся pending до
реального settlement. Resume явный; старый completion не очищает нового owner.

Native fixtures добавлены в BoundedLogTests: blocked real file sink, bounded
caller/backlog/order, pause vs pendingI/O, resume, refusal/throw и продолжение
следующих rows, cleanup своих barriers/workers в5с. Source/diff/public318/0
PASS; C# compilation/native NOT_RUN в Linux. SafeLog ещё НЕ переведён на эту
очередь; это подготовительный source, не исправление всех UI log I/O и не
основание закрыть F31-R08. Далее SafeLog formatter/sink wiring и native/exit
settlement — после отдельного исправления CI590 launcher.

CI590 Win119549198567 FAILURE: launcher получил пустой ExitCode из
Start-Process -PassThru, actual pip exit неизвестен. Это не установленный
dependency FAIL/timeout и не PASS. Ubuntu119549199364 SUCCESS; native direct/
agent Desktop suite не начат. Точный лог будет перенесён в failure checkpoint.
Нельзя отправлять foundation/LogOpen блоки случайно вместе с узким launcher fix.

## F31-R08 этап2 — реальные log commands и shutdown ownership, local NOT_RUN

OpenAppLog/OpenUpdateLog теперь резервируют LogOpenOperation и полный observer
Task до worker queue; существующие log menu actions отключены до completion.
Directory/legacy path/CreateNew и native viewer start идут в worker; путь для
Notepad корректно quoted, handle после принятого запуска освобождается без
закрытия пользовательского editor. Ошибка показывается на native UI через
существующий persistent dispatcher; canceled/closed owner не публикует late UI.

Shutdown и Dispose сразу Cancel обоих diagnostic/log actors. Shutdown ждёт
их settlement совместно в прежнем settingsShutdown budget (не два timeout),
до proxy cleanup/handoff. Agent startup Close остаётся до первого ожидания.
Незавершённый OS call не объявляется отменённым/успешным; выход отказывается
с фиксированной причиной и разрешает явный повтор. ForceDispose сохраняет
LogOpenWork до фактического settlement; старый callback не очищает новый actor.

2 actual command-route native fixtures добавлены к4 worker cases: native UI
heartbeat, duplicate log actions, shutdown timeout без внешнего cleanup,
Cancel→explicit Retry с одним настоящим own child, forced Dispose без late
viewer/UI, повтор shutdown cleanup ровно один раз, installed bytes unchanged.
finally release/cancel всех своих barriers/tasks, cleanup только test child;
локальные5с/3с budgets сохранены. Source/diff/public318/0 PASS; все новые
C# compilation/native NOT_RUN. Worker stage source29b98b34c993fc14f24c5ab6226f5d63519c10f5.

Не входит в CI590 source8487594e. F31-R08 остаётся FAIL по остальным SafeLog/
folder/backups/cold-helper UI I/O; этот узкий путь лишь PARTIAL до exact native
run. Баллы58–80/22 доказанной базы CI589, прирост0. Далее: source commit этого
блока, затем bounded worker logger (отдельный контракт); CI590 читать отдельно.

## F31-R08 этап1 — owned log-open worker +4 fixtures, source PARTIAL

LogOpenOperation публикует Work до Begin, повторный Begin возвращает ту же Task.
Directory/legacy-path/file операции — worker; CreateNew не обнуляет raced файл,
существующий read-only log не требует записи. Cancel не ждёт I/O/native call:
до admission исключает viewer, после admission lifetime остаётся owned до
возврата launch. Отмена не выдаёт blocked read за завершённый; Context будет
ждать Work перед shutdown/handoff. User-owned viewer после принятого запуска
не закрывается приложением. Source не содержит системных изменений.

4 bounded native fixtures: success, held-I/O cancel, prepare error, existing
read-only file. Проверяют published single-flight Task, native UI heartbeat,
worker thread, actual own child invocation/exit, unchanged source bytes,
nonzero failures и cleanup/restore только своих ресурсов. Native NOT_RUN в
Linux, source/diff/public318/0 PASS. Пока runtime Context wiring TODO; helper
не закрывает F31-R08 и не переносит CI590/589 SUCCESS на новый source.
Следующий этап — wiring OpenAppLog/OpenUpdateLog и ожидание/cancel при shutdown/
Dispose с actual command-route fixtures. Баллы58–80/22 прежнего CI589, прирост0.

## CI590 — exact checking source опубликован, compilation/native pending

Head PR88 `8487594e6d7d4e4eb16df86aae27f2e980329aaa`, merge
`20df578c1c2d162f5f373e94aed8107bbfee7d19`, tree
`017b33cbc8504e6af6ff52a619063219bdd6ed69` = local60f5e92e336d78a5d2f438975773e2c03f61fb16.
[CI590/39694720495](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39694720495)
in_progress; повторного dispatch нет. Scope: guidance/recovery51, isolated native
server5, actual direct+agent preparation→SSH→SOCKS→HTTP/recovery, bounded Python
launcher/metrics. Их Windows compilation/native NOT_RUN до exact log review.
CI589 completed SUCCESS на прежнем7964a99a, score22 только его доказанная база;
новых баллов0. Public uploads/release не включались. Source scan9files/1050lines
0 secrets/PII, public317/0, identical tree, expected-head update без force.

Отдельный local F31-R08 worker +4 tests ещё незавершён, не входит в CI590;
не включать его случайно и не приписывать этому source результаты590. Следующий
шаг — wiring worker к actual OpenAppLog/OpenUpdateLog и shutdown/Dispose, затем
отдельный commit/Windows fixtures; параллельно читать только существующий run590.

## CI589 — exact-SHA SUCCESS, общий lifecycle scoped PASS

[CI589/39692239050](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39692239050)
completed SUCCESS, Win119540954252/Ubuntu119540953856 SUCCESS. Source
`7964a99ac4f6adf0d1d9034c855e989f60211c0a`, tested merge
`f8eb30fddc7f0ecdf0f795de76b8f4be56490705`, build identity0.2.2+этого merge.
Raw Windows log сверён: recovery50/446 включая9 новых lifecycle scenarios,
report32/helper2,4 actual modal Cancel→Retry, normal/missing/error chooser3,
Context ownership3, revision lease8, aggregate43/signing55/UI50, Desktop5103,
Instance28/Maintenance126/Shutdown47, HomeVPN975/Portable90. Native sshd.exe
наличиеTrue — только capability, системная служба не менялась этим probe.

Runner ZIP0.2.2 SHA256
`c66a56fda89845e0e4e68c5fb2d1f87c70e74d01f101113ad8d5e1b21f194805`.
Uploads SKIPPED, downloaded bytes/private delivery/owner install NOT_RUN/BLOCKED.
V1.0 сохраняет D0.5/P0.5 для87 весов, A0, raw21.75→22; прирост0, веса прежние.
F31-R11 PARTIAL: combined real preparation/SSH и typed guidance последующих
local commits ещё NOT_RUN. Их подготовленный source8487594e не включён в589.
Untracked src/LogOpenOperation.cs — отдельный незавершённый F31-R08 worker,
пока без wiring/tests/commit, исключён из fixed checking tree017b33cb.
Следующий шаг — expected-head update8487594e от7964a99a, один новый CI.

## Следующий checking tree подготовлен — ref update пока НЕ выполнен

Local `60f5e92e336d78a5d2f438975773e2c03f61fb16`, GitHub object
`8487594e6d7d4e4eb16df86aae27f2e980329aaa`, одинаковый tree
`017b33cbc8504e6af6ff52a619063219bdd6ed69`. Scan9files/1050addedlines,
secret/PII0, public317/0, diff PASS; VERSION/workflows неизменны, upload/release
не включены. Object создан, но source head пока7964a99a: сначала дождаться
completion существующего CI589/39692239050, прочитать его exact log, затем
один expected-head fast-forward от полного7964a99a без force. Не повторять
push/dispatch после обрыва без проверки фактического head/runs. Windows новых
51 recovery/direct+agent combined/launcher/metrics NOT_RUN, score0 за source.

## Проверка CI589 во время подготовки следующего source

Windows119540954252: шаг Repository hygiene and build SUCCESS; relay integration
ещё in_progress. Ubuntu119540953856 SUCCESS. Полный run39692239050 ещё не
завершён; новый head не отправляется, чтобы не отменить его. Загрузка job log
во время работы вернула BlobNotFound404: это недоступность ещё не опубликованного
лога, не FAIL теста. Точное число50 и identity будут сверены после completion.
Следующий local source `de155d5a786fa1965318b7149fb443ec173eb08c` включает
separate guidance/native51, изолированный server, direct+agent combined
start/recovery, bounded launcher и metrics. Все новые Windows сценарии NOT_RUN.
Git clean, logical commits сохранены; score0 за эти невыполненные проверки.
Далее: финальный committed diff/secret scan, после completion CI589 — один
expected-head source update и новый run для изменённого кода, не retry старого SHA.

## F31-R11 test launcher — bounded native Python status

Test-ProGo запускает install и5 server-contract tests как отдельные собственные
processes с budget60/90с. Stdout/stderr читаются после settlement как log text;
обычный unittest progress/ожидаемый отказ handshake в stderr не становится
NativeCommandError PowerShell. Единственный критерий status — проверенный
exit code; nonzero/timeout → FAIL с причиной, NOT_RUN зависимых native сценариев.
После timeout завершается только созданный Process; killed timeout не PASS.
Публичные workflow/upload guards не менялись. Python fixture без subprocess
команд; pip binary-only без сборщиков. PS execution NOT_RUN в текущей Linux.
Diff/source/public317/0 PASS. Оценка58–80/22 прежней CI588 базы, прирост0.
Следующий шаг — результат CI589 и exact-SHA публикация накопленных логических
test/source commits; измерения ещё не выданы за фактический Windows результат.

## F31-R11 dev measurement — source, показатели ещё NOT_RUN

Combined direct/agent startup и recovery печатают elapsed_ms, все длительности
реальных preparation, CPU time/working set/peak working set только своего SSH
child. Счётчики недоступны → явный NOT_RUN с типом ошибки; отсутствие измерения
не превращается в PASS. Это данные изолированного runner, не замер ПК владельца
или HDD/VHDX, и не process I/O всей программы. Последние критерии открыты.
Pinned Paramiko4.0.0 проверяется при создании стенда: другое содержимое старого
build не выдаётся за испытанную зависимость. Python native5 на этом изменённом
контракте PASS за3.665с; Windows C#/metrics/PS NOT_RUN, баллов0.
Agent-only source этап6: `47a7467f4b6d36b33c8ee8b5ad89337ab990e431`.
Далее: bounded Python test launcher с stdout/stderr как обычным log (ожидаемый
native negative-case stderr не должен ломать PowerShell независимо от exit code),
затем checking publication после текущего CI589.

## F31-R11 этап6 — selected agent key в combined native сценарии, NOT_RUN

Второй combined case использует отдельный owned Windows named-pipe agent с
настоящими LIST/SIGN ответами. Единственный private RSA key остаётся в памяти;
на диск попадают только выбранные public references. Ни shared/default agent,
ни mutating ADD/REMOVE, ни системные credentials не используются. Native
ssh-add проверяет подготовку выбранной identity; затем сам OpenSSH должен
подписать server authentication тем же ключом, а Paramiko проверяет signature.
Лишь HTTP payload после SOCKS подтверждает actual SSH, локальная подпись
отдельно не считается доступом на сервер. Recovery повторяет обе реальные фазы.

Accept/clients single-owned, pipe ready deadline3с; Dispose сначала закрывает
только свои pipe handles и затем ждёт accept/clients с deadlines5с и finally,
включая ошибку одного worker. Snapshot env SSH_AUTH_SOCK восстанавливается;
RSA provider удаляется после settlement. Этот тест моделирует уже загруженный
ключ, не ввод passphrase/ACL на ПК владельца. Последние по-прежнему отдельные
критерии; существующий interactive Add suite не заменяется.

Исходный direct combined source `f485cbb0eddd6c05dc9fc21156e9d26d6c7be87e`.
Текущие compilation/native Windows NOT_RUN; source review/diff/public317/0 PASS.
Баллы58–80/22 без прироста; CI589 ещё проверяет source7964a99a без этих local
изменений. Далее — прочитать результат CI589 и опубликовать новый checking
source direct+agent+recovery, вместе с отдельной guidance/native51.

## F31-R11 этап5 — combined native Windows fixture подключён, NOT_RUN

Local source расширяет SshAuthenticationPreparationWorkflow: реальный disposable
keygen → explicit immutable preparation с тем же isolated SSH config → общий
ProxyService StartSingleTunnel → настоящий OpenSSH с pinned host key → SOCKS5
CONNECT → HTTP payload собственного loopback сервера. После прерывания только
своего SSH PID timer recovery обязан вновь выполнить preparation, создать один
новый PID и повторить payload. IdentityAgent none/direct unencrypted сохраняет
штатный вход. Это общий runtime pipeline, но не GUI/real VPS/зашифрованный agent
сценарий; SettingsService final revision lease отдельно проверен прежними suites.

Test-ProGo готовит Paramiko только в build/ssh-fixture-deps через binary wheels,
с install deadline60с; public uploads/workflow не меняются. На dependency FAIL
native NOT_RUN и CI FAIL; тест не пропускается.5 native Python fixture contracts
затем Desktop combined test; временные PYTHONPATH/fixture env восстанавливаются.
Stop/start/server streams и собственные processes имеют локальные deadlines,
явный server cleanup marker обязателен. Уточнён Python cleanup: закрывает все
свои принятые sockets даже до старта Transport; stalled-handshake test сохраняет
клиент открытым до завершения server cleanup (раньше клиент закрывался первым).

C# compilation/native и PowerShell execution NOT_RUN в Linux. Diff/public317/0
PASS; Python native5 на изменённом fixture PASS за4.343с (Linux OpenSSH).
Исходный stand source65b98ead4c901aeae66d009f6eaa7ba6fe4c835a сохранён в истории;
новый Windows source включает также local guidance83cf660e,51 recovery cases.
Никаких баллов за ещё не выполненный combined Windows тест;58–80/22 остаются
привязаны к прежнему CI588. Следующий шаг — завершить текущий CI589 и затем
опубликовать проверенный checking tree этого этапа на одном новом CI.

## F31-R11 dev-стенд — реальный isolated SSH без системной службы

Добавлен test-only Paramiko4.0.0 ServerInterface стенд: signature проверяет сама
библиотека, разрешён только disposable public key пользователя progo-fixture;
SSH/HTTP слушают127.0.0.1 на выделенных портах. Единственный direct-tcpip target —
собственный HTTP listener. Shell/session/password/прочие назначения запрещены.
Никакие службы/accounts/ACL/сеть/VPS владельца не меняются. Зависимость ставилась
только в /tmp/progo-isolated-ssh-deps, не в системный Python и не в release.

Linux native ssh/ssh-keygen:5 unittest PASS за4.179с — действительная signature
→ SOCKS5 → HTTP payload, другой выбранный ключ, changed host key, запрещённый
destination без dial, закрытие stalled SSH handshake с cleanup собственных
transports/threads/child. У всех ожиданий и cleanup есть локальные deadlines;
CLI ready.json атомарен, lifetime90с с обязательным stop/cleanup. Фактический
host key/key bytes ephemeral и в репозиторий не входят. Это проверка стенда,
ещё НЕ ProGo production pipeline: Windows/ProGo combined integration NOT_RUN.
Источник контракта: https://docs.paramiko.org/en/latest/api/server.html и
https://www.paramiko.org/changelog.html (4.0/Python3.9+; frozen test contract).

Следующий этап — native Windows fixture/общая preparation + owned SSH, pin host
key в isolated config, exact-SHA запуск; затем agent signing/missing-key/Cancel.
Средовой Linux blocker /run/sshd для этого тестового пути устранён без изменения
системы. F31-R11 PARTIAL и баллы58–80/22 не меняются: это не приёмка Windows
и не разрешение входа на реальные ресурсы. CI589 по-прежнему отдельный run.

## F31-R11 этап4 — раздельная локальная подсказка при реальном SSH отказе

Source `83cf660ea51674cafa905c60835bd071477acd5b`: минимальный local этап — ProxyService snapshot безопасного typed display
на worker вне process/revision gates. При отказе собственного SSH StartupError
и permanent recovery pause сохраняют исходную классифицированную причину и
отдельно показывают локальное наблюдение preparation и существующий Check/Retry
маршрут. Наблюдение подписи не выдаётся за серверный доступ; UI polling не
выполняет formatter/I/O. Ошибка formatter не запрещает обычную SSH попытку.
Никакого автоматического Add/service/ACL изменения или нового hard gate нет.

Один новый bounded native scenario (51 recovery cases в следующем кандидате)
проверяет separate messages, отсутствие formatter под gate и повторных вызовов
из UI, реальный отказ child, formatter exception fallback и explicit fresh Retry.
finally останавливает свои операции, cleanup budget5с. Source/diff/public315/0
PASS; C# compilation/native NOT_RUN в Linux. Этот local этап не входит в CI589;
не публиковать его до завершения уже запущенного run39692239050, чтобы не
отменять проверяемую версию. CI589: Windows119540954252 in_progress,
Ubuntu119540953856 SUCCESS. Реализация58–80, доказанная база CI58822; рост0.
Далее: читать exact CI589, затем новый checking source этого отдельного этапа;
combined real-preparer/native SSH integration по-прежнему не подтверждён.

## CI589 — опубликован общий lifecycle, native ещё NOT_RUN

Подтверждены head PR88 `7964a99ac4f6adf0d1d9034c855e989f60211c0a`, merge
`f8eb30fddc7f0ecdf0f795de76b8f4be56490705`, tree
`8c4be1b5bd69d06cbac06f6c4f33c1ff0edc9d85` (local c00aeeea8b41a852604b5475cb95f53cb0707e36).
Единственный автоматический [CI589/39692239050](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39692239050)
queued при проверке. Native50 recovery scenarios/compilation этого SHA NOT_RUN.
Никакого повторного dispatch; читать именно этот ID и exact log identity.
Предыдущий CI588 SUCCESS и22/100 — доказанная база прошлого snapshot, не PASS
нового lifecycle. Новых баллов0. Все binary uploads/release остаются запрещены.
Следующий шаг: пока CI выполняется, узкий source этап разделения preparation и
actual SSH причин в существующем error UI; проверять lifecycle run отдельно.
Нижняя запись ref pending — история завершённой операции, не текущая команда.

## Checking lifecycle: commit object создан, branch update ещё PENDING

Local `c00aeeea8b41a852604b5475cb95f53cb0707e36`, GitHub object
`7964a99ac4f6adf0d1d9034c855e989f60211c0a`, одинаковый tree
`8c4be1b5bd69d06cbac06f6c4f33c1ff0edc9d85`. Scan7files/739added lines,
secret/PII0, public315/0, diff PASS, workflows unchanged. Следующий шаг —
expected-head update от b32ac4ece5cd62b98eab22b7e65aaa079544e5bf без force.
При обрыве сначала проверить head PR88/runs; этот object ещё не является
подтверждённо опубликованным head и не имеет Windows PASS. CI588 отдельно.

## Следующий checking candidate — общий lifecycle F31-R11

Local source lifecycle `c0f5610a2931b0230a9839291ff1843b4bb0c787` и capability
probe `494cbf138647fe60affa0ade3ce7882953647050` завершены отдельными commits.
Следующая публикация включает их,9 новых bounded recovery сценариев (всего50)
и checkpoint документы. Она не меняет workflows и не разрешает binary uploads.
Windows compilation/native этого lifecycle NOT_RUN до нового точного CI;
успех CI588 остаётся только у source b32ac4ec / merge e1039ddd.
Прежние пункты ниже с11/100 и in_progress — датированная история CI587/588.
Актуальная доказанная база22/100 относится к завершённому CI588; новых баллов0.
Следующий шаг: scan committed range, tree equality, expected-head fast-forward,
сохранить новый source/merge/run ID. Не делать повторный dispatch того же SHA.

## Dev capability probe следующего Windows run

Test-ProGo теперь выводит только boolean наличия встроенного sshd.exe на
изолированном Windows runner. Это read-only capability probe для планирования
combined native SSH стенда: никакая служба/server/порт/account/ACL не меняются
и не запускаются. Наличие executable не считается интеграционным PASS.
Сам probe локально Windows NOT_RUN, его результат читать на новом exact-SHA CI.
Новых Python/server dependencies и публичных binary uploads не добавлено.

## CI588 — точный SUCCESS; следующий lifecycle отдельно NOT_RUN

[CI588/39688353510](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39688353510)
completed SUCCESS: Windows119528038063 и Ubuntu119528038420. Source
`b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`, build/tested merge
`e1039ddd046528fc75b9d46897e6924f597768d5`, identity0.2.2+того же полного merge.
Raw Windows log повторно проверен, не только зелёная иконка:
SOCKS41/403; DiagnosticReport32; installed helper2;4 real modal Cancel→Retry;
normal/missing/error native chooser3;3 actual Context ownership scenarios;
settings revision lease8; SSH advisory preparation43; signing55/UI50;
Desktop5103; Instance28; Maintenance126; Shutdown47; Portable90.
REVIEW-01/02 narrowly native PASS этого SHA, весь F31-R08/R11 не закрыт.
Старый CI587 FAIL и его исходные SHA остаются в истории.

Pack release zip SUCCESS на runner, SHA256
`7c6febfc2ebda1834ffa1d04ebc0cc13001bb0629de8f505facb78898d223c5c`.
Все uploads SKIPPED; архив не скачан и не передан; version0.2.2. Это не private
пакет у владельца и не установленное приложение. Binary upload/release всё ещё
не разрешены, delivery BLOCKED до согласованного приватного способа.

Новый local lifecycle source `c0f5610a2931b0230a9839291ff1843b4bb0c787`,
план50 recovery cases и последующие изменения НЕ входят в CI588. Их compilation/
native NOT_RUN. Кодверификация отдельных lease/aggregate компонентов не является
доказательством нового общего invocation/Stop/recovery пути.

Оценка v1.0 обновлена только по этому подтверждённому результату:28 требований
(все кроме F19/F27/F30) имеют прежний partial D0.5; новый exact-SHA runner ZIP
возвращает P0.5, A0. Σ87×(0.30×0.5+0.20×0.5)=21.75 →22. От CI587 raw10.50
изменение+11.25; веса и весь implementation interval58–80 не менялись.
Это подтверждение native/пакета нового проверенного снимка, не рост от commits.
До owner install/real SSH/VPS/переноса и критических blockers product PASS нет.

Дополнительная dev-capability проверка: Linux sshd существует, но isolated
sshd -t с временным host key вернул255 из-за отсутствующей /run/sshd; Python
server libraries отсутствуют. Каталоги/account/services не менялись, listener
не запускался. Combined real-server test в этой среде BLOCKED; нужен отдельный
изолированный test-server путь, не реальные VPS владельца. Другие задачи
продолжаются. Следующий шаг — checking commit общего lifecycle и exact-SHA CI.

## F31-R11 этап3: общий owned preparation/admission — local source PARTIAL

Main Program использует настоящий preparer через общий StartSingleTunnel:
manual/auto/fallback/recovery проходят один preparation owner. Snapshot, endpoint,
epoch и settlement публикуются до выхода из process gate; readonly config/signing
и occupied-port probe идут вне gate. Оба private caller владеют ровно одним
Monitor entry; три released scopes всегда возвращают его в finally. Новый manual
ждёт старую recovery preparation вне gate; timer не создаёт второй owner.

Stop/Dispose сразу меняют intent/epoch и cancel, StopTask ждёт preparation и
startup settlement вне process gate. Final revision lease nonblocking; Busy
повторяет admission в существующем budget без shared gates. Changed/cancelled
scope не допускается; Busy/timeout/unconfirmed advisory auth не становятся
hard gates. Перед spawn повторно проверяются token/epoch; child получает ровно
captured SSH_AUTH_SOCK (null удаляет позднее наследование). Вызов Process.Start
остаётся owned до возврата; после поздней отмены завершается только свой child.
Lease освобождается до logging/other callbacks. Stale finally не очищает новый
slot. Observation getter фильтрует epoch/cancellation/intent; новая recovery
подготовка очищает прежнее наблюдение.

Добавлены9 bounded native lifecycle сценариев в существующий SocksRecoveryTests:
async single-flight Cancel→Retry, synchronous owner Stop, recovery Stop,
profile/endpoint mutation, Busy admission (success/change), advisory failure
without new gate, per-fallback preparation, forced Dispose, final admission
cancellation/lease release. Существующий foreign SOCKS fixture дополнительно
требует zero preparation. Каждый барьер3с, cleanup своих tasks5с с cancel/release;
fixtures создают настоящие child/SOCKS процессы, preparer boundary инъецирован
для гонок. Это НЕ live SSH/agent auth: настоящий aggregate/native signer suite
отдельный, combined real preparation/tunnel и presentation ещё требуются.

Local source commit `c0f5610a2931b0230a9839291ff1843b4bb0c787`.
Файлы ProxyService.cs/SocksRecoveryTests.cs; production Main ранее явно wired
в59cec1ae. Независимые/bootstrap/HomeVPN constructor по умолчанию не делают
лишний I/O; их real agent integration этим этапом не заявляется. Полный F31-R11
остаётся PARTIAL. Self source/dependency/diff review и public315/0 PASS;
compilation/native этих новых изменений NOT_RUN, CI588 их не содержит.
Старый WIP checkpoint9826e742 сохранён как история, текущие исходники не удалялись.
Оценка реализации58–80, доказанная база CI58711 без прироста за невыполненные
тесты. Следующий шаг — exact-SHA Windows50 recovery scenarios и затем combined
real-preparer/native tunnel и typed UI statuses. Реальный VPS/ПК не проверены.

## История ожидания CI588 — завершено SUCCESS, см. актуальный результат выше

Head PR88 `b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`, tree
`c987657710561336d54abd77d123babe592769c7`, local commit
`5a170b4c061aeb2b8fd5f6b7b58ff095b75d6303` с тем же деревом. Ref update уже
подтверждён, PR description обновлено. Точный merge
`e1039ddd046528fc75b9d46897e6924f597768d5`, единственный автоматический
[CI588/39688353510](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39688353510)
in_progress. Не дублировать push/dispatch; читать тот же run. Новый selector,
revision lease/composition compilation/native NOT_RUN до logs этого SHA.
Незавершённый root lifecycle не входит; source review/fixtures продолжаются
отдельно, checkpoint9826e742 сохраняет прежнюю WIP версию. CI587 PARTIAL/FAIL
и11/100 conservative evidence остаются со своим точным source. Оценка реализации
58–80; новых баллов за этот push/commit нет. Нижние ref-pending блоки — история.

## История перед публикацией CI588 — ref update впоследствии подтверждён

Local `5a170b4c061aeb2b8fd5f6b7b58ff095b75d6303`, GitHub commit object `b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`,
полное идентичное дерево `c987657710561336d54abd77d123babe592769c7`. Опубликованным head пока
не объявлять: следующий шаг expected-head update от `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`.
Diff10files/492addedlines secrets/PII0, public315/0, diff checks PASS; workflow
не менялся, public uploads/release не включены. Unfinished lifecycle исключён.
Если прервано, сначала проверить head PR88 и runs именно нового SHA,
не повторять update/CI вслепую. Native нового source NOT_RUN; прежняя
консервативная доказанная база CI58711/100 сохраняется со своим SHA.

## Исторический результат CI587 и последующее исправление native chooser

Source `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, tested merge
`decfa11496c12c5e65987043fb58fc7388cb5d6c`,
[CI587/39685125066](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39685125066)
completed FAILURE: Windows119517114158 FAILURE, Ubuntu119517113715 SUCCESS.
Точный log identity `0.2.2+decfa11496c12c5e65987043fb58fc7388cb5d6c`.
C# compilation PASS; SOCKS41/403, DiagnosticReport32, fresh installed helper2,
Codex67, settings93 и4 actual modal ShowDialog Cancel→Retry scoped PASS.
Полный Desktop не прошёл; Context ownership и SSH aggregate suites после
chooser не достигнуты, как и Instance/Maintenance/Shutdown/HomeVPN/ZIP хвост.
Они NOT_RUN, не FAIL и не PASS. Public uploads SKIPPED, owner PC/VPS NOT_RUN.

Подтверждённый FAIL — normal native chooser child exit1:
`native SaveFileDialog not found within 5000 ms for the fixture owner`, при
этом child сообщил DIAGNOSTIC_CHOOSER_CLEANUP_COMPLETE до внешнего20с лимита.
Совпадение caption в normal selector оказалось лишним условием поверх
PID/thread/root-owner/#32770. Вывод об этом согласуется с тем, что тот же
ownership-only cleanup смог вернуть modal вызов; фактическое значение caption
старый log не сохранил, язык/точный текст не выдумываются.

Точечное исправление в отдельном worktree: normal/error режимы выбирают
native dialog по прежним строгим ownership guards без caption; missing режим
по-прежнему намеренно не находит его и должен вернуть20 + точный FAIL + cleanup.
Ничего не выключено: real production ShowDialog, choices1/observedNative,
нулевые записи, неизменность logs,5с deadline,20с external job budget и
ошибки cleanup сохранены. Добавлены только безопасные observed/choices/cleaned
flags в log, без window texts/личных путей. Новый selector пока Windows NOT_RUN.

Проверочный worktree базируется на completed local commit
`59cec1ae32485d65d5f578329aea768d59a04347`: включает завершённые source этапы
revision lease `9f6c3615405158a9d69ea81089d3a4beb47bb272` и observation/composition,
которые ещё compilation/native NOT_RUN. Незавершённый actual lifecycle diff
основного worktree НЕ входит: ProxyService SHA256
`b04bfe80e9426f75c6bc9094ef3e01ae9384ab2a7c1debc91c362e9f81a8c137`, backup
/tmp/progo-f31-owned-launch-wip.patch. Дополнительно сохранён local checkpoint
`checkpoint/f31-owned-launch-wip-20261022`, полный SHA `9826e742eb987d18cadc63a6aa3f396b91e32447`; stash create/branch
не очищали working tree и не меняли активный HEAD/index. Не откатывать его и не публиковать без
review/lifecycle tests. После исправления CI продолжить этот отдельный этап.

### Оценка v1.0 после смены проверяемого кандидата

Веса/формулы прежние. Историческая CI586 база22 не присваивается CI587.
На CI587 есть конкретные partial native доказательства D=0.5 для19 родителей:
F01,F02,F04,F05,F06,F07,F08,F09,F10,F11,F12,F14,F15,F16,F17,F23,F25,F26,F31.
Их веса по исходной матрице суммарно70. Источники — те же указанные критерии
и найденные в точном CI587 log native строки (report/modal/keyboard источники
F14/F17 уточнены новым реальным diagnostic workflow). Остальным строкам D=0:
их прежние конкретные примеры не достигнуты/не трассированы в новом log;
не подтверждённые критерии не начисляются. P=0 у всех: ZIP/hash на CI587 нет;
A=0 у всех: приёмки владельцем нет. Σ70×0.30×0.5=10.50, показ11 (half-up).
Это консервативный индекс заработанных доказательств, не оценка доли функций.
Неполно разобранным native свидетельствам баллы не приписываются. Прежняя
матрица21.75 и все SHA ниже сохранены как историческая первая база, веса не
изменены. Реализация58–80/100; текущая подтверждённая готовность11/100;
изменение raw −11.25 (display22→11) из-за нового candidate/package/native scope,
а не исчезновения реализованного кода. Новый selector/lease/composition NOT_RUN.

Следующий шаг: secrets/diff scan checking tree, source publish в ту же ветку
expected-head без force и один новый CI; основание нового запуска — source
исправление подтверждённого chooser FAIL, не слепой retry старого SHA.

## Постоянный порядок работы — действует с22.10.2026

Этот порядок задан владельцем и заменяет прежние «одно продолжай — одна
микрозадача», обязательные остановки после этапа и временный ночной режим.
Нижние датированные checkpoint сохраняются как история, их команды остановки
не ограничивают текущую разрешённую последовательность.

1. Самостоятельно продолжать согласованные F01–F31, пока есть разрешённая
   незаблокированная работа. Завершённый этап — промежуточный отчёт, не запрос
   «продолжать?». Отсутствие ответа не означает паузу; явные стоп/пауза/новый
   приоритет владельца имеют преимущество. Объём и концепцию не расширять.
2. Одна микрозадача — один конкретный проверяемый результат. Крупный пункт
   делить заранее. Цикл: минимальная правка → diff → целевые проверки → отдельный
   логический локальный commit → состояние/реестр → следующая задача. Чужой или
   незавершённый diff не включать; не удалять/откатывать работу ради чистого status.
3. После этапа и перед долгими операциями фиксировать выполненное/остаток,
   полные source/local/published/tested SHA, PR/CI IDs, проверки, блокеры и шаг.
   При восстановлении сначала читать документы и сверять Git/процессы/CI;
   не повторять внешнюю операцию до установления результата прежней попытки.
   Не запускать конкурирующего исполнителя на те же файлы.
4. Разрешены публикация исходников и согласованных исправлений в существующую
   `integration/approved-plan-31-20261011`, обновление PR88 и необходимые CI;
   разрешение относится также к последующим согласованным задачам. Перед каждой
   публикацией проверять diff/новые файлы на secrets/PII и автоматические workflow.
   При найденных secrets остановить публикацию, сообщить без раскрытия значений.
5. Не разрешены merge main, публичный release, выгрузка бинарных пакетов/installer,
   установка на ПК владельца, реальные VPS/системная сеть/права/credentials и платные
   сервисы без отдельного применимого разрешения. Если соблюдение требует правки
   workflow, сначала предложить точечную правку на согласование; продолжать
   независимую локальную работу. Не менять workflow ради тайного обхода границ.
6. Один SHA — один необходимый CI, без дубликатов без новых оснований. Сохранять
   run ID и проверять его; во время ожидания делать независимую работу, не
   приписывать старый CI новому SHA. При FAIL установить причину, выделить
   исправление; не выключать тесты, не заменять native сценарии моками и не
   увеличивать timeout ради зелёного. Повторять неуспешный подход только с
   новыми данными. Долгие операции не превращать в бесконечное polling-ожидание.
7. BLOCKED задаётся конкретной причиной и минимальным действием владельца;
   один блокер не останавливает независимые пункты. Общая остановка — только
   завершённый объём, команда владельца, пределы среды или отсутствие любой
   разрешённой незаблокированной работы. Никакого фактически не созданного
   расписания/автоперезапуска/гарантии непрерывного исполнения не заявлять.
8. Сохранять v1.0 и веса100;31/55 не удваивать. Рост только за подтверждённый
   результат точного SHA; отчёт, commit и невыполненный тест не дают баллов.
   NOT_RUN не PASS. Открытые критические блокеры не компенсируются суммой.
   Формат после этапа: «Сделано: … · Проверки: … · Реализация: X/100 ·
   Проверенная готовность: Y/100 · Изменение баллов и основание: … · Далее: …».

### F31-R11 этап2: production composition и bootstrap boundary — source PARTIAL

Этап1 сохранён local commit `9f6c3615405158a9d69ea81089d3a4beb47bb272`.
Program теперь явно передаёт настоящий SshAuthenticationPreparation preparer
в ProxyService. В самом ProxyService только ISshLaunchPreparationObservation
и revision-admission delegate: minimal SocksRecovery/HomeVPN bootstrap assemblies
не получают зависимость от всей диагностики/agent/UI и скрытые реальные ssh-G.
Независимый test constructor по умолчанию остаётся без дополнительного I/O.

Observation проверяет immutable candidate/executable/endpoint и отбрасывает
Cancelled/Changed; не является private-key/server authorization. Два assertions
добавлены в существующую aggregate suite. Сам preparer пока ещё не вызывается:
common owned lifecycle — следующий этап. F31-R11 не закрыт. Source/dependency
и diff checks PASS, public315/0; compilation/native NOT_RUN и не часть CI587.
При первой diff-проверке исправлена случайная двойная CR-нормализация Program.cs;
итоговый diff содержит только4 нужные строки Program, полная перезапись не входит.
Оценка58–80/22, прирост0. CI587 e943ca34 остаётся единственным проверяемым run.

### F31-R11 этап1: nonblocking launch revision lease — source PARTIAL

`SettingsService.TryAcquireLaunchRevision` добавляет Acquired/Busy/Changed без
ожидания занятого commitGate, проверяет именно опубликованную ревизию и удерживает
её против другого writer до Dispose. Удаление lease идемпотентно на owner thread;
неправильный thread не теряет возможность owner освободить lock. Никаких native
проб/await/логирования/Save/диска внутри admission нет. API пока не используется
ProxyService и само по себе F31-R11 не закрывает.

В существующем isolated SettingsPersistenceWorkflow добавлены8 native assertions:
неизменность bytes, wrong-thread dispose, blocked real writer, освобождение,
Busy без ожидания, stale revision, null snapshot, fresh retry. BeforeReplace
barrier ограничен3с, finally освобождает barrier/lease и объединённо ждёт обе
свои tasks до5с до исходного file restore. Новых entrypoints/fixtures/main hooks
не требуется; harness уже компилирует этот файл. Файлы Core.cs и
DesktopSettingsPersistenceTests.cs; local compile/native NOT_RUN в Linux.

PASS self source/diff check, public-content315/0 и targeted secrets patterns0.
Эти изменения не входят в source e943ca34/CI587. Следующий этап — actual owned
launch reservation/preparation вне process gate, final revision lease перед
spawn, общий manual/recovery путь и Stop settlement. Оценка58–80/22 без прироста.

### Опубликовано: CI587, единственный запуск нового кандидата

Source `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, local logical commit
`cd8c468b48f293fe809aea1d1ed75662a8e5a97f`, общее дерево
`b1bbec0ca9876edafc8cd0cf8b16a605bef2ed45`; PR88 head подтверждён после
expected-head fast-forward без force. PR88 OPEN/DRAFT, description обновлено.
Точный PR merge `decfa11496c12c5e65987043fb58fc7388cb5d6c`.
[CI587/39685125066](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39685125066)
запущен автоматически, дубли не создавать. Ubuntu job119517113715 SUCCESS;
Windows job119517114158 in_progress, compilation/native накопленного блока
ещё NOT_RUN до проверки logs. CI586 не даёт этому source успеха. Upload OFF.

Во время ожидания независимая следующая микрозадача F31-R11: узкий nonblocking
SettingsService revision lease и bounded regression под существующим settings
fixture. ProxyService пока не меняется; новый local diff не входит в CI587.
Сохранять этот source/run при следующих локальных изменениях, не подменять SHA.
Реализация58–80/100, проверенная готовность22/100, прирост0 до новых критериев.
Предыдущие блоки ниже — история предыдущих фаз, ref update уже выполнен.

### Подготовленный source: remote ref update pending

Local commit `cd8c468b48f293fe809aea1d1ed75662a8e5a97f`, GitHub commit object
`e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, общее дерево
`b1bbec0ca9876edafc8cd0cf8b16a605bef2ed45`. Metadata commit SHA различается
между local git и GitHub API;16 файлов/полное дерево идентичны. Локальный commit
сохранён, историю не переписывать. Scan2881added lines findings0; public315/0
и diff checks PASS. Native Windows NOT_RUN. Объект ещё не подтверждён как head:
следующее действие — expected-head fast-forward remote integration branch
от656b3d65. При прерывании сначала прочитать PR/head и runs для e943ca34,
а не повторять update/push/CI вслепую. Generated binaries не публиковались.

### Активная точка: проверочный16-file блок и новый Windows CI

Проверка актуальности: local HEAD и PR88 head
`656b3d65e03d540be859ee69635c492631093446`; PR OPEN/DRAFT/unmerged;
CI586/39670153699 completed SUCCESS только на прежнем merge
`7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`. Активного конкурирующего агента,
Git merge/rebase/index lock и процессов ProGo/Git/Windows build не обнаружено.
Текущий diff — ровно16 согласованных файлов ниже; код REVIEW-01/02 не менялся
после их точек. Linux toolchain не позволяет native Windows прогон локально.

Перед публикацией проверены оба workflow: PR synchronize запускает ci.yml,
на public repository автоматические uploads OFF; release.yml публикует только
при подходящем main push или отдельном dispatch, которые сейчас не выполняются.
Workflow правка для этого push не требуется. Ни public binary upload, ни release
не разрешены и не включаются. Следующий шаг: final diff/secrets check16 файлов,
отдельный local commit, tree-verified fast-forward существующей remote branch,
фиксировать новый source/tested SHA и ID CI; после этого F31-R11 этапами.

Windows compilation/native накопленного блока NOT_RUN до нового exact-SHA CI.
Реализация58–80/100, проверенная готовность22/100, изменение0. Эта новая
документация/авторизация не повышает оценку. После source commit его полный SHA
и последующий published SHA/CI записать отдельным checkpoint; самоописательный
commit SHA заранее не выдумывать.

## REVIEW-03 — PASS только синхронизация metadata CI586

22.10.2026 01:03 МСК. Изменены только `docs/ACCEPTANCE_RUNBOOK.md`,
`docs/project_state.json`, `docs/PROJECT_STATE.md`. Код и тесты сохранены;
новый commit/staging/push/PR mutation/CI не выполнялись. Следующие датированные
блоки этого документа — история на соответствующий момент: прежние pending,
in_progress и открытая REVIEW-03 внутри них не являются текущим статусом.

| Область | Текущий подтверждённый результат |
| --- | --- |
| Published source B | `656b3d65e03d540be859ee69635c492631093446`, PR88 OPEN/DRAFT/unmerged по последней проверке |
| Реально проверенный merge M | `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2` |
| CI586 | [run39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699), completed SUCCESS только для B/M |
| Jobs, повторная read-only проверка | Windows119465588823 SUCCESS, Ubuntu119465589235 SUCCESS |
| Build identity | `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`, повторно прочитанный Windows log |
| Runner ZIP | Pack SUCCESS, SHA256 `d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`; uploads SKIPPED, bytes не скачаны/не переданы |
| Локальный L поверх B | Диагностика/report/Context, advisory SSH preparation, ShowDialog Cancel→Retry и bounded SaveFileDialog: **Windows compilation/native NOT_RUN**, нового source SHA нет |
| Интеграция/пакет/целевой ПК | Не приняты; final setup/fallback не готовы, приватная передача BLOCKED, установка и live ПК/VPS NOT_RUN |

SUCCESS CI586 не переносится на локальный L или целое требование. Уточнены
current поля ci_candidate, fresh_candidate_verification, remote_visibility.ci,
86 integration/candidate записей и текущие scoped evidence для уже проверенных
в B/M retention/updater/shortcut/settings/signing. Это внесение существующего
доказательства, не новый прогон и не новый функциональный PASS. Все86 ID,
31 parent/55 child, их status/remaining/dependencies/package/target сохранены.

История: CI583,584,585 и их failures остаются без изменений в ci_history.
CI586 добавлен туда с completed результатом и отдельным prior_observation —
прежним in_progress снимком. tested_baseline_source/merge остаются историческим
CI582 baseline с явной пометкой; текущая последняя проверка находится в
ci_candidate. Прежние CI584 scoped passes и CI585 shortcut failure не стираются.

REVIEW-03 **PASS только metadata consistency**. REVIEW-01/02 source fixes
по-прежнему PARTIAL до native Windows; F31-R11 остаётся PARTIAL, общий
SSH start/recovery aggregate не подключён. Из этой документационной правки
не следует приёмка31/31,55/55 или готовность установленного приложения.

Доступные проверки этого прохода:

- PASS JSON parse,86 уникальных ID=31+55, сохранность requirement statuses,
  критериев/остатка/зависимостей, package/target и трёх прежних CI history entries.
- PASS consistency check трёх текущих checkpoint: одинаковые B/M/run/jobs/ZIP;
  completed/success только у опубликованного CI; локальные compilation/native
  NOT_RUN. Архивный prior_observation и датированные исторические pending
  проверены как история, не ошибочно удалены общим поиском/заменой.
- PASS состав следующего коммита совпадает с16 фактическими modified/untracked
  файлами и между тремя документами; текущий HEAD и пустой index сохранены.
- PASS scope guard:13 остальных файлов, включая весь production/test/script
  diff, побайтно прежние по SHA256-манифесту начала задачи.
- PASS `git diff --check` трёх документов и `python scripts/check_public_content.py`
  (315 source,0 release). Это проверки документации, не Windows tests.

### Следующий проверочный коммит — только план

Предлагаемый commit: `Bound diagnostic preview lifecycle and add advisory SSH preparation`.
Точный состав —16 файлов, без generated binaries/release payload:

```text
src/DiagnosticPreview.cs
src/DiagnosticReport.cs
src/UpdateAwareTrayApplicationContext.cs
src/SshAuthenticationPreparation.cs
tests/DesktopDiagnosticTests.cs
tests/DesktopTests.cs
tests/DiagnosticPreviewTests.ps1
tests/DiagnosticReportTests.cs
tests/DesktopAsyncDiagnosticPreviewTests.cs
tests/DesktopDiagnosticContextTests.cs
tests/SshAuthenticationPreparationTests.cs
scripts/Test-ProGo.ps1
docs/ACCEPTANCE_RUNBOOK.md
docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md
docs/PROJECT_STATE.md
docs/project_state.json
```

Перед будущим commit повторно сверить весь diff и secrets/PII; source SHA
будет известен после commit. Коммит включает диагностический/advisory блок,
а не реализацию launch/recovery F31-R11, repair или installer fallback.

Минимальный **существующий** Windows entrypoint на новом точном SHA:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\scripts\Test-ProGo.ps1
```

На изолированном Windows CI/dev runner скрипт сам собирает приложение и harness.
У DesktopTests нет selector этих suites, поэтому этот готовый entrypoint
выполняет общий Desktop и сопутствующие Windows регрессии; отдельно запускать
полный Actions workflow, Ubuntu, Test-HomeVpnRelay или upload не нужно.
Обязательны реально выполненные DiagnosticReportTests, fresh-process installed
DiagnosticPreviewTests.ps1, старый DiagnosticPreviewWorkflow, новый
AsyncDiagnosticPreviewWorkflow (4 modal Cancel→Retry +3 native chooser cases),
DiagnosticContextWorkflow, SshAuthenticationPreparationWorkflow и старые
selected-key/agent/policy, Desktop/Instance/Maintenance/Shutdown ownership cases.
Подробные критерии сохранены в [runbook](ACCEPTANCE_RUNBOOK.md), структурированный
состав/команда — uncommitted_candidate в JSON. Если guard дал SKIP/NOT_RUN,
это не успешная native проверка даже при exit0. Live auth/установка отдельно.

Этот следующий этап сейчас не выполнялся. При будущем долгом запуске сохранить
точный SHA и ID job/сессии, закончить отчёт и проверять тот же запуск следующей
командой. Не начинать новый параллельный CI. Остановиться после REVIEW-03.

Оценка v1.0/веса не менялись; новых Windows/пакетных/целевых результатов нет.
Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery.

## REVIEW-02 — bounded native SaveFileDialog fixture: source исправлен, Windows NOT_RUN

22.10.2026 00:54 МСК. Выполнена только REVIEW-02. HEAD остаётся
`656b3d65e03d540be859ee69635c492631093446`; текущий diff не коммитился,
не публиковался и не запускался в CI. CI586 относится к B/M из оценки ниже,
не к этим локальным изменениям. REVIEW-03 и production source не менялись.

Изменения относительно начала этой микрозадачи — только3 файла:

- `tests/DesktopAsyncDiagnosticPreviewTests.cs`: прежний
  `AsyncDiagnosticNativeChooserCancel` вызывает отдельный дочерний STA fixture
  с настоящим production SaveFileDialog.ShowDialog. Поиск ограничен5с; при
  отсутствии совпадения выдаётся `native SaveFileDialog not found within 5000 ms
  for the fixture owner`, а не бесконечное ожидание suite/job timeout.
  Если окно найдено, но не закрылось, это отдельная timeout failure.
- Поиск и WM_CLOSE больше не используют глобальный FindWindow по caption:
  EnumThreadWindows ограничен native thread своего preview, затем проверяются
  PID, GA_ROOTOWNER и класс #32770. Очистка при неправильном caption использует
  те же проверки ownership; чужие окна по совпавшему заголовку не закрываются.
- Внешнее ожидание ребёнка —20с через существующий `DiagnosticProcess.Run`:
  процесс создаётся suspended и включается в отдельный owned job до исполнения.
  При успехе, исключении и deadline существующий owner завершает только этот
  job и проверяет settlement; бюджет его принудительной очистки — ещё до2с.
  Новый общий CI timeout, process-name kill или поиск чужих PID не добавлялись.
- Внутренний finally останавливает timer, закрывает только свои native dialogs,
  dispose preview и без UI pump ждёт объединённые Work/BuildWork/SaveWork до5с.
  Dispose/settlement выполняются и при ошибке запроса WM_CLOSE. Cleanup marker
  выдаётся только после settlement и проверки отсутствия оставшихся owned окон.
  Если native modal loop вообще перестал обслуживать сообщения, внешний job
  deadline остаётся независимым от его UI timer; такой случай всегда FAIL.
- Добавлены3 сценария одного fixture: обычный native Cancel; намеренно
  неподходящий caption; намеренная ошибка обработчика после нахождения окна.
  Негативные дети возвращают именно ненулевые20/21 с точной ошибкой и cleanup
  marker; harness проверяет оба условия, реальные native window observation,
  отсутствие записи и сохранность источников. Это проверка ожидаемого отказа,
  не преобразование настоящего timeout в PASS. В основном Cancel-сценарии
  любой timeout — FAIL; внешний20с timeout во всех режимах — FAIL. Ошибка
  cleanup либо неожиданный exit тоже FAIL. Если настоящий диалог вообще не
  появился, негативный сценарий не принимается как успешно проверенный отказ.
- `tests/DesktopTests.cs`: ровно один dispatch для
  `--diagnostic-save-chooser-fixture` до общего suite watchdog. Ребёнок исполняет
  только этот fixture и не запускает весь Desktop suite рекурсивно.
- `docs/PROJECT_STATE.md`: эта точка продолжения. Все прежние fixture methods,
  включая REVIEW-01 ShowDialog Cancel→Retry, сохранены побайтно относительно
  начала задачи; исправление REVIEW-03 не выполнялось.

Результат REVIEW-02 — **PARTIAL: source fix и regression scenarios добавлены;
Windows compilation/native execution NOT_RUN**. В этой Linux-среде отсутствуют
Windows Forms, dotnet/csc/mcs/mono/pwsh/powershell. Три native сценария,
изоляция окон/job и регрессия всего диагностического блока здесь не исполнялись;
ни одно из этих утверждений не помечается Windows PASS. Независимый агент
не запускался, выполнено собственное source/diff review.

Завершённые доступные проверки:

- PASS `python scripts/check_public_content.py`:315 source,0 release files.
- PASS целевые source checks: существующий suite вызывает fixture; child route
  достижим до общего watchdog; вызваны все3 режима; локальный5с и внешний20с
  пределы; thread/PID/root-owner guards; finally и объединённый task settlement.
  Это статические проверки wiring/контракта, не C# compilation/native tests.
- PASS `git diff --check` для3 изменённых в этой задаче файлов; сохранено
  существующее предупреждение LF→CRLF для DesktopTests.cs, whitespace errors нет.
- PASS targeted secrets/PII pattern scan137 добавленных строк тестового кода:
  findings0. Это проверка шаблонов, не обещание исчерпывающего secret detection.
- PASS scope guard:13 остальных modified/untracked файлов совпадают с исходным
  SHA256-манифестом; предыдущая часть DesktopAsyncDiagnosticPreviewTests до
  заменённого native chooser метода побайтно прежняя; в DesktopTests добавлен
  только один dispatch. HEAD и пустой staged index не менялись.

Точные текущие test SHA256 (локальный diff поверх HEAD, не commit SHA):

- `tests/DesktopAsyncDiagnosticPreviewTests.cs`:
  `58cf747c4e897862cb9f35ab835caa9d2675cc9edde742cc0e30b01cc6004acd`.
- `tests/DesktopTests.cs`:
  `314d3f00aff6aa3827a1e559b603ea087eb39a5db4d236f3368b23ea83d41f49`.

Остаток: Windows-проверка этих3 chooser cases и ранее добавленного modal
Cancel→Retry на точном будущем candidate SHA — NOT_RUN. REVIEW-03 остаётся
отдельной следующей задачей только по новой команде. Другие пункты сейчас не
исправлялись. Ничего не публиковать, полный CI не запускать; остановиться.

Оценка v1.0 и веса ниже сохранены: функциональность продукта в этой микрозадаче
не добавлялась, новых Windows/пакетных/целевых доказательств нет, прирост0.
Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery.

## Оценка готовности v1.0 — базовая точка, 22.10.2026 00:38 МСК

**Реализация: 58–80/100. Проверенная готовность: 22/100. Изменение: +0.**
Это два разных индекса, а не доля закрытых строк реестра. Они не заменяют
PASS/FAIL/PARTIAL/BLOCKED/NOT_RUN/TODO и обязательные критерии приёмки.
Все31 основных пункта пока PARTIAL; это не означает автоматически «половина».
В этом проходе код не менялся, тесты/CI не запускались, публикации не было.

### Снимки и границы доказательств

- **B — опубликованный source:** `656b3d65e03d540be859ee69635c492631093446`,
  tree `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`,
  [PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
  Последнее подтверждённое состояние PR — OPEN/DRAFT, в main не интегрирован.
- **M — реально проверенный Windows merge:**
  `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`,
  [CI586](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699),
  Windows job `119465588823`, Ubuntu job `119465589235`.
  В этой микрозадаче read-only повторно прочитаны jobs и Windows log:
  оба jobs completed/success. В логе build identity
  `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2` (20:25:38 UTC).
  Указанные ниже native результаты относятся именно к M в CI586 для B;
  это не утверждение о прогоне любого последующего diff или final installer.
- **L — текущий локальный diff поверх B:**12 modified +4 untracked, без staging.
  Его реализация учитывается только как source evidence в диапазоне I;
  Windows compilation/runtime **NOT_RUN**, приращение проверенной готовности0.
  CI586 не проверяет L. Фиксация L без самого изменяемого PROJECT_STATE:
  SHA256 JSON-манифеста15 путей/хэшей
  `73fc808325dafdc80a5f40f2983c811a411a261017f567b840a0f0e13e2e1ade`.
  Ключевые source SHA256:
  `DiagnosticPreview.cs=d408afd28a707fb52e7f4837c6bcce79a040b040e10fe63bba33db120ed700db`,
  `DiagnosticReport.cs=a48223671f45d74152f877d3aeeddaa673937af70099718a3e17604f94f3e563`,
  `SshAuthenticationPreparation.cs=050b76d82f226edb99a2e5196389c60323981845c59b02e3fc999d0858438f86`.
  Наличие этого компонента не подтверждает подключение к SSH start/recovery.
- В CI586 Pack release zip завершён SUCCESS; лог20:47:53 UTC сообщает ZIP SHA256
  `d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
  Это ZIP на runner, не переданный и не установленный владельцем пакет.
  Upload steps skipped/public artifacts OFF; в последней проверке artifacts=[];
  приватная передача, clean setup/update/rollback и целевой ПК **NOT_RUN**.
- Источники критериев: [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md),
  [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md),
  [project_state.json](project_state.json),
  [RESIDUAL_RECHECK_20261021.md](RESIDUAL_RECHECK_20261021.md),
  source review и REVIEW-01 выше/ниже в этом документе.
  Residual recheck выполнен ранее на `d0f9a6902345eab288b25e8f902df1514db3f2ca`;
  отсутствие последующего исправления указано в текущем реестре, это source
  evidence, не новое native воспроизведение. Полные две исходные ChatGPT-истории
  недоступны; восстановленные требования не выдаются за прочтение всех чатов.
  В момент базовой оценки REVIEW-03 ещё фиксировала устаревшие CI metadata;
  использованы более поздние фактические jobs/logs. Теперь metadata исправлены
  текущим checkpoint REVIEW-03 выше; баллы базовой оценки от этого не повышаются.

### Неизменяемая методика и веса

Каждый из31 основных ID имеет один вес W; сумма W=100. Его вложенные55 ID
ограничивают выполнение родительского критерия, но собственного веса не получают.
Они есть у F16(4), F17(5), F19(5), F22(4), F23(7), F27(4), F30(12), F31(14).
Это декомпозиция остатка, а не исчерпывающая замена всех родительских требований.
Исторические23/31,6/55 и количество assertions/строк/коммитов в формулу не входят.

Большие веса получают сценарии соединения, возврата настроек, сохранности данных,
обновления и готовности SSH. F23=7, F31=8 — их отказ мешает безопасно получить
и использовать результат. F30=8 сохраняет значимость согласованного полного
коммерческого контура даже при большом остатке; он не исключён ради роста оценки.
Навигация, стиль и документация имеют меньший самостоятельный вес.
Пересекающийся код не создаёт дополнительных баллов: F02 оценивает retry,
F04 — истинность статуса, F05 — базовый SSH/SOCKS вход, F31 — дополнительные
agent/key/portability/performance критерии; F07 retention, F08 restore,
F22 перенос/repair, F23 setup/update имеют отдельные пользовательские результаты.

**Реализация I = Σ W × r.** r — экспертная ступень по существующим source и
пользовательскому маршруту:0 — функциональной реализации нет;0.25 — отдельные
работающие компоненты при отсутствии основного согласованного контура;
0.50 — основной контур существует, существенные обязательные части отсутствуют;
0.75 — основной пользовательский маршрут и значительная обработка отказов есть,
полнота остатка ещё не подтверждена;1 — весь согласованный code-критерий доказан.
Это грубые функциональные ступени, не измеренная доля функций с точностью до1%.
Если trace неполон, указывается интервал ступеней: нижняя — обоснованный минимум,
верхняя — допустимая полнота уже существующего кода, которую ещё надо доказать.
Ненаписанные функции верхней границей не оплачиваются; известный отсутствующий
существенный контур ограничивает r сверху. Непроверенный L может дать source
основание для I, но не Windows PASS. Край1 в интервале не присваивает требованию
PASS: он отражает неопределённость полноты, а не подтверждённую полноту.

**Проверенная готовность Y = Σ W × (0.30D +0.20P +0.50A).**
Это консервативный индекс заработанных доказательств для B/M; L не наследует
успех CI586. Значения этапов:

| Этап |0 |0.5 |1 |
| --- | --- | --- | --- |
| D — функциональные dev-проверки | Нет релевантного доказательства/NOT_RUN | Конкретная часть критерия проверена native Windows на точном SHA | Весь dev-критерий с обязательными положительными/отрицательными случаями доказан |
| P — проверочный пакет | Нет доказанного payload соответствующего контура | Сборка с identity, составом runtime и ZIP hash на точном SHA | Полученный пакет проверен по hash/составу, clean dev Windows setup/update/rollback пройдены |
| A — установленный реальный сценарий | NOT_RUN либо нет принятого результата | Основной сценарий на точном установленном кандидате принят, часть обязательных случаев остаётся | Весь согласованный реальный сценарий, отказы/отмена/восстановление приняты |

Общий зелёный CI сам по себе не даёт D=1. ZIP на runner не даёт P=1.
Для F19, F27, F30 пока D=P=A=0: наличие общего EXE не доказывает упаковку
отсутствующего preflight/signing/commercial контура. Для остальных строк
есть приведённая ниже конкретная native проверка M и общий build/runtime/ZIP
из CI586: D=0.5, P=0.5, A=0, то есть Y=W/4. Это частичный зачёт существующего
сценария, а не успешная проверка его известного остатка. Ни по одному ID
реальная установка/приёмка A не начислена. NOT_RUN даёт0 баллов за этап,
сохраняет статус NOT_RUN и не превращается в FAIL.

Веса и этапные доли фиксируются как v1.0. Менять их ради увеличения индекса
нельзя. Изменение согласованного объёма требует явного решения владельца,
новой версии методики и отдельного пересчёта прежней базы.
При смене кандидата доказательства затронутых частей перепроверяются; Y может
снизиться. Старое доказательство хранится со своим SHA, не переносится на L.

### Матрица начислений:31 родителей,100 баллов

Все исходники в столбце «Основание I» относятся к B, кроме явно отмеченного L.
Критерии каждого ID — одноимённая строка APPROVED_PLAN_31 и его дочерние ID
в JSON. Сокращённый native пример в последнем столбце однозначно находится
в Windows log CI586/M; время UTC21.10.2026. Он подтверждает только часть D.
Десятичные начисления служат проверке арифметики, не заявлению точности оценки.

| ID / сценарий | W | r → вклад I | Вклад Y | Основание I и существенный остаток | Native evidence M, частичный D |
| --- | ---: | --- | ---: | --- | --- |
| F01 CLI/Codex |5 |0.75–1 →3.75–5 |1.25 | `CliProxyEnvironmentService`: общий CLI/миграция есть; итоговый пакет и обычный запуск владельца не приняты |20:32:32.173 `CLI migration preserves unrelated preferences` |
| F02 Повторы |4 |0.75–1 →3–4 |1 | `AutomationPlan`, `ProxyService`: retry/fallback/cancel есть; полная итоговая матрица не принята |20:28:53.829 `automatic fallback advances through permanent and transient failures without retrying refused candidates` |
| F03 Один экземпляр |3 |0.50–0.75 →1.5–2.25 |0.75 | `ApplicationInstance`, `MaintenanceOperation`: owner/handoff есть; полный installer ещё не включён |20:40:11.757 `restarted application retains single-instance ownership after handoff` |
| F04 Статус связи |3 |0.75–1 →2.25–3 |0.75 | `ConnectionHealthMonitor`: listener/SOCKS/egress разделены; итоговый реальный маршрут не принят |20:28:03.722 `asynchronous startup refuses a foreign non-SOCKS listener` |
| F05 SSH-вход |5 |0.75–1 →3.75–5 |1.25 | `ProxyService`, `SshInteractiveLogin`: запуск/ожидание SOCKS/первый вход; настоящий VPS NOT_RUN; дополнительная key readiness относится к F31 |20:27:46.940 `asynchronous slow startup shares one process and waits for SOCKS` |
| F06 Возврат proxy |5 |0.75–1 →3.75–5 |1.25 | `CliProxyEnvironmentService`, `WindowsProxyService`: baseline/journal/условный возврат; installer/uninstall и целевой профиль не приняты |20:32:42.684 `failed actual native move conditionally restores its own environment, typed Windows values and exact file bytes` |
| F07 Retention |3 |0.75–1 →2.25–3 |0.75 | `BackupRetention`, `BackupService`: preview/owned deletion/retention есть; единый final installer ещё не принят |20:30:59.042 `Backup retention tests PASS: 29` (suite результата, не29 баллов) |
| F08 Restore |4 |0.75–1 →3–4 |1 | `BackupIntegrity`, `Restore-ProGoBackup.ps1`: staging/rollback/cancel ownership; старый FAIL не объявляется текущим; final package NOT_RUN |20:32:59.168 `pending restore X requests cancellation and retains its native owner until the real hash operation settles` |
| F09 PIN/vault |4 |0.75–1 →3–4 |1 | `VaultService`, `VaultDailyStore`, `VaultOpenService`: утверждённый PIN/ложный vault реализован; итоговая Comfort/midnight/I/O приёмка не полна |20:25:43.380 `wrong-pin read leaves source bytes and file set unchanged` |
| F10 HTTP bridge |4 |0.75–1 →3–4 |1 | `CliProxyBridgeService`, `HttpConnectProxyServer`: consumers/bridge/lifecycle есть; реальная цепочка SSH→SOCKS→HTTP не принята |20:32:32.076 `Windows uses tested HTTP/CONNECT bridge` |
| F11 Навигация |2 |0.75 →1.5 |0.5 | `AppCommand`, tray context: общие команды есть; согласованные новые экраны кабинета отсутствуют, локальная диагностика требует Windows регрессии |20:32:34.807 `real tray command dispatches its expected dialog: Settings` |
| F12 Область Stop |4 |0.50–0.75 →2–3 |1 | tray context/`HomeVpnService`: PC/phone разделены, но phone Stop/exit ownership F31-R10 остаётся FAIL |20:33:03.412 `actual production three-second shutdown refusal keeps the native UI ticking while its restore reader remains owned` — restore owner, не phone Stop |
| F13 Маршрут |2 |0.75–1 →1.5–2 |0.5 | `RouteTester`: старт/отмена/источник результата; итоговый live маршрут NOT_RUN |20:38:56.634 `Escape closes the actual modal route dialog while measurements run` |
| F14 Диагностика |2 |0.75–1 →1.5–2 |0.5 | `SshProfileDiagnostics`, `DiagnosticProcess`: фон/deadlines/cancel есть; aggregate L и modal Cancel→Retry L Windows NOT_RUN |20:38:55.911 `diagnostic workers preserve UI heartbeat after the caller loses its synchronization context` |
| F15 Настройки |3 |0.75–1 →2.25–3 |0.75 | `SettingsService`, `SshProfilesSettingsForm`: atomic save/rollback; установленная итоговая версия не принята |20:32:41.088 settings suite PASS93; native память/диск проверены в CI, не на ПК владельца |
| F16 DPI/layout |2 |0.50–0.75 →1–1.5 |0.5 | `MainWindow`, forms/`Program`: часть адаптивной раскладки; весь набор форм/installer и monitor DPI ещё не завершён |20:34:26.022 `scaled cleanup list and actions do not overlap`; synthetic scale не заменяет реальные DPI100/125/150/200 |
| F17 Доступность |2 |0.50–0.75 →1–1.5 |0.5 | `UiTheme`, native forms: часть keyboard/name/state; новые формы, Narrator/High Contrast NOT_RUN |20:38:56.452 `route measurements complete Tab reaches Button Закрыть` |
| F18 Выдача |2 |0.75–1 →1.5–2 |0.5 | `HomeVpnWizardForm`: issued/installed разделены; установка телефона и интернет NOT_RUN |20:37:27.267 `wizard accessible issuance result never claims installation` |
| F19 Preflight сети |3 |0.25–0.50 →0.75–1.5 |0 | `HomeVpnWizardForm`, `HomeVpnService`: сетевые компоненты/подсказки есть; согласованный NAT/CGNAT/UDP verdict контур не доказан; остаток F19-R01…R05 |Релевантная полная preflight интеграция не доказана; D/P/A0, не общий CI PASS |
| F20 Друзья |2 |0.75 →1.5 |0.5 | `HomeInvitationsForm`, `server/home_vpn_setup.py`: selected revoke/reissue; связь со сроками/тарифами F30 не реализована полностью; live API не принят |20:37:29.887 `native friends Down distinguishes revoked duplicate and names repeat-revoke effect` |
| F21 Clipboard |1 |0.75–1 →0.75–1 |0.25 | `ClipboardService`: owned secret copy/cleanup; итоговые busy/exit сценарии не полностью приняты |20:43:01.811 `secret copy writes current clipboard`;20:43:06.915 logs do not contain secrets |
| F22 Перенос/repair |4 |0.50–0.75 →2–3 |1 | `BackupService`, `HomeVpnPortableTransfer`: защищённый перенос есть; `Repair-ProGo.ps1` F22-R03 source FAIL, другой SID/ПК NOT_RUN |20:46:38.740 `a new real HomeVpnService loads clean imported data without activating SSH or relay`; это не repair PASS |
| F23 Update/setup |7 |0.25–0.50 →1.75–3.5 |1.75 | `UpdateLauncher`, `Update-ProGo.Core.ps1`: updater/rollback есть; standalone setup, installer fallback и release UID/asset контур не завершены |20:31:26.805 `finally requests helper progress clearance even after rollback or host failure: hash-failure`; это не setup acceptance |
| F24 Помощь/версия |1 |0.75 →0.75 |0.25 | `HelpForm`, `README.ru.md`: помощь есть, известная устаревшая CI/acceptance metadata ограничивает верхнюю границу |20:36:09.255 `help Начало820 exposes a focusable read-only instruction pane` |
| F25 Logs/export |2 |0.50–0.75 →1–1.5 |0.5 | `BoundedLog`, `DiagnosticReport`, `DiagnosticPreview`: limits/redaction/preview B, worker build/save + modal fix L; SafeLog/viewer UI I/O остаётся |20:30:26.285 bounded log suite PASS32;20:30:57.110 diagnostic report suite PASS29; L Windows NOT_RUN |
| F26 Автозапуск |1 |0.75–1 →0.75–1 |0.25 | `ApplicationShortcuts`, installer script: отдельные настройки есть; final installer и реальный Windows logon NOT_RUN |20:30:56.448 `startup runs quietly without changing SSH preferences` |
| F27 Подпись/AV |2 |0 →0 |0 | `.github/workflows/release.yml`: signing/verification/timestamp pipeline отсутствует; сертификат/Defender/Kaspersky NOT_RUN |D/P/A0; подпись не выводится из успешной unsigned сборки |
| F28 Команды/отчёт |1 |0.50–0.75 →0.5–0.75 |0.25 | `AppCommand`, `Test-ProGo.ps1`: каталог есть; раздельная отчётность suites/scenarios/assertions не завершена |`DesktopUiWorkflowTests.CommandCatalogContract`: unique round-trippable command IDs;20:35:03.163 diagnostic command opens review without implicit Enter export |
| F29 Стиль/иконка |1 |0.75–1 →0.75–1 |0.25 | `BrandIcon`, `UiTheme`: принятая иконка/стиль; все состояния новых экранов и итоговые screenshots ещё не приняты |20:38:07.907 built icon valid ICO header/all seven resolutions;20:38:07.912 PNG corners16 |
| F30 Кабинет/billing |8 |0.25 →2 |0 | `server/profile_share.py`, friends/reissue и отдельные server limits — компоненты; полный кабинет/roles/orders/payment/mail не реализован, есть внешние зависимости |D/P/A0: QR/issuance тест не считается коммерческой end-to-end проверкой |
| F31 SSH/скорость |8 |0.50–0.75 →4–6 |2 | `SshAgent*`, `SshProfile*`, `ProxyService`: agent status/start, key inspection, IPv6/fallback есть; common fresh key preparation F31-R11 TODO, UI I/O/phone Stop FAIL; aggregate L пока advisory |Signing55/UI50 и service-start62 в M;20:28:22.833 `raw and bracketed IPv6 start a native child and complete SOCKS CONNECT`; encrypted key/VPS/другой ПК NOT_RUN |
| **Итого** |**100** |**58.00–79.25** |**21.75** |**Показать I с округлением наружу58–80, Y с округлением до22** |**Ни одного A>0; Windows L NOT_RUN** |

Разброс I обусловлен неполной трассировкой всех критериев к существующему коду,
незавершёнными специфицированными контурами и непроверенным L. Подмена его точкой
середины создала бы ложную точность. Y=21.75 — сумма явно заработанных частичных
этапов по этой методике, отображаемая как22; это не «22% тестов прошло» и не
разрешение эксплуатации. Для текущего L нет самостоятельного Windows балла:
22 описывает доказательства B/M, а готовность нового изменённого блока NOT_RUN.

### Критические блокеры независимо от суммы

1. **F31-R11:** общий свежий этап подготовки конкретного выбранного ключа перед
   `StartSingleTunnel` и recovery ещё не подключён. Public key match/доступный
   ssh-agent не доказывают разблокировку private key или принятие сервером.
   Реальный encrypted-key/agent и SSH→SOCKS5→HTTP на разрешённом VPS NOT_RUN.
2. **F31-R08/R09/R10:** source FAIL для оставшихся SafeLog/viewer/helper UI I/O,
   private HomeVPN disk/DPAPI/session preparation и phone Stop/exit ownership.
   Worker preview/save L закрывает лишь часть R08; Windows регрессия L NOT_RUN.
   Фактическое зависание/утечка на ПК владельца этим не утверждается.
3. **F22-R03/F23:** repair пропускает12 из41 обязательных runtime paths, допускает
   неполный payload и direct replacement без общей транзакции; final standalone
   installer, подходящий fallback при неприменимом patch и его приёмка не готовы.
   Сохранность профиля/ключей при полном конечном обновлении пока не принята.
4. **Кандидат и приёмка:** L не скомпилирован/не проверен Windows, PR не интегрирован,
   пакет владельцу не передан и не установлен. Startup/UI/profile/key/cancel/
   recovery/update/rollback на целевом ПК NOT_RUN. Согласование приватной передачи
   требуется до использования публичных CI artifacts; сейчас ничего не передаётся.
5. **Полный согласованный объём:** F27 signing/verification отсутствует, реальные
   cert/AV проверки NOT_RUN; F30 коммерческий контур в основном отсутствует,
   провайдерные решения/интеграции BLOCKED. Это блокеры окончательного завершения
   всего объёма, а не новый запрет разрешённого unsigned тестового кандидата.

### Три ближайших шага — только по отдельной команде

1. **REVIEW-02:** ограничить native SaveFileDialog fixture своим deadline и owned
   cleanup. Критерий: отсутствующее/неподходящее окно даёт ограниченный по времени
   FAIL, positive chooser Cancel сохраняется, fixture не оставляет свои окна/tasks.
   Не увеличивать общий CI timeout и не исправлять другие функции в этой задаче.
2. **REVIEW-03:** синхронизировать completed CI586 metadata в JSON/runbook с B/M,
   ссылкой и jobs; L явно NOT_RUN. Критерий: нет двух конкурирующих текущих
   CI-статусов, сохранены86 ID и история. Само исправление метаданных не повышает Y.
3. **Целевая Windows-регрессия отдельного диагностического кандидата:** сначала
   зафиксировать состав и точный candidate SHA, затем отдельным разрешённым
   запуском проверить ShowDialog Cancel→Retry, chooser bounds и прежние
   close/Dispose/single-flight/installed helper сценарии. Критерий: результаты
   именно этого SHA, bounded cleanup и отсутствие stale publication; не общий
   старый CI SUCCESS. Если подготовка/запуск крупнее одной микрозадачи, разделить;
   долгий запуск закончить сообщением ID, результат читать следующей командой.
   Этот шаг даёт доказательство только диагностическому блоку, не всему F31.

### Учёт изменений и завершение этой микрозадачи

Это первая оценка v1.0, сопоставимой предыдущей базы нет: **Изменение +0**.
В дальнейших отчётах Δ означает изменение заработанного Y по той же методике
от предыдущей оценённой точки (расчёт до округления); изменение интервала I
описывается отдельно. Только новое релевантное доказательство на точном SHA
может дать прирост Y. Новый код без проверки, NOT_RUN, новая строка отчёта,
количество тестов и пересчёт весов приростом не считаются.

В этой микрозадаче разрешено изменить только PROJECT_STATE.md. Завершены локальные проверки документа:
PASS арифметика31 уникального веса (сумма100; I58–79.25; Y21.75),
PASS86 ID реестра (31+55), PASS `git diff --check -- docs/PROJECT_STATE.md`,
PASS побайтовая неизменность остальных15 modified/untracked файлов по исходному
SHA256-манифесту. HEAD и пустой staged index не изменились. Это не Windows
функциональные тесты. Read-only PR проверка подтвердила OPEN/DRAFT, merged=false,
head=B и merge=M; старое описание PR о pending CI не подменяет jobs CI586. Ничего не публиковать, CI не запускать,
к следующей задаче не переходить.

Формат конца дальнейшего отчёта:
«Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery».
Числа обновлять только по этой методике и доказательствам; для первого отчёта
использовать указанную базу. Остановиться и ждать следующей команды владельца.

## REVIEW-01: modal Cancel → Retry исправлен в source, Windows NOT_RUN

22.10.2026 00:21 МСК. Выполнена ровно одна разрешённая микрозадача: отмена диагностики
в ShowDialog и её modal regression. HEAD остаётся
`656b3d65e03d540be859ee69635c492631093446`; новые изменения не коммитились,
не публиковались и не проверялись полным CI. CI586 не проверяет этот diff.

Изменены только3 файла относительно начала этой микрозадачи:

- `src/DiagnosticPreview.cs`: после Form.CancelButton assignment явно установлен
  `close.DialogResult = DialogResult.None`. Обычный busy Cancel/Esc вызывает
  RequestCancellation без implicit modal close. Idle Close по-прежнему вызывает
  CancelAndClose → CloseSettled. Title-close, shutdown/Dispose и active-operation
  identity/single-flight guards не менялись.
- `tests/DesktopAsyncDiagnosticPreviewTests.cs`: добавлен
  `AsyncDiagnosticModalCancelRetry`,4 вызова в существующем suite — build/save
  × click/Esc. Каждый случай действительно открывает `form.ShowDialog()`.
  Первый build уже прочитал прежний журнал, но удержан перед owner publication;
  отменённый результат не принимается. После settlement retry в том же окне
  читает изменённый журнал и выдаёт только свежие события. Save-вариант сохраняет
  исходный reviewed snapshot после изменения журнала. Во время обеих фаз duplicate
  requests возвращают тот же Task; старый Task завершён до нового native I/O.
  Результат повторно сверяется через несколько modal message-loop turns, затем
  explicit idle Close действительно закрывает окно.
- `docs/PROJECT_STATE.md`: эта точка продолжения; остальная работа сохранена.

Новый test fixture: собственный8с modal deadline на UI timer, каждый controlled
worker barrier ждёт не более3с. Timer exception освобождает оба barrier и dispose
точно своей формы. В finally timer останавливается, barriers освобождаются, форма
dispose, все first/retry/current/build/save Tasks объединяются и ожидаются без UI
pump с пределом5с. Это ограничение контролируемого fixture, не обещание принудительно
прервать зависший OS filesystem call. Чужие окна/процессы не затрагиваются.

Результат REVIEW-01 — **PARTIAL: source fix + regression added; Windows NOT_RUN**.
В доступной Linux-среде нет dotnet/csc/mcs/mono/pwsh/Windows Forms. Нативные4 новых
сценария, C# compilation и существующие title-close/shutdown/Dispose regressions
не запускались; их PASS не заявляется. Новые функции и SSH launch integration
не добавлялись.

Доступные целевые проверки:

- PASS `git diff --check`; только прежние LF→CRLF notices в двух других файлах.
- PASS `python scripts/check_public_content.py`:315 source,0 release files.
- PASS статическая проверка wiring: четыре варианта нового метода вызываются
  существующим suite; в методе ShowDialog, deadlines и finally cleanup; это
  source check, не выполнение WinForms.
- PASS targeted secrets/PII scan119 добавленных строк production/test, findings0.
- PASS scope guard:13 остальных modified/untracked файлов побайтно совпадают с
  началом микрозадачи. В частности, REVIEW-02 chooser method, REVIEW-03 runbook
  и JSON остались без исправлений. HEAD и пустой staged index не менялись.

Проверенный source SHA256 `DiagnosticPreview.cs`:
`d408afd28a707fb52e7f4837c6bcce79a040b040e10fe63bba33db120ed700db`.
Проверенный test SHA256 `DesktopAsyncDiagnosticPreviewTests.cs`:
`fd4b028149f0fe66e20a6cf04c7085536d1ba8a5ecfe5d3d20a0518b36298304`.

Остаток, сейчас не выполнять:

1. REVIEW-02 — ограничить ожидание native SaveFileDialog cancellation fixture
   собственным deadline/owned cleanup; отдельная предлагаемая следующая микрозадача.
2. REVIEW-03 — синхронизировать completed CI586 metadata в runbook и JSON,
   сохранив этому локальному diff NOT_RUN.
3. Для REVIEW-01 необходима отдельная разрешённая Windows-проверка на точном
   будущем candidate SHA; до неё native acceptance остаётся NOT_RUN.

Публикация, полный CI и переход к следующей задаче в этом проходе не разрешены.
Остановиться и ждать следующей команды владельца.

## История: ревью16 незакоммиченных файлов — завершено с находками

22.10.2026 00:05 МСК. Разрешённая единственная микрозадача: source review12 tracked modified
и4 untracked относительно HEAD `656b3d65e03d540be859ee69635c492631093446`.
HEAD/ветка не менялись. Просмотрены diff12 файлов и полный текст4 новых файлов,
связанные existing helper/API ровно для проверки их совместимости.
В этой микрозадаче изменён только этот PROJECT_STATE.md; source/tests/другие
документы сохранены побайтно. Commit/push/PR mutation/full CI не выполнялись.

**Итог: source review завершено, безусловный CLEAR не выдан.** Подтверждены
две source/control-flow находки и рассинхронизация документов. Исправления
не выполнялись. Windows compile/runtime для локального блока **NOT_RUN**:
в доступной Linux-среде нет dotnet/csc/mcs/mono/pwsh/Windows Forms.
CI586 SUCCESS относится только к published source656b3d65 и tested merge
7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2, не к этому рабочему diff.

### REVIEW-01 — P2: Cancel превращается в закрытие модального окна

- Файл/символы: `src/DiagnosticPreview.cs`, constructor (`CancelButton = close`,
  сейчас108), close Click handler (102), `OnFormClosing` (246),
  `RunOperation` owner finally → `CloseSettled` (193).
- Доказательство: UiTheme создаёт Button с DialogResult.None; setter Form.CancelButton
  в .NET Framework автоматически меняет его на Cancel. Локальный код не сбрасывает
  это значение после назначения. При ShowDialog нажатие кнопки/Esc устанавливает
  результат формы; modal close вызывает OnFormClosing, который при busy задаёт
  closing/closeAfterWork. После settlement окно закрывается. Это вывод по исходникам
  проекта и официальному framework contract; native воспроизведение **NOT_RUN**.
- Влияние: действие «Отменить операцию» лишает пользователя повтора в том же окне,
  хотя новый сценарий и статус предлагают retry. При отмене уже выполненной записи
  подтверждение результата также исчезает вместе с окном. Преждевременное освобождение
  native I/O или потеря исходных журналов этим выводом не утверждаются.
- Воспроизведение: открыть actual tray ExportDiagnostics → ShowDialog, удержать build
  или save на существующем beforeIo barrier, нажать «Отменить операцию»/Esc,
  обработать modal messages, отпустить barrier. По коду окно закроется вместо retry.
- Пробел тестов: `AsyncDiagnosticBuildCancelRetry` и `AsyncDiagnosticSaveCancelRetry`
  вызывают `form.Show()`, поэтому implicit modal DialogResult не проверяется.
  Context modal tests покрывают shutdown/Dispose, а не обычный Cancel+Retry.
- Минимальное исправление: нейтрализовать автоматический button DialogResult после
  назначения CancelButton; явное idle Close, title-close и CancelAndClose сохранить.
- Проверка исправления: настоящий ShowDialog, held build/save, click и Esc; после
  settlement окно остаётся открытым, доступен explicit retry с тем же snapshot;
  title-close/shutdown/forced Dispose по-прежнему закрывают с сохранением ownership.

Framework evidence:
[.NET Framework Form.cs — CancelButton и CheckCloseDialog](https://referencesource.microsoft.com/System.Windows.Forms/winforms/Managed/System/WinForms/Form.cs.html),
[Microsoft Button.DialogResult — ShowDialog contract](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.button.dialogresult?view=netframework-4.8.1).

### REVIEW-02 — P2: native chooser fixture не ограничивает собственное ожидание

- Файл/символ: `tests/DesktopAsyncDiagnosticPreviewTests.cs`,
  `AsyncDiagnosticNativeChooserCancel` (259).
- Доказательство: timer при `FindWindow(...) == IntPtr.Zero` постоянно возвращает;
  deadline/watchdog отсутствует. `PerformClick` синхронно входит в production
  ChooseSavePath → SaveFileDialog.ShowDialog; последующий PumpAsyncRestore ещё
  не выполняется. Ограничение всего CI job не ограничивает этот fixture.
- Влияние: если caption/class matching не сработает, тестовый диалог остаётся открытым
  и общий Windows suite может ждать до job timeout. Это условный source-confirmed
  путь без лимита; реальный сбой поиска окна на Windows **NOT_RUN**, не заявлен.
- Воспроизведение для будущего regression: намеренно не находить native dialog при
  настоящем ShowDialog; ожидание не должно удерживать весь Desktop harness.
- Исправление/проверка: собственный deadline с точным владением test helper/dialog,
  bounded cleanup и явным FAIL при отсутствии окна; сохранить реальную native chooser
  проверку и отдельный негативный сценарий. Не увеличивать общий CI timeout.

### REVIEW-03 — P2: CI586 metadata не согласованы с завершённым прогоном

- Файлы/символы: `docs/ACCEPTANCE_RUNBOOK.md` opening candidate block;
  `docs/project_state.json`: ci_candidate.status/acceptance,
  fresh_candidate_verification, remote_visibility.ci и requirements[].integration.
- Доказательство: runbook пишет «Windows выполняется», JSON содержит in_progress /
  NOT_RUN для CI586; предыдущая точка восстановления уже установила completed SUCCESS
  с точными source/merge/jobs. Structural check этого противоречия не исправляет.
- Влияние/воспроизведение: открыть эти документы рядом с completed-CI586 checkpoint —
  читатель получает два разных текущих статуса. Исторические FAIL583–585 сохраняются.
- Исправление/проверка: отдельная небольшая синхронизация только metadata опубликованного
  tested scope; сохранить локальному diff NOT_RUN,86 ID и package/target gates.
  В текущей микрозадаче эти файлы не изменялись.

### Проверенные границы и доступные результаты

- DiagnosticReport сохраняет источники/allowlist/ограничения; добавленные token checks
  не поглощают OperationCanceledException. Worker Build/save используют reviewed
  snapshot; native chooser/clipboard остаются owner UI. Context удерживает точный
  Work до settlement, включая disposed/reentrant paths. Кроме REVIEW-01 новых
  подтверждённых production дефектов в просмотренном diff не выявлено.
- SSH request/result копируют candidate/config/identity list; exact canonical executable
  и endpoint проверяются, общий budget и прежний signing cap разделены. Нет mutating
  ssh-add, новых hard gates или автоматического выбора первого из нескольких ключей.
  Подпись остаётся local observation; повторный -G не является private-key/server proof.
  Launch/recovery integration явно TODO; его отсутствие не скрыто как готовность.
- Native suites содержат реальные file/WinForms/OpenSSH/test-owned agent integrations
  плюс отдельные deterministic seams. Существующие assertions сохранены; async save
  ожидание добавлено до прежних assertions. Три новых suite зарегистрированы и
  вызываются по одному разу; это static wiring check, не compilation/runtime PASS.
- PASS: `git diff --check` (только LF→CRLF notices в2 файлах).
- PASS: `python scripts/check_public_content.py` —315 source,0 release files.
- PASS: targeted secrets/PII scan1549 добавленных/новых строк во всех16 файлах,
  findings0; новые файлы проверены целиком, включая fixture literals.
- PASS: JSON parse/schema shape —86 уникальных ID=31+55; IDs/statuses совпадают с HEAD,
  обязательные verification/integration/package/target/remaining/dependencies поля есть.
  Семантическое metadata расхождение остаётся REVIEW-03.
- NOT_RUN: C# compilation, DiagnosticReport native harness, новые12 form/3 Context/
  auth aggregate scenarios, installed PowerShell fresh-process suite и реальные Windows
  regressions. Полный Python suite/full CI не запускались как не относящиеся к этой
  ограниченной source-review задаче. Source review не заменяет эти проверки.

Review snapshot15 файлов кроме изменяемого PROJECT_STATE:
SHA256 sorted path→SHA256 JSON manifest
`535c9fc55af868e9003c18095733fae052b46c6d0914cd273ad3749375f6b33c`.
Все16 путей перечислены в историческом checkpoint ниже; новых source файлов
в ходе ревью не создавалось. Итоговая приёмка31/55 не пересчитывалась.

**Предложенная следующая единственная микрозадача, сейчас не выполнять:**
исправить REVIEW-01 — обычный Cancel+Retry настоящего модального diagnostic preview.
Проверяемый результат: нейтрализован implicit DialogResult, добавлен modal regression
на отмену build/save и retry; сохранены title-close/shutdown/Dispose ownership guards.
Никаких других функций, правок SSH launch, публикации или полного CI в этом блоке.
При недоступном Windows native результат остаётся NOT_RUN. REVIEW-02/03 отдельные
остатки; переходить к ним автоматически запрещено. Ждать команды владельца.

## История: восстановление после остановки владельцем — STOP

Проверено 21.10.2026 23:51 МСК. Историческая точка после прерывания;
дальнейшая разработка остановлена по прямой команде владельца. Восстановление
ограничено чтением процессов/Git/GitHub и сохранением этого раздела; новых
коммитов, push, PR mutations, запусков или повторов CI не выполнялось.

### Процессы и Git

- Активных дочерних агентов нет. Локальных ProGo/build/test/git процессов в
  snapshot `ps` нет; обнаружены только службы среды и текущая проверка.
  Процессы не завершались. Проверка не описывает процессы на ПК владельца.
- Ветка `work/progo-ci586-20261021`, HEAD
  `656b3d65e03d540be859ee69635c492631093446`, tree
  `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`.
  Git index пуст: staged0; tracked modified12, untracked4.
  Merge/rebase/cherry-pick/revert/index.lock markers отсутствуют.
  Существующие worktree сохранены. Ничего не удалено, не сброшено и не откатано.
- Последние опубликованные коммиты: `d7e349d63b4d53eba5b7d98277996a89269a5ce8`
  (ShellLink читает snapshot из того же pinned handle через COM stream) и
  `656b3d65e03d540be859ee69635c492631093446` (CI585 failure scope/residual evidence).
  Git содержит14 опубликованных коммитов после исходной точки `7861ea9`;
  первые четыре логических изменения и последующие CI/разработческие исправления
  сохранены, API trees сверялись с локальными. Последний publication scan:
  336 added lines/6 files, secrets/PII findings0.

### Фактический PR и CI

[PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88) OPEN/DRAFT, merged=false.
GitHub head совпадает с локальным `656b3d65e03d540be859ee69635c492631093446`;
base main `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

[CI586 /39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699)
**completed SUCCESS**: Windows job119465588823 SUCCESS, Ubuntu job119465589235
SUCCESS. Checkout и build identity проверены по завершённому Windows log:
tested merge `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`,
`0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.

Узкие результаты этого точного tested merge: Desktop4930 PASS; Codex shortcut
ownership67 PASS; settings initial-load preservation93 PASS; selected-key
signing55 и UI50 PASS; agent startup62 PASS; Maintenance126 PASS; Shutdown47
PASS; Home VPN975 PASS; Portable Home VPN90 PASS. Это не приёмка всего31/55,
не проверка незакоммиченных файлов и не установленная программа владельца.
F31-R11 остаётся PARTIAL: общая auth preparation перед own SSH/recovery TODO.

CI583/39660551553, CI584/39663669399, CI585/39667773240 перепроверены:
все completed FAILURE; в этих известных запусках активных jobs не осталось.
Новые CI не запускались. Их неуспехи сохранены как история, CI586 их не стирает.

Pack release zip в CI586 SUCCESS. Версия0.2.2, ZIP SHA256 по runner log:
`d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
Artifact metadata `[]`; screenshots/diagnostics/release folder/ZIP uploads SKIPPED.
ZIP не скачан и не передан: существование файла на временном runner не означает
доступный проверочный пакет. Публичный release/installer не разрешён;
public uploads OFF, способ приватной передачи ещё не согласован. Установка,
реальный ПК/VPS/маршрут, ACL/службы/системная сеть владельца NOT_RUN.

### Сохранённая незавершённая работа

**Не коммитить и не публиковать автоматически.** Два локальных блока не входят
в CI586. Финальное общее review и Windows compilation/runtime для них NOT_RUN.

- F31-R08: worker Build/save диагностического отчёта, Context single-flight,
  cancellation/shutdown ownership; token overload,12 native form сценариев,
  3 actual Context сценария, адаптация прежних UI/installed helper tests.
  Root-owned Context source review получил CLEAR после двух reentrant Disposed
  race fixes. Полное финальное review всего блока не зафиксировано.
  Logger/open/cold helper compilation остаток этим блоком не закрыт.
- F31-R11 блок1: новый immutable advisory aggregate fresh config/signing,
  общий budget и отдельная native suite. Финальное source review не зафиксировано.
  `ProxyService` aggregate ещё не вызывает; launch reservation/revision lease,
  recovery integration и соответствующий native lifecycle matrix остаются TODO.

Tracked modified12: `docs/ACCEPTANCE_RUNBOOK.md`,
`docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md`, `docs/PROJECT_STATE.md`,
`docs/project_state.json`, `scripts/Test-ProGo.ps1`, `src/DiagnosticPreview.cs`,
`src/DiagnosticReport.cs`, `src/UpdateAwareTrayApplicationContext.cs`,
`tests/DesktopDiagnosticTests.cs`, `tests/DesktopTests.cs`,
`tests/DiagnosticPreviewTests.ps1`, `tests/DiagnosticReportTests.cs`.
Новые untracked4: `src/SshAuthenticationPreparation.cs`,
`tests/DesktopAsyncDiagnosticPreviewTests.cs`, `tests/DesktopDiagnosticContextTests.cs`,
`tests/SshAuthenticationPreparationTests.cs`.

`git diff --check` PASS при восстановлении; только предупреждения нормализации
LF→CRLF в двух ранее изменённых файлах. Новый test run не выполнялся.
`project_state.json` и нижние исторические разделы ещё содержат pending/NOT_RUN
до получения completed CI586; они не являются текущим итогом этого запуска.
Этот раздел имеет приоритет для состояния остановки. Новые55/31 PASS не объявлены.

### Где остановились и следующий единственный шаг

До остановки одновременно ожидался CI586 и продолжалось незавершённое review
локальных диагностического/auth блоков. За время прохода последовательно
исправлялись FAIL предыдущих CI и автоматически начинались следующие блоки.
Последний Windows job длился около22м31с (20:25:25–20:47:56 UTC,
23:25:25–23:47:56 МСК), завершился успешно. Точной разбивки всех заявленных
владельцем1ч29м35с по активной работе и ожиданию нет; единственной доказанной
причины всей длительности установить нельзя. Это не подтверждение зависания CI.

**Только после следующего «продолжай»:** одна микрозадача — синхронизировать
completed CI586 в `docs/project_state.json`. Проверяемый результат: один
согласованный machine-readable checkpoint с точными source/merge/run/jobs,
SUCCESS только для опубликованного tested scope; локальные два блока остаются
NOT_RUN, все86 ID и отдельные integration/package/target слои сохранены.
Никакой реализации, нового CI или публикации в этой микрозадаче.

Новый порядок владельца: одно «продолжай» = одна заранее названная микрозадача
с одним критерием результата; ориентир5–15мин активной работы без обещания срока.
Долгий CI/сборку не ждать бесконечно: записать run ID/status и закончить ответ;
следующая команда проверяет тот же запуск. Новую микрозадачу автоматически
не начинать. Прежние ограничения публикации сохраняются.

## Основные 31 пункта

| ID | Требование | Статус | Реализация / область | Остаток / следующий шаг |
| --- | --- | --- | --- | --- |
| F01 | Общий CLI/Codex | PARTIAL | CliProxyEnvironmentService | Перепроверить на итоговом SHA и пакете: Миграции, один запрос CLI, on/off и итоговые переменные окружения. |
| F02 | Автоматика и повторы | PARTIAL | AutomationPlan; ProxyService | Перепроверить на итоговом SHA и пакете: Временный/постоянный отказ, ручная отмена, успешный повтор без перезапуска. |
| F03 | Один экземпляр | PARTIAL | ApplicationInstance; MaintenanceOperation | Перепроверить на итоговом SHA и пакете: Одновременный запуск, авария owner, handoff, update/restore/install без параллельной записи. |
| F04 | Честный статус связи | PARTIAL | ConnectionHealthMonitor | Перепроверить на итоговом SHA и пакете: Чужой listener, silent SOCKS, egress, NO_PROXY, устаревшие данные и частичный CLI. |
| F05 | Один клик и SSH-вход | PARTIAL | ProxyService; SshInteractiveLogin | Перепроверить на итоговом SHA и пакете: Задержка старта, отказ ключа, один клик CLI, отмена, host trust; настоящий VPS позже. |
| F06 | Возврат proxy-настроек | PARTIAL | CliProxyEnvironmentService; WindowsProxyService | Перепроверить на итоговом SHA и пакете: Типы/отсутствие значений, чужие правки, denied write, повтор очистки, uninstall и installer. |
| F07 | Сохранность копий | PARTIAL | BackupRetention; BackupService | Перепроверить на итоговом SHA и пакете: Preview/Cancel, изменившийся manifest, baseline/pre-update, новая копия и junction. |
| F08 | Транзакционный restore | PARTIAL | BackupIntegrity; Restore-ProGoBackup.ps1 | Поздняя отмена/очистка уже исправлялась. Сохранить результат CI582; новая итоговая регрессия restore/path/reparse/tamper/rollback и приёмка пакета. Старый FAIL без нового воспроизведения не считается текущим. |
| F09 | Настоящий и ложный vault | PARTIAL | VaultService; VaultDailyStore; VaultOpenService | PIN и фоновые vault операции уже реализованы. Нужны регрессия на итоговом SHA/пакете и приёмка установленного кандидата; повторная реализация не требуется. |
| F10 | Жизненный цикл bridge | PARTIAL | CliProxyBridgeService; HttpConnectProxyServer | Перепроверить на итоговом SHA и пакете: CLI/Windows/scoped отдельно, последний consumer, Shutdown(Send), нагрузка, Stop и ошибки cleanup. |
| F11 | Навигация и команды | PARTIAL | AppCommand; UpdateAwareTrayApplicationContext | Перепроверить на итоговом SHA и пакете: Главное окно, трей, настройки, закрытие диалогов, симметричные on/off. |
| F12 | Область отключения | PARTIAL | UpdateAwareTrayApplicationContext; HomeVpnService | Перепроверить на итоговом SHA и пакете: Stop во время Connect, независимость каналов, Quit и подтверждённое завершение. |
| F13 | Проверка маршрута | PARTIAL | RouteTester | Перепроверить на итоговом SHA и пакете: Первый/повторный запуск, закрытие окна, timeout и старый ответ. |
| F14 | Фоновая диагностика | PARTIAL | SshProfileDiagnostics; DiagnosticProcess | Перепроверить на итоговом SHA и пакете: Heartbeat UI, медленная сеть, отмена/timeout, отсутствие поздних изменений. |
| F15 | Save и немедленные действия | PARTIAL | SettingsService; SshProfilesSettingsForm | Перепроверить на итоговом SHA и пакете: Память/диск согласованы; Save/Cancel, ошибка поля, конкурентные изменения. |
| F16 | Адаптивность и DPI | PARTIAL | MainWindow; SshProfilesSettingsForm; Program | Перепроверить на итоговом SHA и пакете: Настоящие Windows DPI 100/125/150/200%, 1366×768, длинный текст, доступные главные действия. |
| F17 | Доступность | PARTIAL | UiTheme; native forms | Перепроверить на итоговом SHA и пакете: Tab/Shift+Tab/Enter/Esc, Narrator и High Contrast в среде разработки; недоступную среду отметить. |
| F18 | Выдача и установка | PARTIAL | HomeVpnWizardForm | Перепроверить на итоговом SHA и пакете: Выдача/отзыв/повтор не дают ложный installed; реальная установка проверяется позже. |
| F19 | Условия подключения | PARTIAL | HomeVpnWizardForm; HomeVpnService | Перепроверить на итоговом SHA и пакете: Стендовые ошибки сети; после установки - DNS, смена сети, idle, большие передачи и телефон. |
| F20 | Управление друзьями | PARTIAL | HomeInvitationsForm; server/home_vpn_setup.py | Перепроверить на итоговом SHA и пакете: Отзыв только выбранного доступа, исходный ID запроса, отмена списка, повтор/ошибка API. |
| F21 | Очистка clipboard | PARTIAL | ClipboardService | Перепроверить на итоговом SHA и пакете: Busy clipboard, таймер, закрытие/выход, чужая замена и пустой secret без ложного «скопировано». |
| F22 | Копии и перенос | PARTIAL | BackupService; HomeVpnPortableTransfer; Repair-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Tamper/path/reparse/cancel, чистая установка и другой Windows-пользователь; обычный rollback сохраняет данные. |
| F23 | Обновление и installer | PARTIAL | UpdateLauncher; Update-ProGo.Core.ps1; Install-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Прогресс/Cancel/offline, скачивание, hash, fixed paths, self-check, rollback; выпуск совпадает с commit/tag. |
| F24 | Помощь и версия | PARTIAL | HelpForm; README.ru.md | Перепроверить на итоговом SHA и пакете: UI/документы/пакеты совпадают; понятные Windows/CLI/Android/iPhone сценарии без личных примеров. |
| F25 | Журналы и экспорт | PARTIAL | BoundedLog; DiagnosticReport; DiagnosticPreview | Перепроверить на итоговом SHA и пакете: Пределы журнала, отчёт до отправки, ошибки чтения, отсутствие секретов и блокировки запуска. |
| F26 | Автозапуск | PARTIAL | ApplicationShortcuts; Install-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Комбинации настроек в dev Windows; фактический вход владельца - после установки. |
| F27 | Подпись и антивирусы | PARTIAL | .github/workflows/release.yml | Перепроверить на итоговом SHA и пакете: Совпадение payload, проверяемая подпись, timestamp и отдельные Defender/Kaspersky результаты. |
| F28 | Команды и поддержка | PARTIAL | AppCommand; Test-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Нет неописанных handlers; отдельно scenarios/assertions, FAIL/SKIP/NOT_CHECKED и общий результат. |
| F29 | Визуальный стиль | PARTIAL | BrandIcon; UiTheme | Перепроверить на итоговом SHA и пакете: Native screenshots общей сборки, 16/24 px на разных фонах, читаемость и геометрия. |
| F30 | Почта, кабинет, биллинг | PARTIAL | server/profile_share.py; HomeInvitationsForm; remaining commercial modules absent | Перепроверить на итоговом SHA и пакете: Sandbox/интеграционные сценарии, проверенный webhook, дубликаты, возврат, paid→activation retry, отзыв активных сеансов. |
| F31 | SSH, переносимость, скорость | PARTIAL | Program; ProxyService; SshAgent*; SshProfile*; UpdateAwareTrayApplicationContext; HomeVpnService | Перепроверить на итоговом SHA и пакете: Dev service/key/ACL/cancel/ownership и slow disk/network fixtures, latency/CPU/I/O; фактический encrypted agent, другой ПК/VPS и Rescue замеры — после установки. |

## Декомпозиция остатка: 55 подпунктов

| ID → основной | Требование | Статус | Историческое свидетельство | Зависимости | Следующий шаг |
| --- | --- | --- | --- | --- | --- |
| F16-R01 → F16 | Полнота адаптивной компоновки существующих вспомогательных окон | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R02 → F16 | Адаптивность новых installer и F30 экранов | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R03 → F16 | DPI awareness и реальная смена монитора | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R04 → F16 | Итоговая native DPI/display приёмка | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R01 → F17 | Полнота клавиатуры и доступных имён/состояний | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R02 → F17 | Читаемые disabled и узнаваемые pending кнопки | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F17-R03 → F17 | PIN hint/status без дублирования | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F17-R04 → F17 | Реальная Narrator приёмка | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R05 → F17 | Реальная High Contrast и видимый фокус | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R01 → F19 | Доказательный LAN/NAT/CGNAT preflight | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R02 → F19 | Подтверждённые входящие UDP-пути | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R03 → F19 | Интегрированная поэтапная диагностика | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R04 → F19 | Настоящий телефон, маршрут и DNS | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R05 → F19 | Устойчивость настоящего подключения | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R01 → F22 | Состав обычных копий на итоговом пакете | PARTIAL | Приёмка готовой реализации | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R02 → F22 | Защищённый VPN export/import на чистом dev Windows под другим SID | PARTIAL | Частично; cross-SID NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R03 → F22 | Полный runtime repair | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R04 → F22 | Фактический перенос владельца между ПК | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R01 → F23 | Готовый standalone setup с полным runtime | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R02 → F23 | Installer в общем maintenance protocol | PARTIAL | Частично | F23-R01 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R03 → F23 | Одна кнопка и автоматический выбор updater/installer | TODO | Предстоит | F23-R01, F23-R02, F23-R04, F23-R05 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R04 → F23 | Подтверждённый честный rollback updater | PARTIAL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R05 → F23 | Неизменяемая идентичность принятого выпуска | PARTIAL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R06 → F23 | Единый итоговый setup/updater и dev приёмка | PARTIAL | Package/Dev PENDING | F23-R01, F23-R02, F23-R03, F23-R04, F23-R05, F31-R11 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R07 → F23 | Установленный update workflow владельца | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R01 → F27 | Signing/verification pipeline поставляемых артефактов | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R02 → F27 | Проверка издателя и доверия updater payload | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R03 → F27 | Реальный сертификат и подписанный итоговый выпуск | BLOCKED | Внешняя зависимость | F27-R01, F27-R02, owner certificate | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R04 → F27 | Антивирусная приёмка конечных артефактов | NOT_RUN | NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R01 → F30 | Почтовая выдача и согласованная QR выдача | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R02 → F30 | Серверные коммерческие данные и права | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R03 → F30 | VPS-кабинет и вход в него из ProGo | TODO | Предстоит | F30-R02 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R04 → F30 | Коммерческий жизненный цикл доступа | TODO | Предстоит | F30-R02 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R05 → F30 | Серверное исполнение ограничений и активные сеансы | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R06 → F30 | Онлайн-заказ и платёжный адаптер | TODO | Предстоит | F30-R02, owner payment provider choice | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R07 → F30 | Проверенные платёжные события и активация | TODO | Предстоит | F30-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R08 → F30 | Технический агентский контур | TODO | Предстоит | F30-R02, owner supplier/provider data | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R09 → F30 | Финансовый реестр и сверка | TODO | Предстоит | F30-R07, F30-R08 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R10 → F30 | Реальная сервисная почта | BLOCKED | Внешняя зависимость | owner mail account/provider | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R11 → F30 | Реальные собственные и агентские платежи | BLOCKED | Внешняя зависимость | F30-R06, F30-R07, F30-R08, F30-R09, owner explicit live authorization | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R12 → F30 | Коммерческий lifecycle на реальном VPS и клиентах | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R01 → F31 | SSH IPv6 | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R02 → F31 | Профильный fallback | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R03 → F31 | Постоянные ошибки SSH config/arguments | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R04 → F31 | Include/aliases/IdentityFile tokens в диагностике | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R05 → F31 | Остаточные startup/settings/shortcut операции вне UI | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R06 → F31 | Фоновый retention preview/cleanup | PARTIAL | Исправлен source-confirmed UI I/O; native NOT_RUN | новый CI, итоговый пакет | Выполнить AsyncCleanupWorkflow и retention regression на точном SHA; visual/target отдельно. |
| F31-R07 → F31 | Фоновое ожидание updater handoff | PARTIAL | Source fix и независимое review; native NOT_RUN | новый CI, пакет | Выполнить owned backend и actual Update route suites; ACK/cancel/timeout/queued lifecycle regression. |
| F31-R08 → F31 | Фоновая работа logs/diagnostic export | FAIL | Source recheck d0f9a690: UI disk I/O | нет внешней блокировки | Tracked Build/save, затем logger/open; native heartbeat и snapshot/cancel. |
| F31-R09 → F31 | HomeVPN private I/O, DPAPI и session preparation в worker | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R10 → F31 | Асинхронные phone Stop/cleanup | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R11 → F31 | Подготовка SSH-agent и итоговая dev приёмка F31 | PARTIAL | Dev PENDING | selected-key readiness, startup ordering, F31-R05, F31-R06, F31-R07, F31-R08, F31-R09, F31-R10 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R12 → F31 | Настоящий encrypted-key agent и маршрут | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R13 → F31 | SSH-профили на другом Windows ПК/пользователе и VPS | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R14 → F31 | Installed build в Rescue/HDD/VHDX | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |

## Очерёдность завершения

1. Исправить подтверждённые startup/QR/rollback дефекты; новый Windows+Ubuntu CI, независимое ревью diff.
2. Завершить готовность конкретного выбранного SSH-ключа и связать её со всеми путями запуска. Наличие `.pub`/отпечатка не считать proof приватного ключа/подписи/доступа. F31-R11 остаётся PARTIAL.
3. Закрыть оставшиеся UI workers (retention/handoff/logs/HomeVPN), transfer/repair, сетевой preflight и итоговые DPI/accessibility критерии.
4. Реализовать согласованный setup/updater fallback, подпись pipeline и коммерческий dev-контур. Provider-specific адаптер требует выбора реальных провайдеров; это не блокирует независимую серверную модель.
5. Сформировать общий приватный проверочный пакет; после отдельной установки владельцем пройти live gates.

## Протокол передачи и аудит

См. [TAKEOVER_AUDIT_20261021.md](TAKEOVER_AUDIT_20261021.md): источники/пробелы, Git/исполнители, находки, воспроизведение и проверки. Точка продолжения и новые SHA/CI фиксируются там.

## История: исходная точка продолжения перед CI583

Code/test source `5c803be5534601233e0041202890cbd7c04cd575`:
QR `bd9c0fb17b4ca4e84bdcec2cf5e1c6d90820f69d`,
startup `e2fccacb8c7a859217cc854cb91bc7ee7ac57bd8`,
rollback `5c803be5534601233e0041202890cbd7c04cd575`.
Общая Python discovery на этом source:44 reported,41 PASS,3 SKIP (C# harness).
Public-content и diff checks PASS; runtime Windows NOT_RUN. Независимые source
review startup/rollback завершены. Исторический непринятый остаток49=37 до
установщика+12 зависимых; новых закрытых подпунктов55 в этом сеансе нет.
Шесть прежних scoped PASS сохраняют свои свидетельства, но не заменяют новую
итоговую регрессию. Инструкция дальнейшей приёмки/возврата:
[ACCEPTANCE_RUNBOOK.md](ACCEPTANCE_RUNBOOK.md).

Следующим шагом на этом историческом этапе было исправление подтверждённого FAIL CI583, публикация
связанных signing/settings изменений после проверки секретов, новый Windows/Ubuntu CI
на точном SHA. Прежняя privacy decision уже получена; способ передачи пакета
ещё не согласован. Полный F31-R11 остаётся открытым.

## Следующий связанный блок: выбранный ключ

Source review пройден для SshKeyReadiness, frozen Unicode child environment и
явного «Проверить ключ» в SSH диагностике. Есть отдельные native backend/UI suites,
строгое расширение прежнего keyboard test; timeout/Cancel/Close/freshness покрыты
подготовленными сценариями. Фактический новый Windows run пока NOT_RUN.
Протокол: [MICROTASK_SSH_SELECTED_KEY_SIGNING.md](MICROTASK_SSH_SELECTED_KEY_SIGNING.md).
Подпись публичной reference не доказывает private bytes либо вход на VPS;
автоматическая подготовка выбранного auth перед SSH остаётся в F31-R11.

## CI583: подтверждённый результат и исправление

Run [39660551553](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39660551553),
source `e9fcfc3279269075eddd6def306c1ca2500cd28c`,
tested merge `d379acd6a0dc5dc55d5d01417cc5c674237d1f27`.
Ubuntu SUCCESS. Windows FAIL: Windows PowerShell5.1 прочитал новый русский
текст в `tests/MaintenanceTests.ps1` как ANSI: UTF-8 файл не имел BOM.
ParserError на строке256 возник до выполнения MaintenanceTests.
До ошибки успешно выполнены SOCKS recovery41/403assertions,
SSH-agent startup62, Desktop4539, Instance28. Maintenance/shutdown/HomeVPN,
package и полный F23-R04 — NOT_RUN на этом source. CI583 остаётся FAIL.

Исправление сохраняет все assertions и таймауты: MaintenanceTests получает
UTF-8 BOM; `scripts/Test-ProGo.ps1` до build/native suites вызывает ParseFile
для каждого `tests/*.ps1` с тем же декодированием, что Windows PowerShell.
Следующий CI проверит и этот fix, и новый выбранный ключ; старые scoped PASS
не подтверждают более поздний код.

## A11: сохранность настроек при ошибке первоначального чтения

`SettingsService.Load/SaveCore` теперь держит sticky read-failure guard:
файл, который не удалось прочитать/разобрать при создании сервиса, нельзя
заменить fallback defaults через Save/TrySave/tray/rollback/async Save.
Новая ошибка имеет фиксированный текст без пути/профиля; обычное сохранение
через настройки показывает инструкцию проверить доступ/формат и перезапустить.
Genuine missing создаёт defaults через exclusive Move: появившийся параллельно
чужой файл сохраняется. Поздний detached Load не публикует и не снимает guard.
После устранения причины новый запуск читает исходные profiles/unknown fields.

Независимое source review PASS; новый native suite зарегистрирован один раз.
Реальные isolated lock→unlock, malformed/null JSON, missing/creation race,
все write APIs и fresh trusted recovery — подготовлены, Windows NOT_RUN.
A11 становится PARTIAL до точного CI; F15/F22/F31-R05 полностью не закрыты.

## История: опубликованный кандидат CI584

PR88, source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`,
merge `2f6b901a6fd537e0e0eeb81c0431b200e29260b2`,
run [39663669399](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39663669399).
Signing `28b7a8a155abff0e0f073adcf07ac4f68f2d02e9`,
encoding `883787b6a392020fd10bb07e00f80bfff00e3bf5`,
settings `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`.
Каждый API tree совпал с локальным; expected-head fast-forward без force.
До публикации17files/1499addedlines: targeted secrets/PII0findings,
public-content305/0 и diff checks PASS. Ubuntu completed SUCCESS;
Windows in_progress, общего PASS нет. Public uploads выключены.

Точный остаток auth preparation и исключения direct/custom/other-auth:
[MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md](MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md).
Реальный общий launch в ProxyService покрывает и StartTunnelCore, и PollRecovery;
одна вставка в Context не закрыла бы F31-R11. Новый запрет SSH по неуспеху
локальной подписи не введён.

## F31-R06: фоновая очистка подтверждённых копий

Tray CleanupBackups теперь запускает одну BackupWorkSession: план в worker,
готовый точный список и явное подтверждение на UI, fresh revalidation/delete
в worker. Повторные cleanup/restore/manual requests разделяют текущий actor;
команды удерживаются до полного workflow и owner completion. Cancel/Close,
shutdown refusal/retry и forced disposal сохраняют ownership; поздняя публикация
на закрытый UI не требуется для завершения worker.

Старые retention APIs сохраняются; token overloads проверяют отмену при чтении,
обходе и перед каждым удалением. Approved list не расширяется; изменённый manifest,
manual/unknown и junction guards сохранены. In-flight Directory.Delete не имеет
принудительной отмены: worker остаётся owned до его возврата. Успешный фактический
результат после последнего delete не превращается в abort при поздней отмене.
При отмене/ошибке UI честно сообщает, что уже удалённые копии не восстанавливаются.

Независимое backend source review и root UI/test review PASS; diff/public scan PASS.
Низкоуровневый pre-cancel/no-delete и10 native actor/preview/revalidation/lifecycle
сценариев подготовлены. Отмена внутри OS delete не исполнена/не заявлена.
Runtime всех новых retention изменений NOT_RUN до следующего CI; F31-R06 PARTIAL.

## CI584 completed FAIL: scoped key/settings PASS, fixture encoding

Source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`,
merge `2f6b901a6fd537e0e0eeb81c0431b200e29260b2`, run39663669399.
Ubuntu SUCCESS, Windows FAILURE. Подтверждены backend signing55, signing UI50,
settings initial-load preservation93, Desktop4739, agent startup62, Instance28.
BOM/ранний parser fix CI583 прошёл; Maintenance дошёл до реального rollback.
Первый новый rollback-copy-fail прошёл критерии failure и отсутствие false-success
markers, затем упал assertion русского текста диалога. Driver записывал текст
через Set-Content без encoding. PS5.1 default ANSI делает capture зависимым
от active code page; потеря кириллицы — вывод по source/failure. Точная ACP и bytes
CI584 не сохранены, поэтому это не отдельное наблюдение байтов старого run.
Это отдельный дефект транспорта тестового evidence, не признание rollback PASS.
Исправлены writer/read encoding на UTF8, исходные semantic assertions сохранены.
Остальные rollback vectors/shutdown/HomeVPN/package NOT_RUN в этом CI.

Source signing/settings criteria имеют scoped PASS на6b3122f0; visual review новых
PNG и целевой ПК NOT_RUN. Новые retention/updater изменения требуют отдельного CI.

## F31-R07: worker handoff и граница подтверждения помощника

Actual Update route теперь StartUpdate → StartUpdaterAsync → UpdaterCore →
LaunchUpdater в tracked BackupWorkSession. Installed-path I/O и ожидание ACK идут
в worker, UI получает progress/Cancel и fixed nonsecret ошибки. Backend использует
StartOwnedHandoff: до ACK владеет suspended/job-contained tree; после ACK поздняя
отмена не отзывает независимого помощника. Legacy StartUpdater wrapper сохранён,
actual route его не вызывает. Guard в Test-ProGo проверяет новый owned API.

Shared restore/update reservation не допускает выход/новые mutations до settlement.
Исправлены найденные независимым review queued races: accepted flags до снятия
reservation и reentrant UI callbacks; старый timeout не сбрасывает Accepted;
старый settle не сбрасывает новый ordinary shutdown; rejected flags очищаются до
callback. Own ordinary completion не зависит от чужого pending reserved task.
Ранее admitted native enable может завершить свою принятую фазу; preflight ждёт
её и затем отключает. Новый reconnect/automation после reservation подавляется,
публикация committed config сохраняется. Это не принудительная отмена OS вызова.

Backend independent review и финальный независимый lifecycle/tests review PASS.
Actual helper/descendant accept/reject/cancel/timeout и10 native Context сценариев
подготовлены, зарегистрированы; реальные установки не выполняются. Старые
WindowsRestore gate assertions перенесены на actual updater route и усилены.
Новый native run NOT_RUN; F31-R07 и общий F31-R11 остаются PARTIAL.

## A12: fresh ownership ярлыка Codex

`CodexProxyService.Enable/Disable` проверяет реальную ShellLink target и точные
arguments текущего launcher до изменения cmd/lnk. Copied description/один Marker
не доказывают ownership ярлыка. Проверенный объект удерживается read/delete handle;
Disable задаёт delete disposition именно ему. Existing owned link не переписывается,
новый link/cmd публикуется Move без overwrite. Missing parent не скрывает junction
выше: проверяются все ancestors до root. Foreign/malformed/denied/read-only/reparse
link даёт отказ с сохранением чужих файлов. Legacy MoveOwned/IsOwned не меняются.

Root source review обнаружил и затем подтвердил исправление ancestor bypass.
Реальные isolated COM/ACL/sharing/NTFS junction/replacement/pinned-handle/repeat
сценарии зарегистрированы. Native Windows NOT_RUN; A12 PARTIAL до точного CI.
Запись existing owned cmd остаётся in-place как прежде; общей атомарной транзакции
двух файлов и сохранности собственных bytes при любой I/O-ошибке не заявляется.
F31-R05 и F06 полностью не закрываются этим узким guard.

## История: CI585 и сверка остатка

Source `d0f9a6902345eab288b25e8f902df1514db3f2ca`, merge
`4b952b8b4e3a6473ceac230732384046801ce7fd`,
[run39667773240](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39667773240).
Ubuntu SUCCESS, Windows in_progress; полного PASS нет.
20files/1905addedlines перед push:0 secrets/PII findings, public-content310/0,
diff PASS; Python41PASS/3SKIP. Новые published commits/tree mapping в JSON.

[RESIDUAL_RECHECK_20261021.md](RESIDUAL_RECHECK_20261021.md) фиксирует повторную
независимую проверку на этом source. A04/F22-R03 и F31-R08/R09/R10 подтверждены
как source FAIL; native latency/воспроизведение/ПК NOT_RUN. Исправленные R06/R07
их не закрывают. Никакие owner PC/VPS/repair действия для проверки не выполнялись.

## CI585 completed FAIL: native ShellLink sharing

Source d0f9a690 / merge4b952b8b / run39667773240: Ubuntu SUCCESS,
Windows FAILURE. В CodexShortcutRepeat настоящий IPersistFile.Load упал с
FileLoadException0x80070020: COM повторно открывает pathname без share-delete,
несовместимо с удерживаемым DELETE handle guard. Это новый production guard bug,
не flaky timeout. Guard/ownership не ослабляются: вместо path-open ShellLink
загружается через IPersistStream из bounded bytes того же pinned file object.
Новый полный Desktop/R06/R07/key run, Maintenance encoding fix, shutdown/HomeVPN
и package остаются NOT_RUN в CI585. Предыдущие55/50/93/4739 — scoped CI584,
их нельзя перенести на source d0f9a690. Новый runtime fix получит отдельный CI.

## A12 stream-loader: исправление конкретного native FAIL

После CI585 ShellLink parsing использует снимок1..1МиБ из того же pinned
FileStream → SHCreateMemStream → IPersistStream.Load. Повторного pathname-open
нет; DELETE/share-read и handle deletion сохранены. Native object и memory stream
освобождаются nested finally, pinned file живёт до mutation caller.
Добавлены empty/oversize и отказ обычного File.OpenRead под DELETE pin.
Independent source review PASS; новый native run NOT_RUN. CI585 не исправляется
задним числом. В нём отдельно PASS29 standalone retention checks, включая новый
real pre-cancel/no-delete; десять cleanup Context сценариев ещё NOT_RUN.
