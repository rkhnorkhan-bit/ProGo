# SSH-agent: PGA-01 и PGA-02A — спецификация и read-only service snapshot

Источник — переданная владельцем спецификация16.10.2026, включаемая в текущий
F31/F23: [canonical contract](SSH_AGENT_AUTOSTART_SPEC.md). Исходный файл не
меняется; его адреса/профиль/личные пути не переносятся в Git и не становятся
defaults. Baseline `25d34000f5adb9c481021f47f8ef05ad2af073e5`, VERSION0.2.2.

PGA-01: аудит существующего кода и mapping; проверяются планом и независимым
review. Реализация узкого PGA-02A прошла source review и37 native checks; общая
Code/Dev приёмка пока **PENDING** из-за последующего FAIL #555 в Instance fixture. Весь PGA-02
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
55 уникальных IDs=43+12, прежние6 accepted IDs сохранены. Desktop4028 PASS
относится к555; результат исправленного полного run пока **PENDING**.

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
ожидается Instance28=26+2. Hard-coded sleeps и blanket retries не добавлены.
Точный lock owner555 остаётся неизвестен; это исправляет установленный source
недостаток fixture, не выдаёт предполагаемую причину за измеренный stack.
