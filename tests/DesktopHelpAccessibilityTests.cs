using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr HelpTabKey(IntPtr window, int message, IntPtr key, IntPtr data);

        private static void HelpKeyboardWalk(Form form, Control[] controls, string name)
        {
            Check(controls[0].Focus(), name + " first field accepts focus");
            var key = typeof(Control).GetMethod("ProcessDialogKey", PrivateInstance);
            Action<Keys> send = value => {
                key.Invoke(Descendants(form).Single(c => c.Focused), new object[] { value }); Application.DoEvents();
            };
            for (int i = 1; i < controls.Length; i++) {
                send(Keys.Tab); Check(controls[i].ContainsFocus, name + " Tab reaches " + controls[i].AccessibilityObject.Name);
            }
            for (int i = controls.Length - 2; i >= 0; i--) {
                send(Keys.Shift | Keys.Tab); Check(controls[i].ContainsFocus, name + " Shift+Tab returns to " + controls[i].AccessibilityObject.Name);
            }
        }

        private static void HelpAccessibility()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: help keyboard fixtures require isolated native CI"); return;
            }
            var before = File.ReadAllBytes(AppPaths.SettingsPath); var privateBefore = WizardPrivateSnapshot();
            bool privateExisted = Directory.Exists(HomeVpnPrivateFiles.Root); var calls = new List<AppCommand>();
            var key = typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance);
            using (var form = new HelpForm(command => calls.Add(command))) {
                form.Show(); Application.DoEvents();
                var tabs = Descendants(form).OfType<TabControl>().Single(); var close = (Button)form.CancelButton;
                var guidance = Descendants(form).OfType<Label>().Single(l => l.AccessibleName == "Управление помощью с клавиатуры");
                Check(form.AcceptButton == null && close.AccessibilityObject.Description.Contains("без остановки"), "help has no implicit action default and names independent closure");
                Check(tabs.AccessibilityObject.Description.Contains("Стрелки") && guidance.AccessibilityObject.Description == guidance.Text,
                    "help topics and fixed guidance explain keyboard navigation");
                tabs.Focus(); HelpTabKey(tabs.Handle, 0x100, new IntPtr(0x27), new IntPtr(1)); Application.DoEvents();
                Check(tabs.SelectedIndex == 1 && tabs.ContainsFocus, "native help Right switches topic while retaining tab focus");
                HelpTabKey(tabs.Handle, 0x100, new IntPtr(0x25), new IntPtr(1)); Application.DoEvents();
                Check(tabs.SelectedIndex == 0, "native help Left returns to previous topic");
                foreach (bool minimum in new[] { false, true }) {
                    if (minimum) form.Size = form.MinimumSize; else form.ClientSize = new Size(820, 620);
                    var size = form.ClientSize;
                    Check(guidance.Height >= TextRenderer.MeasureText(guidance.Text, guidance.Font, new Size(guidance.Width, Int32.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height,
                        "help keyboard guide fits at " + size.Width);
                    foreach (TabPage topic in tabs.TabPages) {
                        tabs.SelectedTab = topic; Application.DoEvents();
                        var body = topic.Controls.OfType<FlowLayoutPanel>().Single(); var actions = body.Controls.OfType<Button>().ToArray();
                        string name = "help " + topic.Text + " " + size.Width;
                        Check(body.TabStop && body.AccessibilityObject.Role == AccessibleRole.Pane && body.AccessibilityObject.Description.Contains("только чтение"),
                            name + " exposes a focusable read-only instruction pane");
                        HelpKeyboardWalk(form, new Control[] { tabs, body }.Concat(actions).Concat(new Control[] { close }).ToArray(), name);
                        foreach (var action in actions) {
                            action.Focus(); Application.DoEvents();
                            Check(body.RectangleToScreen(body.ClientRectangle).Contains(action.RectangleToScreen(action.ClientRectangle)), name + " brings focused action fully into view");
                            Check(!String.IsNullOrEmpty(action.AccessibilityObject.Description), name + " describes action effect");
                        }
                        body.Focus();
                        Action<Keys> scroll = value => {
                            var message = Message.Create(body.Handle, 0x100, new IntPtr((int)value), IntPtr.Zero);
                            Check(body.PreProcessMessage(ref message), name + " handles instruction scroll key " + value); Application.DoEvents();
                        };
                        scroll(Keys.Home); Check(body.AutoScrollPosition.Y == 0, name + " Home reaches instruction start");
                        bool overflow = body.VerticalScroll.Visible;
                        scroll(Keys.Down); Check(overflow ? body.AutoScrollPosition.Y < 0 : body.AutoScrollPosition.Y == 0, name + " Down scrolls only overflowing content");
                        scroll(Keys.Home); scroll(Keys.PageDown); int down = body.AutoScrollPosition.Y;
                        Check(overflow ? down < 0 : down == 0, name + " PageDown is bounded by available content");
                        scroll(Keys.PageUp); Check(body.AutoScrollPosition.Y == 0, name + " PageUp returns to start");
                        scroll(Keys.End); int end = body.AutoScrollPosition.Y;
                        scroll(Keys.Down); Check(body.AutoScrollPosition.Y == end, name + " Down cannot scroll beyond end");
                        scroll(Keys.Up); Check(end == 0 ? body.AutoScrollPosition.Y == 0 : body.AutoScrollPosition.Y > end, name + " Up returns toward start");
                        scroll(Keys.End); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                        Check(form.Visible && calls.Count == 0, name + " Enter in instruction does not open logs, export or close");
                        Check(form.ClientRectangle.Contains(form.RectangleToClient(close.RectangleToScreen(close.ClientRectangle))) &&
                            form.ClientRectangle.Contains(form.RectangleToClient(guidance.RectangleToScreen(guidance.ClientRectangle))), name + " keeps Close and keyboard guide visible at instruction end");
                        if (topic.Text == "Телефон" || topic.Text == "Начало") Shot(form, "keyboard-help-" + tabs.SelectedIndex + "-end-" + size.Width);
                    }
                }
                tabs.SelectedIndex = 0; Application.DoEvents();
                var report = Descendants(tabs.SelectedTab).OfType<Button>().Single(b => b.Text == "Передать диагностику…");
                report.Focus(); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(calls.SequenceEqual(new[] { AppCommand.ExportDiagnostics }) && form.Visible,
                    "focused help export Enter routes exactly once to the existing preview command");
                close.Focus(); Shot(form, "keyboard-help-close"); key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(!form.Visible && calls.Count == 1, "focused help Close Enter dismisses help without another command");
            }
            using (var form = new HelpForm(command => calls.Add(command))) {
                form.Show(); Application.DoEvents(); var tabs = Descendants(form).OfType<TabControl>().Single();
                tabs.SelectedIndex = 3; Application.DoEvents(); tabs.SelectedTab.Controls.OfType<FlowLayoutPanel>().Single().Focus();
                key.Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                Check(!form.Visible && calls.Count == 1, "help Escape dismisses a passive topic without actions");
            }
            var privateAfter = WizardPrivateSnapshot();
            Check(File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(before) && privateExisted == Directory.Exists(HomeVpnPrivateFiles.Root) &&
                privateBefore.Count == privateAfter.Count && privateBefore.All(pair => privateAfter.ContainsKey(pair.Key) && privateAfter[pair.Key].SequenceEqual(pair.Value)),
                "help keyboard navigation and injected preview command preserve settings and opaque private access files");
        }
    }
}
