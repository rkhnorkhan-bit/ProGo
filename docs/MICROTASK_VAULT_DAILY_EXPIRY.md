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
Native compile / Windows standalone / integration CI: **PASS**, completed #539.

[CI #539](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38453502193),
Windows `115490149982`, source `43616a2082d231862862a9d1ed54d0e127bd38e3`,
tree `3dad3147706bdab90de517fcaac0bb84d94db22b`, tested merge
`a007e029c0ce8bbeb0fd61605d78c209d78a57e6`: **130 PASS assertions**,
2026-10-13 **19:17:19.0796755–19:17:23.2058330 UTC**, без skips.
Actual DPAPI, CAS/locks/recovery, forward/backward и retained first-B/ABA
fixtures выполнены на owned temp roots. Windows/server/run завершены SUCCESS;
full Test-ProGo также PASS. Это assertion count, не 130 отдельных scenarios.

История полного FAIL #536/#537/#538 сохранена в
[IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md). Прошедший expiry harness
в этих runs не закрывал остановившиеся restart/desktop suites.

UI/runtime coordinator, active-session invalidation, restart-process workflow
и статус полного F09 фиксируются отдельными микрозадачами этого пакета.
Полный F09 имеет Code/Dev PASS в текущем checkpoint; прогресс 23/31 означает
исторические 22 плюс F09, без новой приёмки остальных пунктов. Package PENDING,
Live NOT_CHECKED. Этот файл не подтверждает actual Windows reboot/crash,
owner acceptance, installer всех 31 задач, merge или release.
