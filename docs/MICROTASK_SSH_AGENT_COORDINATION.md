# PGA-06B — общая координация службы и операций с ключами

**18.10.2026 UTC. Реализация подготовлена; Code/Dev PENDING до итогового CI.**
Родитель `c7f335e58c13a9bc9401f7e7cb4411add130b28a`, предыдущий принятый
runtime source `9db506714bb26320246e4519aaf2927934dde14f` / completed CI569.
Два микрошагa внутри F31-R11: общий владелец операции и проверки его lifetime.
Счёт **6/55**, остаток49=37+12, верхний уровень23/31. Новых ID/PASS нет.

## Изменение

Раньше SCM read/start/startup и inventory/fingerprint/add имели независимые
блокировки и могли выполняться одновременно. Теперь SshAgentServiceProbe
резервирует тот же process-wide SshReadOnlyLease, который уже используют
операции с ключами. Историческое имя класса сохранено. Резервирование происходит
до scheduling dedicated worker, без ожидания свободного слота. Конкурирующий
запрос получает typed Busy без native/child work, скрытой очереди и повторов.
Разные экземпляры probe и normal-owner startup используют тот же lease.

Lease передаётся worker и освобождается в его finally после возврата native
callback, очистки принадлежащих ему ресурсов и CTS. Если создать/запланировать
worker не удалось, резервирование освобождает вызывающий код. Таймаут или отмена
ожидающего caller не освобождают lease незавершённого native запроса. Существующие
SCM timeout/cancellation semantics, bounded owned process cleanup и порядок
StartService не меняются. Busy тексты больше не называют конкурента исключительно
проверкой ключей/read-only: слот может быть занят SCM или явным добавлением.

Нет нового native API, shell, UAC, изменения политики службы, остановки агента,
удаления ключей или ACL repair. Добавление по-прежнему только backend будущего
явного действия; оно не вызывается startup. Парольную фразу получает OpenSSH.

## Проверки и ожидаемая матрица

В существующий Desktop partial включён отдельный coordination workflow после
45 проверок интерактивного backend. Новые **43 checks**, ожидаемый Desktop4430
=4387+43; существующие assertions/watchdog budgets и workflows не ослаблены.

- Три реальных production entry points на injected child boundary — inventory,
  fingerprint phase и add — каждый удерживает слот. SCM read/start другого probe
  возвращают Busy без native callback; отмена отклонённого caller не затрагивает
  владельца. One-shot startup также получает Busy и не создаёт скрытый retry.
- Отменённая операция с ключами удерживает слот до возврата runner; поздний успех
  не заменяет отмену. После settlement SCM read/start вновь доступны.
- Native-boundary SCM read/start fixtures удерживают настоящий production SCM
  код до/после caller timeout/cancel. Все три операции с ключами и отдельный probe
  не обходят владельца; после release закрыты fixture handles, позднего StartService
  нет, новый SCM/key запрос может выполняться.
- Исключение, null snapshot и probe без start capability завершаются safe
  Unavailable и не оставляют занятый lease.
- Двенадцать одновременно выпущенных смешанных SCM/key запросов: ровно один
  native callback, один владелец, одиннадцать Busy; после завершения слот свободен.

Используются только временные fake sibling paths и прежние SCM/child boundary
fixtures. Нового системного StartService или работы с реальным агентом нет.
Предыдущие actual native OpenSSH/console fixtures остаются в полной CI матрице.
Локально Windows/.NET runtime отсутствует; source review не объявляется native PASS.
Самостоятельный source review; независимого agent review не было.

## Открытые границы

Это взаимное исключение отдельных SCM/key операций, а не атомарный snapshot
целого диагностического pipeline и не готовность SSH-аутентификации. Кнопка
«Добавить ключ», выбор identity, UI freshness/lifecycle, selected-key possession/
signing/constraints/cert/FIDO/custom endpoint, AutoStartSocks/auth ordering и
классификация ошибок остаются последующими микрошагами. Старый «Первый вход»
не заменяется этим backend. Совпадение public fingerprint и service Running
по-прежнему не означают подтверждённый вход.

PGA-05/06/F31-R11/F31/F17 OPEN, F31-R05 PARTIAL, Package PENDING,
Live NOT_CHECKED. Все55 IDs/10 PGA rows/11 acceptance scenarios сохраняются.
Истории FAIL555/556/562/565/568 и UNKNOWN причины не переопределены.
ПК/VPS/телефон/установленная программа/реальные ключи/ACL/платежи владельца
не проверялись и не менялись. PR88 draft/unmerged, VERSION0.2.2, без merge/release.
