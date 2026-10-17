# F09 micro21–23: активный сеанс при смене локальной даты

Source checkpoint 2026-10-13. Пакет добавляет owner STA boundary для отзыва
принятого вспомогательного сеанса в уже открытом `VaultForm`. Startup cleanup,
backend fencing и application timer проверяются отдельными пакетами/root
fixtures. Полный F09 Code/Dev PASS подтверждён #539: текущий прогресс
**23/31 = исторические 22 + F09**. Историческая baseline-матрица сохранена.

## Контракт окна

- `CheckCurrent()` читает injected local clock только у session с persistence
  lease. `CheckExpiry(localNow)` принимает наблюдение existing application
  timer без собственного IO. Оба метода выполняются только на owner STA.
- После явного Cancel/Close owner boundary заморожена: CheckCurrent/CheckExpiry
  и Activated больше не читают clock и не создают expiry notification. Raw worker
  до settlement сохраняет собственную backend проверку срока.
- Первое несовпадение даты вперёд/назад или некорректные часы устанавливают
  необратимый owner latch и вызывают nonblocking `session.Revoke()`. Подготовленная
  копия использует тот же отозванный lease: A → B → A не восстанавливает сеанс.
- Callback `expired(session, localNow)` вызывается один раз, после публикации
  отзыва и до control events/teardown. Он предназначен только для захвата
  immutable cleanup metadata и постановки worker; без IO, await и диалогов.
- Сразу после callback фильтры скрываются, rows удаляются, открытый owned
  редактор отменяется. `CancelCurrent` закрывает idle окно либо сохраняет
  message pump до actual завершения raw save. Отмена не откатывает commit.
- Активизация владельца и вложенного EntryForm проверяет срок до дальнейших
  действий. Add/Edit/Delete/Copy и adoption также используют эту границу.
  Правильный постоянный сеанс не обращается к часам и expiry callback.
- Throwing clock передаёт explicit invalid sentinel `Utc DateTime.MinValue`.
  UTC clock также остаётся invalid наблюдением. Runtime не сохраняет такое
  значение как локальный день и не удаляет current-day binding на основании
  отсутствующего доказательства смены дня; cleanup повторяется с валидными
  часами. UI сеанс при этом закрывается.
- Expiry из RowsAdded немедленно скрывает viewport и останавливает adoption;
  очистка rows и idle Close завершаются после выхода из native grid callback.
- Callback exception, reentrant Close/Dispose и off-owner вызовы не возвращают
  доступ к данным. `DropSensitive` при обычном закрытии не является общим
  отзывом lease; explicit Revoke применяется только при истечении этого сеанса.

## Нативные fixtures и границы

`DesktopVaultExpiryTests.cs`, entry point `VaultActiveExpiry(settings)`, работает
только в isolated Windows CI. Синтетические leases проверяют idle forward/back,
A → B → A shared wrapper, owner/editor activation, invalid/throwing fake clock,
proper real clock independence, held save до/после commit, callback failure и
teardown, off-owner guard, frozen owner после Cancel, expiry внутри RowsAdded и held open candidate до fake adoption. Последний
fixture задерживает injected open worker; это не измерение реального KDF.
Held workers имеют deadline 15 секунд и release в finally.

Production lease, startup cleanup, timer scheduling, observed-day token/fence
и actual on-disk expiration — отдельные source/native fixtures root. Ни эти
synthetic UI fixtures, ни простое сравнение DateTime не подменяют backend
atomic cleanup acceptance или тесты устройства владельца.

Source implementation и независимый scoped read-only review: **APPROVE / FROZEN**.
Native compile / WinForms / full CI: **PASS**, completed #539.

## Свежая native проверка #539

[CI #539](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38453502193),
Windows `115490149982`, source `43616a2082d231862862a9d1ed54d0e127bd38e3`,
tested merge `a007e029c0ce8bbeb0fd61605d78c209d78a57e6`:
**31 PASS assertions**, 2026-10-13 **19:22:26.9684089–19:22:28.6271700 UTC**.
Windows/server/run и full Test-ProGo завершены SUCCESS/PASS; skips новых
vault fixtures нет. Дополнительно runtime70 и standalone expiry130 проверяют
production owner/backend seams этого же source, restart237 — fresh processes.

Из screenshot artifact `11841933219` просмотрены шесть vault PNG:
`desktop-shots/vault-unlock-pending.png`, `vault-unlock-wrong-pin.png`,
`vault-unlock-unavailable.png`, `vault-unlock-create.png`,
`vault-save-pending.png` и `vault.png` в том же каталоге. Поля/PIN mask,
labels и busy/error состояния проверены; это ограниченная проверка vault,
не full DPI/accessibility acceptance. Неблокирующие F17 follow-up по этим
снимкам перечислены в [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md).

Package PENDING, Live NOT_CHECKED. Реальный Windows reboot, crash/power loss,
устройства владельца, installer всех31, merge и release не проверены.

## История #537: отказ первого idle assertion

[CI #537](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38449783093),
Windows `115477409112`, completed FAIL на первом idle expiry assertion
2026-10-13 18:54:46.7920159 UTC. Предыдущие async save60/durable71 выполнены
успешно; active expiry31 и runtime suite целиком не завершились. Вероятная
причина: скрыт parent фильтров, но собственный Visible flag search оставался
true и становился доступным после detachment во время Dispose. Follow-up
явно скрывает search/typeFilter и сохраняет строгие assertions; bounded
Boolean diagnostics позволяют проверить причину без содержимого/PIN/путей.
Для проверки follow-up требовался следующий completed native run; ошибочный
run сохранён. Его исходная причина остаётся вероятной, несмотря на последующий
PASS исправленного source. Full F09 PASS, installer 31/31, owner acceptance,
merge и release этим историческим запуском не подтверждались. #538 прошёл
active31, но завершился поздним cleanup fixture FAIL; история сохранена в
[MICROTASK_VAULT_DAILY_RUNTIME.md](MICROTASK_VAULT_DAILY_RUNTIME.md).
