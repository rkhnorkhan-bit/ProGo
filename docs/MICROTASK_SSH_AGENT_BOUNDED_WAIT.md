# SSH-agent PGA-02B: bounded waiting и владение системным запросом

Baseline `618fd8e203159760c8d3274b7208ba6c3e3f808f`, VERSION0.2.2.
16.10.2026 UTC, следующая разрешённая владельцем микропорция.
[Контракт](SSH_AGENT_AUTOSTART_SPEC.md). Самостоятельный source review и native CI558 выполнены: узкий PGA-02B
**Code/Dev PASS**. Это часть существующего F31-R11, не новый ID55.

## Поведение и границы

Один process-wide `SshAgentServiceProbe.Shared` допускает только один SCM read
вместе с его cleanup. Отмена/3s deadline завершают ожидание вызывающего кода,
а не принудительно выполняющийся Windows P/Invoke. Cooperative token передаётся
прежнему typed reader; после возврата OS вызова он прекращает последующие queries
и освобождает собственные handles/buffer. Slot остаётся занятым до завершения
raw worker Task. Повтор и другое диагностическое окно получают Busy и не создают
дополнительный native worker. Завершившийся старый snapshot не кешируется для
нового запроса; после полного settlement выполняется свежий read.

Отдельный background worker с Default scheduler не зависит от UI context.
Timer и cancellation registration принадлежат waiter и освобождаются после
ожидания. Native CTS принадлежит worker и освобождается только после его cleanup.
Непредвиденная ошибка/отсутствующий snapshot дают безопасный Unavailable без
raw exception/выдуманного Win32-кода. Результаты Completed/Busy/TimedOut/
Unavailable отделены от фактического runtime/startup snapshot PGA-02A.

`SshProfileDiagnostics.Check` использует общий probe (3s default). При отсутствии
подтверждённого snapshot сохраняет результат выбранного SSH config и откладывает
inventory ключей; это не запрещает другие рабочие методы SSH-аутентификации.
Shared argv/native config parser/подключение/fallback/SOCKS не меняются.
Старый text Read wrapper тоже использует общий bounded probe.

Окно проверки выполняет Check в worker, публикует результат только своему
живому UI owner. Close/Dispose завершают ожидание UI delivery без живого message
pump после фактического settlement CheckWork. Сам raw SCM может ещё выполняться
и остаётся в общем probe. Cancel ДО queued publication подавляет готовый результат;
CTS формы освобождается после завершения CheckWork и публикации либо закрытия
владельца; закрытие не освобождает CTS до возврата CheckWork. UI pump для cleanup
закрытого владельца не требуется. По окончании старого Run нельзя очистить CTS нового retry.
Текст различает лимиты SSH7s, ожидания службы3s и key inventory3s; физическая
остановка принадлежащих диагностике процессов имеет прежний отдельный budget.

Это read-only диагностика. Startup hook и попытка запуска службы по имеющимся
правам относятся к следующему PGA-03. Matching нужного ключа, interactive
ssh-add, новые agent UI действия и installer opt-in ещё впереди. Служба CI
читается read-only; её policy/ключи/ACL не меняются. Проверки на owner PC/VPS/
телефоне не выполняются.
Не заявляется real-time гарантия при остановке планировщика/всего процесса ОС.

## Проверки

Новый Desktop partial использует настоящий typed reader и прежнюю native
boundary fixture; в нужном месте удерживается QueryStatus. CI558 подтвердил32 checks:
валидация budget/pre-cancel, actual typed result/cleanup/background/context,
безопасный failure/null, timeout с raw pending,12 concurrent Busy, cooperative
cleanup и fresh retry, отмена без потери ownership, независимость Busy caller,
UI heartbeat/timeout/два окна/retry, queued cancellation, Close и Dispose без
UI pump, отсутствие поздней публикации. Два новых PNG показывают timeout/Busy.
Native oracle PGA-02A и все старые проверки/budgets сохранены. Check/compiler
получают по одной явной вставке. Desktop4060=4028+32 подтверждён фактическим CI558 PASS. Локального Windows runtime здесь нет; native результат
принимается только на изолированном CI, без действий на устройствах владельца.

Исходные55 IDs=43 до установщика+12 зависимых приёмок сохранены. Приняты только
F17-R02/R03 и F31-R01/R02/R03/R04: **6/55**, остаток49=37+12, общий23/31.
F31/F17 OPEN, F31-R05 PARTIAL; Codex ownership/disabled caption/неизвестный
ssh-G отказ CI556 остаются открытыми findings. Package PENDING, Live NOT_CHECKED.
PR draft/unmerged; merge/release не выполняются.

## Review перед native CI

Самостоятельно проверены: отсутствие mutation API; слот удерживается до raw Task
settlement, включая reader finally; waiter cancel не смешивается с native return;
failed/null worker не оставляет fault; native CTS не освобождается waiter;
Default scheduler/ConfigureAwait(false); cancellation registration/timer cleanup;
нет повторной выдачи abandoned snapshot; CTS нового Retry не очищается старым Run.
UI publication выполняется через BeginInvoke с owner gate и повторной проверкой
токена на UI; закрытие не зависит от message pump, но ждёт реального CheckWork.
Независимый review другим агентом в этой микропорции не выполнялся.

Локально public273/0 и whitespace PASS; inventory55unique=43+12/exact6accepted
сохранён. Проверка main/compiler после удаления по одной новой вставке даёт
полную эквивалентность baseline. Production SCM adapter, config parser,
DiagnosticProcess, ProxyService, Program, installer и все прежние test files
(кроме одной wiring строки main) не меняются.

## Frozen candidate

Source `c045b6f199fc6360a99c29f79954e76ed013cf8d`, tree
`ff79158a863fd2d761d82f9751e8500852a26f75`, parent baseline618fd8e.
Локальный эквивалент `13ca86b99404a8b5685cadea242f68ef3573986a` имеет то же tree.
PR-triggered merge `f9cd955abf97511a08ebc3972b7389d1ca180cac` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #558](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38897865756)
completed **SUCCESS**, весь run и оба jobs: Windows116962589654, Ubuntu116962590018.

| Файл | Frozen Git blob |
| --- | --- |
| `src/SshAgentServiceProbe.cs` | `17657d5c333dcc43e48a854ee95d697a9038dbd9` |
| `src/SshDiagnosticForm.cs` | `7d34350cf47af65778256a8e562b810c43fffd97` |
| `src/SshProfileDiagnostics.cs` | `d87a370b879001a15ac4f43fcc50657e80505821` |
| `tests/DesktopSshAgentProbeTests.cs` | `5f187327a7fd19fff47088518461be80bdaaddca` |
| `tests/DesktopTests.cs` | `f1102eefea1bfbb09df73643b25e2ccac48b2461` |
| `scripts/Test-ProGo.ps1` | `0594d3afe7df4252398bb7e0c20ad4d144aff633` |

### Ubuntu558

Job116962590018 completed SUCCESS:16 operation recovery,9 sharing/sync,
46 isolated provision checks PASS,0 skips. Actual independent QR decoder и
recovery/revoke/reissue/HTTPS/session tail достигнуты. Checkout подтверждает
source/merge/base выше. Полный UTF-8/LF log52855 bytes/573 lines, SHA-256
`2a65b5338a12603bb3090e4d03cb0c98c9c049bf65027e0af0cfde667e501dda`.
Windows и общий run также completed SUCCESS; фактические результаты ниже.

Уточнение статического счёта:32 новых assertions, не33. Первоначальная простая
строковая оценка включила вызов `SshProfileDiagnostics.Check` самого subject.
Точных direct Check sites28, дополнительно2 выполнения из трёх budget cases
и2 из второго Close/Dispose прохода:28+2+2=32. Код и весь набор сценариев
не менялись; предварительная оценка4060 сама по себе не была PASS, фактический
Desktop4060 PASS подтверждён ниже.

## Фактическая приёмка558

Новые32 Check PASS12:12:41.2655680–12:12:43.9897963 UTC. В частности,
12 concurrent retries не создали второй native reader; таймаут/отмена оставили
raw ownership до cleanup; fresh retry вернул новый typed snapshot. Обе формы
Close и Dispose завершили CheckWork/Work без UI pump, пока SCM ещё удерживался;
последующий return освободил native ресурсы без поздних UI изменений. Queued
Cancel подавил уже готовый результат. Это controlled native boundary fixture
внутри настоящего typed reader/WinForms, не зависание службы владельца.

Desktop **4060=4028+32 PASS**12:13:18.2481037 UTC. Прежние37 SCM checks,
independent read-only native oracle (Stopped1/Disabled4/errors0/0), SOCKS41/403,
Instance28, Maintenance95, Shutdown47, updater package5 и весь Test-ProGo PASS.
HomeVPN975/portable90, Windows Python22 PASS+2 прежних Linux-only module skips,
все7 HTTP share cases и public273 source/44 release PASS. Ubuntu16/9/46 PASS
без skips с actual independent QR и полным isolated provisioning tail.
Native `fixture.dotted` oracle PASS12:12:44.0206375; failure-only INFO отсутствует,
старый отказ556 не объяснён задним числом и остаётся открытым finding R11.
Cross-SID/другойПК, полный package/live и остальные исходные gates не закрыты.

Полный Windows UTF-8/LF log925136 bytes/8538 lines, SHA-256
`0bcde2f22ae8998cf0f952d2963e5bcb2e6002ae7c9d85d2db35e87ddbce219a`.
Полный Ubuntu UTF-8/LF log52855 bytes/573 lines, SHA-256 указан выше. Оба checkout
подтверждают source c045b6f1, merge f9cd955a и неизменный base4bb33657.
Code и результат просмотрены самостоятельно; независимого agent review в этой
микропорции не было. Старые tests/budgets не ослаблены, повторный CI не понадобился.
Число assertions33 было только ошибочной первоначальной оценкой, не результатом.

| Артефакт558 | ID | Bytes | SHA-256 |
| --- | --- | ---: | --- |
| Screenshots | 12045303252 | 6536706 | `bde0f6c3e1ae826b240cfbb7b5c761501c97d3037293373ec35aee31df17bf44` |
| Diagnostics | 12045178588 | 87157 | `a3a349c1834f6d7c691b920ba84e41a3e16aff4d5b000d35f93376ec5e72b05f` |
| CI runtime folder | 12045547060 | 536472 | `264a96387e3bca2023eabce0fae07e6467beb063961d048e1975027f41fa98de` |
| CI ZIP wrapper | 12045731514 | 528113 | `d4b2b2a05c3e1107b3caa82391a42a5897e557022b42de9d2fefaedd9f8f47fb` |

Все4 metadata совпали с runner ID/size/digest и head c045b6f1. Screenshot ZIP
скачан:6536706 bytes/293 entries, SHA-256 пересчитан. Просмотрены3 свежих PNG:
`ssh-agent-service-timeout`, `ssh-agent-service-busy`, `ssh-diagnostic-pending`.
Timeout/Busy читаемы в показанном размере, lower report доступен вертикальной
прокруткой; Retry/Close и pending Cancel доступны. Короткий текст лимитов помещается
в pending status. Другие290 PNG и полный DPI/contrast/Narrator не приняты.
Остальные3 artifacts отдельно не скачивались; подтверждены metadata/runner.
Inner CI ZIP531557 bytes; это не цельный installer всех31.

PGA-02B принят только в описанной read-only границе. Startup hook/Start с имеющимися
правами — следующий PGA-03; selected-key readiness, interactive ssh-add, agent UI
и installer остаются впереди. Весь PGA-02 ещё PARTIAL до startup интеграции,
F31-R11 OPEN. **6/55**,49=37+12,23/31, Package PENDING, Live NOT_CHECKED сохранены.
Owner PC/VPS/телефон, служба/policy/ключи/ACL не менялись, merge/release не было.
После tested source c045b6f1 публикуются только4 Markdown evidence/status файла;
runtime/tests/scripts/server/workflow идентичны проверенному source.
