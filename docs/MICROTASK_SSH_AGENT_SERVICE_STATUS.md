# SSH-agent: PGA-01 и PGA-02A — спецификация и read-only service snapshot

Источник — переданная владельцем спецификация16.10.2026, включаемая в текущий
F31/F23: [canonical contract](SSH_AGENT_AUTOSTART_SPEC.md). Исходный файл не
меняется; его адреса/профиль/личные пути не переносятся в Git и не становятся
defaults. Baseline `25d34000f5adb9c481021f47f8ef05ad2af073e5`, VERSION0.2.2.

PGA-01: аудит существующего кода и mapping; проверяются планом и независимым
review. Реализация и native приёмка узкого PGA-02A пока **PENDING**. Весь PGA-02
не завершён: отдельный asynchronous deadline/single-flight lifecycle —02B.
Запуск службы, installer opt-in, matching нужного ключа, secure interactive
ssh-add и отдельные UI действия остаются впереди. Код должен быть готов ДО
installer; live encrypted-key/другойПК/VPS принимаются после установки.

02A вводит read-only typed snapshot: runtime отдельно от startup policy,
raw observed values, отдельные status/config errors со stage и Win32Code.
Running+Disabled не смешиваются; Stopped не утверждает обязательного admin.
Native SCM запросы используют только connect/query rights, status/config
handles независимы; config failure не теряет известный runtime. Буфер config
ограничен8192 bytes, cleanup в finally, token checkpoints до/после операций.
Они не обещают прерывание начатого P/Invoke или hard deadline. Не выполняются
StartService/ChangeConfig/ACL/elevation/PowerShell/key operations.

Старые Read/Describe text wrappers остаются совместимыми; existing agent keys,
SSH alias/config/fallback/SOCKS logic не переписывается. Наличие стандартной
службы не доказывает использование именно этого агента выбранным SSH-профилем,
наличие нужного ключа либо успешную аутентификацию. IdentityAgent none/custom
и другие рабочие методы без агента не блокируются этой диагностикой.

Счёт остаётся **6/55**, остаток **49=37+12**, общий **23/31**, весь F31 **OPEN**.
F31-R11 уточняется существующим обязательным агентским dev объёмом без новых
верхних ID; F23 содержит installer opt-in, F31-R12/13/14 — реальные приёмки.
PGA номера — микродекомпозиция, не10 новых пунктов31 или55. F31-R05 и Codex
shortcut ownership finding сохраняются OPEN/PARTIAL. Package PENDING,
Live NOT_CHECKED; устройства владельца, VPS и установленное приложение не
проверяются. PR draft/unmerged; merge/release не выполняются.
