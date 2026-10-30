# F31-R11: проверка подписи выбранной identity (PGA-06E)

Статус: PARTIAL; source review выполнен, новый native Windows CI NOT_RUN.
Это два связанных микрошагa: signing backend и явное действие диагностики.
F31-R11/PGA-05/06/08 остаются открытыми. Новый пункт в55 не добавляется.

## Контракт

`SshKeyReadiness.Check` обрабатывает одну identity из effective результата
OpenSSH. `ssh-add -T` выполняет sign+verify публичной reference, которую выбрал
OpenSSH (включая законный public-only IdentityFile либо соседний.pub).
AgentSigningVerified означает только успешную локальную подпись этой reference.
Пригодность private bytes, доверие host key и авторизация на VPS не доказаны.
Тест с непригодным primary и действующим.pub специально сохраняет эту границу.

Caller передаёт свежую effective конфигурацию. Политика publickey/IdentityAgent,
selected identity/path и sibling executable проверяются отдельно. Other auth,
прямые незашифрованные ключи и custom agent не получают запрета SSH из-за
Unknown/NotUsed/Unsupported signing result. Automatic authentication gate
этим backend не вводится; интеграция полной подготовки способа входа до каждого
SSH остаётся отдельным незакрытым критерием.

Проверка держит общий lease SCM/inventory/key-add до фактического завершения
owned DiagnosticProcess. Снимки primary+sidecar ограничены размером и сверяются
до/после child; reparse/file-access failures не превращаются в Missing.
Profile/endpoint/cancellation/deadline повторно проверяются после post-read I/O
непосредственно перед успешным результатом. Результат — наблюдение конкретной
попытки, не постоянная гарантия; hostile same-user filesystem race не исключается.

Новый DiagnosticProcess.RunReadOnly использует тот же suspended/job-contained
owner, фиксирует child SSH_AUTH_SOCK и Unicode environment. ASKPASS/DISPLAY
не наследуются, SSH_ASKPASS_REQUIRE=never. Толькоnull означает default;
пустой endpoint отклоняется. Старый Run не меняет поведения. Парольная фраза
не запрашивается/не передаётся/не сохраняется; private bytes/digest не логируются.

## Подготовленные проверки

- Настоящий sibling ssh-add и изолированный test-owned RSA named pipe agent:
  валидная/повреждённая подпись, отказ агента, другой ключ, публичный sidecar.
- Реальные child timeout/cancellation и общий lease; forced askpass не исполняется.
- Unicode frozen endpoint через реальный child, parent environment неизменён;
  legacy Run inheritance отдельно сохранено.
- Missing/locked/oversize/reparse, malformed/truncated child output,
  changed profile/primary/sidecar/endpoint, late deadline, pre-cancel и busy.
- Два детерминированных post-read mutation вектора воспроизводят найденный
  независимым review freshness gap; финальный guard обязателен.
- Native WinForms UI сценарии добавляются отдельно: single selection/freshness,
  heartbeat, cancel/Close/no late publication, keyboard и minimum bounds.
  Созданные PNG без визуального просмотра не являются visual PASS.

В ходе независимого source review исправлены post-read freshness gap,
пустой endpoint contract и кодировка Unicode output helper (явный UTF8 без BOM).
Локальные content/diff/registration checks не подменяют native execution.
В текущей Linux-среде compiler/WinForms/SCM нет; итоговые counts/точные SHA/CI
записываются только после completed run. Прежний CI583 проверяет предыдущий
sourcee9fcfc3 и не подтверждает этот новый backend/UI.

## Источники и границы

Первичные источники прочитаны21.10.2026:
[OpenBSD ssh-add manual](https://man.openbsd.org/ssh-add), опция-T;
[OpenSSH portable ssh-add.c](https://github.com/openssh/openssh-portable/blob/master/ssh-add.c),
`test_key`: публичная reference → ssh_agent_sign → sshkey_verify.
Текущий upstreammaster — объяснение выбранного механизма, не доказательство
версии Windows OpenSSH на ПК владельца. Native CI oracle и live приёмка отдельно.

Отсутствуют live SSH/VPS/key/service changes, elevation и изменения ACL.
Публичный CI upload выключен; способ передачи бинарного кандидата ещё не согласован.
