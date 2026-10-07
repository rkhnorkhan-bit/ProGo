using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static UpdateCheckResult UpdateOffer(string notes = "Исправления подключения.\nНовые настройки.")
        { return new UpdateCheckResult { Availability = UpdateAvailability.Available, LocalVersion = "0.2.2", RemoteVersion = "0.3.0", ReleaseNotes = notes }; }
        private static void UpdateChecksUi(SettingsService settings)
        {
            var settingsBefore = File.Exists(AppPaths.SettingsPath) ? File.ReadAllBytes(AppPaths.SettingsPath) : null;
            var vaultBefore = File.Exists(AppPaths.VaultPath) ? File.ReadAllBytes(AppPaths.VaultPath) : null;
            var dialogKey = typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance);
            foreach (string action in new[] { "button", "escape", "title-close", "dispose" }) {
                var reply = new TaskCompletionSource<UpdateCheckResult>(); CancellationToken seen = default(CancellationToken); int calls = 0;
                var form = new UpdateCheckForm(token => { seen = token; Interlocked.Increment(ref calls); return reply.Task; }, "0.2.2");
                try {
                    form.Show(); PumpUntil(() => Volatile.Read(ref calls) == 1);
                    var close = (Button)Field(form, "close"); var primary = (Button)Field(form, "primary");
                    Check(form.IsChecking && !primary.Visible && close.Text == "Отменить" && form.AcceptButton == null,
                        "update pending exposes cancellation without default install: " + action);
                    Check(((Label)Field(form, "status")).AccessibilityObject.Name == "Состояние проверки обновлений" &&
                        ((Label)Field(form, "notice")).AccessibleDescription.Contains("15 секунд"), "pending update describes status and deadline: " + action);
                    Call(form, "StartCheck"); Application.DoEvents(); Check(calls == 1, "pending update cannot start a duplicate request: " + action);
                    if (action == "button") { Shot(form, "update-check-pending"); close.PerformClick(); }
                    else if (action == "escape") dialogKey.Invoke(form, new object[] { Keys.Escape });
                    else if (action == "title-close") form.Close();
                    else form.Dispose();
                    Check(seen.IsCancellationRequested && !form.Visible, "closing update cancels the captured request: " + action);
                    reply.SetResult(UpdateOffer()); PumpUntil(() => form.CheckWork.IsCompleted);
                    Check(form.AcceptedResult == null && !form.Visible, "late update result cannot reopen or authorize installation: " + action);
                } finally { form.Dispose(); }
            }
            int retries = 0; var retryReply = new TaskCompletionSource<UpdateCheckResult>();
            using (var form = new UpdateCheckForm(token => {
                if (Interlocked.Increment(ref retries) == 1) throw new IOException("private-fixture-detail");
                return retryReply.Task;
            }, "0.2.2")) {
                form.Show(); PumpUntil(() => !form.IsChecking);
                var status = (Label)Field(form, "status"); var primary = (Button)Field(form, "primary"); var close = (Button)Field(form, "close");
                var details = (TextBox)Field(form, "details");
                Check(status.Text == "Не удалось проверить обновления" && primary.Text == "Повторить проверку" && primary.Enabled,
                    "failed update exposes explicit retry");
                Check(!String.Join(" ", Descendants(form).Select(c => c.Text)).Contains("private-fixture-detail"), "update exception details stay out of user interface");
                Check(((Label)Field(form, "notice")).Text.Contains("журнале приложения"), "failed update points to the existing application journal");
                KeyboardWalk(form, new Control[] { details, primary, close }, "update failed retry"); Shot(form, "update-check-error");
                primary.Focus(); primary.PerformClick(); PumpUntil(() => Volatile.Read(ref retries) == 2);
                Check(form.IsChecking && close.Text == "Отменить" && !primary.Visible && close.Focused, "retry replaces stale error with visible progress");
                retryReply.SetResult(UpdateOffer("[Описание](https://example.org)\n<script>plain text</script>\n" + new String('x', 5000)));
                PumpUntil(() => !form.IsChecking);
                Check(status.Text == "Доступна версия 0.3.0" && primary.Text == "Установить обновление" && primary.Enabled,
                    "newer release exposes installation only after successful check");
                Check(details.ReadOnly && details.Text.Contains("<script>plain text</script>") && details.Text.Length < 3200 && details.Text.Contains("Описание сокращено"),
                    "release notes are bounded plain text without active links or markup");
                Check(details.Text.Contains("https://example.org)\r\n<script>plain text</script>\r\n"), "release preview preserves separate native Windows lines");
                Check(form.AcceptedResult == null && form.AcceptButton == null && !primary.ContainsFocus, "successful check never accepts installation or moves focus to install automatically");
                foreach (bool minimum in new[] { false, true }) {
                    if (minimum) form.Size = new Size(560, 380); else form.ClientSize = new Size(640, 440);
                    Application.DoEvents();
                    KeyboardWalk(form, new Control[] { details, primary, close }, "update available " + minimum);
                    foreach (var button in new[] { primary, close }) Check(button.Parent.ClientRectangle.Contains(button.Bounds), "update action remains visible at current size: " + button.Text + "/" + minimum);
                    Check(details.RectangleToScreen(details.ClientRectangle).Bottom <= primary.RectangleToScreen(primary.ClientRectangle).Top,
                        "update notes remain separate from actions: " + minimum);
                    Check(primary.Width >= TextRenderer.MeasureText(primary.Text, primary.Font).Width, "install label fits: " + minimum);
                    Shot(form, minimum ? "update-check-available-minimum" : "update-check-available");
                }
                details.Focus(); dialogKey.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(form.Visible && form.AcceptedResult == null, "Enter while reading notes does not install");
                primary.Focus(); dialogKey.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(!form.Visible && form.DialogResult == DialogResult.OK && form.AcceptedResult.RemoteVersion == "0.3.0",
                    "only explicit install accepts the reviewed release without starting a helper in the form");
            }
            foreach (var result in new[] {
                new UpdateCheckResult { Availability = UpdateAvailability.UpToDate, LocalVersion = "0.2.2" },
                new UpdateCheckResult { Availability = UpdateAvailability.Error, LocalVersion = "0.2.2", ErrorMessage = "Время ожидания ответа GitHub истекло. Повторите проверку обновлений." }
            }) using (var form = new UpdateCheckForm(token => Task.FromResult(result), "0.2.2")) {
                form.Show(); PumpUntil(() => !form.IsChecking);
                Check(form.AcceptedResult == null && ((Button)Field(form, "primary")).Text != "Установить обновление", "current/error result does not offer installation");
                if (result.Availability == UpdateAvailability.UpToDate) {
                    Check(!((Button)Field(form, "primary")).Visible, "current version exposes Close only"); Shot(form, "update-check-current");
                } else Check(((TextBox)Field(form, "details")).Text.Contains("Время ожидания"), "transport deadline is explained in the update window");
                dialogKey.Invoke(form, new object[] { Keys.Escape }); Check(!form.Visible, "Escape closes completed check");
            }
            using (var form = new UpdateCheckForm(token => Task.FromResult(UpdateOffer(null)), "0.2.2")) {
                form.Show(); PumpUntil(() => !form.IsChecking);
                Check(((TextBox)Field(form, "details")).Text.Contains("не опубликовано"), "release without notes has an honest fallback");
                ((Button)Field(form, "close")).PerformClick(); Check(form.AcceptedResult == null, "Later does not authorize installation");
            }
            UpdateModalContext(settings, false); UpdateModalContext(settings, true);
            Check(UpdateSame(settingsBefore, AppPaths.SettingsPath), "update UI preserves settings bytes and existence");
            Check(UpdateSame(vaultBefore, AppPaths.VaultPath), "update UI preserves vault bytes and existence");
        }
        private static bool UpdateSame(byte[] before, string path)
        { return before == null ? !File.Exists(path) : File.Exists(path) && Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(path)); }
        private static void UpdateModalContext(SettingsService settings, bool shutdown)
        {
            var reply = new TaskCompletionSource<UpdateCheckResult>(); int created = 0; UpdateCheckForm latest = null;
            using (var proxy = new ProxyService(settings))
            using (var bridge = new CliProxyBridgeService(settings))
            using (var relay = new Ikev2RelayService())
            using (var home = new HomeVpnService(relay))
            using (var clipboard = new ClipboardService(settings))
            using (var monitor = new ConnectionHealthMonitor(() => settings.Current))
            using (var context = new UpdateAwareTrayApplicationContext(settings, proxy, bridge, home, clipboard, false, monitor, null,
                () => { created++; latest = new UpdateCheckForm(token => reply.Task, "0.2.2"); return latest; }))
            using (var timer = new System.Windows.Forms.Timer { Interval = 30 }) {
                int ticks = 0; Exception callbackError = null;
                timer.Tick += delegate {
                    if (latest == null || !latest.Visible) return;
                    timer.Stop();
                    try {
                        ticks++; Call(context, "StartUpdate");
                        Check(created == ticks && latest.Visible, "repeated context update activates the same modal check: " + shutdown);
                        if (shutdown) Check(context.RequestShutdown(), "application shutdown cancels the modal check after cleanup succeeds");
                        else latest.CancelAndClose();
                    } catch (Exception ex) { callbackError = ex; latest.CancelAndClose(); }
                };
                timer.Start(); Call(context, "StartUpdate"); timer.Stop();
                if (callbackError != null) throw callbackError;
                Check(ticks == 1 && Field(context, "updateForm") == null, "modal completion releases context ownership: " + shutdown);
                reply.SetResult(UpdateOffer()); PumpUntil(() => latest.CheckWork.IsCompleted);
                Check(latest.AcceptedResult == null, "late modal check cannot cross maintenance handoff: " + shutdown);
                if (!shutdown) {
                    reply = new TaskCompletionSource<UpdateCheckResult>(); timer.Start(); Call(context, "StartUpdate"); timer.Stop();
                    if (callbackError != null) throw callbackError;
                    Check(created == 2, "explicit new update action can retry after cancellation");
                    reply.SetResult(UpdateOffer()); PumpUntil(() => latest.CheckWork.IsCompleted);
                }
            }
        }
    }
}
