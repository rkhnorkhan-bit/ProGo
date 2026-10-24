# Микрозадача 14 — атомарная запись суточного хранилища

## Граница изменения

Добавлен `VaultDailyStore.CompareExchange(root, expected, nextState)` — синхронный
слой для последующего вызова с рабочего потока. Он использует защищённый формат
из микрозадачи 12 и явное чтение с immutable stamp из микрозадачи 13.
UI, `VaultSession`, генератор ложных данных, запуск приложения, часы и обработка
полуночи пока не подключены. Общий статус плана остаётся **22/31**, F09 открыт.

## Протокол записи

1. Проверяется явный существующий локальный каталог и принадлежность stamp
   каноническому `vault.daily.dat`. Каталоги не создаются автоматически.
2. На Windows writer получает один `Global` mutex на SID текущего пользователя.
   ACL закрыт от наследования и разрешает полный доступ этому пользователю и
   SYSTEM. Для уже существующего named object ACL проверяется: только эти SID,
   только explicit Allow FullControl, без inherited/deny/чужих правил, и оба SID
   должны присутствовать (SYSTEM как текущий SID допускает одно merged правило).
   Некорректный existing ACL возвращает `FileUnavailable`, его права не меняются.
   Один gate для всех каталогов устраняет обход через алиасы путей.
   Ожидание ограничено 1000 мс; занятый gate возвращает `Busy`. При abandoned
   mutex ownership принимается, но файл обязательно читается заново.
3. Под gate выполняется свежий `Read`. Ошибки доступа, повреждение и неизвестный
   формат сохраняют свой тип; они никогда не превращаются в `Missing`.
   Для совпадения проверяются область пути, отсутствие/наличие файла, дата,
   поколение, ревизия и приватный SHA-256 digest фактически прочитанных байтов.
4. При первом создании выбираются новое поколение и ревизия 0. В пределах дня
   поколение сохраняется, ревизия увеличивается с проверкой переполнения.
   Связанный PIN нельзя заменить или сбросить; удаление всех записей оставляет
   PIN связанным. Переход из unbound в bound допустим.
5. Смена даты разрешает только пустой tombstone с новым поколением и ревизией 0.
   Прямое создание нового binding вместо такого reset отклоняется. Переход
   календаря назад обрабатывается так же. Ревизия `Int64.MaxValue` запрещает
   запись в том же дне, но допускает смену поколения при смене дня.
6. Кандидат кодируется и заново декодируется через DPAPI CurrentUser. Результат
   содержит собственные dataset/list/entries, а не ссылки на изменяемые данные
   вызывающего кода. Stamp также не зависит от последующих правок этих объектов.
   Вызывающий код обязан не менять candidate dataset одновременно с вызовом.
7. Staging создаётся через `CreateNew` в том же каталоге, с `FileShare.None`,
   `WriteThrough` и `Flush(true)`. Ownership staging фиксируется только после
   успешного открытия; чужой файл при коллизии имени не удаляется.
8. После закрытия staging вызывается узкий internal test hook, затем повторно
   проверяются каталог, его предки, атрибуты
   целевого файла и свежий stamp. Затем exclusive `CreateNew` создаёт стабильный
   нулевой `vault.daily.dat.recovery`, с `WriteThrough` и `Flush(true)`. Ownership
   marker фиксируется только после успешного открытия. При существующем target
   так же эксклюзивно резервируется `vault.daily.dat.recovery.previous`.
   Перед native publish пути и атрибуты проверяются ещё раз.
9. Для первого файла используется exclusive `File.Move`, для существующего —
   `File.Replace` с принадлежащим вызову `.recovery.previous` как backup.
   Native API не перемещает стабильный `.recovery` witness. Успешная замена
   помещает прежний ciphertext в отдельный backup. После полной публикации
   удаляются только собственный backup и затем стабильный witness, после чего
   выдаётся `Success`.
   При любом отказе после создания marker он остаётся; reader возвращает
   `FileUnavailable` без record/stamp. Это предотвращает трактовку неизвестного
   native исхода как нового `Missing` и создание другой ложной привязки.
   Delete/copy/rollback fallback отсутствует. Закрытый staging удаляется только
   при own ownership; witness при ошибке автоматически не удаляется.

Рабочие byte buffers очищаются. Код не обращается к `AppPaths`, реальному
хранилищу, часам и логированию; сообщения ошибок статические.

## Проверки

`VaultDailyStoreTests` компилируется отдельно с guard-заглушками `AppPaths` и
`SafeLog`. Весь filesystem ввода-вывода ограничен принадлежащими harness
временными каталогами и синтетическими данными.

- Создание, замена, сохранение правок, detached snapshot и неизменность stamp
  после правок возвращённого dataset.
- Пустой tombstone, первая привязка, запрет замены PIN и сброса в том же дне,
  сохранение привязки после удаления всех записей.
- Stale missing/revision/generation/day/digest и stamp другого каталога.
- Reset при смене даты в обе стороны, stale writer до reset, переполнение и
  новый день после исчерпанной ревизии.
- Повреждённый и неподдерживаемый текущий файл, target directory, отсутствующий
  root и изменённый невалидный candidate: ни одного ложного успешного результата.
- Узкий internal hook вызывается после закрытия staging и до final fresh read/path
  checks. Он нужен для настоящего native delete-lock целевого файла, проверки
  target, появившегося после исходного чтения, и directory перед final read.
  Проверяются сохранность прежних байтов в этих конкретных сценариях,
  удаление owned staging и сохранение recovery witness. Автоматические повторные
  чтение/запись блокируются. Последующая запись проверяется только после явного
  удаления marker тестом в synthetic fixture с предварительно доказанной
  сохранностью прежнего ciphertext; production recovery в этом пакете отсутствует.
- Synthetic persisted checkpoint после witness, но до первого publish, не
  превращает отсутствующий target в новый день: `Read` и следующий writer
  failclosed. Это fixture незавершённого состояния, а не имитация actual crash
  внутри API. Hook fault до witness отдельно проверяет известный pre-publication
  отказ без создания marker. Чужие заранее существующие recovery marker и
  orphan backup не меняются и не удаляются.
- Два настоящих дочерних процесса читают один stamp до старта записи: только
  один writer должен успешно опубликовать полную следующую ревизию, второй
  получает `Conflict`. Координация использует synthetic readiness markers,
  а не PIN/данные в аргументах или stdin.
- Отдельный процесс удерживает тот же SID gate при записи в другой каталог:
  bounded `Busy` без публикации. Отдельно проверяется abandoned mutex со свежим
  чтением после завершения владельца. Deadline и завершение ограничены
  конкретными созданными harness PID; гарантии прекращения дерева не заявляются.
- Заранее созданный mutex с World FullControl получает `FileUnavailable`,
  файловой публикации нет, существующий ACL остаётся неизменным.

Source review и native Windows CI будут записаны ниже после фактического
выполнения. Эта строка описывает исходную контрольную точку; до native evidence
ни один native результат не считается PASS.

## Оставшиеся ограничения

Это compare-exchange для согласованных ProGo writers, а не защита от владельца
Windows account, вручную восстанавливающего прежний файл или заменяющего
предков каталога в гонке. Проверки известных reparse points не являются
handle-relative filesystem sandbox. Другой Windows account/ПК не проверялся.

`File.Replace` не сопровождается обещанием неизменности старого файла при
любом аппаратном отказе или любом native error: native API имеет разные
варианты отказа. Сохранность прежних байтов подтверждается только конкретными
lock/pre-publication тестами. После исключения нет ложного `Success`, нет
разрушающего fallback; recovery witness блокирует обычное чтение до отдельного
протокола восстановления. Для documented native errors 1176 с backup исходные
имена сохраняются, при 1177 прежний target находится под отдельным backup именем.
Стабильный marker не передаётся native API как переименуемый backup.
Общая гарантия crash/hardware atomicity этим не заявляется.
При crash может остаться не опубликованный staging; startup cleanup будет
отдельной микрозадачей и не вправе удалять файлы по общей маске.

Остались явный recovery protocol до интеграции Open/UI, подключение
policy/adoption после durable commit, сохранение CRUD из
сессии, startup expiry, блокировка активной сессии в полночь, UI и owner acceptance.
Окружение владельца не проверялось; release/merge/публикации приложения нет.

## Завершённая native контрольная точка 13–14, 2026-10-12

Production micro14 — `387c5bcbab23f94dd2270e5c716f0b37ed73d513`; итоговый
test-correction head — **`3eaec3ab46a064fd3072c91cb96c80b416533a66`**.
[CI #532](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38227655739)
проверил merge **`6bd2ef1764cae9429c626261e17632ad353ea8ab`** с прежним main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`. Windows `114739023300`, server
`114739024077` и весь run завершены **SUCCESS**. Source review и завершённый
Store log сверены независимо.

| Проверка | Фактическое доказательство |
| --- | --- |
| Отдельный C#5 Store harness с guards | **149 PASS assertions**, 09:06:21.4596637 UTC; zero owner-path/log accesses |
| Создание/замена, detached snapshot, PIN/day/revision/CAS | Выполнены; устаревшие stamp не принимаются, удаление всех entries сохраняет привязку, reset в обе стороны меняет generation |
| Реальный delete-lock во время `File.Replace` | 09:06:18.746: отказ, старый ciphertext сохранён, own staging удалён; стабильный witness и отдельный backup оставлены, обычные Read/Write блокируются |
| Два настоящих child writers с одним stamp | 09:06:19.017: один Success и один Conflict, одна полная следующая ревизия и полный dataset победителя |
| Global SID gate для разных каталогов | Bounded Busy без публикации, 09:06:20.065; abandoned owner разрешает свежий read/commit, 09:06:21.455 |
| Чужой recovery marker/backup и broad existing mutex ACL | Не изменены; небезопасный ACL отклонён без публикации и без изменения прав, 09:06:21.459 |
| Полные Windows/server наборы | PASS; File110, прежние reader165/policy134/record325, desktop3485, Test-ProGo, HomeVPN975, portable90, Windows Python22 PASS +2 platform skips; Ubuntu16/9/46 |

История #529–531 сохранена в
[MICROTASK_VAULT_DAILY_FILE.md](MICROTASK_VAULT_DAILY_FILE.md): каждый ранний
Windows run упал в ACL fixture чтения до отдельной Store-harness компиляции и
запуска. Application build уже компилировал production Store, но не доказывал
его native поведение. Исправления после micro14 затрагивали только File tests;
production reader/store не изменялись.

Тестовое удаление witness после lock failure сделано только в owned synthetic
fixture после проверки сохранности прежних bytes. Это не production recovery.
Persisted initial-publication fixture не доказывает actual crash; родитель
ограничивает собственные child PID, без обещания остановки дерева процессов.
1000 мс ограничивают mutex wait, а не произвольный native I/O/DPAPI.

Явный recovery protocol и ownership оставшегося staging — следующий небольшой
пакет **до Open/UI integration**. Затем нужны durable adoption, CRUD,
startup/day expiry и active-session cleanup. F09 открыт, **22/31** неизменны;
целый installer всех31, другой account/ПК и owner acceptance не проверены.
CI упаковка не является опубликованным release. Подробные результаты и
artifacts: [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md).

## Изменение протокола после контрольной точки 13–14

Micro15–16 заменяет новый нулевой witness полным защищённым journal, а новый
backup получает unique имя `vault.daily.dat.previous-<Operation:N>`. Старый fixed
`.recovery.previous` остаётся неизвестным legacy evidence и блокирует доступ.
После попытки атомарной публикации journal writer сохраняет stage/backup при
ошибке; это может быть единственный проверяемый новый ciphertext. Общая очистка
по маске не добавлена. У неизвестных остатков до journal ownership не доказан.

Перед target publish проверяются exact stage stamp, прежний CAS, исходные bytes
journal и отсутствие legacy backup. Перед удалением journal заново проверяются
exact target, journal bytes и legacy отсутствие. Изменённые артефакты не
удаляются и не приводят к Success. Recovery использует тот же gate, возвращает
подтверждённый Previous/Next отдельно и не увеличивает revision.

Это последующий протокол; исходная таблица 13–14 выше относится к своему
историческому tested head. Новые native результаты сохраняются отдельно в
checkpoint 15–16 и
[MICROTASK_VAULT_DAILY_RECOVERY.md](MICROTASK_VAULT_DAILY_RECOVERY.md).
