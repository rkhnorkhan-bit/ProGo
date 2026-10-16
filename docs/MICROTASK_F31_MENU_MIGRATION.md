# Остаток55: F31-R05, пакет B — миграция меню в фоне

Одна ограниченная микрозадача после пакета A, checkpoint
`d2fe109937922203d7ee7f8d4012a9478c1879b3`. Проверка PENDING: новый полный
native Windows/Ubuntu CI и независимый review ещё не завершены.

Program планирует миграцию только у installed owner после instance.Attach;
специальные каталоги, файловые операции и ShellLink COM выполняются в worker.
Один task включает очередь UI dispatch, worker и cleanup. Повторные запросы
не запускают новую миграцию. Shutdown отменяет ожидающую работу и bounded-await
этого полного task до handoff; timeout сохраняет приложение и допускает retry.
Dispose отменяет queued reservation или сигнализирует active worker; начатая
операция ОС не объявляется физически прерываемой. Token проверяется перед
следующими mutation boundaries. Replacement создаётся и проверяется до удаления
legacy; customized/foreign ярлыки сохраняются. Старые public API сохранены.

Новый native partial использует настоящие временные ShellLink, независимый
COM reader и реальную IPC activation при удержанном worker. Проверяются UI
heartbeat, task identity, отмена, bounded shutdown/retry, Dispose и byte guards.
Hook задерживает настоящий pipeline перед mutation, не заменяет его успехом.
Новых Check фактически22: native success7, held shutdown4, queued shutdown2,
queued/running Dispose4, custom/foreign3, portable1, bad path1. Ожидаемый
Desktop3961=3939+22 ещё не присвоен. Прежние проверки и budgets не меняются.
Root подтвердил: старые DesktopTests/compiler побайтно эквивалентны HEAD
после удаления только новых wiring-вставок; whitespace/public264/0 PASS.
Native результат пока не присвоен.

Счёт остаётся **6/55**, остаток **49=37+12**, общий **23/31**. F31-R05
**PARTIAL**, целые F17/F31 **OPEN**: ранний bootstrap/settings и scoped
shortcut/launch ещё впереди. Package PENDING, Live NOT_CHECKED. Устройства
владельца не проверяются; PR draft/unmerged, merge/release не выполняются.
Открытое F17-R01 замечание по disabled startup подписи сохраняется.
