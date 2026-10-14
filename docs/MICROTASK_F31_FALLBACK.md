# Остаток55: F31-R02 — выбор запасного SSH-профиля

Один ограниченный подпункт после принятых F17-R02/R03 и F31-R01.
Принят в Code/Dev **14.10.2026 UTC**: счёт **4/55**, остаток51 (39 до
установщика +12 зависимых приёмок), общий прогресс **23/31**, весь F31 **OPEN**.
[Список55](REMAINING_SUBTASKS_8.md). Исходный checkpoint:
`5f89598f19f2d42309e89dfe4316d1fc861e08c7`.

## Подтверждённые дефекты

- StartTunnelCore выбирал и сохранял профиль после IsListening: открытый TCP
  порт не подтверждал корректный SOCKS greeting. Owned child с неверным
  ответом мог завершить перебор до рабочего backup.
- ScheduleRecovery/PollRecovery приостанавливали весь список из-за sshError
  одного child. Permanent key/host-key refusal должен исключать конкретного
  кандидата; остальные разрешённые профили остаются доступными.
- ErrorDataReceived проверял sender против изменяемого sshProcess, затем
  писал глобальный sshError. Между проверкой и записью процесс мог смениться;
  поздняя ошибка прежней попытки могла попасть в новую.
- Async wrapper проверял SOCKS раньше owned alive и мог принять чужой
  заранее запущенный валидный SOCKS listener. Он должен остаться чужим.
- Проверка устаревших настроек выполнялась после всего manual перебора.
  Следующий candidate и публикация выбора также требуют актуального
  подключения и сохранённого намерения операции.

## Узкая коррекция и приёмка

1. Classified stderr принадлежит captured attempt; callback не захватывает
   process gate и не пишет ошибку другой попытки. Завершение процесса и
   доставка stderr различаются; ожидание последней ограничено и отменяемо.
2. Permanent refusal исключает candidate connection identity в текущем
   explicit Connect cycle. При исчерпании разрешённых кандидатов сохраняются
   fixed guidance и pause. AutoRestart toggle не снимает её.
3. Выбор требует owned alive child до/после настоящего ответа SOCKS05/00,
   актуальных настроек, token/wanted и существующего revision CAS.
4. Recovery backoff сохраняется отдельно от обхода кандидатов. AutoSwitch
   false разрешает только выбранный профиль. Single-flight, epoch, bounded
   Stop, retained ownership и настройки OpenSSH сохраняются.
5. Новые meaningful native fixtures: permanent→backup для manual/recovery,
   wrong TCP→greeting-gated backup без early save, Stop/cancel/stale settings,
   fallback off/single/all-permanent pause и чужой valid SOCKS listener.
   Прежние29 scenarios/240 assertions и IPv6/DNS проверки не ослабляются.
6. Independent frozen source/tests/document review, whitespace/public,
   свежий полный completed Windows/Ubuntu CI и evidence review. Только после
   всей приёмки — **4/55**, остаток51 (39 до установщика +12 зависимых).

Статус: **Code/Dev PASS**. Сетевое ядро/tests FROZEN; независимые source/tests
и полные native Windows/Ubuntu reviews APPROVE. Свежий completed CI #547
подтвердил37 scenarios/312 assertions: прежние29/240 +новые8/72.
CI #544 остаётся доказательством предыдущего IPv6 checkpoint.
Локальные whitespace/public PASS:256 sources/0 release files.
Все новые десятисекундные fixture guards локальны для сценариев;
прежние guards сохранены. Manual candidate теперь имеет linked deadline5s,
общий startup20s и probe700ms сохранены; EOF observation≤200ms.
Файлы текущей реализации: ProxyService, SocksRecoveryTests и этот документ;
после #545 добавлена узкая коррекция prerequisite в home_vpn_setup.
После #546 consumer orchestration и её UI fixtures FROZEN, независимый
source/tests review APPROVE. Fresh worker SOCKS probe используется только
для5 consumer-команд; persistent UI owner проверяет request и сохранённое
подключение перед reuse или owned fallback. Поздний Off/смена endpoint
отбрасывают результат; собственный успешный profile CAS не отменяется.
Добавлены9 Desktop checks:3 borrowed CLI aliases без PID/intent/recovery
и6 native checks отмены/смены порта во время actual5/1/0 probe. Прежние
assertions/budgets сохранены; Desktop3851 и SOCKS37/312 фактически PASS в #547.
Bounded UI observation700ms дополняется source owner-turn proof и отдельно
не выдаётся за доказательство всех scheduler interleavings. Полный CI SUCCESS.
Классификация config/arguments F31-R03, Include/tokens, агент/VPS и другие
пункты в этот подпункт не входят. Package PENDING, Live NOT_CHECKED;
владелец устанавливает и проверяет цельный продукт после завершения кода31.
PR draft/unmerged; merge, release, ПК/VPS/телефон владельца не затрагиваются.

Основание stderr boundary: [Microsoft Process.WaitForExit](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=netframework-4.8).
Timed exit/HasExited не гарантирует завершение async output handlers;
unbounded WaitForExit для ожидания handlers здесь не подходит.

## Свежая полная приёмка — CI #547

Functional source `7521e1b2d6770350a4aece78b595cd53aa00370e`, tree
`6bbe3d953a8d888242d542e470fdfe4148a93f24`; tested merge
`d06bf6acfb3d5314dbc72a20a0c9897d270f5161` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #547](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38587581940)
completed SUCCESS: Windows `115933580160`, Ubuntu `115933579707`, весь run.
Root и независимые reviewers прочитали полные завершённые логи; частичные
результаты #545/#546 не подменяют свежую приёмку.

Новые8 SOCKS groups, фактические assertions:

| Результат | Assertions |
| --- | ---: |
| Permanent refusal → разрешённый manual backup | 10 |
| Automatic permanent → transient → готовый backup, launch journal/backoff | 14 |
| Выбор публикуется только после правильного greeting | 4 |
| Stop/external cancel во время gated fallback, без save/resurrection | 10 |
| Timer Stop при greeting в полёте, без позднего выбора | 8 |
| Fallback off/single/exhausted pause и explicit retry | 15 |
| Чужой valid SOCKS остаётся чужим для owned startup | 3 |
| Selected/backup edits отменяют устаревший перебор | 8 |
| **Итого новых** | **72** |

SOCKS37/312 PASS **15:18:45.8300340–8301005 UTC**; первые29 groups/240
assertions сохранены. Новые9 consumer checks: три CLI aliases используют
готовый saved SOCKS без SSH PID/intent/recovery; по три проверки для Off и
изменения порта во время реального5/1/0 probe. Все PASS; исходные12 проверок
UnifiedCliActions и2 tray labels также PASS. Desktop3851
**15:25:43.4537311 UTC**, полный ProGo PASS **15:28:24.6509681 UTC**.

| Остальной свежий proof | Результат |
| --- | --- |
| Vault read/policy/record/file/journal/store/recovery/open | 165/134/325/110/211/151/274/112 PASS |
| Daily expiry/restart | 130/237 PASS |
| Instance/Maintenance/Shutdown/update package | 26/95/47/5 PASS |
| HomeVPN/portable | 975/90 PASS, 15:31:49.2445552 /15:33:36.9812128 UTC |
| Windows Python/HTTP | 22 PASS +2 прежних Linux ownership/flock skips; все7 HTTP groups выполнены |
| Ubuntu operations/sharing/provisioning | 16/9/46 PASS без skips; actual independent QR decode15:14:42.1987381 UTC |
| Public scan/runtime ZIP | 256 sources/44 release files PASS; inner ZIP521084 bytes |

Ubuntu снова реально обновил openssh-server13.19→13.20. Неизменённый checked
service prerequisite предшествует managed config/sshd-t; все46 ограничений,
отзыв/перевыдача, private receipts и real TLS PASS. Точный package stop/socket
механизм по-прежнему inference: systemd-state snapshot отсутствует.
Изолированная проверка SSH не является проверкой VPS владельца.

Artifacts uploaded: screenshots `11906449001` (287 files/6380506 bytes),
diagnostics `11906419053` (81543 bytes), runtime folder `11906488941`
(44 files/524700 bytes), ZIP artifact `11906429096` (517738 bytes).
Размер ZIP artifact wrapper отличается от inner ZIP521084 bytes.
Metadata SHA256 ZIP artifact:
`c1a930e4d8f8020c55192b72e2bb9842ab7d86a51b59824e69b8b5a3c642bab9`.
Metadata/runner upload digests совпадают; отдельное download/hash или новая
визуальная приёмка287 PNG здесь не заявлены. Это CI runtime artifact,
цельный setup/updater всех31 остаётся Package PENDING; Live NOT_CHECKED.

Только F31-R02 получает новый PASS ID: **4/55**, осталось51=39+12.
Общий23/31 и целые F17/F31 OPEN сохраняются. После tested source обновляются
ровно три Markdown-документа доказательств; app/tests/scripts/server/workflow
совпадают с проверенным source. Независимый review ровно3 evidence-документов
APPROVE;55 unique IDs и четыре PASS перепроверены отдельно. Whitespace/public
PASS; functional source после native приёмки зафиксирован без изменений.

## История текущей проверки

- CI #545 (`38582571487`), source
  `d423f830dfbaa6512817aace5e688fb688ce22a8`, tree
  `90f7c9c6465851dca1b16e5dce2070bd1376fb61`, tested merge
  `a9be7b0a0d794b2acaf86a0dbb421b194447fb4c` в прежний main4bb3365:
  общий FAILURE, Windows `115916018761` FAIL и Ubuntu `115916019489` FAIL.
  Windows14:40:26.8450793 UTC: compiler CS0136, lambda parameter target
  конфликтует с последующим local target в PollRecovery. Ни foundation,
  ни37 SOCKS scenarios/312 assertions, Desktop или остальной Windows хвост
  не запускались. Коррекция только переименовывает lambda parameter и ссылку
  на него; source semantics/assertions/budgets сохраняются, review APPROVE.
- Ubuntu16 operation tests и9 sharing/sync tests PASS, actual independent
  QR decode выполнен. Provisioning14:40:49.9655319 UTC: `/run/sshd` отсутствует,
  `sshd -t` возвращает255 до первого provision assertion. Fixture предварительно
  запускал SSH service; далее prepare обновил openssh-server13.19→13.20.
  Причина удаления runtime directory внутри package/service transition пока
  проверяется; provisioning46 PASS и общий Ubuntu PASS не заявлены.
- #545 содержит только diagnostics `11902729022`,645 bytes, metadata SHA256
  `caa0b14979af66654439144679251784715c1d533340a2d2a3ccd8c0cc5008f9`.
  Screenshots отсутствуют (upload step success с no-files warning), runtime
  folder/ZIP не созданы. Счёт3/55 сохранён до свежего полного native PASS.
- Узкая коррекция Ubuntu bootstrap: после успешного apt install подготовка
  вызывает checked `systemctl start ssh.service` до создания managed state
  и SSH_CONFIG, проверки `sshd -t` и существующего reload. Working service
  получает start, а не restart; остановленная служба получает штатную
  RuntimeDirectory. Ошибка запуска остаётся ошибкой подготовки.
  Provision fixture и46 security assertions не менялись. Повторный APT
  внутри fixture и обход проверки не добавлены. Source/syntax/whitespace
  review APPROVE; фактический эффект требует свежего полного Ubuntu CI.
  Exact package stop/socket transition не доказан логом и остаётся inference.
  Это отдельный обнаруженный bootstrap-дефект, без дополнительного PASS ID55.

Основание штатной RuntimeDirectory: [Ubuntu Noble OpenSSH changelog](https://lists.ubuntu.com/archives/foundations-bugs/2024-May/515145.html)
и [upstream systemd.exec](https://github.com/systemd/systemd/blob/main/man/systemd.exec.xml).

- CI #546 (`38583881188`), source
  `812f1351ed528c01e0da9f2edc2c281340496041`, tree
  `0d4f9d988164247dedfce9a99325260ab547b83c`, tested merge
  `5b0af579896aa7393d5547fc6c4ba591d72b901c`: Windows `115920580198` FAIL,
  Ubuntu `115920580974` SUCCESS, общий FAILURE. Compiler исправлен;
  все37 SOCKS scenarios/312 assertions PASS14:52:50.0262631–0263067 UTC:
  прежние29/240 +новые8/72, группы10/14/4/10/8/15/3/8. Foundation/expiry/restart
  прошли; Desktop остановился14:55:19.2353612 UTC на UnifiedCliActions cli-start
  с неожиданным occupied-SOCKS dialog. Desktop/остальной Windows хвост не PASS.
- Причина интеграционного отказа установлена source/contract review: manual
  CLI/Windows/scoped consumers раньше употребляли saved reachable SOCKS,
  не принимая его процесс во владение. Их orchestration безусловно вызывала
  StartTunnelAsync, который теперь честно подтверждает только owned SSH.
  Automation и bridge уже допускают готовый настроенный transport. Исправлять
  этот поддерживаемый сценарий заменой прежних assertions было бы неверно.
  Узкая коррекция: fresh async SOCKS probe только для5 consumer commands,
  UI request/connection guards перед adoption результата и owned fallback;
  Connect/Reconnect/HomeVPN сохраняют strict owned startup. Off во время
  preprobe не должен создать поздний SSH child. Новые native guards pending.
- Ubuntu16/9/46 PASS без skips; actual QR decode14:49:11.8621187 UTC.
  APT снова обновил13.19→13.20, затем managed sshd-t/isolated listener и все
  ограничения прошли; bootstrap correction подтверждена в dev. Точный
  stop/socket механизм package transition остаётся inference.
- #546 artifacts: screenshots `11904851394`,39 files/612992 bytes, metadata
  SHA256`f1edde48e882831e0923360cf9ba8fe361856b46d3a39aec6be29c3dc8ce4341`;
  diagnostics `11904781529`. Runtime folder/ZIP отсутствуют. Частичные
  сетевые/Ubuntu PASS не закрывают F31-R02; счёт3/55 сохранён.
