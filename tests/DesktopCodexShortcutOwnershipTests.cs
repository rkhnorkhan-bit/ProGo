using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void CodexShortcutOwnershipWorkflow()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: native Codex shortcut ownership requires isolated Windows CI"); return;
            }
            string root = Path.Combine(Path.GetTempPath(), "ProGo-Codex-ownership-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root); int before = passed;
            try {
                CodexShortcutRepeat(root);
                CodexShortcutForeign(root);
                CodexShortcutBoundedSnapshot(root);
                CodexShortcutUnavailable(root);
                CodexShortcutReparse(root);
                CodexShortcutAncestorReparse(root);
                CodexShortcutPinned(root);
            } finally { Directory.Delete(root, true); }
            Console.WriteLine("Codex shortcut ownership checks PASS: " + (passed - before));
        }
        private static string[] CodexShortcutPaths(string root, string name)
        {
            string fixture = Path.Combine(root, name), launcher = Path.Combine(fixture, "scripts", "Codex-ProGo.cmd");
            string shortcut = Path.Combine(fixture, "Programs", "Codex через ProGo.lnk");
            Directory.CreateDirectory(Path.GetDirectoryName(launcher)); Directory.CreateDirectory(Path.GetDirectoryName(shortcut));
            return new[] { launcher, shortcut };
        }
        private static string CodexFixtureArguments(string launcher)
        { return "/D /K \"\"" + launcher + "\"\""; }
        private static string CodexFixtureCmd
        { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"); } }
        private static void CodexShortcutRepeat(string root)
        {
            var paths = CodexShortcutPaths(root, "repeat-ключ");
            CodexProxyService.Enable(31881, paths[0], paths[1]);
            byte[] link = File.ReadAllBytes(paths[1]); var actual = ReadMigrationLink(paths[1]);
            Check(String.Equals(actual[0], CodexFixtureCmd, StringComparison.OrdinalIgnoreCase) && actual[1] == CodexFixtureArguments(paths[0]),
                "native scoped Codex link targets system cmd with the exact Unicode launcher arguments");
            CodexProxyService.Enable(31882, paths[0], paths[1]);
            Check(link.SequenceEqual(File.ReadAllBytes(paths[1])) && File.ReadAllText(paths[0]).Contains(CliProxyBridgeService.UrlFor(31882)),
                "repeat enable preserves the already owned native shortcut bytes and updates its launcher endpoint");
            CodexProxyService.Disable(paths[0], paths[1]); CodexProxyService.Disable(paths[0], paths[1]);
            Check(!File.Exists(paths[0]) && !File.Exists(paths[1]), "repeat disable deletes both checked native owners and remains idempotent");
            CodexProxyService.Enable(31881, paths[0], paths[1]); File.Delete(paths[1]);
            CodexProxyService.Disable(paths[0], paths[1]);
            Check(!File.Exists(paths[0]), "an absent shortcut still permits removal of its owned launcher");
            File.WriteAllText(paths[0], "@echo external launcher fixture");
            WriteCodexFixtureLink(paths[1], CodexFixtureCmd, CodexFixtureArguments(paths[0]));
            byte[] foreign = File.ReadAllBytes(paths[0]); link = File.ReadAllBytes(paths[1]);
            CodexProxyService.Disable(paths[0], paths[1]);
            Check(foreign.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                "the previous launcher marker guard preserves both files for a foreign cmd replacement");
            CodexShortcutRejected(() => CodexProxyService.Enable(31882, paths[0], paths[1]), "foreign launcher enable");
            Check(foreign.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                "enable also preserves both files when the cmd marker belongs to another application");
        }
        private static void CodexShortcutForeign(string root)
        {
            for (int variant = 0; variant < 5; variant++) {
                var paths = CodexShortcutPaths(root, "foreign-" + variant);
                CodexProxyService.Enable(31881, paths[0], paths[1]);
                if (variant == 0) WriteCodexFixtureLink(paths[1], Path.Combine(Environment.SystemDirectory, "notepad.exe"), CodexFixtureArguments(paths[0]));
                else if (variant == 1) WriteCodexFixtureLink(paths[1], CodexFixtureCmd, CodexFixtureArguments(paths[0]) + " & echo customized");
                else if (variant == 2) WriteCodexFixtureLink(paths[1], CodexFixtureCmd, CodexFixtureArguments(Path.Combine(root, "other-launcher.cmd")));
                else if (variant == 3) File.WriteAllBytes(paths[1], new byte[] { 0, 255, 1, 2, 3 });
                else {
                    // A valid cached/previous link is replaced at the same path
                    // with another real ShellLink before the destructive call.
                    string replacement = paths[1] + ".replacement.lnk";
                    WriteCodexFixtureLink(replacement, CodexFixtureCmd, "/D /K echo foreign replacement");
                    File.Delete(paths[1]); File.Move(replacement, paths[1]);
                }
                byte[] launcher = File.ReadAllBytes(paths[0]), link = File.ReadAllBytes(paths[1]);
                CodexShortcutRejected(() => CodexProxyService.Enable(31882, paths[0], paths[1]), "foreign/malformed link enable " + variant);
                Check(launcher.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                    "enable validates the fresh target and exact arguments before touching either file " + variant);
                CodexShortcutRejected(() => CodexProxyService.Disable(paths[0], paths[1]), "foreign/malformed link disable " + variant);
                Check(launcher.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                    "disable preserves both files after foreign target arguments malformed data or replacement " + variant);
                File.Delete(paths[0]);
                CodexShortcutRejected(() => CodexProxyService.Enable(31881, paths[0], paths[1]), "foreign link without launcher " + variant);
                Check(!File.Exists(paths[0]) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                    "a foreign shortcut prevents even the initial launcher publication " + variant);
            }
        }
        private static void CodexShortcutBoundedSnapshot(string root)
        {
            for (int variant = 0; variant < 2; variant++) {
                var paths = CodexShortcutPaths(root, "bounded-snapshot-" + variant);
                CodexProxyService.Enable(31881, paths[0], paths[1]);
                byte[] valid = File.ReadAllBytes(paths[1]);
                using (var stream = new FileStream(paths[1], FileMode.Open, FileAccess.Write, FileShare.None)) {
                    // The oversized case retains a real valid ShellLink prefix;
                    // rejection must come from the bound before COM parsing.
                    if (variant == 1) stream.Write(valid, 0, valid.Length);
                    stream.SetLength(variant == 0 ? 0 : 1024 * 1024 + 1);
                }
                byte[] launcher = File.ReadAllBytes(paths[0]), link = File.ReadAllBytes(paths[1]);
                foreach (Action action in new Action[] {
                    () => CodexProxyService.Enable(31882, paths[0], paths[1]),
                    () => CodexProxyService.Disable(paths[0], paths[1])
                }) {
                    string error = null; try { action(); } catch (IOException ex) { error = ex.Message; }
                    Check(error == "Не удалось прочитать ярлык Codex для проверки владения. Файлы сохранены без изменений." &&
                        launcher.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                        "empty or oversized native shortcut fails with a fixed nonsecret error before either file changes " + variant);
                }
            }
        }
        private static void CodexShortcutUnavailable(string root)
        {
            var paths = CodexShortcutPaths(root, "denied"); CodexProxyService.Enable(31881, paths[0], paths[1]);
            byte[] launcher = File.ReadAllBytes(paths[0]), link = File.ReadAllBytes(paths[1]);
            using (var held = new FileStream(paths[1], FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                CodexShortcutRejected(() => CodexProxyService.Enable(31882, paths[0], paths[1]), "exclusive native link enable");
                CodexShortcutRejected(() => CodexProxyService.Disable(paths[0], paths[1]), "exclusive native link disable");
                Check(launcher.SequenceEqual(File.ReadAllBytes(paths[0])), "an unreadable real shortcut leaves its launcher unchanged");
            }
            string original = File.GetAccessControl(paths[1], AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access);
            var denied = File.GetAccessControl(paths[1], AccessControlSections.Access);
            using (var identity = WindowsIdentity.GetCurrent()) denied.AddAccessRule(new FileSystemAccessRule(identity.User, FileSystemRights.ReadData, AccessControlType.Deny));
            try {
                File.SetAccessControl(paths[1], denied);
                CodexShortcutRejected(() => CodexProxyService.Enable(31882, paths[0], paths[1]), "real read-deny ACL enable");
                CodexShortcutRejected(() => CodexProxyService.Disable(paths[0], paths[1]), "real read-deny ACL disable");
            } finally {
                var restored = new FileSecurity(); restored.SetSecurityDescriptorSddlForm(original, AccessControlSections.Access); File.SetAccessControl(paths[1], restored);
            }
            Check(launcher.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                "real sharing and read-deny failures preserve both original files byte for byte");
            File.SetAttributes(paths[1], File.GetAttributes(paths[1]) | FileAttributes.ReadOnly);
            try {
                CodexShortcutRejected(() => CodexProxyService.Enable(31882, paths[0], paths[1]), "read-only shortcut enable");
                CodexShortcutRejected(() => CodexProxyService.Disable(paths[0], paths[1]), "read-only shortcut disable");
            } finally { File.SetAttributes(paths[1], File.GetAttributes(paths[1]) & ~FileAttributes.ReadOnly); }
            Check(launcher.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(paths[1])),
                "read-only shortcut refusal happens before a launcher change or deletion");
        }
        private static void CodexShortcutReparse(string root)
        {
            var paths = CodexShortcutPaths(root, "reparse"); CodexProxyService.Enable(31881, paths[0], paths[1]);
            string parent = Path.GetDirectoryName(paths[1]), target = parent + "-target"; Directory.Move(parent, target);
            var info = new ProcessStartInfo(CodexFixtureCmd, "/D /C mklink /J \"" + parent + "\" \"" + target + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            try {
                using (var process = Process.Start(info)) {
                    bool exited = process.WaitForExit(5000);
                    if (!exited) { process.Kill(); process.WaitForExit(2000); }
                    Check(exited && process.ExitCode == 0 && (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0,
                        "native Codex fixture creates an actual disposable NTFS shortcut-directory junction");
                }
                string targetLink = Path.Combine(target, Path.GetFileName(paths[1]));
                byte[] launcher = File.ReadAllBytes(paths[0]), link = File.ReadAllBytes(targetLink);
                CodexShortcutRejected(() => CodexProxyService.Enable(31882, paths[0], paths[1]), "reparse parent enable");
                CodexShortcutRejected(() => CodexProxyService.Disable(paths[0], paths[1]), "reparse parent disable");
                Check(launcher.SequenceEqual(File.ReadAllBytes(paths[0])) && link.SequenceEqual(File.ReadAllBytes(targetLink)),
                    "a reparse shortcut parent cannot redirect enable or disable into another directory");
            } finally { if (Directory.Exists(parent)) Directory.Delete(parent); }
        }
        private static void CodexShortcutAncestorReparse(string root)
        {
            string target = Path.Combine(root, "ancestor-target"), junction = Path.Combine(root, "ancestor-junction");
            Directory.CreateDirectory(target); string sentinel = Path.Combine(target, "preserved.txt");
            File.WriteAllText(sentinel, "preserved ancestor fixture"); byte[] bytes = File.ReadAllBytes(sentinel);
            var info = new ProcessStartInfo(CodexFixtureCmd, "/D /C mklink /J \"" + junction + "\" \"" + target + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            try {
                using (var process = Process.Start(info)) {
                    bool exited = process.WaitForExit(5000);
                    if (!exited) { process.Kill(); process.WaitForExit(2000); }
                    Check(exited && process.ExitCode == 0 && (File.GetAttributes(junction) & FileAttributes.ReparsePoint) != 0,
                        "native Codex fixture creates a higher junction above absent nested directories");
                }
                string launcher = Path.Combine(junction, "missing-scripts", "nested", "Codex-ProGo.cmd");
                string shortcut = Path.Combine(junction, "missing-programs", "nested", "Codex-ProGo.lnk");
                CodexShortcutRejected(() => CodexProxyService.Enable(31881, launcher, shortcut), "missing parents below a launcher junction ancestor");
                var outside = CodexShortcutPaths(root, "ancestor-outside");
                File.WriteAllText(outside[0], CodexProxyService.LauncherContent(31881)); byte[] owned = File.ReadAllBytes(outside[0]);
                CodexShortcutRejected(() => CodexProxyService.Enable(31882, outside[0], shortcut), "missing parents below a shortcut junction ancestor");
                CodexShortcutRejected(() => CodexProxyService.Disable(outside[0], shortcut), "missing shortcut parents below a junction ancestor");
                Check(owned.SequenceEqual(File.ReadAllBytes(outside[0])) && bytes.SequenceEqual(File.ReadAllBytes(sentinel)) &&
                    Directory.GetFileSystemEntries(target).Length == 1 && !File.Exists(outside[1]),
                    "existing junction ancestors are rejected even when immediate parents are absent and no target bytes or directories change");
            } finally { if (Directory.Exists(junction)) Directory.Delete(junction); }
        }
        private static void CodexShortcutPinned(string root)
        {
            var paths = CodexShortcutPaths(root, "pinned"); CodexProxyService.Enable(31881, paths[0], paths[1]);
            byte[] link = File.ReadAllBytes(paths[1]); int called = 0;
            Action attemptReplacement = () => {
                called++;
                CodexShortcutRejected(() => File.Delete(paths[1]), "replacement delete while native owner is held");
                CodexShortcutRejected(() => File.WriteAllText(paths[1], "foreign"), "replacement write while native owner is held");
                CodexShortcutRejected(() => { using (var second = File.OpenRead(paths[1])) { } },
                    "pathname read without DELETE sharing while the parsed native owner is held");
            };
            CodexProxyService.Enable(31882, paths[0], paths[1], attemptReplacement);
            Check(called == 1 && link.SequenceEqual(File.ReadAllBytes(paths[1])) && File.ReadAllText(paths[0]).Contains(CliProxyBridgeService.UrlFor(31882)),
                "the checked native shortcut stays pinned against concurrent replacement through launcher publication");
            CodexProxyService.Disable(paths[0], paths[1], attemptReplacement);
            Check(called == 2 && !File.Exists(paths[0]) && !File.Exists(paths[1]),
                "disable marks deletion on its exact checked native handle rather than a later path lookup");
        }
        private static void CodexShortcutRejected(Action action, string label)
        {
            bool refused = false;
            try { action(); }
            catch (IOException) { refused = true; }
            catch (UnauthorizedAccessException) { refused = true; }
            catch (COMException) { refused = true; }
            Check(refused, "Codex shortcut ownership rejects " + label);
        }
        private static void WriteCodexFixtureLink(string path, string target, string arguments)
        {
            object native = new CodexFixtureShellLink();
            try {
                var link = (ICodexFixtureShellLink)native; link.SetPath(target); link.SetArguments(arguments);
                // A copied description is deliberately insufficient ownership.
                link.SetDescription("Codex CLI с прокси ProGo только для этого окна");
                ((System.Runtime.InteropServices.ComTypes.IPersistFile)native).Save(path, true);
            } finally { Marshal.FinalReleaseComObject(native); }
        }
        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class CodexFixtureShellLink { }
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICodexFixtureShellLink
        {
            void GetPath(IntPtr path, int length, IntPtr data, uint flags);
            void GetIDList(out IntPtr list); void SetIDList(IntPtr list);
            void GetDescription(IntPtr description, int length); void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string description);
            void GetWorkingDirectory(IntPtr directory, int length); void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments(IntPtr arguments, int length); void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short hotkey); void SetHotkey(short hotkey);
            void GetShowCmd(out int command); void SetShowCmd(int command);
            void GetIconLocation(IntPtr path, int length, out int index); void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr window, uint flags); void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }
    }
}
