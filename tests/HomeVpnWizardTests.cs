using System;
using System.Collections.Generic;
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
                Check(!File.ReadAllText(profile).Contains(access.PrivateKey), "iPhone profile never contains SSH private key");
                bool overwrite = false; try { access.WriteProfile(profile, "home.example.org"); } catch (IOException) { overwrite = true; }
                Check(overwrite, "profile export preserves existing file");
                HomeVpnPrivateFiles.Save("test-only", text);
                var saved = Path.Combine(HomeVpnPrivateFiles.Root, "test-only.dat");
                Check(HomeVpnPrivateFiles.Load("test-only") == text && !Encoding.UTF8.GetString(File.ReadAllBytes(saved)).Contains("PROGO1."), "local access is protected by Windows DPAPI");
                Check(Directory.GetAccessControl(HomeVpnPrivateFiles.Root).AreAccessRulesProtected, "credential directory does not inherit broad access");
                File.Delete(saved);
                Application.EnableVisualStyles();
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
                    Check(AllControls(form).OfType<TextBox>().Count() == 3 && AllControls(form).OfType<NumericUpDown>().Count() == 1, "own VPS step has host, account, key and port");
                    show.Invoke(form, new object[] { 0 }); Application.DoEvents();
                    AllControls(form).OfType<Button>().Single(b => b.Text == "Подключиться к готовому VPS").PerformClick(); Application.DoEvents();
                    Check(AllControls(form).OfType<TextBox>().Single().UseSystemPasswordChar, "friend token input is masked");
                    foreach (int step in new[] { 0, 1, 2, 3, 4 })
                    {
                        show.Invoke(form, new object[] { step }); Application.DoEvents();
                        using (var bitmap = new System.Drawing.Bitmap(form.Width, form.Height))
                        { form.DrawToBitmap(bitmap, form.ClientRectangle); bitmap.Save(Path.Combine(work, "wizard-" + step + ".png")); }
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
