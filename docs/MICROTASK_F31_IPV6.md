# Остаток 55: F31-R01 — IPv6 в SSH/SOCKS

Один ограниченный подпункт после принятых F17-R02/R03. Текущий счёт до новой
приёмки остаётся **2/55**, общий прогресс **23/31**; весь F31 остаётся OPEN.
[Список остатка](REMAINING_SUBTASKS_8.md). Исходный checkpoint:
`e303d804cc8568ee14866d4b1c9d0f508e01c137`.

## Дефект и границы исправления

Настройки принимают IPv6 в формах `::1` и `[::1]`, но -D в ProxyService
составлялся простым host:port. Для bare IPv6 отсутствовали скобки, а native
SOCKS probes, latency и CLI bridge создавали IPv4-only TcpClient на Framework.
Строка с IPv6 brackets также передавалась в socket connect без нормализации.

Общий небольшой SocksEndpoint helper в Core сохраняет IPv4, выбирает family
IPv6 dual-mode для literal адреса (включая mapped), снимает ровно одну допустимую пару IPv6 brackets и
составляет `[IPv6]:port` один раз. DNS использует dual-mode socket при доступном
IPv6, иначе IPv4; существующий BeginConnect и deadlines сохраняются. Создание
клиента не выполняет DNS или connect. Некорректные brackets не исправляются
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

Статус: **IN_PROGRESS**. Новый native PASS ещё не получен.
Frozen source/tests и этот PENDING документ: независимый review APPROVE;
локальные whitespace/public checks PASS (255 sources/0 local release files).
Ожидаются28 recovery scenarios (22 прежних +6 новых) и18 новых Desktop bridge
checks. Их фактический результат и число assertions устанавливает свежий CI.
Реальный encrypted-key agent/VPS, IPv6 UDP/телефонный маршрут и устройства
владельца не проверяются этим пакетом. Package PENDING, Live NOT_CHECKED;
цельный installer всех31, merge и release остаются следующими результатами.

Основания: [OpenSSH -D](https://man.openbsd.org/ssh.1),
[Microsoft Framework TcpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient.-ctor?view=netframework-4.7),
[Framework reference TcpClient](https://github.com/microsoft/referencesource/blob/main/System/net/System/Net/Sockets/TCPClient.cs)
и [Socket](https://github.com/microsoft/referencesource/blob/main/System/net/System/Net/Sockets/Socket.cs).
