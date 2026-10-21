# Приёмка следующего ProGo кандидата

Текущий опубликованный source B: версия0.2.2,
`656b3d65e03d540be859ee69635c492631093446`, PR88 OPEN/DRAFT, unmerged.
[CI586 /39670153699](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39670153699)
**completed SUCCESS только для tested merge M**
`7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.
Windows job119465588823 и Ubuntu job119465589235 completed SUCCESS;
build identity `0.2.2+7e204c7659cefd8142bb33a3cbfc7cc0ec974bc2`.
Jobs и точный Windows log повторно прочитаны в REVIEW-03; новый CI не запускался.
История CI583–585 с FAILURE и их точными SHA сохранена в JSON/PROJECT_STATE.
Прежнее наблюдение CI586 in_progress хранится только как prior_observation,
не как текущий статус. CI582 — отдельная историческая база.

Pack release zip SUCCESS, SHA256 по runner log:
`d7a6176ec2da62a5b53e2faea2a86893919a853353a5166f669baa0042896191`.
Uploads SKIPPED; ZIP не скачан/не передан. Hash runner не заменяет hash
полученных bytes. Полный standalone setup, согласованный updater/installer
fallback и установка/приёмка на целевом ПК не готовы. Публичный release/installer
не разрешён; способ приватной передачи пакета остаётся BLOCKED.

**Текущий локальный кандидат L — незакоммиченный diff поверх B, отдельного SHA
ещё нет. Windows compilation/native NOT_RUN** для диагностики/report/Context,
advisory SSH preparation, REVIEW-01 ShowDialog Cancel→Retry и REVIEW-02 native
SaveFileDialog. CI586 SUCCESS на L не распространяется. F31-R11 PARTIAL:
агрегатор не подключён к общему SSH start/recovery. Верхнеуровневые31 и55
критерии этим CI не закрыты. REVIEW-03 меняет только3 документа; commit/push/
новый CI, код и тесты в этой микрозадаче не выполняются/не меняются.

## Постоянный порядок и текущий разрешённый этап

С22.10.2026 действует постоянный [порядок](PROJECT_STATE.md): продолжать
согласованные микрозадачи самостоятельно; source push в существующую рабочую
ветку, PR88 и необходимые CI разрешены также для последующих исправлений.
Прежние обязательные остановки/ожидание «продолжай» отменены. Границы release,
binary upload, main merge, ПК/VPS/служб/ACL/сети сохраняются. Каждый source push
требует scan secrets/PII и проверки автоматических workflow. Новый режим не
создаёт расписания или автоматического перезапуска исполнителя.

Следующий ниже16-file блок теперь разрешён к local commit/source publication
и exact-SHA Windows CI. В этой точке он ещё не отправлен/не проверен; CI586
по-прежнему не подтверждает его. Автоматический public upload остаётся OFF.

## Проверочный коммит — согласованный состав

Предлагаемый отдельный commit: `Bound diagnostic preview lifecycle and add advisory SSH preparation`.
База B указана выше; новый source SHA фиксируется только после реального commit.
Включить **ровно16 текущих modified/untracked файлов** (после финальной сверки
содержимого и secrets/PII; без generated build/release payload):

- `src/DiagnosticPreview.cs`
- `src/DiagnosticReport.cs`
- `src/UpdateAwareTrayApplicationContext.cs`
- `src/SshAuthenticationPreparation.cs`
- `tests/DesktopDiagnosticTests.cs`
- `tests/DesktopTests.cs`
- `tests/DiagnosticPreviewTests.ps1`
- `tests/DiagnosticReportTests.cs`
- `tests/DesktopAsyncDiagnosticPreviewTests.cs`
- `tests/DesktopDiagnosticContextTests.cs`
- `tests/SshAuthenticationPreparationTests.cs`
- `scripts/Test-ProGo.ps1`
- `docs/ACCEPTANCE_RUNBOOK.md`
- `docs/MICROTASK_SSH_AUTH_PREPARATION_SCOPE.md`
- `docs/PROJECT_STATE.md`
- `docs/project_state.json`

Это один диагностический/advisory блок вместе с REVIEW-01/02 и документацией;
он не реализует SSH launch/recovery integration, repair или installer fallback.
Во время REVIEW-03 commit/stage/push не выполнялись. Теперь эти source действия
и необходимый CI разрешены постоянным порядком; перед ними сверить фактическое
состояние и не дублировать уже запущенную операцию.

### Минимальный готовый Windows-прогон накопленного блока

На изолированном Windows runner с .NET Framework/OpenSSH, тем же CI окружением
и доступным native desktop зафиксировать чистый checkout нового source SHA,
а при PR merge — также точный tested merge SHA. Существующий entrypoint:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\scripts\Test-ProGo.ps1
```

Скрипт сам вызывает Build-ProGo: отдельно повторять сборку не требуется.
У текущего DesktopTests нет selector этих suites; поэтому минимальный готовый
путь включает общий Desktop harness и существующие Windows регрессии скрипта.
Это один Windows entrypoint, не запуск всего Actions workflow: отдельные
Ubuntu jobs, Test-HomeVpnRelay, ZIP packing/upload для этого локального блока
не требуются. Целевая приёмка ПК/VPS и всего продукта остаётся отдельной.

Обязательные доказательства нового SHA:

1. C# compilation приложения и всех связанных tests; DiagnosticReportTests
   с cancellation и прежними limits/redaction/snapshot критериями.
2. DiagnosticPreviewTests.ps1 в новом PowerShell-процессе: установленный shared
   loader/API и точный reviewed snapshot; DiagnosticPreviewWorkflow сохраняет
   клавиатуру, minimum layout и действия.
3. AsyncDiagnosticPreviewWorkflow: реальные file build/save, heartbeat,
   single-flight, close/Dispose и отсутствие позднего результата/записи;
   четыре реальных ShowDialog Cancel→Retry (build/save × click/Esc),
   три native chooser режима (Cancel, missing caption, injected error).
   В основном режиме timeout всегда FAIL; негативные режимы требуют точного
   ненулевого20/21 и cleanup marker, а не любого зависания/ненулевого exit.
4. DiagnosticContextWorkflow: существующие actual tray/Context shutdown/Dispose
   и ownership сценарии; старые Desktop/Instance/Maintenance/Shutdown регрессии
   не теряются из-за изменения tray context.
5. SshAuthenticationPreparationWorkflow: настоящий локальный ssh-G и test-owned
   agent pipe, total budget, cancel/freshness/config mutation; существующие
   selected-key sign/UI, policy, agent start/coordination сценарии. Это проверка
   advisory aggregate, не live SSH auth и не launch/recovery integration.

SKIP/NOT_RUN не считаются выполненной проверкой: если изоляционные guards
пропустили suite, её критерий остаётся NOT_RUN даже при exit0 общего скрипта.
Сохранить source SHA/tested SHA, build identity, фактические результаты и logs.
При долгом запуске сохранить ID job/сессии; во время ожидания продолжать
независимые разрешённые задачи и проверять тот же запуск без дубликатов. Установка на ПК владельца,
реальные VPS/службы/ACL/системная сеть в этот Windows dev-прогон не входят.

## До передачи установщика

1. Push/CI в публичный ProGo явно разрешены21.10.2026. Публичный релиз и
   публикация установщика не разрешены. Согласовать передачу пакета; public
   artifact upload OFF, явный dispatch opt-in пока не применять.
2. На точном новом SHA пройти существующие Windows Test-ProGo/Test-HomeVpnRelay
   и Ubuntu isolated suites. При первом FAIL сохранить logs/исходный SHA,
   установить причину, исправить и тестировать новый SHA; не скрывать ранний FAIL.
3. Завершить остальные обязательные dev-критерии PROJECT_STATE.md, включая
   selected-key readiness, полный setup/fallback и package gates. Одна зелёная
   промежуточная CI ZIP не означает завершение31/55.
4. Для успешного пакета зафиксировать version, source SHA, tested merge SHA,
   artifact ID, SHA256 скачанных bytes, состав и unsigned/signed состояние.
   Проверить архив и инструкцию возврата. Пакет должен оставаться приватным.

## Установка владельцем после отдельного разрешения

1. Зафиксировать identity/версию работающей старой программы и проверенную
   резервную копию. Сохранить профили/настройки/vault/личный VPN доступ и paths.
   Приватные SSH-ключи не включать в диагностический отчёт или пакет.
2. Сверить SHA256 кандидата и его source identity. Закрыть ProGo штатно;
   запустить проверенный setup/update с зафиксированными существующими путями.
   Опцию изменения общей службы SSH-agent выбирать отдельно; она OFF по умолчанию.
3. Пройти сценарии ниже. При частичном результате записать точный шаг, причину,
   installed identity и время; не объявлять общий PASS.

| Сценарий | Необходимое свидетельство |
| --- | --- |
| Запуск/отзывчивость | Открываются главное окно/настройки; heartbeat и действия отвечают при медленном диске/сети; latency/CPU/RAM/I/O измерены |
| Профиль | Выбранные alias/user/port/IdentityFile соответствуют effective OpenSSH; нет старого absolute path другого ПК |
| Агент и выбранный ключ | Running/Stopped/Disabled/denied различаются; запуск в доступных правах; encrypted key разблокирован один раз, секрет нигде не сохранён |
| SSH → SOCKS → HTTP/HTTPS | Owned SSH, настоящий SOCKS handshake, HTTP и CONNECT; внешний маршрут проверен отдельно; публичный fingerprint не выдаётся за auth |
| Отказ и отмена | Неверный/недоступный ключ, changed host key, network drop, Stop/Close; нет позднего старта/дублей/завершения чужого процесса |
| Восстановление | Ручной повтор после исправления и допустимый backoff; причины/последний результат актуальны |
| Обновление | Оба пути update/setup, fixed paths, cancel/offline/hash failure, сохранность данных; rollback success только после проверки |
| Перенос | Другой Windows SID/ПК и другой разрешённый VPS; локальный ключ выбирается явно, VPN archive scope не расширяется |

## Возврат к предыдущей версии

Использовать заранее проверенную копию и штатный **Program-only** restore,
сохранив текущие настройки/vault/VPN/request/proxy ownership journals.
Проверить hashes/версию/self-check восстановленного runtime до запуска.
Если rollback не подтверждён, сохранить backup/log/recovery evidence и
остановиться на точной ошибке. Не подтверждать отдельную замену personal data
по умолчанию. Затем повторить запуск/профиль/маршрут на прежней версии.

Живые действия выполняются только на отдельно разрешённых ресурсах. Эта
инструкция не даёт автоматического разрешения установки, правок VPS/ACL/сети
или реальных писем/платежей.
