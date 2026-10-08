using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static class UpdateInstallWindowTests
    {
        private static int passed;
        private static void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            passed++; Console.WriteLine("PASS: " + name);
        }
        private static void InWindow(UpdateInstallSession session, Action<UpdateInstallForm> action) {
            var form = session.Window;
            if (form == null) throw new Exception("Install window disappeared.");
            form.Invoke(new Action(delegate { action(form); }));
        }
        private static void Finish(Task work) {
            if (Task.WhenAny(work, Task.Delay(5000)).GetAwaiter().GetResult() != work)
                throw new Exception("Install fixture watchdog expired.");
            work.GetAwaiter().GetResult();
        }
        private static void Shot(Form form, string directory, string name) {
            using (var bitmap = new Bitmap(form.Width, form.Height)) {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(directory, name + ".png"));
            }
        }
        [STAThread]
        private static int Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: install window requires isolated Windows CI"); return 0;
            }
            string root = args[0]; Directory.CreateDirectory(root);
            var contrast = typeof(UiTheme).GetField("contrastSource", BindingFlags.Static | BindingFlags.NonPublic);
            var originalContrast = contrast.GetValue(null);
            try {
                foreach (string action in new[] { "button", "escape", "title-close" })
                    using (var session = UpdateInstallSession.Open())
                    using (var server = new InstalledUpdateTransportTests.Server(Encoding.UTF8.GetBytes(new String('x', 100)), "body-stall")) {
                        session.ShowPhase("Этап 3 из 10: Скачиваю обновление", "Скорость зависит от подключения. Установленные файлы ещё не заменяются.");
                        session.BeginDownload();
                        string target = Path.Combine(root, action + ".zip");
                        var work = InstalledUpdateTransport.DownloadPackageAsync(server.Endpoint, target, 4000, 65536, session.Token);
                        Check(server.BodyStarted.Wait(3000), "actual partial package arrives before " + action);
                        InWindow(session, form => {
                            Check(form.Visible && form.CancelDownload.Enabled && form.AcceptButton == null,
                                "download window exposes cancel without a default action: " + action);
                            Check(form.Status.AccessibleDescription.Contains("Скачиваю") && form.Explanation.ReadOnly,
                                "phase and explanation are accessible read-only text: " + action);
                            if (action == "button") { Shot(form, root, "install-download"); form.CancelDownload.PerformClick(); }
                            else if (action == "escape") typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape });
                            else form.Close();
                        });
                        try { Finish(work); throw new Exception("Cancelled download completed normally."); }
                        catch (OperationCanceledException error) { Check(error.CancellationToken == session.Token && work.IsCanceled, "UI cancellation interrupts actual transport: " + action); }
                        Check(!File.Exists(target) && session.CancellationRequested, "cancel removes partial package: " + action);
                        InWindow(session, form => {
                            Check(form.Visible && !form.CancelDownload.Enabled && form.Status.Text.Contains("Отменяю"), "window waits for cleanup instead of closing the worker: " + action);
                            if (action == "button") Shot(form, root, "install-cancelling");
                        });
                        try { session.EndDownload(); throw new Exception("Cancelled transfer crossed validation boundary."); }
                        catch (OperationCanceledException) { Check(true, "cancellation wins before validation: " + action); }
                    }
                using (var session = UpdateInstallSession.Open()) {
                    Check(!session.CanCancel && !session.RequestCancellation(), "pre-download cannot cancel transaction startup");
                    session.BeginDownload(); session.EndDownload();
                    foreach (string phase in new[] { "Проверяю скачанный пакет", "Сохраняю резервную копию", "Устанавливаю обновление", "Восстанавливаю предыдущую версию" }) {
                        session.ShowPhase(phase, "Дождитесь результата; не закрывайте окно. Отмена скачивания уже недоступна.");
                        InWindow(session, form => {
                            form.Close(); form.CancelDownload.PerformClick();
                            Check(form.Visible && !form.CancelDownload.Enabled && !session.Token.IsCancellationRequested && !session.RequestCancellation(),
                                "close or stale button cannot cancel protected phase: " + phase);
                        });
                    }
                    foreach (bool highContrast in new[] { false, true }) {
                        contrast.SetValue(null, (Func<bool>)(() => highContrast));
                        foreach (var size in new[] { new Size(640, 340), new Size(544, 261) }) {
                            InWindow(session, form => {
                                form.ClientSize = size; UiTheme.Apply(form); form.PerformLayout();
                                Check(form.CancelDownload.Parent.ClientRectangle.Contains(form.CancelDownload.Bounds), "action fits window " + size + "/contrast=" + highContrast);
                                Check(form.Explanation.RectangleToScreen(form.Explanation.ClientRectangle).Bottom <= form.CancelDownload.RectangleToScreen(form.CancelDownload.ClientRectangle).Top,
                                    "phase explanation does not overlap action " + size + "/contrast=" + highContrast);
                                Check(form.Status.AccessibilityObject.Name == "Этап установки обновления" && form.CancelDownload.AccessibleDescription.Contains("восстановления"), "accessible role and protected-action description");
                                form.Explanation.Focus();
                                typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Enter });
                                Check(form.Visible && !session.CancellationRequested, "Enter in explanation has no implicit action");
                                Shot(form, root, "install-protected-" + size.Width + "-contrast-" + highContrast);
                            });
                        }
                    }
                    InWindow(session, form => {
                        form.Scale(new SizeF(1.5f, 1.5f)); form.PerformLayout();
                        Check(form.CancelDownload.Parent.ClientRectangle.Contains(form.CancelDownload.Bounds), "scaled cancellation action remains inside footer");
                        Shot(form, root, "install-protected-scaled");
                    });
                    session.Dispose(); Check(session.Window == null, "completed session joins and disposes native window");
                    session.ShowPhase("Late result", "Ignored after disposal");
                    Check(session.Window == null && !session.RequestCancellation(), "late status and close cannot recreate completed window");
                }
                // Race the exact boundary with cancellation, without creating 100 forms.
                for (int i = 0; i < 100; i++) using (var session = new UpdateInstallSession()) {
                    session.BeginDownload(); bool cancelled = false, ended = false;
                    using (var start = new ManualResetEventSlim()) {
                        var cancel = Task.Run(delegate { start.Wait(); cancelled = session.RequestCancellation(); });
                        var end = Task.Run(delegate { start.Wait(); try { session.EndDownload(); ended = true; } catch (OperationCanceledException) { } });
                        start.Set(); Finish(Task.WhenAll(cancel, end));
                    }
                    Check(cancelled != ended && (cancelled == session.Token.IsCancellationRequested), "cancel/validation boundary has one outcome: " + i);
                }
                Console.WriteLine("Install window tests PASS: " + passed); return 0;
            }
            catch (Exception error) { Console.WriteLine("FAIL: " + error); return 1; }
            finally { contrast.SetValue(null, originalContrast); }
        }
    }
}
