using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace ProGo
{
    internal static class HomeVpnWizardTests
    {
        private static int passed;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (HomeVpnPreparationTests.Fixture(args) || HomeVpnWaitingTests.Fixture(args)) return 0;
                var text = File.ReadAllText(args[0]);
                var access = HomeVpnAccess.Parse(text);
                Check(access.User.StartsWith("pgv") && access.Identity.EndsWith(".vpn.progo.invalid"), "server-generated token is accepted");
                var original = Json.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(Convert.FromBase64String(text.Substring(7).Replace('-', '+').Replace('_', '/').PadRight((text.Length - 7 + 3) / 4 * 4, '='))));
                foreach (var mutation in new[] {
                    new KeyValuePair<string,object>("Host", "vpn.example.org\nProxyCommand bad"),
                    new KeyValuePair<string,object>("User", "root"),
                    new KeyValuePair<string,object>("Port", 0),
                    new KeyValuePair<string,object>("Version", 2),
                    new KeyValuePair<string,object>("PrivateKey", "not a key"),
                    new KeyValuePair<string,object>("Ca", "not a certificate"),
                    new KeyValuePair<string,object>("Password", "bad\"password"),
                    new KeyValuePair<string,object>("ShareUrl", "http://vpn.example.org"),
                    new KeyValuePair<string,object>("HostKey", "ssh-ed25519 invalid") })
                {
                    var modified = new Dictionary<string, object>(original); modified[mutation.Key] = mutation.Value;
                    var invalid = "PROGO1." + Convert.ToBase64String(Encoding.UTF8.GetBytes(Json.Serialize(modified))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    bool rejected = false;
                    try { HomeVpnAccess.Parse(invalid); } catch (FormatException ex) { rejected = !ex.Message.Contains("bad") && !ex.Message.Contains(invalid); }
                    Check(rejected, "rejects unsafe " + mutation.Key + " without echoing token");
                }
                var work = Path.GetDirectoryName(Path.GetFullPath(args[0]));
                var profile = Path.Combine(work, "test.mobileconfig");
                access.WriteProfile(profile, "home.example.org");
                var xml = new XmlDocument { XmlResolver = null }; xml.Load(profile);
                Check(xml.SelectSingleNode("//key[text()='RemoteAddress']/following-sibling::*[1]").InnerText == "home.example.org", "phone connects to home address");
                Check(xml.SelectSingleNode("//key[text()='RemoteIdentifier']/following-sibling::*[1]").InnerText == access.Identity, "server certificate identity remains pinned");
                Check(xml.SelectSingleNode("//key[text()='DisableMOBIKE']/following-sibling::*[1]").InnerText == "1"
                    && xml.SelectSingleNode("//key[text()='IncludeAllNetworks']/following-sibling::*[1]").InnerText == "1", "profile uses home route and full tunnel");
                Check(xml.SelectNodes("//key[text()='DiffieHellmanGroup']/following-sibling::integer[1]").Cast<XmlNode>().All(n => n.InnerText == "14"), "generated profile uses valid DH14 in both associations");
                Check(xml.SelectSingleNode("//key[text()='EnablePFS']/following-sibling::*[1]").InnerText == "0", "CHILD PFS explicitly matches existing server ESP policy");
                Check(!File.ReadAllText(profile).Contains(access.PrivateKey), "iPhone profile never contains SSH private key");
                bool overwrite = false; try { access.WriteProfile(profile, "home.example.org"); } catch (IOException) { overwrite = true; }
                Check(overwrite, "profile export preserves existing file");
                HomeVpnPrivateFiles.Save("test-only", text);
                var saved = Path.Combine(HomeVpnPrivateFiles.Root, "test-only.dat");
                Check(HomeVpnPrivateFiles.Load("test-only") == text && !Encoding.UTF8.GetString(File.ReadAllBytes(saved)).Contains("PROGO1."), "local access is protected by Windows DPAPI");
                Check(Directory.GetAccessControl(HomeVpnPrivateFiles.Root).AreAccessRulesProtected, "credential directory does not inherit broad access");
                File.Delete(saved);
                var qr = Json.Deserialize<PhoneProfileLink>(File.ReadAllText(Path.Combine(work, "qr.json")));
                HomeProfileShare.Validate(qr, "vpn.example.org");
                Check(HomeProfileShare.Origin("vpn.example.org") == "https://vpn.example.org", "sharing normalizes to HTTPS origin");
                foreach (var url in new[] { "http://vpn.example.org", "https://user@vpn.example.org", "https://vpn.example.org/path", "https://vpn.example.org:8443", "https://vpn.example.org/#secret" })
                {
                    bool rejected = false; try { HomeProfileShare.Origin(url); } catch (ArgumentException) { rejected = true; }
                    Check(rejected, "unsafe sharing origin is rejected");
                }
                using (var bitmap = HomeProfileShare.Render(qr, 400))
                {
                    Check(bitmap.Width <= 400 && bitmap.GetPixel(0, 0).ToArgb() == Color.White.ToArgb(), "QR has integer pixels and white quiet zone");
                    bitmap.Save(Path.Combine(work, "qr-code.png"));
                }
                var invalidQr = new PhoneProfileLink { Url = "https://other.example.org/#" + new string('A', 43), Expires = qr.Expires, Matrix = qr.Matrix };
                bool wrongOrigin = false; try { HomeProfileShare.Validate(invalidQr, "vpn.example.org"); } catch (InvalidOperationException) { wrongOrigin = true; }
                Check(wrongOrigin, "QR cannot redirect to another origin");
                Application.EnableVisualStyles();
                using (var clipboardSettings = new SettingsService())
                using (var clipboard = new ClipboardService(clipboardSettings))
                {
                ClipboardChecks(clipboard, clipboardSettings);
                InvitationChecks(clipboard, work);
                HomeInvitationReissueTests.Run(Check, text, clipboard, work);
                HomeVpnWaitingTests.Run(Check, text, clipboard, work);
                HomeVpnPreparationTests.Run(Check, text, clipboard, work);
                using (var qrForm = new PhoneProfileQrForm(qr, delegate { return System.Threading.Tasks.Task.FromResult(0); }, clipboard))
                {
                    qrForm.Show(); Application.DoEvents();
                    Check(AllControls(qrForm).OfType<PictureBox>().Any(p => p.Image != null), "native dialog renders QR");
                    Check(AllControls(qrForm).OfType<Button>().Any(b => b.Text == "Отозвать ссылку"), "QR revocation action is present");
                    AllControls(qrForm).OfType<Button>().Single(b => b.Text == "Скопировать ссылку").PerformClick();
                    Check(Clipboard.GetText() == qr.Url && AllControls(qrForm).OfType<Label>().Any(l => l.Text.StartsWith("Скопировано.") && l.Text.Contains("5 сек.")), "QR copy uses shared secret timer and visible duration");
                    using (var shot = new Bitmap(qrForm.Width, qrForm.Height))
                    { qrForm.DrawToBitmap(shot, new Rectangle(Point.Empty, shot.Size)); shot.Save(Path.Combine(work, "qr-dialog.png")); }
                    qrForm.Close();
                    Check(Clipboard.ContainsText() && Clipboard.GetText() == qr.Url, "closing QR retains application-owned pending clipboard timer");
                    ClearClipboard(clipboard);
                    Check(!Clipboard.ContainsText(), "QR URL clears through shared timer callback");
                }
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var form = new HomeVpnWizardForm(service, clipboard))
                {
                    form.Show(); Application.DoEvents();
                    var show = typeof(HomeVpnWizardForm).GetMethod("ShowStep", BindingFlags.Instance | BindingFlags.NonPublic);
                    show.Invoke(form, new object[] { 0 }); Application.DoEvents();
                    var buttons = AllControls(form).OfType<Button>().ToArray();
                    Check(buttons.Any(b => b.Text == "Подключиться к готовому VPS") && buttons.Any(b => b.Text == "Добавить свой VPS"), "wizard presents both entry choices");
                    buttons.Single(b => b.Text == "Добавить свой VPS").PerformClick(); Application.DoEvents();
                    Check(AllControls(form).OfType<TextBox>().Count(t => t.Parent is FlowLayoutPanel) == 3 && AllControls(form).OfType<NumericUpDown>().Count() == 1, "own VPS step has host, account, key and port");
                    show.Invoke(form, new object[] { 0 }); Application.DoEvents();
                    AllControls(form).OfType<Button>().Single(b => b.Text == "Подключиться к готовому VPS").PerformClick(); Application.DoEvents();
                    Check(AllControls(form).OfType<TextBox>().Single().UseSystemPasswordChar, "friend token input is masked");
                    var state = (PhoneVerification)typeof(HomeVpnWizardForm).GetField("verification", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                    var issue = typeof(HomeVpnWizardForm).GetMethod("RecordProfileIssue", BindingFlags.Instance | BindingFlags.NonPublic);
                    var advance = typeof(HomeVpnWizardForm).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);
                    show.Invoke(form, new object[] { 3 }); Application.DoEvents();
                    var next = (Button)typeof(HomeVpnWizardForm).GetField("next", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                    Check(!next.Enabled && next.Text == "Перейти к проверке", "profile step cannot advance without explicit installation confirmation");
                    issue.Invoke(form, null);
                    Check(state.Issued && !state.Installed && !next.Enabled, "issuing QR or saving profile does not confirm installation");
                    var expiredQr = new PhoneProfileLink { Url = qr.Url, Matrix = qr.Matrix, Expires = 1 };
                    using (var expired = new PhoneProfileQrForm(expiredQr, delegate { return System.Threading.Tasks.Task.FromResult(0); }, clipboard)) {
                        expired.Show(form); Application.DoEvents();
                        Check(AllControls(expired).OfType<Label>().Any(l => l.Text.Contains("Время истекло")) && !AllControls(expired).OfType<PictureBox>().Single().Visible,
                            "expired QR hides code and explains regeneration"); expired.Close();
                    }
                    Check(!state.Installed && !next.Enabled, "closing expired QR cannot mark installation successful");
                    int revoked = 0;
                    using (var revokedQr = new PhoneProfileQrForm(qr, delegate { revoked++; return System.Threading.Tasks.Task.FromResult(0); }, clipboard)) {
                        revokedQr.Show(form); Application.DoEvents(); AllControls(revokedQr).OfType<Button>().Single(b => b.Text == "Отозвать ссылку").PerformClick(); Application.DoEvents();
                        Check(revoked == 1 && AllControls(revokedQr).OfType<Label>().Any(l => l.Text.Contains("Ссылка отозвана")), "QR revoke completes without claiming installation"); revokedQr.Close();
                    }
                    Check(!state.Installed && !next.Enabled, "closing revoked QR cannot mark installation successful");
                    ((System.Threading.Tasks.Task)advance.Invoke(form, null)).GetAwaiter().GetResult(); Application.DoEvents();
                    Check(!next.Enabled && AllControls(form).OfType<Label>().Any(l => l.Text.Contains("Подтвердите установку")), "advance guard refuses issuance-only state with corrective text");
                    var installed = AllControls(form).OfType<CheckBox>().Single(); installed.Checked = true;
                    Check(state.Installed && next.Enabled, "explicit phone installation confirmation enables verification step");
                    ((System.Threading.Tasks.Task)advance.Invoke(form, null)).GetAwaiter().GetResult(); Application.DoEvents();
                    var result = AllControls(form).OfType<ComboBox>().Single();
                    Check(result.Enabled && result.SelectedIndex == 0 && state.Internet == PhoneInternet.Unknown, "installed profile is not an internet pass");
                    result.SelectedIndex = 1;
                    Check(state.Describe(1, 1).Contains("доступа в интернет нет"), "connected VPN with no internet is distinct from packet exchange");
                    result.SelectedIndex = 2;
                    Check(state.Describe(1, 1).Contains("По вашей проверке: сайт открылся"), "internet success is explicitly attributed to the phone user's check");
                    AllControls(form).OfType<CheckBox>().Single().Checked = false;
                    Check(!result.Enabled && result.SelectedIndex == 0 && state.Internet == PhoneInternet.Unknown, "removing installation confirmation clears stale internet success");
                    state.SetInternet(PhoneInternet.Passed);
                    Check(state.Internet == PhoneInternet.Unknown, "unconfirmed installation cannot carry an internet success result");
                    Check(state.Describe(0, 0).Contains("Пакетов от телефона пока нет"), "verification distinguishes no incoming packets");
                    Check(state.Describe(1, 0).Contains("ответа VPS пока нет"), "verification distinguishes incoming packets with no reply");
                    Check(state.Describe(1, 1).Contains("Авторизация VPN и интернет этим не подтверждаются"), "returned packets cannot claim authentication or internet access");
                    show.Invoke(form, new object[] { 3 }); issue.Invoke(form, null); Application.DoEvents();
                    Check(state.Issued && !state.Installed && !next.Enabled && AllControls(form).OfType<Button>().Any(b => b.Text == "Установить на телефон по QR"),
                        "profile issuance can be repeated after expired or revoked QR without auto-confirming installation");
                    AllControls(form).OfType<CheckBox>().Single().Checked = true; state.SetInternet(PhoneInternet.Passed); issue.Invoke(form, null);
                    Check(!state.Installed && state.Internet == PhoneInternet.Unknown && !AllControls(form).OfType<CheckBox>().Single().Checked,
                        "new profile issuance resets both user confirmations and controls");
                    state.Reset(); Check(!state.Issued && !state.Installed && state.Internet == PhoneInternet.Unknown, "route reset invalidates all phone verification evidence");
                    foreach (int step in new[] { 0, 1, 2, 3, 4 })
                    {
                        show.Invoke(form, new object[] { step }); Application.DoEvents();
                        if (step == 3) {
                            Check(AllControls(form).OfType<Button>().Any(b => b.Text == "Установить на телефон по QR"), "phone step offers QR installation");
                            var confirmation = AllControls(form).OfType<CheckBox>().Single();
                            var viewport = (FlowLayoutPanel)confirmation.Parent;
                            viewport.AutoScrollPosition = Point.Empty; Application.DoEvents();
                            Check(viewport.ClientRectangle.Contains(viewport.RectangleToClient(confirmation.RectangleToScreen(confirmation.ClientRectangle))),
                                "required installation confirmation is visible at the top of the phone step without scrolling");
                        }
                        using (var bitmap = new System.Drawing.Bitmap(form.Width, form.Height))
                        { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(work, "wizard-" + step + ".png")); }
                    }
                    form.Close();
                }
                }
                Console.WriteLine("Home VPN PASS: " + passed + " checks."); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("Home VPN test failed: " + ex.GetType().Name + ": " + ex.Message); return 1; }
        }
        private static void InvitationChecks(ClipboardService clipboard, string work)
        {
            var first = Json.Deserialize<HomeVpnInvitation>("{\"Id\":\"aaaaaaaaaaaaaaaaaaaaaaaa\",\"Name\":\"Друг\",\"Revoked\":false,\"Created\":\"2026-01-02T06:04:05+03:00\"}");
            var second = new HomeVpnInvitation { Id = new string('b', 24), Name = "Друг", Revoked = true };
            Check(first.CreatedText == "2026-01-02 03:04 UTC", "server creation timestamp is displayed in explicit UTC");
            Check(second.CreatedText == "Дата не передана сервером", "legacy invitation does not invent creation date");
            Check(new HomeVpnInvitation { Created = "invalid" }.CreatedText == "Дата не передана сервером", "invalid date remains unknown");
            Check(new HomeVpnInvitation { Id = "x", Name = "Short" }.ToString().Contains("(x)"), "short legacy identifier does not crash invitation list");
            using (var form = new ProGoForm { Text = "Доступ друзей — проверка", ClientSize = new Size(740, 400) })
            using (var view = new HomeInvitationList(new[] { first, second }) { Dock = DockStyle.Fill }) {
                form.Controls.Add(view); form.Show(); Application.DoEvents();
                var search = AllControls(view).OfType<TextBox>().Single(); var list = AllControls(view).OfType<ListBox>().Single();
                var labels = AllControls(view).OfType<Label>().ToArray();
                Check(list.Items.Count == 2 && view.Selected == null && !view.CanRevoke, "friend list requires explicit selection before revocation");
                Check(search.AccessibilityObject.Name.Contains("идентификатор") && list.AccessibilityObject.Name == "Приглашения друзей", "friend search and list have meaningful accessible names");
                list.SelectedIndex = 0; Application.DoEvents();
                Check(view.Selected == first && view.CanRevoke && labels.Any(l => l.Text.Contains(first.CreatedText) && l.Text.Contains("без командной оболочки")), "selected invitation shows date, status and restricted scope");
                search.Text = "ДРУГ"; Application.DoEvents();
                Check(list.Items.Count == 2 && view.Selected == first, "case-insensitive name search preserves visible selection");
                search.Text = "bbbb"; Application.DoEvents();
                Check(list.Items.Count == 1 && view.Selected == null && !view.CanRevoke, "filtering out a selected friend clears destructive-action selection");
                list.SelectedIndex = 0;
                Check(view.Selected == second && !view.CanRevoke, "duplicate names resolve by identity and revoked access cannot be revoked again");
                search.Text = "missing";
                Check(list.Items.Count == 0 && labels.Any(l => l.Text.Contains("Совпадений нет")), "empty search has corrective guidance");
                search.Text = ""; list.SelectedIndex = 0; first.Revoked = true; view.RefreshSelection();
                Check(view.Selected == first && !view.CanRevoke && second.Revoked, "refresh retains revoked identity without changing other rows");
                var added = new HomeVpnInvitation { Id = new string('c', 24), Name = "Новый" }; view.Add(added);
                Check(view.Selected == added && view.CanRevoke && list.Items.Count == 3, "new invitation becomes the visible selected record");
                using (var shot = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(shot, new Rectangle(Point.Empty, shot.Size)); shot.Save(Path.Combine(work, "friends-list.png")); }
                form.Close();
            }
            using (var empty = new HomeInvitationList(new HomeVpnInvitation[0])) {
                Check(!empty.CanRevoke && AllControls(empty).OfType<Label>().Any(l => l.Text.Contains("Приглашений пока нет")), "empty invitation list explains how to begin");
            }
            using (var dialog = HomeInvitationList.TokenDialog("fixture-invite-only", clipboard)) {
                dialog.Show(); Application.DoEvents();
                var text = String.Join(" ", AllControls(dialog).OfType<Label>().Select(l => l.Text));
                Check(text.Contains("только сейчас") && text.Contains("повторно показать") && text.Contains("не QR-ссылка"), "token dialog explains one-time display and distinct QR purpose");
                var field = AllControls(dialog).OfType<TextBox>().Single();
                Check(field.ReadOnly && field.UseSystemPasswordChar, "issued token is masked and read-only");
                AllControls(dialog).OfType<Button>().Single(b => b.Text == "Скопировать токен").PerformClick();
                Check(Clipboard.GetText() == "fixture-invite-only", "actual invitation copy uses shared clipboard service");
                using (var shot = new Bitmap(dialog.Width, dialog.Height)) { dialog.DrawToBitmap(shot, new Rectangle(Point.Empty, shot.Size)); shot.Save(Path.Combine(work, "friends-token.png")); }
                dialog.Close();
            }
            ClearClipboard(clipboard); Check(!Clipboard.ContainsText(), "invitation token cleanup survives closing its dialog");
        }

        private static void ClearClipboard(ClipboardService clipboard)
        {
            typeof(ClipboardService).GetMethod("ClearIfStillOwned", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(clipboard, new object[] { null, EventArgs.Empty });
        }
        private static void ClipboardChecks(ClipboardService clipboard, SettingsService settings)
        {
            settings.Current.ClipboardClearSeconds = 5;
            const string secret = "fixture-secret-clipboard-no-log";
            try {
                clipboard.CopySecret(secret);
                Check(Clipboard.GetText() == secret, "secret copy writes current clipboard");
                var timer = (System.Windows.Forms.Timer)typeof(ClipboardService).GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(clipboard);
                Check(timer.Enabled && timer.Interval == 5000, "secret copy arms configured Windows timer");
                Clipboard.SetText("new-user-content"); ClearClipboard(clipboard);
                Check(Clipboard.GetText() == "new-user-content", "timer preserves newly copied different content");
                clipboard.CopySecret(secret); Clipboard.SetText(secret); ClearClipboard(clipboard);
                Check(Clipboard.GetText() == secret, "sequence ownership preserves a new copy of identical text");
                clipboard.CopySecret(secret); clipboard.CopySecret("second-fixture-secret"); ClearClipboard(clipboard);
                Check(!Clipboard.ContainsText(), "repeat secret copy replaces timer ownership");
                using (var dialog = new Form()) {
                    var button = new Button(); var notice = new Label(); dialog.Controls.Add(button); dialog.Controls.Add(notice); dialog.Show();
                    clipboard.BindSecretCopy(button, delegate { return secret; }, notice); button.PerformClick();
                    Check(Clipboard.GetText() == secret && notice.Text.Contains("5 сек.") && notice.Text.Contains("История"), "invitation copy binding schedules timer and explains history limit");
                    dialog.Close();
                }
                var deadline = DateTime.UtcNow.AddSeconds(7);
                while (Clipboard.ContainsText() && DateTime.UtcNow < deadline) { Application.DoEvents(); System.Threading.Thread.Sleep(20); }
                Check(!Clipboard.ContainsText(), "real Windows timer clears owned secret without manual callback");
                using (var exit = new ClipboardService(settings)) { exit.CopySecret(secret); }
                Check(!Clipboard.ContainsText(), "normal application disposal clears owned secret immediately");
                using (var exit = new ClipboardService(settings)) { exit.CopySecret(secret); Clipboard.SetText("user-content-on-exit"); }
                Check(Clipboard.GetText() == "user-content-on-exit", "normal application disposal preserves new user content");
                var repeated = new ClipboardService(settings); repeated.Dispose(); repeated.Dispose();
                bool disposed = false; try { repeated.CopySecret(secret); } catch (ObjectDisposedException) { disposed = true; }
                Check(disposed, "disposed secret service cannot restart timer");
                Check(!File.ReadAllText(AppPaths.LogPath).Contains(secret) && !File.ReadAllText(AppPaths.LogPath).Contains("second-fixture-secret"), "clipboard logs never contain secret values");
            } finally { Clipboard.Clear(); }
        }
        private static IEnumerable<Control> AllControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                yield return control;
                foreach (var child in AllControls(control)) yield return child;
            }
        }
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            passed++; Console.WriteLine("PASS: " + name);
        }
    }
}
