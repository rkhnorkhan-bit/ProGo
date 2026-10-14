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

Статус **IN_PROGRESS / native rerun PENDING**. Первая попытка CI #550 FAIL;
точная достигнутая граница зафиксирована ниже. Production3 files FROZEN,
независимый source review APPROVE: form +75/−13; Core+Context +32/−4.
Добавлены29 Check: startup17 и tray12. Ожидаются Desktop3939=3910+29,
неизменные SOCKS41/403 и289 screenshots=287+2. Compiler и main явно
подключают оба новых partial. Deterministic native gate/pulse fixtures для responsive pending,
Save/cancel/close/deferred/error, latest merge и unknown fields, duplicate
writer, shutdown timeout/settlement и Dispose. Старые assertions и budgets
не ослабляются; семь ожиданий startup и одно ожидание tray read-after-write меняются на semantic
readiness нового tracked task. До свежего полного CI baseline Desktop3910 и
SOCKS41/403 не считаются результатом этого пакета.

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
