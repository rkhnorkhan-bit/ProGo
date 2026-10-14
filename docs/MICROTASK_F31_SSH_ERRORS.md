# Остаток55: F31-R03 — постоянные ошибки SSH config и параметров

Один ограниченный Code/Dev результат после F31-R02. Исходный checkpoint
`7fd24324ac39a790cc0aca808b25def49f33d21d`; ранее принятые4/55 подтверждены
полным CI #547. F31-R03 принят **14.10.2026 UTC** в новом полном CI #548:
счёт **5/55**, осталось50=38 до установщика +12 зависимых приёмок;
общий **23/31**, весь F31 **OPEN**.
[Список55](REMAINING_SUBTASKS_8.md).

## Подтверждённые пробелы и граница

На исходном checkpoint до этой коррекции:

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

Статус **Code/Dev PASS**: source/tests FROZEN; независимые source/tests,
PENDING документа и полных native результатов reviews APPROVE.
ProxyService +38/−8, tests +122;
прежние37/312 условия сохранены;
прочие application/tests/scripts/server/workflow файлы не менялись.
Фактически **41 scenarios/403 assertions PASS**: прежние37/312 +новые4 groups/91
(30/15/18/28). Desktop3851 PASS; его условия не менялись.

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
подходящего runtime в рабочем Linux контейнере нет. Свежий native proof ниже.

Primary sources проверены независимо: upstream OpenSSH
[readconf.c V_9_6_P1](https://github.com/openssh/openssh-portable/blob/V_9_6_P1/readconf.c),
[ssh.c V_9_6_P1](https://github.com/openssh/openssh-portable/blob/V_9_6_P1/ssh.c),
[sshconnect2.c](https://github.com/openssh/openssh-portable/blob/master/sshconnect2.c).
Warning/fatal различаются по source branches; native runner подтвердил
поддерживаемые сообщения фактического Windows client. Это ограниченный
runtime classifier, без обещания распознавать каждый неизвестный stderr
будущих или локализованных клиентов.

Package PENDING, Live NOT_CHECKED; устройства владельца,
его установленное приложение и VPS не проверяются. PR draft/unmerged;
merge/release/реальные платежи, письма и сертификаты не выполняются.

## Свежая полная приёмка — CI #548

Functional source `f33104ca16e8afc9e6f10025b3988c3a3cb82efa`, tree
`95f76be15751ece95fb2214d5b1d83f96fc1d991`; tested merge
`14c55c2e9849037cc35da84e3e3d5341ce700d8b` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #548](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38608522959)
completed SUCCESS: Windows `116005556356`, Ubuntu `116005556155`, весь run.
Root и независимые reviewers сверили полные завершённые логи; это новый proof
текущего source, а не перенос PASS из предыдущего checkpoint.

| Новый результат | Assertions | Завершение UTC |
| --- | ---: | --- |
| Пять actual native -G parser errors; redirected stderr replay, fixed guidance/pause/redaction и исправленный same-alias retry | 30 | 17:43:54.7645001 |
| Три prelaunch invalid structured fields без PID, исправление под тем же ID→SOCKS | 15 | 17:44:02.6386937 |
| Config/prelaunch failure→manual/timer backup с launch journal и настоящим greeting | 18 | 17:44:13.3616291 |
| TCP/unknown retries, nonfatal warnings/recovery и отдельные auth/host-key guides, IPv6 и security warning | 28 | 17:44:33.4067297 |
| **Итого новых** | **91** | |

SOCKS **41/403 PASS17:44:33.4072760–4073528 UTC**. Первые37 groups суммарно312,
их условия и бюджеты сохранены. Group30 PASS подтверждает все пять реальных
parser iterations, включая direct -o без обязательного fatalaggregate;
сами успешные retry используют controlled native child и настоящий SOCKS,
не аутентификацию на внешнем сервере. Содержимое ключей/owner config не читалось.

| Остальной свежий proof | Результат |
| --- | --- |
| Vault read/policy/record/file/journal/store/recovery/open | 165/134/325/110/211/151/274/112 PASS |
| Daily expiry/restart | 130/237 PASS |
| Desktop/полный ProGo | 3851/весь PASS, 17:51:22.8214329 /17:53:54.2335287 UTC |
| Instance/Maintenance/Shutdown/update package | 26/95/47/5 PASS |
| HomeVPN/portable | 975/90 PASS, 17:57:10.4830728 /17:58:39.3622444 UTC |
| Windows Python/HTTP | 22 PASS +2 прежних Linux ownership/flock module skips; все7 HTTP groups выполнены |
| Ubuntu operations/sharing/provisioning | 16/9/46 PASS без skips; actual independent QR decode17:39:47.1388048 UTC |
| Native public scan/runtime ZIP | 257 sources/44 release files PASS; inner ZIP521799 bytes |

Предыдущие9 Desktop borrowed/preprobe checks снова PASS. Server и46 security
assertions не менялись. APT реально обновил openssh-server13.19→13.20;
managed sshd-t, restricted listener/revoke/reissue/private recovery/real TLS
PASS. Прямая причина package service stop по-прежнему inference из предыдущей
истории; нового systemd-state snapshot нет. Проверки устройств владельца
этим изолированным CI не заменяются.

Artifacts uploaded: screenshots `11918505479` (287 files/6382878 bytes),
diagnostics `11918775483` (81659 bytes), runtime folder `11917556050`
(44 files/525533 bytes), ZIP artifact `11917590940` (518480 bytes).
Inner ZIP521799 отличается от compressed artifact wrapper518480 bytes.
Metadata SHA256 ZIP artifact:
`8aade579abec2d56ea4487a2b0a727e3addb8468c6c6f7e76a5ae7ff04285a43`.
Metadata/runner upload digests совпадают. Отдельные download/hash и новая
визуальная приёмка287 PNG не заявлены; product layout/palette не менялись.
Это CI runtime artifact, а не цельный setup/updater всех31.

Принят только новый F31-R03: **5/55**, осталось50=38+12. Исходные55=43+12,
общий23/31 и целые F17/F31 OPEN сохраняются. После native source меняются
ровно три Markdown evidence-документа; application/tests/scripts/server/
workflow идентичны проверенному source. Независимый review ровно3 evidence
документов APPROVE;55 уникальных ID и5 PASS перепроверены отдельно.
Whitespace/public PASS; functional source после native приёмки не менялся.
