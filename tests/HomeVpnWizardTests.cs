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
                using (var qrForm = new PhoneProfileQrForm(qr, delegate { return System.Threading.Tasks.Task.FromResult(0); }))
                {
                    qrForm.Show(); Application.DoEvents();
                    Check(AllControls(qrForm).OfType<PictureBox>().Any(p => p.Image != null), "native dialog renders QR");
                    Check(AllControls(qrForm).OfType<Button>().Any(b => b.Text == "Отозвать ссылку"), "QR revocation action is present");
                    using (var shot = new Bitmap(qrForm.Width, qrForm.Height))
                    { qrForm.DrawToBitmap(shot, new Rectangle(Point.Empty, shot.Size)); shot.Save(Path.Combine(work, "qr-dialog.png")); }
                    qrForm.Close();
                }
                using (var relay = new Ikev2RelayService())
                using (var service = new HomeVpnService(relay))
                using (var form = new HomeVpnWizardForm(service))
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
                    using (var expired = new PhoneProfileQrForm(expiredQr, delegate { return System.Threading.Tasks.Task.FromResult(0); })) {
                        expired.Show(form); Application.DoEvents();
                        Check(AllControls(expired).OfType<Label>().Any(l => l.Text.Contains("Время истекло")) && !AllControls(expired).OfType<PictureBox>().Single().Visible,
                            "expired QR hides code and explains regeneration"); expired.Close();
                    }
                    Check(!state.Installed && !next.Enabled, "closing expired QR cannot mark installation successful");
                    int revoked = 0;
                    using (var revokedQr = new PhoneProfileQrForm(qr, delegate { revoked++; return System.Threading.Tasks.Task.FromResult(0); })) {
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
                Console.WriteLine("Home VPN PASS: " + passed + " checks."); return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("Home VPN test failed: " + ex.GetType().Name + ": " + ex.Message); return 1; }
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
