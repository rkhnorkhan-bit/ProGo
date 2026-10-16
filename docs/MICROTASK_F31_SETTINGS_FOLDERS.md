# Остаток55: F31-R05, пакет C — каталоги формы в фоне

Одна ограниченная микрозадача после пакета B, исходный checkpoint
`d6b48893d8b67a0de8b7601d5658cbf8f36f7515`. Новый native CI и review
пока **PENDING**. Счёт остаётся **6/55**, остаток **49=37+12**, общий
**23/31**; весь F31-R05 **PARTIAL**, целые F17/F31 **OPEN**.

Programs/Startup для default installed startup manager и UserProfile для
диагностического SSH config получаются в независимых owned workers после
Shown. Constructor сохраняет только чистые captured inputs и portable guard;
injected shortcuts обходят discovery Programs/Startup. Старый public
constructor с5 параметрами сохранён. Pending/failed startup не меняет
регистрацию при сохранении остальных настроек. Manager/snapshot публикуются
или откладываются одной парой на UI, сохраняя draft и save/close generation.

Diagnostic path имеет отдельный pending/error результат; его задержка или
ошибка не блокирует startup и Save. Empty/relative каталоги не превращаются
в guessed cwd path. Close/Dispose запрещают позднюю публикацию, но task
остаётся pending до возврата реального provider/COM; никакой физической
прерываемости начатой операции Windows не обещается. CTS освобождается после
обоих tasks. Новые tests используют настоящий temp ShellLink и held provider.
Новых Check ожидается30: pending5, default deferred pair/retry4, independent
profile3, invalid/failure10, Close/Dispose6, injected/portable2. Ожидаемый
Desktop3991=3961+30, screenshots291=289+2; пока не PASS. Main/compiler явно
подключают partial; прежние файлы после удаления только новых wiring-вставок
эквивалентны HEAD, старые условия и budgets сохранены. Production1 file85+/18−
прошёл независимый source review; полный native результат ещё ожидается.

Ранние bootstrap/settings и scoped shortcuts/launch остаются впереди.
Package PENDING, Live NOT_CHECKED; итоговый installer и устройства владельца
не проверяются. PR draft/unmerged; merge/release не выполняются. Открытое
F17-R01 замечание по disabled startup подписи сохраняется.

## Первая native попытка: CI #553 — FAIL

Functional source `3b2bf894434ae47796f949b36385e5c10f2a4f6e`, tree
`8038162b56d056ce69f7a13c60c468c99d09f561`, parent
`d6b48893d8b67a0de8b7601d5658cbf8f36f7515`; tested merge
`a8cc554c7c573ee98ca1a5ff6232ae9938dad562` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #553](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38879266336):
Windows `116901741841` completed FAILURE, Ubuntu `116901742090` SUCCESS;
весь run FAILURE.
Все5 blob сверены с независимым freeze; remote tree совпал с локальным.
Source/tests/wiring/PENDING evidence reviews APPROVE; whitespace/public266/0 PASS.

Остаток R05: ранние Program AppPaths/SettingsService/Load и scoped Codex
shortcut mutations/launch, PowerShell launch/resolve и открытие Windows settings
ещё требуют отдельных микрозадач. AppPaths.Root/LocalAppData относится к раннему
bootstrap и этим пакетом не перенесён. Нет утверждения, что весь startup или
весь filesystem программы теперь неблокирующий. Исторические пакеты A/B и их
CI #550/#551/#552 остаются самостоятельными доказательствами.

Read-only триаж следующего остатка подтвердил конкретный риск в
`src/ProxyAutomation.cs`, CodexProxyService: Enable проверяет marker launcher,
затем сохраняет ShellLink поверх ShortcutPath; Disable при owned launcher
удаляет существующий .lnk без проверки его target/arguments. Поэтому сохранение
customized/foreign Codex .lnk пока не доказано и не заявлено. Исправление и
native fixture относятся к следующей микрозадаче открытого R05; в пакете C
этот код не менялся. Никаких операций с реальными пользовательскими ярлыками
для этого вывода не выполнялось.

## Отказ нового fixture и узкая коррекция

Полный Windows log CI #553:376376 UTF-8 bytes,3950 lines, SHA-256
`5a8b6306af1a5e16339baa8370baa717c187ee052e5ad7059a936b07f5ace372`.
Первый FAIL **09:25:26.0074457 UTC**: real Save uses the atomically discovered
shortcut manager and snapshot to remove only the chosen login registration.
До него прошли11 новых Check: pending5, deferred4, первые2 independent profile.
Не достигнуты оставшиеся18 после failing12, общая Desktop3991 сводка,
Instance/Maintenance/Shutdown/updater/full ProGo, HomeVPN/portable/Python/public
и ZIP. Их PASS этой попытке не присваивается. SOCKS41/403, старые startup17,
tray12 и migration22 повторно прошли. Ubuntu16/9/46 без skips и actual QR PASS.

Failing compound сравнивал DialogResult OK, отсутствие .lnk и settings endpoint
с endpoint.Text после Close/Dispose, при этом поле получило native handle после
перехода в Diagnostics. Точный false operand553 не залогирован и неизвестен.
Сравнение persisted данных с уничтоженным control — неверный oracle независимо
от того, какое условие отказало в этой попытке. Коррекция только нового fixture:
во всех4 новых Save-сценариях expected endpoint захватывается ДО Save/Close;
результат Save и удаление .lnk
сохранены, persisted endpoint сравнивается с captured draft. Добавлены безопасные
Boolean INFO dialogOk/linkMissing/savedEndpointSame/disposedEndpointSame, без
значений и личных путей. Все30 Check и budgets сохранены; production form и
compiler/main не меняются. Нужен новый полный CI точного corrected source.
Будущая fresh INFO не назначит задним числом точный false operand553.

Uploaded partial screenshots `12037625264`:59 files/1155919 bytes; diagnostics
`12037326139`:36792 bytes. Runtime folder/ZIP не созданы. Download/hash/visual
этих partial artifacts не заявлен; это не291 итоговый снимок. Следующая native
приёмка после исправления пока PENDING, счёт6/55 и R05 PARTIAL сохранены.
