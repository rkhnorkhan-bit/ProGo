# F09 micro20: durable UI save и владелец операции

Source checkpoint 2026-10-13. Пакет проверяет реальный persistence boundary
при сохранении из `VaultForm` и владение raw save worker со стороны приложения.
F09 остаётся открытым, исторический счёт 22/31 не увеличивается.

## Контракт и область

- Add/Edit/Delete передают worker отдельный candidate; UI меняет session и
  строки только после подтверждённого durable save и живого владельца.
- Реальная запись сохраняется в принятом explicit `StoragePath`; accepted
  вспомогательная session сохраняется через собственный CAS persistence lease.
  У этих маршрутов отдельные файлы. Старая memory-only fixture не выдаётся за
  production durable session.
- `VaultWork` — фактическая raw save task. Запрос Cancel, Lock или выхода
  не подменяет её завершённой presentation task. Пока native запись не
  завершилась, приложение ждёт ограниченное время и отказывает в обслуживании
  или выходе до очистки прокси. После settlement нужен явный повторный запрос.
- Forced Dispose отменяет и закрывает владельца без синхронного ожидания IO.
  Позднее завершение не публикует записи и не меняет закрытые controls.
- Отмена не откатывает подтверждённую запись и не обещает остановку уже
  начавшегося native filesystem call. Потеря UI adoption после commit требует
  нового явного открытия для просмотра фактического состояния файла.

## Изолированные нативные проверки

`DesktopVaultDurableSaveTests.cs` содержит четыре ограниченных workflow:

1. Actual UI Add/Edit/Delete для real и accepted auxiliary session. После
   каждого действия backend заново открывает durable данные; проверяются
   record IDs, session/grid и неизменность отдельного real ciphertext. После
   сохранений единственный accepted auxiliary PIN остаётся действующим;
   удаление последней записи сохраняет привязанный пустой набор без новой генерации.
   Другой PIN возвращает точную ошибку «Неверный пин-код». Во время Add реальный
   lease Save удерживается внутри собственного gate: native heartbeat даёт
   не менее трёх ticks, вызов session IsCurrent на STA занимает менее 500 мс.
2. Actual real file sharing lock и actual auxiliary CAS conflict после
   конкурентной записи. Все три UI действия получают обычное предупреждение;
   session/grid не показывают phantom edits, ciphertext и принятая
   конкурентная запись не перезаписываются. Предупреждение не раскрывает
   путь, секрет или тип режима.
3. Actual application context со save worker, удержанным до записи. UI
   heartbeat остаётся активным; повторный shutdown возвращает ту же task,
   отмена наблюдается worker, deadline около трёх секунд отказывает до
   native cleanup. После raw settlement явный retry запускает каждый
   cleanup ровно один раз.
4. Forced context Dispose с удержанной raw save task: менее 500 мс до
   закрытия владельца, затем settlement без UI message pumping и без
   поздней публикации записей, native cleanup или изменения установленных
   файлов. В обеих ownership fixtures после settlement ссылка на candidate
   data в raw result должна быть сброшена.

Все real/daily vault файлы этих fixtures находятся в созданных тестом
каталогах; entry data и PIN синтетические. Нативный entry point защищён
`GITHUB_ACTIONS == true` и использует имеющийся isolated Windows harness.
Held workers ограничены 15 секундами; release и cleanup выполняются в finally.
`AsyncRestoreContextFixture` — существующий isolated harness для actual tray
context и счётчиков очистки. Устройства владельца не проверяются.

## Статус и границы

Source implementation / independent review: **PENDING**.
Native compile / WinForms / full Windows and server CI: **PENDING**.

Full active-session expiry, startup cleanup до входа, restart/day-boundary
end-to-end acceptance и полный F09 PASS относятся к следующему отдельному
пакету. Этот checkpoint не означает готовность установщика всех 31 задач,
owner acceptance или merge/release.
