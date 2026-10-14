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

Статус **IN_PROGRESS / native PENDING**. Production3 files FROZEN,
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
