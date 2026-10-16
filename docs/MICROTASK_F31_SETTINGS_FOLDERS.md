# Остаток55: F31-R05, пакет C — каталоги формы в фоне

Одна ограниченная микрозадача **Code/Dev PASS 16.10.2026 UTC** по полному
CI #554. Исходный checkpoint `d6b48893d8b67a0de8b7601d5658cbf8f36f7515`
содержит ранее принятый пакет B. Счёт остаётся **6/55**, остаток **49=37+12**,
общий **23/31**. Весь F31-R05 **PARTIAL**, целые F17/F31 **OPEN**.
[Список55](REMAINING_SUBTASKS_8.md). Новый целый ID этим пакетом не закрыт.

## Исправление и границы

Programs/Startup и default startup manager больше не получаются в constructor
SshProfilesSettingsForm. Constructor сохраняет только captured inputs и чистый
installed/portable guard. После Shown существующий StartupWork получает
каталоги, создаёт manager и читает реальный ShellLink snapshot в worker.
Injected shortcuts не требуют default Programs/Startup discovery; portable
по-прежнему сразу показывает установленную границу автозапуска.

Diagnostic UserProfile и путь SSH config определяются отдельным ProfilePathWork
после Shown, с понятными pending/error состояниями и accessible description.
Задержка или ошибка одного worker не блокирует другой либо Save.
Пустые, relative, root-relative и drive-relative пути не превращаются в guessed
cwd path. Абсолютные redirected UNC корни допускаются без дополнительных
filesystem probes; реальный сетевой UNC-маршрут этим не принимается.

Manager и его StartupSnapshot публикуются либо откладываются одной парой на UI.
Save до получения snapshot оставляет регистрацию без изменений; deferred
результат после ошибки Save не сбрасывает draft. Новый retry использует
snapshot с тем же Owner. Старый public constructor с5 параметрами сохранён,
internal8 нужен для captured inputs/provider в native fixtures.

Close/Dispose отменяют публикацию по owner/generation/token. Оба task остаются
pending до возврата фактического provider/COM, затем завершаются без живого UI
pump. CTS освобождается после обоих tasks. Начатая операция Windows не
объявляется физически прерываемой. Это чтение/diagnostic discovery, не перенос
раннего AppPaths.Root/LocalAppData bootstrap или всех операций программы.

## Точная идентичность и полный CI #554

Tested functional source `de58a08847bbc2c3228b70d436fef175994d2776`, tree
`1007441dc92f8df9fccc62b71f45ac38e26d9180`, parent
`3b2bf894434ae47796f949b36385e5c10f2a4f6e`. Tested merge
`23cbd018a9d5869d1b5296e06505829f11d56df0` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #554](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38881063456)
completed **SUCCESS**: Windows `116907636680`, Ubuntu `116907636293`, весь run.

Production1 file85+/18−, form blob
`7a49dc7d175aa99afc2433c85f621316a5604577`; после первой попытки не менялся.
Final test partial `f33c3e4442827970458c563e2af6d58842afb439`.
Source/tests/wiring/correction package прошли независимый review до публикации.
Все blob и remote tree сверены с freeze. Root проверил, что после удаления
только нового main call и compiler item оба старых wiring-файла эквивалентны
исходному HEAD; прежние условия и budgets сохранены. Partial подключён явно.

Новые fixtures используют настоящий временный .lnk и folder provider,
задерживающий реальный pipeline, без подмены успешного snapshot. Проверяются
сохранение и повтор через production DefaultSaveAsync/ChangeStartup, согласованная
manager/snapshot пара, heartbeat и независимость двух held workers.
No-late проверки относятся к контролируемой границе provider до дальнейшей
работы; их нельзя переносить на прерывание любого уже начатого OS I/O.

| Проверка | Фактический результат |
| --- | --- |
| Новые folder Check | **30 PASS**, 09:40:48.5199399–09:40:59.4874022 UTC: pending5, default deferred pair/retry4, independent profile3, invalid/failure10, Close/Dispose6, injected/portable2 |
| Desktop | **3991=3961+30 PASS**, 09:44:20.0676175 UTC |
| SOCKS | **41 scenarios /403 assertions PASS**, неизменны |
| Vault foundation | **165/134/325/110/211/151/274/112 PASS**, expiry130/restart237 |
| Instance / Maintenance / Shutdown / updater package | **26/95/47/5 PASS**; весь Test-ProGo PASS09:47:05.5101706 UTC |
| HomeVPN / portable | **975/90 PASS**, 09:50:20.3544300 /09:52:05.3984203 UTC |
| Windows Python |24 reported = **22 PASS +2 прежних Linux module skips**; все7 HTTP ProfileShareTest cases выполнены |
| Ubuntu | **16 operations /9 sharing /46 provision PASS**, без skips; independent QR decode с numpy2.2.6/zxing-cpp2.3.0 |
| Public content | **266 source files /44 release files PASS** |

Свежий INFO #554 в09:40:51.1223385 UTC фактически показывает:
dialogOk=True, linkMissing=True, savedEndpointSame=True,
disposedEndpointSame=False, pathReadOnly=True. Новая проверка сравнивает
persisted данные с draft ДО Close; все три функциональных условия проходят.
Это измерение554, не назначение точного false operand старой попытке553.

Полный Windows log после нормализации LF:889182 chars/8465 splitlines,
UTF-8 916231 bytes, SHA-256
`d3b26bb06624734aea082130984eae53083874b178d37c9674db71a915f074cb`.
Ubuntu53658 bytes, SHA-256
`cb8209523f2ae0c6e69d277613fa280ff42673146207fe4bc1f09d48a93ea6d9`.
Root и reviewers сверили completed logs/run/jobs. Server/workflow/Ubuntu
fixtures идентичны исходному checkpoint. История A/B и CI #550/#551/#552
сохранена отдельно.

## Первая попытка CI #553 — FAIL

[CI #553](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38879266336):
source `3b2bf894434ae47796f949b36385e5c10f2a4f6e`, tree
`8038162b56d056ce69f7a13c60c468c99d09f561`; merge
`a8cc554c7c573ee98ca1a5ff6232ae9938dad562` в тот же main.
Windows `116901741841` FAILURE, Ubuntu `116901742090` SUCCESS, весь run FAIL.
Полный Windows log376376 UTF-8 bytes/3950 lines, SHA-256
`5a8b6306af1a5e16339baa8370baa717c187ee052e5ad7059a936b07f5ace372`.

Первый FAIL09:25:26.0074457 UTC: real Save uses the atomically discovered
shortcut manager and snapshot to remove only the chosen login registration.
До него прошли11 новых Check: pending5, deferred4, первые2 independent profile.
Failing12 и оставшиеся18 не дают PASS; Desktop3991 summary,
Instance/Maintenance/Shutdown/updater/full ProGo, HomeVPN/portable/Python/public
и ZIP не достигнуты. SOCKS41/403, старые startup17/tray12/migration22 прошли.
Ubuntu16/9/46 без skips и actual QR PASS.

Compound сравнивал DialogResult OK, отсутствие .lnk и settings endpoint
с endpoint.Text после Close/Dispose, при этом поле получило native handle после
перехода в Diagnostics. Точный false operand553 не залогирован и неизвестен.
Сравнение persisted данных с уничтоженным control — неверный oracle независимо
от того, какое условие отказало. Коррекция только нового fixture: во всех4 новых
Save-сценариях expected endpoint захватывается ДО Save/Close. Результат Save,
удаление .lnk, exact registration bytes и draft условия сохранены; добавлены
только безопасные Boolean INFO. Все30 Check и budgets сохранены.
Production/compiler/main не менялись. Новый полный554 подтвердил исправленный
oracle; его fresh booleans не назначаются истории553 задним числом.

Uploaded partial screenshots `12037625264`:59 files/1155919 bytes;
diagnostics `12037326139`:36792 bytes. Runtime folder/ZIP не созданы.
Download/hash/visual этих partial artifacts не заявлен; это не291 итоговый
снимок. Исторический553 остаётся FAIL.

## Артефакты #554 и ограниченный visual review

| Artifact | ID | ZIP bytes | SHA-256 metadata/runner |
| --- | --- | ---: | --- |
| ProGo-ci-diagnostics | 12038546214 | 85033 | `c1138c006176f2f67882e062f80eb0c5a5ea8f567d1099f1778be4ba3d40141c` |
| ProGo-release | 12038506411 | 531171 | `d0da945e6f28a43276f26aa724495645706afceb3ce9e598841f0b63d403be5c` |
| ProGo-release-zip | 12038009113 | 523380 | `f82995d26c4ee5f795f405f2d4b34335125bdc177a946d795ef9b76cb7191ffd` |
| ProGo-wizard-screenshots | 12037899271 | 6486463 | `73b0cf2dc5109a10003a2e7763097cd3eaa9217d7f1a0fe826780fbdf22a0859` |

Все4 size/digest/head сверены с metadata и runner upload log.
Screenshot ZIP скачан:6486463 bytes,291 entries; SHA-256
`73b0cf2dc5109a10003a2e7763097cd3eaa9217d7f1a0fe826780fbdf22a0859`
совпал. Root просмотрел settings-folders-pending.png и
settings-folders-failed.png: подпись поля, pending/error текст и Save/Cancel
читаемы и доступны в показанном размере; остальные поля и draft не исчезли.
Остальные289 PNG и полный contrast/DPI/Narrator здесь не приняты. Открытое
F17-R01 замечание по disabled startup подписи из пакета A сохраняется.

Inner CI runtime ZIP526649 bytes, wrapper523380. Diagnostics/folder/ZIP metadata
и runner digests сверены; отдельный download/hash этих трёх не заявлен.
Это CI runtime, цельный setup/updater всех31 ещё впереди.

## Открытый остаток

R05 остаётся PARTIAL: ранние AppPaths/SettingsService/Load и scoped Codex
shortcut mutations/launch, PowerShell launch/resolve, открытие Windows settings
требуют отдельных микрозадач. Read-only source review в src/ProxyAutomation.cs
подтвердил риск CodexProxyService: Enable проверяет marker launcher, но сохраняет
ShellLink поверх ShortcutPath; Disable при owned launcher удаляет существующий
.lnk без fresh target/arguments ownership проверки. Сохранение customized/foreign
Codex .lnk пока не доказано и не заявлено. Этот код и реальные пользовательские
ярлыки при разборе пакета C не менялись; исправление/native fixture впереди.

После tested source сохраняются ровно3 Markdown evidence-документа;
application/tests/scripts/server/workflow идентичны tested source.
Исходные55=43+12, прежние6 принятых ID и утверждённые требования сохранены.
Package PENDING, Live NOT_CHECKED. ПК/VPS/телефон и установленное приложение
владельца не проверялись. PR draft/unmerged; merge/release и боевые операции
не выполнялись.
