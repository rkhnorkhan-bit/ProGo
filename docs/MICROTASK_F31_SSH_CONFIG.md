# Остаток55: F31-R04 — достоверная диагностика конфигурации OpenSSH

Один Code/Dev результат, разделённый на три малые микрозадачи. Исходный
checkpoint `ec9fa214d2a502fe3e4601ca31d5c00da234fa1d`; принятые5/55
подтверждены полным CI #548. До новой полной приёмки счёт **5/55**,
остаток50=38 до установщика +12 зависимых приёмок; общий **23/31**,
весь F31 **OPEN**. [Список55](REMAINING_SUBTASKS_8.md).

## Исходные дефекты и выбранная граница

- Ручной поиск алиаса читает только корневой ~/.ssh/config, не учитывает
  Include/system config/Host и Match полностью, negation и порядок правил.
  Вердикт «найден/не найден» способен противоречить фактическому OpenSSH.
- Эвристика точки в имени принимает dotted alias за прямой адрес. Сам по
  себе успех ssh -G также не доказывает существование алиаса, DNS или VPS.
- IdentityFile из -G — исходная строка конфигурации: она может оставаться
  шаблоном. Передача её в обычную проверку пути даёт ложный «не найден»
  либо предполагает неверный home/cwd/контекст клиента. Raw-key dictionary
  без учёта регистра также смешивает разные case-sensitive tokens.

Микрозадача1: authority native ssh -G вместо собственного Include/alias
parser. Отчёт показывает обработанную конфигурацию и эффективные параметры;
успех не объявляется доказательством alias/DNS/auth. Production Check
сохраняет существующую проверку агента; internal configuration-only helper
позволяет изолированные -F fixtures без agent inventory.

Микрозадача2: сохранять raw IdentityFile отдельно от подтверждённого пути.
Раскрытие — одношаговое и case-sensitive, только при достаточном контексте
native effective fields. Локальный home/user/cwd, ~other, переменные среды
и неподдерживаемые токены не угадываются. Если путь не доказан, результат
ключа — «не проверено», без File.Exists/Open по догадке. Подтверждённый
абсолютный локальный путь допускает проверку доступности без чтения содержимого;
UNC/device/drive-relative/root-relative также остаются NOT_CHECKED.
Это приёмка корректного диагностического поведения, а не обещание раскрыть
каждый токен любого клиента; неопределённый отдельный путь остаётся NOT_CHECKED.
Raw maps используют Ordinal. Ограничение списка32 IdentityFile явно видно.

Микрозадача3: meaningful native fixtures с actual Windows OpenSSH и temp -F:
Include/glob, порядок, wildcard/negation/Match/dotted alias, неизвестный
target, multiple IdentityFile/none, literal/токены/неопределённые пути.
Production result сравнивается с независимым native -G oracle; текущий
user config не заменяется. Старые cancel/deadline/tree ownership checks,
agent behavior, Desktop3851 и SOCKS41/403 условия/бюджеты сохраняются.

## Основания

[OpenBSD ssh -G](https://man.openbsd.org/ssh#G): команда выводит параметры
после обработки Host/Match. [ssh_config](https://man.openbsd.org/ssh_config)
задаёт first-value, Include и case-sensitive tokens/environment semantics.
Upstream [ssh.c V_9_6_P1](https://github.com/openssh/openssh-portable/blob/V_9_6_P1/ssh.c)
и Windows fork подтверждают: config dump/exit выполняется до
load_public_identity_files и раскрытия IdentityFile.
Windows home/user могут отличаться от HOME/Environment.UserName; простая
подстановка .NET переменных не считается доказательством клиентского пути.

Статус **IN_PROGRESS**. Source FROZEN +86/−83, независимый source review APPROVE;
tests FROZEN; независимый source/tests/script/PENDING review APPROVE.
Добавлены59 Check. Ожидаются Desktop3910=3851+59 и
неизменные SOCKS41/403. Raw и expanded path ограничены32768 символами ДО
append: повтор токенов не создаёт неограниченный StringBuilder. Малый fixture
проверяет rejection через production pipeline; allocation ceiling доказывает
source review, измерения памяти в этом fixture не заявлены.
Compiler явно включает новый partial, harness вызывает SshConfigAuthority;
устаревшие static markers двух удалённых эвристик заменены authoritative
SshResolved/CheckConfiguration/IdentityPaths/IdentityListLimited. Прежние
assertions и deadlines не ослабляются. Предыдущий CI #548 подтверждает только
прежний checkpoint; ожидаемые числа не являются новым PASS.
Локальные whitespace/public PASS:259 sources/0 release files; native Windows
не запускался в Linux контейнере, нужен свежий полный Windows/Ubuntu CI.
Package PENDING, Live NOT_CHECKED; устройства/ПК/VPS/телефон владельца
и установленное приложение не проверяются. PR draft/unmerged;
merge/release/реальные платежи, письма и сертификаты не выполняются.
