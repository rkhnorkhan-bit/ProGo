using System;
using System.IO;
using System.Threading.Tasks;

namespace ProGo
{
    internal static class HomeVpnInvitationService
    {
        internal static async Task<string> RunAsync(HomeVpnOwner owner, bool recover, string label, Action<string> progress,
            Func<string, string, Task> copy, Func<string, string, string, Task> transport)
        {
            owner.Validate();
            owner = new HomeVpnOwner { Host = owner.Host, Port = owner.Port, Login = owner.Login, KeyFile = owner.KeyFile };
            if (label != null && (label.Length > 80 || Array.Exists(label.ToCharArray(), Char.IsControl)))
                throw new ArgumentException("Имя приглашения: до 80 символов без управляющих знаков.");
            using (var lease = await HomeVpnInvitationRecovery.AcquireAsync()) {
                var request = HomeVpnInvitationRecovery.Load(owner, label);
                bool checking = request != null;
                if (recover && !checking) throw new HomeVpnInvitationPendingException("Сохранённый запрос выдачи отсутствует. Новый токен не создавался.");
                HomeVpnPrivateFiles.SecureDirectory(HomeVpnPrivateFiles.Root);
                string work = Path.Combine(HomeVpnPrivateFiles.Root, "admin-" + Guid.NewGuid().ToString("N"));
                HomeVpnPrivateFiles.SecureDirectory(work);
                string name = "progo-" + Guid.NewGuid().ToString("N"), remote = "/tmp/" + name;
                string upload = Path.Combine(work, name); Directory.CreateDirectory(upload);
                bool dispatched = false;
                try {
                    // Invitation issuance and receipt queries need only this public helper.
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "home-vpn", "server", "home_vpn_setup.py"), Path.Combine(upload, "home_vpn_setup.py"));
                    string key = String.IsNullOrWhiteSpace(owner.KeyFile) ? "" : " -i " + HomeVpnService.Argument(owner.KeyFile);
                    string target = owner.Login + "@" + owner.Host;
                    progress(checking ? "Подготовка проверки прежней выдачи. Запрос сохранён; новый доступ не создаётся."
                        : "Копирование помощника для выдачи токена. Ответьте на запрос SSH в его окне.");
                    await copy("scp.exe", "-o ConnectTimeout=15 -P " + owner.Port + key + " -r " + HomeVpnService.Argument(upload) + " " + HomeVpnService.Argument(target + ":/tmp/"));
                    if (!checking) request = HomeVpnInvitationRecovery.Register(owner, label);
                    string prefix = (owner.Login == "root" ? "" : "sudo -n ") + "python3 -I " + remote + "/home_vpn_setup.py ";
                    string ssh = "-o ConnectTimeout=15 -T -p " + owner.Port + key + " " + HomeVpnService.Argument(target) + " ";
                    string output = Path.Combine(work, "result.txt");
                    dispatched = true;
                    if (checking) {
                        progress("Проверяем исходную выдачу токена. Новый доступ не запрашивается.");
                        await transport("ssh.exe", ssh + HomeVpnService.Argument(prefix + "operation-status --request-id " + request.RequestId
                            + " --output " + remote + "/status 1>&2 && cat " + remote + "/status"), output);
                        HomeVpnInvitationRecovery.RequireCompleted(HomeVpnInvitationRecovery.ReadOutput(output), request);
                        output = Path.Combine(work, "recovered.txt");
                        await transport("ssh.exe", ssh + HomeVpnService.Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix
                            + "operation-result --request-id " + request.RequestId + " --output " + remote + "/result 1>&2 && cat " + remote + "/result"), output);
                    } else {
                        progress("Создание отдельного токена. Исходный запрос сохранён до SSH; при потере ответа проверьте эту выдачу.");
                        await transport("ssh.exe", ssh + HomeVpnService.Argument("trap 'rm -rf -- " + remote + "' EXIT; " + prefix
                            + "invite --host " + Shell(owner.Host) + " --port " + owner.Port + " --name " + Shell(request.Name)
                            + " --request-id " + request.RequestId + " --output " + remote + "/result 1>&2 && cat " + remote + "/result"), output);
                    }
                    return HomeVpnInvitationRecovery.RetainResult(owner, request, HomeVpnInvitationRecovery.ReadOutput(output));
                } catch (HomeVpnPreparationCancelledException) {
                    if (checking) throw new HomeVpnPreparationCancelledException(true); throw;
                } catch (HomeVpnInvitationPendingException) { throw; }
                catch (Exception ex) {
                    if (ex is OutOfMemoryException || request == null) throw;
                    throw new HomeVpnInvitationPendingException(HomeVpnInvitationRecovery.PendingMessage);
                } finally {
                    try { Directory.Delete(work, true); }
                    catch {
                        if (request != null) throw new HomeVpnInvitationPendingException("Локальные файлы выдачи не удалось удалить. Исходный запрос сохранён; завершение операции не подтверждено. Закройте программы, использующие эти файлы, перед проверкой выдачи.");
                        if (!dispatched) throw new IOException("Выдача токена не запускалась, но файлы подготовки не удалось удалить. Закройте программы, использующие эти файлы.");
                    }
                }
            }
        }
        private static string Shell(string value) { return "'" + value.Replace("'", "'\"'\"'") + "'"; }
    }
}
