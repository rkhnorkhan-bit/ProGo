# F09 micro21: worker для истечения дневного binding

Source checkpoint 2026-10-13. `VaultDailyExpiry` выполняет Recovery, отдельное
свежее чтение, проверку локальных часов и ограниченную CAS-очистку. Файловые
операции выполняются только worker; модуль не знает UI, AppPaths, logging или
генератор данных. Результат содержит Status, immutable Stamp, HasFakeBinding
и immutable observation metadata, без Record/Data: retained raw task не
удерживает dataset.

## Контракт

- Missing остаётся Missing: startup cleanup не создаёт новый target. Текущее
  состояние дня, включая Empty и binding с нулём записей, не перезаписывается.
- Устаревший localDay очищается через защищённый Empty tombstone другого дня,
  новый generation и revision zero. Это сбрасывает PIN и dataset, сохраняя
  прежний atomic writer и recovery protocol. Правильный PIN и реальный vault
  не являются зависимостями worker.
- Recovery snapshot не используется как CAS authority: после Recovery нужен
  отдельный Read. Каждый CAS использует свежий полный immutable Stamp.
  Conflict перечитывается; суммарно не более трёх CAS, включая ABA-переходы.
  Busy/ошибки возвращаются без fallback, удаления неизвестных файлов или
  создания нового binding.
- `VaultDailyExpiryInvalidation` фиксирует исходные Generation/LocalDay и
  первый наблюдённый отличный день. Revision не входит в token: late CRUD
  внутри старого generation не отменяет истечение.
- `ObservedInvalidations` возвращает клонированный массив immutable metadata
  на success и failure. Runtime сохраняет first-B observation до завершения
  очистки; Busy/Conflict/Cancelled не дают вернуть часы к A и забыть B.
- Наблюдённый A→B→A не восстанавливает старый binding. Для matching original
  generation при now=A сначала применяется Empty(B), затем после нового
  Read выполняется нормализация Empty(A). Same-day сброс binding запрещён
  прежним Store, его правила не обходятся.
- Чужой новый generation текущего дня сохраняется, включая bound-zero.
  Старый token сам по себе не разрешает удалять конкурента. Новое собственное
  calendar observation фиксируется только после свежего Read соответствующего
  generation и фактического отличия localDay от часов.
- Перед CAS и перед safe result перепроверяются clock и cancellation.
  Перемещение дня перед native publication отменяет uncommitted stage и
  запускает ограниченный retry; постоянное движение часов возвращает
  DateChanged. Local/Unspecified допустимы, UTC отвергается.
- Отмена после начавшегося native commit может вернуть Cancelled после
  состоявшегося durable expiration. Rollback не обещается. Ошибки сохраняют
  recovery evidence; успешное Recovery удаляет только доказанные собственные
  artifacts. Возврат safe result требует нового явного открытия для данных.
- Ошибки статические и обычные: без private paths, PIN, parser exception или
  подсказок о режиме. Module не выводит данные и не вызывает logs.

## Нативные fixtures

`VaultDailyExpiryTests.cs` использует синтетические owned Temp roots, настоящий
CurrentUser DPAPI, writer/recovery и native file sharing/mutex. Entry point
ограничен `GITHUB_ACTIONS == true`. Нет mock-CAS или обращения к устройствам
владельца; AppPaths/SafeLog guards должны остаться нулевыми.

Покрытие: Missing/Empty/current binding/bound-zero без rewrite; forward и
backward cleanup; late old-stamp CRUD; A→B→A после advanced revision; первый
fresh-read clock B→A; stale token и новый competitor; fresh clock expiration
нового generation; actual CAS conflict и лимит retries; staged publication
с движением clock/отменой; отказ actual target sharing lock и последующий
recovery; interruption после JournalPublished; pre/post-commit cancellation;
first-B metadata после Conflict/Cancelled/actual Busy и retry при clock A; getter isolation;
unknown recovery proofs и unsupported record; UTC rejection; actual held
global writer gate. Held waits ограничены 15 секундами и освобождаются в finally.

## Статус и границы

Source implementation / independent review: **APPROVE / FROZEN**.
Native compile / Windows standalone / integration CI: **PENDING**.

UI/runtime coordinator, active-session invalidation, restart-process workflow
и статус полного F09 фиксируются отдельными микрозадачами этого пакета.
Этот файл не подтверждает owner acceptance, installer всех 31 задач или release.
