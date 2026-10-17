using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void StartupBackupLayout(SettingsService settings)
        {
            const string status = "Резервная копия не готова: ошибка проверки или копирования. Откройте состояние копии и журнал.";
            using (var proxy = new ProxyService(() => settings.Current, delegate { }, "unused-backup-layout-fixture", () => DateTime.UtcNow, false))
            using (var pingEntered = new ManualResetEventSlim())
            using (var routeEntered = new ManualResetEventSlim())
            using (var speedEntered = new ManualResetEventSlim())
            {
                CancellationToken pingToken = default(CancellationToken), routeToken = default(CancellationToken), speedToken = default(CancellationToken);
                using (var withoutBackup = new StatusForm(settings, proxy, false, (s, p, t) => "Fixture", null, null,
                    (s, t) => null, (s, t) => Tuple.Create((double?)null, (string)null)))
                using (var form = new StatusForm(settings, proxy, false,
                    delegate(AppSettings s, ProxyService p, CancellationToken token) {
                        routeToken = token; routeEntered.Set(); token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return "Fixture";
                    }, null, null,
                    delegate(AppSettings s, CancellationToken token) {
                        pingToken = token; pingEntered.Set(); token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return null;
                    },
                    delegate(AppSettings s, CancellationToken token) {
                        speedToken = token; speedEntered.Set(); token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested();
                        return Tuple.Create((double?)null, (string)null);
                    }, () => status))
                using (var pulse = new System.Windows.Forms.Timer { Interval = 10 })
                {
                    Check(form.MinimumSize == new Size(790, 610) && form.MinimumSize == withoutBackup.MinimumSize && form.Size == withoutBackup.Size,
                        "backup diagnostic row preserves the original minimum and initial window size");
                    int pulses = 0; pulse.Tick += delegate { pulses++; }; pulse.Start();
                    form.Show(); Application.DoEvents();
                    ((System.Windows.Forms.Timer)Field(form, "pingTimer")).Stop();
                    var viewport = Descendants(form).OfType<Panel>().Single(p => p.Name == "StatusBodyViewport");
                    var table = Descendants(form).OfType<TableLayoutPanel>().Single(t => t.Name == "StatusRows");
                    var backup = (Label)Field(form, "backupState");
                    var footer = Descendants(form).OfType<FlowLayoutPanel>().Single();
                    Check(table.Parent == viewport && table.Dock == DockStyle.Top && table.AutoSize && viewport.AutoScroll && footer.Parent == viewport.Parent,
                        "backup diagnostics use a top-sized body viewport and a separate fixed footer");
                    foreach (var size in new[] { form.MinimumSize, new Size(870, 690), new Size(790, 790), form.MinimumSize }) {
                        form.Size = size; Application.DoEvents(); form.PerformLayout(); Application.DoEvents();
                        var heights = table.GetRowHeights();
                        Check(heights.Length == 10 && heights.Select(h => (float)h).SequenceEqual(table.RowStyles.Cast<RowStyle>().Select(r => r.Height)) &&
                            table.Height == heights.Sum() + table.Padding.Vertical,
                            "backup diagnostics retain all exact body row heights at " + size);
                        Check(!viewport.HorizontalScroll.Visible && viewport.AutoScrollPosition.X == 0 && table.Right <= viewport.ClientSize.Width,
                            "backup diagnostics do not expand the body into horizontal scrolling at " + size);
                        foreach (var label in table.Controls.OfType<Label>()) {
                            int row = table.GetRow(label);
                            var textSize = label.GetPreferredSize(new Size(Math.Max(1, label.Width), 0));
                            Check(!label.AutoEllipsis && label.AutoSize && label.Width >= textSize.Width && label.Height >= textSize.Height &&
                                label.Height + label.Margin.Vertical <= heights[row] && label.Right <= table.ClientSize.Width,
                                "backup diagnostic row exposes all native text at " + size + "/" + label.AccessibleName);
                        }
                        var actions = footer.Controls.OfType<Button>().ToArray();
                        Check(actions.Length == 4 && actions.All(b => footer.ClientRectangle.Contains(b.Bounds) &&
                            form.ClientRectangle.Contains(form.RectangleToClient(b.RectangleToScreen(b.ClientRectangle)))),
                            "all four diagnostic footer actions stay visible at " + size);
                        Check(actions.OrderBy(b => b.Left).Zip(actions.OrderBy(b => b.Left).Skip(1), (a, b) => a.Right <= b.Left).All(v => v),
                            "diagnostic footer actions do not overlap at " + size);
                    }
                    Check(viewport.VerticalScroll.Visible && backup.Text == status && backup.AccessibilityObject.Description == status,
                        "original diagnostic minimum scrolls the complete Russian backup status vertically");
                    viewport.AutoScrollPosition = Point.Empty; Application.DoEvents(); Shot(form, "startup-backup-route-minimum");
                    var footerBounds = footer.Bounds;
                    viewport.ScrollControlIntoView(backup); Application.DoEvents();
                    Check(viewport.AutoScrollPosition.Y < 0 && viewport.ClientRectangle.Contains(viewport.RectangleToClient(backup.RectangleToScreen(backup.ClientRectangle))) &&
                        footer.Bounds == footerBounds,
                        "bottom backup row is reachable without moving the diagnostic footer");
                    Shot(form, "startup-backup-route-status");
                    ((Button)Field(form, "checkButton")).PerformClick(); ((Button)Field(form, "speedButton")).PerformClick();
                    PumpUntil(() => pingEntered.IsSet && routeEntered.IsSet && speedEntered.IsSet && pulses >= 3);
                    var pending = new[] { form.PingWork, form.RouteWork, form.SpeedWork };
                    Check(pending.All(t => t != null && !t.IsCompleted), "scrolled backup diagnostics retain responsive concurrent cancellable measurements");
                    Descendants(form).OfType<Button>().Single(b => b.Text == "Закрыть").PerformClick();
                    Check(pingToken.IsCancellationRequested && routeToken.IsCancellationRequested && speedToken.IsCancellationRequested,
                        "closing scrolled backup diagnostics cancels ping, route and speed probes");
                    Check(SpinWait.SpinUntil(() => pending.All(t => t.IsCompleted), 3000) && pending.All(t => !t.IsFaulted),
                        "closed backup diagnostics settle actual workers without another UI dispatch");
                    pulse.Stop();
                }
                using (var relay = new Ikev2RelayService())
                using (var home = new HomeVpnService(relay))
                {
                    using (var form = new MainWindow(settings, proxy, home, delegate { })) {
                        Check(Field(form, "backupProgress") == null && !Descendants(form).OfType<LinkLabel>().Any(l => l.Name == "BackupProgress"),
                            "dashboard without a backup callback creates no unused hidden status row");
                    }
                    string current = "";
                    using (var form = new MainWindow(settings, proxy, home, delegate { }, null, null, null, null, null, () => current)) {
                        form.Show(); Application.DoEvents(); ((System.Windows.Forms.Timer)Field(form, "timer")).Stop();
                        DashboardFixtureSize(form, new Size(744, 521));
                        var link = (LinkLabel)Field(form, "backupProgress");
                        Check(!link.Visible, "dashboard omits empty backup text before startup scheduling");
                        current = status + " Текущие данные и предыдущие копии сохранены. Автоматических повторов нет.";
                        form.RefreshConnectionState(); Application.DoEvents(); form.PerformLayout(); Application.DoEvents();
                        var viewport = (Panel)Field(form, "viewport"); var body = (TableLayoutPanel)Field(form, "body");
                        Check(link.Visible && link.Text == current && link.AutoSize && !link.AutoEllipsis && link.MaximumSize.Width > 0 &&
                            link.Right <= link.Parent.ClientSize.Width && link.Bottom <= link.Parent.ClientSize.Height,
                            "minimum dashboard wraps the full live backup status within its native stack");
                        Check(!viewport.HorizontalScroll.Visible && body.Width <= viewport.ClientSize.Width,
                            "long backup status does not widen the minimum dashboard viewport");
                        Check(Descendants(body).OfType<TableLayoutPanel>().Where(t => t.ColumnCount == 1).All(t => {
                            var controls = t.Controls.Cast<Control>().OrderBy(c => t.GetRow(c)).ToArray();
                            return controls.Zip(controls.Skip(1), (a, b) => a.Bottom <= b.Top).All(v => v);
                        }), "minimum dashboard backup status preserves nonoverlapping stack rows");
                        viewport.ScrollControlIntoView(link); Application.DoEvents();
                        Check(viewport.ClientRectangle.Contains(viewport.RectangleToClient(link.RectangleToScreen(link.ClientRectangle))),
                            "minimum dashboard backup status is reachable by vertical scroll");
                        foreach (string text in new[] { "Обновить ProGo", "Открыть Codex CLI с прокси", "Помощь" }) {
                            var button = Descendants(form).OfType<Button>().Single(b => b.Text == text);
                            Check(form.ClientRectangle.Contains(form.RectangleToClient(button.RectangleToScreen(button.ClientRectangle))),
                                "dashboard backup status keeps its fixed footer visible: " + text);
                        }
                        Shot(form, "startup-backup-dashboard-minimum"); form.Close();
                    }
                }
            }
        }
    }
}
