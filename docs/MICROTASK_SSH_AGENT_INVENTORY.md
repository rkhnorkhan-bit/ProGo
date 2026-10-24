# PGA-05A — структурированный read-only inventory SSH-agent

**17.10.2026 UTC. Узкий PGA-05A Code/Dev PASS.** Родительский checkpoint
`596f0787c19cfcc9695b4e4f1f540bf25c2cd0ef` (PGA-03B/CI560).

## Граница и исправления

Первый узкий шаг оставшейся PGA-05 и основа PGA-06 внутри F31-R11.
Вместо подсчёта строк ssh-add — typed inventory SHA256 fingerprints.
Раньше любая успешная строка считалась ключом, любой exit1 — отсутствием ключей;
default agent мог проверяться вопреки effective IdentityAgent none/custom endpoint.
Теперь повреждённый/неполный ответ не доказывает наличие или отсутствие ключа,
command failure отделён от точного native empty response, default не подставляется
за явно заданный другой агент.

**Весь PGA-05/PGA-06/F31-R11 не PASS.** Связь effective IdentityFile с fingerprint,
проверка подписи, сертификаты/constraints/FIDO, custom endpoint propagation,
interactive add/unlock, ACL и согласование readiness с AutoStartSocks остаются
последующими микрошагами. Present в inventory не означает подходящий профиль,
разрешённую подпись или аутентификацию. Агент не становится обязательным для
прямого незашифрованного ключа и других рабочих auth методов.

## Реализация

- `SshAgentInventoryResult`: immutable state/nullable exit/listed count и read-only
  уникальные canonical SHA256 fingerprints в native order. Exact Ordinal lookup
  Present/Absent/Unknown; invalid digest и неполные данные дают Unknown.
- Только sibling ssh-add выбранного OpenSSH, команда `-l -E sha256`. Deadline3000ms
  передан прежнему owned JobObject runner. Это бюджет запуска/ожидания, не обещание
  физического cleanup ровно через3000ms. Runner/kill-tree протокол не менялся.
- Nonblocking process-wide slot для inventory: concurrent caller получает Busy,
  владелец держит slot до полного return/throw runner, включая его cleanup.
  Это ещё не единый coordinator SCM и будущего mutating ssh-add.
- Read синхронен внутри прежнего diagnostic worker, не на UI. Pre/post cancellation
  не публикует поздний result уже отменённой операции. Private key/passphrase не
  читаются; нет settings/cache/телеметрии. Fingerprints доступны только в ephemeral
  typed inventory; comments/raw stdout/stderr/exceptions не сохраняются в нём и
  не попадают в display/report.
- Полный bounded capture: <=131072 UTF-16 code units на поток (не bytes),
  <=256 identities, положительный bit
  count, native line shape и canonical base64 digest32bytes. Invalid/truncated/
  overflow/unexpected stderr отбрасывает весь prefix. Повторы сохраняют listed count.
- NoIdentities только для exit1 + точного stdout OpenSSH
  `The agent has no identities.` + пустого stderr. Иной exit1/nonzero — CommandFailed;
  exit2 — Unavailable. Неизвестный/локализованный ответ не угадывается.
  MissingExecutable/TimedOut/Failed/Busy/InvalidOutput — отдельные safe states.
- `SshProfileDiagnostics.Check` хранит typed AgentInventory и safe display string.
  Unresolved profile, IdentityAgent none, explicit endpoint или pending service
  probe не запускают default inventory. Любой explicit endpoint, включая
  SSH_AUTH_SOCK/$variable, пока EndpointNotChecked; environment не переписывается.
  Program/startup/ProxyService/arguments/config resolution/SCM API не менялись.

## Реализованная матрица проверок

Новый Desktop partial: canonical/order/dedup/immutable/exact match; malformed,
NUL/noncanonical/mixed prefix/limits/stderr/truncation; safe display всех states;
read-only argv/deadline; missing client/pre-cancel;12 parallel Busy callers,
held owner после cancel/late output/slot reuse/timeout/failure; profile scope guards.

Native tests: настоящий Windows OpenSSH `-G -F` читает только fixture config;
настоящий sibling ssh-add обращается к временному test-owned named pipe.
Pipe допускает только REQUEST_IDENTITIES11 и отдаёт synthetic public-only wire blob:
identities, empty и protocol failure; nonexistent pipe проверяет exit2.
SHA256 blob вычисляется независимо от production parser. Системная служба не
запускается, private keys не создаются/не загружаются, signing и VPS connect нет.
Тест временно меняет SSH_AUTH_SOCK только своего процесса и восстанавливает его.

Прежние diagnostic guards сохранены: fixtures приведены к canonical digest,
argv уточнён до SHA256, exit1 с private error теперь обязан означать failure,
а не ошибочный empty. Partial явно включён в Desktop compile/workflow; прежние
watchdog/budgets не менялись. Локально нет Windows/.NET runtime; native приёмка
подтверждена CI561 ниже. Самостоятельный source/native review, без независимого
agent review.

## Native Code/Dev приёмка

Tested source `7366f33ee58678122e6c2ccfbd19963db5dcd744`, tree
`bf06485ad4fc63f39ce68b960293a553a2374697`, merge
`e4f4fe315f7f11118eeac782ea6a3e5a54cd9e6d` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #561](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39015169806)
completed SUCCESS: Windows job117345682756, Ubuntu job117345682578 и весь run.
Оба checkout log подтверждают один merge/source/base; main — ancestor source.
Повторного запуска CI не было.

Новый block BEGIN08:02:55.4679290UTC,66 фактических PASS между BEGIN и marker
`SSH-agent inventory checks PASS: 66`08:02:55.7803630UTC. Desktop
**4232=4166+66 PASS**08:03:36.0924832UTC. Все native checks дошли до конца:
fixture-only effective profile08:02:55.5231902; настоящий ssh-add available
08:02:55.7011029, independent SHA25608:02:55.7012328, empty08:02:55.7273742,
protocol failure08:02:55.7538625, nonexistent pipe/exit2 08:02:55.7802660UTC.
Это реальные executable/IPC проверки с public-only wire fixture, не mock argv и
не реальный service start/private key/signing/server authentication.

Прежние37 SCM/32 bounded-probe checks,66 start и40 startup сохранены; actual
read-only SCM oracle08:02:48.1840259: runtime1/Stopped,startup4/Disabled,
statusError0/configError0. Старые66/40 markers —08:02:53.3349622/08:02:55.4678619.
SOCKS41/403 PASS07:56:06.7548445; Instance28 PASS08:03:43.6669648;
Maintenance95 PASS08:05:53.2986556; Shutdown47 PASS08:06:23.3646898;
updater5 PASS08:06:25.3843088; весь Test-ProGo PASS08:06:25.3850260UTC.
Старый native fixture.dotted oracle PASS08:02:55.8219488; причина исторического
CI556 по-прежнему UNKNOWN, нынешний PASS её не объясняет.

HomeVPN975 PASS08:09:49.1305053, portable90 PASS08:11:30.5541210,
Windows Python24=22PASS+2 прежних Linux-only skips, OK08:11:34.0808627;
все7 HTTP profile-share cases ...ok; public282source/44release PASS08:11:35.4269221.
Ubuntu16 recovery и9 sharing/sync PASS,46 provisioning без skips, actual
numpy2.2.6/zxing-cpp2.3.0 independent QR; recovery/revoke/reissue/TLS/session tail
дошёл до restart/session PASS07:52:55.7225501UTC.

Изменения functional source:10 файлов,511insertions/30deletions. SCM/probe/startup,
Program/ProxyService/Core/DiagnosticProcess/InstanceTests/workflows идентичны
родительскому checkpoint. Desktop main и compiler script имеют по одной вставке.
Старые diagnostic guards не удалены: изменённая exit1 проверка требует правильного
failure, а не старого ошибочного empty. После tested source — только4 Markdown;
runtime/tests/scripts/server/workflow идентичны tested source.

## Логи и artifacts

Локальные decoded logs нормализованы в UTF-8/LF с одной конечной новой строкой:
Windows945871bytes/8717lines, SHA256
`9e318fed9ba6dedfe0343c50b02cf2e9bad75b2813e6859b0c750b9ae5a518b6`;
Ubuntu52051bytes/573lines, SHA256
`2adb8b273b3d94c51ab94aff456b0517b20bc5ebf6f5bd7a73cd7f27365daf6c`.

Все4 API artifact head/ID/size/digest совпали с source7366f33 и actual upload log:

| Artifact | ID | Wrapper bytes | SHA256 digest |
| --- | --- | --- | --- |
| ProGo-ci-diagnostics | 12094776746 | 90827 | 7267be05de6434f947c95f33ebcc66d58a72569b3ee9e88a0c975d669778feaa |
| ProGo-release | 12094069706 | 541485 | 15efb581165552e2ecdb19ba5dd224b4bca796843934c68a77629e382d975e00 |
| ProGo-release-zip | 12095260681 | 533153 | b941a27d9dcea78fea9e56b43c75519792bf782bd2a4e135f45bb20686345fcd |
| ProGo-wizard-screenshots | 12095260669 | 6540943 | 1d3ee3a3fd34500f1051a341c95dddde5a7b574f8f814d9b11375de397d323ec |

Архивы не скачивались: локальной проверки их ZIP entries/actual archive hashes и
новой visual/DPI/High Contrast/Narrator приёмки нет. Runner сообщает293 screenshot
files, это не локально проверенное число ZIP entries. Inner CI release.zip536608bytes
по runner — не готовый установщик всех31 задач. Package PENDING/Live NOT_CHECKED.

## Первичные источники

[OpenSSH ssh-add manual](https://man.openbsd.org/ssh-add) — -l/-E и exit0/1/2;
[list_identities](https://github.com/openssh/openssh-portable/blob/master/ssh-add.c) —
точный empty stdout/failure stderr/fingerprint shape;
[ssh_config](https://man.openbsd.org/ssh_config) — IdentityAgent overrides environment,
none отключает агент, IdentitiesOnly ограничивает выбор. Compatibility подтверждается
native CI, а не выводится из текущей версии manpage.

## Дополнительная source-проверка и локальная граница

Human-readable inventory с comments не является anti-injection протоколом или
доказательством possession/signing. В частности, недоверенные multiline comments
не превращают текстовый Present в криптографическое доказательство. Новый API не
выставляет Ready и не допускается как единственный auth gate: дальнейшие PGA-05/06
обязаны проверять нужную identity/пригодность и фактическое SSH-подключение.

Дополнительный локальный Linux wire-oracle — **NOT_CHECKED**: среда вернула EPERM
при создании AF_UNIX socket до соединения/первого запроса. Эскалации/обхода не было,
временная пустая fixture directory очищена. Это не PASS и не execution failure
продукта. Заранее запущенный Windows CI с named-pipe/native ssh-add проверяется
отдельно; он не выдаётся за выполнение недоступного локального oracle.

Счёт **6/55**,23/31,49=37+12 и exact6 accepted сохранены; новых ID55 нет.
F31/F17/F31-R11 OPEN, R05 PARTIAL, Package PENDING, Live NOT_CHECKED.
PGA-03B/CI560 сохраняет прежнюю приёмку. Codex shortcut ownership, disabled caption
и причина исторического ssh-G556 UNKNOWN остаются findings. Устройства/службы/
ключи/ACL владельца не проверялись и не менялись; PR draft/unmerged,
без merge/release, VERSION0.2.2.
