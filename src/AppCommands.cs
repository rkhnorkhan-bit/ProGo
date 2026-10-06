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
        CreateCodexShortcut, RemoveCodexShortcut, OpenCodex, OpenTerminal, Help, Update
    }

    // UI-thread snapshot: never retain the application's mutable pending collection.
    internal sealed class AppCommandState
    {
        private readonly HashSet<AppCommand> pending;
        internal readonly bool Connecting, Stopping;
        internal AppCommandState(IEnumerable<AppCommand> pending, bool connecting, bool stopping)
        { this.pending = new HashSet<AppCommand>(pending); Connecting = connecting; Stopping = stopping; }
        internal bool IsPending(AppCommand command) { return pending.Contains(command); }
        internal bool HasPending { get { return pending.Count != 0; } }
    }

    internal sealed class AppCommandDefinition
    {
        internal readonly AppCommand Command;
        internal readonly string LegacyId;
        internal readonly string Label, CompactLabel, ManualLabel;
        internal readonly bool RequiresRoute;
        internal readonly string Navigation;
        internal AppCommandDefinition(AppCommand command, string legacyId, bool requiresRoute, string navigation,
            string label, string compactLabel, string manualLabel)
        {
            Command = command; LegacyId = legacyId; RequiresRoute = requiresRoute; Navigation = navigation;
            Label = label; CompactLabel = compactLabel ?? label; ManualLabel = manualLabel ?? CompactLabel;
        }
    }

    internal static class AppCommands
    {
        private static readonly Dictionary<string, AppCommandDefinition> byId = new Dictionary<string, AppCommandDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<AppCommand, AppCommandDefinition> byCommand = new Dictionary<AppCommand, AppCommandDefinition>();

        static AppCommands()
        {
            Add(AppCommand.Connect, "connect", "Подключиться к серверу", "Подключиться", null, true);
            Add(AppCommand.Reconnect, "restart", "Переподключиться", "Переподключиться", "Перезапустить", true);
            Add(AppCommand.StopDesktop, "stop", "Отключить прокси на ПК");
            Add(AppCommand.StopPhone, "phone-stop", "Остановить VPN для телефона");
            Add(AppCommand.StopAll, "stop-all", "Остановить все подключения", "Остановить все\nподключения", null);
            Add(AppCommand.Settings, "settings", "Настройки и автоматика…", "Настройки", null, false, "settings");
            Add(AppCommand.Connections, "connections", "Подключения", navigation: "connections");
            Add(AppCommand.WindowsSettings, "windows-settings", "Настройки прокси Windows", "Настройки", null, false, "settings");
            Add(AppCommand.Vault, "vault", "Хранилище паролей и ключей…", "Хранилище", null, false, "vault");
            Add(AppCommand.Phone, "iphone", "VPN для телефона…", "VPN для телефона", null, false, "iphone");
            Add(AppCommand.Diagnostics, "diagnostics", "Открыть диагностику и скорость", "Диагностика", null, false, "diagnostics");
            Add(AppCommand.CheckRoute, "route-check", "Проверить маршрут", navigation: "diagnostics");
            Add(AppCommand.StartCli, "cli-start", "Запустить CLI (терминалы и Codex)", "Запустить CLI", "Включить", true, null, "terminal-on", "codex-on");
            Add(AppCommand.StopCli, "cli-off", "Выключить прокси для терминалов и Codex", "Выключить CLI", "Выключить", false, null, "terminal-off", "codex-off");
            Add(AppCommand.EnableWindows, "windows-on", "Windows — включить", "Включить", null, true);
            Add(AppCommand.DisableWindows, "windows-off", "Windows — выключить", "Выключить", null);
            Add(AppCommand.CreateCodexShortcut, "codex-shortcut-on", "Создать отдельный ярлык", route: true);
            Add(AppCommand.RemoveCodexShortcut, "codex-shortcut-off", "Удалить отдельный ярлык");
            Add(AppCommand.OpenCodex, "codex-open", "Открыть Codex CLI с прокси", route: true);
            Add(AppCommand.OpenTerminal, "terminal-open", "Открыть терминал с прокси", route: true);
            Add(AppCommand.Help, "help", "Открыть помощь…", "Помощь", null);
            Add(AppCommand.Update, "update", "Проверить обновления…", "Обновить ProGo", null);
        }

        private static void Add(AppCommand command, string id, string label, string compactLabel = null, string manualLabel = null, bool route = false, string navigation = null, params string[] aliases)
        {
            var definition = new AppCommandDefinition(command, id, route, navigation, label, compactLabel, manualLabel);
            byCommand.Add(command, definition); byId.Add(id, definition);
            foreach (var alias in aliases) byId.Add(alias, definition);
        }

        internal static bool CanExecute(AppCommand command, AppCommandState state)
        {
            var definition = Get(command);
            if (state.Stopping) return false;
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
