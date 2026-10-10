using System;
using System.Collections.Generic;

namespace ProGo
{
    // Internal operations, not persisted settings or public command-line arguments.
    internal enum AppCommand
    {
        Connect, Reconnect, StopDesktop, StopPhone, StopAll,
        Settings, Connections, WindowsSettings, Vault, Phone, Diagnostics, CheckRoute,
        StartCli, StopCli, EnableWindows, DisableWindows,
        CreateCodexShortcut, RemoveCodexShortcut, OpenCodex, OpenTerminal, Help, Update,
        ShowMain, CreateBackup, RestoreBackup, OpenBackups, CleanupBackups, ExportDiagnostics, OpenAppLog, OpenUpdateLog, OpenFolder, Exit,
        ExportHomeVpn, ImportHomeVpn
    }

    // UI-thread snapshot: never retain the application's mutable pending collection.
    internal sealed class AppCommandState
    {
        private readonly HashSet<AppCommand> pending;
        internal readonly bool Connecting, Stopping, BackingUp, Integrating;
        internal AppCommandState(IEnumerable<AppCommand> pending, bool connecting, bool stopping, bool backingUp = false, bool integrating = false)
        { this.pending = new HashSet<AppCommand>(pending); Connecting = connecting; Stopping = stopping; BackingUp = backingUp; Integrating = integrating; }
        internal bool IsPending(AppCommand command) { return pending.Contains(command); }
        internal bool HasPending { get { return pending.Count != 0; } }
    }

    internal sealed class AppCommandDefinition
    {
        internal readonly AppCommand Command;
        internal readonly string LegacyId;
        internal readonly string Label, CompactLabel, ManualLabel, CardLabel, Effect;
        internal readonly bool RequiresRoute;
        internal readonly string Navigation;
        internal AppCommandDefinition(AppCommand command, string legacyId, bool requiresRoute, string navigation,
            string label, string compactLabel, string manualLabel, string effect)
        {
            Command = command; LegacyId = legacyId; RequiresRoute = requiresRoute; Navigation = navigation;
            Label = label; CompactLabel = compactLabel ?? label; ManualLabel = manualLabel ?? CompactLabel;
            CardLabel = command == AppCommand.Phone ? "Открыть мастер" : CompactLabel; Effect = effect;
        }
    }

    internal static class AppCommands
    {
        private static readonly Dictionary<string, AppCommandDefinition> byId = new Dictionary<string, AppCommandDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<AppCommand, AppCommandDefinition> byCommand = new Dictionary<AppCommand, AppCommandDefinition>();

        static AppCommands()
        {
            Add(AppCommand.Connect, "connect", "Подключиться к серверу", "Подключить сервер. Если соединение уже работает, подключить его заново.", "Подключиться", null, true);
            Add(AppCommand.Reconnect, "restart", "Переподключиться", "Подключить сервер заново и отменить ожидающие действия прежнего соединения.", "Переподключиться", "Перезапустить", true);
            Add(AppCommand.StopDesktop, "stop", "Отключить прокси на ПК", "Отключить прокси приложений на ПК и остановить его SSH-подключение. VPN телефона продолжит работать.");
            Add(AppCommand.StopPhone, "phone-stop", "Остановить VPN для телефона", "Остановить канал VPN телефона. Подключение ПК продолжит работать.");
            Add(AppCommand.StopAll, "stop-all", "Остановить все подключения", "Отключить прокси на ПК, остановить SSH-подключение и VPN телефона.", "Остановить все\nподключения", null);
            Add(AppCommand.Settings, "settings", "Настройки и автоматика…", "Открыть настройки автоматики. Изменения полей применяются после сохранения.", "Настройки", null, false, "settings");
            Add(AppCommand.Connections, "connections", "Подключения", "Открыть настройки сервера и SSH-подключений.", navigation: "connections");
            Add(AppCommand.WindowsSettings, "windows-settings", "Настройки прокси Windows", "Открыть настройки прокси приложений Windows.", "Настройки", null, false, "settings");
            Add(AppCommand.Vault, "vault", "Хранилище паролей и ключей…", "Открыть хранилище после ввода PIN.", "Хранилище", null, false, "vault");
            Add(AppCommand.Phone, "iphone", "VPN для телефона…", "Открыть мастер подключения телефона и управления приглашениями.", "VPN для телефона", null, false, "iphone");
            Add(AppCommand.Diagnostics, "diagnostics", "Открыть диагностику и скорость", "Открыть состояние подключения и инструменты проверки маршрута и скорости.", "Диагностика", null, false, "diagnostics");
            Add(AppCommand.CheckRoute, "route-check", "Проверить маршрут", "Открыть диагностику и сразу проверить доступ в интернет через настроенный маршрут.", navigation: "diagnostics");
            Add(AppCommand.StartCli, "cli-start", "Запустить CLI (терминалы и Codex)", "Включить общий прокси и переменные окружения для новых терминалов и Codex. Отдельный ярлык не требуется.", "Запустить CLI", "Включить", true, null, "terminal-on", "codex-on");
            Add(AppCommand.StopCli, "cli-off", "Выключить прокси для терминалов и Codex", "Отменить ожидающий запуск CLI и восстановить изменённые ProGo переменные окружения. Режим Windows остаётся независимым.", "Выключить CLI", "Выключить", false, null, "terminal-off", "codex-off");
            Add(AppCommand.EnableWindows, "windows-on", "Windows — включить", "Включить системный прокси для приложений, использующих настройки Windows.", "Включить", null, true);
            Add(AppCommand.DisableWindows, "windows-off", "Windows — выключить", "Отменить ожидающее включение и восстановить изменённые ProGo настройки прокси Windows.", "Выключить", null);
            Add(AppCommand.CreateCodexShortcut, "codex-shortcut-on", "Создать отдельный ярлык", "Создать дополнительный ярлык для отдельного запуска Codex с прокси.", route: true);
            Add(AppCommand.RemoveCodexShortcut, "codex-shortcut-off", "Удалить отдельный ярлык", "Удалить созданный ProGo дополнительный ярлык Codex. Обычный режим CLI не выключается.");
            Add(AppCommand.OpenCodex, "codex-open", "Открыть Codex CLI с прокси", "Открыть отдельный сеанс Codex CLI с прокси. Codex должен быть установлен.", route: true);
            Add(AppCommand.OpenTerminal, "terminal-open", "Открыть терминал с прокси", "Открыть отдельный терминал с прокси.", route: true);
            Add(AppCommand.Help, "help", "Открыть помощь…", "Открыть инструкции по подключению, терминалам, телефону и восстановлению.", "Помощь", null);
            Add(AppCommand.Update, "update", "Проверить обновления…", "Проверить наличие новой версии. Установка требует подтверждения.", "Обновить ProGo", null);
            Add(AppCommand.ShowMain, "show-main", "Открыть ProGo", "Открыть главное окно ProGo.");
            Add(AppCommand.CreateBackup, "backup-create", "Создать копию сейчас", "Создать ручную копию в фоне с возможностью отмены. Текущие данные не заменяются.");
            Add(AppCommand.RestoreBackup, "backup-restore", "Восстановить из копии…", "Выбрать и проверить резервную копию. Восстановление начнётся только после подтверждения.");
            Add(AppCommand.OpenBackups, "backups-open", "Открыть папку с копиями", "Открыть папку резервных копий в Проводнике.");
            Add(AppCommand.CleanupBackups, "backups-cleanup", "Удалить старые автоматические копии…", "Показать старые автоматические копии. Удаление требует подтверждения; ручные копии сохраняются.");
            Add(AppCommand.ExportHomeVpn, "home-vpn-export", "Экспорт VPN на другой ПК…", "Создать отдельный защищённый архив личного VPN-доступа и незавершённых запросов. Нужна длинная парольная фраза; исходный VPS и доступ не изменяются.");
            Add(AppCommand.ImportHomeVpn, "home-vpn-import", "Импорт защищённого VPN…", "Проверить архив, просмотреть состав и явно импортировать VPN в чистую установку. Подключение не запускается; существующий доступ не заменяется.");
            Add(AppCommand.ExportDiagnostics, "diagnostics-export", "Передать диагностику…", "Открыть предпросмотр отчёта. Копирование и сохранение выполняются отдельно; автоматической отправки нет.");
            Add(AppCommand.OpenAppLog, "log-app", "Журнал приложения", "Открыть личный журнал приложения в Блокноте. Он может содержать адреса и пути.");
            Add(AppCommand.OpenUpdateLog, "log-update", "Журнал обновления", "Открыть личный журнал обновления в Блокноте. Он может содержать адреса и пути.");
            Add(AppCommand.OpenFolder, "folder-open", "Папка приложения", "Открыть папку ProGo в Проводнике.");
            Add(AppCommand.Exit, "exit", "Завершить работу ProGo", "Восстановить изменённые настройки прокси и завершить ProGo. При ошибке очистки программа останется открытой.");
        }

        private static void Add(AppCommand command, string id, string label, string effect, string compactLabel = null, string manualLabel = null, bool route = false, string navigation = null, params string[] aliases)
        {
            var definition = new AppCommandDefinition(command, id, route, navigation, label, compactLabel, manualLabel, effect);
            byCommand.Add(command, definition); byId.Add(id, definition);
            foreach (var alias in aliases) byId.Add(alias, definition);
        }

        internal static bool CanExecute(AppCommand command, AppCommandState state)
        {
            var definition = Get(command);
            if (state.Stopping) return false;
            if (state.Integrating && (definition.RequiresRoute || command == AppCommand.RemoveCodexShortcut)) return false;
            if (state.BackingUp && (command == AppCommand.CreateBackup || command == AppCommand.RestoreBackup ||
                command == AppCommand.CleanupBackups || command == AppCommand.Update || command == AppCommand.ExportHomeVpn || command == AppCommand.ImportHomeVpn)) return false;
            if (!definition.RequiresRoute) return true; // Off/Stop must remain usable to cancel waiting work.
            if (state.IsPending(command)) return false;
            // Plain Connect must not restart a route other modes are waiting for.
            // Explicit Reconnect deliberately retains its existing replacement semantics.
            return command != AppCommand.Connect || (!state.HasPending && !state.Connecting);
        }

        internal static AppCommandDefinition Get(AppCommand command) { return byCommand[command]; }
        internal static bool TryResolve(string id, out AppCommandDefinition definition)
        {
            definition = null;
            return id != null && byId.TryGetValue(id, out definition);
        }
    }
}
