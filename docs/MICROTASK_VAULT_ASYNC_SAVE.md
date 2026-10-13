# F09 micro19–20: сохранение записей без блокировки UI

Source checkpoint 2026-10-13. Пакет переводит production Add/Edit/Delete
`VaultForm` на worker boundary для постоянного и суточного хранилища.
Исторический счёт **22/31** сохраняется; общий F09 ещё открыт.

## Контракт сохранения

- Перед запуском worker UI создаёт отдельный список и клонирует каждую запись.
  Candidate через `session.CopyWithData` сохраняет исходный путь/lease; KDF,
  сериализация и файловая запись выполняются через `Task.Run`.
- Одновременно допускается только одна запись. Пока raw `Work` не завершён,
  исходные session/grid остаются прежними до запроса закрытия, Add/Edit/Delete/Copy, поиск и фильтр
  заблокированы. Доступный status сообщает о записи; кнопки закрытия остаются
  доступны для запроса отмены.
- Add/Edit используют редактор с явным владельцем и повторно проверяют live,
  current и application gate после modal. Закрытие/Dispose владельца отменяет
  открытый редактор. Delete также перепроверяет владельца после подтверждения.
- `Work` является actual task файловой операции и регистрируется у application
  context до callbacks. `PresentationWork` отдельно возвращается на owner STA
  через `OwnerUiDispatcher`, включая потерянный ambient SynchronizationContext.
- Только успешно завершённая запись публикует новый список. Ошибка оставляет
  session/grid прежними и показывает статическое предупреждение без private
  exception, PIN или содержимого. При необходимости предлагается повторное
  открытие хранилища; суточный режим в сообщениях не раскрывается.
- Закрыть/Заблокировать/Escape/X во время работы запрашивают cancellation и
  ждут actual settlement, сохраняя message pump. Фильтры и содержимое сразу
  скрываются, grid очищается от названий/логинов; остаётся состояние ожидания.
  Перед Save проверяется token;
  уже начавшийся native вызов отмена не откатывает. Hook после durable Save
  не может превратить завершённый commit в rollback.
- После успешной записи потеря owner/current/application gate или поздняя
  отмена закрывает сеанс. Старый grid не продолжает работать с advanced lease
  и не может затереть уже записанные изменения следующим действием.
- Worker сбрасывает ссылки candidate после actual completion. `SaveResult.Data`
  очищается после adoption/discard, включая forced Dispose, чтобы сохранённый
  raw task не удерживал dataset. `DropSensitive` не меняет чужой общий список;
  стирание immutable строк и managed heap не заявлено.
- Legacy session без persistence и с `CanPersist=false` сохраняет прежнее
  синхронное поведение в памяти. Правильный постоянный PIN/сеанс не зависит
  от пользовательских часов. Полный активный/startup expiry — следующий пакет.

## Проверки и статус

`DesktopVaultAsyncSaveTests.cs` содержит isolated native fixtures с собственными
синтетическими данными и held-worker deadline 15 секунд. Suite:
`VaultAsyncSave(SettingsService settings)`.

Проверяются heartbeat во время задержанной записи, snapshot isolation,
single-flight, неизменность исходного grid, запреты всех actions, accessible
status, owner STA при null SynchronizationContext, error/retry без phantom
Saved, все пять close routes до/после commit, поздний Dispose, reentrant gates,
midnight adoption rejection, ошибка after-commit hook, reentrant enable с новой
записью, memory compatibility и закрытие вложенного редактора. Actual real/daily CRUD и shutdown barrier
проверяются отдельными `DesktopVaultDurableSaveTests.cs` root fixtures.

Source implementation и независимый read-only review: **APPROVE / FROZEN**.
Native compile / Windows CI / общий CI: **PENDING**. До completed native
результата PASS не заявлен. Устройства владельца, merge, release и полный
installer 31/31 в этом пакете не проверяются и не выполняются.
