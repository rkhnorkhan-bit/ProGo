using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void ProfileSharingAccessibility(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: profile-sharing keyboard fixtures require isolated native CI"); return;
            }
            bool existed = System.IO.Directory.Exists(HomeVpnPrivateFiles.Root); var before = WizardPrivateSnapshot();
            var dialogKey = typeof(Form).GetMethod("ProcessDialogKey", PrivateInstance);
            using (var relay = new Ikev2RelayService())
            using (var service = new HomeVpnService(relay))
            using (var clipboard = new ClipboardService(settings)) {
                typeof(HomeVpnService).GetProperty("Access", PrivateInstance).SetValue(service, null, null);
                foreach (bool owner in new[] { true, false }) {
                    typeof(HomeVpnService).GetProperty("Owner", PrivateInstance).SetValue(service, owner
                        ? new HomeVpnOwner { Host = "vpn.example.org", Port = 22, Login = "example", KeyFile = "" } : null, null);
                    using (var form = HomeProfileShare.CreateConfigureForm(service)) {
                        form.Show(); Application.DoEvents();
                        var address = Descendants(form).OfType<TextBox>().Single(); var close = (Button)form.CancelButton;
                        var install = Descendants(form).OfType<Button>().Single(b => b.Text == "Настроить HTTPS на VPS");
                        var verify = Descendants(form).OfType<Button>().Single(b => b.Text == "Адрес уже настроен — проверить");
                        var status = Descendants(form).OfType<Label>().Single(l => l.AccessibleName == "Результат настройки HTTPS-выдачи");
                        Check(form.AcceptButton == null && install.Visible == owner && address.AccessibilityObject.Name == "HTTPS-адрес выдачи профиля" &&
                            address.AccessibilityObject.Description.Contains("только после успешной проверки"), "share address matches its label and gates saving on verified VPS ownership");
                        KeyboardWalk(form, owner ? new Control[] { address, install, verify, close } : new Control[] { address, verify, close }, "share address owner=" + owner);
                        Check(install.AccessibilityObject.Description.Contains("Изменяет настройки сервера") && verify.AccessibilityObject.Description.Contains("не запускает настройку") &&
                            close.AccessibilityObject.Description.Contains("без сохранения"), "share address actions distinguish server setup, verification and dismissal");
                        address.Text = "http://vpn.example.org"; address.Focus(); dialogKey.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                        Check(status.Text == "" && form.DialogResult == DialogResult.None, "share address Enter never starts setup or verification");
                        verify.PerformClick(); Application.DoEvents();
                        Check(status.Text.Contains("Укажите HTTPS-домен") && status.AccessibilityObject.Description == status.Text &&
                            address.Enabled && verify.Enabled && install.Enabled && close.Enabled && service.ShareOrigin == null,
                            "invalid share address exposes current textual error and restores actions without saving or making a request");
                        if (owner) { install.PerformClick(); Application.DoEvents(); Check(status.AccessibilityObject.Description == status.Text && service.ShareOrigin == null,
                            "invalid setup input also fails before SSH and leaves address unchanged"); }
                        KeyboardWalk(form, owner ? new Control[] { address, install, verify, close } : new Control[] { address, verify, close }, "share address after validation error owner=" + owner);
                        Shot(form, owner ? "keyboard-share-owner" : "keyboard-share-friend");
                        dialogKey.Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                        Check(!form.Visible && form.DialogResult == DialogResult.Cancel && service.ShareOrigin == null && !relay.IsRunning,
                            "share address Escape dismisses pending input without saving or starting a channel");
                    }
                }
                var link = new PhoneProfileLink { Url = "https://vpn.example.org/#" + new string('A', 43),
                    Expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 900, Matrix = Enumerable.Repeat(new string('0', 21), 21).ToArray() };
                int revoked = 0; var pending = new TaskCompletionSource<object>();
                using (var form = new PhoneProfileQrForm(link, () => { revoked++; return revoked == 1 ? pending.Task : Task.FromResult<object>(null); }, clipboard)) {
                    form.Show(); Application.DoEvents(); var clock = (Timer)Field(form, "clock"); clock.Stop();
                    var copy = Descendants(form).OfType<Button>().Single(b => b.Text == "Скопировать ссылку");
                    var revoke = Descendants(form).OfType<Button>().Single(b => b.Text == "Отозвать ссылку"); var close = (Button)form.CancelButton;
                    var picture = Descendants(form).OfType<PictureBox>().Single();
                    var status = Descendants(form).OfType<Label>().Single(l => l.AccessibleName == "Срок действия ссылки на профиль");
                    var notice = Descendants(form).OfType<Label>().Single(l => l.AccessibleName == "Результат копирования ссылки");
                    var error = Descendants(form).OfType<Label>().Single(l => l.AccessibleName == "Результат отзыва ссылки");
                    Check(!picture.TabStop && picture.AccessibilityObject.Role == AccessibleRole.Graphic && picture.AccessibilityObject.Name == "QR-код выдачи профиля VPN" &&
                        !picture.AccessibilityObject.Description.Contains(link.Url) && form.AcceptButton == null, "QR is a named non-Tab graphic without exposing the private link in metadata");
                    Check(copy.AccessibilityObject.Description.Contains(clipboard.CopyNotice) && revoke.AccessibilityObject.Description.Contains("продолжает работать") &&
                        close.AccessibilityObject.Description.Contains("без отзыва"), "QR actions distinguish copying, revocation and closing with the current clipboard policy");
                    Check(status.AccessibilityObject.Description == status.Text && notice.AccessibilityObject.Description == clipboard.CopyNotice,
                        "QR link lifetime and clipboard notice expose their current text");
                    KeyboardWalk(form, new Control[] { copy, revoke, close }, "QR active");
                    Check(Descendants(form).All(c => !(c.AccessibilityObject.Name ?? "").Contains(link.Url) &&
                        !(c.AccessibilityObject.Description ?? "").Contains(link.Url)), "QR metadata never includes the personal link");
                    revoke.PerformClick(); Application.DoEvents();
                    Check(revoked == 1 && !revoke.Enabled && copy.Enabled && close.Enabled, "QR pending revocation prevents duplicate requests and retains existing independent actions");
                    KeyboardWalk(form, new Control[] { copy, close }, "QR pending skips revoke");
                    pending.SetException(new InvalidOperationException("fixture failure")); PumpUntil(() => revoke.Enabled);
                    Check(error.Text.StartsWith("Отозвать не удалось") && error.AccessibilityObject.Description == error.Text &&
                        picture.Visible && copy.Enabled, "QR failed revocation exposes a named corrective result and keeps the link usable");
                    KeyboardWalk(form, new Control[] { copy, revoke, close }, "QR failure allows retry");
                    dialogKey.Invoke(form, new object[] { Keys.Tab }); dialogKey.Invoke(form, new object[] { Keys.Tab }); Application.DoEvents();
                    Check(close.ContainsFocus && close.Parent.ClientRectangle.Contains(close.Bounds) && error.Visible && error.Parent.ClientRectangle.Contains(error.Bounds),
                        "QR Tab reaches fully visible Close and corrective error outside scrolling content");
                    Shot(form, "keyboard-qr-retry");
                    revoke.PerformClick(); Application.DoEvents();
                    Check(revoked == 2 && !clock.Enabled && !picture.Visible && !copy.Enabled && !revoke.Enabled &&
                        status.Text.StartsWith("Ссылка отозвана") && status.AccessibilityObject.Description == status.Text && !error.Visible && error.AccessibilityObject.Description == "",
                        "QR successful retry removes copy and revoke actions while naming the real result");
                    KeyboardWalk(form, new Control[] { close }, "QR revoked");
                    close.Focus(); dialogKey.Invoke(form, new object[] { Keys.Enter }); Application.DoEvents();
                    Check(!form.Visible && revoked == 2, "QR Enter on Close never issues a second revoke");
                }
                using (var active = new PhoneProfileQrForm(link, () => { revoked++; return Task.FromResult(0); }, clipboard)) {
                    active.Show(); Application.DoEvents(); ((Timer)Field(active, "clock")).Stop();
                    ((Button)active.CancelButton).Focus(); dialogKey.Invoke(active, new object[] { Keys.Escape }); Application.DoEvents();
                    Check(!active.Visible && revoked == 2, "QR Escape dismisses an active link without revocation");
                }
                link.Expires = 1;
                using (var form = new PhoneProfileQrForm(link, () => { revoked++; return Task.FromResult(0); }, clipboard)) {
                    form.Show(); Application.DoEvents(); ((Timer)Field(form, "clock")).Stop();
                    var copy = Descendants(form).OfType<Button>().Single(b => b.Text == "Скопировать ссылку");
                    var revoke = Descendants(form).OfType<Button>().Single(b => b.Text == "Отозвать ссылку"); var close = (Button)form.CancelButton;
                    var status = Descendants(form).OfType<Label>().Single(l => l.AccessibleName == "Срок действия ссылки на профиль");
                    Check(!copy.Enabled && !Descendants(form).OfType<PictureBox>().Single().Visible && status.AccessibilityObject.Description == status.Text && status.Text.StartsWith("Время истекло"),
                        "expired QR removes copying and exposes the current expiry result");
                    KeyboardWalk(form, new Control[] { revoke, close }, "QR expired skips copying"); Shot(form, "keyboard-qr-expired");
                    dialogKey.Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                    Check(!form.Visible && revoked == 2, "QR Escape alone never invokes revocation even after expiry");
                }
            }
            var after = WizardPrivateSnapshot();
            Check(existed == System.IO.Directory.Exists(HomeVpnPrivateFiles.Root) && before.Keys.OrderBy(p => p).SequenceEqual(after.Keys.OrderBy(p => p)) &&
                before.All(pair => after[pair.Key].SequenceEqual(pair.Value)), "profile-sharing fixtures leave private access files unchanged");
        }
    }
}
