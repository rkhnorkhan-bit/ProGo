# Остаток55: F31-R05, пакет A — автозапуск и отметка трея в фоне

Две ограниченные микрозадачи внутри одного ещё не завершённого F31-R05.
Исходный checkpoint `2141498f412e750b74c9685a475baf8167913f7d`; ранее принятые
**6/55** подтверждены полным CI #549. Счёт остаётся **6/55**, остаток49=37 до
установщика +12 зависимых приёмок; общий **23/31**, весь F31 **OPEN**.
[Список55](REMAINING_SUBTASKS_8.md). Новый целый ID этим пакетом не закрывается.

## Две найденные задержки и исправления

1. Конструктор SshProfilesSettingsForm вызывал LoadValues→ReadStartup,
   читая filesystem и ярлык через COM до показа окна. Теперь LoadValues
   устанавливает только memory values и понятное pending состояние. Один
   owned worker запускается после Shown; autoLaunch недоступен до результата,
   остальные настройки можно редактировать и сохранять. Неполученный либо
   неуспешный snapshot не попадает в ChangeStartup: unchecked default не
   становится командой удалить ярлык. Existing StartupSnapshot/ChangeStartup
   CAS сохраняется. Результат во время Save откладывается; при ошибке Save
   применяется только startup state, без повторного LoadValues и перезаписи
   draft. Успешное сохранение/Close/Dispose запрещают позднее обновление.
   StartupWork наблюдает реальный read до возврата и заканчивается без живого
   UI pump после закрытия владельца. Системный COM read не объявляется
   прерываемым; cancellation/generation запрещают запуск и позднюю публикацию.
2. DashboardClosing синхронно сохранял полную копию settings на UI. Теперь
   отдельный TrayPreferenceWork выполняет field-only MarkTrayCloseExplained:
   latest publication клонируется внутри существующего commitGate, меняется
   только этот flag, unknown JSON сохраняется. Повторное закрытие не создаёт
   параллельных writers; SettingsWork/IntegrationWork не заменяются. Ошибка
   наблюдается и допускает повтор при следующем закрытии. Shutdown/handoff
   bounded-await именно raw task и отказывают при незавершённой записи;
   новые writers при подготовке выхода запрещены. Dispose отменяет ожидающую
   запись без блокировки UI; token проверяется до I/O и после beforeReplace
   непосредственно перед atomic replacement. Уже выполняющийся OS Replace
   не объявляется прерываемым, нормальный handoff ждёт завершения task.

## Проверка и границы

Функциональная Code/Dev приёмка **двух микрозадач пакета A — PASS** по
полному CI #551 **14.10.2026 UTC**. Весь F31-R05 остаётся **PARTIAL**;
новый ID55 не закрывается. Production3 files FROZEN, source/tests/compiler/
PENDING и полные native reviews APPROVE: form +75/−13; Core+Context +32/−4.

Фактически **Desktop3939=3910+29 PASS**, SOCKS41/403 неизменны и PASS.
Все29 новых Check выполнены19:43:15.3993677–19:43:22.4361572 UTC:
startup17 (pending7/deferred4/read-failure2/Close2/Dispose2), tray12
(latest merge+shutdown7/failure+retry3/Dispose2). Старые3910 условия/budgets
сохранены;7 startup readiness waits и1 tray tracked-task wait заменяют только
прежнюю предпосылку синхронного результата. Оба старых файла после удаления
этих wait-only вставок эквивалентны исходным; Check18/48 остались прежними.
Compiler/main явно подключают оба partial. Новые2 Shot дают289=287+2.

В свежем CI #551 оба Close/Dispose INFO фактически показывают:
preNoticeSame=False, postNoticeSame=True, checked=False, checkedSame=True,
linkSame=True, settingsSame=True, snapshotNull=True, eventsStable=True.
Это подтверждает изменение getter при teardown и отсутствие поздних эффектов
в новом прогоне. Точный false operand старого #550 не был залогирован и
по-прежнему не назначается задним числом. История FAIL сохранена ниже.

F31-R05 **PARTIAL**: ранние Program.Main bootstrap/settings/shortcut migration
до Application.Run и scoped shortcut/launch операции на UI ещё впереди.
Фактическая responsiveness всей installed программы и Rescue/HDD/agent/VPS
этим ограниченным пакетом не принимается. Требования и исходные55=43+12
сохранены. Package PENDING, Live NOT_CHECKED; ПК/VPS/телефон владельца и
установленное приложение не проверяются. PR draft/unmerged;
merge/release/боевые платежи/письма/сертификаты не выполняются.

## Первая native попытка: CI #550 — FAIL

[CI #550](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38622998398),
source `3a3d5df78c5b4657a7654414e41f96167bc1995a`, tree
`ead96c4a443e9c1e494c929875d6c968d16f592b`, tested merge
`63311b7cac59e196d232308854ff4866e3b4b286` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
Windows `116054994324` completed FAILURE; Ubuntu `116054994273` SUCCESS.
Полный Windows log369323 chars/3900 lines, первый FAIL **19:29:34.7605387 UTC**:
`startup Close ignores late completion and preserves both saved files`.

Достигнуты14 новых startup PASS: pending7, deferred4, read-failure2,
первая Close/cancellation проверка1. Следующий combined Check FAIL;
Dispose2 и все12 tray checks не достигнуты. Desktop3939/overall ProGo PASS,
Instance/Maintenance/Shutdown/update package и relay/Python/public/ZIP этапы
не достигнуты и не получают PASS этой попытки. SOCKS41/403 повторно PASS
19:26:10.032 UTC. Ubuntu16/9/46 без skips, real QR19:21:50.7328971 UTC.
Uploaded partial screenshots58 files `11924275466` (1133632 bytes) и diagnostics
`11923930589` (35244 bytes); runtime folder/ZIP не созданы. Это не полный
визуальный результат289. Отдельные download/hash/visual этого FAIL не заявлены.

Старый combined Check не вывел, какое из четырёх условий было false;
этому запуску нельзя приписать измеренное изменение файлов или поздний
callback. Reader удерживался перед token check; уже прошедшая проверка
подтвердила отмену до release. Source review не нашёл здесь пути записи файлов
или публикации snapshot после отмены. Обнаружен неверный временной baseline
теста: текст до Close сравнивался с getter уже disposed child control.
Microsoft [Control.cs](https://github.com/dotnet/winforms/blob/main/src/System.Windows.Forms/System/Windows/Forms/Control.cs)
показывает native WindowText и иной teardown/cache path; это объяснение
возможного расхождения, а не измеренный false operand CI #550.

Узкая коррекция только нового startup fixture: snapshot UI после
Close/Dispose при всё ещё удержанном worker gate; затем отсутствие изменений
после release. Сохраняются !checked, cancellation/no-pending, оба exact-byte
сравнения с файлами ДО Close, прежние budgets и29 Check. Добавлены null
startupSnapshot и отсутствие новых TextChanged/CheckedChanged, безопасные
Boolean diagnostics без private bytes/путей/текста. Production3 files и
остальные tests/compiler не меняются. Нужен новый полный CI точного source;
результат550 не переименовывается в PASS задним числом.

## Точная идентичность и завершённый CI #551

Functional tested source `9959ce782743b766255336b315aec9a4f2d2a77b`, tree
`0b748966bc29e31b6c20e96f64a540a3bc2db85b`; tested merge
`84aa0a99e324af0738f0b60b688e2220d782c4c2` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #551](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38624788536)
completed SUCCESS: Windows `116061076420`, Ubuntu `116061075819`, весь run.
Production/other tests/compiler совпадают с source первой попытки550;
коррекция изменила только новый test oracle и PENDING историю отказа.
Локальные эквиваленты до синхронизации: initial source
`9a5d5213348c6cac32a9482f147586a4f7055d13`, correction
`745cafd8726cc7246651d08436d7ac63c2532b7d`; их trees совпадают с remote.

- Desktop3939 PASS19:46:26.0946326 UTC; полный Test-ProGo PASS19:49:08.4770958.
  SOCKS41/403 PASS19:39:20.112; все прежние R04/SSH agent/cancel/deadline/tree/
  unrelated-child/CAS проверки повторно PASS.
- Foundation165/134/325/110/211/151/274/112, expiry130/restart237,
  Instance26/Maintenance95/Shutdown47/update package5 PASS.
  HomeVPN975/portable90 PASS. Отрицательные maintenance fixtures ожидаемо
  печатают injected failures; suite завершилась PASS95, это не отказ CI.
- Windows Python22 PASS +2 прежних Linux ownership/flock module skips,
  покрытых Ubuntu; все7 HTTP tests выполнены. Public262 sources/44 release
  files PASS19:54:09.3362659. Native Windows не запускался в Linux контейнере.
- Ubuntu16/9/46 без skips; actual independent QR19:34:28.9416252 UTC.
  OpenSSH package13.19→13.20, managed sshd-t/restrictions/revoke/preserve/
  reissue/private receipts/firewall/real TLS/session expiry PASS. Server blob
  `a76ab265e5afbbf022f823b400fe44946bab3753` и fixture46 неизменны.
  Точный механизм прежней package/service остановки остаётся inference.

Root и независимые reviewers сверили completed logs, run/jobs и artifact
metadata. Повторная приёмка точного source551 не отменяет FAIL550.

## Артефакты и ограниченный визуальный просмотр

| Artifact | ID | Bytes | Подтверждение |
| --- | ---: | ---: | --- |
| Screenshots | 11923384928 | 6438755 | Скачано,289 файлов; hash совпал с metadata/runner |
| Diagnostics | 11923454997 | 83663 | Uploaded; metadata/runner digest совпали |
| Runtime folder | 11925050128 | 527994 | Uploaded;44 файлов, metadata/runner digest совпали |
| ZIP wrapper | 11923484945 | 520266 | Uploaded; metadata/runner digest совпали |

Inner CI runtime ZIP **523627 bytes**, runner19:54:13.4365469 UTC.
SHA-256 downloaded screenshots/metadata/runner:
`3d84625badf4e9b551c6883c19a3b30923071fe2056e0c963535532d3c665b53`.
Diagnostics digest:
`86f383e462a907e0e505afabd1bfb449d724bdd0522539fe8e23ca289e35673e`;
folder digest:
`be92f71e805c24b641c8e5589c9964c46f9ba2a9da768841020ee69edc0b8fc1`;
ZIP wrapper digest:
`477ed7155dcd1dcd4377b1096ca34297022da55cf37a7156c0c4912512f3c4ff`.
Для этих трёх artifacts отдельные download/hash не заявлены.

Root просмотрел3 свежих PNG: `settings-startup-pending.png`,
`settings-startup-read-failed.png`, `settings-startup-minimum.png`.
Инструкции pending/error читаемы, Save/Cancel доступны; длинный контент
прокручивается отдельно от нижних действий. Минимальный снимок показывает
несохранённый checkbox draft при прежнем registration notice — ожидаемый
контракт «после Сохранить», не доказательство уже применённого автозапуска.

**Визуальное замечание, пока открыто:** disabled startup checkbox caption
тусклый на тёмном фоне в pending/error. Это исходный CheckBox style; перенос
worker сделал эти состояния явно наблюдаемыми. Исправление учитывается в
открытой полноте F17-R01; ранее принятый F17-R02 относился к кнопкам, не ко
всем disabled CheckBox. Контраст не измерялся; полный visual/DPI/High Contrast/
Narrator PASS не заявлен. Просмотрены3 из289 PNG, остальные286 здесь не приняты.

Приняты только2 микрозадачи внутри F31-R05. Счёт **6/55** и остаток49=37+12
сохранены; accepted IDs остаются F17-R02/R03 и F31-R01/R02/R03/R04. Ранние
bootstrap/settings и scoped shortcuts/launch впереди. После tested source
меняются ровно3 Markdown evidence-документа; application/tests/scripts/
server/workflow идентичны проверенному source. Декомпозиция55 уникальных ID,
6 PASS, whitespace/public проверяются при фиксации; evidence проходит
независимый review перед публикацией. Package PENDING, Live NOT_CHECKED.
