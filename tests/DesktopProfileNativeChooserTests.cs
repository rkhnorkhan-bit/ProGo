using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private const string ProfileChooserCleanup = "PROFILE_CHOOSER_CLEANUP_COMPLETE";
        private const string ProfileChooserMissing = "profile native SaveFileDialog not found within 5000 ms for the fixture owner";
        private const string ProfileChooserInjected = "injected profile native SaveFileDialog fixture failure";
        private static void ProfileNativeChooserWorkflow(string root)
        {
            foreach (string mode in new[] { "cancel", "missing", "error" }) {
                string childRoot = Path.Combine(root, "profile-chooser-" + Guid.NewGuid().ToString("N"));
                DiagnosticProcessResult result;
                try {
                    result = DiagnosticProcess.Run(Application.ExecutablePath,
                        "--profile-save-chooser-fixture " + SshConnection.Quote(childRoot) + " " + mode, 20000, CancellationToken.None);
                } catch (TimeoutException ex) { throw new TimeoutException("profile native SaveFileDialog fixture exceeded 20000 ms: " + mode, ex); }
                int expected = mode == "cancel" ? 0 : mode == "missing" ? 20 : 21;
                string error = mode == "missing" ? ProfileChooserMissing : ProfileChooserInjected;
                bool valid = result.ExitCode == expected && !result.Truncated && result.Error.Length == 0 && result.Output.Contains(ProfileChooserCleanup)
                    && (expected == 0 || result.Output.Contains("FAIL: " + error));
                Check(valid, "actual profile native chooser owns bounded cancel/failure cleanup: " + mode
                    + (valid ? "" : "; exit=" + result.ExitCode + "; stdout=" + result.Output + "; stderr=" + result.Error));
            }
        }
        private static int ProfileNativeChooserFixture(string[] args)
        {
            try {
                Console.OutputEncoding = new UTF8Encoding(false);
                if (args.Length != 3 || !new[] { "cancel", "missing", "error" }.Contains(args[2])) throw new ArgumentException("invalid profile chooser fixture arguments");
                string mode = args[2], root = Path.GetFullPath(args[1]); Directory.CreateDirectory(root);
                bool privateExisted = Directory.Exists(HomeVpnPrivateFiles.Root), observed = false, cleaned = false, returned = false;
                var before = WizardPrivateSnapshot(); int choices = 0, writes = 0; Exception failure = null;
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay, true))
                using (var clipboard = new ClipboardService(null)) {
                    PumpAsyncRestore(() => service.InitializationWork.IsCompleted, 5000); service.InitializationWork.GetAwaiter().GetResult();
                    // Presentation only. Cancel must never reach XML/file export,
                    // token import, SSH, clipboard or an external service.
                    typeof(HomeVpnService).GetProperty("Access", PrivateInstance).SetValue(service, new HomeVpnAccess { ServerId = new string('a', 32) }, null);
                    typeof(HomeVpnService).GetProperty("HomeAddress", PrivateInstance).SetValue(service, "home.example", null);
                    using (var form = new HomeVpnWizardForm(service, clipboard))
                    using (var timer = new System.Windows.Forms.Timer { Interval = 30 }) {
                        IntPtr owner = form.Handle; uint processId; uint threadId = DiagnosticChooserWindows.GetWindowThreadProcessId(owner, out processId);
                        var watch = new Stopwatch(); var initialExport = service.ProfileExportWork;
                        form.BeforeProfileIo = delegate { Interlocked.Increment(ref writes); };
                        timer.Tick += delegate {
                            try {
                                if (DiagnosticChooserWindows.Find(owner, threadId, processId, null) != IntPtr.Zero) observed = true;
                                if (watch.ElapsedMilliseconds >= 5000) throw new TimeoutException(choices == 0 ? ProfileChooserMissing : "profile native SaveFileDialog did not close within 5000 ms");
                                string caption = mode == "missing" ? "deliberately unmatched profile chooser caption" : null;
                                if (choices != 0 || DiagnosticChooserWindows.Find(owner, threadId, processId, caption) == IntPtr.Zero) return;
                                choices++;
                                if (mode == "error") throw new InvalidOperationException(ProfileChooserInjected);
                                DiagnosticChooserWindows.CloseOwned(owner, threadId, processId);
                            } catch (Exception ex) {
                                failure = ex; timer.Stop();
                                try { DiagnosticChooserWindows.CloseOwned(owner, threadId, processId); }
                                catch (Exception cleanup) { failure = new AggregateException(ex, cleanup); }
                            }
                        };
                        try {
                            PumpAsyncRestore(() => form.InitializationWork.IsCompleted && form.SetupSnapshotWork.IsCompleted, 5000);
                            form.InitializationWork.GetAwaiter().GetResult();
                            Call(form, "ShowStep", 3);
                            var button = Descendants(form).OfType<Button>().Single(b => b.Text == "Сохранить профиль iPhone файлом…");
                            form.Shown += delegate { form.BeginInvoke((Action)delegate {
                                try {
                                    Check(form.Visible && form.Modal, "profile chooser uses actual outer ShowDialog");
                                    watch.Start(); timer.Start(); button.PerformClick(); timer.Stop(); returned = true;
                                    if (failure != null) throw failure;
                                    Check(choices == 1 && observed && writes == 0 && Object.ReferenceEquals(initialExport, service.ProfileExportWork)
                                        && form.Visible && form.Modal && !(bool)Field(form, "busy") && !((PhoneVerification)Field(form, "verification")).Issued,
                                        "actual production profile chooser Cancel keeps modal owner open and never schedules profile I/O");
                                } catch (Exception ex) { failure = ex; }
                                finally { timer.Stop(); form.Close(); if (form.Visible) form.Dispose(); }
                            }); };
                            form.ShowDialog();
                            if (!returned) throw new Exception("profile modal exited before native chooser returned");
                        } catch (Exception ex) { failure = ex; }
                        finally {
                            timer.Stop();
                            try {
                                try { DiagnosticChooserWindows.CloseOwned(owner, threadId, processId); }
                                finally {
                                    form.Dispose(); service.Dispose(); var stop = service.StopAsync();
                                    var pending = Task.WhenAll(form.ProfileExportWork, form.InitializationWork, form.SetupSnapshotWork, service.ProfileExportWork, stop);
                                    WaitAsyncRestoreWithoutPump(pending, 5000); pending.GetAwaiter().GetResult();
                                    if (!stop.GetAwaiter().GetResult()) throw new Exception("profile chooser service cleanup incomplete");
                                }
                                var after = WizardPrivateSnapshot();
                                if (!form.IsDisposed || DiagnosticChooserWindows.Find(owner, threadId, processId, null) != IntPtr.Zero || relay.IsRunning || service.HasOwnedResources
                                    || privateExisted != Directory.Exists(HomeVpnPrivateFiles.Root) || before.Count != after.Count || !before.All(item => after.ContainsKey(item.Key) && item.Value.SequenceEqual(after[item.Key]))
                                    || Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length != 0 || writes != 0 || !Object.ReferenceEquals(initialExport, service.ProfileExportWork))
                                    throw new Exception("profile chooser cleanup changed protected files or left owned resources");
                                cleaned = true;
                            } catch (Exception ex) { failure = failure == null ? ex : new AggregateException(failure, ex); }
                        }
                    }
                }
                Console.WriteLine("PROFILE_NATIVE_CHOOSER_OBSERVED=" + observed + "; choices=" + choices + "; cleaned=" + cleaned);
                if (cleaned) Console.WriteLine(ProfileChooserCleanup);
                if (failure == null) return 0;
                Console.WriteLine("FAIL: " + failure.Message);
                if (cleaned && observed && returned) {
                    if (mode == "missing" && choices == 0 && failure is TimeoutException && failure.Message == ProfileChooserMissing) return 20;
                    if (mode == "error" && choices == 1 && failure is InvalidOperationException && failure.Message == ProfileChooserInjected) return 21;
                }
                return 1;
            } catch (Exception ex) { Console.Error.WriteLine("profile native SaveFileDialog fixture failed: " + ex); return 1; }
        }
    }
}
