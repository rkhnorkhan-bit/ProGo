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

    internal sealed class AppCommandDefinition
    {
        internal readonly AppCommand Command;
        internal readonly string LegacyId;
        internal readonly bool RequiresRoute;
        internal readonly string Navigation;
        internal AppCommandDefinition(AppCommand command, string legacyId, bool requiresRoute, string navigation)
        { Command = command; LegacyId = legacyId; RequiresRoute = requiresRoute; Navigation = navigation; }
    }

    internal static class AppCommands
    {
        private static readonly Dictionary<string, AppCommandDefinition> byId = new Dictionary<string, AppCommandDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<AppCommand, AppCommandDefinition> byCommand = new Dictionary<AppCommand, AppCommandDefinition>();

        static AppCommands()
        {
            Add(AppCommand.Connect, "connect", true);
            Add(AppCommand.Reconnect, "restart", true);
            Add(AppCommand.StopDesktop, "stop");
            Add(AppCommand.StopPhone, "phone-stop");
            Add(AppCommand.StopAll, "stop-all");
            Add(AppCommand.Settings, "settings", false, "settings");
            Add(AppCommand.Connections, "connections", false, "connections");
            Add(AppCommand.WindowsSettings, "windows-settings", false, "settings");
            Add(AppCommand.Vault, "vault", false, "vault");
            Add(AppCommand.Phone, "iphone", false, "iphone");
            Add(AppCommand.Diagnostics, "diagnostics", false, "diagnostics");
            Add(AppCommand.CheckRoute, "route-check", false, "diagnostics");
            Add(AppCommand.StartCli, "cli-start", true, null, "terminal-on", "codex-on");
            Add(AppCommand.StopCli, "cli-off", false, null, "terminal-off", "codex-off");
            Add(AppCommand.EnableWindows, "windows-on", true);
            Add(AppCommand.DisableWindows, "windows-off");
            Add(AppCommand.CreateCodexShortcut, "codex-shortcut-on", true);
            Add(AppCommand.RemoveCodexShortcut, "codex-shortcut-off");
            Add(AppCommand.OpenCodex, "codex-open", true);
            Add(AppCommand.OpenTerminal, "terminal-open", true);
            Add(AppCommand.Help, "help");
            Add(AppCommand.Update, "update");
        }

        private static void Add(AppCommand command, string id, bool route = false, string navigation = null, params string[] aliases)
        {
            var definition = new AppCommandDefinition(command, id, route, navigation);
            byCommand.Add(command, definition); byId.Add(id, definition);
            foreach (var alias in aliases) byId.Add(alias, definition);
        }

        internal static AppCommandDefinition Get(AppCommand command) { return byCommand[command]; }
        internal static bool TryResolve(string id, out AppCommandDefinition definition)
        {
            definition = null;
            return id != null && byId.TryGetValue(id, out definition);
        }
    }
}
