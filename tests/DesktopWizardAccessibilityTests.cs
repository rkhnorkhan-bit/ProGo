using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static Dictionary<string, byte[]> WizardPrivateSnapshot()
        {
            return Directory.Exists(HomeVpnPrivateFiles.Root)
                ? Directory.GetFiles(HomeVpnPrivateFiles.Root, "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllBytes)
                : new Dictionary<string, byte[]>();
        }
        private static void WizardAccessibility(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: wizard keyboard fixtures require isolated native CI"); return;
            }
            bool existed = Directory.Exists(HomeVpnPrivateFiles.Root); var before = WizardPrivateSnapshot();
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var clipboard = new ClipboardService(settings)) {
                // Populate presentation only; do not import a token, write private files or start SSH.
                typeof(HomeVpnService).GetProperty("Access", PrivateInstance).SetValue(service, null, null);
                typeof(HomeVpnService).GetProperty("Owner", PrivateInstance).SetValue(service,
                    new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "example", KeyFile = "" }, null);
                typeof(HomeVpnService).GetProperty("HomeAddress", PrivateInstance).SetValue(service, "home.example.org", null);
                using (var form = new HomeVpnWizardForm(service, clipboard)) {
                    form.Show(); Application.DoEvents(); ((Timer)Field(form, "refresh")).Stop();
                    var body = (FlowLayoutPanel)Field(form, "body"); var back = (Button)Field(form, "back"); var next = (Button)Field(form, "next");
                    var heading = (Label)Field(form, "heading"); var status = (Label)Field(form, "status"); var progress = (WizardProgress)Field(form, "progress");
                    Func<string, Button> button = text => body.Controls.OfType<Button>().Single(b => b.Text == text);
                    Action<string> walk = name => {
                        var inputs = body.Controls.Cast<Control>().Where(c => c is Button || c is TextBox || c is NumericUpDown || c is CheckBox || c is ComboBox || c is DataGridView);
                        KeyboardWalk(form, inputs.Concat(new Control[] { back, next }).Where(c => c.Enabled && c.Visible).ToArray(), "wizard " + name);
                        Check(body.Controls.OfType<Button>().All(b => !String.IsNullOrEmpty(b.AccessibilityObject.Description)), "wizard actions describe their effects: " + name);
                        Check(progress.AccessibilityObject.Name == "Шаг " + ((int)Field(form, "step") + 1) + " из 5" &&
                            progress.AccessibilityObject.Description == heading.Text && heading.AccessibilityObject.Description == heading.Text && !progress.TabStop,
                            "wizard current step is named without a decorative Tab stop: " + name);
                    };
                    Check(form.AcceptButton == null && form.CancelButton == null, "wizard has no implicit setup or stop default");
                    walk("choice");
                    button("Добавить свой VPS").PerformClick(); Application.DoEvents();
                    var host = (TextBox)Field(form, "host"); var port = (NumericUpDown)Field(form, "port");
                    var login = (TextBox)Field(form, "login"); var key = (TextBox)Field(form, "key");
                    Check(host.AccessibilityObject.Name == "Адрес VPS (IP или имя)" && port.AccessibilityObject.Name == "SSH-порт" &&
                        login.AccessibilityObject.Name.StartsWith("SSH-пользователь") && key.AccessibilityObject.Name == "Файл SSH-ключа (необязательно)", "wizard own-VPS fields match their visible labels");
                    host.Text = "pending.example.org"; port.Value = 2222; login.Text = "pending"; key.Text = "fixture-key";
                    walk("own VPS"); Shot(form, "keyboard-wizard-owner");
                    Check(next.AccessibilityObject.Description.Contains("Изменяет настройки сервера"), "wizard own-VPS continuation explains its server effect");
                    back.PerformClick(); Application.DoEvents(); button("Добавить свой VPS").PerformClick(); Application.DoEvents();
                    Check(((TextBox)Field(form, "host")).Text == "pending.example.org" && ((NumericUpDown)Field(form, "port")).Value == 2222 &&
                        ((TextBox)Field(form, "login")).Text == "pending" && ((TextBox)Field(form, "key")).Text == "fixture-key", "wizard Back retains pending owner fields in memory");
                    back.PerformClick(); Application.DoEvents(); button("Подключиться к готовому VPS").PerformClick(); Application.DoEvents();
                    var token = (TextBox)Field(form, "token"); token.Text = "synthetic-private-invitation";
                    Check(token.UseSystemPasswordChar && token.AccessibilityObject.Name == "Токен PROGO1.…" && token.AccessibilityObject.Description.Contains("скрыто") &&
                        !token.AccessibilityObject.Description.Contains(token.Text), "wizard invitation has a named masked input without value disclosure");
                    walk("friend token"); Shot(form, "keyboard-wizard-token");
                    Check(next.AccessibilityObject.Description.Contains("Проверяет токен"), "wizard friend continuation explains import and channel startup");
                    back.PerformClick(); Application.DoEvents(); button("Подключиться к готовому VPS").PerformClick(); Application.DoEvents();
                    Check(((TextBox)Field(form, "token")).Text == "synthetic-private-invitation", "wizard Back retains the pending masked token");
                    Call(form, "ShowStep", 2); Application.DoEvents();
                    var grid = body.Controls.OfType<DataGridView>().Single(); var home = (TextBox)Field(form, "home");
                    var router = (CheckBox)Field(form, "routerCheck");
                    Check(grid.ReadOnly && grid.StandardTab && grid.AccessibilityObject.Name == "Правила переадресации портов" &&
                        home.AccessibilityObject.Name.StartsWith("3. Внешний IPv4 дома") && router.AccessibilityObject.Description.Contains("не проверяет"), "wizard router fields distinguish instructions, address and manual confirmation");
                    walk("router"); grid.Focus();
                    typeof(DataGridView).GetMethod("ProcessDialogKey", PrivateInstance).Invoke(grid, new object[] { Keys.Tab }); Application.DoEvents();
                    Check(home.ContainsFocus, "native router-table Tab reaches the home address");
                    home.Text = "pending-home.example.org"; router.Checked = true; back.PerformClick(); Application.DoEvents();
                    Call(form, "ShowStep", 2); Application.DoEvents();
                    Check(((TextBox)Field(form, "home")).Text == "pending-home.example.org" && ((CheckBox)Field(form, "routerCheck")).Checked,
                        "wizard Back retains home address and router confirmation without saving access");
                    Call(form, "ShowStep", 3); Application.DoEvents();
                    var installed = (CheckBox)Field(form, "installedCheck"); var state = (PhoneVerification)Field(form, "verification");
                    walk("profile without confirmation");
                    Check(!next.Enabled && next.AccessibilityObject.Description.Contains("автоматически не проверяется"), "wizard profile navigation preserves explicit installation gating");
                    installed.Checked = true; walk("profile confirmed"); Call(form, "RecordProfileIssue");
                    var profile = (Label)Field(form, "profileState");
                    Check(!next.Enabled && !state.Installed && profile.AccessibilityObject.Description == profile.Text && profile.Text.Contains("не подтверждено"),
                        "wizard accessible issuance result never claims installation");
                    Call(form, "SetProgress", "Проверяем операцию…");
                    var waiting = new TaskCompletionSource<object>();
                    var task = (Task)form.GetType().GetMethod("RunStep", PrivateInstance).Invoke(form, new object[] { (Func<Task>)(() => waiting.Task) });
                    Check(!body.Enabled && !back.Enabled && !next.Enabled && Descendants(form).Where(c => c.TabStop).All(c => !c.Enabled), "wizard pending operation leaves no enabled keyboard action");
                    form.Close(); Application.DoEvents();
                    Check(form.Visible && (bool)Field(form, "busy") && status.AccessibilityObject.Description == status.Text && status.Text.Contains("Дождитесь"),
                        "wizard blocked close exposes current corrective status while work continues");
                    waiting.SetException(new InvalidOperationException("Проверка не выполнена.")); PumpUntil(() => task.IsCompleted);
                    Check(body.Enabled && back.Enabled && !next.Enabled && status.AccessibilityObject.Name == "Результат операции настройки VPN" &&
                        status.AccessibilityObject.Description == "Проверка не выполнена.", "wizard failed operation restores controls and retains installation gate with a named textual error");
                    walk("profile after failure");
                    ((CheckBox)Field(form, "installedCheck")).Checked = true; next.PerformClick(); Application.DoEvents();
                    var internet = (ComboBox)Field(form, "internetCheck"); var counters = (Label)Field(form, "counters");
                    walk("verification confirmed"); internet.SelectedIndex = 1;
                    Check(counters.AccessibilityObject.Description == counters.Text && counters.Text.Contains("доступа в интернет нет") &&
                        counters.AccessibilityObject.Name == "Состояние канала и проверки телефона", "wizard accessible phone result follows the user's current evidence");
                    ((CheckBox)Field(form, "installedCheck")).Checked = false; walk("verification unconfirmed");
                    Check(!internet.Enabled && state.Internet == PhoneInternet.Unknown, "wizard unconfirmed installation skips internet selection and clears stale evidence");
                    Check(next.Text == "Закрыть" && next.AccessibilityObject.Name == "Закрыть" && next.AccessibilityObject.Description.Contains("остаётся включённым"),
                        "wizard final action distinguishes closing the window from stopping the channel");
                    Shot(form, "keyboard-wizard-verification"); next.PerformClick(); Application.DoEvents();
                    Check(!form.Visible && !relay.IsRunning && service.Access == null, "wizard navigation never starts a real SSH or relay connection");
                }
            }
            var after = WizardPrivateSnapshot();
            Check(existed == Directory.Exists(HomeVpnPrivateFiles.Root) && before.Keys.OrderBy(p => p).SequenceEqual(after.Keys.OrderBy(p => p)) &&
                before.All(pair => after[pair.Key].SequenceEqual(pair.Value)), "wizard keyboard fixtures leave private access files unchanged");
        }
    }
}
