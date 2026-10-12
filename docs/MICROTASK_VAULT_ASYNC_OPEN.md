# F09 micro18: асинхронное открытие хранилища

Source checkpoint 2026-10-12. Этот пакет подключает worker boundary к отдельному
production-окну `VaultUnlockForm`; прежний input-only `PinForm(bool)` сохраняется
для существующих keyboard/accessibility fixtures. F09 остаётся открытым;
исторические 22/31 не увеличиваются на основании одной микрозадачи.

## Контракт окна

- Начальный probe выполняется через `Task.Run` до разрешения ввода. Ready и
  CreateRequired различаются явно; ошибка доступа не включает создание.
- Маскированные PIN и подтверждение используют прежний контракт четырёх
  `Char.IsDigit`. Проверка синтаксиса и подтверждения не запускает KDF/IO.
- Подтверждённый immutable PIN передаётся worker и убирается из текстовых полей.
  Worker вызывает Create/Open вне UI. Окончательный результат сервиса является
  единственным источником `AcceptedSession`; pending fake candidate в окно
  хранилища не передаётся.
- Raw `Work` включает probe/open и отделён от `PresentationWork`. `workStarted`
  вызывается на owner STA; контекст приложения использует raw task для shutdown
  barrier. Кнопки не присваивают преждевременный `DialogResult.OK/Cancel`.
- Одна операция одновременно; ввод и повторный OK заблокированы. Cancel,
  Escape и X запрашивают отмену и ждут actual worker settlement в живом modal
  окне. Отмена не обещает остановку уже начавшегося native filesystem call.
- `OwnerUiDispatcher` возвращает presentation на STA даже при отсутствующем
  ambient SynchronizationContext. После закрытия/Dispose controls не изменяются;
  UI-owner/generation/cancellation перепроверяются после внешних callbacks и
  часов до публикации.
- Правильный постоянный PIN не зависит от часов. Daily session проверяет lease
  непосредственно перед UI adoption; переход даты отклоняет session и требует
  нового явного ввода.
- Любой success result, который не принят окном, вызывает `DropSensitive`.
  Сбрасываются управляемые ссылки; стирание immutable строк/managed heap не
  заявлено. Dataset вызывающей стороны не изменяется этим сбросом.
- Wrong PIN и другие ошибки остаются внутри того же окна; private exception
  message/path/PIN/dataset не попадают в status или log. После отказа Create
  следующий явный клик повторяет probe: файл мог создать конкурирующий процесс.

## Проверки

`DesktopVaultUnlockTests.cs` содержит нативные WinForms fixtures с injected
workers и синтетическими данными. Они не вызывают production default paths и
выполняются только в изолированном Windows CI. Имеющийся desktop harness
сохраняет свой watchdog; ожидания held worker ограничены 15 секундами.

Значимые наблюдения: heartbeat не менее трёх ticks при заблокированном worker;
одна работа при повторном OK; raw tracking на STA при потерянном SyncContext;
null AcceptedSession до commit barrier; маскированный очищенный ввод; explicit
probe retry/creation confirmation; wrong-PIN retry; безопасная ошибка worker;
start/publication gates; throwing-clock real success; midnight fake rejection;
реентрантный callback Dispose/Cancel; гонка Create → probe Ready → новый Open;
button/Escape/X/Dispose и отмена probe без late publication. Ошибки создания,
занятости и отмены не превращаются в success.

Application context отдельно отменяет и ограниченно ждёт raw VaultWork до
обслуживания/выхода; его held-worker shutdown/disposal tests находятся в
`DesktopVaultContextTests.cs`. Истечение этого deadline отказывает в выходе и
сохраняет рабочий message pump. Forced Dispose не подменяет raw settlement
ранним completed task.

## Статус и границы

Source implementation и независимый read-only review: **APPROVE / FROZEN**.
Исправлены зависимости real adoption от clock, реентрантные owner callbacks,
гонка Create с повторным probe и native CancelButton DialogResult.
Native compile/WinForms/полный Windows/server CI: **PASS** по completed
[CI #534](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38309091536),
functional `26b6731087b2de20d93d0de212eeb6740d29f4e9`,
merge `24daf6981e4091362e00d5b4cf53d0ead048cd1b`.
Attempt2 Windows `115013837172`: UI **51**, context **8** assertions,
21:27:26.7101272–21:27:31.3753374 UTC; без vault skips. Desktop3544,
весь Test-ProGo, HomeVPN975, portable90 и общий run — PASS.
Первый job `115010171117` также выполнил все59 новых assertions, но позднее
завершился FAIL из-за неизменённого native SSH smoke: 7001ms при budget7000ms.
На том же коде повтор прошёл за53ms; ни лимит, ни тест не ослаблены.
Полная история и границы находятся в `IMPLEMENTATION_STATUS.md`.

Это ещё не полный F09: фоновый CRUD суточного набора, общий startup/day expiry и очистка
уже открытой fake session в полночь остаются следующими пакетами. UI adoption
check не заменяет активный expiry timer. ПК/VPS/телефон владельца и его
установленное приложение не проверялись; merge, release и полный installer
31/31 этим пакетом не выполняются.
