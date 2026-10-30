using System;
using System.Windows.Forms;

namespace ProGo
{
    // Presentation helper only. The catalogue/state model remains independent of WinForms.
    internal static class AppCommandUi
    {
        internal static Button Button(AppCommand command, EventHandler click, bool primary = false, bool manual = false)
        {
            var definition = AppCommands.Get(command);
            var button = UiTheme.Button(manual ? definition.ManualLabel : definition.CompactLabel, click, primary);
            button.AccessibleDescription = definition.Effect;
            return button;
        }
    }
}
