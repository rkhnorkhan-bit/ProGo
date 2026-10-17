# PGA-05C — effective политика метода publickey перед key checks

**17.10.2026 UTC. Узкий PGA-05C Code/Dev PASS по completed CI564.**
Родитель `6ca7cd41358fb68c3a798bbdd6c3e996cfbfd61d` (PGA-05B/CI563).
Микрошаг внутри F31-R11; **6/55**,49=37+12 и23/31 сохраняются.

## Найденный пропуск и результат

Диагностика учитывала effective IdentityAgent/IdentitiesOnly/IdentityFile, но не
PubkeyAuthentication/PreferredAuthentications. Поэтому даже профиль, отключающий
метод publickey, проверял файлы/агент и получал советы загрузить ключ/запустить
службу. Ещё inventory говорил «совпадение с ключом профиля не проверено» рядом
с уже выполненным matching PGA-05B. Это исправляется перед следующим key proof.

Только выбранный OpenSSH через прежний owned `-G` разрешает Include/Host/Match и
first-value precedence. Сохраняются два effective поля; config не разбирается
повторно, auth/tunnel arguments/settings не меняются.

Typed policy: Unknown/Disabled/Excluded/AllowedByClient. PubkeyAuthentication
no/false отключает метод. PreferredAuthentications — bounded exact comma tokens;
валидный список без точного publickey исключает этот метод. yes/true/unbound/
host-bound + publickey допускают лишь client attempt. AllowedByClient не Ready,
не private-key binding/signing/certificates/constraints/server auth proof.
Неизвестные/пропущенные/malformed/oversized поля не дополняются догадкой default:
остаются Unknown, прежние bounded read-only observations могут выполняться.
No/false и валидный исключающий список — независимые достаточные ограничения.

Локальный native OpenSSH `-G -F /dev/null` с explicit fixture target/loopback
подтвердил dump PubkeyAuthentication как false/true, хотя config использует no/yes.
Оба варианта учтены; Windows приёмка подтверждена CI564 ниже. Команда не соединялась с сервером.

## Реализация и UX

- Известные Disabled/Excluded пропускают разрешение/availability open IdentityFile,
  inventory ssh-add и fingerprint keygen. Новые NotUsedByAuthentication states
  не означают пустой агент, отсутствующий ключ или успешный другой метод.
- Cancellation проверяется до skip; unresolved конфигурация остаётся NotChecked.
  Policy scope определяется до fingerprint children. Shared read-only lease,
  capture/deadline/cleanup/primary-sidecar guards не менялись.
- SCM/service facts остаются отдельными. Program/startup/AutoStartSocks/order/
  ProxyService/интерактивный вход/служба/installer не меняются. Пропуск относится
  к диагностике выбранного метода, не к новым auth gates или отмене службы startup.
- Report показывает безопасное объяснение effective policy без raw unknown values.
  Для Disabled/Excluded не предлагает ssh-add/Start-Service/Set-Service и не
  заявляет выполненное matching. Для Allowed/Unknown прежняя совместимость
  сохранена; прямой ключ и другие методы не зависят искусственно от агента.
- Общий inventory теперь нейтрально говорит, что selected identities проверяются
  отдельно. Старый guard на отсутствие fingerprints/comments/ложной readiness
  сохранён; обновлён только ожидаемый исправленный текст.

## Dev/native матрица

Новый Desktop partial: recognized/no/false/true/host-bound/unbound; exact methods
без substring/case guesses/modifiers, missing/future/malformed/limits/duplicate;
safe display. Scope: skip при занятом lease без child, Unknown presence, cancel,
unresolved, Allowed/Unknown compatibility, independent IdentityAgent none,
snapshot до child, redaction unknown values и непротиворечивый inventory display.

Native Windows: реальный selected ssh-G читает только explicit temporary config
и Include с3 fixture aliases — disabled/excluded/allowed. Сравниваются actual
effective поля native oracle и production pipeline. Временный plain-text
IdentityFile удерживается FileShare.None: Disabled/Excluded не разрешает путь и
не проверяет файл; Allowed сохраняет обычную availability error. Полный diagnostic
pipeline при первых двух политиках завершает key phases NotUsed без key/agent
children. SCM Running там **injected boundary fixture**, не изменение службы.
Нет новых private keys/agent mutation/signing/askpass/server connect/ACL repair;
fixture content сохраняется, временные файлы удаляются. Реальная WinForms форма
показывает policy без неуместных unlock/service инструкций.

Новая enum inventory добавляет1 display check в прежний foreach states:
фактический marker стал67=66+1. Новый policy block —57 PASS по CI564.
Desktop4342=baseline4284+57+1; budgets/watchdog/старые
checks сохранены. Новый partial явно включён в compiler/workflow.
Локально нет Windows/.NET. Source review самостоятельный; независимого
agent review не было. Узкий Code/Dev scope принят по completed CI564 ниже.

## Открытые gates

PGA-05/06/F31-R11/F31/F17 OPEN; private-key binding/signing/constraints/custom
endpoint/interactive unlock/auth-startup coordinator/UI действия ещё впереди.
Этот policy шаг уточняет diagnostic scope, не закрывает пригодность ключа.
Все55 IDs/11 acceptance scenarios сохранены. Package PENDING, Live NOT_CHECKED,
VERSION0.2.2, draft PR88 без merge/release. ПК/VPS/телефон/установленная программа/
реальные ключи/ACL/платежи владельца не проверялись и не менялись.
CI562 FAIL нового synthetic fixture и UNKNOWN ssh-G556 остаются в истории;
Codex shortcut ownership/disabled caption остаются отдельными findings.

## Native Code/Dev приёмка: CI564 SUCCESS

Tested source `eccbe3cbf0ef55fc5528b66d1587e9c3772f1847`, tree
`039bca5ffccb33e5697685149d16f2f05b3afdd9`, merge
`e446bc916472139f9f4e082037320e7206ecc83c` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI564](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39036222485)
completed SUCCESS: Windows117406415468/Ubuntu117406415677 и весь run.
Оба checkout log подтверждают один merge/source/base; main — ancestor source.
Один полный run, без повторного запуска.

Новый block BEGIN13:31:08.2399765UTC;57 фактических PASS до marker
`SSH public-key policy checks PASS: 57`13:31:08.4452861UTC.
Новый inventory enum case дал ещё1 display check: marker67=66+1
13:31:07.7932281UTC. Desktop **4342=4284+57+1 PASS**13:31:42.7549354UTC.
Это58 дополнительных проверенных случаев, не58 новых ID плана.

Native policy observations: disabled13:31:08.2852258/Excluded13:31:08.3428333
с identities1/confirmedPaths0; AllowedByClient13:31:08.3997627 с
identities1/confirmedPaths1. Для первых двух полный pipeline сохранил отдельный
SCM result и пропустил key phases13:31:08.3050830/13:31:08.3622432UTC.
Allowed сохранил locked-file availability error13:31:08.3999198; content unchanged
13:31:08.3999572; actual diagnostic WinForms report13:31:08.4407269 PASS.
Реален выбранный ssh-G и temporary config/Include/locked plain file; SCM Running
здесь injected boundary. Новых private keys/agent mutation/signing/server connect
или ACL repair в этих fixtures нет. Разрешённая client policy не key/auth readiness.

Прежние37 SCM/32 bounded-probe,66 start/40 startup/52 matching checks PASS.
Actual read-only SCM oracle13:31:01.0386600UTC: runtime1/Stopped,startup4/Disabled,
statusError0/configError0. Старые66/40/52 markers —
13:31:05.8124778/13:31:07.6047405/13:31:08.2395792UTC.
Inventory parser/redaction/ownership guards сохранены;67 — прежние66 плюс новый
state. Изменён только старый expected inventory text, чтобы убрать противоречие
со selected-identity matching, не убрать redaction/readiness assertions.
PGA-05B synthetic envelope снова InvalidOutput/exit0/stderrPresent13:31:08.1373958;
это не encrypted unlock и не объяснение исходного CI562 FAIL.
Native fixture.dotted PASS13:31:08.4770005; причина исторического CI556 UNKNOWN.

SOCKS41/403 PASS13:25:57.9239967; Instance28 PASS13:31:48.9998838;
Maintenance95 PASS13:33:31.7591731; Shutdown47 PASS13:33:53.9637938;
updater5 PASS13:33:55.1276043; весь ProGo tests PASS13:33:55.1281088UTC.
HomeVPN975 PASS13:36:39.8321762; portable90 PASS13:37:44.7598817.
Windows Python24=22PASS+2 прежних Linux-only skips, OK13:37:46.2974816;
все7 HTTP profile-share cases ...ok; public289source/44release PASS13:37:46.8419916.
Ubuntu16 recovery/9 sharing/sync,46 provisioning без skips; actual
numpy2.2.6/zxing-cpp2.3.0 independent QR и полный recovery/revoke/reissue/TLS/session
tail до restart/session PASS13:22:53.1056595UTC.

Functional diff:12 файлов,350insertions/13deletions. Core/Program/ProxyService/
DiagnosticProcess/SCM/probe/startup/предыдущий matching partial/server/workflows/
installer идентичны родителю. Compiler script и Desktop main имеют по одной
вставке нового partial/workflow; прежние budgets/watchdog не менялись.
После tested source — только4 Markdown, runtime/tests/scripts/server/workflows
идентичны. Самостоятельный source/native review; независимого agent review не было.

## Логи и CI artifacts

Decoded UTF-8/LF с одной final newline:

| Job | Lines | Bytes | SHA256 |
| --- | --- | --- | --- |
| Windows117406415468 | 8834 | 959525 | `d360cd8e4589b313fb9e92e0fbccd802a296b9aeaefacf96ec8dcae4c46b0c8c` |
| Ubuntu117406415677 | 573 | 53125 | `6903a276ea156d03ce111e5d752c22fcb458b040cfc5e0b745331305562b36b7` |

Все4 artifact source head/run ID/upload ID/size/digest сверены между API и
полным upload log. Ниже внешний artifact ZIP:

| Artifact | ID | ZIP bytes | SHA256 |
| --- | --- | --- | --- |
| ProGo-wizard-screenshots | 12102616620 | 6539101 | `b568c2102b79e22da1bb74635f51136297610c201313780cd80d0a6cf0e41755` |
| ProGo-release-zip | 12102606843 | 536482 | `c63f7247835a121c891dde4ada997aaf910ee53425edb5915a0f9053448fd033` |
| ProGo-ci-diagnostics | 12102566716 | 93350 | `f9defb46c56c0945b7c7afd20b2af2656fde7696da769c80965cadeb8a1b28ca` |
| ProGo-release | 12102237582 | 544731 | `28fef03f0146262fb0b5db2e74d6e8f9ee4597f1f71aba0926eff18446da5824` |

Архивы не скачивались; новой локальной archive/hash/visual/DPI/contrast/Narrator
приёмки нет. Runner сообщает293 screenshot files; inner ProGo-release.zip539938bytes
отличается от внешнего artifact ZIP и не является итоговым installer31/31.
Package PENDING/Live NOT_CHECKED;6/55/23/31 и все открытые key/auth gates сохраняются.

## Первичные источники

- [OpenBSD ssh_config](https://man.openbsd.org/ssh_config): PubkeyAuthentication
  задаёт попытку public-key auth; PreferredAuthentications задаёт методы клиента.
  Ни разрешение попытки, ни порядок методов не подтверждают server acceptance.
- [OpenSSH sshconnect2.c](https://github.com/openssh/openssh-portable/blob/master/sshconnect2.c):
  publickey связан с pubkey_authentication; authmethod selection отдельно от
  authentication success. Диагностика не подменяет native negotiation.
