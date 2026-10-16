# Остаток ProGo: 8 открытых пунктов, 55 рабочих подпунктов

Рабочая декомпозиция составлена по утверждённым требованиям и сохранённой реализации на source checkpoint `464f41052db96d7508f336c11137661015831142`. Это результат анализа остатка: она не меняет согласованные функции, не закрывает критерии и не является оценкой количества часов или будущих микрокоммитов.

**Всего 55 подпунктов: 43 до передачи цельного тестового установщика; 12 требуют реальной среды или внешних зависимостей.** Текущий счёт верхнего уровня остаётся **23/31 = исторические 22 + F09 Code/Dev PASS**. Часть 55 относится к приёмке уже реализованных модулей на итоговом пакете, а не к написанию их заново.

**Текущий прогресс остатка: 6/55 выполнены; осталось 49 (37 до установщика +12 зависимых приёмок).** F17-R02/R03 приняты в Code/Dev на source `8327054cbba5db8eba6d6f0415f538a36d2a9eb5`, completed [CI #541](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38472084629), native/visual review ([доказательства](MICROTASK_F17_BUTTONS_PIN.md)). F31-R01 принят на source `9e1f2ac9a27dfab63083b2d287f1d53018e1a476`: completed [CI #544](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38567399942), Windows/Ubuntu SUCCESS, независимый source/tests/native review ([доказательства](MICROTASK_F31_IPV6.md)). F31-R02 принят на source `7521e1b2d6770350a4aece78b595cd53aa00370e`: completed [CI #547](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38587581940), Windows/Ubuntu SUCCESS, SOCKS37/312 и Desktop3851, независимый source/tests/native review ([доказательства](MICROTASK_F31_FALLBACK.md)). F31-R03 принят на source `f33104ca16e8afc9e6f10025b3988c3a3cb82efa`: completed [CI #548](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38608522959), Windows/Ubuntu SUCCESS, SOCKS41/403 и Desktop3851, независимый source/tests/native review ([доказательства](MICROTASK_F31_SSH_ERRORS.md)). Теперь F31-R04 принят на source `1025eb2f47ee41d1b0cb0e356bb0e90caabc43f0`: completed [CI #549](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38616061130), Windows/Ubuntu SUCCESS, Desktop3910=3851+59, неизменные SOCKS41/403, native и свежий SSH report visual review ([доказательства](MICROTASK_F31_SSH_CONFIG.md)). F31-R05 продвинут пакетом A: две микрозадачи приняты на source `9959ce782743b766255336b315aec9a4f2d2a77b`, completed [CI #551](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38624788536), Desktop3939/+29 и Windows/Ubuntu SUCCESS ([доказательства](MICROTASK_F31_STARTUP_BACKGROUND.md)); весь R05 остаётся Частично и нового PASS55 не добавляет. Пакет B принят на source `3d37630b562e42afa0fdb3ba8c73c5a3c34044f4`: startup menu migration в tracked worker после instance.Attach; completed [CI #552](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38874943052), Windows/Ubuntu SUCCESS, Desktop3961/+22 ([доказательства](MICROTASK_F31_MENU_MIGRATION.md)). Весь R05 остаётся Частично. Пакет C принят на source `de58a08847bbc2c3228b70d436fef175994d2776`: Programs/Startup/UserProfile формы в независимых workers после Shown, completed [CI #554](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38881063456), Windows/Ubuntu SUCCESS, Desktop3991/+30 и2 свежих diagnostic PNG ([доказательства](MICROTASK_F31_SETTINGS_FOLDERS.md)). История FAIL #553 и коррекция нового fixture сохранены. Весь R05 остаётся Частично. Исходная декомпозиция43/12/55 в таблице ниже сохранена; целые F17/F31 остаются OPEN, общий счёт23/31 не увеличен.

16.10.2026 в существующие F31/F23 включена [спецификация SSH-agent](SSH_AGENT_AUTOSTART_SPEC.md). PGA-01 audit/mapping и узкий PGA-02A read-only typed snapshot — Code/Dev PASS на source `13b98cceec08cd48ff70155edcb5515a1ccf7a21`, completed [CI #557](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38889610234):37 SCM, Desktop4028, Instance28=26+2 и обе native jobs SUCCESS ([протокол](MICROTASK_SSH_AGENT_SERVICE_STATUS.md)). FAIL555/556 сохранены; причина старого ssh-G отказа556 остаётся UNKNOWN и открытым finding F31-R11. Весь PGA-02/R11 не закрыт. PGA-02B и остальной агентский код с dev-проверками явно обязательны до установщика в F31-R11; реальные ключи и устройства относятся к последующей приёмке. PGA IDs не добавляют новых единиц к55; исходные43+12 и6 PASS сохранены.

PGA-02B: общий bounded SCM probe и lifecycle окна приняты в Code/Dev на source `c045b6f199fc6360a99c29f79954e76ed013cf8d`, completed [CI #558](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38897865756):32 новых checks, Desktop4060, Windows/Ubuntu SUCCESS ([протокол](MICROTASK_SSH_AGENT_BOUNDED_WAIT.md)). Весь PGA-02/R11 ещё открыт до startup интеграции/следующих работ; счёт6/55 и все55 исходных ID сохраняются.

PGA-03A — узкий **Code/Dev PASS** на source `4a7a6a9fec80a17321f6ffc0ac4fa5fdf6fedd6a`, completed [CI #559](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38971701412):66 новых start/rights/race/cancel/ownership checks, Desktop4126, обе native jobs SUCCESS ([протокол](MICROTASK_SSH_AGENT_SERVICE_START.md)). Это internal start API и общий read/start slot; startup hook/lifetime — отдельный PGA-03B. Реальный StartService только с invalid handle, рабочие start-сценарии на native boundary fixtures; не live приёмка. F31-R11/PGA-03/PGA-02 не закрыты, новых PASS55 нет.


Один подпункт здесь — отдельный проверяемый результат с приёмкой. Тестовые случаи, каждая форма, каждый DPI, поля настройки и исторические буквенные stage IDs отдельными единицами не считаются. Крупный подпункт исполняется несколькими небольшими микрозадачами по прежнему ограничению объёма;55 не означает 55 обращений к ассистенту. Дополнительные обнаруженные дефекты фиксируются отдельно, поэтому это версия декомпозиции текущего остатка, а не обещание неизменного объёма будущей работы.

Фаза **До** включает остаток кода, dev-проверки и приёмку общего setup/updater. Фаза **Реальная** включает проверки установленного продукта и/или зависимые от доступных аккаунтов, сертификата, сервисов и реквизитов проверки. Последние могут выполняться раньше при наличии условий; весь функциональный код F30 и его dev/sandbox проверки должны быть готовы до передачи installer. ПК/VPS/телефон владельца проверяются после установки им готовой программы. Недоступная обязательная проверка остаётся NOT_CHECKED, не превращается в PASS.

| Пункт | До установщика | Реальная среда / внешние зависимости | Всего |
| --- | ---: | ---: | ---: |
| F16 — Адаптивность и DPI | 4 | 0 | **4** |
| F17 — Доступность и состояния интерфейса | 5 | 0 | **5** |
| F19 — Условия подключения и диагностика | 3 | 2 | **5** |
| F22 — Копии, перенос и repair | 3 | 1 | **4** |
| F23 — Обновление и готовый установщик | 6 | 1 | **7** |
| F27 — Подпись, доверие пакету и антивирусы | 2 | 2 | **4** |
| F30 — Почта, кабинет и биллинг | 9 | 3 | **12** |
| F31 — SSH, переносимость и скорость | 11 | 3 | **14** |
| **Итого** | **43** | **12** | **55** |

## F16 — Адаптивность и DPI

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F16-R01 | До | Частично | Полнота адаптивной компоновки существующих вспомогательных окон | Проверить оставшиеся и поздние формы на переполнение, длинный текст, минимальный размер и доступность главных действий; исправить обнаруженные дефекты. |
| F16-R02 | До | Предстоит | Адаптивность новых installer и F30 экранов | Будущие формы удовлетворяют тем же требованиям геометрии. Их функциональное создание считается в F23/F30; здесь принимается компоновка. |
| F16-R03 | До | Частично | DPI awareness и реальная смена монитора | Настроить режим для используемого runtime и подтвердить перестроение при смене DPI/монитора без потери управления. |
| F16-R04 | До | Dev NOT_CHECKED | Итоговая native DPI/display приёмка | Один протокол всех итоговых форм: реальные Windows 100/125/150/200%, 1366×768, минимальные размеры, длинные тексты и доступные действия. Synthetic Scale сам по себе критерия не закрывает. |

## F17 — Доступность и состояния интерфейса

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F17-R01 | До | Частично | Полнота клавиатуры и доступных имён/состояний | Проверить итоговые поздние и новые формы: Tab/Shift+Tab/Enter/Esc, focus order, доступные имена, статусы и ошибки; устранить только найденные gaps. Для будущих agent UI отдельно проверить состояния службы/ключа/SSH/SOCKS, Start Agent/Add Key/Check Profile/Retry и объяснения недоступных действий по [спецификации](SSH_AGENT_AUTOSTART_SPEC.md). Новое замечание по 3 PNG CI #551: тусклая подпись disabled startup CheckBox на тёмном фоне; исправление ещё впереди ([фиксация](MICROTASK_F31_STARTUP_BACKGROUND.md)). |
| F17-R02 | До | **Code/Dev PASS** | Читаемые disabled и узнаваемые pending кнопки | Исправлены тёмные подписи выключенных текстовых Save-действий и яркая pending primary-кнопка Unlock; native bitmap/state проверки и свежие снимки приняты в CI #541. [Доказательства](MICROTASK_F17_BUTTONS_PIN.md). |
| F17-R03 | До | **Code/Dev PASS** | PIN hint/status без дублирования | Повтор правила четырёх цифр убран; форматная ошибка доступна, исправление локального ввода возвращает инструкцию без неявной отправки. Ошибки аутентификации/worker/owner gate сохраняются; CI #541 и свежие снимки приняты. [Доказательства](MICROTASK_F17_BUTTONS_PIN.md). |
| F17-R04 | До | Dev NOT_CHECKED | Реальная Narrator приёмка | Проверить конечные маршруты и чтение названий, порядка, состояний и ошибок; недоступную dev среду явно оставить NOT_CHECKED с причиной. |
| F17-R05 | До | Dev NOT_CHECKED | Реальная High Contrast и видимый фокус | Проверить системную тему, её переключение, контраст разных состояний и видимый фокус на итоговых формах. Synthetic palette не заменяет этот результат. |

## F19 — Условия подключения и диагностика

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F19-R01 | До | Предстоит | Доказательный LAN/NAT/CGNAT preflight | Различать обнаруженное ограничение, предположение и отсутствие достаточных сведений; dev fixtures подтверждают корректные verdicts. |
| F19-R02 | До | Предстоит | Подтверждённые входящие UDP-пути | Отдельное доказательство пути 500→15000 и 4500→14500 либо честная ошибка/NOT_CHECKED. Галочка и relay handshake внешнего проброса не доказывают. |
| F19-R03 | До | Частично | Интегрированная поэтапная диагностика | Связать preflight, SSH/SOCKS, VPS, relay и телефонные этапы с конкретной причиной и следующим действием; dev отказ/timeout/cancel/устаревший результат проверены. |
| F19-R04 | Реальная | Live NOT_CHECKED | Настоящий телефон, маршрут и DNS | После установки владельцем подтвердить подключение, рабочий DNS/интернет и ожидаемый внешний маршрут для точного установленного кандидата. |
| F19-R05 | Реальная | Live NOT_CHECKED | Устойчивость настоящего подключения | После установки проверить смену сети, простой и крупные передачи с честным состоянием работы/восстановления; новые численные гарантии не вводятся. |

## F22 — Копии, перенос и repair

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F22-R01 | До | Приёмка готовой реализации | Состав обычных копий на итоговом пакете | Подтвердить заявленные settings/vault/private/access/request/proxy snapshots, manifest/hashes и исключение временных секретов; обычный rollback сохраняет текущие личные данные. |
| F22-R02 | До | Частично; cross-SID NOT_CHECKED | Защищённый VPN export/import на чистом dev Windows под другим SID | Portable90 уже PASS; дополнительно подтвердить другую Windows identity, новую DPAPI-защиту, исходные VPS/trust/request bindings, отказ tamper/path/reparse/cancel и показ границ состава. |
| F22-R03 | До | Открытый дефект | Полный runtime repair | Из проверенного готового payload восстановить обязательные helpers/home-vpn и весь runtime; self-check проходит, личные данные сохранены, отсутствующие обязательные компоненты дают ошибку. |
| F22-R04 | Реальная | Live NOT_CHECKED | Фактический перенос владельца между ПК | После установки подтвердить импорт VPN, сохранение доверия и pending requests на реальном окружении владельца. |

## F23 — Обновление и готовый установщик

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F23-R01 | До | Предстоит | Готовый standalone setup с полным runtime | Пользователь устанавливает готовый проверенный продукт без Build-ProGo или самостоятельной сборки исходников. [SSH-agent](SSH_AGENT_AUTOSTART_SPEC.md): отдельная необязательная настройка Automatic+Start, OFF по умолчанию, только по явному выбору пользователя; отсутствие службы/прав/policy объясняется, uninstall сохраняет общую службу и её настройки. |
| F23-R02 | До | Частично | Installer в общем maintenance protocol | Одно владение app/update/restore/install, проверенная копия/retention, staging, установленный self-check и rollback; параллельная запись исключена. |
| F23-R03 | До | Предстоит | Одна кнопка и автоматический выбор updater/installer | Проверенная автоматическая загрузка пакета либо setup; полный installer запускается в новом окне с заранее зафиксированными существующими program/data paths, сохраняя данные и выборы. |
| F23-R04 | До | Открытый дефект | Подтверждённый честный rollback updater | Отказ копирования/самотеста остаётся ошибкой; успех только после проверки восстановленного runtime. Подавление copy errors не превращает откат в завершённый. |
| F23-R05 | До | Открытый дефект | Неизменяемая идентичность принятого выпуска | Привязать version/commit/tag/hash и устранить release UID gap; старый asset нельзя заменить другим payload той же версии. Новый формат UID заранее не предписывается. |
| F23-R06 | До | Package/Dev PENDING | Единый итоговый setup/updater и dev приёмка | Одна версия/commit, release notes, чистая установка и оба пути обновления, fixed paths, cancel/offline/hash failures, self-check, rollback/handoff. Это общий пакетный результат F23. |
| F23-R07 | Реальная | Live NOT_CHECKED | Установленный update workflow владельца | После установки подтвердить сохранение реальных данных/путей/выборов; отменённое SSH-ожидание сохраняет исходный запрос, проверка результата не повторяет выдачу. |

## F27 — Подпись, доверие пакету и антивирусы

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F27-R01 | До | Предстоит | Signing/verification pipeline поставляемых артефактов | Подготовить exe/scripts/installer/payload, защищённое подключение signer, проверку подписи/timestamp. Тестовая сборка явно unsigned; реальный сертификат после тестов. |
| F27-R02 | До | Частично | Проверка издателя и доверия updater payload | Добавить и проверить механизм подписи/издателя/состава к существующим hash/archive/version checks; dev режим не выдаёт unsigned артефакт за подписанный. |
| F27-R03 | Реальная | Внешняя зависимость | Реальный сертификат и подписанный итоговый выпуск | После тестов подключить сертификат и проверить реальные подписи, timestamp и соответствие payload конечных артефактов. |
| F27-R04 | Реальная | NOT_CHECKED | Антивирусная приёмка конечных артефактов | Отдельные результаты Defender и Kaspersky, исправление обнаруженных проблем и относящаяся к ним повторная проверка. Это один пакет приёмки с двумя результатами. |

## F30 — Почта, кабинет и биллинг

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F30-R01 | До | Частично | Почтовая выдача и согласованная QR выдача | Явный адресат, временная отзываемая HTTPS-ссылка без секретного профиля/ключей во вложении, честные ошибки/retry, защищённые секреты; перевыдача при полном лимите работает. |
| F30-R02 | До | Предстоит | Серверные коммерческие данные и права | Сохранённые тарифы/периоды/лимиты, free/trial/paid, владелец/операторы, серверная авторизация и история действий. |
| F30-R03 | До | Предстоит | VPS-кабинет и вход в него из ProGo | Выдача/продление/приостановка/отзыв, тариф/цена/период, история платежей/действий; pending/success/error/unknown различаются. |
| F30-R04 | До | Предстоит | Коммерческий жизненный цикл доступа | Первый успешный авторизованный connect либо 7×24 ч после оплаты, что раньше; серверное время, сроки free/trial/paid, только ручное продление, без двойного продления от дублей. |
| F30-R05 | До | Частично | Серверное исполнение ограничений и активные сеансы | Новые соединения проверяют срок/приостановку/отзыв; список и принудительное прекращение выбранных активных сеансов сохраняют чужой доступ. |
| F30-R06 | До | Предстоит | Онлайн-заказ и платёжный адаптер | Собственная/агентская услуга, СБП/карты, единая цена с включёнными 5%; сервер фиксирует цену/поставщика/доступ, секреты остаются на сервере. |
| F30-R07 | До | Предстоит | Проверенные платёжные события и активация | Webhook/API, идемпотентность/дубли/отказ/возврат; оплаченный заказ не теряется, активация повторяется контролируемо с видимым состоянием. |
| F30-R08 | До | Предстоит | Технический агентский контур | Поставщик виден клиенту; данные/проверенные статусы ИП/НПД, роли и чековые сведения соответствуют утверждённым собственной/агентской моделям. |
| F30-R09 | До | Предстоит | Финансовый реестр и сверка | Платежи, возвраты, фактические комиссии, корректировки и выплаты; база и доли 25/75 по утверждённому контракту, без повторного вычета условных 5%. |
| F30-R10 | Реальная | Внешняя зависимость | Реальная сервисная почта | Подключённый аккаунт/SMTP/провайдер и подтверждённая доставка временных ссылок, ошибки и повтор. |
| F30-R11 | Реальная | Внешняя зависимость | Реальные собственные и агентские платежи | Пригодные аккаунты/реквизиты/договоры, проверенные поставщики; подтверждённые СБП/карты, чеки, возвраты, комиссии, выплаты и сверка после отдельного разрешения реальных операций. |
| F30-R12 | Реальная | Live NOT_CHECKED | Коммерческий lifecycle на реальном VPS и клиентах | Подтвердить связь оплаты с авторизованным connect, сроком/ручным продлением и прекращением выбранных действующих сеансов. DNS/смена сети/idle/передачи остаются F19. |

## F31 — SSH, переносимость и скорость

| ID | Фаза | Состояние остатка | Проверяемый результат | Критерий завершения |
| --- | --- | --- | --- | --- |
| F31-R01 | До | **Code/Dev PASS** | SSH IPv6 | Корректные native аргументы -D, реальный IPv6-only SOCKS/CLI HTTP/CONNECT и bounded DNS/cancel подтверждены в CI #544. [Доказательства](MICROTASK_F31_IPV6.md). |
| F31-R02 | До | **Code/Dev PASS** | Профильный fallback | Отказ одного профиля не исключает разрешённые остальные кандидаты; выбор требует настоящего SOCKS handshake и owned child. Stop/cancel/settings guards и отдельный consumer reuse готового SOCKS подтверждены CI #547. [Доказательства](MICROTASK_F31_FALLBACK.md). |
| F31-R03 | До | **Code/Dev PASS** | Постоянные ошибки SSH config/arguments | Известные fatal config/arguments и prelaunch validation приостанавливают только отказавший candidate; разрешённый backup остаётся доступен. Transient/warnings и key/host-key refusal разделены; actual native parser/replay/no-PID/retry подтверждены CI #548. [Доказательства](MICROTASK_F31_SSH_ERRORS.md). |
| F31-R04 | До | **Code/Dev PASS** | Include/aliases/IdentityFile tokens в диагностике | Native OpenSSH — источник effective конфигурации; raw IdentityFile отделён от подтверждённого пути, неизвестный контекст явно NOT_CHECKED без догадок. CI #549 Desktop3910/+59 и свежий SSH report visual приняты. [Доказательства](MICROTASK_F31_SSH_CONFIG.md). |
| F31-R05 | До | Частично | Остаточные startup/settings/shortcut операции вне UI | Пакет A: startup snapshot и field-only tray preference в owned workers приняты по CI #551, Desktop3939/+29. [Доказательства](MICROTASK_F31_STARTUP_BACKGROUND.md). Пакет B: startup menu migration перенесена в tracked worker после instance.Attach; CI #552, Desktop3961/+22 PASS ([доказательства](MICROTASK_F31_MENU_MIGRATION.md)). Пакет C: constructor folder discovery перенесён после Shown, CI #554, Desktop3991/+30 PASS ([доказательства](MICROTASK_F31_SETTINGS_FOLDERS.md)). Ранние bootstrap/settings и scoped shortcuts/launch ещё впереди; у Codex .lnk ещё требуется fresh target/arguments ownership guard перед overwrite/delete (read-only source finding). Критерий всего R05 не закрыт. |
| F31-R06 | До | Открытый дефект | Фоновый retention preview/cleanup | Owned worker, отмена, повторная проверка плана перед удалением; UI не выполняет Plan/ApplyCleanupPlan синхронно. |
| F31-R07 | До | Открытый дефект | Фоновое ожидание updater handoff | ACK/ownership сохраняются, незавершённый helper запрещает handoff; ожидание StartHandoff не блокирует UI. Сам installer создаётся в F23. |
| F31-R08 | До | Частично | Фоновая работа logs/diagnostic export | Build/read/write/open и остаточный UI logging не ждут диск на UI; slow/locked fixtures сохраняют отзывчивость и redaction. |
| F31-R09 | До | Открытый дефект | HomeVPN private I/O, DPAPI и session preparation в worker | ShareOrigin/save, token/home-address, ACL/secret files выполняются в worker с зафиксированными входными данными операции; отмена/смена доступа запрещают позднее сохранение. |
| F31-R10 | До | Открытый дефект | Асинхронные phone Stop/cleanup | Неполная очистка видима, ownership/retry сохранены, Dispose/удаление session не блокируют UI. Готовый desktop Stop/F09 shutdown повторно не переделываются. |
| F31-R11 | До | Dev PENDING | Подготовка SSH-agent и итоговая dev приёмка F31 | До установщика реализовать и проверить [PGA-02–09](SSH_AGENT_AUTOSTART_SPEC.md): асинхронные ограниченные операции без дублей, запуск службы в имеющихся правах без повторного UAC, нужный ключ выбранного профиля и безопасный interactive ssh-add, классификацию ошибок/повторы и доступный UI. Агент не обязателен для других рабочих методов аутентификации; пароли не сохраняются. Дополнительно native slow disk/network/process fixtures и отчёт latency/CPU/RAM/I/O на итоговом commit с отдельными PASS/FAIL/NOT_CHECKED. Микрошаг02A не закрывает этот критерий. |
| F31-R12 | Реальная | Live NOT_CHECKED | Настоящий encrypted-key agent и маршрут | После установки подтвердить unattended auth, отказ/восстановление и реальный SOCKS→HTTP/HTTPS маршрут. Проверить реальные encrypted-key сценарии [PGA-10](SSH_AGENT_AUTOSTART_SPEC.md); разработка и dev-проверки этого функционала обязательны раньше в F31-R11. |
| F31-R13 | Реальная | Live NOT_CHECKED | SSH-профили на другом Windows ПК/пользователе и VPS | Переносимые пути, независимые порты/профили и выбор локального ключа; SSH-поведение отдельно от VPN-архива F22. |
| F31-R14 | Реальная | Live NOT_CHECKED | Installed build в Rescue/HDD/VHDX | После установки подтвердить identity exe и startup/UI latency, CPU/RAM/process I/O/физический диск, idle и unavailable VPS/agent. |

## Правила исключения дублей и границы

- Старые adaptive dashboard/settings, клавиатурные маршруты большинства нынешних форм и system palette уже реализованы: F16/F17 считают полноту итоговых новых форм и отсутствующие реальные dev проверки, плюс три свежих замечания UI #539.
- Native expiry130, restart237/12 стадий, active31, runtime70, async save60/durable71 и полный CI #539 не возвращаются в список реализации F09. Фоновый vault, desktop Stop, backup/restore/settings/portable workers не переписываются как новые задачи F31.
- **Repair считается только F22-R03.** Создание installer и общая setup/updater приёмка — только F23. F22 приёмка состава копий/архива и F31 responsiveness — отдельные свойства; общий package gate второй раз не добавляется.
- F22-R02 проверяет другой Windows SID на чистом dev Windows до installer;F22-R04 — фактический перенос владельца между его ПК. Существующий `.progo-vpn` переносит VPN private данные и исходные trust/request bindings; settings/vault, `.ssh` и proxy snapshots не становятся его содержимым автоматически. Обычные копии проверяются отдельно. Новый универсальный формат экспорта этим подсчётом не вводится.
- F31-R07 исправляет ожидание handoff на UI; сам updater/installer workflow остаётся F23. F31-R09 относится к локальному I/O/DPAPI/session preparation HomeVPN; коммерческая серверная модель — F30.
- F19 реальные DNS/маршрут/смена сети/idle/передачи и F30 коммерческие срок/оплата/отзыв активных сеансов имеют разные критерии. Общая приёмка владельцем всех 31 сверх этих пунктов второй раз не посчитана.
- Системные Windows DPI/Narrator/High Contrast в dev относятся к фазе До; synthetic Scale/theme или native control assertions не подменяют фактическую проверку среды.
- F27 включает подготовленный pipeline и явно неподписанный тестовый продукт до установщика. Реальный сертификат и финальные подписи — после тестов; результаты Defender/Kaspersky учитываются отдельно внутри одного пакета приёмки.
- Почта/кабинет/биллинг F30 полностью реализуются и проверяются в dev/sandbox до передачи тестового продукта. Mock/sandbox не подменяют реальную доставку/оплату/чеки/выплаты/серверные сеансы; три зависимые приёмки отмечены отдельно.
- Автоматизация роутера и приём обычных физлиц-поставщиков не включены: они отложены утверждёнными решениями. Регион, конечная цена, комиссии, сроки и ручное продление сохранены по утверждённому контракту; новой юридической модели нет.

## Основания анализа

- [Утверждённые 31 требования](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/APPROVED_PLAN_31.md), [критерии остатка](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/AUDIT_CRITERIA_REMAINING.md), [статус и доказательства #539](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/IMPLEMENTATION_STATUS.md). Исторические [AUDIT_PROGRESS](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/AUDIT_PROGRESS.md) и [DEVELOPMENT](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/DEVELOPMENT.md) не считаются актуальным списком недоделок.
- F16/F17: [Program](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/Program.cs), [UiTheme](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/UiTheme.cs), [build](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/scripts/Build-ProGo.ps1), [synthetic contrast tests](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/tests/DesktopContrastThemeTests.cs), [geometry-only tests](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/tests/DesktopSettingsPagesLayoutTests.cs). AutoScaleMode.Dpi и шесть просмотренных свежих vault PNG подтверждают лишь указанную часть критериев; итоговая DPI/assistive приёмка не доказана.
- F19: [HomeVpnWizardForm](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/HomeVpnWizardForm.cs), [HomeVpnService](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/HomeVpnService.cs), [Home IKEv2](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/HOME_IKEV2.md). Ручная UDP галочка остаётся неподтверждённой; общий HomeVPN975 не закрывает отсутствующий preflight и реальный маршрут телефона.
- F22/F23: [portable scope](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/F22_PORTABLE_HOME_STAGE.md), [обычный install вызывает Build-ProGo](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/scripts/Install-ProGo.ps1), [неполный Repair](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/scripts/Repair-ProGo.ps1), [updater transaction](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/scripts/Update-ProGo.Core.ps1), [release workflow](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/.github/workflows/release.yml). Portable90 PASS не подменяет cross-SID/final package приёмку.
- F27/F30: [полный контракт F30](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/docs/FUTURE_FEATURES.md), [profile_share](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/server/profile_share.py), [home_vpn_setup](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/server/home_vpn_setup.py), [текущие invitations](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/HomeInvitationsForm.cs). Existing QR/HTTPS/invitations есть; полный mail/cabinet/billing не обнаружен в проверенной реализации. Capacity-check до revoke в create оставляет проверяемый остаток при полном лимите.
- F31: [UI/context ownership](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/UpdateAwareTrayApplicationContext.cs), [phone runtime](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/HomeVpnService.cs), [ProxyService](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/ProxyService.cs), [SSH diagnostics](https://github.com/rkhnorkhan-bit/ProGo/blob/464f41052db96d7508f336c11137661015831142/src/SshProfileDiagnostics.cs). На точном source видны синхронные StopPhone/StopAll→homeVpn.Stop, private preparation до первого await и оставшиеся UI handoff/retention/log/shortcut операции.

При первоначальном анализе на checkpoint `464f41052db96d7508f336c11137661015831142` новые сборки и изменения кода не выполнялись. Текущие 6/55 и их новые source/CI отражены отдельно выше. Реальные платежи/письма, изменения сертификатов, установка и проверки устройств владельца не выполнялись; утверждённые требования сохранены.
