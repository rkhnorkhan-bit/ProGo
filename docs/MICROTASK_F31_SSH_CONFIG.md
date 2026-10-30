# Остаток55: F31-R04 — достоверная диагностика конфигурации OpenSSH

Один Code/Dev результат, разделённый на три малые микрозадачи. Исходный
checkpoint `ec9fa214d2a502fe3e4601ca31d5c00da234fa1d`; прежние5/55
подтверждены CI #548. F31-R04 принят **14.10.2026 UTC** в полном CI #549:
счёт **6/55**, остаток49=37 до установщика +12 зависимых приёмок;
общий **23/31**, весь F31 **OPEN**. [Список55](REMAINING_SUBTASKS_8.md).

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

## Приёмка Code/Dev

Статус **Code/Dev PASS**. Source FROZEN +86/−83, tests FROZEN;
независимые source/tests/compiler/static markers и полные native reviews
APPROVE. Compiler явно включает новый partial; harness вызывает
SshConfigAuthority. Устаревшие markers двух удалённых эвристик заменены
SshResolved/CheckConfiguration/IdentityPaths/IdentityListLimited.

Фактически **Desktop3910=3851+59 PASS**. Все59 новых checks выполнены
18:43:49.0718207–18:43:49.5872661 UTC: восемь независимых oracle/production
пар, first-value Include/glob/Match/negation, dotted и неизвестный target,
relative Include не рядом с -F, сохранение raw/order/case-sensitive maps,
пять подтверждённых путей, none и семь неподтверждённых записей,
drive-relative, root-relative и client-relative paths,35 native identities против явного cap32,
ограничение expanded path. Прежние3851 проверки, assertions и deadlines
сохранены. Старые SSH cancel/deadline/process-tree/unrelated-child и agent
inventory/guidance проверки повторно PASS; agent не проверяется новым helper.

Raw и expanded path ограничены32768 символами ДО append: повтор токенов
не создаёт неограниченный StringBuilder. Малый controlled fixture проходит
production pipeline и подтверждает bounded rejection; allocation ceiling
подтверждён source review. Измерение памяти этим fixture не заявлено.
Неопределённые отдельные пути остаются NOT_CHECKED, а не «файл не найден».

## Точная идентичность и полный CI

Functional source `1025eb2f47ee41d1b0cb0e356bb0e90caabc43f0`, tree
`ccd40b3f7852b69f2998ddb8c6b879eafd9e5d03`; tested merge
`36c5e2d97117e7e675dde065021cb62568d0eba3` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #549](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38616061130)
completed SUCCESS: Windows `116031324966`, Ubuntu `116031324494`, весь run.
Локальный эквивалент до синхронизации `aa04b60628ae183398c4c43189b9ab8eef1607fb`
имеет то же tree; удалённый functional source указан выше.

- Desktop3910 PASS18:44:28.5867561 UTC; полный Test-ProGo PASS18:47:05.3754031.
  SOCKS41 scenarios/403 assertions неизменны и повторно PASS18:37:50.
- Foundation165/134/325/110/211/151/274/112, expiry130/restart237,
  Instance26/Maintenance95/Shutdown47/update package5 PASS.
  HomeVPN975/portable90 PASS; обычные backup/update negative fixtures
  ожидаемо печатают injected failures, завершаются утверждённым PASS95.
- Windows Python22 PASS +2 прежних Linux ownership/flock module skips,
  покрытых Ubuntu; все7 HTTP tests выполнены. Public259 sources/44 release
  files PASS18:52:08.5312688. Native Windows не запускался в Linux контейнере.
- Ubuntu16/9/46 без skips, actual independent QR decode18:32:57.5081891 UTC.
  Реальный OpenSSH package upgrade13.19→13.20, managed sshd-t, ограничения,
  revoke/preserve/reissue, private receipts и real TLS PASS. Bootstrap server
  blob `a76ab265e5afbbf022f823b400fe44946bab3753` и fixture46 неизменны.
  Точный механизм прежней package/service остановки остаётся inference.

Root и независимые reviewers сверили completed logs, run/jobs и metadata.
Это новая полная приёмка точного source; прежний CI #548 остаётся историей5/55.
Runtime fallback/classifier, server/workflow и продуктовая компоновка не менялись.

## Артефакты и визуальная проверка

| Artifact | ID | Bytes | Подтверждение |
| --- | ---: | ---: | --- |
| Screenshots | 11920744276 | 6391189 | Uploaded; скачано,287 файлов, hash сверён |
| Diagnostics | 11919799744 | 82645 | Uploaded; metadata/runner digest совпали |
| Runtime folder | 11920289570 | 525800 | Uploaded;44 файлов, metadata/runner digest совпали |
| ZIP wrapper | 11920004751 | 518675 | Uploaded; metadata/runner digest совпали |

Inner CI runtime ZIP **521994 bytes**, runner18:52:13.0473404 UTC.
SHA-256 screenshots download/metadata/runner:
`9de0a0c7358363eaf0e2b51ccace2365f7d8c34a6ab9a9a4479c5025b2a52407`.
Diagnostics digest:
`da1d10c6836c4f9b556cdb7c343376e803104a0610cb9373c19fb597a002b755`;
folder digest:
`58e9c65b2118d70216a59b836e7ed84bd25507be0905217e8b3ef61620befb01`;
ZIP wrapper digest:
`18152bff4a7d1b2ee64d71ae5a74c99a463e540f13fe094997dcc781ec056675`.

Root визуально просмотрел свежие `ssh-diagnostic-success.png` и
`ssh-agent-guidance.png`: raw IdentityFile и проверяемый путь различимы,
текст, переносы, статус и действия читаемы; длинный отчёт прокручивается
вертикально. Это2 просмотренных PNG из287, не визуальная приёмка всего
интерфейса/DPI. Снимки используют controlled DesktopTests.exe fixture и его
test assembly identity0.0.0.0, не являются идентичностью shipping ProGo.exe.
Для остальных трёх artifacts отдельный download/hash не заявлен.

## Сохранённые границы

Принят только новый **F31-R04**: **6/55**, осталось49=37+12. Исходные55=43+12,
общий23/31 и целые F17/F31 OPEN сохранены. Реальные agent/key/VPS проверки,
прочие F31 результаты и цельный setup/updater всех31 не закрываются этим CI.
Package PENDING, Live NOT_CHECKED. ПК/VPS/телефон владельца и установленное
приложение не проверялись; PR draft/unmerged, merge/release/реальные платежи,
письма и сертификаты не выполнялись.

После functional source меняются ровно3 Markdown evidence-документа;
application/tests/scripts/server/workflow идентичны проверенному source.
Декомпозиция55 уникальных ID и6 PASS, whitespace/public проверены при
фиксации evidence. Независимый review evidence выполняется перед публикацией.
