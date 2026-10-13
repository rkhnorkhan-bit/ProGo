# Остаток 55: F17-R02 / F17-R03 — кнопки и PIN-инструкция

Первый небольшой пакет после начала счётчика **0/55**: теперь **2/55 PASS**,
осталось53 подпункта (41 до установщика +12 зависимых приёмок). Общий прогресс
остаётся **23/31**; весь F17 этим пакетом не закрывается. Основание:
[REMAINING_SUBTASKS_8.md](REMAINING_SUBTASKS_8.md), analysis head
`2bbcb6185c8ad8397da21a34caa231054053ccdd`.

## Границы изменений

- F17-R02: читаемые подписи отключённых действий в тёмной теме и визуально
  недоступная primary-кнопка при pending. Проверить переходы Enabled,
  возврат темы и фактический native bitmap; одного ForeColor недостаточно.
- F17-R03: инструкция о четырёх цифрах показывается в одном месте; постоянная
  подсказка описывает скрытый ввод. После исправления format/mismatch ввода
  собственная устаревшая ошибка сбрасывается к инструкции текущего режима.
  Ошибки аутентификации, I/O и срока сохраняют прежние правила.
- Native Button, accessible names, Enter/Escape, Cancel, worker ownership,
  точное сообщение неправильного PIN и четырёхзначная policy сохраняются.
- High Contrast продолжает использовать системную отрисовку. Полная реальная
  High Contrast/Narrator и DPI-приёмка остаются отдельными открытыми задачами.

## Приёмка

1. Независимое scoped source/tests review и whitespace/public checks.
2. Новый completed native Windows/Ubuntu CI на точном functional source.
3. Новые meaningful проверки состояния, bitmap и PIN-инструкции; прежние
   vault/lifecycle и общие suites не ослабляются и не пропускаются.
4. Просмотр свежих native PIN/pending-save/button снимков этого source.
5. Только после полного результата — доказательства в Git и **2/55**.

Текущий статус пакета: **Code/Dev PASS** на новом полном CI #541 и свежих
снимках. Предыдущий CI #539 остаётся историческим доказательством своего source.

## История текущей проверки

- CI #540 (`38470533331`), functional source
  `4b62b49ffa670d689456e04b4ee60845102e61ff`, merge
  `223a145490cad2808ed43cbc421dd04fa48a99e1`: Windows FAIL,
  Ubuntu PASS. Windows остановился на точном сравнении native High Contrast
  bitmap в 2026-10-13 21:32:49.6327572 UTC. Предшествующие 16 новых проверок,
  включая фактический контраст disabled primary/Save и native image fallback,
  прошли; новая PIN-suite и полный Desktop итог ещё не были достигнуты.
- Эталон был standalone Button без родителя; Standard themed renderer может
  брать прозрачный фон рамки у parent. Test-only коррекция задаёт эталону тот
  же Parent/Bounds/default-state и добавляет диагностику расхождений. Проверка
  всех пикселей по-прежнему требует ноль отличий. Production не изменён;
  независимый review APPROVE. Подтверждено свежим полным CI #541 ниже.

## Свежие доказательства CI #541

Functional source `8327054cbba5db8eba6d6f0415f538a36d2a9eb5`, tree
`89a4bba6a372ba88f393f8eebd57ea1c2ca08313`; tested merge
`9aca1b055ab81678fc88ee72154a201b1568f866` в неизменённый main
`4bb336575bb1216d3047bd2b294aeecad9fdd65a`.
[CI #541](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38472084629)
completed **SUCCESS** (2026-10-13 21:55:07 UTC): Windows `115553454771`,
Ubuntu `115553455232` и общий run. Source/tests и независимые native/visual
reviews **APPROVE**; whitespace/public checks PASS.

| Проверка | Фактический результат | UTC 13.10.2026 / граница |
| --- | --- | --- |
| Новая button bitmap/state suite | **27 PASS** | 21:44:53.8223501–21:44:53.9663570; glyph cores >=4.5, Enabled/roles/handler/align/RTL/mnemonic/GDI+ переходы |
| Native image / system reference | **0 /0 отличий** | 21:44:53.8611010 /21:44:53.8865550; весь bitmap включая рамку, одинаковый Parent/Bounds/default state |
| Unlock/PIN UI | **63 PASS =51 прежних +12 новых** | 21:45:09.3525293–21:45:11.5417731; busy edits, доступная ошибка, correction, owner/auth/worker retention, reentrant edit/dispose |
| Reader / policy / record / file / journal / store / recovery / open | **165 /134 /325 /110 /211 /151 /274 /112 PASS** | Полный свежий Windows run; прежние vault suites сохранены |
| Daily expiry / restart | **130 /237 PASS; все12 child stages** | 21:39:38.6092326 /21:40:03.6862037; same-SID processes, injected local days |
| Async save / durable save | **60 /71 PASS** | 21:45:15.2619724–21:45:19.3807550 /21:45:20.2035757–21:45:47.4306309 |
| Active expiry / daily runtime | **31 /70 PASS** | 21:45:47.5369903–21:45:49.6973320 /21:45:49.7944384–21:46:09.8992483 |
| Desktop / Test-ProGo | **3815 /PASS** | 21:47:31.6771652 /21:50:07.7917715; Instance26, Maintenance95, Shutdown47, UpdatePackage5 |
| HomeVPN / portable | **975 /90 PASS** | 21:53:11.6148700 /21:54:53.3247268; cross-SID/owner перенос по-прежнему NOT_CHECKED |
| Windows Python / HTTP | **22 PASS +2 прежних Linux module skips; 7/7 HTTP выполнены** | 24 reported items, итог 21:54:55.3862830; эти Linux modules проверены на Ubuntu |
| Ubuntu operations / sharing+sync / provisioning | **16 /9 /46 PASS, без skips** | 21:38:17.0695125 /21:38:24.6801601 /21:39:51.2800993; numpy2.2.6/zxing-cpp2.3.0, independent QR decode выполнен |
| Native public scan / CI payload ZIP | **254 sources /44 release files PASS; 518153 bytes** | 21:54:56.1372952 /21:54:59.7827688; inner ZIP, не размер artifact wrapper |

Expected negative maintenance `installed=broken-fixture` error в
21:49:38.1796666 UTC сопровождается проверкой отката и Maintenance95 PASS;
это не отказ кандидата. Native OpenSSH `ssh.exe -G` прошёл за85ms при
неизменённом deadline7000ms в21:46:48.9986776 UTC. Assertions, deadlines и
watchdogs не ослаблены; новый vault/UI код не пропущен.

## Снимки и границы приёмки

Свежий [screenshot artifact `11852188348`](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38472084629/artifacts/11852188348)
содержит287 файлов. SHA256 скачанного ZIP совпал с опубликованным digest:
`34662c518776a0d494958faa6fd3cf53c680ae62cf74ade0b13562819ea60cc6`.
Root просмотрел9 выбранных PNG; независимые UI/PIN reviewers подтвердили
свои5 снимков каждый (pending unlock пересекается):

- `desktop-shots/contrast-disabled-dark-actions.png`,
  `contrast-disabled-system-actions.png`, `contrast-disabled-dark-restored.png`:
  читаемые текстовые disabled подписи, native system drawing и возврат темы.
- `desktop-shots/vault-unlock-pending.png`, `vault-save-pending.png`:
  subdued disabled действия читаемы, Cancel/lock/close остаются различимы.
- `desktop-shots/vault-unlock-create-invalid.png`, `vault-unlock-create.png`:
  одна точная форматная ошибка либо одна инструкция; после correction
  устаревшей ошибки и повторного правила нет, PIN masked.
- `desktop-shots/vault-unlock-wrong-pin.png`, `vault-unlock-unavailable.png`:
  прежняя auth/I/O ошибка сохранена, текст и retry action помещаются.

Image/ImageList кнопки сохраняют native fallback; его точное совпадение
подтверждает layout/render preservation, а не новую гарантию читаемости
подписи с изображением. System-палитра была injected: реальная Windows
High Contrast/Narrator/DPI и приёмка всех форм остаются открытыми F17/F16.

Дополнительно загружены diagnostics `11853735875`, runtime folder `11853097522`
и ZIP artifact `11853012578`. SHA256 ZIP artifact wrapper:
`c57385145642dc0458e3b6af9f368bc79ed3650be2ef358407bd8e880146b65e`.
Это CI артефакты текущего source, а не цельный установщик всех31.

Установщик всех31, пользовательская установка, проверка устройств владельца,
merge и release этим пакетом не выполняются.
