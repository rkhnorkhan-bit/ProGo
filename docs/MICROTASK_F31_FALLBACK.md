# Остаток55: F31-R02 — выбор запасного SSH-профиля

Один ограниченный подпункт после принятых F17-R02/R03 и F31-R01.
До свежей приёмки счёт остаётся **3/55**, остаток52 (40 до установщика +12
зависимых приёмок), общий прогресс **23/31**, весь F31 **OPEN**.
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

Статус: **IN_PROGRESS**. Source FROZEN и независимый review APPROVE;
tests FROZEN после точечного review timer/Stop fixture. Ожидаются37 scenarios /
312 assertions: прежние29/240 +новые8/72. Фактические числа и PASS устанавливает
свежий полный CI. Новый native PASS не получен; CI #544 подтверждает только
предыдущий IPv6 checkpoint. Локальные whitespace/public PASS:256 sources/0
release files. Все новые десятисекундные fixture guards локальны для сценариев;
прежние guards сохранены. Manual candidate теперь имеет linked deadline5s,
общий startup20s и probe700ms сохранены; EOF observation≤200ms.
Файлы текущей реализации: ProxyService, SocksRecoveryTests и этот документ;
после #545 добавлена узкая коррекция prerequisite в home_vpn_setup.
Классификация config/arguments F31-R03, Include/tokens, агент/VPS и другие
пункты в этот подпункт не входят. Package PENDING, Live NOT_CHECKED;
владелец устанавливает и проверяет цельный продукт после завершения кода31.
PR draft/unmerged; merge, release, ПК/VPS/телефон владельца не затрагиваются.

Основание stderr boundary: [Microsoft Process.WaitForExit](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=netframework-4.8).
Timed exit/HasExited не гарантирует завершение async output handlers;
unbounded WaitForExit для ожидания handlers здесь не подходит.

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
