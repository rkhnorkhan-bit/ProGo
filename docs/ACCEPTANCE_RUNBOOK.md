## Текущий этап: Reissue uncertainty owned pending-read поверх CI646

Сверены PR88 OPEN/DRAFT, source `1a0b9ab4a3f75775bb712f418a780fb9f754c40f`, checkpoint
`b0b7a093e0b991b72bfc1a9cb89f2a0c0d61b82f`. До изменений51 worktrees clean/locks0,
root sole agent; работа выполняется в отдельной work/progo-reissue-uncertainty-on646-20261023.
Полные финальные [CI646](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39984905832) Windows12904/Ubuntu578
строк самостоятельно прочитаны. Checkout обоих jobs и Windows build identity: tested merge
`2c1787a60146fc7abeb8555af2e60b82bf5238d6`. Exact per-case evidence: JSON ci646_final.
PASS Create uncertainty8 (empty,pending,corrupt,locked,close-retry,dispose,stop-retry,service-dispose),
API6/UI8/UI12, selection exact ID/missing-ID, snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/
recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3. Desktop5298/Instance30/
Maintenance126/Shutdown47/HomeVPN3431/Portable91/SSH policy61. Python reported28 incl2SKIPPED,
Ubuntu operation16/QR13/provisioning PASS. Pack PASS только runner; uploads/prune SKIPPED.
Прежние CI640 permissions/CI642 startup причины не объявляются исправленными; диагностика сохранена.

Выбран ТОЛЬКО Reissue IssueUnconfirmed pending-read: это следующий согласованный синхронный UI I/O.
Local code `89d68a07c812e41c9dbd0d8133ad42c85f027b2f`:1 runtime+1 existing test147add/2delete.
Existing RefreshPendingAsync/RunAdminOwnedAsync/original Stop scope/явный owner; pending/unknown/
unreadable остаются gated, empty не разрешает слепой повтор без Refresh. Один read/version guard;
RevokeUnconfirmed и Complete/handoff/consume неизменны. Никакого нового API, recovery/consumption/
transport изменения. Create uncertainty8 и все прежние тела тестов byte-identical.

Восемь новых actual ShowDialog lifecycle modes: empty,pending,corrupt,locked,close-retry,dispose,
stop-retry,service-dispose. Реальный reader/DPAPI own fixture под barrier5s, modal15s/joins5s;
existing reissue core получает ровно revoke+invite frozen selected ID/name, без реального SSH.
Проверяются UI heartbeat/gating, confirmed old revocation/same-name sibling/selection/search/draft,
no replacement/handoff/consume/duplicate read, exact private bytes/request. Stop→explicit read-only
Refresh не повторяет revoke/invite и требует нового явного выбора; Close→reopen retained pending;
Dispose не получает late status. Finally releases barrier/disposes own window/stream/joins actual body
и service cleanup; старые timeout/assert/coverage сохранены.

PASS local review/structural isolation/signatures/C#5 await scopes/lexical delimiters/diff/JSON/
public-content321source0release/added secretsPII0. Lexical не AST/compiler.
Windows/.NET Framework/PowerShell недоступны: новая compilation/Reissue uncertainty8/full suites
NOT_RUN; SUCCESS646 не переносится на candidate.
Publication TODO: fresh expected-head guarded non-force existing PR88, ровно один automatic CI.
Workflows/VERSION неизменны, public uploads OFF, release/prune ineligible. После сохранения source/run/
checkpoint STOP без полного ожидания/второго этапа. Следующий шаг — ТОТ ЖЕ CI final logs/exact merge.
Оставшиеся production paths: RecoverAsync Pending request.Name; ConfirmConsumed bindings existing API8;
invite/revoke/reissue/recover-invite owned transport (отдельные ограниченные этапы). FullR09/owner ПК open.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового slice и перечисленные незавершённые UI I/O paths.

## История этапа: Create invitation uncertainty read поверх CI645

HEAD PR88/source `7ce8d202a7ddd15b7f7733fbc493a3d27b5e59c1` и checkpoint
`42f4dd2260032ccb23ef8bd1526ba05517fa535c` сверены;49 прежних worktrees clean/locks0,
Git/build processes0/root sole agent/другого писателя нет. Изолированная
work/progo-invite-uncertainty-on645-20261023 сохраняет прежние ветки/исправления.
Самостоятельно прочитаны полные [CI645](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39978779254)
Windows12768/Ubuntu578 строк; exact checkout/build tested merge
`20ed439869dbfbaf536693c639c8951d2a13dbbb`. Per-case evidence в JSON ci645_final.
PASS button exact original ID/escape+close gated clear/all7 other conditions, same-name missing-ID
no selection/destructive buttons disabled/no dispatch. API6/UI8/UI12 фактически PASS. Также PASS
snapshot12/dispatcher5/ownership8/cleanup4/repair8/list10/recover10/fresh10/HTTPS11/freshHTTPS10/
export11/chooser3; Desktop5298/Instance30/Maintenance126/Shutdown47/HomeVPN3295/Portable91/policy61.
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/provisioning PASS. Pack PASS runner-only,
uploads/prune SKIPPED; передачи/установки/owner acceptance нет. Старые причины640/642 не объявляются
исправленными, diagnostics сохранены. Selection/API/worker уже готовы, повторно не реализуются.

Выбран один ближайший остаток по реестру: синхронный HasPending в generic Create uncertainty UI.
Code local `3dd85ed8d5816355ced59ac98a94de439629450e`:1 runtime+1 existing test124add/2delete.
CreateAsync после неподтверждённого ответа ждёт existing RefreshPendingAsync/RunAdminOwnedAsync,
original operation Stop scope/explicit Friends owner. No new API/feature. Empty snapshot не разрешает
новую выдачу: прежний needsRefresh сохраняется до явного read-only Refresh. Unknown/unreadable остаются
blocked; нет raw exception detail и выдуманного подтверждения pending. Version gate исключает второй
post-action read. Cancel/Close/Dispose сохраняет actual ownership, Stop запрещает поздний результат.
Typed cancellation/pending catch, success/handoff/ConfirmConsumed, Reissue/Recover/transport неизменны.

Новые8 actual ShowDialog cases empty/pending/corrupt/locked/close-retry/dispose/stop-retry/service-dispose:
реальный DPAPI/private-state worker под barrier5s, modal15s/final joins5s. Fixture own request моделирует
retained request перед потерянным ответом, transport delegate только бросает safe canary exception.
Один mutation intent/no automatic replay/no handoff/no consume/no duplicate read; exact private bytes,
draft, UI heartbeat/admission, Stop Retry via read-only Refresh и reopen retained pending. Все старые
API6/UI8/UI12/selection и прочие test bodies byte-identical; никаких реальных VPS/credentials/network.
PASS local manual/static/signatures/lexical delimiter/diff/JSON/public321/0/secretsPII0. AST parser
недоступен (NOT_RUN), lexical не выдаётся за AST/compiler. Windows/.NET Framework/PowerShell нет:
new compilation/uncertainty8/full regression NOT_RUN, SUCCESS645 относится только к exact baseline.
Publication PASS: source `1a0b9ab4a3f75775bb712f418a780fb9f754c40f`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact645/full tree `0caa67cf03efd57f3008b545c94eae0525321be1`
равен reviewed freeze `d7a924660df4a15309e0c01de9e4d39fc07117e3`. Public diff5files633add/62delete:
1 runtime+1 test124add/2delete +3docs509add/60delete; прочие routes/API/worker/consumption/transport unchanged.
Merge ref `2c1787a60146fc7abeb8555af2e60b82bf5238d6` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic [CI646/39984905832](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39984905832),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated16:13:02UTC;
Windows120521444261/Ubuntu120521444000 in_progress. New compilation/uncertainty8/API6/UI8/UI12/full
suites NOT_RUN; SUCCESS645 не переносится. PR uploads OFF/release/prune ineligible, completion
skips ещё не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed для
исключения второго CI; full checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Далее ТОТ ЖЕ CI646 full final logs/exact merge: uncertainty8/API6/UI8/UI12/selection/mandatory suites.
Остаток: reissue HasPending; recovery request.Name; consumption API8 bindings; invite/revoke/reissue/
recover-invite owned transport. Не закрывать fullR09/приёмку ПК.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof нового Create uncertainty slice, затем названные remaining routes.

## История этапа: доказанное исправление selection test-contract CI644

PR88/source `b5af631eb2285a05a12485b4a45acc565c705f48`, checkpoint
`4ec09012d4d2c2b4904a63525d56348f8159acfe` сверены; все worktrees clean/locks0,
активных Git/build процессов/другого агента или писателя нет. Изолированная
work/progo-ci644-selection-contract-20261023; прежние изменения/ветки сохранены.
Полные финальные [CI644](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39918347267)
Windows10365/Ubuntu578 строк прочитаны самостоятельно. Exact checkout/build tested merge
`9c267269441761be2e3f47360916af632f957689`. Safe exact evidence в JSON ci644_final.
Единственный violated=selection-empty(False), selectedOriginalId=True; copies2/calls2/rows2,
needsRefreshFalse. Остальные7 исходных invariant=True, private/draft preserved. Это доказанный
устаревший test-contract ListRefreshWaiting/button, а не runtime defect.

Изначально Refresh сбрасывал выбор всегда. В утверждённом repeated-read migration владелец потребовал
сохранить selection; source6f8a41e уже реализует Replace(items,!needsRefresh): обычный read-only Refresh
сохраняет ТОЧНЫЙ прежний ID, uncertain-mutation reconciliation очищает выбор для нового явного решения.
Legacy test всё ещё требовал empty и для обычного route button. Исправление не просто снимает assert:
predicate теперь gated ? selectionEmpty : selected!=null && selected.Id==first.Id. Safety исходного
reconciliation сохранена и дополнена проверкой disabled Revoke/Reissue. Все7 других условий прежние.

Code local `4d511bda96dbb8ba45a32f4a5426f50d16ddcd43`:ONLY HomeVpnPreparationTests25add/4delete.
Регрессия missing-ID использует ещё один РЕАЛЬНЫЙ Refresh/native fixture: серверный ответ оставляет
другого друга с ТЕМ ЖЕ именем и вторым ID. Assert exact survivor ID/selected null/index-1/disabled
Revoke+Reissue, create/reconciliation/draft/search/private bytes. Нажатия disabled buttons не добавляют
copy/list calls и не потребляют request. Fixture response восстанавливается finally; исходные ожидания,
Pump/таймаут/cleanup не менялись, coverage увеличен. Runtime/transport/API/consumption/security unchanged.

CI644 PASS production/Desktop/relay compilation, startup both invariants, policy61/Desktop5298/
Instance30/Maintenance126/Shutdown47/export11/chooser3/list10; Ubuntu operation16/QR13/provisioning.
API6/UI8/UI12 и поздние suites NOT_RUN, aggregate HomeVPN/Portable не достигнуты. Pack/uploads/prune
SKIPPED. Исторические причины permissions640/startup642 не объявляются исправленными; diagnostics сохранены.
PASS local manual structural review/signatures/diff/JSON/public321/0/secretsPII0. Временный AST parser
недоступен: AST NOT_RUN, не выдаётся за compilation. .NET Framework/Windows unavailable: новый
compiler/native exact-ID/gated-clear/missing-ID/full regression NOT_RUN; старые PASS не переносятся.
Publication PASS: source `7ce8d202a7ddd15b7f7733fbc493a3d27b5e59c1`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact644/full tree `9b352d5f9c01305e9e8f5fb63d2e2b672c2be25a`
равен reviewed freeze `756d0a75c9e4d4605c22a5fd264e8d12872f0aa5`. Public diff4files345add/59delete:
1 test25add/4delete +3docs320add/55delete; runtime/other methods/workflows/VERSION unchanged.
Merge ref `20ed439869dbfbaf536693c639c8951d2a13dbbb` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic [CI645/39978779254](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39978779254),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated15:30:04UTC;
Windows120500269788/Ubuntu120500270121 in_progress. New compilation/exact-ID/gated-clear/missing-ID/
API6/UI8/UI12/full suites NOT_RUN. PR uploads OFF/release/prune ineligible; completion skips ещё
не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed для исключения
второго CI; exact checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Далее ТОТ ЖЕ CI645 full final logs/exact merge: button/escape/close, missing-ID/no-dispatch safety,
API6/UI8/UI12 и все mandatory suites. Written regression не объявляется выполненной.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: Windows proof corrected selection/missing-ID и недостигнутые invitation suites.

## История этапа: CI643 Refresh retry — test-only восемь инвариантов

HEAD PR88/source `ff8e6e25e7f5b4d58606faf1aff5e290c15d393a` и checkpoint
`9d17c59c5098c351cc40274a5e8f5dd581ff3db1` сверены;47 прежних worktrees clean/locks0,
активных Git/build процессов нет/root единственный агент, иной писатель не обнаружен. Отдельная
work/progo-ci643-refresh-diagnostic-20261023 сохраняет все прежние ветки/изменения.
Самостоятельно прочитаны полные [CI643](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39914345110)
Windows10356/Ubuntu578 строк: checkout/build identity exact tested merge
`f99df0878e1c8a0ffa08363ed52a12d162967852`. Exact evidence/markers в JSON ci643_final.

Startup diagnostics: capturedPidPresent/aliveKnown/alive=True, releaseExists=False. Before/after
connect/after alias:count1/pidPresentTrue/pidSameTrue/connectingTrue/stoppingFalse/requestedTrue/
expectedWaitingLifecycleTrue; оба исходных invariant=True, violations пусто. CI642 отказ НЕ
воспроизвёлся; его причина не установлена, runtime/test-contract fix не объявляется. Safe diagnostics
и гипотеза opportunistic CurrentPid null на busy gate сохранены. SSH policy61 PASS643, но причина
исторического permissions CI640 также остаётся невыясненной.
PASS643 Desktop5298/Instance30/Maintenance126/Shutdown47/export modal11/chooser3/owned list10.
Production/Desktop/relay fixture compilation PASS. API6/initialUI8/repeatedUI12 НЕ выполнялись:
HomeVpnPreparationTests.Run(Main88) упал ДО HomeVpnAdminRecoveryTests.Run(Main89). Поздние snapshot/
dispatcher/ownership/cleanup/repair/recover/fresh/HTTPS suites NOT_RUN643; aggregate HomeVPN/Portable
не достигнуты. Ubuntu operation16/QR13/isolated provisioning PASS, pack/uploads/prune SKIPPED.

Новый FAIL07:19:05.0395862UTC: tests/HomeVpnPreparationTests.cs:756–758, ListRefreshWaiting/button,
финальный составной assert после cancellation PASS/explicit retry. Safe stack: HomeVpnWizardTests.Check
il12→ListRefreshWaiting il1250→ListWaiting il351→PreparationTests.Run il656→WizardTests.Main il1794.
Ожидаются copies2/listCalls2/create enabled/needsRefresh false/selection empty/rows2/draft same/private
unchanged. Actual conjunct facts не выведены. Статическое противоречие: обычный non-gated Refresh
теперь сохраняет selected ID по требованию владельца; legacy assert всё ещё ждёт selection empty.
Это НЕ доказательство, что только этот conjunct нарушен; не меняем runtime или ожидание наугад.
В разрешённом объёме выбран ОДИН targeted diagnostic CI, а не повтор без новых данных.

Code local `9cc2fd762ed00ee66d15bfd5dbfab3496bc84735`: ONLY HomeVpnPreparationTests.cs14add/2delete.
Все восемь исходных условий именованы и snapshot-ятся в прежнем порядке ДО output; сохранены
один combined assert/selection-empty/таймауты/coverage/последовательность сценария/fixture cleanup.
Выводятся counts/boolean и все violated names; selectedOriginalId — только boolean сравнения,
не ID. No raw name/path/args/settings/credentials/journal/private bytes. Существующий safe stack
сохранён. Никаких runtime/new features/security/VPS/network/credential действий.
PASS local review/static/signatures/AST syntax1/C#5/diff/public321/0/secretsPII0. Windows/.NET Framework/
PowerShell недоступны: новый compiler/native8-invariant diagnostics/API6/UI8/UI12/full regression NOT_RUN.
Publication PASS: source `b5af631eb2285a05a12485b4a45acc565c705f48`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact643/full tree `6c628bf2715183e9966cc6391f6aaad2eba4e0d8`
равен reviewed freeze `de63d71d1567e2b6504bf7a35c080d268ea69ed3`. Public diff4files405add/51delete:
1 test14add/2delete +3 docs391add/49delete; runtime/startup diagnostics/other tests unchanged.
Merge ref `9c267269441761be2e3f47360916af632f957689` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic diagnostic [CI644/39918347267](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39918347267),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated07:31:42UTC;
Windows120297230336/Ubuntu120297230272 in_progress. New compilation/native8-invariants/API6/UI8/UI12/
full suites NOT_RUN; SUCCESS build643 не переносится. PR uploads OFF/release/prune ineligible,
completion skips пока не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ
pushed во избежание второго CI; exact checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI644 full final logs/exact merge, все8 named conditions, затем отдельно
минимальный доказанный fix с сохранением ordinary selection/gated reconciliation safety.
UI12 PASS только по реально выполненным markers; startup642 причина остаётся unresolved.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: concrete Refresh retry invariant и недостигнутые invitation API6/UI8/UI12.

## История этапа: разрешённая test-only диагностика CI642 count/PID

PR88/source `6f8a41e67709dae2a9794885e7e975918401f765`, local checkpoint
`b8f4a52e4fedbca2232316b71d7387f02ac890f0` сверены;46 прежних worktrees clean/locks0,
Git/build processes0/root sole agent, иной писатель не обнаружен. Exact-source CI collection содержит
только completed FAILURE642/attempt1, дубликатов/активных CI на baseline нет. Отдельная ветка
work/progo-ci642-command-diagnostic-20261023; прежняя работа не меняется/не откатывается.
CI642 source/merge/failure evidence сохранены в ci642_final; причина code/test/infra всё ещё не доказана.
Владелец отдельно разрешил test-only instrumentation и ОДИН automatic diagnostic CI; предыдущий
permission blocker снят, но guessed runtime fix по-прежнему запрещён.

Code local `4fe4e87df3d61a7d391efe07f3c944eafb6cefcf`: ONLY DesktopStartupTests.cs49add/2delete.
Именованы оба исходных условия: pending-route-count-one (count==1), original-fixture-pid-retained
(CurrentPid==captured nullable pid). Исходный conjunction/nullable equality и один combined Check
сохранены; ничего не исключается. Оба operand snapshot фиксируются в original count→PID порядке
до диагностического вывода. Original calls connect→codex-on не меняются; добавлены nonblocking
metadata samples до connect/после connect/после alias. count/pidPresent/pidSame/connecting/stopping/
requested/expectedWaitingLifecycle — только int/boolean. Own captured fixture liveness измеряется
отдельно: aliveKnown/alive, capturedPidPresent/releaseExists. При ошибке чтения liveness UNKNOWN,
не dead и не PASS. Sequential snapshots не выдаются за атомарный lifecycle.
CurrentPid использует Monitor.TryEnter и может дать null при занятости gate: это source contract,
но НЕ доказанная причина642. Runtime/getters/route admission/process ownership не меняются.

Collect violated names для обоих условий; failure-only safe stack: StackTrace(false), только имена
методов DesktopTests, максимум6. Никаких PID/путей/args/settings/raw exception/credentials/journal/
секретов в новых logs. Нет новых ожиданий/таймаутов/процессов; только readonly query собственного
captured fixture process, без kill/ACL/network. Original test steps/assert count/coverage/timeout/
release gate и finally fixture cleanup неизменны. Прежние API6/UI8/UI12/runtime tests не трогаются.

PASS local manual/static/signatures/AST syntax1/C#5/diff/public321/0/secretsPII0. Temporary review
checker false-positive Wait matched WaitingLifecycle; исправлен token check, повторно PASS до push.
Windows/.NET Framework/PowerShell отсутствуют: new compilation/native diagnostics/UI12/API6/UI8/
Instance/full suites NOT_RUN. CI642 compilation production/Desktop PASS, full Desktop FAIL;
SSH policy/ACL и relay suites NOT_RUN642, Ubuntu PASS — не переносятся на новый SHA.
Publication PASS: source `ff8e6e25e7f5b4d58606faf1aff5e290c15d393a`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact642/full tree `b7f61ef2e0a70ae30635c6f23e385d523df13b73`
равен reviewed freeze `36edeb58ed1e268db5c330bf09fe9aaa855753ab`. Public diff4files543add/70delete:
1 test49add/2delete +3 docs494add/68delete; runtime/other tests/workflows/VERSION unchanged.
Merge ref `f99df0878e1c8a0ffa08363ed52a12d162967852` пока НЕ final tested merge (нужны checkout/build logs).
Один automatic diagnostic [CI643/39914345110](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39914345110),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated06:54:11UTC;
Windows120284369071/Ubuntu120284368749 in_progress. New compilation/native invariant diagnostics/
UI12/API6/UI8/full suites NOT_RUN. PR uploads OFF/release/prune ineligible; completion skips пока
не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed для исключения
второго CI; full checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI643 full final logs/exact merge, именованные count/PID/liveness/lifecycle
facts и фактически выполненные suites. Нет runtime fix; причина642 пока не доказана.
Если отказ не воспроизводится, причина642 остаётся unproven, не объявлять runtime fix или полный UI12 PASS.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: concrete command availability invariant evidence и новая Windows проверка UI12.

## История этапа: CI642 investigation — причина пока не доказана, STOP

PR88/source `6f8a41e67709dae2a9794885e7e975918401f765` и checkpoint
`b645744846bdf27c5c00201107cb5c3c8c25932e` сверены;45 worktrees чистые, locks0,
активных git/build процессов нет, root единственный агент/других писателей не обнаружено.
Полные финальные [CI642](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39911077389)
Windows4275/Ubuntu578 строк прочитаны самостоятельно; checkout/build identity exact tested merge
`709e070d10138ffa330a0def5c21d61e0bf3e235`. Safe evidence в JSON ci642_final.

FAIL06:35:29.6307562UTC: tests/DesktopStartupTests.cs:150, CommandAvailabilityWorkflow.
После real CLI toggle и gated SOCKS ожидания вызываются Execute(connect), Execute(codex-on).
Составной assert: PendingRouteCount==1 && proxy.CurrentPid==captured pid. Message: dispatcher
rejects stale plain Connect and duplicate alias without replacing pending route.
Safe stack: DesktopTests.Check(Boolean,String)→CommandAvailabilityWorkflow(SettingsService)→Main(String[]).
Предыдущий UI availability assert passed06:35:29.5311042UTC. Actual count/old-new PID equality/
lifecycle state не напечатаны: неизвестно, нарушен первый/второй/оба инварианта. Первопричина
code/test/runner НЕ ДОКАЗАНА. Оригинальный fixture ждёт own release-file; исходный barrier и
route callbacks просмотрены, но нет evidence его состояния/child exit/late route completion.
Нельзя объявить race/fixture/инфраструктуру установленной причиной или чинить runtime наугад.

Это НЕ подтверждённое повторение permissions CI640. CommandAvailabilityWorkflow вызывается Main:118,
SSH policy только Main:162; execution до policy НЕ дошло. Policy и сохранённый failure-only ACL block
NOT_RUN, никаких ACL flags из этого запуска нет. Историческая причина CI640 остаётся невыясненной.
PASS exact642: production Build OK/Desktop compilation; ранее завершённые repair/vault/SOCKS51,
logging/diagnostics/backups/update suites приведены в JSON ci642_final. Полный Desktop FAIL.
Instance30/API6/initialUI8/repeatedUI12 и snapshot/dispatcher/ownership/cleanup/repair/list/recover/
fresh/HTTPS/export/chooser/Portable suites NOT_RUN; relay step SKIPPED, их fixture compilation NOT_RUN.
Ubuntu operation16/QR13/isolated provisioning PASS по full final logs. Pack/uploads/prune SKIPPED.
Результаты641 не переносятся на642.

Изменены ТОЛЬКО3 документа. Никаких code/test/timeout/assert/coverage/workflow/security/SSH/network
изменений. No push/new CI/rerun: команда разрешает новый CI только при обоснованном исправлении,
а доказанного исправления пока нет. Сохранены все предыдущие production changes/diagnostics.
Local docs checkpoint — HEAD(git log -1), не опубликован; PR HEAD остаётся6f8a41e.
Следующий ограниченный шаг — отдельный test-only диагностический этап: именованные ОБА исходных
инварианта, безопасные actual pending count/PID-present/PID-equal/connecting/stopping flags до и
после каждого Execute, без settings/args/credentials. Assert semantics/order/timeout/coverage прежние.
Для diagnostic CI нужно отдельное разрешение в силу текущего ограничения «CI только при обоснованном
исправлении». До него dependent fix BLOCKED, не guessed fix и не blind rerun. Других этапов нет.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: неразделённый route-count/PID invariant и невыполненные invitation UI12.

## История этапа: повторные invitation pending-read поверх CI641

PR88 HEAD/source `589a6364d42f2fbbcdbbfe4bd80396c27a26f41f`, local checkpoint
`70f0d4db5d645244145a58b2f6f1165016af2654` сверены.45 прежних worktrees чистые/locks0,
root единственный агент; active Git/build processes0 по /proc (ps unavailable). Отдельная ветка
work/progo-invitation-refresh-on641-20261023; сохранены все прежние ветки и checkpoints.
Полные финальные [CI641](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39907256278)
Windows12520/Ubuntu578 строк самостоятельно прочитаны: exact checkout/build tested merge
`517b65079be6d6645f132e313b2acf551cbb887c`. JSON ci641_final хранит exact per-case markers.
PASS SSH policy61/role canary4/all3 native policy targets/API6/initial UI8/reopen, Desktop5298/
Instance30/Maintenance126/Shutdown47/HomeVPN3079/Portable91; snapshot12/dispatcher5/ownership8/
cleanup4/repair8/list10/recover10/fresh10/HTTPS recovery11/freshHTTPS10/export11/chooser3.
Python reported28 incl2SKIPPED; Ubuntu operation16/QR13/isolated provisioning PASS по final logs.
Pack PASS runner-only; uploads/prune SKIPPED, ни передачи пакета, ни owner installation/acceptance.
CI640 permissions refusal НЕ воспроизвёлся: failure-only ACL block NOT_RUN. Конкретный файл/ACL/
principal и первопричина остаются НЕВЫЯСНЕННЫМИ; runtime/ACL fix не объявляется, diagnostics сохранены.

Один выбранный production slice: повторные pending reads после существующих операций и Refresh.
Code local `83db3a07889501395e4c288d38e9b725f36996d3`:2 runtime+1 existing test,204add/36delete.
Shared initial/repeated reader использует НЕИЗМЕНЁННЫЙ readonly API/RunAdminOwnedAsync; explicit
Friends owner/dispatcher. Logical ActionWork и physical PendingReadWork сохраняют владение. Original
operation Stop scope захватывается ДО transport и проверяется before/after private read/publication;
поздний list response после Stop не допускает fresh read/rows. Busy/action gate держится до settlement.
Один pending snapshot/version на action: Refresh не дублирует I/O в post-action path. Cancel Close/
Dispose удерживает реальную работу; explicit Close после settlement, Stop Retry/new owner reopen без
старого результата. Ошибка/отмена не изобретают empty и не снимают pending/reconciliation gate.
Refresh до accepted snapshot не меняет rows; сохраняет выбранный ID/search/draft. Старый safety
контракт uncertain mutation reconciliation по-прежнему очищает destructive selection, требуя явный
новый выбор (старый reissue test не изменён/не ослаблен). Другие Replace callers сохраняют прежний default.

Новые12 actual ShowDialog cases: refresh-empty/pending/corrupt/locked, post-complete/failure/pending,
close-retry/dispose/stop-retry/service-dispose/stale-scope. Real private bytes/held read5s/modal15s/
cleanup joins5s; native UI heartbeat, no parallel admission/duplicate I/O, Stop scope, unchanged request,
selection/search/draft and no late disposed status. Post-action route использует существующий revoke;
только transport delegate заменён fixture, реальные VPS/credentials/network не вызываются.
Old API6/UI8 и все прежние test method bodies сохранены точно. Consumption/direct uncertainty reads/
recovery request.Name/transport core/workflows/VERSION unchanged; никакой новой функции/протокола.
PASS local manual/static/AST syntax3/C#5/signatures/diff/public321/0/secretsPII0. Windows/.NET Framework/
PowerShell недоступны: новая compilation/repeated12/full regression NOT_RUN, SUCCESS641 не переносится.

Publication PASS: source `6f8a41e67709dae2a9794885e7e975918401f765`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Fetched parent exact641/full tree `553612d55db87d0f3dadfe971d6a545c0e7944a1`
равен reviewed freeze `9c6a66f41e7ae2972408081b1464a22f5767ac80`. Public diff6files730add/88delete:
2 runtime+1 existing test204add/36delete +3docs526add/52delete; остальные files unchanged.
Merge ref `709e070d10138ffa330a0def5c21d61e0bf3e235` пока НЕ final tested merge: нужны checkout/build logs.
Один automatic [CI642/39911077389](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39911077389),
pull_request/attempt1/raw exact-source count1, in_progress/conclusion null, updated06:22:27UTC;
Windows120273983681/Ubuntu120273983952 in_progress. New compilation/repeated12/API6/UI8/full suites
NOT_RUN, SUCCESS641 не переносится. PR uploads OFF/release/prune ineligible; completion skips ещё
не наблюдались. Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed во избежание
второго CI; full checkpoint в PR88 body. STOP без полного ожидания/второго этапа.
Следующий шаг — ТОТ ЖЕ CI642 full logs/exact tested merge: repeated12/API6/UI8/selection/
reconciliation и все mandatory old suites; historical CI640 ACL cause unresolved.
Остаток production invitation UI I/O:1) direct create/reissue uncertainty и recovery request.Name;
2) ConfirmConsumed create/reissue/recover (saved consumption API8 separately);3) invite/revoke/reissue/
recover-invite owned transport binding. CI640 ACL cause remains unresolved historical finding.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 — нет нового полного weighted gate;
главный блокер: exact Windows proof нового repeated12, затем remaining UI I/O и owner acceptance.

## История этапа: CI640 native auth-policy — bounded test-only diagnosis

HEAD PR88/source `fcc6e70b69ef591997f26a5b7fd5e12be5f7720b`, local checkpoint
`ce474ce730dc3787a512fbe89f3d76e04bf901ff`; все прежние worktrees чистые, Git locks0,
других git/build операций/агентов/писателей не обнаружено. Отдельная work/progo-ci640-auth-diagnostic-20261023.
Полные финальные [CI640](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39904249694)
Windows7755/Ubuntu578 строк самостоятельно прочитаны. Checkout/build identity exact tested merge
`45a69d39a2e7c7378f50e1202a60d863da5859f2`. JSON ci640_final хранит safe evidence/summary.

FAIL05:26:07.9541087UTC: tests/DesktopSshAuthPolicyTests.cs:131, AuthPolicyNative/policy.disabled,
original oracle assert ExitCode0&&!Truncated. exit255/0x000000FF/truncatedFalse/stdoutChars0/
stderrChars330/permissionsHintTrue/configHintFalse; stack DesktopTests.Check→AuthPolicyNative→
SshAuthPolicyWorkflow→Main. Production Build OK и Desktop compilation PASS; причина НЕ compilation.
Доказан native OpenSSH отказ «Bad owner or permissions» при чтении fixture config tree. КАКОЙ файл/
principal/rule — НЕ доказано; совпадение с новым invitation runtime, runner drift или inherited ACL —
гипотезы. Не изменяем runtime/ACL по предположению и не делаем blind rerun.

Фактически PASS SSH start66/startup62/inventory67/keymatching52/signing55/preparation76/signingUI50;
остальные ранее завершённые suites перечислены в ci640_final. Полный Desktop FAIL.
Instance30/API6/UI8/HTTPS recovery11/freshHTTPS10/snapshot/dispatcher/ownership/cleanup/repair/list/
recover/fresh/export/chooser/Portable — NOT_RUN на640, relay step SKIPPED. Production compilation
не означает compilation invitation fixture: она NOT_RUN. Ubuntu operation16/QR13/isolated provisioning
PASS по финальному логу. Pack/uploads/prune SKIPPED; прежний SUCCESS639 не переносится.

Code local `e40dc17cc709a3194f158a45f0ce52cf31e6cc09`: только DesktopSshAuthPolicyTests,
56add/1delete. При native failure до ПРЕЖНЕГО assert печатаются allowlisted role booleans и
readonly ACL flags/counts для собственных config/include/locked identity/directory. No raw stderr,
paths/SID/SDDL/names/credentials/journal/config contents; exception только typed flags/HResult.
Original three targets/order/7000ms/assert/cleanup unchanged, additional pure role canary4.
Reads не меняют ACL/fixture bytes; runtime/server/workflow/test-runner unchanged. Запуск диагностический,
чтобы определить конкретный file+ACL invariant; не считается исправлением первопричины.

PASS local static/manual/signatures/AST C#5/diff/public321/0/secretsPII0. Windows/.NET Framework/
PowerShell недоступны: новая compiler/native диагностика/роль canary4/API6/UI8/full suites NOT_RUN.
Publication PASS: source `589a6364d42f2fbbcdbbfe4bd80396c27a26f41f`, guarded non-force expected-head existing PR88 branch
integration/approved-plan-31-20261011. Fetched parent exact640/full tree `e939ae31e3eb6c5796448d79267ea997e1ca0bf1`
равен reviewed freeze `598f406abe6391c080c8c2077828d27800503996`. Public diff4files435add/67delete:
1 test56add/1delete+3docs379add/66delete; runtime/workflows/VERSION/other tests byte-identical640.
Merge ref `517b65079be6d6645f132e313b2acf551cbb887c` пока НЕ final tested merge (нужны checkout/build logs).
Один automatic [CI641/39907256278](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39907256278),
pull_request/attempt1/raw exact-source count1; in_progress/conclusion null, updated05:42:45UTC;
Windows120261940053/Ubuntu120261939848 in_progress. Новые compiler/native/canary4/API6/UI8/full suites
NOT_RUN. PR uploads OFF/release/prune ineligible, completion skips ещё не наблюдались.
Postpublication3-doc checkpoint local HEAD(git log -1), НЕ pushed во избежание второго CI;
точный checkpoint в PR88 body. STOP без ожидания полного CI/следующего этапа.
Следующий шаг — ТОТ ЖЕ diagnostic CI: full logs/checkout/build identity, failure role+ACL facts;
при случайном PASS oracle причина ACL остаётся недоказанной. Проверить реально выполненные API6/UI8
и regressions без переноса PASS. Ни второй этап, ни runtime fix в этом проходе.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0;
главный блокер: exact native refusal root cause и новая Windows проверка invitation, затем owner acceptance.

## История кандидата: initial invitation pending UI поверх CI639

Baseline source `b935fde9770bfcec83913e19d929a9558e4c76e2`, checkpoint
`c3348452409b168a48925e9e4423d667df8f56da`; PR88 head сверён, чужих dirty/lock/build
операций и активных писателей не обнаружено. Изолированная ветка сохраняет все прежние worktrees.
CI639 https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39900231280 completed SUCCESS:
самостоятельно прочитаны полные Windows12366/Ubuntu578 строк; tested merge/build identity
`0b6e71eb6794c03801949a83645917353e76b84e`. Exact per-case markers в JSON ci639_final.
PASS fresh HTTPS10, recovery11 (owner-dispose violations0/liveDraftTrue/late publications0),
Instance30, snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10/repair8/list10/export11/chooser3;
Desktop5294/Maintenance126/Shutdown47/HomeVPN2929/Portable91. Python reported28 incl2SKIPPED.
Pack PASS runner-only, uploads/prune SKIPPED; передачи/установки/owner acceptance нет.

Выбор: первичное локальное чтение pending-state при открытии существующего «Доступ друзей».
Сохранённый API и API6 перенесены БЕЗ переписывания из `d500df68629d8af7533e4eca2993284c3675161a`;
в public baseline они отсутствовали. Consumption `e8fbd2a` не переносится. Code local
`ce02533755992e0f66fcb4a0aff06c081ef6c699`: 3 runtime+2 existing test files251add/10delete.
Wizard передаёт explicit Friends owner/service. Actual readonly work принадлежит RunAdminOwnedAsync,
Stop удерживает I/O до settlement, CTS Close/Dispose и old Stop scope запрещают поздний результат.
До validated state изменения заблокированы. Явное закрытие отменяет чтение и ждёт реальную работу
асинхронно; CancelButton не завершает ShowDialog раньше. Reopen делает новый read, без позднего
cancelled результата. Снимок advisory, настоящая family admission прежняя; нет новых функций/записи/
потребления request. Legacy injected 3arg constructor совместим; production factory всегда supplies service.

Тесты: перенесённые API6 +8 actual ShowDialog cases empty/pending/corrupt/locked/close-retry/
dispose/stop/service-dispose; последующий reopen проверяет реальный retry. Held I/O5s/modal15s/
cleanup joins5s, real private bytes, no Admin calls, explicit owner, mutation gate, heartbeat и no duplicate.
Existing owned-list fixture ждёт InitializationWork перед Refresh (2 lines); прежние asserts не ослаблены.
PASS local manual/AST syntax5/C#5/signatures/diff/public321/0/secretsPII0; old API6 и existing test bodies
проверены на точное сохранение. .NET Framework/PowerShell/Windows отсутствуют: compilation/new native
API6/UI8/full regression NOT_RUN. SUCCESS639 относится ТОЛЬКО к baseline, не новому коду.

Publication PASS: source `fcc6e70b69ef591997f26a5b7fd5e12be5f7720b`, existing PR88 integration/approved-plan-31-20261011,
fresh expected-head non-force update. Parent exact639; fetched full tree `9d83e9a490da70528cbaa9a426749fce5be00c5d`
равен reviewed freeze `3600e2cf5b480c0864901aa0bd291f7fe3299421`. Public diff8files822add/82delete:
3 runtime+2 existing test251add/10delete,3 docs571add/72delete; worktree clean before postpub docs.
Merge ref `45a69d39a2e7c7378f50e1202a60d863da5859f2` пока НЕ tested merge: нужна checkout/build identity финальных logs.
Один automatic [CI640/39904249694](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39904249694),
raw exact-source total_count1/pull_request/attempt1; in_progress/conclusion null, updated05:10:04UTC.
Windows120252436528/Ubuntu120252436238 in_progress; new compilation/API6/UI8/full regression NOT_RUN.
Workflows/VERSION прежние, PR uploads OFF/release и prune ineligible; completion skip ещё не проверен.
Postpublication docs-only checkpoint local HEAD (git log -1), НЕ pushed чтобы не создавать второй CI;
full checkpoint также PR88 body. STOP без ожидания/второго этапа. Следующий шаг — ТОТ ЖЕ CI640:
финальные full logs/exact tested merge, API6/UI8/reopen и все mandatory прежние regression.
Остаток production UI I/O invitation (не полный новый scope проекта):
1. Повторный pending-state в RunAsync/RefreshAsync.
2. Pending checks при uncertainty create/reissue и recovery request.Name.
3. ConfirmConsumed после create/reissue/recover (saved API8 отдельно).
4. Invite/revoke/reissue/recover-invite transport через owned worker вместо legacy Admin.
Чужие setup/HTTPS/list/repair/export/EOF/ownership/cleanup routes не повторяются.
Реализация:58–80/100 · Проверенная готовность:22/100 · Изменение:+0 — новый native NOT_RUN;
главный блокер: точная Windows проверка кандидата, затем незавершённые UI I/O paths и приёмка ПК.

## История предыдущего кандидата

## Текущий кандидат: fresh HTTPS owned production10 на CI638; Windows NOT_RUN

Checkpoint `f75c5a0ba00b2f7aa9e495419ec48823f8acfd86`; PR88 source
`b69e7008bc889d88623669058e95197822b09f1e` / exact tested merge
`bd8646ce3bb15b2aebeed68b2b6d5ffdd3aad015`, no other writer/dirty work found.
[CI638](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39896043639) full final Windows/Ubuntu logs read.
PASS owner-dispose/HTTPS11/Instance30/snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10/
repair8/list10/export11/chooser3/HomeVPN2803/Portable91. Python relay reported28 incl2SKIPPED.
Pack PASS runner-only ZIP SHA2562bf8ec967e7bed08f527a41807ddb7f418d6e1815f40cfafd1108d3f56602f92;
uploads/prune SKIPPED, no public artifacts/owner install. All exact per-case evidence in JSON ci638_final.

Code `414cb610621a91ebd09b71d2ae6126e130bd813e`:2 runtime files+1 existing test222add/2delete +3 docs.
Existing new HTTPS button now uses retained ShareOwnedAsync and explicit configure UI/dispatcher,
frozen owner/origin, core single lease/receipt, owned cleanup. Stop after copy refuses registration/SSH;
mode1 post-result scope prevents later HTTPS/save/consumption after Stop, preserving actual Task outcome.
No new feature/protocol/invitation changes. Existing recovery11 test body/core/other routes preserved.
New10 real ShowDialog/default binding cases: default-refusal/copy-fault/cancel-retry/stop-copy/
dispose-copy/owner-dispose/cancel-command/cleanup-lock/result/stop-result. Prove heartbeat/admission,
no premature request, exact native-observed ID/journal/private bytes, cancellation/drain/no replay,
real locked cleanup retry, corrected live/disposed draft and no late publication. Local native executable
only; fixture stops at real confirmation boundary before external HTTPS/save. Own-resource finally joins.
PASS static review/AST/syntax3/C#5/signature arities including WaitForShare5+timeout/diff/public321/0/
secretPII0; no Windows compiler proof. New compilation/native/fresh HTTPS10 NOT_RUN, not638 SUCCESS.
Same5s/15s/300s deadlines, no coverage/assert relaxation or duplicate lease/Stop self-await.
Publication PASS source `b935fde9770bfcec83913e19d929a9558e4c76e2`, existing PR88 integration branch;
merge ref `0b6e71eb6794c03801949a83645917353e76b84e` not yet verified checkout/build identity.
Freeze `62b192a144a923a4383a67812494633f3e0fcff5`, tree `6cbea066716dd4ce92e58c71521b798b00248523`;
fetched exact638 parent/full tree equal reviewed freeze, expected-head non-force guard checked.
Public diff6files889add/71delete=2 runtime+1 test222add/2delete+3docs667add/69delete; other routes preserved.
Sole automatic [CI639/39900231280](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39900231280), pull_request/attempt1,
raw exact-source runs total_count1. In_progress/conclusion null, updated_at2026-10-23T04:25:25Z;
Windows120239881809/Ubuntu120239882025 in_progress. New compiler/native fresh HTTPS10/full NOT_RUN.
Uploads OFF/release/prune ineligible; completion not yet verified. Local post-publication docs checkpoint
is HEAD via git log -1, NOT pushed to avoid second CI; exact checkpoint in PR88 body. STOP without full
wait/second stage. Next SAME639 final exact logs: compiler/signatures, fresh HTTPS10/recovery11/
owner-dispose/Instance30/snapshot12/dispatcher5/ownership8/cleanup4/recover10/fresh10/export/chooser/full suites.
Invitation worker remains separate future scope, not started here.
Weights58–80/22 +0 unchanged, no fullR09 or owner acceptance claim/system/deployment action.

## История: CI637 draft lifetime test-contract fix; Windows NOT_RUN

Checkpoint `118b14aeabb21b78eebef260f8811c477ae857d3`, source
`e0a8185ae3deb66e14872735170b64d1f414acf6` / exact tested merge
`5aa6cbe43e07dc25d491b94a91f747b41f28e5aa`, PR88 OPEN/DRAFT, no other writer observed.
[CI637](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39892894242) both full final logs read.
Repository step SUCCESS/Desktop5294/Instance30/Maintenance126/Shutdown47/UpdaterPackage5;
export11/chooser3/repair8/list10 PASS. Ubuntu operation16/QR13/provisioning PASS.
Overall FAILURE: HTTPS recovery11 only5PASS/owner-dispose FAIL/5NOT_RUN. Later snapshot12/
dispatcher5/ownership8/cleanup4/recover10/fresh10/portable/Python relay transport NOT_RUN;
pack/uploads/prune SKIPPED. No whole CI green or inherited native PASS.

Only draft-unchanged=False,13 others True; owner-dispose state violations1/calls0/expected0/
copies1/verified0, both disposed/no handles, operation Faulted/Stop RanToCompletion, publications5/5.
Safe actual stack Check IL12/OwnedHttpsRecovery callback IL4968; then ModalWizardCase155/
OwnedHttpsRecovery1159/Run1101/Main1861. Wrapper Test-HomeVpnRelay.ps1:19.
Root cause is a test lifetime contract: reading native Text after destroying TextBox is not
preservation of a live editable draft. Exact14 outcomes/timestamps in JSON ci637_final.

Code `139e2eb4b4182d73d1807833fdc87096a97f2dcd`: one test method17add/4delete plus3 docs.
Corrected regression requires actual live controls/handles and unchanged draft immediately
before real owner Dispose. After physical operation/Stop, require actual destruction/no handles,
successful live assertion, no subsequent TextChanged or dispatcher publication. Snapshot counter
after framework teardown; live-window branch retains exact old address.Text comparison.
All14 named invariants/13 other expressions/aggregate0/old late-publication check preserved,
no phase/coverage/cleanup/timeout weakening. Safe boolean/count output only, runtime unchanged.
PASS local AST/reverse-diff/syntax1/C#5/diff/public321/0/secretsPII0; new compiler/native NOT_RUN.
Publication PASS source `b69e7008bc889d88623669058e95197822b09f1e`, existing PR88 integration branch;
merge ref `bd8646ce3bb15b2aebeed68b2b6d5ffdd3aad015` not yet verified checkout/build identity.
Freeze `bbf4d53850640efcd2b7877f1d8a0d58c7a07814`, tree `5641ec9975c979b371690ad1ee2b56156284acd3`;
fetched exact637 parent/full tree equal reviewed freeze, expected-head non-force guard checked.
Public diff4files588add/65delete=1 test method17add/4delete+3docs571add/61delete; runtime unchanged.
Sole automatic [CI638/39896043639](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39896043639), pull_request/attempt1,
raw exact-source runs total_count1. In_progress/conclusion null; updated_at2026-10-23T03:38:02Z;
Windows120226632093/Ubuntu120226631811 in_progress. New compilation/native/owner-dispose/recovery11
NOT_RUN. Uploads OFF/release/prune ineligible; completion steps not yet verified. Post-publication
docs checkpoint local HEAD via git log -1, NOT pushed to avoid second CI; exact checkpoint in PR88 body.
STOP without full wait/second feature. Next SAME638 final logs/exact tested merge: real live draft
and disposed owner/no late TextChanged/publication guarantees, all14 invariants/full recovery11/prior suites.
Weights58–80/22 +0 unchanged; no system/deployment/credential/binary actions.

## История: разрешённая test-only14-invariant диагностика; Windows NOT_RUN

Owner explicitly authorized instrumentation and one automatic diagnostic CI, not a blind rerun.
Checkpoint `1c005ecd6772e8f2c1e7d5f95ed9e4410a6e6fc3`, PR88 baseline source
`cffa667469498a0245857f0406246c9da3638ddd` / tested merge `fa58ba1bcef1e0e8ef8e450f14f37751b7f5b85d`,
CI636 FAILURE preserved: Instance30 PASS, HTTPS recovery11 only5PASS/1FAIL(owner-dispose)/5NOT_RUN.
Code `3ae2766fbe2d4436f192e705a17f27188a07db92`:2 test files58add/5delete, plus3 docs.
All14 original predicates are named and evaluated in original order at the same settled boundary;
false/evaluation exceptions remain failures, collected before original aggregate assertion.
No predicate/coverage/deadline/phase/cleanup change, including post-Dispose draft read.
Safe output only fixed names and booleans/counts/task/disposal states; no address text, request IDs,
credential/journal bytes, paths/argv or exception messages/Data/ToString. Safe type/method/IL stack
is emitted before modal rethrow and native harness catch via stdout; original exit1 retained.
PASS static AST14 parity/scenario preservation/syntax2/C#5/diff/public321/0/secretPII0, not compiler proof.
Local parity-script LF/CRLF mismatch corrected, no product change. New Windows compilation/native
NOT_RUN; owner-dispose/recovery11 NOT PASS. Runtime/other tests/scripts/workflows/VERSION exact636.

Publication PASS source `e0a8185ae3deb66e14872735170b64d1f414acf6`, existing PR88 integration branch;
merge ref `5aa6cbe43e07dc25d491b94a91f747b41f28e5aa` not yet verified checkout/build identity.
Freeze `fe007d3b47d6b43db571ac63e44162c05fa8e02c`, tree `ba3c18754f4989129c4b5aea419cad27e83cc077`;
fetched exact636 parent/full tree equal reviewed freeze, expected-head non-force guard checked.
Public diff5files750add/60delete=2 tests58add/5delete+3 docs692add/55delete; runtime unchanged.
Sole automatic diagnostic [CI637/39892894242](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39892894242),
pull_request/attempt1/raw exact-head runs total_count1. In_progress/conclusion null,
updated_at2026-10-23T03:03:05Z; Windows120216525306/Ubuntu120216524993 in_progress.
New Windows compilation/diagnostic/native/recovery11 NOT_RUN; owner-dispose not PASS.
Uploads OFF/release/prune ineligible; completion steps not yet verified. Local post-publication
docs checkpoint is HEAD via git log -1, NOT pushed to avoid second CI; exact checkpoint in PR88 body.
STOP without full wait/second feature. Next SAME637 final logs/exact tested merge: identify all
violated invariants/count/state/safe stack before runtime fix; previous636 FAIL remains history.
Weights58–80/22 +0 unchanged. No external system/deployment/credential/binary action.

## История: CI636 completed FAILURE; root cause BLOCKED

Recovery checkpoint `d136325336c9ff43bfc2e94f91c3ce2ede37778b`, unchanged published source
`cffa667469498a0245857f0406246c9da3638ddd`, tested merge/build identity
`fa58ba1bcef1e0e8ef8e450f14f37751b7f5b85d`. PR88 OPEN/DRAFT, no concurrent writer observed.
[CI636/39888080167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39888080167) final Windows/Ubuntu
logs independently read. Windows120200902870 FAILURE only late relay step; repository step SUCCESS.
PASS Test-ProGo/Desktop5294/Instance30(new actual receipt absence/negative guards)/Maintenance126/
Shutdown47/UpdaterPackage5, modal export11 and chooser3, repair8 and list10. Ubuntu120200903120
PASS full operation16/QR13/provisioning logs. No overall CI green; pack/uploads/prune SKIPPED.
HTTPS recovery11:5PASS(default-refusal/status-pending/cancel-retry/stop-copy/dispose-copy),
owner-dispose FAIL,5NOT_RUN(cancel-command/cleanup-lock/result/stop-result-copy/stop-result).
Later snapshot12/dispatcher5/ownership8/cleanup4/setup recover10/fresh10 NOT_RUN because their
Main186 invocation follows failing share-recovery Main91; historic earlier PASS not inherited.
HomeProfileHttp/portable/Python relay transport/full HomeVPN also NOT_RUN.

Failure02:36:01.6054070UTC: composite preservation assert tests/HomeVpnShareRecoveryTests.cs243–246,
OwnedHttpsRecovery owner-dispose. Observed stack only Test-HomeVpnRelay.ps1:19 char32;
HomeVpnWizardTests.Main190 prints exception type/message, not C# frames.14 independent predicates
share one label, so failed predicate and runtime/test root cause NOT proven. Prior typed cancellation,
Stop/drain/cleanup/no-late-publication/no-verification boundaries PASS. Suspected post-Dispose
address.Text observation is a hypothesis, not proof; no conditional bypass or timeout/assert weakening.
All14 predicates and final timestamp evidence retained in JSON ci636_relay_diagnosis.

Only3 local checkpoint docs changed; runtime/tests/scripts/workflows/VERSION exact636.
No push/new CI/rerun/PR mutation: current authorization requires a real fix before publication.
Next bounded diagnostic stage: capture separately labelled safe booleans plus full exception stack
while preserving every invariant/deadline, run exact owner-dispose on Windows .NET Framework.
Local Windows environment unavailable; a diagnostic CI requires separate authorization before
running because no proven correction exists yet. Then fix only proven cause. Local checkpoint
is HEAD via git log -1, intentionally not published. No next feature/system/deployment operation.
Weights58–80/22 +0 unchanged; narrow Instance PASS does not close whole weighted acceptance.

## История: CI635 instance receipt readiness fix; Windows NOT_RUN

Checkpoint `3768dc1ff4f9dd346234f188bf4dc4e45ae38f12`. Source `18585ce3df454d3280048db4b62fc7dfc1777d19` /
tested merge `ceb83663d58a7158227001868d013723bed70225`, [CI635](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39884480364)
completed FAILURE. Final Windows120189329732 log independently read: build identity/runtime
compile PASS, prior CS1501/CS1660 gone, Desktop5294 PASS. New fatal FileNotFoundException for
proxy-environment-backup.json during actual primary startup baseline. Observed frame only
Test-ProGo.ps1:649/NativeCommandError; C# frames absent. Source-inferred chain Wait253–259 →
sampler97 or107 → FileStream85, exact read cannot be distinguished. Recovery11 executed0,
NOT_RUN; later HomeVpnRelay/pack/uploads/prune SKIPPED. Ubuntu120189329558 SUCCESS metadata
only here; do not present metadata as final-log proof. Production timing/transience unknown.

Code `37a1c38f3d1e0c292a5fe61c281322fa0a4add44`: only InstanceTests sampler + real-file regressions,
38add/1delete +3 docs. Missing exact own receipt at its two read stages returns false/null,
never PASS. Same Wait10s/30ms and all Main assertions; permanent absence still timeout FAIL.
Missing settings/foreign file/malformed receipt/other I/O remain errors, lock32/33 unchanged.
Regressions: absent before/during read, repeated absence, then matching publication; fatal
guards and old wrong-port/pending/locked/atomic replacement checks retained. Own temp root
finally only. Runtime/scripts/workflows/other tests/signature fix/recovery11 unchanged635.
PASS local review/syntax1/await-scope/diff/public321/0/secretPII0/method preservation, not
Windows compiler/native proof. Changed fixture compiler/native/recovery11/full NOT_RUN.

Publication PASS source `cffa667469498a0245857f0406246c9da3638ddd` existing PR88 integration branch;
merge ref `fa58ba1bcef1e0e8ef8e450f14f37751b7f5b85d` is not yet verified checkout/build identity.
Freeze `8e414b821b9328e2f16238010961079704dd2387`, tree `b4da8abfda0d27954c339addd5db33589e7def3d`;
fetched exact635 parent/full tree equal freeze, expected-head non-force update checked.
Public diff4files355add/43delete, only fixture38add/1delete +3 docs. Sole automatic
[CI636/39888080167](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39888080167), attempt1,
raw exact-head runs total_count1. Run in_progress/conclusion null; updated_at2026-10-23T02:10:14Z.
Windows120200902870 build in_progress; Ubuntu120200903120 completed SUCCESS metadata only,
full final log not read here. Changed fixture compilation/native/complete Instance/recovery11
NOT_RUN. Uploads OFF/release excluded; completion upload/prune not yet verified. Local
post-publication docs checkpoint is HEAD via git log -1, NOT pushed to avoid a second CI;
exact checkpoint also in PR88 body. STOP without full CI wait/next function. Next SAME636
final logs/exact tested merge: receipt regressions, full Instance and actual recovery11/prior
checks. Persistent missing receipt must FAIL original deadline, then diagnose primary;
no deadline/assert relaxation. CI635 observed stack limits/producer uncertainty retained.
Weights58–80/22 +0 unchanged, fullR09/owner acceptance open; no VPS/network/key/install/
binary upload/release/personal deployment actions.

## История: кандидат CI634 share wait signature fix; Windows NOT_RUN

Checkpoint `f959ebc0fcee4eadae30a4e000649b0879d90523`. Bad source `763c730dc5e4d505fac35d592024911d744c2f91` /
checkout merge `5a0f6d56cf62b7c06f0e6ab862094519e47c239a`, [CI634](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39883511908)
completed FAILURE. Final Windows120186199123 log самостоятельно прочитан: CS1501 at1166/36
(WaitForShareAsync4args вместо5) и accompanying CS1660 at1165/25. Build failed before native
tests; recovery11 NOT_RUN, pack/later checks/uploads/prune SKIPPED. Ubuntu120186198956
completed SUCCESS metadata only в этом fix этапе, не полная проверка его финальных логов.

Code `e9b5f6c9de00549d304dbf0d63cb2549028e835b`: передан обязательный fifth TimeoutMs, прежние300000.
Только одна runtime строка + existing cancel-command regression purpose/owner/modal/native
Task/CancelButton assertion; всего2 source/test +3 docs. Checks/timeouts/workflow не ослаблены.
PASS local review/syntax2/await-scope/diff/public321/0/secretPII0 и targeted arity old4 reject/
fixed5 accept/removed-timeout mutation reject. Это не compiler/native proof. All other
tracked blobs/прежние contracts preserved. Исправленная compilation/recovery11/full regression
NOT_RUN; прежние633 PASS не наследуются. Требовать exact new source/merge на Test-ProGo.ps1
и Test-HomeVpnRelay.ps1, отсутствие CS1501/CS1660, actual recovery11 и прежние регрессии.

Publication PASS existing PR88: source `18585ce3df454d3280048db4b62fc7dfc1777d19`,
merge ref `ceb83663d58a7158227001868d013723bed70225` (build identity635 NOT_RUN),
freeze `58c3da75984d49e9e0bd620e0aaf1c5ea6b8ba6f`, tree `e319a5161ed816dcb7e306e3b2603739329d95dd`.
Fetched parent/tree checked, expected-head non-force update, diff5files403add/158delete.
Sole new automatic [CI635/39884480364](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39884480364),
attempt1/raw exact-head runs total_count1; no CI634 rerun. Run.updated_at2026-10-23T01:31:36Z
in_progress, Windows120189329732/Ubuntu120189329558 in_progress/conclusion null. Corrected
compiler/recovery11/full regression NOT_RUN; CI634 compile FAIL retained. Upload guards
OFF/release excluded; completion upload/prune not yet checked. Post-publication docs-only
checkpoint local HEAD (git log -1), not push; IDs/checkpoint also in PR body. STOP without
full wait/next function. Next SAME CI635 exact merge and final compiler/recovery11/prior
regression logs. Веса58–80/22 +0 прежние; fullR09/owner acceptance open.
No main merge/release/binaries/install/VPS/network/keys/credentials/personal deployment actions.

## История: кандидат HTTPS recover-share owned-worker11; Windows NOT_RUN

Checkpoint `65c0c497d72a3c110623c31b100f6351c0148326` сохранён. Проверенная база source
`3a212e47d18ad51a8037ed3944477dbba987a701` / tested merge `99c1737906e67ddaf8303856d24ee71dca7eea7d`,
[CI633 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39879011054): final BOTH
job logs independently checked, checkout/build identity/fresh10/recovery10/list10/repair8/
ownership8/cleanup4/snapshot12/dispatcher5/export modal11/chooser3 PASS; Desktop5294/HomeVPN2674/
Portable91. Windows Python26 PASS/2 SKIP; Ubuntu16+13 unit OK/isolated smoke PASS. Pack
SUCCESS runner-only, uploads/prune SKIPPED, artifacts API empty, no delivery/install.
Older pending633 sections below are dated history; new HTTPS recovery not covered by633.

Code `4f10ec66e08359dd75fe3e85e06f0bed8a279482`: existing configure recovery button → RecoverShareOwnedAsync
→ retained worker → explicit configure owner copy/status/result → exact cleanup. Only2
runtime +1 existing test +3 docs. Existing single share lease/core ID/status/result,
no wrapper reacquire/public Stop self-await. New mode2 continuation scope prevents HTTPS/
save/consumption after Stop while preserving an accepted SSH Task's actual success.
Fresh share/installation and invitation routes unchanged; no new product feature.

Minimum exact new source/merge Windows run: scripts/Test-ProGo.ps1 + Test-HomeVpnRelay.ps1.
Require11 new explicit PASS: default-refusal, status-pending, cancel-retry, stop-copy,
dispose-copy, owner-dispose, cancel-command, cleanup-lock, result, stop-result-copy,
stop-result. Actual default/modal binding/real DPAPI/helper/native tree and responsive UI;
Cancel→Retry preserves journal/draft/Task, Stop drains actual copy/query and refuses next
result, owner loss has no late UI, locked Stop false→unlock true without replay, protected
bytes/sibling/unrelated process unchanged. Result reaches test HTTPS boundary with original
origin then cancels before external HTTPS/save. Stop after successful result must leave
API success but prevent verification/save/consumption. No VPS or real target acceptance.
Held5s/modal15s/finally join5s, original native300s unchanged, own-resource cleanup only.
Require fresh10/recovery10/list10/repair8/ownership8/cleanup4/API17/export14/full regression.
New Windows compilation/native11/full regression NOT_RUN until completed exact logs.

Local PASS review/syntax3/C#5 await-scope/diff/public321/0/secretPII0/method preservation;
not compiler/native proof. Prior methods/EOF/BOM/export/core receipt/harness/workflows
preserved. Publication PASS in existing PR88: source `763c730dc5e4d505fac35d592024911d744c2f91`,
merge ref `5a0f6d56cf62b7c06f0e6ab862094519e47c239a` (build identity634 NOT_RUN),
freeze `52f72318de967f0e83c26675f7478e698ac8cdcb`, tree `b8755d64da13dfab57f19fdaa9f737ad7f116d0d`.
Fetched parent/tree verified; expected HEAD updated without force. Diff6files791add/63delete.
Sole automatic [CI634/39883511908](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39883511908),
attempt1/raw exact-head runs total_count1. Run.updated_at2026-10-23T01:21:24Z in_progress,
Windows120186199123/Ubuntu120186198956 in_progress/conclusion null. New compiler/native11/
full regression NOT_RUN, SUCCESS633 not inherited. Upload guards OFF/release excluded;
completion upload/prune not yet checked. Post-publication3-doc checkpoint local HEAD
(git log -1), not push, publication IDs/checkpoint also in PR body. STOP without waiting
or second stage. Next final logs of SAME CI634/exact merge, then separate fresh-share
worker preparation after proof; not started.
Weights58–80/22 +0 unchanged. FullR09/remaining Admin/owner acceptance open; no main merge/
release/binaries/install/VPS/network/credentials/personal deployment actions.

## История: кандидат fresh setup owned-worker10; Windows NOT_RUN

Checkpoint `c1e8b509eb0dff03175a737dce806472bffb53b0` сохранён. Проверенная база source
`5d5fed17998f77c1e1225127501f3f32ec4c0557` / tested merge `e52e4dfb1702d09ce7d8ec3a123bafc70d4ef370`,
[CI632 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39873394320), final BOTH
job logs independently checked: build identity, recovery10/list10/repair8/ownership8/cleanup4,
snapshot12/dispatcher5/export modal11/chooser3 explicit PASS; Desktop5294/HomeVPN2569/Portable91.
Windows Python26 PASS/2 SKIP, Ubuntu16+13 unit OK/isolated smoke PASS. Runner pack SUCCESS,
uploads/prune SKIPPED, artifacts API empty; package bytes not retrieved/delivered/installed.
Old pending632 entries below are dated history, not current; new fresh code not covered.

Code `ad66afc42d36b240671187004a0046b09b55bb1f`: existing fresh setup modal button → retained worker →
explicit wizard native copy/command → exact cleanup. Only2 runtime +1 existing test +3 docs.
Single unchanged core family lease includes Register/result/cleanup; no wrapper reacquire
or public Stop self-await. Post-copy Stop check prevents registration and next SSH; if
already registered/admitted, preserve exact request and offer read-only recovery, no replay.
Pending appearing after wizard read switches unchanged core to same original request.

Minimum exact new source/merge Windows run: scripts/Test-ProGo.ps1 + Test-HomeVpnRelay.ps1.
Require fresh10 PASS: default-refusal, copy-fault, cancel-retry, stop-copy, dispose-copy,
owner-dispose, cancel-command, cleanup-lock, result, pending-race. Actual ShowDialog/default
binding, single admission/lease/heartbeat and helper work, no request before copy settles,
Cancel→Retry retains original Task and issues only one setup after explicit retry, Stop/
Dispose before registration refuses it, native subprocess reads actual DPAPI journal before
command and retains same ID after cancel, pending race only reads status. Locked cleanup:
Stop false→unlock true, exact original error, no replay. Credentials/sibling/unrelated
process unchanged; no late UI. Positive fixture cancels local import before writes/tunnel.
Only transport/executable isolated, not replacement for real target acceptance. Bounded
5s I/O,15s modal,5s finally joins, native300s unchanged; finally owns only its resources.
Require recovery10/list10/repair8/ownership8/cleanup4/API17/export14/full regression as well.
New Windows compilation/native fresh10/full regression NOT_RUN until completed exact logs.

Local PASS manual review/syntax3/C#5 await-scope/diff/public321/0/secretPII0/AST preservation;
not compiler/native proof. All71 service and41 wizard methods and existing recovery code
unchanged, all other blobs/EOF/BOM/workflows/harness preserved. Publication PASS in existing
PR88: source `3a212e47d18ad51a8037ed3944477dbba987a701`, merge ref `99c1737906e67ddaf8303856d24ee71dca7eea7d`
(build identity633 NOT_RUN), freeze `c482b97121d840ebe8d14e7d889ea9597c84f873`,
tree `3a8a19954bb1b76431c8977015568e2d508717c5`. Fetched public parent/tree verified, expected HEAD
updated without force. Diff6files806add/68delete. Sole automatic
[CI633/39879011054](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39879011054), attempt1/raw head runs total_count1.
Run.updated_at2026-10-23T00:35:26Z in_progress, Windows120171576507/Ubuntu120171576142
in_progress/conclusion null. New compiler/native fresh10/full regression NOT_RUN; SUCCESS632
not inherited. Upload guards OFF/release excluded, completion upload/prune not yet checked.
Post-publication3-doc checkpoint local HEAD (git log -1), not push; latest IDs in PR body.
STOP without waiting or next code stage. Next final logs of SAME CI633 and exact merge,
fresh10 plus prior regression; weights58–80/22 +0 unchanged.
FullR09/remaining Admin/owner acceptance open; merge/release/binaries/install/VPS/credentials/
network and personal deployment excluded.

## История: кандидат recover-setup owned-worker10; Windows NOT_RUN

Проверенная база `2882b1f104bbfe469be4b51a615c2fe72f43855c` / merge `40dff9acb6c62600ee7495bb06ad76a5fb7332bc`,
[CI631 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39868272469). Получены
финальные логи обоих jobs: точный build identity, list10/repair8/ownership8/cleanup4,
snapshot12/dispatcher5/export modal11/chooser3 PASS, Desktop5294/HomeVPN2465/Portable91.
Windows Python26 PASS/2 SKIP; Ubuntu16+13 unit OK/isolated smoke PASS. Все uploads/prune
SKIPPED, артефактов нет. Checkpoint `8a89037677013c1e7e12156a73797800eccb1e19` сохранён; прежние pending — история.

Code `4976a5bf42ee98d7b986c5ec06c75543ecf9edf6`: existing recover-setup modal action → retained worker →
explicit wizard copy/status/result UI → exact cleanup. Четыре source/test/harness файла
+3 docs. Fresh setup/HTTPS/invitation mutation/recovery исключены, dependencies уже в631.
Существующий core family lease не захватывается повторно и удерживается через результат/
cleanup; worker не ждёт public Stop. Принятый native Task остаётся owned, следующий
transport после Stop/Dispose не запускается; прежний ID не заменяется, issuance/replay нет.

Минимальный Windows прогон нового exact source/merge: scripts/Test-ProGo.ps1 и
scripts/Test-HomeVpnRelay.ps1. Требовать10 новых mode PASS: default-refusal, status-pending,
cancel-retry, stop-copy, dispose-copy, owner-dispose, stop-between, cancel-command,
cleanup-lock, result. Реальная modal кнопка/default binding, lease/heartbeat/no duplicate,
typed cancellation и исходный ID/journal, Stop после принятого status до result, actual
native tree cancel/Dispose, locked Stop false→unlock true без copy/query replay, no late UI,
credentials/sibling/unrelated process сохранены. Native fixture принимает только исходные
read-only status/result. Positive result сохраняет исходный token; local test import
отменяется до credential writes/tunnel. Это не целевая приёмка и не live VPS integration.
Held5s/modal15s/finally join5s, original native300s unchanged; watchdog disposes only own
forms, cleanup joins actual operation/import/snapshot/driver/Stop. NativeRecovery остаётся последним.
Обязательны прежние list10/repair8/ownership8/cleanup4/API17/export14/full regression.
Новые Windows compilation/native10 NOT_RUN до completed exact evidence, старый631 не наследуется.

Local PASS review/syntax4/await-scope/diff/public321/0/secretPII0/AST-preservation;
это не compiler/native proof. Все прежние service/wizard method bodies и receipt protocol
сохранены. Публикация PASS в existing PR88, source `5d5fed17998f77c1e1225127501f3f32ec4c0557`,
merge ref `e52e4dfb1702d09ce7d8ec3a123bafc70d4ef370` (build identity632 ещё NOT_RUN),
freeze `30c8fa142af5a4b8bf794348a680489e33a2b09d`, tree `21bf1d85c7fce56a17a32926f0925665df8c77f9`.
Fetched parent/tree проверены, expected HEAD обновлён без force, diff7files581add/35delete.
Один automatic [CI632/39873394320](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39873394320),
attempt1/source exact, raw head runs total_count1. Run.updated_at2026-10-22T23:41:06Z:
in_progress, Windows120153006376/Ubuntu120153006482 in_progress/conclusion null.
Uploads OFF/release excluded по неизменным workflow guards; completion upload/prune ещё
не проверен. Новые compiler/native10/full regression NOT_RUN; SUCCESS631 не наследуется.
Post-publication3-doc checkpoint локальный HEAD (git log -1), не push; свежие IDs в PR body.
STOP без ожидания или второго этапа. Следующий шаг — финальные логи ЭТОГО ЖЕ CI632 и точный
tested merge; после proof отдельный fresh-setup owned preparation срез, сейчас не начат.
Веса58–80/22 прежние,delta0. Полный R09/owner acceptance открыт; main merge/release/
бинарники/установка/VPS/credentials/network исключены.

## История: кандидат read-only Admin list owned-worker10; Windows NOT_RUN

Проверенная база `e8588af677b15f6d72a9543f22658ca7ccdc06a9` / merge `75a0e468986846e836f1f430454bc705eaaca5c7`,
[CI630 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39860593042): completed
build identity/Windows120109980387/Ubuntu120109980505, repair8/ownership8/cleanup4/API17/
export14 и Desktop5294/HomeVPN2347/Portable91 PASS. Все uploads/prune SKIPPED, артефактов нет.
Checkpoint `4ba3ade1699c50bcabdd5c5e0d9f356a2138982f` сохранён; старые pending ниже — история.

Новый code `f13e12ecfc44651c9de7943cc4cfc359b7bf18e7`: existing initial list/Refresh → ListAdminOwnedAsync →
retained worker → dispatcher фактического wizard/Friends owner → native list waiting →
точный cleanup. Только3 source/test файла +3 docs. Все зависимости в630; repair/создание/
отзыв/recovery invitation, setup/HTTPS не меняются. Scope child dispatcher восстанавливается
в finally, disposed/Stop owner не запускает следующий transport/новое окно. Принятый Task
дренируется до реального исхода; cancel остаётся read-only typed, без mutation uncertainty.

Минимальный Windows прогон нового точного source/merge: scripts/Test-ProGo.ps1 и
scripts/Test-HomeVpnRelay.ps1. Требовать10 новых PASS: default-refusal, complete,
cancel-retry, stop-copy, dispose-copy, owner-dispose, stop-command, cleanup-lock,
refresh-cancel-retry, refresh-dispose. Production binding/real modal buttons, фактический
owner wizard/Friends, heartbeat/no duplicate, Cancel→Retry, исходный typed result,
Stop/Dispose drain, next SSH refusal, no late UI, locked false→unlock true без copy/SSH
replay, protected bytes/sibling/unrelated process и rows/selection/search/draft/gate сохранены.
Изолированы только transport/SSH executable; helpers/sharing/native process tree реальные.
Bounded5s barrier/15s modal/5s finally cleanup/20s native; timeout не PASS, свои окна/процессы.
Обязательна прежняя repair8/ownership8/cleanup4/snapshot12/dispatcher5/export14 и полная
регрессия. Новые Windows compiler/native10 NOT_RUN до exact evidence; SUCCESS630 не наследуется.

PASS local manual/syntax3/await-scope/diff/public321/0/secretPII0/AST-preservation;
это не compilation/native. EOF/BOM/export UI methods/API17/task8/cleanup4/repair8 и прочие
источники/workflows/VERSION сохранены. Один guarded PR88 update → один automatic PR CI;
uploads OFF/release исключён. Записать run и STOP без ожидания или второго этапа.
Следующий этап сначала проверяет тот же run; после proof — отдельный setup/recover-setup
snapshot/preparation worker срез. Веса58–80/22 неизменны,delta0. Полный R09/owner acceptance
открыты; main merge/release/бинарники/установка/VPS исключены.

## История: кандидат production repair owned-worker8; Windows NOT_RUN

Проверенная база: source `2b10ed6df31e2f40711c4efd856f98c5c2420a05`, merge `15448208d8bcb6428c1fbc8d2538729d978be9d5`,
[CI629 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39854845261).
Completed build identity/Windows120090256338/Ubuntu120090256606 подтверждают cleanup4,
ownership8, snapshot12/dispatcher5/export modal11/chooser3, Desktop5294/HomeVPN2263/
Portable91 PASS. Все uploads/prune SKIPPED, артефактов нет. Checkpoint `51d79ff99e443c36a871314d733d5c142dbd4ef8`
сохранён; старые pending записи ниже — история, не состояние нового кода.

Новый code `92a6304e5c66db0007878266e5e1eb4e2f5e8b02`: существующая repair кнопка → retained worker →
явный wizard dispatcher → actual preparation copy/native SSH → exact cleanup callback.
Только3 source/test файла +3 docs. Setup/recovery/list/HTTPS/invitation migration исключены.
Зависимости уже в629, незавершённых обязательных блоков нет. При Stop/Dispose перед
следующим transport он не запускается; принятый native Task остаётся owned до результата
или локальной отмены. Public Stop не ожидается внутри owned worker. Replay/откат не обещаются.

Минимальный Windows прогон — существующие scripts/Test-ProGo.ps1 и Test-HomeVpnRelay.ps1
на новом точном source/merge. Требовать8 новых mode PASS: default-refusal, complete,
cancel-retry, stop-copy, dispose-copy, owner-dispose, stop-command, cleanup-lock.
Default binding probe отказывает до key I/O; остальные вызывают real modal wizard button,
real helper/file sharing и native command substitute без VPS. Проверить responsive UI,
single admission, Cancel→Retry без старого результата, Stop/Dispose drain, отказ следующего
SSH после Stop, отсутствие post-dispose UI, honest typed outcome, locked false/unlock retry
true и отсутствие repeated copy/SSH/изменения protected bytes/чужих файлов/процессов.
Ожидания5s barrier/15s modal/5s fixture cleanup/20s native; finally release/dispose/drain.
Не повышать таймауты и не превращать timeout в PASS. Дополнительно прежние cleanup4,
ownership8/API17/export14 и полная регрессия. Новые compiler/native8 NOT_RUN до exact CI.

Локальные PASS manual diff/syntax3/await-scope/diff/public321/0/secretPII0/AST-preservation;
не считать их Windows compilation/native. EOF/BOM/UI export/API17/task8/cleanup4 и прочие
источники/workflows/VERSION сохранены. Один guarded PR88 update → один automatic PR CI,
uploads OFF/release исключён. Записать ID и STOP без ожидания или второго этапа.
Следующая проверка того же run; после доказательства — отдельный read-only Admin list срез.
Реализация58–80/22, изменение0, фиксированная методика прежняя. Остальной Admin/R09/owner
acceptance открыты; main merge/release/бинарники/установка/VPS исключены.

## История: кандидат exact Admin cleanup4; Windows NOT_RUN

Проверенная база `b2c79830d078497fadc284e96b02e6be815aa300` / merge `72d546f06ebc680029b202b75ba5489bcfcb7d51`;
[CI628 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39849852240)
независимо подтверждён completed jobs и Windows log: ownership8, snapshot12/dispatcher5,
export modal11/chooser3, Desktop5294/HomeVPN2241/Portable91 PASS. Все uploads/prune SKIPPED,
артефактов нет. Локальный исходный checkpoint `28d43e1f9076fd76403a7060f73e0b247c8d8646` сохранён.

Новый отдельный код `ef9bd8d7d730912874b2f32ed6589b8038f6e26d`: только HomeVpnService и SetupRecoveryTests,
точный replay e1e793e0. Dependencies уже в628; HTTPS/UI/invitation не нужны и не добавляются.
Фиксация exact cleanup action до typed fault; Stop/Dispose drain actual bodies, затем
только точное удаление. Пока lock остаётся — false/retained; unlock→explicit Stop — true,
без повторного copy/SSH. New factories отклоняются до завершения cleanup. Callback ещё
не подключён к production Admin UI; fullR09 остаётся открытым.

Минимальная Windows проверка нового source: существующие scripts/Test-ProGo.ps1 и
scripts/Test-HomeVpnRelay.ps1. Требовать точный source/merge build identity и PASS четырёх
режимов complete/list-lock/preparation-lock/dispose-lock: actual core, sharing failure,
исходный typed error, честный locked Stop false, unlock/retry true, Dispose drain,
защищённые bytes/чужой sibling сохранены, transport счётчики не увеличились при retry.
Bounded3s/5s и finally cleanup, исходный Pump12s; NativeRecovery остаётся последним.
Обязательна прежняя ownership8/snapshot12/dispatcher5/export14 и полная регрессия.
Локальные syntax/await-scope/manual/diff/public321/0/secretPII0/scope checks PASS;
новые Windows compilation/native4 NOT_RUN до фактического exact CI, старый628 не наследуется.

Один guarded update существующей PR88 ветки после scan; один automatic PR CI, без manual
dispatch/retry. Public uploads OFF, release/main merge/бинарники/установка/VPS исключены.
Сохранить source/merge/tree/run и завершить проход без ожидания; следующий этап сначала
проверяет этот же run. Отдельный дальнейший кодовый этап — owned Admin core wrapper.
Реализация58–80/22, изменение0, методика и веса прежние; NOT_RUN не PASS.

## История: подготовка кандидата Admin task ownership8 поверх точного CI627

Последняя проверенная база: source `8637c5e7ffcb19e2ed0622d74532bd88e245f9a0`, merge
`8968a9b0506fb0157a1f130c4a8379a2b78e8820`, [CI627 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39829750388).
Завершённый Windows лог подтверждает Desktop5294/snapshot12/dispatcher5/export modal11+
chooser3. Новый код `736740009c02fd88e65d4830f204166c953a471a`: compiler/native8 NOT_RUN.

Критерий этапа: reservation перед scheduling; Stop/Dispose до admission не запускает
factory; принятые задачи удерживаются до фактического file I/O; typed failure не теряется;
общий Stop drain учитывает несколько задач и запрещает новые factories во время ожидания.
Bodies не ожидают public Stop (он владеет их drain). Новый тест8: complete/stop/dispose/
lock/typed/queued-stop/queued-dispose/multiple, реальные локальные файлы, UI heartbeat,
ограниченные3s/5s ожидания и finally cleanup. Исходный Pump12s сохранён.

Следующий существующий CI выполняет scripts/Test-ProGo.ps1 и Test-HomeVpnRelay.ps1.
Проверить точный build identity,8 новых ownership случаев, прежние snapshot12/dispatcher5,
export14 и полную регрессию. NOT_RUN не PASS; чужой/старый SHA не подтверждает этот код.
Локальные PASS: syntax/await-scope2/manual/diff/public321/0/secretPII0/canonical scope2.
Snapshot/dispatcher и тело AdminAsync, UI/EOF, invitation/HTTPS/workflows/VERSION неизменны.
Public uploads OFF, release/main merge/выгрузка бинарников/установка исключены.

Отдельный личный MTProto deployment: по отчёту владельца iPhone/mobile без VPN,
сообщения/медиа PASS; автозапуск включён. Wi-Fi/server reboot/длительный прогон NOT_RUN.
Интеграция MTProto в ProGo и приёмка проекта этим не подтверждаются; личные реквизиты
не сохраняются. Веса и баллы58–80/22 прежние,delta0. После публикации сохранить SHA/run,
завершить этап без ожидания CI; следующий отдельный блок — exact Admin cleanup4.

## История: подготовка Admin snapshot + dispatcher поверх CI626

Проверенная база: source `ec5b0e9e2f9c52ab7437ae3b99b79d897a734262`, merge
`041aa00c505bb88b0d1bb876c9a79f004336d23f`, [CI626 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39823713132).
Завершённый Windows лог подтверждает Desktop5294, export modal11/native chooser3 PASS.
Новый код `30c712c4102aa93cb5f22ac5ce101311bd8468d6`: compiler/native17 NOT_RUN.

Проверка зависимостей PASS: все используемые recovery request/lease API, owner.Validate,
HomeProfileUiDispatcher и ModalWizardCase существуют в точной базе; текущий relay harness
уже компилирует и вызывает новые сценарии. Ownership/cleanup/production Admin migration
и invitation/HTTPS UI не входят в этап. Два исходных файла совпадают с сохранённым API17.

Минимальный Windows прогон — существующий CI: scripts/Test-ProGo.ps1, затем
scripts/Test-HomeVpnRelay.ps1. В новом точном build identity проверить12 snapshot случаев
(реальные DPAPI/lease/heartbeat, исходные типы ошибок, сохранность bytes),5 dispatcher
случаев (ShowDialog, owner loss до/после admission, native locked file error, single startup,
retained physical Task), затем полную регрессию включая ранее подтверждённые export14.
NOT_RUN не является PASS. Новый код не получает SUCCESS626 от базы.

Локальные PASS: syntax/await-scope2, полный diff, public321/0, secretPII0; source2 only,
полная сохранность EOF скрипта, export UI/tests, production AdminAsync, invitation/HTTPS,
workflows/VERSION. Один guarded PR update создаёт один автоматический CI; ручной dispatch
не нужен, public uploads OFF/release не запускается. Сохранить новый source/merge/run и
закончить этап без ожидания CI. Веса58–80/22 неизменны, изменение0. Ниже — история.

## История: проверочный кандидат UI экспорта на CI625

Последняя проверенная база: source `cc835548a51e525b4537959d414dc4a4fb17280a`, merge
`080e54c3312d62cc72977afb94d55c3c1a4f42ce`, [CI625 SUCCESS](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39811359508).
Новый локальный код `46cb9a253abd1aa8e2f69861e8a50ad216808ed4`: Windows compilation/native NOT_RUN.
Один следующий PR CI выполняет scripts/Test-ProGo.ps1 на новом точном source/merge.

Минимальное доказательство нового блока:11 реальных ShowDialog post-chooser случаев
(save, Cancel→Retry, close, Dispose, accepted cancel/close/Dispose, Stop до/после admission,
existing-file→Retry, accepted access change) и3 native SaveFileDialog child-сценария
(cancel, deliberately missing, injected error). Каждый child ограничен20s, discovery5s,
cleanup5s; missing/error должны сохранять ожидаемые ненулевые коды20/21 и подтверждённую
очистку только собственных окон/процесса. Исходный export API5+ownership9 и общий Desktop/
HomeWaiting regression также должны пройти. NOT_RUN не является PASS. Приёмка на ПК NOT_RUN.

До публикации PASS: syntax7/C#5 scope, public321/0, diff/secretPII0, восемь кодовых файлов;
полная каноническая побайтная сохранность EOF launcher кроме добавления одного harness
source; Admin/invitation/workflows/VERSION идентичны625. Upload guards OFF, release workflow
не срабатывает на integration/PR. Сохранить новый SHA/merge/run, закончить этот этап без
ожидания CI. Баллы58–80/22, изменение0. Прежние записи ниже являются историей.

## Exact EOF expectation corrected locally — strict protocol, native NOT_RUN

Isolated `c548e3baefd805bca43dbdd0cd9b6ea0081b898a` on exact624; one test script
5add1delete only. Read raw stdin to EOF and compare EXACT declared Console.InputEncoding
preamble passed as safe hex; reject arbitrary/extra/missing bytes with42. Logs lengths/
match only,no input data. This corrects my empty-text expectation against documented
Framework StreamWriter/AutoFlush behavior; no Console.InputEncoding/codepage mutation,
no broad acceptance of unexpected data. Actual624 byte count not invented. All0/17/
200ms/EOF5s/termination/stream/entry marker gates retained. Manual source/quoting/BOM/
diff/public320/0/secretsPII0 PASS; embedded Linux protocol accepted empty/exact BOM2,
rejected arbitrary/extra/missing42 in3 cases,all5s. Actual new PowerShell/native NOT_RUN.
Next only script+latest3docs freeze/canonical tree/fresh public workflow+PR guard and
one exact correction CI; UI/Admin/invitation code stays excluded. I58–80/Y22,delta0.

## CI624 actual startup PASS; new EOF test expectation FAIL — no full SUCCESS

Source63264c4119735220fa560ec0b20fb350d45e7754 /merge3f0b0c14ba3db55ea63b6825d486ea07e04e94b6,
run39807343603 completed Windows119923973872 FAIL/Ubuntu119923974608 SUCCESS.
Production/Desktop compilation PASS15:42:50.408. Actual positive Python exit0/body-entry
PASS15:52:24.5987627(process5780/start15ms,within5s); prior pre-entry stall did not recur.
New EOF probe reached its body15:52:24.6379583(process7316/start1ms) then returned42:
nonempty text,exact bytes/count NOT logged. This is my new test expectation error:
[Framework Process](https://github.com/microsoft/referencesource/blob/main/System/services/monitoring/system/diagnosticts/Process.cs)
creates Console.InputEncoding StreamWriter with AutoFlush; [StreamWriter](https://github.com/microsoft/referencesource/blob/main/mscorlib/system/io/streamwriter.cs)
flush writes its encoding preamble without application text. Next compare raw input to
EXACT declared preamble and EOF,reject all other bytes with42. No encoding/codepage or
system configuration change, deadline/negative-check weakening. Prior622/623 native
stack cause remains UNKNOWN; no retroactive causal certainty. exit17/200ms negatives,
Desktop/phone correction/HomeAPI14/harness/package NOT_RUN after new probe failed;
all uploads/prune SKIPPED. Native fixture failure preserved until corrected exact proof.

New agreed existing receipt-consumption worker `e8fbd2ae6dad919b95c2b92ed34bda3005aa52d6`
first implemented after recovery(pre409 patch never applied). Owns family lease through
actual private read/delete; validates exact token and rechecks all10 fields before
memory admission; cancel/refusal/stale ID or valid changed result preserves journal,
missing false,accepted actual delete true,sharing failure typed/no private error text.
Eight actual ShowDialog/real DPAPI+delete sharing/cancel/race vectors with3s/5s bounds,
full protected bytes/lease cleanup. Static manual/syntax2/C#5/diff/public321/0/secretsPII0
PASS; compiler/native8 NOT_RUN. Invitation view still synchronous/unconnected. Next
freeze its API14(pending6+consume8) separately; native proof before owned UI binding.
I58–80/Y22 exact620 weighted partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## CI624 in progress — owned stdin source published once

Source `63264c4119735220fa560ec0b20fb350d45e7754`, merge
`3f0b0c14ba3db55ea63b6825d486ea07e04e94b6`, full tree
`7a04b4e6b3850bdd52469cdb700854a2e6de7492` equals local freeze4794599 and fetched
source/merge exactly. [CI624/39807343603](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39807343603)
in_progress; one automatic run after guarded e847→63264c, no duplicate623/manual run.
Only test launcher owned stdin+strict EOF regression and three docs; runtime/src/tests/
workflows/VERSION unchanged623. New exact compiler/native/package NOT_RUN until final
completed evidence. Earlier623 zero-CPU pre-entry stall and622/621 failures preserved;
causal stdin hypothesis not retroactively proven. Local development UI14/Admin17/task8/
cleanup4/pending6 are EXCLUDED and compilation/native NOT_RUN. All public uploads OFF;
no release/main merge/install/binary delivery. Next retarget separate UI14 checking
freeze on exact624 with latest fixture/launcher fixes, preserving every local block;
inspect same624 before any new source changes its checking head. Scores I58–80/Y22
exact620 weighted partial evidence,delta0/fullR09 FAIL/R11 open/targetA0.

## Owned stdin correction prepared — strict native EOF check, Windows NOT_RUN

Isolated code `2e011b71219c5d7f3032c269911ade6edec3092b` on exact623,one test
script9add1delete. Noninteractive Python fixture tools now own RedirectStandardInput
and immediately close their own input for EOF, removing dependence on CI host input.
Dedicated stdin-eof-regression reads to EOF and requires marker+exit0 within5000ms.
Original positive0/nonzero17/200ms timeout and termination/stream criteria unchanged;
no runtime/interactive SSH/workflow/VERSION or system configuration changes. Busy CI
stdin remains a supported hypothesis for622/623, not an asserted captured native stack.
Manual input/stream/lifetime/PS5.1 quoting/BOM/diff/public320/0/secretsPII0 PASS. Exact
embedded bodies positive0/EOF0/wrong-input42 passed under Linux5s; actual PowerShell/
Windows launcher/native suites NOT_RUN. Next script+latest3docs freeze/scans/canonical
fulltree/fresh public PR+workflow guard and one new exact CI. All local UI/Admin/pending
changes excluded; no points. Current JSON pointers now explicitly separate published
tree and local development tree; obsolete608 current-string preserved as history.
I58–80/Y22 exact620 weighted partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## CI623 FAIL — native pre-entry facts; stdin ownership correction next

Sourcee84792499b2949602691c349c369d62a533fb444 /merge539e2a23fe7da9640c15971d0b35c7d3b3b47af6,
run39803053552 completed Windows119908864469 FAIL/Ubuntu119908863649 SUCCESS.
Production/Desktop compilation PASS15:15:32.696. Positive Python process started95ms
at15:24:42.145,pid1732,hostedtoolcache. Deadline15:24:47.169 elapsed5115ms: alive,
CPU0ms,RAM5959680bytes,both streams WaitingForActivation,no probe error,entry marker
ABSENT. Own process terminated/streams empty; strict5000ms failure retained. Desktop/
phone correction/API14/later Home/package NOT_RUN; all uploads/prune SKIPPED.

Confirmed launcher ownership gap: stdout/stderr redirected, stdin inherited. Exact
native stack/root cause still UNKNOWN. Busy inherited stdin is a supported HYPOTHESIS,
not a proven explanation: [CPython78961](https://github.com/python/cpython/issues/78961)
documents pre-entry Windows blocking on a pipe with outstanding I/O;
[Microsoft RedirectStandardInput](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardinput)
documents owning input/closing it for EOF. Next minimal script change: redirect stdin,
close it immediately for noninteractive fixture tools, add strict EOF regression. Keep
all deadlines/exit/error gates, no system/network/security changes or blind same-SHA run.

Local pending-state worker `d500df68629d8af7533e4eca2993284c3675161a` reconstructs
missing9859 as NEW source: Pending/Unreadable booleans, worker actual DPAPI validation,
corrupt/locked stays unreadable+pending,cancel distinct,no token/write/lease. Six actual
ShowDialog worker vectors empty/pending/completed/corrupt/locked/cancel preserve exact
encrypted file set/bytes and caller lease;3s heartbeat/5s settlement+cleanup, old Pump15s
unchanged. Manual/syntax2/C#5/diff/public321/0/secretsPII0 PASS; compiler/native6 NOT_RUN.
Invitation UI binding/consumption worker still TODO; no missing original object restored.
I58–80/Y22 exact620 partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## Exact Admin cleanup retention4 reconstructed locally — Windows NOT_RUN

Code `e1e793e0b0789e8d686454b052fd3b773623ed57` reconstructs optional core exact
cleanup retention before its original typed error completes; work/upload creation is
inside completion envelope. Service Stop drains all bodies, then retries only retained
closures off UI, keeps locked cleanup owned/false and permits explicit unlock+Stop
without copy/SSH/request replay. New factories refused while cleanup pending. No
production broad directory scan/delete; same exact cleanup routine and typed outcomes.
Four actual-core/injected local transport fixtures complete/list lock/preparation lock/
Dispose lock use real helper/output/DPAPI files, heartbeat/bounded barriers, strict
failed Stop then unlock retry, exact original failure, preserved unrelated sibling and
protected files. Early core failure surfaces directly instead of generic entry timeout.
Manual closure/lease/overload/lifetime/cleanup/diff/syntax2/C#5/public321/0/secretsPII0
PASS; compiler/native4 NOT_RUN, excluded623/API17 freeze. Static Admin UI still legacy;
no complete R09 or VPS claim. Next native API proof before owned production binding;
independent lost invitation pending-state primitive6 can be reconstructed separately.
Same623 Windows119908864469 build in_progress,Ubuntu119908863649 SUCCESS; no duplicate
or head change. I58–80/Y22 exact620 partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## CI623 in progress — bounded launcher diagnostic source published once

Source `e84792499b2949602691c349c369d62a533fb444`, merge
`539e2a23fe7da9640c15971d0b35c7d3b3b47af6`, full tree
`d527b0007a1cf4ef793cb3ccf2556078e2bb64fc` equals local freezef5484d6 and fetched
source/merge exactly. [CI623/39803053552](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39803053552)
in_progress; one automatic run, no duplicate622/manual dispatch. Script-only owned
Python startup facts/positive entry marker +three docs over622; all runtime/src/tests/
workflows/VERSION unchanged. New exact compiler/native/package NOT_RUN until completed
evidence. Earlier621 phone failure and622 Python timeout preserved separately, neither
silently converted to success. Upload guards OFF/no release/merge/install/binary delivery.
Next independent local exact Admin cleanup closure retention/retry4; UI/API17 frozen and
excluded623. I58–80/Y22 exact620 partial gates,delta0/fullR09 FAIL/R11 open/targetA0.

## Python launcher diagnostic prepared — unchanged strict native deadlines

Isolated code `7c95af616c38fd146e47e4dac388784d15d474f6` on exact622; one test
script16add2delete. Adds own PID/start duration/interpreter-path category and timeout
HasExited/CPU/RAM/stream-task state/error-type facts, never arguments/environment or
personal paths. Exit0 probe emits an entry marker and must both execute its body and
exit0. Kill is conditional on this owned process still running; deadline remains FAIL
even if it exits at the boundary. Existing5s exit0/exit17,200ms negative timeout,5s
termination/3s stream joins and all exit/error criteria retained. This diagnoses UNKNOWN
622 failure, not an invented cause fix or relaxed check. Manual PS5.1 quoting/lifetime/
strict-criteria/BOM/diff/public320/0/secretsPII0 PASS; exact embedded Python body executed
under Linux5s, required marker/exit0 PASS. Actual PowerShell/Windows launcher NOT_RUN.
Next isolated script+latest3docs freeze/canonical tree/fresh public PR+workflow/secret
check and one new exact diagnostic CI. Product/UI/Admin/workflows/VERSION unchanged622;
local later code excluded. I58–80/Y22 exact620 partial evidence,delta0.

## CI622 FAIL — different earlier Python startup boundary, cause UNKNOWN

Source9e6e5a15e9414d562af6f1788505437f2ac96d9c /merge8db81f1c11cc713690f3c87f9669d2e12b9aba73,
run39800500570 completed Windows119899498867 FAIL/Ubuntu119899499567 SUCCESS.
Production/Desktop compilation PASS, build identity14:59:13.4176149Z. Native owned
Python exit0-regression failed15:08:05.7445169: process exceeded5000ms and was cleaned.
Empty captured stdout/stderr; no old interpreter-entry/CPU/RAM/HasExited/stream-state
facts, so cause UNKNOWN. Desktop execution was NOT_RUN: phone budget correction is
NOT_RUN, not a repeat of621 or a claimed fix PASS. Later Home/API14/harness/package
NOT_RUN; all upload/prune SKIPPED. Existing test failure preserved, no timeout PASS.

Next isolate script-only safe startup/deadline facts and positive interpreter-entry
marker; retain5s positive,exit17/200ms negative criteria and exact owned cleanup. No
production or workflow change, no global timeout increase or blind same-SHA rerun.
UI freeze07363f7/Admin API17 c1dfc6d/ownership437b69e preserved and excluded.
I58–80/Y22 exact620 partial evidence,delta0/fullR09 FAIL/R11 open/targetA0.

## Admin ownership8 reconstructed; CI622 failed step awaiting final evidence

Local `437b69e8037a9c33e0f581679f0736dfc1321f2c` reserves every Admin public task
before scheduling, checks Stop identity/lifetime before factory admission, keeps actual
accepted body outcomes and snapshots/drains all tasks in public Stop/Dispose. Original
typed recovery cancellation survives. Metadata only under lifecycle gate; admitted body
runs on worker and must not await the public Stop that owns its drain. HasOwnedResources
includes pending bodies. Eight bounded real-file cases complete/Stop/Dispose/lock-fault/
typed-fault/queued Stop/queued Dispose/multiple accepted bodies check heartbeat, exact
single drain, no unstarted factory after Stop and protected-file preservation. Errors
also occur while Stop waits. Static manual ownership/lifetime/error/diff/syntax2/C#5/
public321/0/secretPII0 PASS; new compiler/native8 NOT_RUN. Actual Admin route unconnected;
no failed-cleanup retention yet. Excluded API17 freeze c1dfc6d and checking622.

Same622 Windows119899498867 latest job in_progress but build step completed FAILURE;
later test/package/upload steps SKIPPED,Ubuntu119899499567 SUCCESS. Final completed
log/conclusion must be checked before claiming cause or another fix. Next inspect this
same run's exact phone-state diagnostics, preserve every local block, no blind rerun.
I58–80/Y22 exact620 partial evidence,delta0/fullR09 FAIL/R11 open.

## Admin transport5 reconstructed — explicit owner, actual Task retained

Local `74f90504778f90ef9d89010e61f4ef2975e13853` reconstructs the reviewed UI
startup adapter and its publication-barrier followup in one coherent block. Only
callback startup uses HomeProfileUiDispatcher; actual returned Task is awaited after
owner disposal. Closed-before-admission refuses callback, null dispatcher permits only
worker-compatible injection. No automatic retry/process/lease creation, no production
AdminAsync binding yet. Five actual ShowDialog local-file vectors ready/before Dispose/
during Dispose/real locked-file IOException/worker-only; lost ambient context, worker
heartbeat, actual outcome and single UI publication, publication signal only AFTER
physical Task assignment.15s modal/5s barriers+join, own GUID outputs/cleanup only.
Manual lifecycle/publication/cleanup,diff,syntax2/C#5/public321/0/secretPII0 PASS;
compiler/native5 NOT_RUN. Snapshot12 a1fb931 also NOT_RUN, both excluded622.
Next freeze these two API blocks17 on published622 without UI or later task ownership.
Same622 Windows119899498867 in_progress(build step),Ubuntu119899499567 SUCCESS;
no duplicate/new checking head. I58–80/Y22 exact620 partial evidence,delta0.

## CI622 queued — narrow phone fixture correction published once

Source `9e6e5a15e9414d562af6f1788505437f2ac96d9c`, merge
`8db81f1c11cc713690f3c87f9669d2e12b9aba73`, canonical tree
`abf6f9e0f568d30833f9917ed2025d302c472a14` equals isolated freezeebe59b1 and fetched
source/merge exactly. [CI622/39800500570](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39800500570)
queued; new compiler/native/full regression/package NOT_RUN. Runtime unchanged621,
only two test files+three docs over2c0cb. Source CAS independently confirmed, no manual
rerun/duplicate621; preserve that failed exact result and620 last full SUCCESS separately.
All uploads OFF/no release/merge/install. UI freeze07363f7/Admin snapshota1fb931 excluded.
Next independent local Admin transport bridge5; poll same622 completed evidence before
any source publication cancels it. I58–80/Y22 exact620 partial gates,delta0.

## Narrow CI621 fixture correction prepared — Windows NOT_RUN

Isolated code `c5e35cc62dd40d8ccec2b7409489cbb0ac4a1c32` on exact published2c0cb;
two TEST files11add4delete only. New optional fixture settings budget retains old
shared3000 default; phone vectors explicitly select250 for phone/settings as well as
backup. Original compound refusal/responsiveness/paused/native proxy effects and retry
criteria unchanged byte-for-byte, barrier5s unchanged. Safe per-flag/task-status/fault-
type/elapsed diagnostic prints only if the original predicate fails. No product or
global timeout change; exact historical621 failing flag remains UNKNOWN. Confirmed
configuration mismatch is fixed; fresh Windows evidence must establish the result.
Manual timing/parity/arity/diff/syntax2/C#5/public320/0/secretPII0 PASS; new compiler/
native NOT_RUN. Next latest3 docs freeze on isolated621+c5e35cc, scan/canonical tree/
fresh PR head+workflow guard, publish one new source and inspect one fresh CI. UI freeze
07363f7 and Admin snapshota1fb931 are excluded and remain NOT_RUN. I58–80/Y22,delta0.

## CI621 FAIL — preserve local Admin snapshot; narrow fixture correction next

Exact source2c0cb1418dd2c6dc4b82698f1ee1fc49f1a57850 /merge2d3f1144a9608fdd85b7ed8ed99d99d4238b8d0c,
run39797286590 completed: Windows119887722612 FAIL,Ubuntu119887722929 SUCCESS.
Completed Windows log build identity14:38:00.173 PASS production/Desktop compilation;
diagnostic native chooser3 PASS14:49:54–14:50:00. Desktop fails14:50:27.8268657 in
PhoneStopContextHeld/shutdown original compound phone-cleanup-deadline assertion.
Individual failed flags UNKNOWN. Later Home/API14,harness compilation,package NOT_RUN;
all uploads/prune SKIPPED. Earlier API responses said queued; completed log proves
Windows already ran14:38–14:50, so no duration is falsely attributed to a queue.

Confirmed fixture configuration mismatch: AsyncRestoreContextFixture(root,250) forwards
250 only to backup budget; actual phone/settings budget defaults3000 while its held
cleanup barrier is5000. Sequential preceding drains can consume the remaining margin;
exact failed621 predicate still UNKNOWN. Next isolate two test-file correction: explicit
phone/settings budget250 for these phone vectors plus safe per-flag/task-type/elapsed
failure diagnostic. Retain all original criteria,5s barrier and existing shared defaults;
no production timeout increase/disabled checks/blind same-SHA rerun. Freeze on exact621,
exclude UI/Admin work, publish new source only after review and one fresh CI.

Local readonly Admin snapshot `a1fb9316c32537fdbdd3a30d248ebabfdd963329` preserved:
frozen owner, worker selected-key existence/DPAPI request reads, existing family refusals,
no lease/register/consume/staging/remote action; copied snapshot outputs.12 bounded
native vectors before terminalNativeRecovery; original Pump default12s retained while
new vectors use3s heartbeat/5s barrier+join. Static manual/syntax2/C#5/diff/public321/0/
secretPII0 PASS; new compiler/native NOT_RUN, actual AdminAsync still sync. Excluded621.
UI freeze07363f7 untouched. I58–80/Y22 exact620 partial evidence,delta0/fullR09 FAIL.

## Native profile chooser3 reconstructed — Windows NOT_RUN

Code/tests `a1fb4e78d6b8a3d1e4e9a1e214db1af7ec7193fa`: actual production profile
Save button inside real outer ShowDialog/nested SaveFileDialog, own child/job20s;
existing PID/thread/root-owner helper unchanged, native discovery/close5s and cleanup
join5s. Positive Cancel verifies modal owner remains open, no export task/file/issuance,
unchanged protected tree. Deliberate missing20 and injected error21 require the exact
nonzero outcome plus cleanup marker and actual native-window observation; real timeout
in positive case fails. Explicit new DesktopTests partial file compiled exactly once;
script BOM preserved. Manual trace/diff/syntax3/C#5/public321/0/added secretPII0 PASS.
PowerShell/compiler/native3 NOT_RUN. Not included in620 or API-only621.

Local export UI9b555e5 +modal11 b1f9967 +chooser3 a1fb4e7 form a separate next checking
cohort; freeze on exact published621 source with latest3 docs, but do not publish while
621 is active (would cancel it). Same621 Windows119887722612 queued/Ubuntu119887722929
SUCCESS. Continue independent reviewed Admin read snapshot locally; preserve both frozen
sources. I58–80/Y22,delta0; fullR09 and target/installer/private delivery remain open.

## Modal export regression reconstructed — Windows NOT_RUN

Tests `b1f99672aaecc0bb3a9916b1af6b51ae11988e36` add11 actual outer ShowDialog
post-chooser scenarios: save,Cancel→Retry,Close,Dispose,accepted Cancel/Close/Dispose,
global Stop/accepted Stop,locked existing→new-name retry,accepted access change.
Checks retain one physical task across native loop turns, DialogResult.None, honest
actual file result, original captured identity, no old result over new retry, unchanged
DPAPI settings, no installation claim/late relay/late disposed controls. Existing15s
modal/5s worker+cleanup budgets, exact owned GUID outputs only; shared production
ExportProfile helper invoked after chooser. Actual native chooser proof is separate.
Manual phase/cleanup/lifetime diff,syntax1/C#5/public320/0/secretsPII0 PASS. New compiler/
native11 NOT_RUN; excluded from621. Next native profile chooser3 with existing owned
PID/thread/root-owner helper and unchanged5s discovery/20s child job budgets.
I58–80/Y22,delta0; do not alter checking621 source while Windows is queued.

## Actual export UI reconstructed locally — NOT_RUN on Windows

Code `9b555e5ea0f4d8de797b516a84ed464db32561cd`, three production files only:
actual SaveFileDialog now selects the path before retained ExportProfile/ExportProfileAsync;
all profile XML/file I/O is worker-owned, CancelButton stays DialogResult.None and
explicit Close/Dispose retains actual work. Cancel before admission permits retry;
accepted save is reported honestly, changed access/home cannot mark the current
profile issued, file saving never confirms installation. IOException messages omit
local paths. Export blocks only another phone route; desktop/settings/Stop remain
available under existing command policy. Manual diff/syntax3/C#5-await/public320/0/
secretPII0 PASS; new compiler/native NOT_RUN. Not included in checking621.
Next reconstruct11 actual ShowDialog post-chooser cases, then native chooser3; preserve
exact621 source while its Windows119887722612 is queued. Ubuntu119887722929 SUCCESS
only for621/2c0cb/2d3f. No duplicate CI or upload. I58–80/Y22,delta0.

## CI621 queued — reconstructed API published once

Published source `2c0cb1418dd2c6dc4b82698f1ee1fc49f1a57850`, merge
`2d3f1144a9608fdd85b7ed8ed99d99d4238b8d0c`, full tree
`d75aa2836c8d83ac1a1ccae39ac9b088ae3940f4` equals local freeze48343aa; fetched source
and merge equal exactly. Guarded source CAS independently confirmed in PR88.
[CI621/39797286590](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39797286590)
queued; new Windows compilation/native14/regression and package NOT_RUN. Do not inherit
620 SUCCESS or launch duplicate CI. Poll this same ID/completed jobs and log. Public
uploads OFF, workflow/VERSION unchanged; no release/merge/install or binary delivery.
First independent local next task: reconstruct actual post-chooser export UI ownership
in a separate commit, without changing621 while it runs. I58–80/Y22(raw21.75),delta0.

## Reconstructed owned export API — checking publication pending

Local service/test code `8903762ee659efaaa1923aacf146f7201e90c557` follows worker
`0cf5328f5e00f669a18e91baeb43d72842788d98`; these are NEW reconstructed sources,
not restored original missing objects. Service publishes one public export task before
worker/file I/O, joins exact duplicates and refuses conflicting export, captures access,
home and Stop identity with memory-only owner admission. Stop/Dispose cancels before
admission and joins actual accepted file work without UI blocking or self-await. Nine
bounded native cases save/cancel/Stop/Dispose/accepted Stop/accepted Dispose/stale access/
stale home/refused owner also check unchanged real DPAPI files and no started relay.
Local manual ownership/lifetime/diff, syntax/C#5 scope, public320/0 and added secretPII0
PASS; new Windows compilation/native14 NOT_RUN. Actual SaveProfile remains synchronous,
fullR09 FAIL. CI620 is only the unchanged published baseline, never this local code.

Freeze exactly six files: src/HomeVpnAccess.cs,src/HomeVpnService.cs,
tests/HomeVpnWaitingTests.cs,docs/PROJECT_STATE.md,docs/ACCEPTANCE_RUNBOOK.md,
docs/project_state.json. No export UI/chooser/Admin/invitation work or workflow/VERSION
changes. Public PR88 head still12d5/base4bb,620 completed; upload guards OFF/release not
triggered. Next canonical tree equality and guarded source CAS once, record one new CI
ID; new native14 plus existing full regression on its exact SHA. In parallel local UI
reconstruction can proceed in a separate commit without changing that checking source.
I58–80/Y22(raw21.75),delta0; target/installer/private delivery gates stay open.

## Reconstructed export worker — new local source, Windows NOT_RUN

Local code `0cf5328f5e00f669a18e91baeb43d72842788d98` reconstructs the missing reviewed
export primitive on exact published620. Two files,70 additions: immutable value/string
snapshot before worker scheduling; memory-only admission; real XML/file I/O off UI;
CreateNew prevents overwrite; cancellation after admission keeps the actual outcome.
Five bounded native cases cover save/cancel/refusal/locked existing/accepted-cancel,
including original identity/password/CA after caller edits. Own GUID output only,
release and join5s, heartbeat3s; this primitive fixture uses Show(), not modal UI proof.
Manual diff/syntax2/C#5 await-scope/public320/0/added-content secretPII0 PASS. No .NET
or Windows runtime here: new compilation/native NOT_RUN; CI620 does not cover this
code. Scores unchanged I58–80/Y22(raw21.75),delta0. Next reconstruct service ownership,
then publish only reviewed API5+9 and latest evidence docs for one new exact CI.

## Recovery after environment_offline — CI620 verified; local snapshot earlier

Current published/tested source `12d5cbc47b5cf2c1be0a0b16b2186b938da51952`, merge
`7853b8cd1aefe09456392e9b30b765943ea5e5af`, canonical tree `e160e3a481292fa6df0f20628120932f9ea2caab`.
[CI620/39777591168](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39777591168)
Windows119820381219/Ubuntu119820380887 SUCCESS, completed jobs/log verified; local
source and merge trees exactly equal. Build identity12:21:11.777Z, Desktop5287,
HomeVPN1948 at12:47:47.040Z, Portable91, Python28=26PASS2SKIP. Actual modal import/
receipt11 +initial snapshot5 +receipt worker7 PASS, including cancel-edited-retry,
actual retry SSH boundary and terminal recovery order. Signing55/UI50/preparation76
and four actual direct/owned-agent SSH startup/recovery child-lifetime I/O metrics
PASS. These are partial dev criteria, not installed owner/VPS or whole-app performance.

Runner ZIP0.2.2 SHA256 `ba60dcbc4642834baaa8128de3176e1bd751b7a5e5ff8b8dff07dd53b033eae6`;
all uploads/prune SKIPPED. ZIP bytes not retrieved/delivered/installed; private delivery
BLOCKED pending owner-approved channel. No release/merge/install or duplicate CI.
All28 existing weighted partial evidence markers matched exactly in620 completed log:
same weights/D0.5/P0.5/A0, I58–80/Y22(raw21.75)/delta0; no new full weighted gate.

Environment now executes. Observed old local root HEAD b1b2139cf3b338ca4ed965941bfab07d8158cec7,
clean20 worktrees, no pending Git operations/build/SSH processes or another executor.
Later local source/checking objects9859/5387/674e/65590/export/Admin and checkpointfecb718
are MISSING in this filesystem; the reason for the older snapshot is UNKNOWN. Nothing
was reset/deleted. Old root protected by recovery/pre-offline-snapshot-20261022 and
left untouched. Published source+merge fetched successfully; separate current worktree
ProGo-recovered-ci620 /work/progo-recovered-ci620-20261022 starts at tested12d5.
No uncommitted changes existed to save. Git fetch sessions completed, no duplicate CI.

Unpublished export/Admin/default-configure/pending-state work described below remains
historical reviewed work, not present recovered code or Windows PASS. Do not pretend
the missing original commits were restored. First unfinished task: reconstruct the
same bounded immutable profile-export worker/API5+9 cases from saved source review,
with new commits/SHAs and fresh Windows verification. Do not expand features or replay
any external action. Later Admin blocks and invitation pending-state9859 also require
explicit reconstruction/verification; consumption-worker patch never applied before
409. Existing public620 native evidence stays exact; fullR09 FAIL/R11PARTIAL/targetA0.

Historical checkpoint sections below keep their original time/source meaning; their
in-progress/ref-NOT_RUN entries are superseded by this recovery record.

## Frozen actual-SSH boundary fixture fix — one file on exact619

Code `b1eec12581c67a53c7ecc0fbdd512488f0c0b904`, code treebabe3b22bf81ed9bc1e9b12035fa893c9f9d5c76,
ProGo-ci619-boundary on5c5ac7e/f682. Root5387ddeb narrow test fix only,19add6delete.
Unchanged original SSH Close assertions after a bounded actual command admission;
finally releases/joins exact response/task/snapshot. Production files and all
export/Admin extensions/default configure binding excluded. Fullsource manual/diff/
public320/0/syntax/C#5 PASS; new Windows compiler/native NOT_RUN. Next latest3 docs
freeze, added-content secret/PII and fresh public workflow/head checks, fulltree equality,
guarded f682→new source CAS once.619 FAIL/partial proofs and617 last full SUCCESS
kept separately. I58–80/Y22/raw21.75/delta0. History below.

## CI619 FAIL after full Test-ProGo PASS — asynchronous fixture boundary fix

Exact source `f682432f4afd53bc7b3e7c9142d60dcb42a6a22e`, merge
`5d615896c349051606e6212ed119b3a2cb8ce356`, run39773542091 Windows119806729629 FAIL,
Ubuntu119806729392 SUCCESS, completed log checked. Build identity11:50:13.807;
full Test-ProGo step SUCCESS, Desktop5287 PASS12:08:30.718/Instance28/Maintenance126/
Shutdown47 PASS12:12:04.641. Prior618 startup timeout did not reproduce; underlying
predicate cause still UNKNOWN, diagnostic retained (no fabricated root-cause fix).
Four actual direct/agent startup/recovery latency/CPU/RAM/child-I/O collections PASS
12:07:14.820–21.684. HomeWaiting completed actual modal import/receipt11 routes
12:12:32–41, preserving Cancel→Retry/Close/Dispose/global Stop/partial owner behavior.
Later HomePreparation.WizardBoundary failed12:14:18.407 at original protected-SSH
Close assertion. Dedicated initial snapshot5, setup receipt7 and later Home/Admin/
portable/Python relay suites NOT_RUN after this point. ZIP/package NOT_RUN/uploads
SKIPPED; overall619 FAIL, not full SUCCESS.

Confirmed test sequencing defect: second Next immediately followed by Close while
new cached refresh/ReadSetupPending is asynchronous. That tests cancelling a local
read rather than the intended actual held command; old compound assert did not emit
individual flags, so their historical values are not invented. Root narrow fix
`5387ddeb498d8bf3f51189447f26b9e1e03af315`,1file19add6delete: wait actual snapshot
readiness, then actual command callback (bounded5s) before Close; preserve original
close-refusal/status criterion and assert real command entered. Finally releases
pending response, joins exact retry and refresh, observes typed outcome. No product
change, global timeout increase, gate removal or same-SHA blind retry. Syntax/C#5/
manual phase/cleanup/diff PASS, new compiler/native NOT_RUN. Next isolated checking
from exact619 local5c5ac7e +only this fix/latest3 evidence docs; all export/Admin code
excluded. I58–80/Y22 exact617 partial evidence/package, delta0. History below.

## CI619 actual running — exact diagnostic publication verified

PR88 OPEN/DRAFT source `f682432f4afd53bc7b3e7c9142d60dcb42a6a22e`, merge
`5d615896c349051606e6212ed119b3a2cb8ce356`, tree7da67502463739fd3a427ebf7ebddc629679858c
= local5c5ac7e05da8fde49c66313c6c1779045d478f7a. Guarded c122→f682 CAS PASS;
actual PR/ref/merge independently checked. One automatic [CI619/39773542091](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39773542091),
Windows119806729629/Ubuntu119806729392 in_progress at verification. Initial empty
webhook lookup then one run; no manual duplicate. PR body updated.4 paths300add77:
only readiness diagnostic test19add8 +3 docs; public uploadsOFF/workflows unchanged.
New Windows compiler/native NOT_RUN pending exact completed log. Old618 partial
Desktop/IO success does not cover this SHA or its unexecuted Home modal routes.

Next inspect same619; independent exact-owned Admin cleanup retention foundation
before real worker/owner-dispatch binding. Local task trackinge4 remains unconnected
and NOT_RUN, as do all export/Admin future code. Last full617 I58–80/Y22/raw21.75,
no score growth. Do not push checkpoint-only docs while CI active. History below.

## Diagnostic source object f682 — ref-CAS not yet attempted at checkpoint

Source `f682432f4afd53bc7b3e7c9142d60dcb42a6a22e`, parentc122, full canonical tree
`7da67502463739fd3a427ebf7ebddc629679858c` = local5c5ac7e05da8fde49c66313c6c1779045d478f7a.
4 paths300add77delete: test-only readiness diagnostic19add8 +3 evidence docs.
Fullscope/manual/secretsPII0/public320/0/syntax/C#5/JSON86/fulltree PASS. Fresh public
repo/headc122/base4bb verified, workflows unchanged/uploadsOFF/release not triggered.
New compiler/native NOT_RUN. Ref-CAS NOT_RUN here: recover actual PR/run after any
interruption before retry. Next expectedc122→f682 forcefalse once, automatic exactCI.
No profile export/Admin code included; I58–80/Y22 same617 partial evidence.

## Frozen shutdown diagnostic — one source file on exact618 tree

Isolated source `f4659691b65b2ca957d9473b8cb75ff26069d6a1` in ProGo-ci618-diagnostic
on34aa/c122; only tests/ShutdownTests.cs19add8delete. Root original diagnostic
0292411e9be5608980797be15a5683f5acb47615 +d919cc5ab998c655ce81bf520a505656a7431664.
No production behavior change, readiness predicates/12s limit unchanged; new safe
boolean/listener state only. Full delta source/manual diff/public320/0 PASS; new
Windows compiler/native NOT_RUN. All local export/admin extensions excluded. Next
latest3 evidence docs, full secret/PII/static/canonical-tree check, guarded c122→new
source CAS once.618 failure and617 last full SUCCESS remain separate. I58–80/Y22
unchanged. This pre-publication freeze is history once actual ref/run is verified.

## CI618 FAIL — shutdown startup predicate unknown; narrow diagnostic next

Exact published source `c12200870264832f6ad6d7a325ef19678ee1447e` /merge
`dbed93e150be5eeafb274e88826389aacff78d0a`, run39769965390 Windows119794736280 FAIL,
Ubuntu119794735960 SUCCESS. Completed log checked: build identity11:21:49.067,
Desktop5287 PASS11:40:12.906, Instance28/Maintenance126 PASS. Four actual direct/agent
startup/recovery child-lifetime process I/O collections PASS11:38:55–11:39:02,
with latency/preparation/CPU/RAM and six counters; not full-app/physical-disk or
owner-PC performance acceptance. Full Test-ProGo FAIL at ShutdownTests11:43:44.509:
`Timed out: external-edit fixture fully applied`, third external startup after
attempts1/2 passed. Existing12s bound. Last startup logged no CLI/Windows completion;
individual alive/journal/windows/environment/logged/listener flags were not emitted,
so exact underlying cause is UNKNOWN, not a proven product regression or flake.
HomeVPN wizard new11+5 modal cases and all later suites NOT_RUN because execution
stopped before their stage. Package NOT_RUN; uploads SKIPPED. No618 SUCCESS claimed.

Local narrow diagnostic `0292411` plus scope-preserving probe follow-up
`d919cc5ab998c655ce81bf520a505656a7431664` adds only individual boolean readiness and
owned SOCKS listener Task/error-type facts on timeout. All existing criteria/12s
budget and original guarded port probe retained; no rerun of same SHA, no fallback
PASS. Next isolated checking from34aa/c122, these test edits +latest evidence docs
only, excluding e4 Admin foundation and all profile export/admin helpers. Recheck
scope/secrets/workflows/fulltree, publish once then inspect exact newCI. Windows
compiler/native for this new diagnostic NOT_RUN. CI618 failure preserved below.
I58–80/Y22 exact617 partial evidence unchanged, delta0; no new full weighted gate.

## CI618 actual running — wizard/IO candidate published

PR88 OPEN/DRAFT source `c12200870264832f6ad6d7a325ef19678ee1447e`, merge
`dbed93e150be5eeafb274e88826389aacff78d0a`, tree205e6e6e5583d669e03664ea44eafa9749c2a829
= local34aa821d1e86fc86c5d381a6bad337d28ef76068. CAS expectedf255→c122 PASS,
один автоматический [CI618/39769965390](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39769965390),
Windows119794736280 in_progress/Ubuntu119794735960 SUCCESS atverification. Initial
empty webhook lookup then one run, no manual duplicate. PR body updated.13 paths
957add182delete,10 source/test477add68 +3docs; fullscope/secretsPII0/syntax10/C#5/
public320/0/JSON86/canonicaltree PASS, workflows/VERSION unchanged/uploadsOFF.
New compiler/native NOT_RUN pending exact Windows log, no inherited617 result.
Later profile export/admin snapshot/UI-transport code excluded, their Windows NOT_RUN.

Last full617 sourcef255/merge7f720 remains evidence/package/28partial weighted criteria,
I58–80/Y22/raw21.75/delta0. Next same618 exact result; do not change active head for
checkpoint-only docs. Independently add service tracking/drain of accepted Admin
Tasks before actual static AdminAsync worker migration, then retained-cleanup routing.
Do not claim global task accounting already connected to the user route. History
below; object-before-CAS entries are not current incomplete publication.

## Wizard checking rebuilt on617 — code89e static-only; F31 anchor corrected

Runtime/test source `89e44d5ea0c9226a0b1444921919049be8043631`, tree3200f55fcf5202d269ff9f226f70dc09b299d307
in ProGo-ci617-wizard, on verified617 localf412/sourcef255.10 paths477add68delete:
3 wizard/context/card +7 tests, async import/receipt, initial cached setup snapshot,
first-success owner restore preserving edits,11 import/receipt+5 initial modal routes,
actual owned SSH child I/O counters4. Only Run call-order cherry conflict resolved;
receipt diagnostic, own3file restoration and terminal NativeRecovery-last preserved.
Service/access/recovery source, scripts/server/workflows/VERSION unchanged. All later
profile export/native profile chooser/admin snapshot/transport helpers excluded.
Manual scoped diff/call-site/conflict/receipt-parity/syntax10/C#5/public320/0/secretPII0
PASS; compiler/native NOT_RUN. Docs still predecessor in worktree until final freeze.

Evidence correction without points/weight change: current F31 representative score
anchor had unrelated `Update check tests PASS:72`. Replaced with exact617 native
`real recovery repeats local signing and actual server authentication against the
same selected owned agent key` (same completed log), matching original F31 table's
signing/UI/SSH basis. Historical entry retained in JSON correction record. Underlying
partial gate, v1.0 weights/I58–80/Y22 raw21.75 unchanged; written metrics not credited.
Next attach latest3 evidence docs, whole delta scan/public workflow/canonicaltree/CAS,
one exact newCI.617 fullSUCCESS applies only to f255/7f720. History below.

## CI617 SUCCESS — exact import/receipt API cohort, future UI excluded

Exact source `f255e8ad7339af4481e0590c31c7db32ab959bfe`, merge/build identity
`7f72092c47f68617f9fec6bf1b7c2dc273c9f426`, tree323f022e49ac2b485e6d9c32f528ffd0d6822a98
= localf41257f78ba1e8fb4166257c2db9012be16a0fd7.
[CI617/39763459020](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39763459020)
Windows119773047477/Ubuntu119773046886 SUCCESS; completed jobs/log checked. Build
10:29:34.0880510Z, full Test-ProGo PASS10:47:50.1596462Z, Desktop5283/Home1805/
Portable91, Python28=26PASS2SKIP. Pair9/import9 PASS and all7 real receipt worker cases
PASS10:51:26.400–27.174Z: complete/cancel-read/cancel-consume/scope-refused/real delete
lock/changed-request/changed-credentials. Terminal recovery after these also PASS,
confirming corrected fixture ordering/state restoration without a timeout increase.
Full configure initial/readmodal3+3/private-save8, diagnostics modal/chooser3/Context
ownership, selected-key signing55/UI50/prep72, actual direct+owned-agent native SSH→
SOCKS→HTTP/crash→fresh preparation, Instance28/Maintenance126/Shutdown47 also PASS.

Same28 weighted criterion anchors match613 verbatim: I58–80/Y22 raw21.75, delta0.
This validates the published API cohort, NOT future wizard a501/839/a0/bbcc/d940,
profile export6d180/320a/1edef/4c1/0b5, process I/O metricsf9 or admin snapshotca3d/
transport819d9+760. Their compiler/native remain NOT_RUN. Published wizard still uses
sync compatibility importer; full R09 FAIL and R11 PARTIAL, owner/installer gates open.
Next before isolated wizard publication: fix first-success setup snapshot retry
restoring stale saved-owner endpoint for a different pending VPS, preserving edits;
then rebuild on checked617 with diagnostics/order fix retained and review scope.

Runner ZIP0.2.2 SHA256 `91f617b9b9d09ed460aab74c05e12fde6b267994642c2cae77094bc46c6b17d7`.
All upload/prune steps SKIPPED; bytes not retrieved/delivered/installed. Private
transfer BLOCKED pending approved channel; no main merge/release/owner-PC install.
History below; previous running/object states are historical, do not repeat617.

## CI617 actual running — fixture order/state fix only

PR88 OPEN/DRAFT source `f255e8ad7339af4481e0590c31c7db32ab959bfe`, merge
`7f72092c47f68617f9fec6bf1b7c2dc273c9f426`, tree323f022e49ac2b485e6d9c32f528ffd0d6822a98
= localf41257f78ba1e8fb4166257c2db9012be16a0fd7. CAS expected5ee→f255 PASS,
один автоматический [CI617/39763459020](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39763459020),
Windows119773047477/Ubuntu119773046886 in_progress. Empty initial webhook lookup
followed by one run; no manual duplicate. PR body updated.4 paths343add47delete,
only1 fixture11add1delete+3docs, product runtime/scripts/workflow/VERSION unchanged,
fullscan/canonicaltree PASS/public uploadsOFF. New compilation/native NOT_RUN until
exact log. Future wizardd940/export/metricsf9/adminsnapshotca3d excluded and NOT_RUN.
Last fullSUCCESS613/28anchors/runnerZIP remain current evidence; I58–80/Y22/delta0.
Next check same617; do not publish checkpoint-only docs on active head. Independent
next stage: task-preserving UI transport adapter using existing owner dispatcher,
then wire Admin snapshot/staging without moving modal windows onto worker threads.
Historical pending-CAS notes below are not a new unfinished publication.

## CI616 FAIL — receipt worker held correctly, UI timer did not tick

Exact source `5ee7f1eb2545abef5ba03a1ef2f0e56253f55eaf`, merge/build identity
`e9b8b681b07d1e480bc309303fa52f8508425694`, treeb26fca3af636009cb33fca50d5ec561afc546b34.
[CI616/39758749192](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39758749192):
Windows119757324701 FAIL, Ubuntu119757325146 SUCCESS, completed jobs/log checked.
Build09:52:36.6364113Z; Desktop5283/full Test-ProGo PASS10:12:15.5275318Z. Previous
pair9/import9 and regressions reached. New receipt preconditions10:16:07.1234579Z:
access=True/owner=True/request=True. Failure10:16:10.1283004Z:
`mode=complete; stage=2; entered=True; ticks=0; status=Running`. Actual worker passed
private reads/RequireSavedImport and is held at its intentional consume barrier;
missing condition is native heartbeat, not evidence of a product I/O deadlock.
Remaining receipt7/Home tail/Portable/Python tail NOT_RUN, package/uploads SKIPPED.
Last fullSUCCESS/scoring/package613 only; no616 credit.

Confirmed fixture-order defect: tests/HomeVpnSetupRecoveryTests.cs::Run appended
OwnedConsumption after NativeRecovery; NativeRecovery starts Application.Run and
calls loop.ExitThread. tests/HomeVpnWizardTests.cs::Main already documents that this
terminal recovery must run after all modeless timer checks. New fixture violated
that lifetime contract. Fix `7f1d6f14c7a6ef3bff26f9ff83eeaacfe7b63170`, isolated
`cf4dbce3f78b7d39b7cb87c7c1ad00d6e046af96` on checking9829:1 test path11add1delete.
All timer fixtures now precede terminal NativeRecovery. OwnedConsumption wrapper
also restores its own access/owner/request bytes before subsequent scenarios: its
last changed-credentials case otherwise intentionally leaves invalid access/pending
request and would pollute reordered recovery. Original seven-case body byte-identical,
3s/5s budgets and every assertion unchanged. Static diff/syntax/public321/0(root)/
secretPII0 PASS; new Windows/native NOT_RUN. Exact new CI must confirm causal repair;
no claim old615 individual flags were recorded or exclusive OS mechanism measured.

Next isolate only this test +3 current evidence docs, runtime/scripts/workflows/
VERSION unchanged, public uploadsOFF, canonicaltree/CAS on5ee; one new exactCI,
not a rerun616. Future wizardd940/export/metricsf9/adminsnapshotca3d excluded and
NOT_RUN. After full success rebuild wizard on the latest checked diagnostic/order
base; preserve this terminal ordering when adding future native fixtures. FullR09
FAIL/R11 PARTIAL, I58–80/Y22 exact613 unchanged. History below.

## CI616 actual running — receipt-only diagnostic published

PR88 OPEN/DRAFT source `5ee7f1eb2545abef5ba03a1ef2f0e56253f55eaf`, merge
`e9b8b681b07d1e480bc309303fa52f8508425694`, treeb26fca3af636009cb33fca50d5ec561afc546b34
= local9829d870db750c2e32ebd11c60aefb8aac21844a. CAS expected6f8→5ee PASS,
один автоматический [CI616/39758749192](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39758749192),
Windows119757324701/Ubuntu119757325146 in_progress. PR body обновлён.4 paths493add31delete,
only1 receipt test16add2 +3docs, runtime/scripts/workflows/VERSION unchanged,
fullscan/tree equality PASS/public uploadsOFF. New compilation/native NOT_RUN;
615 cause UNKNOWN pending stage/tick/task/input flags. Do not rerun615 or duplicate616.
Future wizard d940 and all profile export source excluded; their native NOT_RUN.
Last fullSUCCESS613/28anchors/runner ZIP remain current evidence, I58–80/Y22/delta0.
Next same616 exact result and minimal causal fix. Independently F31-R11 native
resource report lacks process I/O counters; inspect existing owned SSH metrics scope
and add only that missing test evidence if supported, no product concept change.
Historical object-before-CAS notes below are not current unfinished publication.

## CI615 FAIL — setup consumption heartbeat ambiguity; no blind rerun

Exact source `6f8b200cd34910ee167b44e6dab661c86cb506aa`, merge/build identity
`2c7db226b9705367574ca6806b550e6e40b59cd0`, treec3efc70354edb1b9b880af6e78d29153a78a9b4c.
[CI615/39753790118](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753790118)
Windows119740579385 FAIL, Ubuntu119740579716 SUCCESS, completed jobs/log checked.
Build09:14:05.8795949Z; full Test-ProGo PASS09:36:51.8168291Z/Desktop5283. Import pair9
PASS09:37:14.265–15.333Z, service import9 PASS09:37:15.555–16.929Z incl real partial
save/cleanup retry/stale access/acceptedStopDispose. Previous configure/UI/key/direct+
agent regressions reached. At09:41:09.246Z first new receipt `complete` returns owned
task;09:41:12.256Z `Setup consumption heartbeat deadline`. Its loop ignores early
operation completion and prints no stage/timer/task state: actual reason UNKNOWN,
not yet a confirmed product deadlock or timer defect. Existing3s deadline remains.
Receipt7 criteria not passed; Home suite tail/Portable/Python tail NOT_RUN, package/
uploads SKIPPED. Last fullSUCCESS/scoring/package remains613, no615 credit.

Next narrow test correction: assert exact real saved request/owner/token preconditions,
record read/consume stage and detect unexpected task completion before held barrier;
on timeout print safe booleans/counters/type only. Do not print credentials/paths or
increase timeouts. Isolate on615 checking63dae, exclude wizard d940 and profile export
work; one new diagnostic SHA/CI is justified by new evidence, not same-run retry.

Independent native profile chooser fixtures source `0b5ca87f65ed20b687864d36ef519874e191bf66`:
4 paths116add1delete (new DesktopProfileNativeChooserTests +entry/list/call). Uses real
production profile button, outer ShowDialog and nested native SaveFileDialog; existing
PID/thread/root-owner helper unmodified. Owned child/job20s, native5s; positiveCancel
must return0, real missing window fails; deliberately unmatched caption/error fixtures
must return20/21 with exact failure and cleanup marker. Cleanup checks native window
absent before owner disposal, joins retained worker/service tasks and unchanged private
input snapshot, closes only its own windows/processes. Clipboard unused, settings not
touched. Static syntax3/diff/public321/0/secretPII0/explicit compile list PASS;
PowerShell execution/Windows compile/native NOT_RUN. Excluded615/wizardd940. FullR09
FAIL/R11 PARTIAL, I58–80/Y22 exact613 unchanged. History below.

## CI615 running — exact narrow compile fix, future wizard separate

PR88 head `6f8b200cd34910ee167b44e6dab661c86cb506aa`, merge
`2c7db226b9705367574ca6806b550e6e40b59cd0`, treec3efc70354edb1b9b880af6e78d29153a78a9b4c
= local63dae01967394a25ad735b17353bd50cddeb67d8. GuardedCAS0bfa→6f8 PASS, один
автоматический [CI615/39753790118](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753790118),
Windows119740579385/Ubuntu119740579716 in_progress. Первоначально пустой webhook lookup
затем один run; ручного дубля нет.4 paths149add83delete (source call1/1 +3docs),
full secretsPII0/tree equality PASS, workflows/VERSION unchanged/public uploadsOFF.
PR body обновлён. Новая compilation/native NOT_RUN до exact completed log;614FAIL
сохранён. Last fullSUCCESS613 — только его SHA/ZIP/28anchors, I58–80/Y22/delta0.

Независимый local `bbcc3356835ff08df01199990b1d67d7313ab757` (1 test path10add4delete)
адаптирует будущие modal import Dispose assertions к подтверждённому613 native-unwind
интервалу: перед release worker требуется завершение ShowDialog unwind, закрытый
OwnerUi publication gate и всё ещё удерживаемый импорт; после actual I/O нулевой
прирост control-events относительно измеренной native baseline. Все параметры
выводятся в assertion. Никакой проверки не удалено/таймаут не увеличен. Ожидается
также завершение настоящего setup snapshot перед проверкой повторного UI-действия
и при очистке. Manual diff/syntax/public320/0 PASS; compilation/native NOT_RUN,
из615 исключено вместе с wizard a501/839/a0e3413.

Далее: подготовить отдельный wizard UI checking с captured async import/snapshot
и11+4 реальными modal fixtures на последней проверенной API базе после615. Проверять
тот же615, не менять его head checkpoint-only публикациями. Полный R09 FAIL/R11
PARTIAL, owner/final installer/delivery BLOCKED или NOT_RUN по прежним критериям.
Ниже история, pending-CAS записи не означают незавершённую фактическую публикацию.

## CI614 завершён FAIL — narrow callback argument correction saved

[CI614/39753293285](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753293285)
завершён: Windows119738915213 FAIL CS1501, Ubuntu119738914876 SUCCESS. Source
`0bfa2f97c3cb627e7bc6a7b1053260084a8f28c8`, merge4b25e365a50779395f0a4c99576f7a8041258b61.
Compilation FAIL, full Test-ProGo/native NOT_RUN, pack/uploads SKIPPED; старый
SUCCESS613 не распространяется на этот API. Повтор614 не запускался.

Исправление `bc9abc24ebc2b52af6e7a4205ae5da8d60002809`: явный четвёртый null beforeRead
в вызове LoadShareAsync для импорта, без изменения сигнатуры/поведения. Изолированная
копия `102efb03d869ce5d8ee37b4ffbc3af9a7a4780bc` на locala2b6422 (exact614). Один путь1add1delete,
manual diff/AST syntax +оба вызова с4 аргументами/public320/0 PASS. Это не компиляция:
новый Windows compile/native NOT_RUN. Следующий кандидат — только эта правка плюс
3 evidence docs на том же API; wizard a501/839/a0e3413 исключён. Перед публикацией
повторить full delta scan/workflow/tree/CAS, один новый CI обоснован конкретным fix.
I58–80/Y22 exact613, delta0. Ниже история.

## CI614 actual publication — Windows compilation FAIL, native NOT_RUN

CAS de7→`0bfa2f97c3cb627e7bc6a7b1053260084a8f28c8` PASS; PR88 OPEN/DRAFT,
merge `4b25e365a50779395f0a4c99576f7a8041258b61`, treecfd761a43bfe4e5b736c15460dfd4df8f48342e7
= locala2b6422bed9d0955e612ca1385db2a71994ee63c. Один автоматический
[CI614/39753293285](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39753293285),
Windows119738915213 FAIL, Ubuntu119738914876 пока in_progress. Windows completed log:
09:10:12.8112991Z src/HomeVpnService.cs405 CS1501 — LoadShareAsync вызван с3 аргументами,
объявленная сигнатура91 требует4 (beforeRead). Build failed; full Test-ProGo/native
NOT_RUN, pack/uploads SKIPPED. Причина определена; таймауты/проверки не меняются.
Следующая микрозадача — передать явный null callback только в этом месте, проверить
все вызовы сигнатуры и новый exact SHA. Не повторять тот же614/ошибочный source.
Последний полный SUCCESS613 source de7/merge5262; I58–80/Y22 unchanged. Actual wizard
и snapshot sourcea0e3413 исключены614 и Windows NOT_RUN. Ниже история до/после CAS.

## CI613 SUCCESS — exact configure UI/owned persistence evidence

Точный source `de7b42628ccd656f369ba0e286e9b406cc20dee6`, merge/build identity
`5262c806c06ba715d61bf3e070b52fbc31b3f2ab`, treebc5d3f70d2ff0833692d91d75eb62d108b33b215
= localbcb699bb9f3641d0d5a870836fd233e155748214. [CI613/39749169861](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39749169861):
Windows119725094348 и Ubuntu119725094777 SUCCESS, проверены jobs и completed Windows
log. Build identity08:38:06.6443827Z; full Test-ProGo PASS08:59:56.1352563Z,
Desktop5283/Home1619/Portable91, Python28=26PASS2SKIP. Native initial configure ready/
Close/Dispose3 и прежний modal read Cancel→Retry/Close/Dispose3 PASS. Actual configure
private-save/recovery8 PASS09:03:42.751–45.129Z: verify-save/close/dispose/accepted-close/
error-retry; recover-save/close/accepted-close. Реальные DPAPI, локальный HTTP и
защищённые файлы; fixture recovery transport не подтверждает работу реального VPS.

Dispose09:00:15.0283133Z: initialized=False/disposed=True/modal=False/controlEvents=2/
nativeDisposeEvents=2/publications=0/disposedAt=0. На613 наблюдались два native unwind
события, ни одной принятой публикации продукта послеDispose и нулевой прирост событий
после physical I/O. Это подтверждает измеренный интервал613; индивидуальные значения
старого612 не зарегистрированы, его failure сохранён как история. Будущий wizard
observer839 ещё требует отдельного аналогичного измерения, SUCCESS не наследуется.
Selected-key signing55/UI50/preparation72, реальная direct+owned-agent SSH→SOCKS→HTTP
и crash→fresh preparation, diagnostics modal/chooser/Context ownership, Instance28/
Maintenance126/Shutdown47 также PASS в полном прогоне.28 score-anchor критериев
совпали дословно с611: I58–80/Y22 raw21.75, delta0, веса неизменны.

Runner ZIP0.2.2 SHA256 `242078d847fb3ac8dcae27f1c739ae507cb4e6e302fb5d901e46733c1b049879`.
Все upload/prune шаги SKIPPED; bytes не получены/не переданы/не установлены. Приватная
передача BLOCKED до согласования канала владельцем. Main merge/release NOT_RUN.
Полный R09 FAIL/R11 PARTIAL, target/installer NOT_RUN. ImportAPI560/7bb/4a/fc,
wizard a501/839/a0e3413 исключены из613: compilation/native NOT_RUN. Далее изолированный
API-кандидат39e05f19b02f2947fa3fd7beff0e0b64f27ffeb2 (5 runtime/test paths444add32delete)
на613 disposal base; финальные scan/workflow/tree/CAS и один новый CI. Ниже история.

## Восстановление09:04Z и локальный wizard setup snapshot

PR88 OPEN/DRAFT, source `de7b42628ccd656f369ba0e286e9b406cc20dee6`, merge
`5262c806c06ba715d61bf3e070b52fbc31b3f2ab`; единственный CI613/39749169861
in_progress: Windows119725094348 build PASS, integration step running;
Ubuntu119725094777 SUCCESS. Completed Windows log ещё недоступен: полный
Test-ProGo/native результат NOT_RUN до его проверки. Публикация direct+agent после
589 состоялась ранее (590 FAIL, исправления595 SUCCESS); повторный push/CI не
создавался. Других активных исполнителей/локальных build/git/SSH процессов не
найдено. Все worktree и изменения сохранены.

Локальный source `a0e34136ca5cce469e39a2a870542c090f40ea99`:7 путей210add49delete.
Constructor мастера удерживает собственные CTS/TCS/driver до фактического окончания
worker PendingOwnerAsync после service initialization. Поля и RefreshStatus читают
только snapshot; смена страницы/завершение операции обновляет его без чтения на
каждое нажатие. Начальная отмена сохраняет окно, явный Close ждёт собственное I/O,
Dispose запрещает позднюю публикацию. Фикстуры ожидают реальную инициализацию без
синхронного тестового обхода. Четыре новых ShowDialog-сценария: ready, Cancel→retry
через реальный выбор своего VPS, Close, Dispose;15s окно/5s worker barrier,
ограниченная очистка удерживаемых задач и ресурсов. Ready/retry дополнительно
редактируют поля и обновляют статус при реальной эксклюзивной блокировке request
file: требуется отсутствие повторного чтения/замены нового ввода.

Manual diff/call-site review, syntax7/C#5 await-scope/diff/public320/0/secretPII0
PASS. Это статические проверки, не компиляция. Windows compilation/native всех
этих новых изменений NOT_RUN, в CI613 они исключены. Local importAPI99ad и wizard
a501/839 также не получают SUCCESS613. Полный R09 остаётся FAIL, R11 PARTIAL.
I58–80/Y22 raw21.75 exact611, delta0: код и невыполненный тест баллы не добавляют.

Далее проверить тот же613. После результата изолировать importAPI560/7bb/4a/fc
на последней проверенной базе с disposal79, исключив дальнейший wizard-код;
финальный diff/секреты/workflow/tree перед единственным новым CI. До проверки613
не менять опубликованный head. Будущий import-modal Dispose observer требует
отдельной проверки native-unwind интервала по фактическим значениям613. История ниже.

## CI613 running — narrow disposal diagnostic source, future import excluded

PR88 head independently verified `de7b42628ccd656f369ba0e286e9b406cc20dee6`, merge
`5262c806c06ba715d61bf3e070b52fbc31b3f2ab`, full treebc5d3f70d2ff0833692d91d75eb62d108b33b215
= localbcb699bb9f3641d0d5a870836fd233e155748214. One automatic
[CI613/39749169861](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39749169861),
Windows119725094348/Ubuntu119725094777 in_progress at verification. CAS expected
17c64→de7 PASS, empty initial webhook lookup followed by single run, no manual
retry.7 paths342add60delete:4 diagnostic source/test paths +3 docs, full scan/tree
PASS, workflows/VERSION unchanged/public uploads OFF. New compilation/native
NOT_RUN. Dispose failure cause remains hypothesis until detailed flags/interval
are observed; do not label narrowed measurement successful before actual result.

ImportAPI99ad/wizard a501+839 remain excluded and NOT_RUN. Next independently
move wizard initial PendingOwner/status probes to owned cached worker state and
adapt actual native lifecycle fixtures; source writer/format unchanged. Do not
change the checked613 head for checkpoint-only commits. Last full SUCCESS611 and
its28 score anchors/ZIP remain the only current readiness evidence: I58–80/Y22
raw21.75, delta0. Full R09 FAIL/R11 PARTIAL; owner/installer/delivery still open.
Ниже object-before-CAS record — история, не повторять публикацию.

## CI612 FAIL — dispose compound assertion, detailed ownership proof pending

Exact source `17c64b6276567f1b2f2c090ef8130b1c064aa45c`, merge/build identity
`27a8d83cd6a1cb704ab36269eabd3406abd402f4`, treecf0eb0f428029c314dcf191fc0e2df1aa24eebcb.
[CI612/39744824752](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39744824752)
Ubuntu119710569884 SUCCESS, Windows119710569658 FAIL at08:25:35.5384376Z:
`configure read publishes only to its live owner; Close waits for physical settlement
and Dispose gets no late controls: dispose`. Compilation and full Test-ProGo PASS
08:25:16.3862166Z/Desktop5283; read-modal old3 and new initial ready/Close cases PASS.
The compound Dispose test gives no individual values; exact failed subcondition
not established. Remaining new modal persistence/recovery8, Home suite tail,
Portable and Python tail NOT_RUN; pack/public release-content steps SKIPPED, all
uploads SKIPPED. Last full SUCCESS/scoring/package still611 only, no new credit.

Hypothesis (not confirmed): controls emit lifecycle events when native ShowDialog
finally unwinds after direct Dispose, before the held private worker is released.
Microsoft Form.Close/ShowDialog contract already distinguishes modal disposal;
ReferenceSource Form/Control full pages were unavailable (DisabledError), only
search snippets were accessible, so no claim they were fully read. Next exact
runtime measurement must establish the event interval instead of assuming cause.

Narrow local diagnostic correction `79fcb6b419935f90e4bf5d3a4f0f26a2ff5217cc`: a
memory-only dispatcher PublicationCount exposed to its configure fixture proves
zero product callback execution after Dispose across the full interval. Existing
control-change observer additionally requires zero delta after real modal unwind
and physical-I/O release; baseline captured while actual work is still held and
publications cannot change. Detailed initialized/disposed/modal/event/callback
values accompany the assertion. Both initial-read and save Dispose cases use the
same proof;15s/5s budgets and owned cleanup unchanged.4 files24add9delete source/
syntax/public320/0/secretPII0 PASS; compilation/native NOT_RUN. No claim fixed until
new exact CI; no612 rerun or success inherited. Publish isolated correction+docs,
exclude all future import and wizard work.

Independent `83945c64026a5eef21637830aa711ff675535089` adds11 actual ShowDialog
import/receipt cases: cancel/different-token retry, explicit Close/Dispose, accepted
Cancel/Close/Dispose/global Stop, actual partial-owner-save→Retry, receipt Cancel/
Close/global Stop. Real DPAPI and fixed private bytes; successful retry uses the
existing owned local SSH substitute (not live VPS/native OpenSSH acceptance).
15s modal/5s barriers and joined service/window cleanup; no new external setup or
late SSH after Stop expected. One file99add2delete source/syntax/public/secretPII0
PASS, compilation/native NOT_RUN; actual wizard sourcea501 also untested. Future
wizard Dispose event observer will adopt this measured interval separately if
validated; not included in isolated correction. Constructor/status PendingOwner
still sync, full R09 FAIL. I58–80/Y22 exact611 unchanged. Ниже история.

## CI612 running — actual configure UI published, import still separate

PR88 actual source `17c64b6276567f1b2f2c090ef8130b1c064aa45c`, merge
`27a8d83cd6a1cb704ab36269eabd3406abd402f4`, full treecf0eb0f428029c314dcf191fc0e2df1aa24eebcb
= local023bf0779010f22b6a89211668f0c1e5835053e7. Single automatic
[CI612/39744824752](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39744824752)
Windows119710569658/Ubuntu119710569884 in_progress at verification. Initial webhook
lookup empty, then unique run observed; no manual duplicate. CAS expectedd896→17c64
PASS. Source8 frozen UI paths +3 evidence docs,11 paths849add150delete, full scan/
syntax/C#5/diff/JSON/tree PASS. Workflows/VERSION unchanged/public uploads OFF.
New actual UI compilation/native NOT_RUN pending exact logs; no inherited611
success. Last full SUCCESS611 only sourced896/mergef0bf, same28 score anchors.

Import API99ad (Stop/pair/service/setup worker) excluded from612. Next independently:
actual wizard await UseTokenAsync/ConfirmConsumedAsync with captured input/scope,
modal Cancel/close/accepted/partial/retry outcomes and context loading gates. Setup
constructor/text-change pending probes still sync and remain a later separate
worker migration; do not declare full R09 finished after importing alone. Source
head/doc-only pushes must wait for same612 completion, preserve checking source.
I58–80/Y22 raw21.75 exact611 unchanged; target/installer/private delivery blocked.
Ниже object-before-CAS checkpoint — история, не повторять публикацию.

## CI611 full SUCCESS — share API, actual UI migration still excluded

[CI611/39740277423](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39740277423)
Windows119695558302/Ubuntu119695558139 SUCCESS. Source
`d89673c27aa8ce72ffc5e13eb8e5dc849398ea5a`, merge/build identity0.2.2+
`f0bf91f6e0a15b5e01e4ecd7f274096565cd1cc7`, tree7da84ab401825c7483d62770ad73114003d57a9a
= localee240bcc5abd69f48dbd0be73283239da5d59b24. Native store9 home+9 share and
invalid-ID5/origin4 refusals, service-save7 home+9 share including stale server/
Access and cross-kind write exclusion PASS07:47:42–46.8 receipt worker cases PASS
07:51:29.407–30.706: actual protected save, live UI checks, cancel/dispose, real
replace/delete locks and changed request keep exact recovery. Actual configure
still uses legacy sync callback at this source; no inherited UI9faa/import success.

Full Desktop5281/HomeVPN1521/Portable91; diagnostics modal6/chooser3/Context ownership,
selected-key signing55/UI50/prep72 +actual direct/owned-agent OpenSSH→SOCKS→HTTP→crash/
fresh preparation, Instance28/Maintenance126/Shutdown47; Python28=26PASS2SKIP.
Runner ZIP0.2.2 SHA256 `321759f1a32fc1c8bfc7250b3fa482ed2687b22f22f91eb685c65ef7bbc9d021`.
Pack PASS, uploads SKIPPED all; bytes not retrieved/delivered/installed. Private
delivery BLOCKED until approved channel; installer/owner acceptance NOT_RUN.
Same28 score anchors exact611 with same criterion text/weight87/D0.5/P0.5/A0=>
21.75/display22, delta0. I58–80, full R09 FAIL/R11 PARTIAL; no target readiness claim.

Independent source `fc257e151856d7e9224c4fe6b1db75dc11b66749`: setup HasPending/
PendingOwner/ConfirmConsumed worker APIs, copied inputs and cancellation/memory
admission, rechecked same request/result and both actual saved credentials before
Delete. Seven native read/cancel/scope/real lock/new request/new credentials vectors
written,98add10delete in2 files; source/syntax/public320/0/secretPII0 PASS, native/
compilation NOT_RUN. Actual wizard still calls sync adapter and synchronous setup
constructor/status probes; these are explicitly remaining R09, not closed.

Frozen later import API: local `99ad0031e48a5e420d6149d2025a5345a6893bb9`, tree
8b8b432942c88a0582ffb30cd29d113dd6fa5803 in ProGo-import-api based UI9faa.5 source/
test paths444add32delete (Stop560,pair7bb,service4a,setupfc), exact root source
match; token/owner format unchanged, partial-save honest, no crash-atomicity claim.
All compilation/native NOT_RUN; later wizard changes excluded. Next publish only
frozen UI9faa+exact611 evidence docs after scan/tree, one CI; preserve import API
source separately while actual wizard/initial setup probes migrate. Ниже история.

## CI611 running — published frozen share API, UI/Stop excluded

PR88 actual head `d89673c27aa8ce72ffc5e13eb8e5dc849398ea5a`, merge
`f0bf91f6e0a15b5e01e4ecd7f274096565cd1cc7`, tree7da84ab401825c7483d62770ad73114003d57a9a
= localee240bcc5abd69f48dbd0be73283239da5d59b24. One automatic
[CI611/39740277423](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39740277423),
Windows119695558302 in_progress (build), Ubuntu119695558139 SUCCESS. Initial run
lookup was empty until webhook appeared; no duplicate manual workflow dispatched.
CAS expected04ca→d896 PASS, independent actual PR head/merge/run verified.
5 frozen API source/test paths +3 evidence docs, full tree/secretPII/public/diff/
syntax/JSON PASS; automatic binary uploads OFF, no release. Current compilation/
native NOT_RUN pending exact completed log. Last full SUCCESS610 only source04ca/
mergec506; its28 score anchors remain current evidence. Root UI9faa and Stop560
not published/tested by611. No source-head/document-only pushes while611 active.

Next independent microtask: staged access+owner persistence primitive under fixed
private destinations, immutable inputs, real DPAPI/file locks/cancel/partial-save/
owned cleanup tests. Preserve existing recovery contract and truthful partial write
outcomes; no new format, no crash-atomicity claim. Then retained import ownership
using split physical/public stop; actual wizard awaits import/receipt later.
I58–80/Y22 raw21.75 unchanged, no credit for pending611 or unexecuted local code.
Ниже object-before-CAS checkpoint — история, не повторять ref update.

## CI610 full SUCCESS — home-address/modal block accepted in isolated Windows

[CI610/39736682268](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39736682268)
Windows119683904424/Ubuntu119683904096 SUCCESS. Exact source
`04ca974519bfffdb70a1706066ebaef7ed26bf6b`, merge/build identity0.2.2+
`c506814d33c4ef39f5cb946c41121a70850ff851`, tree36c0e5cb4f0842d9273105b4ae9265e99d4da2e2
= localfa61c973a5c9878fb2154ada0d02b6d5fa618a09. Same run completed, no rerun.
New native: read-modal3 (actual Close order07:12:57.6950548Z), protected store9
07:12:58.162–59.165, service-owned save7 07:12:59.254–13:02, actual write-modal7
07:13:02.344–05.399. Cancel→Retry, explicit Close after physical I/O, Dispose/no
late controls, accepted save after Cancel, real file sharing error→Retry and
retained locked-stage cleanup→unlock→Stop→Retry PASS. Previous609 test defect fixed
without deadline increase or assertion removal. New source home write now compiled
and executed; later root share/init/configure/Stop changes remain NOT_RUN.

Full: Desktop5281/HomeVPN1325/Portable91; selected-key signing55/UI50/prep72,
diagnostics modal6/chooser3/Context ownership, Instance28/Maintenance126/Shutdown47,
actual direct+owned-agent OpenSSH→SOCKS→HTTP→crash→fresh preparation; Python28=
26PASS2SKIP. Native combined SSH measured direct startup1945ms/prep33ms/SSH CPU0ms/
working-set11382784B/peak11460608B, recovery1949ms/prep33,28ms/CPU31.25ms/working-set
11350016B/peak11423744B. These are isolated fixture observations, not full app I/O/
slow hardware/owner acceptance or final performance PASS. Program process GUI /
full ShowQr→real VPS / target Windows still NOT_RUN.

Runner ZIP0.2.2 SHA256 `c472e2b61e6f5f5b2def0c1ebb208b54e662d299380386496f63099a967513df`.
Pack PASS, every upload SKIPPED; bytes not retrieved/delivered/installed. Private
delivery BLOCKED until approved channel; final installer and target acceptance
NOT_RUN. Fixed v1.0 same28 criteria/text anchors verified exact610, weight87 with
D0.5/P0.5/A0 =>21.75/display22 unchanged. No extra credit for more native cases or
reusing criterion/parent-child weights. I58–80/Y22, delta0, R09 full FAIL/R11 PARTIAL.

Independent root `56084e8a5c1bbbdbaf4a72b8c4b1ea6593a2270f` separates public stop task
reservation/completion from retained physical StopChannelAsync. Existing start/read/
write gates now reject both stop owners; cancellation, actual cleanup and retry
body unchanged. This enables future import to stop channel without self-await,
then outer Stop can join import. One file25add7delete diff/manual review/syntax/
C#5/public320/0/secretPII0 PASS; existing native lifecycle regressions NOT_RUN.
No import code/schema changed, not in610 or frozen399/API or9faa/UI checking.
Next publish frozen399/API+exact610 evidence docs after scan/tree and one CI;
actual UI9faa and Stop560 remain independent and untested. Ниже — история.

## CI610 queued — isolated modal Close proof, source04ca

PR88 head independently verified `04ca974519bfffdb70a1706066ebaef7ed26bf6b`, merge
`c506814d33c4ef39f5cb946c41121a70850ff851`, full tree36c0e5cb4f0842d9273105b4ae9265e99d4da2e2
= localfa61c973a5c9878fb2154ada0d02b6d5fa618a09. One automatic
[CI610/39736682268](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39736682268),
queued at verification; job IDs not yet read. Expected-head CAS3474→04ca PASS.
Only one test file modal Close assertions +3 evidence docs published, secrets/
PII0/full tree PASS, workflows unchanged/public uploads OFF. New compilation/native
NOT_RUN; no duplicate609 retry. Last full SUCCESS608 remains scoring evidence,
609 Test-ProGo PASS followed by native Close-test FAIL remains historical partial.

Root share-address primitivea2fd/service02bc/receipt5610 source is NOT in610.
Next independently: actual configure UI owned pending-read/save/receipt route,
then bounded native modal acceptance/cancel tests; poll same610. I58–80/Y22
raw21.75 unchanged, full R09 FAIL/R11 PARTIAL, no binary delivery/owner acceptance.
Ниже checkpoint до CAS — история, не указание повторить публикацию.

## CI609 FAIL — modal test assumption isolated, new private-write vectors NOT_RUN

Exact source `34740802206b6bd24ec41838ba25c44980ddec6c`, merge
`0da565ba835dbf63fc2f402eafdac3f380f23624`, tree4752a5e04f2b38c4516d969f466bf163f10abee6.
[CI609/39733721723](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39733721723):
Ubuntu119674440425 SUCCESS; Windows119674440595 FAIL at06:42:52.7584245Z,
`explicit modal close/disposal waits for its actual read result: close`.
Compilation/build identity0.2.2+merge and complete scripts/Test-ProGo.ps1 PASS
(06:42:36.5499890Z): Desktop5281, signing55/UI50/prep72, diagnostics actual modal/
three chooser/Context ownership, Instance28/Maintenance126/Shutdown47. Later
Test-HomeVpnRelay native fails after actual read-modal Cancel→Retry PASS at
06:42:52.4008475Z. Read Close asserts passed physical cancellation/no late dialog/
source preservation, then incorrectly required IsDisposed. Read Dispose and new
9 store+7 owned-save+7 write-modal vectors were not reached: NOT_RUN. Python tail/
public release-content check/pack NOT_RUN (steps SKIPPED); all uploads SKIPPED.

Root cause: modal ShowDialog Close returns/hides a reusable form and requires
explicit fixture Dispose; it does not establish IsDisposed. Microsoft contract:
[Form.Close](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.form.close?view=netframework-4.8)
and [ShowDialog](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.form.showdialog?view=netframework-4.8).
Isolated fix source `d2512ed83577cb46cb4e10ca6c4d1f83ef00f376` (ProGo-ci609-fix,
root equivalent `784194e5b1fb14f338612303b9732f8126f1d8b2`) changes only18 lines
in tests/HomeVpnWaitingTests.cs. Close now requires actual FormClosed event,
worker-completed/busy-false at that event, and real ShowDialog return (Modal false,
not visible) before fixture Dispose. Explicit Dispose remains independently
asserted and physical worker joined. Both read and write Close expectations fixed;
no assertion disabled, deadline15s and cleanup preserved. Production code unchanged.
Local syntax/diff PASS; fixed assertion Windows/native NOT_RUN. No blind rerun of609:
new checking commit needed, including only this fix and current evidence docs.

Independent root source `5610af7ddec886956ff269543064bf54fb333850` adds worker pending
read and ConfirmOwnedAsync: receipt/proof/private consumption outside UI, two
memory-only owner checks around actual retained save, physical receipt recheck after
the final owner queue. Cancel/save failure/new receipt/closed owner keep the request;
accepted save followed by cancellation is reported as saved with pending receipt.
Eight native vectors written using real DPAPI, real delete/replace locks, changed
request, live UI heartbeat and bounded join/cleanup. Source/syntax/C#5/public320/0/
secretPII0 PASS; compiler/native NOT_RUN. API not yet used by actual configure UI.
Source a2fd/02bc/5610 remains excluded from isolated modal-fix checking. Next after
it: connect actual configure form to owned read/save/confirmation, preserve receipt
and Cancel/close outcomes. Whole R09 FAIL, R11 PARTIAL. I58–80/Y22 raw21.75 exact608,
change0. Last known ZIP remains exact608 only, no delivered/installed package.
Ниже checkpoints — история; running609 status superseded by this completed result.

## CI609 опубликован — новый адресный/modal блок проверяется

Публикация подтверждена фактическим head PR88: source
`34740802206b6bd24ec41838ba25c44980ddec6c`, merge
`0da565ba835dbf63fc2f402eafdac3f380f23624`, полный tree
`4752a5e04f2b38c4516d969f466bf163f10abee6` = isolated local
`968fe3d461f402a892ebf8662cf84c2eb59223e6` (ProGo-ci608-followups).
[CI609/39733721723](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39733721723)
единственный известный запуск для этого source: Ubuntu119674440425 SUCCESS,
Windows119674440595 in_progress (Repository hygiene and build). Compilation/
native нового source остаются NOT_RUN до чтения завершённого результата.
Проверенный ранее CI608 SUCCESS относится только к source479addfd/merge36f39fb0.

Перед CAS479a→3474 проверены все11 файлов:8 source/test +3 evidence docs,
secret/PII0, public320/0, source/C#5 structure, diff, JSON31+55 и полное совпадение
tree. Репозиторий public перепроверен; workflows/VERSION unchanged, автоматическая
загрузка бинарных артефактов OFF, release не запускается этим PR/branch push.
Никакого повторного push/CI: после обрыва сначала проверить этот же run.

Новый scope: staged protected home-address write, retained service ownership/
Stop/Dispose/commit admission, actual async wizard save и явный DialogResult.None
после CancelButton для трёх ожиданий. Написаны9 store +7 owned-save +7 write-modal
и3 read-modal cases с ShowDialog(), bounded deadline и owned cleanup. До результата
609 это не Windows PASS. Полный R09/R10/R11/owner/installer acceptance остаётся
открытым. Общий startup Program.Main/ShowQr→VPS и целевой ПК NOT_RUN.

Следующая независимая микрозадача: общий staged private-address primitive для
captured per-server HTTPS origin с безопасным фиксированным destination; сохранить
home-address контракты и добавить native cancellation/refusal/real-file-lock cases.
Далее — owned share persistence и прежний same-request receipt boundary. Изменения
не входят в609; не менять текущий проверяемый source ради checkpoint-документов.
Реализация58–80/100, проверенная готовность22/100 (raw21.75, exact608), изменение0.
Ниже — история, включая запись object до фактического CAS.

## CI608 full SUCCESS — live read routes, address/modal-save excluded

[CI608/39730132565](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39730132565)
Windows119663025746/Ubuntu119663025549 SUCCESS. Exact source
`479addfd6f711831a12e1c7b49019a446f7970ce`, merge/build identity
`36f39fb080e0195cad40922793b298a5da500914`, tree22821ad5fdf6f654deea6eb0ab293e629b817b79
= local8417f9a9b9d51cadeac8a29088a689d4b602c0f1. Program selects background API.
New narrow native: real configure action actual protected read/cancel/retry
06:08:26.982–27.238, queued close/Dispose/no late dialog06:08:27.369–744;
production background imported-data reload06:11:56.2206615 and memory share
getter with actual exclusive source lock06:11:56.2207408. Эти wizard parent cases
использовали Show(), не подтверждают реальную modal CancelButton семантику.
Новая explicit-None fix/ShowDialog regression882 в этот source не входят.

Full regressions: Desktop5280/HomeVPN1195/Portable91;51/456, report32/diagnostic
modal6/chooser3/Context ownership, selected-key signing55/UI50/prep72 и actual
direct/owned-agent OpenSSH→SOCKS→HTTP→crash→fresh preparation, Instance28/
Maintenance126/Shutdown47. Python28=26PASS2SKIP. Runner ZIP0.2.2 SHA256
`cc39eef38418287617e8bc2ddb9c175a7b0bbe8474ee477b428bebc4d2bb7ab2`.
All uploads SKIPPED; bytes не получены/не доставлены/не установлены; final setup/
owner acceptance NOT_RUN, delivery BLOCKED до согласованного приватного канала.

V1.0 I58–80, Y21.75→22 unchanged: same28 parents weight87, D0.5/P0.5/A0.
27 прежних anchors выполнены с тем же текстом; F22 copy/import reload теперь
имеет новый stronger background-constructor anchor того же partial критерия,
не другой вес/уровень и не дополнительный балл. Whole R09 FAIL/R11 PARTIAL.

Следующее source — frozen address draft `d6517e871854bb1d161bde55c6a3282a40339a01`
8 source/test paths553add52delete: primitive418/servicecf29/actual modal UI882,
9 store+7 owned-save+7 actual write-modal и3read-modal сценария написаны; compiler/
native NOT_RUN. Добавить только exact608 evidence docs, full scan/tree и один
expected479a publication/CI. Дальнейшие share persistence changes excluded;
узкий trace в PROJECT_STATE сохраняет прежние owner/request/receipt boundaries.
Не приписывать608 успех home-address writer, modal phone fix или всему R09.
No binary/release/main merge/owner PC/VPS actions. Ниже checkpoints — история.

## CI607 full SUCCESS — staged API/gates/refresh native подтверждены

[CI607/39727292233](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39727292233)
Windows119654028621/Ubuntu119654028836 SUCCESS. Exact source
`ab6a16fda8a80adb494bc7245af867c04487ac18`, merge/build identity
`73f972a9a09f3f56d6d120bd0625bc83273f935e`, tree9876155a50502a23e0c377d9e7c9507950f45a76
= local2fbe4d3c8b006136864a24b5911749c8e814148a. Новые scoped native PASS:
actual Context/tray/dashboard held read/no modal/action restore05:30:31.820–854;
owned initialization load/Stop/Dispose/Stop→Dispose05:37:21.410–793;
optional owner/home/share real lock warning +basic own process/local relay
start05:37:21.937–27.900; share cancel/stop/dispose/no late value05:37:28.196–511,
newer-save protection05:37:28.642, real unlock→retry05:37:28.877.

Full native regressions проходят: Desktop5280/HomeVPN1165/Portable90; остальные
наборы51/456, diagnostic report32/modal6/chooser3/Context ownership, selected-key
signing55/UI50/prep72 и actual direct/owned-agent OpenSSH→SOCKS→HTTP→crash→fresh
preparation, Instance28/Maintenance126/Shutdown47. Late Python28=26PASS2SKIP.
ZIP0.2.2 SHA256 `35c034cc25c428f6abc16c5a58ded57c7c8e2792090d806c7bdd5099bc241785`.
All uploads SKIPPED; bytes не получены/не переданы/не установлены, installer и
owner acceptance NOT_RUN. Private delivery BLOCKED до согласованного канала.

Program source607 ещё legacy: не приписывать этому run live default switch/
wizard5cce/eb97, а также storage418/servicecf29. Whole R09 FAIL, R11 PARTIAL.
V1.0 I58–80, verified21.75→22, изменение0: те же28 parent anchors переведены
на exact607, D0.5/P0.5/A0 weight87 без роста уровней и двойного учёта подпунктов.

Следующее проверочное source — frozen production-route localda77228; обновить
только3 evidence docs и выполнить один expectedab6a publication после scan/tree.
Source4 paths не меняются. Local address persistence primitive418 и service
`cf29d23e4e67105ba56024e8c0be346ac2677388` исключить из него: API task-before-I/O,
immutable input/Stop join/cancel-before-commit/accepted-save completion/retained
cleanup+7 native vectors написаны, source/syntax/C#5/public/diff PASS, compiler/
native NOT_RUN. Actual wizard still uses sync address adapter; stage3 отдельно.
No binary/release/main merge/owner PC/VPS actions. Ниже checkpoints — история.

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

## Текущий staged R09 checking — ещё без Windows результата

Published606 source `bf1c1e5cff7151b4d0f11d8831a7e3b7a2e0f1dc` / merge
`cd20daf85da786d42244e7ce17fc83726b81fbcc`, run39724309390 проверяет guard/read
primitive/committed cleanup vector. Windows119644614336 in_progress,
Ubuntu119644614044 SUCCESS; новый source не наследует SUCCESS605.

Следующий отдельный checking включает optional background initialization API
ad3fa3e, optional-metadata compatibility00a1831, actual Context/main loading gates
7d9d80f, owned share refresh/retrye385513 и их native tests. Program/default
startup/ShareOrigin getter/QR-configure production routes остаются legacy;
новые tests Windows compilation/native NOT_RUN до exact нового run. Минимальный
Windows прогон — scripts/Test-ProGo.ps1: held initial read/Stop/Dispose, optional
real-lock basic start, Context/tray/dashboard pending/no modal, share real
lock→unlock→retry/cancel/newer publication/cleanup; затем общие native regressions,
диагностика/modal Cancel→Retry/chooser3/Context ownership/selected-key SSH chain.

I58–80, verified22/raw21.75 только exact605;0 баллов за staged code/tests/docs.
No public binary uploads/release/main merge/owner PC/VPS actions. Далее exact606
result, checking source/tree scan and one PR CI; затем actual route integration.
Ниже сохранённые checkpoints — история, не новые результаты.

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

## F22-R03 — local checking candidate, Windows NOT_RUN

Source local b342dfc4c055de5b4e8e7d1733fdb39b0e65ada4/tree2b9640811e30ccfc5e79c26e767b50830aa5712c
на matching85f33/source37f02. Repair changes не входят в CI598. Сначала exact
Windows Test-ProGo:4 missing/empty payload refusals; actual complete built-image
self-check; real sharing failure rollback; contained hanging root+child timeout;
actual parent hard-kill,9 refusal vectors, explicit recovery without payload.
Shared maintenance/native ownership suites обязательны, старый SUCCESS не наследуется.

При проверке на разрешённой изолированной Windows: после interruption не повторять
обычный repair. Сохранить journal/snapshot и использовать Repair-ProGo.ps1 -Recover
из того же проверенного prepared source (installed script ранней фазы может быть
ещё прежним). Команда проверяет ownership/hashes, не запускает приложение и не
повторяет self-check. При foreign/corrupt state остановиться с сохранёнными records;
ручное удаление journal не является восстановлением. Это developer runbook,
установка/действия на ПК владельца ещё NOT_RUN/требуют отдельного разрешения.

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


## Актуальная проверочная база CI592 — completed FAILURE, узкие Windows PASS

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


## История предыдущих проверок (не текущий статус)

CI591 completed FAILURE на source693de8abda98f48108cea97624cbf9cd165b376d /
merge774c77a6f94193ea91281c6adf72a08481db82db. Launcher3 и Windows real server5
PASS; substantial Desktop/recovery51/456/sign55 прошли до нового combined start
FAIL при successful cleanup. Agent/recovery/metrics/ZIP NOT_RUN. ExtraOptions
boundary defect воспроизведён native Linux ssh-G; Windows underlying typed reason
не был записан. Следующий source fix отдельный от local R08. Evidence11/raw10.50
по прежним весам70×D0.5×30%, P/A0; полный CI SUCCESS не заявляется.

Текущий CI590 completed FAILURE на source8487594e6d7d4e4eb16df86aae27f2e980329aaa /
merge20df578c1c2d162f5f373e94aed8107bbfee7d19. Native51/456/guidance10 PASS,
Desktop compilation PASS, но launcher вернул пустой ExitCode: pip outcome неизвестен;
Desktop/server/combined/metrics/ZIP NOT_RUN. Evidence v1.0 raw6.15→6 только
трассированные веса41×D0.5×30%; P/A0. Исторические CI58922 и CI588 ниже не
подтверждают эти недостигнутые сценарии. Следующий шаг — отдельный launcher fix.

Текущая добавочная проверенная база: CI589 SUCCESS, source7964a99ac4f6adf0d1d9034c855e989f60211c0a,
mergef8eb30fddc7f0ecdf0f795de76b8f4be56490705; native lifecycle50/446 PASS.
Runner ZIP0.2.2 SHA256 c66a56fda89845e0e4e68c5fb2d1f87c70e74d01f101113ad8d5e1b21f194805;
uploads SKIPPED, не передан/не установлен. Следующий direct+agent combined/
guidance51 source8487594e Windows NOT_RUN. Нижние CI588/587/586 — история.

# Приёмка следующего ProGo кандидата

Текущий подтверждённый published snapshot: **CI588 SUCCESS**,
source `b32ac4ece5cd62b98eab22b7e65aaa079544e5bf`, tested merge
`e1039ddd046528fc75b9d46897e6924f597768d5`. Native report/helper,4 modal
Cancel→Retry,3 chooser, Context и SSH preparation43 реально выполнены;
Desktop5103 и остальные Windows jobs прошли. Runner ZIP SHA256
`7c6febfc2ebda1834ffa1d04ebc0cc13001bb0629de8f505facb78898d223c5c`, uploads
SKIPPED, архив не передан/не установлен. V1.0 проверенная база22/100 только
этого снимка; common lifecycle c0f5610/50 новых+старых recovery cases отдельно
compilation/native NOT_RUN. Сначала их новый exact-SHA run, не старый SUCCESS.

История CI587 опубликованного16-file кандидата: CI587 completed FAILURE
на source `e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, merge
`decfa11496c12c5e65987043fb58fc7388cb5d6c`. C# compilation/report32/helper2 и4
real modal Cancel→Retry scoped PASS; normal native chooser FAIL по caption
selector при успешной очистке. Context/SSH preparation и последующий хвост
NOT_RUN, ZIP отсутствует. Исправленный selector и последующие local lease/
composition этапы compilation/native NOT_RUN до нового exact-SHA CI.
По неизменной v1.0 консервативно11/100 проверенной готовности (raw10.50);
CI58622 — история. Новый checking tree исключает unfinished lifecycle.

История ожидания опубликованного16-file кандидата: source
`e943ca348d17cbed3f4b5b4b9bca4e206ffb3d03`, tested merge-кандидат
`decfa11496c12c5e65987043fb58fc7388cb5d6c`,
[CI587/39685125066](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39685125066).
Ubuntu119517113715 SUCCESS; Windows119517114158 in_progress. Compilation/native
нового блока пока NOT_RUN; последующий local diff не относится к CI587.
Source push/PR update выполнены без public uploads/release. Прежний CI586 ниже
— отдельный завершённый snapshot, не проверка текущего кандидата.

История предыдущего опубликованного source B: версия0.2.2,
`656b3d65e03d540be859ee69635c492631093446`, PR88 OPEN/DRAFT, unmerged.
[CI586 /39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699)
**completed SUCCESS только для tested merge M**
`7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.
Windows job119465588823 и Ubuntu job119465589235 completed SUCCESS;
build identity `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.
Jobs и точный Windows log повторно прочитаны в REVIEW-03; новый CI не запускался.
История CI583–585 с FAILURE и их точными SHA сохранена в JSON/PROJECT_STATE.
Прежнее наблюдение CI586 in_progress хранится только как prior_observation,
не как текущий статус. CI582 — отдельная историческая база.

Pack release zip SUCCESS, SHA256 по runner log:
`d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
Uploads SKIPPED; ZIP не скачан/не передан. Hash runner не заменяет hash
полученных bytes. Полный standalone setup, согласованный updater/installer
fallback и установка/приёмка на целевом ПК не готовы. Публичный release/installer
не разрешён; способ приватной передачи пакета остаётся BLOCKED.

**Текущий локальный кандидат L — незакоммиченный diff поверх B, отдельного SHA
ещё нет. Windows compilation/native NOT_RUN** для диагностики/report/Context,
advisory SSH preparation, REVIEW-01 ShowDialog Cancel→Retry и REVIEW-02 native
SaveFileDialog. CI586 SUCCESS на L не распространяется. F31-R11 PARTIAL:
агрегатор не подключён к общему SSH start/recovery. Верхнеуровневые31 и55
критерии этим CI не закрыты. REVIEW-03 меняет только3 документа; commit/push/
новый CI, код и тесты в этой микрозадаче не выполняются/не меняются.

## Постоянный порядок и текущий разрешённый этап

С22.10.2026 действует постоянный [порядок](PROJECT_STATE.md): продолжать
согласованные микрозадачи самостоятельно; source push в существующую рабочую
ветку, PR88 и необходимые CI разрешены также для последующих исправлений.
Прежние обязательные остановки/ожидание «продолжай» отменены. Границы release,
binary upload, main merge, ПК/VPS/служб/ACL/сети сохраняются. Каждый source push
требует scan secrets/PII и проверки автоматических workflow. Новый режим не
создаёт расписания или автоматического перезапуска исполнителя.

Следующий ниже16-file блок теперь разрешён к local commit/source publication
и exact-SHA Windows CI. В этой точке он ещё не отправлен/не проверен; CI586
по-прежнему не подтверждает его. Автоматический public upload остаётся OFF.

## Проверочный коммит — согласованный состав

Предлагаемый отдельный commit: `Bound diagnostic preview lifecycle and add advisory SSH preparation`.
База B указана выше; новый source SHA фиксируется только после реального commit.
Включить **ровно16 текущих modified/untracked файлов** (после финальной сверки
содержимого и secrets/PII; без generated build/release payload):

- `src/DiagnosticPreview.cs`
- `src/DiagnosticReport.cs`
- `src/UpdateAwareTrayApplicationContext.cs`
- `src/SshAuthenticationPreparation.cs`
- `tests/DesktopDiagnosticTests.cs`
- `tests/DesktopTests.cs`
- `tests/DiagnosticPreviewTests.ps1`
- `tests/DiagnosticReportTests.cs`
- `tests/DesktopAsyncDiagnosticPreviewTests.cs`
- `tests/DesktopDiagnosticContextTests.cs`
- `tests/SshAuthenticationPreparationTests.cs`
- `scripts/Test-ProGo.ps1`
- `docs/ACCEPTANCE_RUNBOOK.md`
- `docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md`
- `docs/PROJECT_STATE.md`
- `docs/project_state.json`

Это один диагностический/advisory блок вместе с REVIEW-01/02 и документацией;
он не реализует SSH launch/recovery integration, repair или installer fallback.
Во время REVIEW-03 commit/stage/push не выполнялись. Теперь эти source действия
и необходимый CI разрешены постоянным порядком; перед ними сверить фактическое
состояние и не дублировать уже запущенную операцию.

### Минимальный готовый Windows-прогон накопленного блока

На изолированном Windows runner с .NET Framework/OpenSSH, тем же CI окружением
и доступным native desktop зафиксировать чистый checkout нового source SHA,
а при PR merge — также точный tested merge SHA. Существующий entrypoint:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\scripts\Test-ProGo.ps1
```

Скрипт сам вызывает Build-ProGo: отдельно повторять сборку не требуется.
У текущего DesktopTests нет selector этих suites; поэтому минимальный готовый
путь включает общий Desktop harness и существующие Windows регрессии скрипта.
Это один Windows entrypoint, не запуск всего Actions workflow: отдельные
Ubuntu jobs, Test-HomeVpnRelay, ZIP packing/upload для этого локального блока
не требуются. Целевая приёмка ПК/VPS и всего продукта остаётся отдельной.

Обязательные доказательства нового SHA:

1. C# compilation приложения и всех связанных tests; DiagnosticReportTests
   с cancellation и прежними limits/redaction/snapshot критериями.
2. DiagnosticPreviewTests.ps1 в новом PowerShell-процессе: установленный shared
   loader/API и точный reviewed snapshot; DiagnosticPreviewWorkflow сохраняет
   клавиатуру, minimum layout и действия.
3. AsyncDiagnosticPreviewWorkflow: реальные file build/save, heartbeat,
   single-flight, close/Dispose и отсутствие позднего результата/записи;
   четыре реальных ShowDialog Cancel→Retry (build/save × click/Esc),
   три native chooser режима (Cancel, missing caption, injected error).
   В основном режиме timeout всегда FAIL; негативные режимы требуют точного
   ненулевого20/21 и cleanup marker, а не любого зависания/ненулевого exit.
4. DiagnosticContextWorkflow: существующие actual tray/Context shutdown/Dispose
   и ownership сценарии; старые Desktop/Instance/Maintenance/Shutdown регрессии
   не теряются из-за изменения tray context.
5. SshAuthenticationPreparationWorkflow: настоящий локальный ssh-G и test-owned
   agent pipe, total budget, cancel/freshness/config mutation; существующие
   selected-key sign/UI, policy, agent start/coordination сценарии. Это проверка
   advisory aggregate, не live SSH auth и не launch/recovery integration.

SKIP/NOT_RUN не считаются выполненной проверкой: если изоляционные guards
пропустили suite, её критерий остаётся NOT_RUN даже при exit0 общего скрипта.
Сохранить source SHA/tested SHA, build identity, фактические результаты и logs.
При долгом запуске сохранить ID job/сессии; во время ожидания продолжать
независимые разрешённые задачи и проверять тот же запуск без дубликатов. Установка на ПК владельца,
реальные VPS/службы/ACL/системная сеть в этот Windows dev-прогон не входят.

## До передачи установщика

1. Push/CI в публичный ProGo явно разрешены21.10.2026. Публичный релиз и
   публикация установщика не разрешены. Согласовать передачу пакета; public
   artifact upload OFF, явный dispatch opt-in пока не применять.
2. На точном новом SHA пройти существующие Windows Test-ProGo/Test-HomeVpnRelay
   и Ubuntu isolated suites. При первом FAIL сохранить logs/исходный SHA,
   установить причину, исправить и тестировать новый SHA; не скрывать ранний FAIL.
3. Завершить остальные обязательные dev-критерии PROJECT_STATE.md, включая
   selected-key readiness, полный setup/fallback и package gates. Одна зелёная
   промежуточная CI ZIP не означает завершение31/55.
4. Для успешного пакета зафиксировать version, source SHA, tested merge SHA,
   artifact ID, SHA256 скачанных bytes, состав и unsigned/signed состояние.
   Проверить архив и инструкцию возврата. Пакет должен оставаться приватным.

## Установка владельцем после отдельного разрешения

1. Зафиксировать identity/версию работающей старой программы и проверенную
   резервную копию. Сохранить профили/настройки/vault/личный VPN доступ и paths.
   Приватные SSH-ключи не включать в диагностический отчёт или пакет.
2. Сверить SHA256 кандидата и его source identity. Закрыть ProGo штатно;
   запустить проверенный setup/update с зафиксированными существующими путями.
   Опцию изменения общей службы SSH-agent выбирать отдельно; она OFF по умолчанию.
3. Пройти сценарии ниже. При частичном результате записать точный шаг, причину,
   installed identity и время; не объявлять общий PASS.

| Сценарий | Необходимое свидетельство |
| --- | --- |
| Запуск/отзывчивость | Открываются главное окно/настройки; heartbeat и действия отвечают при медленном диске/сети; latency/CPU/RAM/I/O измерены |
| Профиль | Выбранные alias/user/port/IdentityFile соответствуют effective OpenSSH; нет старого absolute path другого ПК |
| Агент и выбранный ключ | Running/Stopped/Disabled/denied различаются; запуск в доступных правах; encrypted key разблокирован один раз, секрет нигде не сохранён |
| SSH → SOCKS → HTTP/HTTPS | Owned SSH, настоящий SOCKS handshake, HTTP и CONNECT; внешний маршрут проверен отдельно; публичный fingerprint не выдаётся за auth |
| Отказ и отмена | Неверный/недоступный ключ, changed host key, network drop, Stop/Close; нет позднего старта/дублей/завершения чужого процесса |
| Восстановление | Ручной повтор после исправления и допустимый backoff; причины/последний результат актуальны |
| Обновление | Оба пути update/setup, fixed paths, cancel/offline/hash failure, сохранность данных; rollback success только после проверки |
| Перенос | Другой Windows SID/ПК и другой разрешённый VPS; локальный ключ выбирается явно, VPN archive scope не расширяется |

## Возврат к предыдущей версии

Использовать заранее проверенную копию и штатный **Program-only** restore,
сохранив текущие настройки/vault/VPN/request/proxy ownership journals.
Проверить hashes/версию/self-check восстановленного runtime до запуска.
Если rollback не подтверждён, сохранить backup/log/recovery evidence и
остановиться на точной ошибке. Не подтверждать отдельную замену personal data
по умолчанию. Затем повторить запуск/профиль/маршрут на прежней версии.

Живые действия выполняются только на отдельно разрешённых ресурсах. Эта
инструкция не даёт автоматического разрешения установки, правок VPS/ACL/сети
или реальных писем/платежей.
