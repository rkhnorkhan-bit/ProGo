# PGA-05B — наблюдаемое сопоставление публичных identity профиля

**17.10.2026 UTC. Узкий PGA-05B Code/Dev PASS по completed CI563.**
Родительский checkpoint `52ec8043f71e2f5b598a7ec33265bb3c469d7056`
(PGA-05A/CI561). Этот микрошаг относится к существующему F31-R11.
Счёт **6/55**, остаток49=37+12 и общий23/31 не меняются.

## Область и границы результата

Для каждого effective IdentityFile из разрешённого OpenSSH-контекста получаем
публичный fingerprint и сравниваем его с immutable inventory PGA-05A.
Результат хранит отдельные Present/Absent/Unknown, состояния ошибки и provenance
явного fallback; завершение фазы Completed не означает Ready.

**Это публичная reference, не связь с закрытым ключом и не проверка подписи.**
Нативный `ssh-keygen -l` может предпочесть соседний `.pub`, даже устаревший.
Совпадение такого отпечатка не доказывает владение исходным private key,
разрешение подписи, пригодность сертификата/FIDO/constraints или вход на сервер.
Text inventory также не является криптографическим possession proof; прежняя
граница comments/multiline output PGA-05A сохраняется. Ready/auth gate отсутствует.

Если основной вызов завершился nonzero, разрешён один явный запрос к `path.pub`.
Его metadata всегда **Unknown** относительно агента, даже при совпавшем fingerprint:
исходный файл/ACL не подтверждены, связь sidecar с ним не установлена.
PrimaryExitCode и SidecarFallback сохранены. Nonzero ssh-keygen не считается
«агент недоступен», пустым агентом или доказанным отсутствием выбранного ключа.

## Реализация

- Используются effective IdentityFiles и подтверждённая IdentityPaths map из R04.
  Не угадываются home/env/tokens, относительные/device пути. `none` не запускает
  child. Список ограничен32, как native config.
- Только `ssh-keygen.exe` рядом с выбранным ssh.exe; PATH-подмена отсутствует.
  Production argv только `-l -E sha256 -f <quoted confirmed path>`.
  Нет `-y`, passphrase argv/env, key generation/import/signing или подключения.
  Внешний keygen может читать файл ключа; приложение не разбирает его private bytes.
- Один общий3000ms бюджет фазы на все identities/fallback, уменьшающийся перед
  каждым child. Поздний output отбрасывается. Cleanup прежнего JobObject runner
  отдельно ограничен его протоколом; мгновенная физическая остановка не обещается.
- Общий process-wide read-only lease для inventory либо всей fingerprint фазы.
  Concurrent caller получает Busy; cancel не освобождает lease раньше return/throw
  owned runner с cleanup. SCM и будущий mutating ssh-add coordinator — отдельные работы.
- Identity/path/ssh/agent-policy/inventory snapshots берутся до child. Exact Ordinal
  path cache исключает повторный child для точно одинакового пути, сохраняя отдельные
  observations для путей, различающихся регистром.
- Прежний bounded canonical inventory parser: partial/truncated/oversized/
  unexpected stderr/invalid output не подтверждает fingerprint. Несколько native
  строк, даже одинаковых, дают Ambiguous вместо выбора первой.
- Explicit IdentityAgent none/custom endpoint не использует default inventory.
  Metadata файла доступна для других способов входа. Unavailable inventory даёт
  Unknown и не блокирует прямой ключ/другие auth методы.
- Report выводит safe descriptions и границу публичной identity; fingerprints,
  comments, raw stdout/stderr/exception не выводятся. Typed result immutable.
- Полный Check допускает fixture configurationFile, ранее доступный configuration
  фазе. Production calls прежние. Program/ProxyService/startup/auth arguments/SCM
  операции не менялись. Pending caption перечисляет отдельные бюджеты.

## Матрица dev-проверок

Новый Desktop partial включён в прежний compile/workflow. Старые watchdog,
phase budgets и проверки не ослаблены; inventory fixture только принимает
опциональный public blob для полной native pipeline.

Boundary: exact sibling/argv, несколько identity/unrelated inventory, immutable
list/safe report, invalid budgets/pre-cancel/unresolved/none/unconfirmed paths/
missing sibling, explicit agent scope/unavailable inventory, unbound fallback,
8 unsafe result cases, exact cache/case-distinct paths, cap32/frozen inputs,
shrinking deadline/late output/no child after deadline, shared Busy/cancel/double
Dispose/reuse, WinForms heartbeat/close без ожидания cleanup/late reopening.

Native Windows: настоящий selected ssh `-G -F` с fixture-only alias config,
реальный sibling ssh-add через test-owned public-only named pipe и реальный sibling
keygen для двух временных test-only Ed25519 identities. SCM Running здесь —
**injected boundary fixture**, не запуск или изменение системной службы.
Expected SHA256 вычисляется отдельно из public wire bytes, не production parser.
Проверяются stale sidecar/native precedence, неизменный private file hash,
отсутствующий sidecar/public envelope, несколько public keys и отсутствующий
основной файл с отдельно помеченным unbound fallback.

Synthetic OpenSSH encrypted envelope содержит public blob и фиктивное encrypted
body, **без пригодного private key**. Разрешён только точный публичный fingerprint
либо явный native отказ/неподтверждённый ответ со stderr и Unknown без fingerprint.
Timeout/exception/wrong fingerprint не принимаются. Отдельный corrupt public blob
обязан дать CommandFailed/Unknown. Encrypted-key unlock и usable signing не проверены.
ACL только этого test-owned envelope копируется с keygen-created test fixture.
Forced SSH_ASKPASS spy обязан остаться невызванным. Env восстанавливается,
fixtures удаляются. Production не принимает passphrase через argv или env.

Локальный Linux `ssh-keygen -l -E sha256 -f` успешно прочитал synthetic envelope;
fingerprint совпал с независимо вычисленным SHA256. После ограничения permissions
только временного fixture stderr пуст. Windows/.NET здесь отсутствует,
native приёмка подтверждена CI563 ниже. Самостоятельный source/native review;
независимого agent review не было.

## Открытые gates

Весь PGA-05/06/F31-R11/F31/F17 OPEN. Предстоят private-key binding/signing/
constraints/certificates, custom endpoint propagation, interactive add/unlock,
ACL причины отказа, auth/startup coordinator и раздельные UI действия.
Package PENDING, Live NOT_CHECKED, VERSION0.2.2; draft PR88 без merge/release.
ПК/VPS/телефон/службы/реальные ключи/ACL/платежи владельца не проверялись и не менялись.
Codex shortcut ownership, disabled caption и причина исторического ssh-G556
остаются открытыми findings. Принят только узкий scope по CI563 ниже.

## История нового fixture: CI562 FAIL

Source `6f8bf867d3f853300a8c8ac2a3421e569a9c9d4d`, tree
`3736a518a1217c1aef2cc6b8ed2ad1762d108a51`, merge
`f4df1dc897b7ceffd5a96a125b35e29b599277d9` в прежний main.
[CI562](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39022774570):
Windows117367726682 FAILURE, Ubuntu117367726812 SUCCESS.
Новый block дошёл до47 PASS;10:04:41.1343098UTC failed assertion требовал
успешную metadata extraction из synthetic unusable encrypted body. Нативные
matching/stale sidecar/private hash/absent sidecar перед ним PASS; весь Desktop,
Test-ProGo и поздние Windows suites не завершились. На CI562 микрошаг не принят,
счётчики6/55 и23/31 не увеличены.

Ошибка нового positive fixture: обязательное принятие неполноценного private
container не является корректным межверсионным контрактом. Исправлен только
test/protocol: exact public metadata либо явный safe Unknown; добавлен отдельный
обязательный отказ для повреждённого public blob. Production parser/state/deadline/
guards не ослаблены. Изначальный captured state/stderr не логировался, поэтому
конкретная native причина исходного отказа UNKNOWN; не объявляем её ACL/version
или расшифровкой. Следующий CI печатает только state/exit-count/stderr-present,
без raw error/path/fingerprint. Старый FAIL сохранён; результат следующего
полного прогона CI563 приведён отдельно ниже.

Decoded UTF-8/LF logs с одной final newline: Windows6800lines/739700bytes,
SHA256 `a66c3084758c424964161e9a6e7510d3dcf79a1a0133acbfe410a523022a963b`;
Ubuntu573lines/56182bytes,
SHA256 `2ba271a134f8677bfc8a8d22654e9b7544e88dd0b11f5d4897292e39c824462f`.
Ubuntu16 recovery/9 sharing/46 provisioning, без skips, independent QR и полный
TLS/recovery/session tail PASS. Эти результаты не подменяют Windows acceptance.

## Native Code/Dev приёмка: CI563 SUCCESS

Tested source `6278300b98f61f4ff0688ed7dda110cfafca2d78`, tree
`f4508bbaa443fc4c17c49159c53c7a446c2e9931`, merge
`097800882928b089f0b5a062dc4f8537aedaccf9` в неизменный main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI563](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/39023758270)
completed SUCCESS: Windows117370576458, Ubuntu117370576602 и весь run.
Оба checkout log подтверждают этот merge/source/base; main — ancestor source.
Это новый полный прогон после коррекции fixture, не замена истории CI562.

Новый block BEGIN10:20:32.2685214UTC;52 фактических PASS до marker
`SSH profile key matching checks PASS: 52`10:20:32.9743256UTC.
Desktop **4284=4232+52 PASS**10:21:16.2409518UTC.
Full native fixture pipeline10:20:32.6475850, independent SHA256/match
10:20:32.6477389, stale sidecar10:20:32.7065304, private hash unchanged
10:20:32.7066664, absent sidecar10:20:32.7672486UTC.

Synthetic envelope10:20:32.8006214UTC: **InvalidOutput, attempts1, exit0,
stderrPresent=True**. Production сохранил Unknown без fingerprint; это проверенный
отказ от неполного подтверждения, а не успех encrypted private key/decrypt/signing.
Raw stderr не выводился; его причина и причина первого CI562 UNKNOWN.
Corrupt public blob10:20:32.8599842, real multi-public10:20:32.8879662,
unbound sidecar10:20:32.9730084 и forced askpass untouched10:20:32.9731322 PASS.
Имитируемая SCM Running — boundary fixture. Реальны ssh-G/keygen/ssh-add/IPC,
временные unencrypted test keys и public SHA256; реальной службы/signing/VPS нет.

Прежние37 SCM/32 bounded-probe и66 start/40 startup/66 inventory checks сохранены.
Actual read-only SCM oracle10:20:24.6541767UTC: runtime1/Stopped,
startup4/Disabled,statusError0/configError0. Старые66/40/66 markers —
10:20:29.8868051/10:20:32.0156594/10:20:32.2680105UTC.
SOCKS41/403 PASS10:13:33.1243310; Instance28 PASS10:21:23.7123237;
Maintenance95 PASS10:23:30.1244525; Shutdown47 PASS10:23:59.6056178;
updater5 PASS10:24:01.4566483; весь ProGo tests PASS10:24:01.4573439UTC.
Исторический native fixture.dotted PASS10:20:33.0129077; это не объясняет CI556.

HomeVPN975 PASS10:27:17.2594601, portable90 PASS10:29:09.3528729;
Windows Python24=22PASS+2 прежних Linux-only skips, OK10:29:11.3562112.
Все7 HTTP profile-share cases ...ok. Public286source/44release PASS10:29:12.1713201.
Ubuntu16 recovery/9 sharing/sync PASS,46 provisioning без skips,
actual numpy2.2.6/zxing-cpp2.3.0 independent QR; полный recovery/revoke/reissue/
TLS/session tail дошёл до restart/session PASS10:10:38.2206739UTC.

Локальный corrupt public envelope также отвергнут Linux keygen exit255 без
fingerprint; missing-primary/explicit .pub и multi-public поведение проверены
на временных public-only fixtures. Windows/.NET локально отсутствует, native
приёмка — CI, не устройства владельца.

Functional diff относительно52ec8043:14 файлов,685insertions/23deletions.
После первого source6f8bf867 production src/scripts/workflows не менялись:
коррекция затронула только новый test/protocol/status. После tested source —
только4 Markdown, runtime/tests/scripts/server/workflows идентичны tested source.
Самостоятельный source/native review; независимого agent review не было.

## Логи и CI artifacts

Decoded UTF-8/LF с одной final newline:

| Job | Lines | Bytes | SHA256 |
| --- | --- | --- | --- |
| Windows117370576458 | 8771 | 951572 | `a02e7e2f43364282673c3474b1ca20a1d635eccb475b82fa7d46b5b2e8e6555f` |
| Ubuntu117370576602 | 573 | 53101 | `5fcc3a1b4d580cc4b57cd7731497f3fb881d223fe80769b73c7b3a619214fb6f` |

Все4 artifact workflow_run source head/run ID, upload ID/size/digest сверены
между API и full upload log. Таблица содержит внешний artifact ZIP:

| Artifact | ID | ZIP bytes | SHA256 |
| --- | --- | --- | --- |
| ProGo-ci-diagnostics | 12098366658 | 92215 | `05ee6fe8fad98c724789c4a31bf31da12306420636516e7d0b9f8c0a9bd11f20` |
| ProGo-release | 12098262122 | 543850 | `fe63b9414a12ae5c391d61c5cb4302655f98bf4fc7639c7d02b0603be40b3cf6` |
| ProGo-release-zip | 12098092775 | 535644 | `7bd3905612cc820b81b0081caf00116c9ba85c7a4e6e25b9d29ae5988a021e83` |
| ProGo-wizard-screenshots | 12098072869 | 6541787 | `a184f01250c3c72b419f71d494e6b2b1df26e38e42601935c914f0ffd1d469e1` |

Архивы не скачивались, локальной archive/hash/visual/DPI/contrast/Narrator приёмки
нет. Runner сообщает293 screenshot files; inner ProGo-release.zip539089bytes —
отдельный файл от внешнего artifact ZIP, не итоговый installer31/31.
Package PENDING/Live NOT_CHECKED. Этот узкий Code/Dev PASS не меняет6/55/23/31
и не закрывает PGA-05/06/F31-R11/F31/F17.

## Первичные источники границы metadata

- [OpenBSD ssh-keygen manual](https://man.openbsd.org/ssh-keygen): fingerprint mode
  `-l`, SHA256 `-E`, отдельное извлечение public key `-y`.
- [OpenSSH ssh-keygen.c](https://github.com/openssh/openssh-portable/blob/master/ssh-keygen.c):
  do_fingerprint/fingerprint_private выводят публичные metadata; успешный `-l`
  не является подписью или проверкой входа.
- [OpenSSH authfile.c](https://github.com/openssh/openssh-portable/blob/master/authfile.c):
  sshkey_load_public пробует public file, затем sidecar и public envelope;
  эта очередность не доказывает соответствие sidecar закрытому ключу.
