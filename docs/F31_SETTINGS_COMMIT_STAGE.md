# F31k — согласованное сохранение настроек

Этап подготовлен от `a3322e8`. F31 остаётся **OPEN**: Windows compilation/runtime
и замеры Windows Rescue/HDD/VHDX для этого этапа ещё не выполнены.

## Причины и исправление

Ранее `SettingsService.Save` выполнял независимые atomic file replacements,
после чего отдельно присваивал `Current`. Два concurrent saves могли оставить
последний файл B и позднюю память A. `Normalize` также менял caller object до
успеха записи. `ProxyService.SelectWorkingProfile` менял live или устаревший
settings object перед сохранением: отказ записи мог менять память, а поздний
автовыбор — возвращать старые пользовательские параметры.

Один `SettingsService` теперь последовательно выполняет весь durable commit.
Сохраняется отделённая копия, нормализация не меняет caller. Только после
успешного `File.Replace`/`File.Move` публикуется пара settings/revision. Отказ
сохраняет предыдущую пару, файл и revision; собственный temporary file удаляется.
Пара публикуется одной memory reference. `Current`, `Revision` и `Capture` не ждут
дисковую блокировку. `Capture` получает согласованную revision и отдельный clone
из одной опубликованной пары. Это не обещание одновременного атомарного чтения
файла и памяти извне: до завершения commit readers могут видеть предыдущую
опубликованную память. После завершения writers файл и память согласованы.

SSH worker использует один snapshot для списка целей и запуска конкретного
процесса. Автовыбор сохраняется только при той же revision, выбранном профиле,
connection signature и SOCKS endpoint. Проверка и изменение только `SshProfile`
выполняются в том же settings commit gate. Любое более новое сохранение владельца
отклоняет старый автовыбор без повторной записи. Неудачный автовыбор не меняет
память и не записывается в журнал как успешный. Production `SetAutoRestart`
использует тот же commit с изменением только своего поля.

Settings gate не вызывает proxy, UI, listeners или пользовательские callbacks.
Единственный injected hook — test seam перед настоящим atomic replacement.
Proxy может вызвать settings commit под своим process gate; обратного callback
из settings gate нет. `Save` остаётся синхронным primitive. Асинхронное применение
из UI, single-flight формы и lifecycle владельца — отдельная интеграция F31j;
этот этап не меняет context/listeners/SettingsForm.

## Совместимость и границы

Из успешного первоначального load сохраняются неизвестные top-level JSON values,
включая вложенный payload и разные по регистру неизвестные имена. Known fields
пишутся в каноническом виде; повторяющиеся known/legacy имена без учёта регистра
по-прежнему отклоняются при load. Неизвестные поля не участвуют в case-insensitive
словаре миграционных флагов. Устаревшие `AutoApplyProxy`/`AutoCodexProxy` удаляются
при сохранении, как требовала существующая миграция; явный `AutoCliProxy=false`
не заменяется старым `true`. Вложенные known formats, field validation перед UI
apply, defaults/normalization, настройки PIN/vault и atomic file format не меняются.
Unknown values не выводятся в журналы.

Scope — один production instance `SettingsService` в существующем single-instance
владельце. Несколько сервисов/процессов, внешнее редактирование JSON во время работы
и слияние таких edits этим этапом не координируются. `Current` сохраняет прежний
reference API для совместимости; production consumers читают его, а изменения
передают clone через commit. Прямое изменение `Current` обходит revision и не
является transactional save. Independent options/test delegate constructors
`ProxyService` сохраняют совместимость; атомарная revision policy production
подключается через настоящий `SettingsService`.

Полное сохранение владельца остаётся last committed writer wins. Этот этап не
сливает два независимо отредактированных settings snapshots. Восстановление
предыдущих settings после сбоя других интеграций (`CliProxyBridgeService`) тоже
требует отдельного анализа stale rollback; успешный commit не доказывает успех
системной интеграции. Новый код не обращается к VPS и не меняет SSH-agent.

## Проверки

| Проверка | Доказательство | Статус |
| --- | --- | --- |
| Реальные concurrent saves и отсутствие file B/current A | `DesktopSettingsPersistenceTests.cs`: первый writer задержан перед настоящим atomic replacement, второй запускается отдельным worker; счетчик, bytes, complete settings и revision проверяются после settlement. | NOT_CHECKED — Windows CI. |
| UI и getters не ждут HDD gate | Native WinForms heartbeat и memory reads/Capture при задержанном writer; production proxy-to-settings gate проверяется отдельно с opportunistic status. | NOT_CHECKED — Windows CI. |
| Реальный отказ commit | `FileShare.None` на actual settings file: bytes/current reference/revision и caller values сохраняются, temporary file не остаётся. | NOT_CHECKED — Windows CI. |
| Stale autoswitch и fresh update only one field | Настоящий `SettingsService` и production `ProxyService` conditional wiring под real process gate; свежий user edit сохраняется, failed switch не публикуется, success меняет только profile. SSH не запускается. | NOT_CHECKED — Windows CI. |
| Transactional recovery preference | Production `SetAutoRestart`: blocked, denied и successful atomic file commits с сравнением всех settings fields. | NOT_CHECKED — Windows CI. |
| Unknown JSON/canonical known/legacy migration | Настоящий load/save: nested unknown payload, case-distinct unknown keys, canonical known output и удаление deprecated flags при explicit false. | NOT_CHECKED — Windows CI. |
| Документационные тесты/public content/diff | Локальные проверки исходников и текста. | PASS — docs 3; public content 178 source files; diff-check. |
| Windows Rescue/HDD/VHDX: physical disk activity, CPU/RAM и реальный UI | Требуется проверка установленной сборки владельцем; injected delays не являются physical benchmark. | NOT_CHECKED. |
