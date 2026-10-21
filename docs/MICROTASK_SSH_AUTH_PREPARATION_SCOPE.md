# F31-R11: оставшаяся подготовка метода входа перед SSH

Сверено на source `6b3122f0cd375ff1c0ae597bc677df58bbd2e5b8`.
Это восстановленный контракт и точка продолжения; реализация агрегатора/launch
интеграции ниже TODO. Проверка подписи уже реализована отдельным микрошагом,
но не считается готовностью всех способов аутентификации.

Источник: [SSH_AGENT_AUTOSTART_SPEC.md](SSH_AGENT_AUTOSTART_SPEC.md), разделы
«SSH-профиль и ключ» и «SSH/SOCKS и восстановление»;
[подпись выбранной identity](MICROTASK_SSH_SELECTED_KEY_SIGNING.md).

| Условие | Согласованное поведение |
| --- | --- |
| Нужная effective public identity подписывает через агент | Без повторного ввода фразы перейти к реальному SSH/SOCKS; подпись не доказывает доступ на сервер. |
| Зашифрованный ключ отсутствует в агенте | Предложить явное «Добавить ключ» через уже проверенную отдельную консоль OpenSSH; затем fresh профиль/ключ и явный повтор. Startup/recovery не должны сами открывать mutating ssh-add. |
| Прямой незашифрованный ключ | Агент не обязателен; отсутствие подписи через агент не запрещает прямой вход. |
| IdentityAgent none | Не обращаться к default agent как к обязательному условию. |
| Custom IdentityAgent | Нынешний backend сообщает UnsupportedEndpoint, не подменяет endpoint и не запрещает штатную попытку OpenSSH. |
| Publickey отключён или исключён | Agent/key proof не нужен; остальные разрешённые методы проверяются реальным SSH. |
| Неизвестная policy, cert/FIDO/constraints, несколько identities | Неподтверждённость с понятной причиной; не выбирать первую identity автоматически и не объявлять весь способ готовым. |
| Уже работающий настроенный SOCKS для CLI/Windows | Проверить SOCKS; новый собственный SSH не создаётся и новая подготовка ключа не требуется. |

`CommandFailed` от ssh-add -T не означает автоматически «зашифрованный ключ
не загружен»: возможны отказ, constraints, неподдержанный механизм или другой
ключ. Success по .pub не доказывает пригодность private bytes. Принятые источники
не задают общего запрета SSH по любому неуспеху подписи. Новый hard gate не вводится.

## Три следующих связанных блока

1. Агрегатор fresh CheckConfiguration и локальной подписи: immutable candidate,
   executable/endpoint, effective identity list, typed результат, общий бюджет,
   cancel/freshness; unknown/custom/other-auth допускают штатную попытку.
2. Одна preparation/reservation/epoch перед созданием собственного SSH в общем
   `ProxyService.StartSingleTunnel`: его вызывают StartTunnelCore и PollRecovery.
   Подготовка идёт вне UI/долгого gate; перед spawn повторно проверяются intent,
   настройки, endpoint и отмена. Context-only правка оставила бы recovery обход.
   Проверки: manual/auto/settings/recovery/fallback, borrowed SOCKS, Stop/Close,
   отсутствие late spawn и дублей. Это отдельный lifecycle блок, не одна вставка.
3. Раздельные profile/agent/key/SSH/SOCKS причины и существующие Check/Add/Retry;
   после Add fresh check, без повтора мутации. Bad-permissions одного ключа не
   запрещает другие identities. ACL исправляется только по отдельному подтверждению.

Выбор Combo диагностики сейчас не сохраняется в SshProfileSetting и не меняет
SSH argv: это выбранная reference текущей проверки, не новая identity туннеля.
Alias сохраняет effective список IdentityFile; direct explicit key задаёт -i
и IdentitiesOnly=yes. Изменение этой семантики не подразумевается новым UI.

Когда можно подавить первую штатную SSH-попытку, требует проверяемого определения
«единственный разрешённый метод — именно этот зашифрованный ключ». Такого
состояния backend пока не устанавливает. До его реализации сохраняется advisory
наблюдение и классификация настоящего SSH; это не новый внешний блокер выбора
владельца и не основание закрыть F31-R11.
