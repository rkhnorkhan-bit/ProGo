# Микрозадача 15 — защищённый журнал суточной транзакции

## Граница изменения

Добавлены `VaultDailyJournal`, `VaultDailyJournalCodec` и отдельный memory/native
DPAPI harness. Журнал — persisted evidence для следующего bounded recovery
слоя. Codec не читает и не пишет файлы, не создаёт каталогов, не обращается к
`AppPaths`, реальному vault, логам, часам, генератору ложных данных или UI.
Текущий статус плана остаётся **22/31**, F09 открыт.

## Журнал и ownership

Immutable `DataPath`, непустой `Operation` Guid, `Previous` и `Next` содержат
область операции и старый/новый immutable stamp. Digest остаётся приватным
внутри stamp; наружу не выдаётся изменяемый массив. Имена выводятся только из
фиксированного filename и Guid:

- staging: `vault.daily.dat.new-<Operation:N>`;
- unique backup: `vault.daily.dat.previous-<Operation:N>`.

Журнал не содержит произвольных имён вспомогательных файлов. Его factory
проверяет принадлежность обоих stamp указанному scope и запрещает missing
`Next`. При missing `Previous` требуется revision 0. В тот же день нужны прежнее
generation и ровно следующая revision, без переполнения. Смена календарного дня
в любом направлении требует другого generation и revision 0.

Это валидация stamp metadata. Запрет смены закреплённого PIN и требование пустого
day-reset проверяет writer по самим records **до** создания журнала: stamp не
содержит dataset, binding flag или PIN. Journal codec не заменяет эти проверки.
Операция не читает системную дату и не создаёт новое поколение самостоятельно.

## Формат и защиты

Envelope: magic `PGODJNL1` (8 bytes), Int32 version 1, Int32 protected-payload
length и ровно указанный payload без trailing bytes. Внутренний plaintext:
Int32 version 1, Int32 UTF8 byte length + data-path bytes, operation Guid
(16 bytes), previous evidence, next evidence, exact EOF.

Evidence: byte 0 для missing либо byte 1, local-day ticks (8), generation Guid
(16), revision (8), private SHA256 ciphertext digest (32). Парсер stamp проверяет
валидные календарные metadata; factory журнала дополнительно проверяет переход.
Строка scope сравнивается с переданным canonical data path посредством
`OrdinalIgnoreCase`. Успешный decode закрепляет stamps и `DataPath` за scope
вызывающего кода. Проверка canonical local filesystem path остаётся обязанностью
внешнего слоя; codec выполняет только memory operations.

Используется native DPAPI CurrentUser с отдельным purpose
`ProGo/VaultDailyJournal/v1`. Внутри DPAPI защищены scope, Operation и оба
свидетельства. Ошибки DPAPI, malformed metadata, неизвестные evidence flags,
недопустимые переходы и foreign scope не публикуют journal. Отдельный результат
`UnsupportedFormat` возвращается для неизвестной outer/inner версии; неизвестная
outer версия всё равно требует правильного полного framing.

Строгий UTF8 ограничивает scope 4096 bytes. Plaintext ограничен 8192 bytes,
envelope — `8192 + 65536` bytes. Поддерживаемая структура помещается в выделенный
сразу 8192-byte backing buffer, без его роста. Owned plaintext, DPAPI payload,
path/Guid byte buffers и backing buffer очищаются в `finally`. Caller envelope
не меняется. Сообщения статические, без path, SID, digest, PIN или данных.

Нулевой legacy `.recovery` не является валидным журналом. Codec не трактует его
как отсутствие и ничего не удаляет. Журнал не предотвращает hostile rollback
владельцем Windows account и не является доказательством полной hardware или
power-loss durability. Ownership cleanup допускает только распознанные
транзакционные артефакты; untracked pre-journal staging нельзя удалять по маске.

## Проверки и текущая контрольная точка

`tests/VaultDailyJournalTests.cs` использует только memory buffers и synthetic
Windows path strings. Он не обращается даже к временной файловой системе.
Есть guard-заглушки `AppPaths`/`SafeLog`, фактическое native DPAPI roundtrip,
независимый unprotect для проверки точного wire layout и независимо собранные
authenticated отрицательные fixtures.

Проверяются initial/replacement/day rotation в обе стороны, exhausted revision,
точные derived filenames, сохранность private digest после очистки caller
buffer, привязка к настоящему record ciphertext, foreign scope, static errors,
outer truncation/size/EOF/version, purpose mismatch/tamper, inner UTF8/caps,
metadata/evidence/transition errors, exact path byte boundary и zero legacy
marker. Количество assertions устанавливается по завершённому native log;
пока оно не объявляется фактическим PASS.

Исходный checkpoint: native Windows harness **PENDING**; здесь нет local .NET
runtime. Source review не подменяет native выполнение. Root отдельно подключает
stamp evidence seam, writer/recovery и PowerShell harness. Owner PC/VPS/телефон,
установленное приложение и другой Windows account/ПК не проверялись. Open/UI,
durable adoption, CRUD, startup/day expiry, полный installer всех31 и owner
acceptance остаются следующими задачами; merge/release не выполнялись.
