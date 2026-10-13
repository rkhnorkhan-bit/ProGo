# F09 micro21–23: активный сеанс при смене локальной даты

Source checkpoint 2026-10-13. Пакет добавляет owner STA boundary для отзыва
принятого вспомогательного сеанса в уже открытом `VaultForm`. Startup cleanup,
backend fencing и application timer проверяются отдельными пакетами/root
fixtures. Исторический счёт **22/31** не меняется до полного F09 acceptance.

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
Native compile / WinForms / full CI: **PENDING**. Full F09 PASS, installer 31/31,
owner acceptance, merge и release этим документом не заявлены.
