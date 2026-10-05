using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void BackupAccessibility()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") { Console.WriteLine("SKIP: backup keyboard fixtures require isolated native CI"); return; }
            var root = Path.Combine(work, "backup-keyboard-fixture"); Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "ProGo.exe"), "fixture; never execute");
            File.WriteAllText(Path.Combine(root, "VERSION"), "0.0.1");
            File.WriteAllText(Path.Combine(root, "settings.json"), "{}");
            File.WriteAllText(Path.Combine(root, "vault.enc.json"), "opaque fixture");
            var dialogKey = typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic);
            using (var form = new RestoreOptionsForm(root)) {
                form.Show(); Application.DoEvents();
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
                form.Show(); Application.DoEvents(); var program = (RadioButton)Field(form, "program");
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
