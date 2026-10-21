# ProGo: передача, аудит и продолжение 21.10.2026

## Проверенная база и источники

- Linux, отдельный worktree `/workspace/scratch/4d867f513347/ProGo`, ветка
  `work/progo-takeover-20261021`. Старые worktree чистые, не переключались.
- Применимые AGENTS.md при обходе `/workspace` и проверке родительских каталогов
  не найдены. Не обнаружены Git locks/rebase/merge и отдельные работающие
  ProGo/build/test процессы. Это наблюдение доступной среды, не доказательство
  отсутствия исполнителя на чужом ПК. Текущее поручение владельца — передача работ.
- Выбрана `integration/approved-plan-31-20261011`, PR88, head
  `7861ea919eed24c5fe9964fc35a23d3958ba5a9d`. Main остаётся
  `4bb336575bb1216d3047bd2b294aeecad9fdd65a`. PR79/80/81 и поздние PR82–87
  входят в интеграционную историю; проверено ancestor для head79/80/81/87.
  Открытые PR не закрывались и не сливались. Более поздний PR, чем 88, в
  доступной выборке всех 88 PR не найден.
- `qa/f23k-native-check` не ancestor: это отдельная прежняя QA-линия,
  последнее изменение добавляет workflow. Автоматический merge/cherry-pick не
  выполнялся; её существование не основание брать более старую продуктовую базу.
- CI582, run39481464526: обе jobs completed/success независимо прочитаны;
  Windows job118830819541 log прочитан. Tested source `b02b0e9606fb57518ed18d17de42e5f82fca8fa8`,
  merge `cf2e19691057c143b4f66d93c0041e2e1c5681a8`.
  Diff source→7861ea9 содержит только четыре Markdown-файла, 79 добавлений/4 удаления.
  Успех не распространяется на новые изменения этого аудита.
- Исходная `PROGO_SSH_AGENT_AUTOSTART_SPEC(1).md` найдена и прочитана локально.
  Каноническая очищенная спецификация — `SSH_AGENT_AUTOSTART_SPEC.md`.
  Личные адреса, пути и ключи из исходного вложения в код/отчёт не копируются.
- Обе ссылки на chatgpt.com/c из поручения возвращают DisabledError при прямом
  чтении. Получены отдельные поисковые извлечения Personal Context, а не полные
  чаты. Полное чтение обеих историй НЕ подтверждено. Для исчерпывающей сверки
  сообщений нужен экспорт этих двух чатов; независимая реализация продолжается
  по согласованным документам репозитория.

Текущий реестр: [PROJECT_STATE.md](PROJECT_STATE.md), точные 86 строк/слои —
[project_state.json](project_state.json). Утверждённый объём: F01–F31;
55 — разложение остатка восьми F-пунктов, а не новая версия всех 31.
Исторические 23/31 и 6/55 не суммируются и не задают процент готовности.

## Архитектура, зависимости и границы проверки

Приложение — native WinForms/C#, .NET Framework4.8, Windows OpenSSH и
PowerShell5.1; компиляция штатным Windows csc. Профили/настройки — локальный
JSON, секреты HomeVPN — CurrentUser DPAPI, vault — существующий PIN/KDF/envelope.
SSH/SOCKS владеет ProxyService, HTTP/CONNECT — локальный bridge, серверные
компоненты — Python/strongSwan. CI отдельно выполняет Windows и изолированный
Ubuntu provisioning. В dependency manifests не обнаружен отдельный NuGet/npm
runtime-контур; это не скан CVE всех компонентов ОС.

Независимый source review выполнен для SSH/agent/startup/process lifecycle,
QR/link/session issuance, PIN-контракта, backup/update/repair и UI worker gaps.
Существующие тесты и документы проверены как свидетельства с границами.
Полная итоговая Windows/system/network/package приёмка пока NOT_RUN:
локальная Linux-среда не исполняет native WinForms/DPAPI/SCM.
Настоящие ключи/VPS, WinHTTP, службы/ACL/защита и устройство владельца не менялись.
Rescue/HDD/VHDX — исторический контекст, текущая неисправность не утверждается.

## Подтверждённые находки

Номера строк ниже относятся к исходной базе 7861ea9; после правок используйте
символы и diff. Статическое доказательство отдельно от фактического воспроизведения.

| ID / состояние | Файл и символ, доказательство | Влияние и воспроизведение | Исправление и проверка |
| --- | --- | --- | --- |
| A01 / FAIL → исправляется | `src/Program.cs:71–78`, Main: AutoStartSocks запускает ProxyService раньше Attach/StartSshAgentAsync; `ProxyService.StartTunnelCore` сразу BatchMode=yes | Гонка на старте: stopped agent + encrypted key может дать Permission denied и permanent pause раньше подготовки. Подтверждено порядком вызовов; live ключ владельца не проверялся | Упорядочить tracked startup; bounded waiter без обязательного Running; проверять Stop/Close/settings перед late start. Native harness на задержку, denied, cancel, отсутствие позднего SSH |
| A02 / FAIL → локальный PASS | `server/profile_share.py`, Shares.create:128–132, capacity до revoke(user) | При полном лимите нельзя перевыпустить собственную QR выдачу; локальные RED HTTP-тесты на исходном коде получили400 вместо200 | Projected count без заменяемых собственных links/sessions; отказ ДО revoke. Четыре новые HTTP проверки; 13 профильных тестов PASS, чужие данные/прежняя выдача при отказе сохранены |
| A03 / FAIL → исправляется | `scripts/Update-ProGo.Core.ps1`, Restore-BackupToMain:471–485, optional copy + warning; catch:682 пишет Rollback completed | Неполный откат может выглядеть успешным. Подтверждено source; исходный отказ копирования в этой Linux-среде не воспроизводился | Проверенный program snapshot, обязательное копирование, hashes/version/self-check; rollback-failed отдельный результат. Windows tests: partial copy/corruption/self-check failure и personal-state preservation |
| A04 / FAIL | `scripts/Repair-ProGo.ps1`, Copy-IfExists:18–22 и success:49–54; состав:33–47 | Missing updater transport/session/home-vpn пропускается; оставшегося exe достаточно для OK. Статическое доказательство; native reproduction NOT_RUN | Общий проверенный payload/runtime manifest и транзакционный repair; missing/tamper/offline/write failure/self-check тесты. F22-R03 |
| A05 / FAIL | `src/UpdateAwareTrayApplicationContext.cs`, CleanupBackups:1310–1324: Plan/ApplyCleanupPlan синхронно на UI | Медленное чтение manifest/каталога/удаление блокирует сообщения окна. Статически подтверждён blocking call; latency замер NOT_RUN | Owned preview/apply workers, cancellation, fresh plan validation, heartbeat и changed-manifest/Close тесты. F31-R06 |
| A06 / FAIL | `UpdateAwareTrayApplicationContext.BeginMaintenance:1249–1252`; `UpdateLauncher.StartUpdater:224`; `MaintenanceOperation.StartHandoff:154–170` | В UI dispatch вызывается launch, который ждёт ACK до15s. Статическое доказательство; новый timing fixture NOT_RUN | Tracked handoff worker с сохранением ownership/ACK, delayed/no/late ACK и Close тесты. F31-R07 |
| A07 / PARTIAL | `SshProfileKeyMatching.Check/Read`: metadata-only; `ssh-keygen -l` может использовать .pub; diagnostics явно не обещает signing/auth | Наличие отпечатка не доказывает доступность конкретного private key/подписи. Это известный незавершённый критерий, не повод повторно делать Add Key/Start Agent | Проверяемая готовность выбранного способа auth, binding/freshness и безопасная подпись; не запретить direct unencrypted/other auth. F31-R11 остаётся открытым |
| A08 / FAIL | `ProxyService.ClassifySshErrorLine`: общая Permission denied; Load key bad permissions/invalid format отдельно не отражаются | Пользователь получает общий совет вместо конкретной причины ключевого файла. Само предупреждение не всегда fatal: другой ключ может сработать | Сохранять точную безопасную причину до финального refusal, не превращать warning в запрет других identities. Native stderr и warning/banner spoof tests; ACL не менять |
| A09 / TODO | `Install-ProGo.ps1:11–12` вызывает Build-ProGo; updater загружает release ZIP, standalone installer отсутствует | Согласованный готовый setup/fallback с сохранёнными путями ещё не реализован | F23-R01/R02/R03/R06: общий manifest/maintenance/backup/self-check/rollback, hash failure/offline/cancel/fixed paths; не объявлять текущий CI ZIP итоговым setup |
| A10 / PARTIAL | `SECURITY_MODEL.md` описывал старый sample dataset для любого неверного PIN и два future credentials | Конкурирующая документация может вернуть устаревшее поведение | Заменить текущим утверждённым F09, старую семантику явно обозначить исторической. Полная PIN-модель уже реализовывалась, заново не переписывается |

## PIN и обновление: восстановленные решения

PIN «Б/комфорт»: один постоянный настоящий четырёхзначный PIN; первый
допустимый неверный PIN локального дня становится единственным ложным.
Повтор открывает сохранённые ложные данные, остальные неверные дают точно
«Неверный пин-код». Настоящий не сбрасывает ложный. Состояние переживает restart;
смена дня блокирует сессию/удаляет ложные данные. Реальные/ложные записи отдельные,
атомарные; I/O/KDF в фоне. При допустимом envelope неверный MAC нельзя надёжно
отличить от неверного PIN. Источник: APPROVED_PLAN_31.md, F09.

Fallback: одна кнопка автоматически получает проверенный пакет либо готовый
setup, запускает setup в новом окне с зафиксированными существующими program/data
paths. Сохраняются личные данные, профили, ключи, backup, startup/shortcuts.
Оба пути используют общий maintenance/backup/self-check и проверенный rollback.
Источник: APPROVED_PLAN_31.md, F23. Это требование, а не принятая реализация.

## Точка продолжения и блокеры

Блоки этого продолжения: A02 QR; A01 startup sequencing; A03 честный rollback.
После каждого — отдельный diff/review/commit и обновление доказательств.
В этой точке новые Windows tests NOT_RUN до нового CI; старый CI582 не засчитывается.

Независимые следующие работы: A07 selected-key readiness, A04/A05/A06,
весь остаток реестра. Отсутствие сертификата/SMTP/payment accounts блокирует
только соответствующие live/provider-specific критерии, не общую разработку.
Боевые платежи/письма/изменение VPS и установка владельцу требуют отдельного шага.
Полноту двух чатов можно подтвердить только после получения их экспорта.

## Блок 1: реестр и QR

Commit `bd9c0fb17b4ca4e84bdcec2cf5e1c6d90820f69d`: единый реестр86 ID,
исправление QR capacity, устранение устаревшего PIN описания. Повторная команда
`python -m unittest discover -s tests -p 'test_profile_share*.py' -q`:
13/13 PASS после commit (серверные файлы идентичны commit). Два новых сценария
раньше воспроизвели исходный400 вместо200. Root независимо просмотрел diff;
изолированные HTTP/server tests не являются проверкой live VPS/mail.

## Блок 2: порядок startup

Удалён ранний вызов SSH из Program. Tracked automatic task ожидает bounded
service waiter; медленный best-effort log его не задерживает. Stop/Close,
shutdown/settings/явная команда инвалидируют late start; ручная команда ждёт
тот же waiter. Failed/Disabled/Timeout агента не запрещает direct/other auth.
Добавлены22 native assertions поверх прежних40, с локальным SOCKS child.
Независимый source review вторым исполнителем: блокеров не найдено; UI/ownership/
no-late-start/other-auth инварианты просмотрены. Локальные diff/public scan PASS;
native Windows execution до нового CI NOT_RUN. Selected-key readiness остаётся
F31-R11 PARTIAL. Этот блок не закрывает полный критерий SSH-agent.

## Блок 3: проверенный результат rollback

Restore-BackupToMain подготавливает проверенную detached backup до изменений,
восстанавливает только program roots и сравнивает точные состав/hashes/версию.
Затем запускает self-check и повторно сверяет program evidence. При отказе
копирования, проверки либо cleanup остаётся rollback_failed; backup сохранена,
успех/повторный запуск не обещаются. Текущие personal roots не восстанавливаются
из старой копии. Независимое source review нашло и затем подтвердило устранение
unbounded WaitForExit: теперь30s ожидания+2s cleanup; неподтверждённый owned child
сохраняет handle/PID/start identity для диагностики. Prepared Windows tests
включают copy fail/corruption/self-check fail/настоящий sleeping child timeout,
missing/tampered backup. Native execution NOT_RUN. F23-R04 остаётся PARTIAL.

Локальная общая регрессия: `python -m unittest discover -s tests -p 'test_*.py' -v`
сообщила44 tests:41 PASS,3 SKIP из-за отсутствующего compiled C# relay harness.
Ни SKIP, ни source review не считаются Windows PASS.

## Дополнительные source findings и гипотеза

- A11 / FAIL: `SettingsService.Load`, `src/Core.cs:342–365`, возвращает defaults
  после ошибки read/JSON. Следующий `SaveCore:426–430` может заменить исходный
  settings без загруженных profiles/unknown fields. Статическое доказательство;
  locked-file/native reproduction NOT_RUN. Нужен degraded-load state с сохранением
  исходного файла и понятным recovery; тест locked→unlock→unrelated Save.
- A12 / FAIL: `CodexProxyService.Enable/Disable`, `src/ProxyAutomation.cs:133–149,
  187–191`, проверяет marker cmd, но пишет/удаляет одноимённый lnk без fresh
  target/arguments ownership guard. Воспроизведение: заменить только lnk чужим
  после создания launcher; native execution NOT_RUN. Исправить fresh COM read
  перед изменением, тест foreign/repointed shortcut. F31-R05.
- H01 / PARTIAL, гипотеза: `ProxyService.StartSingleTunnel/StopProcessOnly` владеет
  root ssh Process, не job tree. ProxyCommand descendants могут пережить Stop.
  Наличие orphan НЕ воспроизведено; нужны native owned-descendant fixtures прежде,
  чем объявлять подтверждённый дефект процессов.

## Блокер remote CI и передачи пакета

GitHub get_repo по full name и URL дважды вернул `visibility=public` для ProGo.
Это расходится с указанным владельцем приватным назначением. Новый push сделал
бы код/отчёт публичным, поэтому до privacy decision он не выполняется.
PR88 не изменён, новый CI не запущен, видимость/permissions не менялись.
Локально нет Windows/.NET/PowerShell: сборка и native validation нового кода
NOT_RUN. Поэтому новый бинарный пакет/его SHA256 пока отсутствуют.
Нужен один шаг владельца: разрешить push этих проверяемых изменений в публичный
ProGo либо сделать назначение приватным. Это блокирует публикацию и CI, а не
подменяет оставшийся объём разработки завершением проекта.

## Точный локальный результат перед передачей

- Реестр/QR: `bd9c0fb17b4ca4e84bdcec2cf5e1c6d90820f69d`.
- Startup ordering: `e2fccacb8c7a859217cc854cb91bc7ee7ac57bd8`.
- Rollback: `5c803be5534601233e0041202890cbd7c04cd575`.
- Полная Python discovery повторена на code/test source5c803be:44 reported,
  41 PASS/3 SKIP; public-content299/0 PASS; clean code diff.
- Windows новых commits NOT_RUN; push/PR update/CI/package BLOCKED privacy decision.
- Архив исходных коммитов для ревью не является бинарным проверочным пакетом.
- Приёмка и возврат описаны в [ACCEPTANCE_RUNBOOK.md](ACCEPTANCE_RUNBOOK.md);
  инструкция не выполняет установку и не разрешает live/system actions.

## Продолжение: разрешение публичного push и CI

21.10.2026 владелец явно разрешил отправку четырёх локальных коммитов,
обновление PR и CI в публичном ProGo. Прежний privacy blocker выше — история.
До публикации повторно проверены19 файлов/2896 addedlines и целиком четыре
новых файла: targeted scan и независимое read-only review не нашли секретов/PII.
Public-content300/0 и diff check PASS. Реальные идентификаторы из оригинальной
SSH спецификации не добавлены. Значения потенциальных секретов не печатались.

Отдельным служебным commit workflow получает manual dispatch и opt-in upload:
в public repo по умолчанию файлы не загружаются, build/test/scan/pack сохранены.
Это нужно для соблюдения запрета публичного установщика и согласования передачи.
Новый CI запускается на следующем точном SHA без переписывания четырёх коммитов;
последний из них содержит skip-ci. Релиз/merge/установка не выполняются.

## Опубликованные SHA и начало CI583

Обычный gitpush не имел локальной авторизации. Подключённый GitHub API создал
blobs/trees/commits, после чего expected-head fast-forward безforce передвинул
существующую integration ветку. Каждый tree сравнен с локальным и совпал:

| Блок | Исходный локальный SHA | Опубликованный SHA | Git tree |
| --- | --- | --- | --- |
| Registry/QR | bd9c0fb17b4ca4e84bdcec2cf5e1c6d90820f69d | 6009675e0b4f72f100e19bb7adfb966d79e95fda | a30546cf54fc4416c469b15efdb9c52496453136 |
| Startup | e2fccacb8c7a859217cc854cb91bc7ee7ac57bd8 | b4a95d2cc67af929fabe94248ae9a854c5fe2b32 | 2d2fc8f3507a70dc6dd41c4f2c1d90dfc0a71725 |
| Rollback | 5c803be5534601233e0041202890cbd7c04cd575 | 8fa8b6dd5da1aedebdc17c11e33d0be9aaf1eea2 | e0e774371eaced3517f9fcd0120fc663c025f01f |
| Evidence | 641f3a3bd929b1c342d313ce194e11af65a5df03 | f06e5e547a6adb59dd3ac9118a5a66b2e8eaa1be | 8c578f3efdbc2aeae208578c5cfd764fa3c458a2 |
| PublicCI gate | 83b0aa55558d7c04617a75e1007c24b4991d476c | e9fcfc3279269075eddd6def306c1ca2500cd28c | 8bc4a80e56c7a910412b871a3ece569ae33a8e96 |

PR88 обновлён; новый run39660551553/CI583 запущен наe9fcfc3.
На момент записи Ubuntu completedSUCCESS, Windows in_progress. ОбщегоPASS нет.
Полные исходные локальные commits сохранены в прежней work ветке; active branch
work/progo-published-20261021 создана изAPIhead с идентичной base tree. Чужие
изменения не перезаписывались. Установщик/release не публиковались.

## Продолжение F31-R11: signing backend и UI

Добавлены два связанных микрошагa: локальная sign+verify одной выбранной
public reference и явное действие «Проверить ключ» после fresh profile check.
Shared lease/owned job, frozen endpoint/sanitized Unicode env, bounded snapshots,
post-read scope/cancel guard и UI retained task исключают заявленный ложный успех
в проверяемых контрактных сценариях. Это не auth gate и не доказательство VPS.
Независимые backend/UI source reviews завершены. Исправлены конкретный
post-read freshness gap, emptyendpoint ambiguity, Unicode console encoding;
подготовлены регрессионные векторы. Root включил оба native suites ровноодинраз
и расширил существующий строгий Tab route новой кнопкой. Public scan304/0,
Python41PASS/3SKIP и diff checks не подменяют Windows/visual приёмку нового кода.
CI583 наe9fcfc3 ещё не содержит signing изменений.
