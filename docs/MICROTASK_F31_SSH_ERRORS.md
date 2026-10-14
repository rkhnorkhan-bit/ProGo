# Остаток55: F31-R03 — постоянные ошибки SSH config и параметров

Один ограниченный Code/Dev результат после F31-R02. Исходный checkpoint
`7fd24324ac39a790cc0aca808b25def49f33d21d`; ранее принятые4/55 подтверждены
полным CI #547. До новой полной приёмки счёт **4/55**, осталось51=39 до
установщика +12 зависимых приёмок; общий **23/31**, весь F31 **OPEN**.
[Список55](REMAINING_SUBTASKS_8.md).

## Подтверждённые пробелы и граница

- Runtime stderr classifier знает key/host-key refusal, но ошибки config и
  командных параметров остаются неизвестными и повторяются с backoff5–60s.
- ArgumentException при построении аргументов возникает до Process; fixed
  permanent reason не сохраняется. Ошибка параметров должна принадлежать
  connection identity своего candidate и не создавать PID.
- Нынешний Contains(permission denied) способен принять сетевой EACCES за
  отказ SSH-аутентификации. Требуется характерная auth message shape.
- Unsupported/ignored опции и предупреждение о недоступном identity file
  сами по себе не доказывают постоянный отказ. Unknown stderr и временные
  DNS/connect/refused/reset/timeout ошибки сохраняют автоматические повторы.

Коррекция использует принятые per-attempt state, candidate identity и map
из R02. Постоянная причина исключает только своего кандидата; разрешённый
backup остаётся доступен. Общая пауза наступает после исчерпания кандидатов;
preference toggle её не снимает, explicit Connect после исправления снимает.
Видимые причины — фиксированные русские сообщения с действием; raw stderr,
пути/значения аргументов и Exception.Message в guidance не переносятся.

Существующие owned startup/consumer reuse, profile CAS, cancellation и
backoff/20s startup/5s candidate/700ms probe/200ms EOF budgets сохраняются.
Ошибки запуска executable остаются отдельными от validation параметров.
R04 Include/aliases/IdentityFile tokens и диагностический parser, agent/VPS,
UI/installer/прочие F31 задачи в этот пакет не входят.

## Проверка

Статус **IN_PROGRESS**: source FROZEN, независимый source review APPROVE;
tests FROZEN после двух контрольных payload rows; финальное независимое review
source/tests/PENDING документа APPROVE. ProxyService +38/−8, tests +122;
прежние37/312 условия сохранены;
прочие application/tests/scripts/server/workflow файлы не менялись.
Ожидаются **41 scenarios/403 assertions**: прежние37/312 +новые4 groups/91
(30/15/18/28). Desktop3851 не меняется. Это ожидаемые числа, не новый PASS.

Meaningful fixtures: actual Windows OpenSSH -G с изолированным temp
config/явными неверными аргументами, без user config/подключения; controlled
stderr replay через native child проверяет production redirected pipeline.
Дополнительно candidate→backup, permanent pause/no PID/исправление параметров
под прежним ID→SOCKS и transient/warning boundaries. Прежние условия и
deadlines не ослабляются; unknown stderr не объявляется постоянной ошибкой.
Пять native parser cases: неизвестная config опция, неверный config Port,
отсутствующий аргумент Hostname, неверный -D и неизвестная -o опция. Direct
-o ошибка не обязана иметь итоговый fatalaggregate. Для каждого проверяются
фиксированная причина/пауза/отсутствие raw path, no timer/toggle relaunch и
исправление под тем же alias с настоящим SOCKS greeting.
Три prelaunch поля — SSH port/user/relative key path — не создают PID;
исправление под тем же profile ID позволяет запуск. Config и construction
ошибки оставляют рабочий manual/timer backup. TCP EACCES и unknown stderr
сохраняют backoff; unsupported/identity-file warning с рабочим SOCKS не
становится постоянной причиной даже после crash. Auth и host-key guidance
проверяются отдельно, включая IPv6 auth prefix и security host-key warning;
прежние37/312 условия сохранены.

Локальные whitespace/public PASS:257 sources/0 release files. Windows CLR,
реальный Windows ssh.exe и весь новый suite локально не запускались:
подходящего runtime в рабочем Linux контейнере нет. Нужен свежий native CI.

Primary sources проверены независимо: upstream OpenSSH
[readconf.c V_9_6_P1](https://github.com/openssh/openssh-portable/blob/V_9_6_P1/readconf.c),
[ssh.c V_9_6_P1](https://github.com/openssh/openssh-portable/blob/V_9_6_P1/ssh.c),
[sshconnect2.c](https://github.com/openssh/openssh-portable/blob/master/sshconnect2.c).
Warning/fatal различаются по source branches; native runner подтвердит
поддерживаемые сообщения фактического Windows client. Это ограниченный
runtime classifier, без обещания распознавать каждый неизвестный stderr
будущих или локализованных клиентов.

Source/tests/независимое review и полный completed Windows/Ubuntu CI нужны
до нового PASS. Package PENDING, Live NOT_CHECKED; устройства владельца,
его установленное приложение и VPS не проверяются. PR draft/unmerged;
merge/release/реальные платежи, письма и сертификаты не выполняются.
