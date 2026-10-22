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
