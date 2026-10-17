# Остаток55: F31-R05, пакет B — миграция меню в фоне

Одна ограниченная микрозадача **Code/Dev PASS 16.10.2026 UTC** по полному
CI #552. Исходный checkpoint `d2fe109937922203d7ee7f8d4012a9478c1879b3`
содержит ранее принятый пакет A. Счёт остаётся **6/55**, остаток **49=37+12**,
общий **23/31**. Весь F31-R05 **PARTIAL**, целые F17/F31 **OPEN**.
[Список55](REMAINING_SUBTASKS_8.md). Новый целый ID этим пакетом не закрыт.

## Что изменено

Program после instance.Attach планирует один task у instance owner. Worker
проверяет installed-путь до hook и discovery каталогов; portable owner получает
no-op task. Получение Programs/Startup, файловые операции и ShellLink COM
выполняются в worker после owner dispatch, без ожидания на UI. Secondary,
self-check и maintenance startup guards сохранены.

ShortcutMigrationWork включает очередь, реальный worker и finally cleanup.
Повторы возвращают тот же task. Shutdown отменяет последующие шаги и ждёт
полный task с ограничением времени до handoff. Timeout сохраняет owner
запущенным; после settlement допустим явный retry. Старый отдельный vault
drain budget сохранён: cancel миграции выполняется рано, её await — после vault.

Dispose отменяет queued reservation либо сигнализирует active worker.
Начатый COM/Move/Delete не объявляется физически прерываемым: token проверяется
перед следующими mkdir/COM Save/Move/legacy Delete. Task остаётся pending до
фактического завершения cleanup; успешный normal shutdown/handoff его не обгоняет.
При отмене между созданием primary и удалением legacy могут остаться оба
валидных ярлыка. Откат не удаляет primary. Replacement создаётся и проверяется
до удаления старого; customized/foreign ярлыки сохраняются. Старые public
MigrateMenu/MigrateForExecutable API и47 прежних shortcut checks сохранены.

## Точный source и проверка

Functional source `3d37630b562e42afa0fdb3ba8c73c5a3c34044f4`, tree
`df8e41e36432c0a38b4c2c1cc35628a88fa3b425`, parent
`d2fe109937922203d7ee7f8d4012a9478c1879b3`. Tested merge
`8a3ebf1c8fa3c2275b29bc0d60cd5d1aa8f4cd96` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #552](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38874943052)
completed **SUCCESS**: Windows `116887612160`, Ubuntu `116887612440`, весь run.

Source/tests/wiring прошли независимый review перед публикацией. Все7 blob
сверены с freeze, remote tree совпал с локальным. Production:3 files,88+/8−.
Root подтвердил: после удаления только нового main call и compiler item
содержимое двух старых файлов wiring эквивалентно исходному HEAD; прежние
условия и budgets не менялись. Новый partial явно подключён в Main/compiler.

Новый partial использует настоящие ShellLink в disposable Programs/Startup,
MTA worker, независимый COM reader target/arguments/workdir и реальную
ApplicationInstance IPC activation. Hook задерживает реальный pipeline перед
mutation, не заменяет его успехом. No-late утверждения относятся к этой
контролируемой границе, а не к прерыванию произвольного уже начатого OS I/O.

| Проверка | Фактический результат |
| --- | --- |
| Новые migration Check | **22 PASS**, 08:46:50.9716710–08:46:53.5196575 UTC: success7, held shutdown4, queued shutdown2, queued/running Dispose4, custom/foreign3, portable1, invalid path1 |
| Desktop | **3961=3939+22 PASS**, 08:50:11.1017549 UTC; UI heartbeat, task identity/100 повтора, raw pending deadline/retry и exact-byte guards |
| SOCKS | **41 scenarios /403 assertions PASS**, неизменны |
| Vault foundation | **165/134/325/110/211/151/274/112 PASS**, expiry130/restart237 |
| Instance / Maintenance / Shutdown / updater package | **26/95/47/5 PASS**; полный Test-ProGo PASS 08:52:55.7240656 UTC |
| HomeVPN / portable | **975/90 PASS**, 08:56:16.4486361 /08:58:03.1411740 UTC |
| Windows Python |24 reported = **22 PASS +2 прежних Linux module skips**; все7 HTTP ProfileShareTest cases выполнены |
| Ubuntu | **16 operations /9 sharing /46 provision PASS**, без skips; independent QR decoder с numpy2.2.6/zxing-cpp2.3.0 |
| Public content | **264 source files /44 release files PASS** |

Root и независимые reviewers сверили completed logs и job metadata. Полный
Windows log после нормализации LF:884918 chars/8435 lines, UTF-8 911967 bytes,
SHA-256 `d90f7d88f8e3b10001f2e6abe1f362e92888cfc9287654ed049b0ec7f07b5c5f`.
Ubuntu log53875 bytes, SHA-256
`80c3c2a35b7e3b497b8b4496a1e183c45fe5491e1ca320112dcf54c0df3e38f9`.
Server/workflow/Ubuntu fixtures идентичны исходному checkpoint; прежний A
CI #551 и FAIL #550 остаются самостоятельной историей, без ретроактивного PASS.

## Артефакты и границы приёмки

| Artifact | ID | ZIP bytes | SHA-256 metadata/runner |
| --- | --- | ---: | --- |
| ProGo-ci-diagnostics | 12035811904 | 84327 | `4d2f80774f472d604c3e88f43bbaefe1813db2305030cae7a31f333a651e6cc5` |
| ProGo-release | 12035831921 | 529561 | `c5c8ddc31b28812a57357dd4c9e8b741320d0b2398d2e12f8b886d98774dbee2` |
| ProGo-release-zip | 12036315419 | 521730 | `2dcde47f79f5860d319753010cff536e66f7a8d0a510cfcff5cdb74d448cb715` |
| ProGo-wizard-screenshots | 12035851808 | 6439073 | `49c796fdab8ce14b00c6639ae4bd4c180204ff38191596c7b2a117d31b6cdc3f` |

Все4 размера/digest/head сверены с metadata и runner upload log.
Runner сообщает289 screenshot files и44 runtime files. Inner runtime
ZIP524992 bytes, wrapper521730. Отдельный download/hash/visual review этих
артефактов в пакете B не заявлен. Layout/theme не менялись; нового
visual/contrast/DPI/Narrator PASS нет. Открытое замечание F17-R01 по тусклой
disabled startup подписи из пакета A сохраняется.

R05 остаётся PARTIAL: ранние AppPaths/SettingsService/Load, получение
Programs/Startup/UserProfile в constructor формы, scoped Codex shortcut
mutations и запуск Codex/PowerShell требуют отдельных микрозадач.
Фоновая миграция не означает, что весь startup и весь filesystem уже вне UI.

После tested source сохраняются ровно3 Markdown evidence-документа;
application/tests/scripts/server/workflow остаются идентичны tested source.
Исходные55=43+12, прежние6 принятых ID и утверждённые требования сохранены.
Package PENDING, Live NOT_CHECKED; цельный setup/updater всех31 впереди.
ПК/VPS/телефон и установленное приложение владельца не проверялись.
PR draft/unmerged; merge/release и боевые операции не выполнялись.
