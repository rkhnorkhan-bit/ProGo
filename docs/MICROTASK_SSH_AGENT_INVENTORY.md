# PGA-05A — структурированный read-only inventory SSH-agent

**17.10.2026 UTC. Candidate, native CI PENDING.** Родительский checkpoint
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
- Полный bounded capture: <=128KiB на поток, <=256 identities, положительный bit
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

## Проверки candidate

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
watchdog/budgets не менялись. Локально нет Windows/.NET runtime, native приёмка
только по будущему CI. Самостоятельный source review, без независимого agent review.

## Первичные источники

[OpenSSH ssh-add manual](https://man.openbsd.org/ssh-add) — -l/-E и exit0/1/2;
[list_identities](https://github.com/openssh/openssh-portable/blob/master/ssh-add.c) —
точный empty stdout/failure stderr/fingerprint shape;
[ssh_config](https://man.openbsd.org/ssh_config) — IdentityAgent overrides environment,
none отключает агент, IdentitiesOnly ограничивает выбор. Compatibility подтверждается
native CI, а не выводится из текущей версии manpage.

Счёт **6/55**,23/31,49=37+12 и exact6 accepted сохранены; новых ID55 нет.
F31/F17/F31-R11 OPEN, R05 PARTIAL, Package PENDING, Live NOT_CHECKED.
PGA-03B/CI560 сохраняет прежнюю приёмку. Codex shortcut ownership, disabled caption
и причина исторического ssh-G556 UNKNOWN остаются findings. Устройства/службы/
ключи/ACL владельца не проверялись и не менялись; PR draft/unmerged,
без merge/release, VERSION0.2.2.
