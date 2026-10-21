# ProGo: единый реестр фактического состояния

Обновлено22.10.2026 (МСК). Текущий опубликованный source `656b3d65e03d540be859ee69635c492631093446`, tree `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`; PR88 OPEN/DRAFT, интеграция в main NOT_RUN. Исходная аудируемая база — `7861ea919eed24c5fe9964fc35a23d3958ba5a9d`. Машиночитаемые поля и точные критерии: [project_state.json](project_state.json). Это текущий реестр; прежние checkpoint-файлы сохраняются как история. Утверждённые требования остаются в [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md) и [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md).

23/31 и 6/55 — разные исторические области приёмки разработки. Эти исторические счётчики не складываются и не переводятся в процент готовности. Отдельная взвешенная оценка v1.0 приведена ниже. В 55 входят 43 результата до установщика и 12 зависимых приёмок. Шесть исторических Code/Dev PASS: F17-R02, F17-R03, F31-R01, F31-R02, F31-R03, F31-R04. Их полная перепроверка нового кандидата пока NOT_RUN. Верхнеуровневый PASS требует всех применимых слоёв.

Слои каждого ID: реализация; реальный пользовательский маршрут; проверка/commit; интеграция; пакет; целевой ПК. Код находится в PR88, не в main. Общий финальный installer отсутствует, установка и приёмка на ПК владельца NOT_RUN. PARTIAL в таблице не отменяет прежний узкий PASS. FAIL обозначает подтверждённое нарушение критерия; BLOCKED — конкретную внешнюю зависимость.

CI582 независимо проверен через jobs и Windows log: Windows/Ubuntu SUCCESS, source `b02b0e9606fb57518ed18d17de42e5f82fca8fa8`, merge `cf2e19691057c143b4f66d93c0041e2e1c5681a8`. На базе `7861ea9` только четыре Markdown-файла поверх tested source; runtime/tests/scripts/server/workflows/VERSION идентичны. Новые изменения требуют нового CI.

## Постоянный порядок работы — действует с22.10.2026

Этот порядок задан владельцем и заменяет прежние «одно продолжай — одна
микрозадача», обязательные остановки после этапа и временный ночной режим.
Нижние датированные checkpoint сохраняются как история, их команды остановки
не ограничивают текущую разрешённую последовательность.

1. Самостоятельно продолжать согласованные F01–F31, пока есть разрешённая
   незаблокированная работа. Завершённый этап — промежуточный отчёт, не запрос
   «продолжать?». Отсутствие ответа не означает паузу; явные стоп/пауза/новый
   приоритет владельца имеют преимущество. Объём и концепцию не расширять.
2. Одна микрозадача — один конкретный проверяемый результат. Крупный пункт
   делить заранее. Цикл: минимальная правка → diff → целевые проверки → отдельный
   логический локальный commit → состояние/реестр → следующая задача. Чужой или
   незавершённый diff не включать; не удалять/откатывать работу ради чистого status.
3. После этапа и перед долгими операциями фиксировать выполненное/остаток,
   полные source/local/published/tested SHA, PR/CI IDs, проверки, блокеры и шаг.
   При восстановлении сначала читать документы и сверять Git/процессы/CI;
   не повторять внешнюю операцию до установления результата прежней попытки.
   Не запускать конкурирующего исполнителя на те же файлы.
4. Разрешены публикация исходников и согласованных исправлений в существующую
   `integration/approved-plan-31-20261011`, обновление PR88 и необходимые CI;
   разрешение относится также к последующим согласованным задачам. Перед каждой
   публикацией проверять diff/новые файлы на secrets/PII и автоматические workflow.
   При найденных secrets остановить публикацию, сообщить без раскрытия значений.
5. Не разрешены merge main, публичный release, выгрузка бинарных пакетов/installer,
   установка на ПК владельца, реальные VPS/системная сеть/права/credentials и платные
   сервисы без отдельного применимого разрешения. Если соблюдение требует правки
   workflow, сначала предложить точечную правку на согласование; продолжать
   независимую локальную работу. Не менять workflow ради тайного обхода границ.
6. Один SHA — один необходимый CI, без дубликатов без новых оснований. Сохранять
   run ID и проверять его; во время ожидания делать независимую работу, не
   приписывать старый CI новому SHA. При FAIL установить причину, выделить
   исправление; не выключать тесты, не заменять native сценарии моками и не
   увеличивать timeout ради зелёного. Повторять неуспешный подход только с
   новыми данными. Долгие операции не превращать в бесконечное polling-ожидание.
7. BLOCKED задаётся конкретной причиной и минимальным действием владельца;
   один блокер не останавливает независимые пункты. Общая остановка — только
   завершённый объём, команда владельца, пределы среды или отсутствие любой
   разрешённой незаблокированной работы. Никакого фактически не созданного
   расписания/автоперезапуска/гарантии непрерывного исполнения не заявлять.
8. Сохранять v1.0 и веса100;31/55 не удваивать. Рост только за подтверждённый
   результат точного SHA; отчёт, commit и невыполненный тест не дают баллов.
   NOT_RUN не PASS. Открытые критические блокеры не компенсируются суммой.
   Формат после этапа: «Сделано: … · Проверки: … · Реализация: X/100 ·
   Проверенная готовность: Y/100 · Изменение баллов и основание: … · Далее: …».

### Активная точка: проверочный16-file блок и новый Windows CI

Проверка актуальности: local HEAD и PR88 head
`656b3d65e03d540be859ee69635c492631093446`; PR OPEN/DRAFT/unmerged;
CI586/39670153699 completed SUCCESS только на прежнем merge
`7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`. Активного конкурирующего агента,
Git merge/rebase/index lock и процессов ProGo/Git/Windows build не обнаружено.
Текущий diff — ровно16 согласованных файлов ниже; код REVIEW-01/02 не менялся
после их точек. Linux toolchain не позволяет native Windows прогон локально.

Перед публикацией проверены оба workflow: PR synchronize запускает ci.yml,
на public repository автоматические uploads OFF; release.yml публикует только
при подходящем main push или отдельном dispatch, которые сейчас не выполняются.
Workflow правка для этого push не требуется. Ни public binary upload, ни release
не разрешены и не включаются. Следующий шаг: final diff/secrets check16 файлов,
отдельный local commit, tree-verified fast-forward существующей remote branch,
фиксировать новый source/tested SHA и ID CI; после этого F31-R11 этапами.

Windows compilation/native накопленного блока NOT_RUN до нового exact-SHA CI.
Реализация58–80/100, проверенная готовность22/100, изменение0. Эта новая
документация/авторизация не повышает оценку. После source commit его полный SHA
и последующий published SHA/CI записать отдельным checkpoint; самоописательный
commit SHA заранее не выдумывать.

## REVIEW-03 — PASS только синхронизация metadata CI586

22.10.2026 01:03 МСК. Изменены только `docs/ACCEPTANCE_RUNBOOK.md`,
`docs/project_state.json`, `docs/PROJECT_STATE.md`. Код и тесты сохранены;
новый commit/staging/push/PR mutation/CI не выполнялись. Следующие датированные
блоки этого документа — история на соответствующий момент: прежние pending,
in_progress и открытая REVIEW-03 внутри них не являются текущим статусом.

| Область | Текущий подтверждённый результат |
| --- | --- |
| Published source B | `656b3d65e03d540be859ee69635c492631093446`, PR88 OPEN/DRAFT/unmerged по последней проверке |
| Реально проверенный merge M | `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2` |
| CI586 | [run39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699), completed SUCCESS только для B/M |
| Jobs, повторная read-only проверка | Windows119465588823 SUCCESS, Ubuntu119465589235 SUCCESS |
| Build identity | `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`, повторно прочитанный Windows log |
| Runner ZIP | Pack SUCCESS, SHA256 `d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`; uploads SKIPPED, bytes не скачаны/не переданы |
| Локальный L поверх B | Диагностика/report/Context, advisory SSH preparation, ShowDialog Cancel→Retry и bounded SaveFileDialog: **Windows compilation/native NOT_RUN**, нового source SHA нет |
| Интеграция/пакет/целевой ПК | Не приняты; final setup/fallback не готовы, приватная передача BLOCKED, установка и live ПК/VPS NOT_RUN |

SUCCESS CI586 не переносится на локальный L или целое требование. Уточнены
current поля ci_candidate, fresh_candidate_verification, remote_visibility.ci,
86 integration/candidate записей и текущие scoped evidence для уже проверенных
в B/M retention/updater/shortcut/settings/signing. Это внесение существующего
доказательства, не новый прогон и не новый функциональный PASS. Все86 ID,
31 parent/55 child, их status/remaining/dependencies/package/target сохранены.

История: CI583,584,585 и их failures остаются без изменений в ci_history.
CI586 добавлен туда с completed результатом и отдельным prior_observation —
прежним in_progress снимком. tested_baseline_source/merge остаются историческим
CI582 baseline с явной пометкой; текущая последняя проверка находится в
ci_candidate. Прежние CI584 scoped passes и CI585 shortcut failure не стираются.

REVIEW-03 **PASS только metadata consistency**. REVIEW-01/02 source fixes
по-прежнему PARTIAL до native Windows; F31-R11 остаётся PARTIAL, общий
SSH start/recovery aggregate не подключён. Из этой документационной правки
не следует приёмка31/31,55/55 или готовность установленного приложения.

Доступные проверки этого прохода:

- PASS JSON parse,86 уникальных ID=31+55, сохранность requirement statuses,
  критериев/остатка/зависимостей, package/target и трёх прежних CI history entries.
- PASS consistency check трёх текущих checkpoint: одинаковые B/M/run/jobs/ZIP;
  completed/success только у опубликованного CI; локальные compilation/native
  NOT_RUN. Архивный prior_observation и датированные исторические pending
  проверены как история, не ошибочно удалены общим поиском/заменой.
- PASS состав следующего коммита совпадает с16 фактическими modified/untracked
  файлами и между тремя документами; текущий HEAD и пустой index сохранены.
- PASS scope guard:13 остальных файлов, включая весь production/test/script
  diff, побайтно прежние по SHA256-манифесту начала задачи.
- PASS `git diff --check` трёх документов и `python scripts/check_public_content.py`
  (315 source,0 release). Это проверки документации, не Windows tests.

### Следующий проверочный коммит — только план

Предлагаемый commit: `Bound diagnostic preview lifecycle and add advisory SSH preparation`.
Точный состав —16 файлов, без generated binaries/release payload:

```text
src/DiagnosticPreview.cs
src/DiagnosticReport.cs
src/UpdateAwareTrayApplicationContext.cs
src/SshAuthenticationPreparation.cs
tests/DesktopDiagnosticTests.cs
tests/DesktopTests.cs
tests/DiagnosticPreviewTests.ps1
tests/DiagnosticReportTests.cs
tests/DesktopAsyncDiagnosticPreviewTests.cs
tests/DesktopDiagnosticContextTests.cs
tests/SshAuthenticationPreparationTests.cs
scripts/Test-ProGo.ps1
docs/ACCEPTANCE_RUNBOOK.md
docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md
docs/PROJECT_STATE.md
docs/project_state.json
```

Перед будущим commit повторно сверить весь diff и secrets/PII; source SHA
будет известен после commit. Коммит включает диагностический/advisory блок,
а не реализацию launch/recovery F31-R11, repair или installer fallback.

Минимальный **существующий** Windows entrypoint на новом точном SHA:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\scripts\Test-ProGo.ps1
```

На изолированном Windows CI/dev runner скрипт сам собирает приложение и harness.
У DesktopTests нет selector этих suites, поэтому этот готовый entrypoint
выполняет общий Desktop и сопутствующие Windows регрессии; отдельно запускать
полный Actions workflow, Ubuntu, Test-HomeVpnRelay или upload не нужно.
Обязательны реально выполненные DiagnosticReportTests, fresh-process installed
DiagnosticPreviewTests.ps1, старый DiagnosticPreviewWorkflow, новый
AsyncDiagnosticPreviewWorkflow (4 modal Cancel→Retry +3 native chooser cases),
DiagnosticContextWorkflow, SshAuthenticationPreparationWorkflow и старые
selected-key/agent/policy, Desktop/Instance/Maintenance/Shutdown ownership cases.
Подробные критерии сохранены в [runbook](ACCEPTANCE_RUNBOOK.md), структурированный
состав/команда — uncommitted_candidate в JSON. Если guard дал SKIP/NOT_RUN,
это не успешная native проверка даже при exit0. Live auth/установка отдельно.

Этот следующий этап сейчас не выполнялся. При будущем долгом запуске сохранить
точный SHA и ID job/сессии, закончить отчёт и проверять тот же запуск следующей
командой. Не начинать новый параллельный CI. Остановиться после REVIEW-03.

Оценка v1.0/веса не менялись; новых Windows/пакетных/целевых результатов нет.
Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery.

## REVIEW-02 — bounded native SaveFileDialog fixture: source исправлен, Windows NOT_RUN

22.10.2026 00:54 МСК. Выполнена только REVIEW-02. HEAD остаётся
`656b3d65e03d540be859ee69635c492631093446`; текущий diff не коммитился,
не публиковался и не запускался в CI. CI586 относится к B/M из оценки ниже,
не к этим локальным изменениям. REVIEW-03 и production source не менялись.

Изменения относительно начала этой микрозадачи — только3 файла:

- `tests/DesktopAsyncDiagnosticPreviewTests.cs`: прежний
  `AsyncDiagnosticNativeChooserCancel` вызывает отдельный дочерний STA fixture
  с настоящим production SaveFileDialog.ShowDialog. Поиск ограничен5с; при
  отсутствии совпадения выдаётся `native SaveFileDialog not found within 5000 ms
  for the fixture owner`, а не бесконечное ожидание suite/job timeout.
  Если окно найдено, но не закрылось, это отдельная timeout failure.
- Поиск и WM_CLOSE больше не используют глобальный FindWindow по caption:
  EnumThreadWindows ограничен native thread своего preview, затем проверяются
  PID, GA_ROOTOWNER и класс #32770. Очистка при неправильном caption использует
  те же проверки ownership; чужие окна по совпавшему заголовку не закрываются.
- Внешнее ожидание ребёнка —20с через существующий `DiagnosticProcess.Run`:
  процесс создаётся suspended и включается в отдельный owned job до исполнения.
  При успехе, исключении и deadline существующий owner завершает только этот
  job и проверяет settlement; бюджет его принудительной очистки — ещё до2с.
  Новый общий CI timeout, process-name kill или поиск чужих PID не добавлялись.
- Внутренний finally останавливает timer, закрывает только свои native dialogs,
  dispose preview и без UI pump ждёт объединённые Work/BuildWork/SaveWork до5с.
  Dispose/settlement выполняются и при ошибке запроса WM_CLOSE. Cleanup marker
  выдаётся только после settlement и проверки отсутствия оставшихся owned окон.
  Если native modal loop вообще перестал обслуживать сообщения, внешний job
  deadline остаётся независимым от его UI timer; такой случай всегда FAIL.
- Добавлены3 сценария одного fixture: обычный native Cancel; намеренно
  неподходящий caption; намеренная ошибка обработчика после нахождения окна.
  Негативные дети возвращают именно ненулевые20/21 с точной ошибкой и cleanup
  marker; harness проверяет оба условия, реальные native window observation,
  отсутствие записи и сохранность источников. Это проверка ожидаемого отказа,
  не преобразование настоящего timeout в PASS. В основном Cancel-сценарии
  любой timeout — FAIL; внешний20с timeout во всех режимах — FAIL. Ошибка
  cleanup либо неожиданный exit тоже FAIL. Если настоящий диалог вообще не
  появился, негативный сценарий не принимается как успешно проверенный отказ.
- `tests/DesktopTests.cs`: ровно один dispatch для
  `--diagnostic-save-chooser-fixture` до общего suite watchdog. Ребёнок исполняет
  только этот fixture и не запускает весь Desktop suite рекурсивно.
- `docs/PROJECT_STATE.md`: эта точка продолжения. Все прежние fixture methods,
  включая REVIEW-01 ShowDialog Cancel→Retry, сохранены побайтно относительно
  начала задачи; исправление REVIEW-03 не выполнялось.

Результат REVIEW-02 — **PARTIAL: source fix и regression scenarios добавлены;
Windows compilation/native execution NOT_RUN**. В этой Linux-среде отсутствуют
Windows Forms, dotnet/csc/mcs/mono/pwsh/powershell. Три native сценария,
изоляция окон/job и регрессия всего диагностического блока здесь не исполнялись;
ни одно из этих утверждений не помечается Windows PASS. Независимый агент
не запускался, выполнено собственное source/diff review.

Завершённые доступные проверки:

- PASS `python scripts/check_public_content.py`:315 source,0 release files.
- PASS целевые source checks: существующий suite вызывает fixture; child route
  достижим до общего watchdog; вызваны все3 режима; локальный5с и внешний20с
  пределы; thread/PID/root-owner guards; finally и объединённый task settlement.
  Это статические проверки wiring/контракта, не C# compilation/native tests.
- PASS `git diff --check` для3 изменённых в этой задаче файлов; сохранено
  существующее предупреждение LF→CRLF для DesktopTests.cs, whitespace errors нет.
- PASS targeted secrets/PII pattern scan137 добавленных строк тестового кода:
  findings0. Это проверка шаблонов, не обещание исчерпывающего secret detection.
- PASS scope guard:13 остальных modified/untracked файлов совпадают с исходным
  SHA256-манифестом; предыдущая часть DesktopAsyncDiagnosticPreviewTests до
  заменённого native chooser метода побайтно прежняя; в DesktopTests добавлен
  только один dispatch. HEAD и пустой staged index не менялись.

Точные текущие test SHA256 (локальный diff поверх HEAD, не commit SHA):

- `tests/DesktopAsyncDiagnosticPreviewTests.cs`:
  `58cf747c4e897862cb9f35ab835caa9d2675cc9edde742cc0e30b01cc6004acd`.
- `tests/DesktopTests.cs`:
  `314d3f00aff6aa3827a1e559b603ea087eb39a5db4d236f3368b23ea83d41f49`.

Остаток: Windows-проверка этих3 chooser cases и ранее добавленного modal
Cancel→Retry на точном будущем candidate SHA — NOT_RUN. REVIEW-03 остаётся
отдельной следующей задачей только по новой команде. Другие пункты сейчас не
исправлялись. Ничего не публиковать, полный CI не запускать; остановиться.

Оценка v1.0 и веса ниже сохранены: функциональность продукта в этой микрозадаче
не добавлялась, новых Windows/пакетных/целевых доказательств нет, прирост0.
Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery.

## Оценка готовности v1.0 — базовая точка, 22.10.2026 00:38 МСК

**Реализация: 58–80/100. Проверенная готовность: 22/100. Изменение: +0.**
Это два разных индекса, а не доля закрытых строк реестра. Они не заменяют
PASS/FAIL/PARTIAL/BLOCKED/NOT_RUN/TODO и обязательные критерии приёмки.
Все31 основных пункта пока PARTIAL; это не означает автоматически «половина».
В этом проходе код не менялся, тесты/CI не запускались, публикации не было.

### Снимки и границы доказательств

- **B — опубликованный source:** `656b3d65e03d540be859ee69635c492631093446`,
  tree `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`,
  [PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88).
  Последнее подтверждённое состояние PR — OPEN/DRAFT, в main не интегрирован.
- **M — реально проверенный Windows merge:**
  `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`,
  [CI586](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699),
  Windows job `119465588823`, Ubuntu job `119465589235`.
  В этой микрозадаче read-only повторно прочитаны jobs и Windows log:
  оба jobs completed/success. В логе build identity
  `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2` (20:25:38 UTC).
  Указанные ниже native результаты относятся именно к M в CI586 для B;
  это не утверждение о прогоне любого последующего diff или final installer.
- **L — текущий локальный diff поверх B:**12 modified +4 untracked, без staging.
  Его реализация учитывается только как source evidence в диапазоне I;
  Windows compilation/runtime **NOT_RUN**, приращение проверенной готовности0.
  CI586 не проверяет L. Фиксация L без самого изменяемого PROJECT_STATE:
  SHA256 JSON-манифеста15 путей/хэшей
  `73fc808325dafdc80a5f40f2983c811a411a261017f567b840a0f0e13e2e1ade`.
  Ключевые source SHA256:
  `DiagnosticPreview.cs=d408afd28a707fb52e7f4837c6bcce79a040b040e10fe63bba33db120ed700db`,
  `DiagnosticReport.cs=a48223671f45d74152f877d3aeeddaa673937af70099718a3e17604f94f3e563`,
  `SshAuthenticationPreparation.cs=050b76d82f226edb99a2e5196389c60323981845c59b02e3fc999d0858438f86`.
  Наличие этого компонента не подтверждает подключение к SSH start/recovery.
- В CI586 Pack release zip завершён SUCCESS; лог20:47:53 UTC сообщает ZIP SHA256
  `d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
  Это ZIP на runner, не переданный и не установленный владельцем пакет.
  Upload steps skipped/public artifacts OFF; в последней проверке artifacts=[];
  приватная передача, clean setup/update/rollback и целевой ПК **NOT_RUN**.
- Источники критериев: [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md),
  [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md),
  [project_state.json](project_state.json),
  [RESIDUAL_RECHECK_20261021.md](RESIDUAL_RECHECK_20261021.md),
  source review и REVIEW-01 выше/ниже в этом документе.
  Residual recheck выполнен ранее на `d0f9a6902345eab288b25e8f902df1514db3f2ca`;
  отсутствие последующего исправления указано в текущем реестре, это source
  evidence, не новое native воспроизведение. Полные две исходные ChatGPT-истории
  недоступны; восстановленные требования не выдаются за прочтение всех чатов.
  В момент базовой оценки REVIEW-03 ещё фиксировала устаревшие CI metadata;
  использованы более поздние фактические jobs/logs. Теперь metadata исправлены
  текущим checkpoint REVIEW-03 выше; баллы базовой оценки от этого не повышаются.

### Неизменяемая методика и веса

Каждый из31 основных ID имеет один вес W; сумма W=100. Его вложенные55 ID
ограничивают выполнение родительского критерия, но собственного веса не получают.
Они есть у F16(4), F17(5), F19(5), F22(4), F23(7), F27(4), F30(12), F31(14).
Это декомпозиция остатка, а не исчерпывающая замена всех родительских требований.
Исторические23/31,6/55 и количество assertions/строк/коммитов в формулу не входят.

Большие веса получают сценарии соединения, возврата настроек, сохранности данных,
обновления и готовности SSH. F23=7, F31=8 — их отказ мешает безопасно получить
и использовать результат. F30=8 сохраняет значимость согласованного полного
коммерческого контура даже при большом остатке; он не исключён ради роста оценки.
Навигация, стиль и документация имеют меньший самостоятельный вес.
Пересекающийся код не создаёт дополнительных баллов: F02 оценивает retry,
F04 — истинность статуса, F05 — базовый SSH/SOCKS вход, F31 — дополнительные
agent/key/portability/performance критерии; F07 retention, F08 restore,
F22 перенос/repair, F23 setup/update имеют отдельные пользовательские результаты.

**Реализация I = Σ W × r.** r — экспертная ступень по существующим source и
пользовательскому маршруту:0 — функциональной реализации нет;0.25 — отдельные
работающие компоненты при отсутствии основного согласованного контура;
0.50 — основной контур существует, существенные обязательные части отсутствуют;
0.75 — основной пользовательский маршрут и значительная обработка отказов есть,
полнота остатка ещё не подтверждена;1 — весь согласованный code-критерий доказан.
Это грубые функциональные ступени, не измеренная доля функций с точностью до1%.
Если trace неполон, указывается интервал ступеней: нижняя — обоснованный минимум,
верхняя — допустимая полнота уже существующего кода, которую ещё надо доказать.
Ненаписанные функции верхней границей не оплачиваются; известный отсутствующий
существенный контур ограничивает r сверху. Непроверенный L может дать source
основание для I, но не Windows PASS. Край1 в интервале не присваивает требованию
PASS: он отражает неопределённость полноты, а не подтверждённую полноту.

**Проверенная готовность Y = Σ W × (0.30D +0.20P +0.50A).**
Это консервативный индекс заработанных доказательств для B/M; L не наследует
успех CI586. Значения этапов:

| Этап |0 |0.5 |1 |
| --- | --- | --- | --- |
| D — функциональные dev-проверки | Нет релевантного доказательства/NOT_RUN | Конкретная часть критерия проверена native Windows на точном SHA | Весь dev-критерий с обязательными положительными/отрицательными случаями доказан |
| P — проверочный пакет | Нет доказанного payload соответствующего контура | Сборка с identity, составом runtime и ZIP hash на точном SHA | Полученный пакет проверен по hash/составу, clean dev Windows setup/update/rollback пройдены |
| A — установленный реальный сценарий | NOT_RUN либо нет принятого результата | Основной сценарий на точном установленном кандидате принят, часть обязательных случаев остаётся | Весь согласованный реальный сценарий, отказы/отмена/восстановление приняты |

Общий зелёный CI сам по себе не даёт D=1. ZIP на runner не даёт P=1.
Для F19, F27, F30 пока D=P=A=0: наличие общего EXE не доказывает упаковку
отсутствующего preflight/signing/commercial контура. Для остальных строк
есть приведённая ниже конкретная native проверка M и общий build/runtime/ZIP
из CI586: D=0.5, P=0.5, A=0, то есть Y=W/4. Это частичный зачёт существующего
сценария, а не успешная проверка его известного остатка. Ни по одному ID
реальная установка/приёмка A не начислена. NOT_RUN даёт0 баллов за этап,
сохраняет статус NOT_RUN и не превращается в FAIL.

Веса и этапные доли фиксируются как v1.0. Менять их ради увеличения индекса
нельзя. Изменение согласованного объёма требует явного решения владельца,
новой версии методики и отдельного пересчёта прежней базы.
При смене кандидата доказательства затронутых частей перепроверяются; Y может
снизиться. Старое доказательство хранится со своим SHA, не переносится на L.

### Матрица начислений:31 родителей,100 баллов

Все исходники в столбце «Основание I» относятся к B, кроме явно отмеченного L.
Критерии каждого ID — одноимённая строка APPROVED_PLAN_31 и его дочерние ID
в JSON. Сокращённый native пример в последнем столбце однозначно находится
в Windows log CI586/M; время UTC21.10.2026. Он подтверждает только часть D.
Десятичные начисления служат проверке арифметики, не заявлению точности оценки.

| ID / сценарий | W | r → вклад I | Вклад Y | Основание I и существенный остаток | Native evidence M, частичный D |
| --- | ---: | --- | ---: | --- | --- |
| F01 CLI/Codex |5 |0.75–1 →3.75–5 |1.25 | `CliProxyEnvironmentService`: общий CLI/миграция есть; итоговый пакет и обычный запуск владельца не приняты |20:32:32.173 `CLI migration preserves unrelated preferences` |
| F02 Повторы |4 |0.75–1 →3–4 |1 | `AutomationPlan`, `ProxyService`: retry/fallback/cancel есть; полная итоговая матрица не принята |20:28:53.829 `automatic fallback advances through permanent and transient failures without retrying refused candidates` |
| F03 Один экземпляр |3 |0.50–0.75 →1.5–2.25 |0.75 | `ApplicationInstance`, `MaintenanceOperation`: owner/handoff есть; полный installer ещё не включён |20:40:11.757 `restarted application retains single-instance ownership after handoff` |
| F04 Статус связи |3 |0.75–1 →2.25–3 |0.75 | `ConnectionHealthMonitor`: listener/SOCKS/egress разделены; итоговый реальный маршрут не принят |20:28:03.722 `asynchronous startup refuses a foreign non-SOCKS listener` |
| F05 SSH-вход |5 |0.75–1 →3.75–5 |1.25 | `ProxyService`, `SshInteractiveLogin`: запуск/ожидание SOCKS/первый вход; настоящий VPS NOT_RUN; дополнительная key readiness относится к F31 |20:27:46.940 `asynchronous slow startup shares one process and waits for SOCKS` |
| F06 Возврат proxy |5 |0.75–1 →3.75–5 |1.25 | `CliProxyEnvironmentService`, `WindowsProxyService`: baseline/journal/условный возврат; installer/uninstall и целевой профиль не приняты |20:32:42.684 `failed actual native move conditionally restores its own environment, typed Windows values and exact file bytes` |
| F07 Retention |3 |0.75–1 →2.25–3 |0.75 | `BackupRetention`, `BackupService`: preview/owned deletion/retention есть; единый final installer ещё не принят |20:30:59.042 `Backup retention tests PASS: 29` (suite результата, не29 баллов) |
| F08 Restore |4 |0.75–1 →3–4 |1 | `BackupIntegrity`, `Restore-ProGoBackup.ps1`: staging/rollback/cancel ownership; старый FAIL не объявляется текущим; final package NOT_RUN |20:32:59.168 `pending restore X requests cancellation and retains its native owner until the real hash operation settles` |
| F09 PIN/vault |4 |0.75–1 →3–4 |1 | `VaultService`, `VaultDailyStore`, `VaultOpenService`: утверждённый PIN/ложный vault реализован; итоговая Comfort/midnight/I/O приёмка не полна |20:25:43.380 `wrong-pin read leaves source bytes and file set unchanged` |
| F10 HTTP bridge |4 |0.75–1 →3–4 |1 | `CliProxyBridgeService`, `HttpConnectProxyServer`: consumers/bridge/lifecycle есть; реальная цепочка SSH→SOCKS→HTTP не принята |20:32:32.076 `Windows uses tested HTTP/CONNECT bridge` |
| F11 Навигация |2 |0.75 →1.5 |0.5 | `AppCommand`, tray context: общие команды есть; согласованные новые экраны кабинета отсутствуют, локальная диагностика требует Windows регрессии |20:32:34.807 `real tray command dispatches its expected dialog: Settings` |
| F12 Область Stop |4 |0.50–0.75 →2–3 |1 | tray context/`HomeVpnService`: PC/phone разделены, но phone Stop/exit ownership F31-R10 остаётся FAIL |20:33:03.412 `actual production three-second shutdown refusal keeps the native UI ticking while its restore reader remains owned` — restore owner, не phone Stop |
| F13 Маршрут |2 |0.75–1 →1.5–2 |0.5 | `RouteTester`: старт/отмена/источник результата; итоговый live маршрут NOT_RUN |20:38:56.634 `Escape closes the actual modal route dialog while measurements run` |
| F14 Диагностика |2 |0.75–1 →1.5–2 |0.5 | `SshProfileDiagnostics`, `DiagnosticProcess`: фон/deadlines/cancel есть; aggregate L и modal Cancel→Retry L Windows NOT_RUN |20:38:55.911 `diagnostic workers preserve UI heartbeat after the caller loses its synchronization context` |
| F15 Настройки |3 |0.75–1 →2.25–3 |0.75 | `SettingsService`, `SshProfilesSettingsForm`: atomic save/rollback; установленная итоговая версия не принята |20:32:41.088 settings suite PASS93; native память/диск проверены в CI, не на ПК владельца |
| F16 DPI/layout |2 |0.50–0.75 →1–1.5 |0.5 | `MainWindow`, forms/`Program`: часть адаптивной раскладки; весь набор форм/installer и monitor DPI ещё не завершён |20:34:26.022 `scaled cleanup list and actions do not overlap`; synthetic scale не заменяет реальные DPI100/125/150/200 |
| F17 Доступность |2 |0.50–0.75 →1–1.5 |0.5 | `UiTheme`, native forms: часть keyboard/name/state; новые формы, Narrator/High Contrast NOT_RUN |20:38:56.452 `route measurements complete Tab reaches Button Закрыть` |
| F18 Выдача |2 |0.75–1 →1.5–2 |0.5 | `HomeVpnWizardForm`: issued/installed разделены; установка телефона и интернет NOT_RUN |20:37:27.267 `wizard accessible issuance result never claims installation` |
| F19 Preflight сети |3 |0.25–0.50 →0.75–1.5 |0 | `HomeVpnWizardForm`, `HomeVpnService`: сетевые компоненты/подсказки есть; согласованный NAT/CGNAT/UDP verdict контур не доказан; остаток F19-R01…R05 |Релевантная полная preflight интеграция не доказана; D/P/A0, не общий CI PASS |
| F20 Друзья |2 |0.75 →1.5 |0.5 | `HomeInvitationsForm`, `server/home_vpn_setup.py`: selected revoke/reissue; связь со сроками/тарифами F30 не реализована полностью; live API не принят |20:37:29.887 `native friends Down distinguishes revoked duplicate and names repeat-revoke effect` |
| F21 Clipboard |1 |0.75–1 →0.75–1 |0.25 | `ClipboardService`: owned secret copy/cleanup; итоговые busy/exit сценарии не полностью приняты |20:43:01.811 `secret copy writes current clipboard`;20:43:06.915 logs do not contain secrets |
| F22 Перенос/repair |4 |0.50–0.75 →2–3 |1 | `BackupService`, `HomeVpnPortableTransfer`: защищённый перенос есть; `Repair-ProGo.ps1` F22-R03 source FAIL, другой SID/ПК NOT_RUN |20:46:38.740 `a new real HomeVpnService loads clean imported data without activating SSH or relay`; это не repair PASS |
| F23 Update/setup |7 |0.25–0.50 →1.75–3.5 |1.75 | `UpdateLauncher`, `Update-ProGo.Core.ps1`: updater/rollback есть; standalone setup, installer fallback и release UID/asset контур не завершены |20:31:26.805 `finally requests helper progress clearance even after rollback or host failure: hash-failure`; это не setup acceptance |
| F24 Помощь/версия |1 |0.75 →0.75 |0.25 | `HelpForm`, `README.ru.md`: помощь есть, известная устаревшая CI/acceptance metadata ограничивает верхнюю границу |20:36:09.255 `help Начало820 exposes a focusable read-only instruction pane` |
| F25 Logs/export |2 |0.50–0.75 →1–1.5 |0.5 | `BoundedLog`, `DiagnosticReport`, `DiagnosticPreview`: limits/redaction/preview B, worker build/save + modal fix L; SafeLog/viewer UI I/O остаётся |20:30:26.285 bounded log suite PASS32;20:30:57.110 diagnostic report suite PASS29; L Windows NOT_RUN |
| F26 Автозапуск |1 |0.75–1 →0.75–1 |0.25 | `ApplicationShortcuts`, installer script: отдельные настройки есть; final installer и реальный Windows logon NOT_RUN |20:30:56.448 `startup runs quietly without changing SSH preferences` |
| F27 Подпись/AV |2 |0 →0 |0 | `.github/workflows/release.yml`: signing/verification/timestamp pipeline отсутствует; сертификат/Defender/Kaspersky NOT_RUN |D/P/A0; подпись не выводится из успешной unsigned сборки |
| F28 Команды/отчёт |1 |0.50–0.75 →0.5–0.75 |0.25 | `AppCommand`, `Test-ProGo.ps1`: каталог есть; раздельная отчётность suites/scenarios/assertions не завершена |`DesktopUiWorkflowTests.CommandCatalogContract`: unique round-trippable command IDs;20:35:03.163 diagnostic command opens review without implicit Enter export |
| F29 Стиль/иконка |1 |0.75–1 →0.75–1 |0.25 | `BrandIcon`, `UiTheme`: принятая иконка/стиль; все состояния новых экранов и итоговые screenshots ещё не приняты |20:38:07.907 built icon valid ICO header/all seven resolutions;20:38:07.912 PNG corners16 |
| F30 Кабинет/billing |8 |0.25 →2 |0 | `server/profile_share.py`, friends/reissue и отдельные server limits — компоненты; полный кабинет/roles/orders/payment/mail не реализован, есть внешние зависимости |D/P/A0: QR/issuance тест не считается коммерческой end-to-end проверкой |
| F31 SSH/скорость |8 |0.50–0.75 →4–6 |2 | `SshAgent*`, `SshProfile*`, `ProxyService`: agent status/start, key inspection, IPv6/fallback есть; common fresh key preparation F31-R11 TODO, UI I/O/phone Stop FAIL; aggregate L пока advisory |Signing55/UI50 и service-start62 в M;20:28:22.833 `raw and bracketed IPv6 start a native child and complete SOCKS CONNECT`; encrypted key/VPS/другой ПК NOT_RUN |
| **Итого** |**100** |**58.00–79.25** |**21.75** |**Показать I с округлением наружу58–80, Y с округлением до22** |**Ни одного A>0; Windows L NOT_RUN** |

Разброс I обусловлен неполной трассировкой всех критериев к существующему коду,
незавершёнными специфицированными контурами и непроверенным L. Подмена его точкой
середины создала бы ложную точность. Y=21.75 — сумма явно заработанных частичных
этапов по этой методике, отображаемая как22; это не «22% тестов прошло» и не
разрешение эксплуатации. Для текущего L нет самостоятельного Windows балла:
22 описывает доказательства B/M, а готовность нового изменённого блока NOT_RUN.

### Критические блокеры независимо от суммы

1. **F31-R11:** общий свежий этап подготовки конкретного выбранного ключа перед
   `StartSingleTunnel` и recovery ещё не подключён. Public key match/доступный
   ssh-agent не доказывают разблокировку private key или принятие сервером.
   Реальный encrypted-key/agent и SSH→SOCKS5→HTTP на разрешённом VPS NOT_RUN.
2. **F31-R08/R09/R10:** source FAIL для оставшихся SafeLog/viewer/helper UI I/O,
   private HomeVPN disk/DPAPI/session preparation и phone Stop/exit ownership.
   Worker preview/save L закрывает лишь часть R08; Windows регрессия L NOT_RUN.
   Фактическое зависание/утечка на ПК владельца этим не утверждается.
3. **F22-R03/F23:** repair пропускает12 из41 обязательных runtime paths, допускает
   неполный payload и direct replacement без общей транзакции; final standalone
   installer, подходящий fallback при неприменимом patch и его приёмка не готовы.
   Сохранность профиля/ключей при полном конечном обновлении пока не принята.
4. **Кандидат и приёмка:** L не скомпилирован/не проверен Windows, PR не интегрирован,
   пакет владельцу не передан и не установлен. Startup/UI/profile/key/cancel/
   recovery/update/rollback на целевом ПК NOT_RUN. Согласование приватной передачи
   требуется до использования публичных CI artifacts; сейчас ничего не передаётся.
5. **Полный согласованный объём:** F27 signing/verification отсутствует, реальные
   cert/AV проверки NOT_RUN; F30 коммерческий контур в основном отсутствует,
   провайдерные решения/интеграции BLOCKED. Это блокеры окончательного завершения
   всего объёма, а не новый запрет разрешённого unsigned тестового кандидата.

### Три ближайших шага — только по отдельной команде

1. **REVIEW-02:** ограничить native SaveFileDialog fixture своим deadline и owned
   cleanup. Критерий: отсутствующее/неподходящее окно даёт ограниченный по времени
   FAIL, positive chooser Cancel сохраняется, fixture не оставляет свои окна/tasks.
   Не увеличивать общий CI timeout и не исправлять другие функции в этой задаче.
2. **REVIEW-03:** синхронизировать completed CI586 metadata в JSON/runbook с B/M,
   ссылкой и jobs; L явно NOT_RUN. Критерий: нет двух конкурирующих текущих
   CI-статусов, сохранены86 ID и история. Само исправление метаданных не повышает Y.
3. **Целевая Windows-регрессия отдельного диагностического кандидата:** сначала
   зафиксировать состав и точный candidate SHA, затем отдельным разрешённым
   запуском проверить ShowDialog Cancel→Retry, chooser bounds и прежние
   close/Dispose/single-flight/installed helper сценарии. Критерий: результаты
   именно этого SHA, bounded cleanup и отсутствие stale publication; не общий
   старый CI SUCCESS. Если подготовка/запуск крупнее одной микрозадачи, разделить;
   долгий запуск закончить сообщением ID, результат читать следующей командой.
   Этот шаг даёт доказательство только диагностическому блоку, не всему F31.

### Учёт изменений и завершение этой микрозадачи

Это первая оценка v1.0, сопоставимой предыдущей базы нет: **Изменение +0**.
В дальнейших отчётах Δ означает изменение заработанного Y по той же методике
от предыдущей оценённой точки (расчёт до округления); изменение интервала I
описывается отдельно. Только новое релевантное доказательство на точном SHA
может дать прирост Y. Новый код без проверки, NOT_RUN, новая строка отчёта,
количество тестов и пересчёт весов приростом не считаются.

В этой микрозадаче разрешено изменить только PROJECT_STATE.md. Завершены локальные проверки документа:
PASS арифметика31 уникального веса (сумма100; I58–79.25; Y21.75),
PASS86 ID реестра (31+55), PASS `git diff --check -- docs/PROJECT_STATE.md`,
PASS побайтовая неизменность остальных15 modified/untracked файлов по исходному
SHA256-манифесту. HEAD и пустой staged index не изменились. Это не Windows
функциональные тесты. Read-only PR проверка подтвердила OPEN/DRAFT, merged=false,
head=B и merge=M; старое описание PR о pending CI не подменяет jobs CI586. Ничего не публиковать, CI не запускать,
к следующей задаче не переходить.

Формат конца дальнейшего отчёта:
«Реализация: 58–80/100 · Проверенная готовность: 22/100 · Изменение: +0 · Главный блокер: F31-R11 — подготовка выбранного ключа не подключена к SSH start/recovery».
Числа обновлять только по этой методике и доказательствам; для первого отчёта
использовать указанную базу. Остановиться и ждать следующей команды владельца.

## REVIEW-01: modal Cancel → Retry исправлен в source, Windows NOT_RUN

22.10.2026 00:21 МСК. Выполнена ровно одна разрешённая микрозадача: отмена диагностики
в ShowDialog и её modal regression. HEAD остаётся
`656b3d65e03d540be859ee69635c492631093446`; новые изменения не коммитились,
не публиковались и не проверялись полным CI. CI586 не проверяет этот diff.

Изменены только3 файла относительно начала этой микрозадачи:

- `src/DiagnosticPreview.cs`: после Form.CancelButton assignment явно установлен
  `close.DialogResult = DialogResult.None`. Обычный busy Cancel/Esc вызывает
  RequestCancellation без implicit modal close. Idle Close по-прежнему вызывает
  CancelAndClose → CloseSettled. Title-close, shutdown/Dispose и active-operation
  identity/single-flight guards не менялись.
- `tests/DesktopAsyncDiagnosticPreviewTests.cs`: добавлен
  `AsyncDiagnosticModalCancelRetry`,4 вызова в существующем suite — build/save
  × click/Esc. Каждый случай действительно открывает `form.ShowDialog()`.
  Первый build уже прочитал прежний журнал, но удержан перед owner publication;
  отменённый результат не принимается. После settlement retry в том же окне
  читает изменённый журнал и выдаёт только свежие события. Save-вариант сохраняет
  исходный reviewed snapshot после изменения журнала. Во время обеих фаз duplicate
  requests возвращают тот же Task; старый Task завершён до нового native I/O.
  Результат повторно сверяется через несколько modal message-loop turns, затем
  explicit idle Close действительно закрывает окно.
- `docs/PROJECT_STATE.md`: эта точка продолжения; остальная работа сохранена.

Новый test fixture: собственный8с modal deadline на UI timer, каждый controlled
worker barrier ждёт не более3с. Timer exception освобождает оба barrier и dispose
точно своей формы. В finally timer останавливается, barriers освобождаются, форма
dispose, все first/retry/current/build/save Tasks объединяются и ожидаются без UI
pump с пределом5с. Это ограничение контролируемого fixture, не обещание принудительно
прервать зависший OS filesystem call. Чужие окна/процессы не затрагиваются.

Результат REVIEW-01 — **PARTIAL: source fix + regression added; Windows NOT_RUN**.
В доступной Linux-среде нет dotnet/csc/mcs/mono/pwsh/Windows Forms. Нативные4 новых
сценария, C# compilation и существующие title-close/shutdown/Dispose regressions
не запускались; их PASS не заявляется. Новые функции и SSH launch integration
не добавлялись.

Доступные целевые проверки:

- PASS `git diff --check`; только прежние LF→CRLF notices в двух других файлах.
- PASS `python scripts/check_public_content.py`:315 source,0 release files.
- PASS статическая проверка wiring: четыре варианта нового метода вызываются
  существующим suite; в методе ShowDialog, deadlines и finally cleanup; это
  source check, не выполнение WinForms.
- PASS targeted secrets/PII scan119 добавленных строк production/test, findings0.
- PASS scope guard:13 остальных modified/untracked файлов побайтно совпадают с
  началом микрозадачи. В частности, REVIEW-02 chooser method, REVIEW-03 runbook
  и JSON остались без исправлений. HEAD и пустой staged index не менялись.

Проверенный source SHA256 `DiagnosticPreview.cs`:
`d408afd28a707fb52e7f4837c6bcce79a040b040e10fe63bba33db120ed700db`.
Проверенный test SHA256 `DesktopAsyncDiagnosticPreviewTests.cs`:
`fd4b028149f0fe66e20a6cf04c7085536d1ba8a5ecfe5d3d20a0518b36298304`.

Остаток, сейчас не выполнять:

1. REVIEW-02 — ограничить ожидание native SaveFileDialog cancellation fixture
   собственным deadline/owned cleanup; отдельная предлагаемая следующая микрозадача.
2. REVIEW-03 — синхронизировать completed CI586 metadata в runbook и JSON,
   сохранив этому локальному diff NOT_RUN.
3. Для REVIEW-01 необходима отдельная разрешённая Windows-проверка на точном
   будущем candidate SHA; до неё native acceptance остаётся NOT_RUN.

Публикация, полный CI и переход к следующей задаче в этом проходе не разрешены.
Остановиться и ждать следующей команды владельца.

## История: ревью16 незакоммиченных файлов — завершено с находками

22.10.2026 00:05 МСК. Разрешённая единственная микрозадача: source review12 tracked modified
и4 untracked относительно HEAD `656b3d65e03d540be859ee69635c492631093446`.
HEAD/ветка не менялись. Просмотрены diff12 файлов и полный текст4 новых файлов,
связанные existing helper/API ровно для проверки их совместимости.
В этой микрозадаче изменён только этот PROJECT_STATE.md; source/tests/другие
документы сохранены побайтно. Commit/push/PR mutation/full CI не выполнялись.

**Итог: source review завершено, безусловный CLEAR не выдан.** Подтверждены
две source/control-flow находки и рассинхронизация документов. Исправления
не выполнялись. Windows compile/runtime для локального блока **NOT_RUN**:
в доступной Linux-среде нет dotnet/csc/mcs/mono/pwsh/Windows Forms.
CI586 SUCCESS относится только к published source656b3d65 и tested merge
7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2, не к этому рабочему diff.

### REVIEW-01 — P2: Cancel превращается в закрытие модального окна

- Файл/символы: `src/DiagnosticPreview.cs`, constructor (`CancelButton = close`,
  сейчас108), close Click handler (102), `OnFormClosing` (246),
  `RunOperation` owner finally → `CloseSettled` (193).
- Доказательство: UiTheme создаёт Button с DialogResult.None; setter Form.CancelButton
  в .NET Framework автоматически меняет его на Cancel. Локальный код не сбрасывает
  это значение после назначения. При ShowDialog нажатие кнопки/Esc устанавливает
  результат формы; modal close вызывает OnFormClosing, который при busy задаёт
  closing/closeAfterWork. После settlement окно закрывается. Это вывод по исходникам
  проекта и официальному framework contract; native воспроизведение **NOT_RUN**.
- Влияние: действие «Отменить операцию» лишает пользователя повтора в том же окне,
  хотя новый сценарий и статус предлагают retry. При отмене уже выполненной записи
  подтверждение результата также исчезает вместе с окном. Преждевременное освобождение
  native I/O или потеря исходных журналов этим выводом не утверждаются.
- Воспроизведение: открыть actual tray ExportDiagnostics → ShowDialog, удержать build
  или save на существующем beforeIo barrier, нажать «Отменить операцию»/Esc,
  обработать modal messages, отпустить barrier. По коду окно закроется вместо retry.
- Пробел тестов: `AsyncDiagnosticBuildCancelRetry` и `AsyncDiagnosticSaveCancelRetry`
  вызывают `form.Show()`, поэтому implicit modal DialogResult не проверяется.
  Context modal tests покрывают shutdown/Dispose, а не обычный Cancel+Retry.
- Минимальное исправление: нейтрализовать автоматический button DialogResult после
  назначения CancelButton; явное idle Close, title-close и CancelAndClose сохранить.
- Проверка исправления: настоящий ShowDialog, held build/save, click и Esc; после
  settlement окно остаётся открытым, доступен explicit retry с тем же snapshot;
  title-close/shutdown/forced Dispose по-прежнему закрывают с сохранением ownership.

Framework evidence:
[.NET Framework Form.cs — CancelButton и CheckCloseDialog](https://referencesource.microsoft.com/System.Windows.Forms/winforms/Managed/System/WinForms/Form.cs.html),
[Microsoft Button.DialogResult — ShowDialog contract](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.button.dialogresult?view=netframework-4.8.1).

### REVIEW-02 — P2: native chooser fixture не ограничивает собственное ожидание

- Файл/символ: `tests/DesktopAsyncDiagnosticPreviewTests.cs`,
  `AsyncDiagnosticNativeChooserCancel` (259).
- Доказательство: timer при `FindWindow(...) == IntPtr.Zero` постоянно возвращает;
  deadline/watchdog отсутствует. `PerformClick` синхронно входит в production
  ChooseSavePath → SaveFileDialog.ShowDialog; последующий PumpAsyncRestore ещё
  не выполняется. Ограничение всего CI job не ограничивает этот fixture.
- Влияние: если caption/class matching не сработает, тестовый диалог остаётся открытым
  и общий Windows suite может ждать до job timeout. Это условный source-confirmed
  путь без лимита; реальный сбой поиска окна на Windows **NOT_RUN**, не заявлен.
- Воспроизведение для будущего regression: намеренно не находить native dialog при
  настоящем ShowDialog; ожидание не должно удерживать весь Desktop harness.
- Исправление/проверка: собственный deadline с точным владением test helper/dialog,
  bounded cleanup и явным FAIL при отсутствии окна; сохранить реальную native chooser
  проверку и отдельный негативный сценарий. Не увеличивать общий CI timeout.

### REVIEW-03 — P2: CI586 metadata не согласованы с завершённым прогоном

- Файлы/символы: `docs/ACCEPTANCE_RUNBOOK.md` opening candidate block;
  `docs/project_state.json`: ci_candidate.status/acceptance,
  fresh_candidate_verification, remote_visibility.ci и requirements[].integration.
- Доказательство: runbook пишет «Windows выполняется», JSON содержит in_progress /
  NOT_RUN для CI586; предыдущая точка восстановления уже установила completed SUCCESS
  с точными source/merge/jobs. Structural check этого противоречия не исправляет.
- Влияние/воспроизведение: открыть эти документы рядом с completed-CI586 checkpoint —
  читатель получает два разных текущих статуса. Исторические FAIL583–585 сохраняются.
- Исправление/проверка: отдельная небольшая синхронизация только metadata опубликованного
  tested scope; сохранить локальному diff NOT_RUN,86 ID и package/target gates.
  В текущей микрозадаче эти файлы не изменялись.

### Проверенные границы и доступные результаты

- DiagnosticReport сохраняет источники/allowlist/ограничения; добавленные token checks
  не поглощают OperationCanceledException. Worker Build/save используют reviewed
  snapshot; native chooser/clipboard остаются owner UI. Context удерживает точный
  Work до settlement, включая disposed/reentrant paths. Кроме REVIEW-01 новых
  подтверждённых production дефектов в просмотренном diff не выявлено.
- SSH request/result копируют candidate/config/identity list; exact canonical executable
  и endpoint проверяются, общий budget и прежний signing cap разделены. Нет mutating
  ssh-add, новых hard gates или автоматического выбора первого из нескольких ключей.
  Подпись остаётся local observation; повторный -G не является private-key/server proof.
  Launch/recovery integration явно TODO; его отсутствие не скрыто как готовность.
- Native suites содержат реальные file/WinForms/OpenSSH/test-owned agent integrations
  плюс отдельные deterministic seams. Существующие assertions сохранены; async save
  ожидание добавлено до прежних assertions. Три новых suite зарегистрированы и
  вызываются по одному разу; это static wiring check, не compilation/runtime PASS.
- PASS: `git diff --check` (только LF→CRLF notices в2 файлах).
- PASS: `python scripts/check_public_content.py` —315 source,0 release files.
- PASS: targeted secrets/PII scan1549 добавленных/новых строк во всех16 файлах,
  findings0; новые файлы проверены целиком, включая fixture literals.
- PASS: JSON parse/schema shape —86 уникальных ID=31+55; IDs/statuses совпадают с HEAD,
  обязательные verification/integration/package/target/remaining/dependencies поля есть.
  Семантическое metadata расхождение остаётся REVIEW-03.
- NOT_RUN: C# compilation, DiagnosticReport native harness, новые12 form/3 Context/
  auth aggregate scenarios, installed PowerShell fresh-process suite и реальные Windows
  regressions. Полный Python suite/full CI не запускались как не относящиеся к этой
  ограниченной source-review задаче. Source review не заменяет эти проверки.

Review snapshot15 файлов кроме изменяемого PROJECT_STATE:
SHA256 sorted path→SHA256 JSON manifest
`535c9fc55af868e9003c18095733fae052b46c6d0914cd273ad3749375f6b33c`.
Все16 путей перечислены в историческом checkpoint ниже; новых source файлов
в ходе ревью не создавалось. Итоговая приёмка31/55 не пересчитывалась.

**Предложенная следующая единственная микрозадача, сейчас не выполнять:**
исправить REVIEW-01 — обычный Cancel+Retry настоящего модального diagnostic preview.
Проверяемый результат: нейтрализован implicit DialogResult, добавлен modal regression
на отмену build/save и retry; сохранены title-close/shutdown/Dispose ownership guards.
Никаких других функций, правок SSH launch, публикации или полного CI в этом блоке.
При недоступном Windows native результат остаётся NOT_RUN. REVIEW-02/03 отдельные
остатки; переходить к ним автоматически запрещено. Ждать команды владельца.

## История: восстановление после остановки владельцем — STOP

Проверено 21.10.2026 23:51 МСК. Историческая точка после прерывания;
дальнейшая разработка остановлена по прямой команде владельца. Восстановление
ограничено чтением процессов/Git/GitHub и сохранением этого раздела; новых
коммитов, push, PR mutations, запусков или повторов CI не выполнялось.

### Процессы и Git

- Активных дочерних агентов нет. Локальных ProGo/build/test/git процессов в
  snapshot `ps` нет; обнаружены только службы среды и текущая проверка.
  Процессы не завершались. Проверка не описывает процессы на ПК владельца.
- Ветка `work/progo-ci586-20261021`, HEAD
  `656b3d65e03d540be859ee69635c492631093446`, tree
  `5f9e6589ee195b8a584a131e0ed66a9dbedbd545`.
  Git index пуст: staged0; tracked modified12, untracked4.
  Merge/rebase/cherry-pick/revert/index.lock markers отсутствуют.
  Существующие worktree сохранены. Ничего не удалено, не сброшено и не откатано.
- Последние опубликованные коммиты: `d7e349d63b4d53eba5b7d98277996a89269a5ce8`
  (ShellLink читает snapshot из того же pinned handle через COM stream) и
  `656b3d65e03d540be859ee69635c492631093446` (CI585 failure scope/residual evidence).
  Git содержит14 опубликованных коммитов после исходной точки `7861ea9`;
  первые четыре логических изменения и последующие CI/разработческие исправления
  сохранены, API trees сверялись с локальными. Последний publication scan:
  336 added lines/6 files, secrets/PII findings0.

### Фактический PR и CI

[PR88](https://github.com/rkhnorkhan-bit/ProGo/pull/88) OPEN/DRAFT, merged=false.
GitHub head совпадает с локальным `656b3d65e03d540be859ee69635c492631093446`;
base main `4bb336575bb1216d3047bd2b294aeecad9fdd65a`.

[CI586 /39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699)
**completed SUCCESS**: Windows job119465588823 SUCCESS, Ubuntu job119465589235
SUCCESS. Checkout и build identity проверены по завершённому Windows log:
tested merge `7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`,
`0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.

Узкие результаты этого точного tested merge: Desktop4930 PASS; Codex shortcut
ownership67 PASS; settings initial-load preservation93 PASS; selected-key
signing55 и UI50 PASS; agent startup62 PASS; Maintenance126 PASS; Shutdown47
PASS; Home VPN975 PASS; Portable Home VPN90 PASS. Это не приёмка всего31/55,
не проверка незакоммиченных файлов и не установленная программа владельца.
F31-R11 остаётся PARTIAL: общая auth preparation перед own SSH/recovery TODO.

CI583/39660551553, CI584/39663669399, CI585/39667773240 перепроверены:
все completed FAILURE; в этих известных запусках активных jobs не осталось.
Новые CI не запускались. Их неуспехи сохранены как история, CI586 их не стирает.

Pack release zip в CI586 SUCCESS. Версия0.2.2, ZIP SHA256 по runner log:
`d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
Artifact metadata `[]`; screenshots/diagnostics/release folder/ZIP uploads SKIPPED.
ZIP не скачан и не передан: существование файла на временном runner не означает
доступный проверочный пакет. Публичный release/installer не разрешён;
public uploads OFF, способ приватной передачи ещё не согласован. Установка,
реальный ПК/VPS/маршрут, ACL/службы/системная сеть владельца NOT_RUN.

### Сохранённая незавершённая работа

**Не коммитить и не публиковать автоматически.** Два локальных блока не входят
в CI586. Финальное общее review и Windows compilation/runtime для них NOT_RUN.

- F31-R08: worker Build/save диагностического отчёта, Context single-flight,
  cancellation/shutdown ownership; token overload,12 native form сценариев,
  3 actual Context сценария, адаптация прежних UI/installed helper tests.
  Root-owned Context source review получил CLEAR после двух reentrant Disposed
  race fixes. Полное финальное review всего блока не зафиксировано.
  Logger/open/cold helper compilation остаток этим блоком не закрыт.
- F31-R11 блок1: новый immutable advisory aggregate fresh config/signing,
  общий budget и отдельная native suite. Финальное source review не зафиксировано.
  `ProxyService` aggregate ещё не вызывает; launch reservation/revision lease,
  recovery integration и соответствующий native lifecycle matrix остаются TODO.

Tracked modified12: `docs/ACCEPTANCE_RUNBOOK.md`,
`docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md`, `docs/PROJECT_STATE.md`,
`docs/project_state.json`, `scripts/Test-ProGo.ps1`, `src/DiagnosticPreview.cs`,
`src/DiagnosticReport.cs`, `src/UpdateAwareTrayApplicationContext.cs`,
`tests/DesktopDiagnosticTests.cs`, `tests/DesktopTests.cs`,
`tests/DiagnosticPreviewTests.ps1`, `tests/DiagnosticReportTests.cs`.
Новые untracked4: `src/SshAuthenticationPreparation.cs`,
`tests/DesktopAsyncDiagnosticPreviewTests.cs`, `tests/DesktopDiagnosticContextTests.cs`,
`tests/SshAuthenticationPreparationTests.cs`.

`git diff --check` PASS при восстановлении; только предупреждения нормализации
LF→CRLF в двух ранее изменённых файлах. Новый test run не выполнялся.
`project_state.json` и нижние исторические разделы ещё содержат pending/NOT_RUN
до получения completed CI586; они не являются текущим итогом этого запуска.
Этот раздел имеет приоритет для состояния остановки. Новые55/31 PASS не объявлены.

### Где остановились и следующий единственный шаг

До остановки одновременно ожидался CI586 и продолжалось незавершённое review
локальных диагностического/auth блоков. За время прохода последовательно
исправлялись FAIL предыдущих CI и автоматически начинались следующие блоки.
Последний Windows job длился около22м31с (20:25:25–20:47:56 UTC,
23:25:25–23:47:56 МСК), завершился успешно. Точной разбивки всех заявленных
владельцем1ч29м35с по активной работе и ожиданию нет; единственной доказанной
причины всей длительности установить нельзя. Это не подтверждение зависания CI.

**Только после следующего «продолжай»:** одна микрозадача — синхронизировать
completed CI586 в `docs/project_state.json`. Проверяемый результат: один
согласованный machine-readable checkpoint с точными source/merge/run/jobs,
SUCCESS только для опубликованного tested scope; локальные два блока остаются
NOT_RUN, все86 ID и отдельные integration/package/target слои сохранены.
Никакой реализации, нового CI или публикации в этой микрозадаче.

Новый порядок владельца: одно «продолжай» = одна заранее названная микрозадача
с одним критерием результата; ориентир5–15мин активной работы без обещания срока.
Долгий CI/сборку не ждать бесконечно: записать run ID/status и закончить ответ;
следующая команда проверяет тот же запуск. Новую микрозадачу автоматически
не начинать. Прежние ограничения публикации сохраняются.

## Основные 31 пункта

| ID | Требование | Статус | Реализация / область | Остаток / следующий шаг |
| --- | --- | --- | --- | --- |
| F01 | Общий CLI/Codex | PARTIAL | CliProxyEnvironmentService | Перепроверить на итоговом SHA и пакете: Миграции, один запрос CLI, on/off и итоговые переменные окружения. |
| F02 | Автоматика и повторы | PARTIAL | AutomationPlan; ProxyService | Перепроверить на итоговом SHA и пакете: Временный/постоянный отказ, ручная отмена, успешный повтор без перезапуска. |
| F03 | Один экземпляр | PARTIAL | ApplicationInstance; MaintenanceOperation | Перепроверить на итоговом SHA и пакете: Одновременный запуск, авария owner, handoff, update/restore/install без параллельной записи. |
| F04 | Честный статус связи | PARTIAL | ConnectionHealthMonitor | Перепроверить на итоговом SHA и пакете: Чужой listener, silent SOCKS, egress, NO_PROXY, устаревшие данные и частичный CLI. |
| F05 | Один клик и SSH-вход | PARTIAL | ProxyService; SshInteractiveLogin | Перепроверить на итоговом SHA и пакете: Задержка старта, отказ ключа, один клик CLI, отмена, host trust; настоящий VPS позже. |
| F06 | Возврат proxy-настроек | PARTIAL | CliProxyEnvironmentService; WindowsProxyService | Перепроверить на итоговом SHA и пакете: Типы/отсутствие значений, чужие правки, denied write, повтор очистки, uninstall и installer. |
| F07 | Сохранность копий | PARTIAL | BackupRetention; BackupService | Перепроверить на итоговом SHA и пакете: Preview/Cancel, изменившийся manifest, baseline/pre-update, новая копия и junction. |
| F08 | Транзакционный restore | PARTIAL | BackupIntegrity; Restore-ProGoBackup.ps1 | Поздняя отмена/очистка уже исправлялась. Сохранить результат CI582; новая итоговая регрессия restore/path/reparse/tamper/rollback и приёмка пакета. Старый FAIL без нового воспроизведения не считается текущим. |
| F09 | Настоящий и ложный vault | PARTIAL | VaultService; VaultDailyStore; VaultOpenService | PIN и фоновые vault операции уже реализованы. Нужны регрессия на итоговом SHA/пакете и приёмка установленного кандидата; повторная реализация не требуется. |
| F10 | Жизненный цикл bridge | PARTIAL | CliProxyBridgeService; HttpConnectProxyServer | Перепроверить на итоговом SHA и пакете: CLI/Windows/scoped отдельно, последний consumer, Shutdown(Send), нагрузка, Stop и ошибки cleanup. |
| F11 | Навигация и команды | PARTIAL | AppCommand; UpdateAwareTrayApplicationContext | Перепроверить на итоговом SHA и пакете: Главное окно, трей, настройки, закрытие диалогов, симметричные on/off. |
| F12 | Область отключения | PARTIAL | UpdateAwareTrayApplicationContext; HomeVpnService | Перепроверить на итоговом SHA и пакете: Stop во время Connect, независимость каналов, Quit и подтверждённое завершение. |
| F13 | Проверка маршрута | PARTIAL | RouteTester | Перепроверить на итоговом SHA и пакете: Первый/повторный запуск, закрытие окна, timeout и старый ответ. |
| F14 | Фоновая диагностика | PARTIAL | SshProfileDiagnostics; DiagnosticProcess | Перепроверить на итоговом SHA и пакете: Heartbeat UI, медленная сеть, отмена/timeout, отсутствие поздних изменений. |
| F15 | Save и немедленные действия | PARTIAL | SettingsService; SshProfilesSettingsForm | Перепроверить на итоговом SHA и пакете: Память/диск согласованы; Save/Cancel, ошибка поля, конкурентные изменения. |
| F16 | Адаптивность и DPI | PARTIAL | MainWindow; SshProfilesSettingsForm; Program | Перепроверить на итоговом SHA и пакете: Настоящие Windows DPI 100/125/150/200%, 1366×768, длинный текст, доступные главные действия. |
| F17 | Доступность | PARTIAL | UiTheme; native forms | Перепроверить на итоговом SHA и пакете: Tab/Shift+Tab/Enter/Esc, Narrator и High Contrast в среде разработки; недоступную среду отметить. |
| F18 | Выдача и установка | PARTIAL | HomeVpnWizardForm | Перепроверить на итоговом SHA и пакете: Выдача/отзыв/повтор не дают ложный installed; реальная установка проверяется позже. |
| F19 | Условия подключения | PARTIAL | HomeVpnWizardForm; HomeVpnService | Перепроверить на итоговом SHA и пакете: Стендовые ошибки сети; после установки - DNS, смена сети, idle, большие передачи и телефон. |
| F20 | Управление друзьями | PARTIAL | HomeInvitationsForm; server/home_vpn_setup.py | Перепроверить на итоговом SHA и пакете: Отзыв только выбранного доступа, исходный ID запроса, отмена списка, повтор/ошибка API. |
| F21 | Очистка clipboard | PARTIAL | ClipboardService | Перепроверить на итоговом SHA и пакете: Busy clipboard, таймер, закрытие/выход, чужая замена и пустой secret без ложного «скопировано». |
| F22 | Копии и перенос | PARTIAL | BackupService; HomeVpnPortableTransfer; Repair-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Tamper/path/reparse/cancel, чистая установка и другой Windows-пользователь; обычный rollback сохраняет данные. |
| F23 | Обновление и installer | PARTIAL | UpdateLauncher; Update-ProGo.Core.ps1; Install-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Прогресс/Cancel/offline, скачивание, hash, fixed paths, self-check, rollback; выпуск совпадает с commit/tag. |
| F24 | Помощь и версия | PARTIAL | HelpForm; README.ru.md | Перепроверить на итоговом SHA и пакете: UI/документы/пакеты совпадают; понятные Windows/CLI/Android/iPhone сценарии без личных примеров. |
| F25 | Журналы и экспорт | PARTIAL | BoundedLog; DiagnosticReport; DiagnosticPreview | Перепроверить на итоговом SHA и пакете: Пределы журнала, отчёт до отправки, ошибки чтения, отсутствие секретов и блокировки запуска. |
| F26 | Автозапуск | PARTIAL | ApplicationShortcuts; Install-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Комбинации настроек в dev Windows; фактический вход владельца - после установки. |
| F27 | Подпись и антивирусы | PARTIAL | .github/workflows/release.yml | Перепроверить на итоговом SHA и пакете: Совпадение payload, проверяемая подпись, timestamp и отдельные Defender/Kaspersky результаты. |
| F28 | Команды и поддержка | PARTIAL | AppCommand; Test-ProGo.ps1 | Перепроверить на итоговом SHA и пакете: Нет неописанных handlers; отдельно scenarios/assertions, FAIL/SKIP/NOT_CHECKED и общий результат. |
| F29 | Визуальный стиль | PARTIAL | BrandIcon; UiTheme | Перепроверить на итоговом SHA и пакете: Native screenshots общей сборки, 16/24 px на разных фонах, читаемость и геометрия. |
| F30 | Почта, кабинет, биллинг | PARTIAL | server/profile_share.py; HomeInvitationsForm; remaining commercial modules absent | Перепроверить на итоговом SHA и пакете: Sandbox/интеграционные сценарии, проверенный webhook, дубликаты, возврат, paid→activation retry, отзыв активных сеансов. |
| F31 | SSH, переносимость, скорость | PARTIAL | Program; ProxyService; SshAgent*; SshProfile*; UpdateAwareTrayApplicationContext; HomeVpnService | Перепроверить на итоговом SHA и пакете: Dev service/key/ACL/cancel/ownership и slow disk/network fixtures, latency/CPU/I/O; фактический encrypted agent, другой ПК/VPS и Rescue замеры — после установки. |

## Декомпозиция остатка: 55 подпунктов

| ID → основной | Требование | Статус | Историческое свидетельство | Зависимости | Следующий шаг |
| --- | --- | --- | --- | --- | --- |
| F16-R01 → F16 | Полнота адаптивной компоновки существующих вспомогательных окон | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R02 → F16 | Адаптивность новых installer и F30 экранов | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R03 → F16 | DPI awareness и реальная смена монитора | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F16-R04 → F16 | Итоговая native DPI/display приёмка | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R01 → F17 | Полнота клавиатуры и доступных имён/состояний | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R02 → F17 | Читаемые disabled и узнаваемые pending кнопки | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F17-R03 → F17 | PIN hint/status без дублирования | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F17-R04 → F17 | Реальная Narrator приёмка | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F17-R05 → F17 | Реальная High Contrast и видимый фокус | PARTIAL | Dev NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R01 → F19 | Доказательный LAN/NAT/CGNAT preflight | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R02 → F19 | Подтверждённые входящие UDP-пути | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R03 → F19 | Интегрированная поэтапная диагностика | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R04 → F19 | Настоящий телефон, маршрут и DNS | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F19-R05 → F19 | Устойчивость настоящего подключения | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R01 → F22 | Состав обычных копий на итоговом пакете | PARTIAL | Приёмка готовой реализации | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R02 → F22 | Защищённый VPN export/import на чистом dev Windows под другим SID | PARTIAL | Частично; cross-SID NOT_CHECKED | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R03 → F22 | Полный runtime repair | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F22-R04 → F22 | Фактический перенос владельца между ПК | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R01 → F23 | Готовый standalone setup с полным runtime | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R02 → F23 | Installer в общем maintenance protocol | PARTIAL | Частично | F23-R01 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R03 → F23 | Одна кнопка и автоматический выбор updater/installer | TODO | Предстоит | F23-R01, F23-R02, F23-R04, F23-R05 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R04 → F23 | Подтверждённый честный rollback updater | PARTIAL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R05 → F23 | Неизменяемая идентичность принятого выпуска | PARTIAL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R06 → F23 | Единый итоговый setup/updater и dev приёмка | PARTIAL | Package/Dev PENDING | F23-R01, F23-R02, F23-R03, F23-R04, F23-R05, F31-R11 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F23-R07 → F23 | Установленный update workflow владельца | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R01 → F27 | Signing/verification pipeline поставляемых артефактов | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R02 → F27 | Проверка издателя и доверия updater payload | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R03 → F27 | Реальный сертификат и подписанный итоговый выпуск | BLOCKED | Внешняя зависимость | F27-R01, F27-R02, owner certificate | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F27-R04 → F27 | Антивирусная приёмка конечных артефактов | NOT_RUN | NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R01 → F30 | Почтовая выдача и согласованная QR выдача | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R02 → F30 | Серверные коммерческие данные и права | TODO | Предстоит | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R03 → F30 | VPS-кабинет и вход в него из ProGo | TODO | Предстоит | F30-R02 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R04 → F30 | Коммерческий жизненный цикл доступа | TODO | Предстоит | F30-R02 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R05 → F30 | Серверное исполнение ограничений и активные сеансы | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R06 → F30 | Онлайн-заказ и платёжный адаптер | TODO | Предстоит | F30-R02, owner payment provider choice | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R07 → F30 | Проверенные платёжные события и активация | TODO | Предстоит | F30-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R08 → F30 | Технический агентский контур | TODO | Предстоит | F30-R02, owner supplier/provider data | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R09 → F30 | Финансовый реестр и сверка | TODO | Предстоит | F30-R07, F30-R08 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R10 → F30 | Реальная сервисная почта | BLOCKED | Внешняя зависимость | owner mail account/provider | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R11 → F30 | Реальные собственные и агентские платежи | BLOCKED | Внешняя зависимость | F30-R06, F30-R07, F30-R08, F30-R09, owner explicit live authorization | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F30-R12 → F30 | Коммерческий lifecycle на реальном VPS и клиентах | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R01 → F31 | SSH IPv6 | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R02 → F31 | Профильный fallback | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R03 → F31 | Постоянные ошибки SSH config/arguments | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R04 → F31 | Include/aliases/IdentityFile tokens в диагностике | PARTIAL | **Code/Dev PASS** | нет внешней блокировки | Подтвердить сохранение исторического Code/Dev критерия на новом SHA. |
| F31-R05 → F31 | Остаточные startup/settings/shortcut операции вне UI | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R06 → F31 | Фоновый retention preview/cleanup | PARTIAL | Исправлен source-confirmed UI I/O; native NOT_RUN | новый CI, итоговый пакет | Выполнить AsyncCleanupWorkflow и retention regression на точном SHA; visual/target отдельно. |
| F31-R07 → F31 | Фоновое ожидание updater handoff | PARTIAL | Source fix и независимое review; native NOT_RUN | новый CI, пакет | Выполнить owned backend и actual Update route suites; ACK/cancel/timeout/queued lifecycle regression. |
| F31-R08 → F31 | Фоновая работа logs/diagnostic export | FAIL | Source recheck d0f9a690: UI disk I/O | нет внешней блокировки | Tracked Build/save, затем logger/open; native heartbeat и snapshot/cancel. |
| F31-R09 → F31 | HomeVPN private I/O, DPAPI и session preparation в worker | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R10 → F31 | Асинхронные phone Stop/cleanup | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R11 → F31 | Подготовка SSH-agent и итоговая dev приёмка F31 | PARTIAL | Dev PENDING | selected-key readiness, startup ordering, F31-R05, F31-R06, F31-R07, F31-R08, F31-R09, F31-R10 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R12 → F31 | Настоящий encrypted-key agent и маршрут | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R13 → F31 | SSH-профили на другом Windows ПК/пользователе и VPS | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R14 → F31 | Installed build в Rescue/HDD/VHDX | NOT_RUN | Live NOT_CHECKED | F23-R06 | Выполнить критерий; зафиксировать тесты и точный SHA. |

## Очерёдность завершения

1. Исправить подтверждённые startup/QR/rollback дефекты; новый Windows+Ubuntu CI, независимое ревью diff.
2. Завершить готовность конкретного выбранного SSH-ключа и связать её со всеми путями запуска. Наличие `.pub`/отпечатка не считать proof приватного ключа/подписи/доступа. F31-R11 остаётся PARTIAL.
3. Закрыть оставшиеся UI workers (retention/handoff/logs/HomeVPN), transfer/repair, сетевой preflight и итоговые DPI/accessibility критерии.
4. Реализовать согласованный setup/updater fallback, подпись pipeline и коммерческий dev-контур. Provider-specific адаптер требует выбора реальных провайдеров; это не блокирует независимую серверную модель.
5. Сформировать общий приватный проверочный пакет; после отдельной установки владельцем пройти live gates.

## Протокол передачи и аудит

См. [TAKEOVER_AUDIT_20261021.md](TAKEOVER_AUDIT_20261021.md): источники/пробелы, Git/исполнители, находки, воспроизведение и проверки. Точка продолжения и новые SHA/CI фиксируются там.

## История: исходная точка продолжения перед CI583

Code/test source `5c803be5534601233e0041202890cbd7c04cd575`:
QR `bd9c0fb17b4ca4e84bdcec2cf5e1c6d90820f69d`,
startup `e2fccacb8c7a859217cc854cb91bc7ee7ac57bd8`,
rollback `5c803be5534601233e0041202890cbd7c04cd575`.
Общая Python discovery на этом source:44 reported,41 PASS,3 SKIP (C# harness).
Public-content и diff checks PASS; runtime Windows NOT_RUN. Независимые source
review startup/rollback завершены. Исторический непринятый остаток49=37 до
установщика+12 зависимых; новых закрытых подпунктов55 в этом сеансе нет.
Шесть прежних scoped PASS сохраняют свои свидетельства, но не заменяют новую
итоговую регрессию. Инструкция дальнейшей приёмки/возврата:
[ACCEPTANCE_RUNBOOK.md](ACCEPTANCE_RUNBOOK.md).

Следующим шагом на этом историческом этапе было исправление подтверждённого FAIL CI583, публикация
связанных signing/settings изменений после проверки секретов, новый Windows/Ubuntu CI
на точном SHA. Прежняя privacy decision уже получена; способ передачи пакета
ещё не согласован. Полный F31-R11 остаётся открытым.

## Следующий связанный блок: выбранный ключ

Source review пройден для SshKeyReadiness, frozen Unicode child environment и
явного «Проверить ключ» в SSH диагностике. Есть отдельные native backend/UI suites,
строгое расширение прежнего keyboard test; timeout/Cancel/Close/freshness покрыты
подготовленными сценариями. Фактический новый Windows run пока NOT_RUN.
Протокол: [MICROTASK_SSH_SELECTED_KEY_SIGNING.md](MICROTASK_SSH_SELECTED_KEY_SIGNING.md).
Подпись публичной reference не доказывает private bytes либо вход на VPS;
автоматическая подготовка выбранного auth перед SSH остаётся в F31-R11.

## CI583: подтверждённый результат и исправление

Run [39660551553](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39660551553),
source `e9fcfc3279269075eddd6def306c1ca2500cd28c`,
tested merge `d379acd6a0dc5dc55d5d01417cc5c674237d1f27`.
Ubuntu SUCCESS. Windows FAIL: Windows PowerShell5.1 прочитал новый русский
текст в `tests/MaintenanceTests.ps1` как ANSI: UTF-8 файл не имел BOM.
ParserError на строке256 возник до выполнения MaintenanceTests.
До ошибки успешно выполнены SOCKS recovery41/403assertions,
SSH-agent startup62, Desktop4539, Instance28. Maintenance/shutdown/HomeVPN,
package и полный F23-R04 — NOT_RUN на этом source. CI583 остаётся FAIL.

Исправление сохраняет все assertions и таймауты: MaintenanceTests получает
UTF-8 BOM; `scripts/Test-ProGo.ps1` до build/native suites вызывает ParseFile
для каждого `tests/*.ps1` с тем же декодированием, что Windows PowerShell.
Следующий CI проверит и этот fix, и новый выбранный ключ; старые scoped PASS
не подтверждают более поздний код.

## A11: сохранность настроек при ошибке первоначального чтения

`SettingsService.Load/SaveCore` теперь держит sticky read-failure guard:
файл, который не удалось прочитать/разобрать при создании сервиса, нельзя
заменить fallback defaults через Save/TrySave/tray/rollback/async Save.
Новая ошибка имеет фиксированный текст без пути/профиля; обычное сохранение
через настройки показывает инструкцию проверить доступ/формат и перезапустить.
Genuine missing создаёт defaults через exclusive Move: появившийся параллельно
чужой файл сохраняется. Поздний detached Load не публикует и не снимает guard.
После устранения причины новый запуск читает исходные profiles/unknown fields.

Независимое source review PASS; новый native suite зарегистрирован один раз.
Реальные isolated lock→unlock, malformed/null JSON, missing/creation race,
все write APIs и fresh trusted recovery — подготовлены, Windows NOT_RUN.
A11 становится PARTIAL до точного CI; F15/F22/F31-R05 полностью не закрыты.

## История: опубликованный кандидат CI584

PR88, source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`,
merge `2f6b901a6fd537e0e0eeb81c0431b200e29260b2`,
run [39663669399](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39663669399).
Signing `28b7a8a155abff0e0f073adcf07ac4f68f2d02e9`,
encoding `883787b6a392020fd10bb07e00f80bfff00e3bf5`,
settings `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`.
Каждый API tree совпал с локальным; expected-head fast-forward без force.
До публикации17files/1499addedlines: targeted secrets/PII0findings,
public-content305/0 и diff checks PASS. Ubuntu completed SUCCESS;
Windows in_progress, общего PASS нет. Public uploads выключены.

Точный остаток auth preparation и исключения direct/custom/other-auth:
[MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md](MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md).
Реальный общий launch в ProxyService покрывает и StartTunnelCore, и PollRecovery;
одна вставка в Context не закрыла бы F31-R11. Новый запрет SSH по неуспеху
локальной подписи не введён.

## F31-R06: фоновая очистка подтверждённых копий

Tray CleanupBackups теперь запускает одну BackupWorkSession: план в worker,
готовый точный список и явное подтверждение на UI, fresh revalidation/delete
в worker. Повторные cleanup/restore/manual requests разделяют текущий actor;
команды удерживаются до полного workflow и owner completion. Cancel/Close,
shutdown refusal/retry и forced disposal сохраняют ownership; поздняя публикация
на закрытый UI не требуется для завершения worker.

Старые retention APIs сохраняются; token overloads проверяют отмену при чтении,
обходе и перед каждым удалением. Approved list не расширяется; изменённый manifest,
manual/unknown и junction guards сохранены. In-flight Directory.Delete не имеет
принудительной отмены: worker остаётся owned до его возврата. Успешный фактический
результат после последнего delete не превращается в abort при поздней отмене.
При отмене/ошибке UI честно сообщает, что уже удалённые копии не восстанавливаются.

Независимое backend source review и root UI/test review PASS; diff/public scan PASS.
Низкоуровневый pre-cancel/no-delete и10 native actor/preview/revalidation/lifecycle
сценариев подготовлены. Отмена внутри OS delete не исполнена/не заявлена.
Runtime всех новых retention изменений NOT_RUN до следующего CI; F31-R06 PARTIAL.

## CI584 completed FAIL: scoped key/settings PASS, fixture encoding

Source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`,
merge `2f6b901a6fd537e0e0eeb81c0431b200e29260b2`, run39663669399.
Ubuntu SUCCESS, Windows FAILURE. Подтверждены backend signing55, signing UI50,
settings initial-load preservation93, Desktop4739, agent startup62, Instance28.
BOM/ранний parser fix CI583 прошёл; Maintenance дошёл до реального rollback.
Первый новый rollback-copy-fail прошёл критерии failure и отсутствие false-success
markers, затем упал assertion русского текста диалога. Driver записывал текст
через Set-Content без encoding. PS5.1 default ANSI делает capture зависимым
от active code page; потеря кириллицы — вывод по source/failure. Точная ACP и bytes
CI584 не сохранены, поэтому это не отдельное наблюдение байтов старого run.
Это отдельный дефект транспорта тестового evidence, не признание rollback PASS.
Исправлены writer/read encoding на UTF8, исходные semantic assertions сохранены.
Остальные rollback vectors/shutdown/HomeVPN/package NOT_RUN в этом CI.

Source signing/settings criteria имеют scoped PASS на6b3122f0; visual review новых
PNG и целевой ПК NOT_RUN. Новые retention/updater изменения требуют отдельного CI.

## F31-R07: worker handoff и граница подтверждения помощника

Actual Update route теперь StartUpdate → StartUpdaterAsync → UpdaterCore →
LaunchUpdater в tracked BackupWorkSession. Installed-path I/O и ожидание ACK идут
в worker, UI получает progress/Cancel и fixed nonsecret ошибки. Backend использует
StartOwnedHandoff: до ACK владеет suspended/job-contained tree; после ACK поздняя
отмена не отзывает независимого помощника. Legacy StartUpdater wrapper сохранён,
actual route его не вызывает. Guard в Test-ProGo проверяет новый owned API.

Shared restore/update reservation не допускает выход/новые mutations до settlement.
Исправлены найденные независимым review queued races: accepted flags до снятия
reservation и reentrant UI callbacks; старый timeout не сбрасывает Accepted;
старый settle не сбрасывает новый ordinary shutdown; rejected flags очищаются до
callback. Own ordinary completion не зависит от чужого pending reserved task.
Ранее admitted native enable может завершить свою принятую фазу; preflight ждёт
её и затем отключает. Новый reconnect/automation после reservation подавляется,
публикация committed config сохраняется. Это не принудительная отмена OS вызова.

Backend independent review и финальный независимый lifecycle/tests review PASS.
Actual helper/descendant accept/reject/cancel/timeout и10 native Context сценариев
подготовлены, зарегистрированы; реальные установки не выполняются. Старые
WindowsRestore gate assertions перенесены на actual updater route и усилены.
Новый native run NOT_RUN; F31-R07 и общий F31-R11 остаются PARTIAL.

## A12: fresh ownership ярлыка Codex

`CodexProxyService.Enable/Disable` проверяет реальную ShellLink target и точные
arguments текущего launcher до изменения cmd/lnk. Copied description/один Marker
не доказывают ownership ярлыка. Проверенный объект удерживается read/delete handle;
Disable задаёт delete disposition именно ему. Existing owned link не переписывается,
новый link/cmd публикуется Move без overwrite. Missing parent не скрывает junction
выше: проверяются все ancestors до root. Foreign/malformed/denied/read-only/reparse
link даёт отказ с сохранением чужих файлов. Legacy MoveOwned/IsOwned не меняются.

Root source review обнаружил и затем подтвердил исправление ancestor bypass.
Реальные isolated COM/ACL/sharing/NTFS junction/replacement/pinned-handle/repeat
сценарии зарегистрированы. Native Windows NOT_RUN; A12 PARTIAL до точного CI.
Запись existing owned cmd остаётся in-place как прежде; общей атомарной транзакции
двух файлов и сохранности собственных bytes при любой I/O-ошибке не заявляется.
F31-R05 и F06 полностью не закрываются этим узким guard.

## История: CI585 и сверка остатка

Source `d0f9a6902345eab288b25e8f902df1514db3f2ca`, merge
`4b952b8b4e3a6473ceac230732384046801ce7fd`,
[run39667773240](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39667773240).
Ubuntu SUCCESS, Windows in_progress; полного PASS нет.
20files/1905addedlines перед push:0 secrets/PII findings, public-content310/0,
diff PASS; Python41PASS/3SKIP. Новые published commits/tree mapping в JSON.

[RESIDUAL_RECHECK_20261021.md](RESIDUAL_RECHECK_20261021.md) фиксирует повторную
независимую проверку на этом source. A04/F22-R03 и F31-R08/R09/R10 подтверждены
как source FAIL; native latency/воспроизведение/ПК NOT_RUN. Исправленные R06/R07
их не закрывают. Никакие owner PC/VPS/repair действия для проверки не выполнялись.

## CI585 completed FAIL: native ShellLink sharing

Source d0f9a690 / merge4b952b8b / run39667773240: Ubuntu SUCCESS,
Windows FAILURE. В CodexShortcutRepeat настоящий IPersistFile.Load упал с
FileLoadException0x80070020: COM повторно открывает pathname без share-delete,
несовместимо с удерживаемым DELETE handle guard. Это новый production guard bug,
не flaky timeout. Guard/ownership не ослабляются: вместо path-open ShellLink
загружается через IPersistStream из bounded bytes того же pinned file object.
Новый полный Desktop/R06/R07/key run, Maintenance encoding fix, shutdown/HomeVPN
и package остаются NOT_RUN в CI585. Предыдущие55/50/93/4739 — scoped CI584,
их нельзя перенести на source d0f9a690. Новый runtime fix получит отдельный CI.

## A12 stream-loader: исправление конкретного native FAIL

После CI585 ShellLink parsing использует снимок1..1МиБ из того же pinned
FileStream → SHCreateMemStream → IPersistStream.Load. Повторного pathname-open
нет; DELETE/share-read и handle deletion сохранены. Native object и memory stream
освобождаются nested finally, pinned file живёт до mutation caller.
Добавлены empty/oversize и отказ обычного File.OpenRead под DELETE pin.
Independent source review PASS; новый native run NOT_RUN. CI585 не исправляется
задним числом. В нём отдельно PASS29 standalone retention checks, включая новый
real pre-cancel/no-delete; десять cleanup Context сценариев ещё NOT_RUN.
