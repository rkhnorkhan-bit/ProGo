# ProGo: единый реестр фактического состояния

Дата: 21.10.2026 (Москва). Аудируемая база `7861ea919eed24c5fe9964fc35a23d3958ba5a9d`. Машиночитаемые поля и точные критерии: [project_state.json](project_state.json). Это текущий реестр; прежние checkpoint-файлы сохраняются как история. Утверждённые требования остаются в [APPROVED_PLAN_31.md](APPROVED_PLAN_31.md) и [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md).

23/31 и 6/55 — разные исторические области приёмки разработки. Суммирование и процент общей готовности не применяются. В 55 входят 43 результата до установщика и 12 зависимых приёмок. Шесть исторических Code/Dev PASS: F17-R02, F17-R03, F31-R01, F31-R02, F31-R03, F31-R04. Их полная перепроверка нового кандидата пока NOT_RUN. Верхнеуровневый PASS требует всех применимых слоёв.

Слои каждого ID: реализация; реальный пользовательский маршрут; проверка/commit; интеграция; пакет; целевой ПК. Код находится в PR88, не в main. Общий финальный installer отсутствует, установка и приёмка на ПК владельца NOT_RUN. PARTIAL в таблице не отменяет прежний узкий PASS. FAIL обозначает подтверждённое нарушение критерия; BLOCKED — конкретную внешнюю зависимость.

CI582 независимо проверен через jobs и Windows log: Windows/Ubuntu SUCCESS, source `b02b0e9606fb57518ed18d17de42e5f82fca8fa8`, merge `cf2e19691057c143b4f66d93c0041e2e1c5681a8`. На базе `7861ea9` только четыре Markdown-файла поверх tested source; runtime/tests/scripts/server/workflows/VERSION идентичны. Новые изменения требуют нового CI.

## Разрешение push/CI и передача кандидата

21.10.2026 владелец явно разрешил push четырёх коммитов в публичный ProGo,
обновление PR и новый CI. Прежний privacy blocker снят. До push19 файлов/
2896 новых строк проверены targeted scan и независимым review: секретов и
личных данных не найдено; public-content300/0 и diff checks PASS.

Публичный релиз и публикация установщика не разрешены. В workflow сохранены
build/test/content-check/pack; автоматическая загрузка любых CI файлов в
public repo выключена. Загрузка требует отдельного явного workflow_dispatch
opt-in, который в этом прогоне не используется. Способ передачи проверочного
пакета подлежит отдельному согласованию. CI583 завершён: Ubuntu PASS, Windows FAIL из-за кодировки PowerShell теста;
новый signing/settings блок ещё NOT_RUN. Подробности и точные SHA ниже.

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
| F31-R07 → F31 | Фоновое ожидание updater handoff | FAIL | Открытый дефект | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
| F31-R08 → F31 | Фоновая работа logs/diagnostic export | PARTIAL | Частично | нет внешней блокировки | Выполнить критерий; зафиксировать тесты и точный SHA. |
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

## Зафиксированная точка продолжения

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

Текущий следующий шаг — исправление подтверждённого FAIL CI583, публикация
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

## Текущий опубликованный кандидат CI584

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
