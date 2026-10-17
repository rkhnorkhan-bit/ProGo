# PGA-05C — effective политика метода publickey перед key checks

**17.10.2026 UTC. Код/dev fixtures готовы; native CI PENDING.**
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
Оба варианта учтены; на Windows ещё требуется CI. Команда не соединялась с сервером.

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
старый marker ожидаемо станет67=66+1, а не ослабнет. Отдельный policy block
посчитается по фактическому CI. Desktop baseline4284; budgets/watchdog/старые
checks сохранены. Новый partial явно включён в compiler/workflow.
Локально нет Windows/.NET. Source review самостоятельный; независимого
agent review не было. Code/Dev приёмка PENDING до completed CI.

## Открытые gates

PGA-05/06/F31-R11/F31/F17 OPEN; private-key binding/signing/constraints/custom
endpoint/interactive unlock/auth-startup coordinator/UI действия ещё впереди.
Этот policy шаг уточняет diagnostic scope, не закрывает пригодность ключа.
Все55 IDs/11 acceptance scenarios сохранены. Package PENDING, Live NOT_CHECKED,
VERSION0.2.2, draft PR88 без merge/release. ПК/VPS/телефон/установленная программа/
реальные ключи/ACL/платежи владельца не проверялись и не менялись.
CI562 FAIL нового synthetic fixture и UNKNOWN ssh-G556 остаются в истории;
Codex shortcut ownership/disabled caption остаются отдельными findings.

## Первичные источники

- [OpenBSD ssh_config](https://man.openbsd.org/ssh_config): PubkeyAuthentication
  задаёт попытку public-key auth; PreferredAuthentications задаёт методы клиента.
  Ни разрешение попытки, ни порядок методов не подтверждают server acceptance.
- [OpenSSH sshconnect2.c](https://github.com/openssh/openssh-portable/blob/master/sshconnect2.c):
  publickey связан с pubkey_authentication; authmethod selection отдельно от
  authentication success. Диагностика не подменяет native negotiation.
