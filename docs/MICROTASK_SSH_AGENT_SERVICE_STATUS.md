# SSH-agent: PGA-01 и PGA-02A — спецификация и read-only service snapshot

Источник — переданная владельцем спецификация16.10.2026, включаемая в текущий
F31/F23: [canonical contract](SSH_AGENT_AUTOSTART_SPEC.md). Исходный файл не
меняется; его адреса/профиль/личные пути не переносятся в Git и не становятся
defaults. Baseline `25d34000f5adb9c481021f47f8ef05ad2af073e5`, VERSION0.2.2.

PGA-01: аудит существующего кода и mapping завершены. Узкий PGA-02A —
**Code/Dev PASS** на source13b98cce, completed CI #557, независимых source/tests/
native/ограниченном visual reviews. Истории FAIL555/556 сохранены ниже. Весь PGA-02
не завершён: отдельный asynchronous deadline/single-flight lifecycle —02B.
Запуск службы, installer opt-in, matching нужного ключа, secure interactive
ssh-add и отдельные UI действия остаются впереди. Код должен быть готов ДО
installer; live encrypted-key/другойПК/VPS принимаются после установки.

02A вводит read-only typed snapshot: runtime отдельно от startup policy,
raw observed values, отдельные status/config errors со stage и Win32Code.
Running+Disabled не смешиваются; Stopped не утверждает обязательного admin.
Native SCM запросы используют только connect/query rights, status/config
handles независимы; config failure не теряет известный runtime. Буфер config
ограничен8192 bytes, cleanup в finally, token checkpoints до/после операций.
Они не обещают прерывание начатого P/Invoke или hard deadline. Не выполняются
StartService/ChangeConfig/ACL/elevation/PowerShell/key operations.

Старые Read/Describe text wrappers остаются совместимыми; existing agent keys,
SSH alias/config/fallback/SOCKS logic не переписывается. Наличие стандартной
службы не доказывает использование именно этого агента выбранным SSH-профилем,
наличие нужного ключа либо успешную аутентификацию. IdentityAgent none/custom
и другие рабочие методы без агента не блокируются этой диагностикой.

Счёт остаётся **6/55**, остаток **49=37+12**, общий **23/31**, весь F31 **OPEN**.
F31-R11 уточняется существующим обязательным агентским dev объёмом без новых
верхних ID; F23 содержит installer opt-in, F31-R12/13/14 — реальные приёмки.
PGA номера — микродекомпозиция, не10 новых пунктов31 или55. F31-R05 и Codex
shortcut ownership finding сохраняются OPEN/PARTIAL. Package PENDING,
Live NOT_CHECKED; устройства владельца, VPS и установленное приложение не
проверяются. PR draft/unmerged; merge/release не выполняются.

## Frozen source и проверки

Source `c1a716b7cf3183d5bcfe955ab519c6759b8ceb27`, tree
`99a38e2f375f06e64963c0beecbdd1285343c24d`, parent
`25d34000f5adb9c481021f47f8ef05ad2af073e5`; локальный эквивалент
`a94957cf854d4b4c8d8bf877ba9e664d9ebaf82e` имеет идентичное tree.
Независимый полный source/tests/docs/wiring review — **APPROVE**.
Точный исходный attachment SHA-256 записан в canonical contract.

| Файл | Git blob |
| --- | --- |
| `src/SshAgentServiceStatus.cs` | `c560871683457443153d32f185bb79fe084f076f` |
| `src/SshProfileDiagnostics.cs` | `9a872c81a9426469d623c3a91520467cc6309e35` |
| `tests/DesktopSshAgentServiceTests.cs` | `c47ca9fac9d33d8e8919c6702cc1bc98a5739946` |
| `tests/DesktopTests.cs` | `c0a8d6573aeb7d589998ea1a86276dc6ddb14fd6` |
| `scripts/Test-ProGo.ps1` | `5502ded756d7bb7581602ab755455ee6fd34e3b8` |

Новые37 checks:8 state/format/rights,19 failures/buffers,9 cancel/repeat,
1 independent native oracle. Fake контролирует только native boundary,
классификация/буфер/cleanup выполняются production кодом. Проверяются
Running+Disabled, все переходные состояния и startup modes, missing1060,
denied5, RPC1722, независимые partial результаты, неизвестные raw значения,
min/max8192, ошибочный probe и рост config, неопределённый successful
bytesNeeded, unwritten startup sentinel, exceptions/null allocation,
pre-cancel и7 native boundaries,24 повторных чтения без утечек.

Native oracle использует собственные P/Invoke и полный native struct,
фиксированный8192 buffer вместо production probe/parser; считывает текущую
службу изолированного CI без start/change/delete/registry/UAC. Он не доказывает
работу зашифрованного ключа, будущего автозапуска или arbitrary live служб.
Старые test conditions и budgets сохранены. Main/compiler получили по одной
вставке; production *.cs подключаются существующим glob.

[CI #555](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38885779652)
completed **FAILURE**. Windows job `116923046711` FAILURE, Ubuntu
`116923046466` SUCCESS.
PR-triggered merge `30649d3d023bf02f35419f4570b29985abe00c82` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
Локально whitespace и public content **270 source/0 release PASS**;
55 уникальных IDs=43+12, прежние6 accepted IDs сохранены. На момент подготовки первой коррекции Desktop4028 PASS
относился к555; итог исправленной версии — отдельный completed557 ниже.

## Ubuntu native результат

CI #555 Ubuntu job `116923046466` completed **SUCCESS**. Фактически16 operation
recovery +9 sharing/sync +46 isolated provision checks PASS. Независимый QR
декодирован zxing-cpp2.3.0/numpy2.2.6 в том же venv; recovery/revoke/reissue/HTTPS
достигнуты. Пропусков и ошибок нет. Checkout log подтверждает source/merge/base,
указанные выше. Прежнее предупреждение Node20 checkout не скрывает test failures.
Полный UTF-8/LF log:54747 bytes, SHA-256
`0a72977b2e7c05ed5ca4bf2966c424afe5543615cfd94e639da88eb9087fcfb7`.
Это изолированный dev runner, а не проверка VPS или устройства владельца.

## История первого native CI #555 — FAIL сохраняется

Новые37 checks **PASS** с10:23:48.3744859 до10:23:48.3897901 UTC.
Independent SCM oracle фактически увидел runtime1=`Stopped`, startup4=`Disabled`,
statusError0/configError0. Это read-only состояние изолированного runner.
Desktop **4028=3991+37 PASS**,10:24:26.0969001 UTC; SOCKS **41/403 PASS**.

Затем Instance fixture дошёл до21 PASS, последний — clean owner disposal;
в10:24:31.0435955 появился IOException sharing на proxy-environment-backup.json.
Измеренный stack — только PowerShell Test-ProGo.ps1:526. C# line и lock owner
не сохранились. По source участок после primary startup читает baseline backup,
хотя прежний AppliedPort>0 доказывает только environment+listener. Независимый
review подтвердил недостаточность этого readiness predicate: receipt/corrections
ещё могут публиковаться. Это source inference о возможном окне, а не измеренный
exact lock owner либо доказанная SCM-регрессия.

Maintenance95/Shutdown47/updater package5, HomeVPN975/portable90, Windows Python,
release public и release ZIP в555 **НЕ ДОСТИГНУТЫ**, PASS им не присвоен.
Полный UTF-8/LF Windows log740166 bytes/6809 lines, SHA-256
`52e2d9b3950497da32517124d2dcdaa2fef90786ef556579c2aa0b5d6a9876c1`.
Metadata и upload-log двух partial artifacts совпали: screenshot12039896440,
5542632 bytes/240 files, SHA-256
`1da0700fefda292a62ace23e11a5121401fc67dfaa0903ec9ba8aaad9c536bbd`;
diagnostics12040550184,80023 bytes, SHA-256
`0ed480e4c5b955318db33e69281ded586e473a7f7370515c7dd63b73f65ae330`.
Эти555 artifacts не скачивались; полного runtime/package artifact нет.

Узкая коррекция Instance fixture выполняется отдельно: existing10s readiness
должен captured port/settings/валидный settled owned receipt до baseline,
в новом capture helper ожидать только sharing/lock32/33, не скрывать остальные ошибки. Backup reader
не должен мешать atomic File.Replace. Старые26 assertions и budgets сохраняются,
production SCM/proxy код не меняется. Новая полная native проверка обязательна.

### Коррекция fixture после555

Изменён только `tests/InstanceTests.cs`, frozen blob
`7929d2fd544c094980dc0d58d874fb93b22bb5f3`. Production/tests Desktop/scripts
изc1a716 не менялись. Baseline reader использует Read|Delete без Write sharing:
открытая версия остаётся неизменной, atomic File.Replace разрешён. Existing10s
Wait принимает только целые совпадающие settings/receipt с тем же owned port,
всеми4 original environment names, без pending typed corrections; повторные
чтения и AppliedPort исключают замеченную смену baseline. В новом capture helper
только native32/33 означают not ready, остальные ошибки не подавляются. Прежний
AppliedPort с catch(IOException) сохранён без изменений. Последующий
assert сохранности строгий и без retry. Readiness доказывает устойчивый baseline,
а не физический возврат всех broadcast calls; при0guards prepared/final receipt
идентичны по bytes.

Две новые native regression checks используют тот же helper и реальные temp
files: замена при held old reader оставляет old bytes неизменными и возвращает
новый receipt; wrongport/pending/exclusive writer не дают baseline, released
settled receipt принимается. Старые26 Check/их смысл и прежние budgets сохранены;
до свежего CI ожидалось Instance28=26+2, затем подтверждено557. Hard-coded sleeps и blanket retries не добавлены.
Точный lock owner555 остаётся неизвестен; это исправляет установленный source
недостаток fixture, не выдаёт предполагаемую причину за измеренный stack.

## Повторный полный CI #556 — FAIL сохраняется

Candidate `8d2618a072e45cb6dcae6f5a66a0f2b6916af7c5`, tree
`e97d40471b97113c1230ffb718270d7d165bbfdc`, parent
`c1a716b7cf3183d5bcfe955ab519c6759b8ceb27`. Локальный эквивалент
`754438856b337cb8c3dd6a4fb7726777557ea089` имеет идентичное tree.
PR-triggered merge `f576eca52d86bb43eb4de71ee1a546668cfbba04` в main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #556](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38887906839):
Windows `116930056522` completed FAILURE; Ubuntu `116930056270` completed SUCCESS.
Ubuntu16/9/46 PASS без skips; actual independent QR/recovery/revoke/reissue/TLS
достигнуты. Полный UTF-8/LF log52935 bytes, SHA-256
`2886fbdbd2c3fed5a4ecc2f27309607e9b3a3a361cb7777be14a839657c4c717`.
Correction source/docs независимый freeze review APPROVE; runtime source
не менялся после555. Новый run не меняет сохранённый статус FAIL #555.

### Точная граница556

Все37 SCM checks вновь PASS10:43:40.249–10:43:40.267 UTC, independent oracle
runtime1/startup4/statusError0/configError0; SOCKS41/403 PASS. Затем первый
старый config oracle `fixture.dotted` FAIL10:43:40.3003191 UTC на условии
ExitCode==0 && !Truncated. Код выхода, false operand и stderr OpenSSH прежний
fixture не выводил, поэтому причина **UNKNOWN**. C# frames: ConfigMatchesNative →
SshConfigAuthority → Main. Desktop summary4028, Instance28 и обе новые file
regressions, Maintenance/Shutdown/updater/HomeVPN/portable/Python/public/ZIP
в556 **НЕ ДОСТИГНУТЫ**. PASS из555 им не переносится.

Windows runner image в555/556 одинаковый: windows-2025-vs2026/20261007.264.1;
разница образа не является объяснением. Между SCM INFO и FAIL около33ms, это
не подтверждение7000ms timeout. Full Windows UTF-8/LF log710331 bytes/6542 lines,
SHA-256 `d5c7a176fd42ce34c61f655848126dfbe25242458ece8ac2d32c5d8d7da874c6`.
Partial screenshots12040489236:4671410 bytes/207 files, SHA-256
`2658cf9b212cb36244e4241c4dcceb583b980558170051b507897916ebb41f7e`;
diagnostics12041310173:75207 bytes, SHA-256
`1cb5dd97faac8be662a80790c81c747d814d60ce1ebcdf22cfcb7ac70dc6966e`.
Metadata/upload-log согласованы, downloads не выполнялись; полного package нет.

Перед новым прогоном добавляется только diagnostic INFO при failure старого
изолированного native ssh-G oracle: exit/truncated/lengths и ограниченный,
очищенный от control chars и fixture/CI profile paths stderr. Условия,
7000ms budget, argv, количество checks, отсутствие retries и production код
сохраняются. Это не выдуманная коррекция неизвестной причины556; новый прогон
должен либо дать полный результат, либо раскрыть фактический отказ.

Diagnostic-only frozen blob `tests/DesktopSshConfigTests.cs`:
`666721dc04cc47c838b618f444fe4278cebc8b06` (+21 lines). INFO появляется только
при прежнем failure condition, stdout не выводится, stderr до1024 chars после
редактирования fixture/CI profile paths и удаления control/line-separator chars.
Runtime source, argv, Check и7s budget не менялись. Source/root review подтверждает
эту границу; обязательная свежая полная native приёмка выполнена в557. Одинаковый image
не доказывает отдельно hash/version выбранного ssh.exe: они в555/556 не измерены.

## Полный CI #557 — Code/Dev PASS для PGA-02A

Source `13b98cceec08cd48ff70155edcb5515a1ccf7a21`, tree
`8a9ef3c9f42b88f4e890b7f650dc4865801845d9`, parent
`8d2618a072e45cb6dcae6f5a66a0f2b6916af7c5`. Локальный эквивалент
`06cfca7765fd4e735d709068cfcba69fbe4104b4` имеет идентичное tree.
Merge `f2ae6db0b1d753337f525d12d6648f3f28df10d8` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #557](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38889610234):
Весь run и оба jobs completed **SUCCESS**: Windows `116935638378`,
Ubuntu `116935638585`.
Ubuntu16/9/46 PASS без skips, actual independent QR/recovery/TLS выполнены.
Полный UTF-8/LF log52822 bytes, SHA-256
`10004ab24dfeb9343e84ced3dc95c36a19c4477e4a110d22017f380643ba0d6d`.
Истории FAIL555/556 сохраняются. Результат557 не устанавливает неизвестный
lock owner555 или неизвестную причину ssh-G отказа556 задним числом.

### Фактический Windows результат557

Все37 SCM checks PASS11:00:04.1112444–11:00:04.1279798 UTC; independent native
oracle runtime1=`Stopped`, startup4=`Disabled`, обе ошибки0. Desktop
**4028=3991+37 PASS**11:00:46.6699990. Instance **28=26+2 PASS**11:00:54.0358190.
Новые реальные file regression PASS11:00:47.4413733/.4462671. INFO native32
11:00:47.4453674 находится между этими двумя fixture checks ДО first-owner;
он относится к специально заблокированному temp receipt, а не к actual startup.
Прежние26, strict settings/backup compare и crash replacement прошли.

SOCKS **41/403**, прежние foundation/expiry/restart suites, Maintenance95,
Shutdown47, updater package5 и весь Test-ProGo PASS11:03:29.2067144.
HomeVPN975/portable90 PASS, Windows Python24=22 PASS+2 прежних Linux-only skips,
все7 HTTP profile-share cases ok; public **270 source/44 release PASS**.
Все последующие suite gates, пропущенные в555/556, достигнуты на557.
Старый native config oracle `fixture.dotted` PASS11:00:04.168 UTC. Failure-only
SSH INFO не выполнялась: причина556 остаётся UNKNOWN, её исправление не заявлено.
Она сохраняется как незавершённый диагностический finding в открытом F31-R11;
новый failure INFO позволит уточнить его при воспроизведении.

Полный Windows UTF-8/LF log921347 bytes/8506 lines, SHA-256
`98c3efaf53f5500d52c56f03c87c64dffda9617c3e5d6ecb0158f64f5716c84e`.
Source/tests/wiring/docs, correction, обе native jobs,2 limited visual PNG
проверены независимо — **APPROVE**. Старые assertions и budgets сохранены.

| Артефакт557 | ID | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| Screenshots | 12041748472 | 6488268 | `0437d9736683e6eeeb014e26915e385407cf970ff8e49d8c17894854c82510f3` |
| Diagnostics | 12041309870 | 86220 | `257ac6db415e5a5733f1b33b7bc676c78d06d6c24746b929ae1129da8d01a26c` |
| CI runtime folder | 12041783422 | 533493 | `1b5dfd7d06a66adaa46135f617451c92328aa3f45ed6ce72e93dd3bcb4eeb798` |
| CI ZIP wrapper | 12042352402 | 525341 | `602ad1e1f68e1283171a7c55fbd4010f1668270fa71f2853b47877588c76f1c2` |

Metadata всех4 совпала с runner ID/size/digest/head13b98cce. Screenshot ZIP
скачан, его6488268 bytes/291 entries и SHA-256 пересчитаны root и reviewer.
Просмотрены только2 свежих `ssh-agent-guidance.png`/`ssh-diagnostic-success.png`:
новая подсказка stopped читаема в показанном размере, нижний текст в scrollable
report, Retry/Close доступны. Другие289 PNG и полные contrast/DPI/Narrator
не приняты. Остальные3 artifacts не скачивались; для них подтверждены только
metadata/runner. Inner CI ZIP528621 bytes; это не цельный installer всех31.

PGA-01/02A завершены в узкой Code/Dev границе. Следующий02B — bounded asynchronous
waiting и single-flight с учётом полного settlement, затем PGA-03/start и
selected-key/interactive/UI/installer части. Весь PGA-02 и F31-R11 **PARTIAL/OPEN**,
счёт **6/55**,49=37+12,23/31 не меняется. Необъяснённый отказ556 не потерян.
Package PENDING, Live NOT_CHECKED; owner PC/VPS/телефон не проверялись,
служба/ключи/ACL не менялись, merge/release не выполнялись.
После tested source13b98cce публикуются только4 Markdown evidence/status файла;
application/tests/scripts/server/workflow идентичны tested source.
