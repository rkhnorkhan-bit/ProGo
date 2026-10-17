# Остаток 55: F31-R01 — IPv6 в SSH/SOCKS

Один ограниченный подпункт после принятых F17-R02/R03, принят в Code/Dev
**14.10.2026 UTC**. Теперь **3/55**, осталось52 (40 до установщика +12
зависимых приёмок). Общий прогресс **23/31**; весь F31 остаётся OPEN.
[Список остатка](REMAINING_SUBTASKS_8.md). Исходный checkpoint:
`e303d804cc8568ee14866d4b1c9d0f508e01c137`.

## Дефект и границы исправления

Настройки принимают IPv6 в формах `::1` и `[::1]`, но -D в ProxyService
составлялся простым host:port. Для bare IPv6 отсутствовали скобки, а native
SOCKS probes, latency и CLI bridge создавали IPv4-only TcpClient на Framework.
Строка с IPv6 brackets также передавалась в socket connect без нормализации.

Общий небольшой SocksEndpoint helper в Core сохраняет IPv4, выбирает family
IPv6 dual-mode для literal адреса (включая mapped), снимает ровно одну допустимую пару IPv6 brackets и
составляет `[IPv6]:port` один раз. Создание клиента не выполняет DNS или connect.
Общий connector выполняет async DNS и независимые APM подключения по найденным
адресам с fresh family clients в одном общем deadline. Переданный caller winner
отделяется от losers; timeout/cancel запрещают позднее создание подключения и
закрывают неусыновлённые sockets. Некорректные brackets не исправляются
в допустимый адрес; политика SettingsValidation сохранена.

Изменяемые маршруты: SSH -D, TCP ownership probe, SOCKS handshake/recovery,
ConnectionHealth, SOCKS latency и CLI HTTP/CONNECT bridge. Profile fallback,
классификация постоянных SSH ошибок и config Include/tokens относятся к
отдельным F31-R02/R03/R04. IKEv2 relay уже использует address.AddressFamily;
его source и самостоятельные compile dependencies этим пакетом не меняются.

## Приёмка

1. Независимый review точного source/tests diff, whitespace/public checks.
2. Meaningful native fixtures: captured child argv и разбор -D системным
   OpenSSH без подключения к VPS; настоящий локальный IPv6-only SOCKS exchange
   для bare/bracketed адреса, readiness/recovery/stop/cancel/foreign ownership.
3. IPv4/DNS regression, latency SOCKS CONNECT и HTTP/CONNECT через CLI bridge
   на локальном IPv6 fixture; прежние assertions и budgets не ослабляются.
4. Свежий полный completed Windows/Ubuntu CI на зафиксированном source;
   прежний CI #541 не подменяет проверку нового кода.
5. Только после native PASS и evidence review — **3/55**.

Статус: **Code/Dev PASS** на свежем completed CI #544. Frozen source/tests,
PENDING документ и native результаты прошли независимое review; root также
проверил completed logs, run/jobs, source/merge и artifacts metadata.
Whitespace/public PASS: локально255/0, native255 sources/44 release files.

Functional source `9e1f2ac9a27dfab63083b2d287f1d53018e1a476`, tree
`7906427e9f9ff7d732ff5aa8b6e791716488982a`; tested merge
`ae7794627650f2742e5e666e036932853902fca6` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #544](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38567399942):
Windows `115863485883`, Ubuntu `115863486254` и весь run completed SUCCESS.

Реальный encrypted-key agent/VPS, IPv6 UDP/телефонный маршрут и устройства
владельца не проверяются этим пакетом. Package PENDING, Live NOT_CHECKED;
цельный installer всех31, merge и release остаются следующими результатами.
После functional checkpoint изменяются только три Markdown-документа
доказательств; app/tests/scripts совпадают с проверенным source.

Основания: [OpenSSH -D](https://man.openbsd.org/ssh.1),
[Microsoft Framework TcpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient.-ctor?view=netframework-4.7),
[Framework reference TcpClient](https://github.com/microsoft/referencesource/blob/main/System/net/System/Net/Sockets/TCPClient.cs)
и [Socket](https://github.com/microsoft/referencesource/blob/main/System/net/System/Net/Sockets/Socket.cs).

## История текущей проверки

- CI #542 (`38560899731`), source
  `6e46ad7f385169b680996a4bb3a3b9355575685d`, merge
  `b02ff40356d5fcb9828b8351073422c4bcddb767`: Windows FAIL /Ubuntu PASS,
  общий FAILURE (2026-10-14 12:05:57 UTC). В12:05:51.5239295 UTC новый
  localhost compatibility fixture остановился на asynchronous readiness.
  Прежние22 SOCKS scenarios прошли; новый scenario не завершён. Новые IPv6,
  Desktop/bridge, HomeVPN/Python/public/package downstream ещё не достигнуты.
- Конкретная причина пока не установлена: исходный assert объединял
  Wait5000/result/IsListening. Узкая test-only диагностика различает эти
  состояния и native APM/DNS localhost outcomes. Production, assertion и
  budgets не изменяются; следующий полный CI должен дать фактическую причину.
- #542 создал только diagnostics `11892595467`; screenshots отсутствуют
  (no files warning), runtime folder/ZIP не созданы. Ubuntu16/9/46 PASS
  без skips и independent QR decode не подменяют незавершённый Windows.
- CI #543 (`38562946918`), diagnostic source
  `89e61f5a78aa61b26fd19307b9a3372a58b0ae16`, merge
  `85ee7ba87279dc44c44471cb143dc2d2d1844abc`: Windows FAIL /Ubuntu PASS,
  общий FAILURE (2026-10-14 12:22:50 UTC). Production не менялся.
  В12:22:43.9530985 UTC исходный Wait5000 вернулfalse при pending task,
  owned PID3568 и startup=none. DNSlocalhost завершился за6ms: сначала::1,
  затем127.0.0.1. Fresh AF6 dual-mode APM по hostname не завершился за1002ms;
  explicit mapped IPv4 и IPv4 literal подключились за0ms к тому же порту.
  В12:22:45.1102453 UTC исходный assert снова FAIL. Printed listening=false
  был gated ready&&IsListening, а не отдельной отрицательной TCP-пробой.
- Это устанавливает регрессию hostname/APM в текущем бюджете при быстром DNS
  и доступном literal listener; конкретный механизм внутри Framework остаётся
  выводом из source review. План коррекции: bounded async DNS и independent
  fresh family clients в одном total deadline, безопасная передача winner,
  закрытие losers и запрет позднего connect после timeout/cancel. IPv4-only
  и IPv6-only localhost fixtures должны подтвердить обе стороны; profile
  fallback не меняется. Новый native PASS пока не получен, счёт2/55 сохранён.
- #543 downstream Windows опять не достигнут; есть только diagnostics
  `11892868304`. Ubuntu16/9/46 и independent QR decode PASS без skips.

- CI #544 (`38567399942`): свежая коррекция DNS/address connector и fixtures
  завершились SUCCESS; весь run completed 2026-10-14 13:12:31 UTC. Ошибки
  #542/#543 остаются историей тех source, downstream не получают PASS задним числом.

## Фактическая native приёмка #544

| Новая группа | Assertions PASS | UTC завершения |
| --- | ---: | --- |
| Endpoint forms, IPv4/localhost, mapped IPv6 и настоящий localhost→IPv6-only | 35 | 12:57:17.1005677 |
| Bare/bracketed IPv6 child argv, readiness, greeting, latency CONNECT | 22 | 12:57:19.5477312 |
| IPv6 crash recovery, stop и повторное использование порта | 22 | 12:57:25.8178556 |
| IPv6 pending startup cancel/timeout и retry | 6 | 12:57:31.1474154 |
| IPv6 foreign/silent listener, bounded и cancelled probes | 16 | 12:57:35.1790441 |
| Native OpenSSH -G с bracketed -D, без подключения к VPS | 2 | 12:57:35.3015587 |
| Delayed DNS cancel/deadline, pre-cancel и late no-dial | 15 | 12:57:36.5187461 |
| **Новые recovery assertions** | **118** | |

Все **29 SOCKS scenarios /240 assertions** PASS в12:57:36.5188605–5190162 UTC:
прежние22/122 плюс7/118 новых. DNS cancellation завершает caller до ответа
resolver; поздний ответ не вызывает dial по source ownership review и
bounded наблюдению настоящих IPv4/IPv6 listeners. Последнее отдельно не
выдаётся за доказательство всех возможных scheduler interleavings.

CLI bridge: **27 новых checks** в6 случаях `::1`/`[::1]`/`localhost` ×
HTTP/CONNECT, 13:00:39.2178324–2547912 UTC. IPv6-only listener принимает реальный
AF6 peer; проверены SOCKS framing/target, точный HTTP response либо ping/pong,
stop и reusable ordinary HTTP port. Desktop **3842 PASS** в13:04:27.7455922 UTC.

Прежние suites на том же source: reader/policy/record/file/journal/store/
recovery/open **165/134/325/110/211/151/274/112**, expiry130, restart237/12 stages;
Instance26, Maintenance95, Shutdown47, updater package5 PASS. Полный Test-ProGo
PASS в13:07:13.3188670 UTC. Предыдущие F09/F17 fixtures выполнены в этом прогоне;
их ранее принятые визуальные/DPI границы не расширяются сетевым исправлением.
HomeVPN975 PASS в13:10:27.4670724, portable90 PASS в13:12:13.2177381 UTC.
Windows Python:24 reported items =22 PASS +2 прежних Linux module skips;
все7 HTTP tests выполнены. Public255/44 PASS в13:12:16.5157097 UTC.

Ubuntu:16 operation tests,9 sharing/sync tests,46 provisioning checks PASS,
без skips. Actual numpy2.2.6/zxing-cpp2.3.0 installed в12:54:25.7507980;
independent QR decode OK в12:54:26.3241014 UTC. Job log66.703s <12min budget.
Ожидаемые отрицательные maintenance/server fixtures не являются отказами
кандидата; native error annotations/compiler errors отсутствуют.

| CI artifact | ID | Фактический результат |
| --- | --- | --- |
| Screenshots | 11897700449 | 287 files uploaded; metadata SHA256 f6ea415395c07c33ae5dc36d010e1f854f8591b35939ff41492f7ca846455490 |
| Diagnostics | 11897480669 | uploaded |
| Release folder | 11897535603 | uploaded |
| Release ZIP | 11897580658 | uploaded; inner ZIP519457 bytes; metadata SHA256 a8bc7cf49f986ec4772b0f6ae947118b22725d85db4aa970d17276b73f704aa9 |

Это CI runtime artifact; цельный setup всех31 ещё Package PENDING.
Completed Windows log:902983 UTF-8 bytes/8300 lines, SHA256
`006bf90e2665faf6685b94cf021816c05120185daf1d127b04e1603a0decc503`.
Root fetched log независимо; bytes/lines совпали с сохранённым review log.
