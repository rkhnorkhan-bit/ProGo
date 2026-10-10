using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr BackupPickerListKey(IntPtr window, int message, IntPtr key, IntPtr data);

        private static void BackupPickerAccessibility()
        {
            var privateBefore = WizardPrivateSnapshot(); bool privateExisted = Directory.Exists(HomeVpnPrivateFiles.Root);
            var settingsBefore = File.ReadAllBytes(AppPaths.SettingsPath);
            var root = Path.Combine(work, "backup-picker-keyboard"); Directory.CreateDirectory(root);
            var firstPath = Path.Combine(root, "first"); var secondPath = Path.Combine(root, "second");
            Directory.CreateDirectory(firstPath); Directory.CreateDirectory(secondPath);
            var firstFile = Path.Combine(firstPath, "VERSION"); var secondFile = Path.Combine(secondPath, "VERSION");
            File.WriteAllText(firstFile, "0.0.1"); File.WriteAllText(secondFile, "0.0.2");
            var items = new List<BackupInfo> {
                new BackupInfo { Path = firstPath, DisplayName = "Сохранённая копия", Version = "0.0.1", Kind = "manual", Result = "manual", Reason = "fixture", Created = "2026-01-01" },
                new BackupInfo { Path = secondPath, DisplayName = "Сохранённая копия", Version = "0.0.2", TargetVersion = "0.0.3", Kind = "pre-update", Result = "ok", Reason = "fixture", Created = "2026-01-02" }
            };
            var key = typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance);
            using (var form = new BackupPickerForm(items)) {
                form.Show(); Application.DoEvents();
                var list = (ListBox)Field(form, "list"); var details = (TextBox)Field(form, "details");
                var next = (Control)form.AcceptButton; var cancel = (Control)form.CancelButton;
                Check(list.SelectedIndex == 0 && details.Text.Contains(firstPath) && details.Text.Contains("0.0.1"),
                    "backup picker retains its existing first-copy selection and current details");
                Check(list.AccessibilityObject.Name == "Сохранённые копии" && list.AccessibilityObject.Description.Contains("Стрелки") &&
                    details.AccessibilityObject.Name == "Сведения о выбранной копии" && details.ReadOnly && details.AccessibilityObject.Description.Contains("Только чтение"),
                    "backup picker exposes named selection and read-only current details");
                Check(next.AccessibilityObject.Description.Contains("отдельного подтверждения") && cancel.AccessibilityObject.Description.Contains("без изменения"),
                    "backup picker distinguishes next-step verification from cancellation");
                KeyboardWalk(form, new Control[] { list, details, next, cancel }, "backup picker");
                list.Focus(); BackupPickerListKey(list.Handle, 0x100, new IntPtr(0x28), new IntPtr(1)); Application.DoEvents();
                Check(list.SelectedIndex == 1 && details.Text.Contains(secondPath) && !details.Text.Contains(firstPath) && details.Text.Contains("0.0.3") && form.SelectedBackupPath == null,
                    "native picker Down distinguishes duplicate labels and updates details without committing selection");
                BackupPickerListKey(list.Handle, 0x100, new IntPtr(0x26), new IntPtr(1)); Application.DoEvents();
                Check(list.SelectedIndex == 0 && details.Text.Contains(firstPath) && !details.Text.Contains(secondPath), "native picker Up replaces stale details");
                foreach (var control in new Control[] { list, details, next, cancel })
                    Check(form.ClientRectangle.Contains(form.RectangleToClient(control.RectangleToScreen(control.ClientRectangle))), "backup picker keeps named keyboard controls visible");
                Shot(form, "keyboard-backup-picker"); form.Close();
            }
            // Exercise the real modal selector. It returns only a path; this fixture
            // never enters RestoreOptions, prepares a copy or launches maintenance.
            using (var timer = new Timer { Interval = 100 }) {
                timer.Tick += delegate {
                    var form = Application.OpenForms.OfType<BackupPickerForm>().Single(); timer.Stop();
                    var list = (ListBox)Field(form, "list"); list.SelectedIndex = 1; list.Focus();
                    key.Invoke(form, new object[] { Keys.Enter });
                };
                timer.Start(); string selected;
                Check(BackupPickerForm.TryPick(items, out selected) && selected == secondPath,
                    "modal picker Enter returns the selected backing path despite duplicate display names");
            }
            foreach (var empty in new[] { false, true }) {
                using (var timer = new Timer { Interval = 100 }) {
                    timer.Tick += delegate {
                        var form = Application.OpenForms.OfType<BackupPickerForm>().Single(); timer.Stop();
                        var list = (ListBox)Field(form, "list"); var details = (TextBox)Field(form, "details");
                        if (empty) {
                            Check(list.Items.Count == 0 && !((Control)form.AcceptButton).Enabled && details.Text.Contains("Нет сохранённых копий"),
                                "empty backup picker explains absence and disables next step");
                            KeyboardWalk(form, new Control[] { list, details, (Control)form.CancelButton }, "empty backup picker skips disabled next step");
                            key.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                            Check(form.DialogResult == DialogResult.None && form.SelectedBackupPath == null, "empty picker Enter cannot accept a missing copy");
                            Shot(form, "keyboard-backup-picker-empty");
                        }
                        details.Focus(); key.Invoke(form, new object[] { Keys.Escape });
                    };
                    timer.Start(); string selected;
                    Check(!BackupPickerForm.TryPick(empty ? new List<BackupInfo>() : items, out selected) && selected == null,
                        "modal picker Escape returns no selection for " + (empty ? "empty" : "populated") + " list");
                }
            }
            Check(File.ReadAllText(firstFile) == "0.0.1" && File.ReadAllText(secondFile) == "0.0.2" &&
                File.ReadAllBytes(AppPaths.SettingsPath).SequenceEqual(settingsBefore), "picker navigation, acceptance and cancellation leave copies and settings untouched");
            var privateAfter = WizardPrivateSnapshot();
            Check(Directory.Exists(HomeVpnPrivateFiles.Root) == privateExisted && privateBefore.Count == privateAfter.Count &&
                privateBefore.All(pair => privateAfter.ContainsKey(pair.Key) && privateAfter[pair.Key].SequenceEqual(pair.Value)),
                "backup picker leaves opaque private access files and directory existence unchanged");
        }

        private static void BackupAccessibility()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: backup keyboard fixtures require isolated native CI"); return; }
            BackupPickerAccessibility();
            var root = Path.Combine(work, "backup-keyboard-fixture"); Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "ProGo.exe"), "fixture; never execute");
            File.WriteAllText(Path.Combine(root, "VERSION"), "0.0.1");
            File.WriteAllText(Path.Combine(root, "settings.json"), "{}");
            File.WriteAllText(Path.Combine(root, "vault.enc.json"), "opaque fixture");
            var dialogKey = typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic);
            using (var form = new RestoreOptionsForm(root)) {
                form.Show(); Application.DoEvents(); PumpUntil(() => form.PreviewReady);
                var program = (RadioButton)Field(form, "program"); var data = (RadioButton)Field(form, "data");
                var all = (RadioButton)Field(form, "all"); var consent = (CheckBox)Field(form, "consent");
                var contents = (TextBox)Field(form, "contents"); var prepare = (Button)Field(form, "prepare");
                var cancel = (Button)form.CancelButton;
                Check(cancel.ContainsFocus && form.AcceptButton == null, "accessible restore preserves safe initial cancel focus and no Enter default");
                Check(contents.AccessibilityObject.Name == "Состав восстановления" && contents.ReadOnly && contents.AccessibilityObject.Description.Contains("Только чтение"),
                    "restore list exposes its purpose and read-only behavior");
                Check(((Control)Field(form, "status")).AccessibilityObject.Name == "Состояние подготовки копии" && consent.AccessibilityObject.Description.Contains("сбрасывает"),
                    "restore status and separate consent expose their purpose");
                KeyboardWalk(form, new Control[] { program, contents, prepare, cancel }, "restore program");
                program.Focus(); dialogKey.Invoke(form, new object[] { Keys.Down }); Application.DoEvents();
                Check(data.Checked && data.ContainsFocus && form.Scope == "Data" && consent.Enabled && !prepare.Enabled,
                    "native arrow selection reaches Data and keeps preparation gated by consent");
                KeyboardWalk(form, new Control[] { data, consent, contents, cancel }, "restore data without consent skips preparation");
                consent.Checked = true;
                KeyboardWalk(form, new Control[] { data, consent, contents, prepare, cancel }, "restore data confirmed");
                data.Focus(); dialogKey.Invoke(form, new object[] { Keys.Down }); Application.DoEvents();
                Check(all.Checked && all.ContainsFocus && !consent.Checked && !prepare.Enabled,
                    "native arrow scope change clears earlier consent");
                dialogKey.Invoke(form, new object[] { Keys.Up }); Application.DoEvents();
                Check(data.Checked && data.ContainsFocus && !consent.Checked, "reverse arrow selection retains separate confirmation requirement");
                contents.Focus(); dialogKey.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(form.DialogResult == DialogResult.None && !((bool)Field(form, "busy")), "Enter from restore preview does not start preparation");
                Shot(form, "keyboard-restore-options"); cancel.PerformClick();
                Check(form.DialogResult == DialogResult.Cancel && File.ReadAllText(Path.Combine(root, "settings.json")) == "{}",
                    "keyboard restore cancellation leaves source data untouched");
            }
            File.Delete(Path.Combine(root, "settings.json")); File.Delete(Path.Combine(root, "vault.enc.json"));
            using (var form = new RestoreOptionsForm(root)) {
                form.Show(); Application.DoEvents(); PumpUntil(() => form.PreviewReady); var program = (RadioButton)Field(form, "program");
                program.Focus(); dialogKey.Invoke(form, new object[] { Keys.Down }); Application.DoEvents();
                Check(form.Scope == "Program" && !((RadioButton)Field(form, "data")).Enabled && !((RadioButton)Field(form, "all")).Enabled,
                    "arrows cannot select unavailable data scopes"); form.Close();
            }
            var candidate = Path.Combine(root, "automatic-copy"); Directory.CreateDirectory(candidate);
            var plan = new BackupRetentionPlan(); plan.Candidates.Add(new BackupRetentionCandidate { Path = candidate });
            using (var form = new BackupCleanupForm(plan)) {
                form.Show(); Application.DoEvents();
                var paths = Descendants(form).OfType<TextBox>().Single(); var cancel = (Button)form.CancelButton;
                var remove = Descendants(form).OfType<Button>().Single(b => b != cancel);
                Check(cancel.ContainsFocus && form.AcceptButton == null, "accessible cleanup preserves safe initial cancel focus and no Enter default");
                Check(paths.AccessibilityObject.Name == "Копии для удаления" && paths.AccessibilityObject.Description.Contains("Только чтение") && paths.ReadOnly,
                    "cleanup list exposes its purpose and read-only behavior");
                KeyboardWalk(form, new Control[] { paths, remove, cancel }, "cleanup candidates");
                paths.Focus(); dialogKey.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                Check(form.DialogResult == DialogResult.None && Directory.Exists(candidate), "Enter from cleanup preview cannot approve deletion");
                Shot(form, "keyboard-backup-cleanup"); cancel.PerformClick();
                Check(form.DialogResult == DialogResult.Cancel && Directory.Exists(candidate), "keyboard cleanup cancellation preserves candidates");
            }
            using (var form = new BackupCleanupForm(new BackupRetentionPlan())) {
                form.Show(); Application.DoEvents(); var paths = Descendants(form).OfType<TextBox>().Single();
                KeyboardWalk(form, new Control[] { paths, (Control)form.CancelButton }, "empty cleanup skips disabled removal"); form.Close();
            }
        }
    }
}
