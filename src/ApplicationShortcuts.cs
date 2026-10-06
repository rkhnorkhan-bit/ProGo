using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace ProGo
{
    // Windows shortcuts are the source of truth. Installation must not restore
    // an absent Startup link on an existing installation.
    public sealed class ApplicationShortcuts
    {
        private readonly string root, menu, startup, executable;
        public ApplicationShortcuts(string installDirectory, string programsDirectory, string startupDirectory)
        {
            root = Path.GetFullPath(installDirectory);
            menu = Path.Combine(Path.GetFullPath(programsDirectory), "ProGo");
            startup = Path.Combine(Path.GetFullPath(startupDirectory), "ProGo.lnk");
            executable = Path.Combine(root, "ProGo.exe");
        }

        public static bool IsExistingInstallation(string directory)
        {
            // Check before copying files. Retained user data also counts after
            // an uninstall/repair, so an opt-out is not silently undone.
            return File.Exists(Path.Combine(directory, "ProGo.exe")) ||
                File.Exists(Path.Combine(directory, "VERSION")) ||
                File.Exists(Path.Combine(directory, "settings.json"));
        }

        public sealed class StartupSnapshot
        {
            internal readonly byte[] Bytes;
            internal readonly ApplicationShortcuts Owner;
            public bool Registered { get { return Bytes != null; } }
            internal StartupSnapshot(ApplicationShortcuts owner, byte[] bytes) { Owner = owner; Bytes = bytes; }
        }

        public sealed class StartupChange
        {
            private readonly ApplicationShortcuts owner;
            private readonly byte[] before, after;
            internal StartupChange(ApplicationShortcuts owner, byte[] before, byte[] after)
            { this.owner = owner; this.before = before; this.after = after; }

            public bool TryRollback()
            {
                try {
                    if (!Equal(owner.ReadStartupBytes(), after)) return false;
                    if (before == null) { if (after != null) File.Delete(owner.startup); }
                    else if (after == null) {
                        // Never overwrite a concurrently created shortcut.
                        using (var stream = new FileStream(owner.startup, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            stream.Write(before, 0, before.Length);
                    }
                    return true;
                } catch { return false; }
            }
        }

        public StartupSnapshot ReadStartup()
        {
            if (!File.Exists(executable)) throw new IOException("Installed executable is missing.");
            byte[] bytes = ReadStartupBytes();
            if (bytes != null && (!Matches(startup, "") || !Equal(bytes, ReadStartupBytes())))
                throw new IOException("Startup shortcut was customized or changed. It was preserved.");
            return new StartupSnapshot(this, bytes);
        }

        public StartupChange ChangeStartup(StartupSnapshot expected, bool enabled)
        {
            if (expected == null || expected.Owner != this) throw new ArgumentException("A current startup snapshot is required.");
            var current = ReadStartup();
            if (!Equal(expected.Bytes, current.Bytes)) throw new IOException("Startup changed outside this window. Reopen settings.");
            if (enabled == current.Registered) return new StartupChange(this, current.Bytes, current.Bytes);
            byte[] after = null;
            if (enabled) after = EnsureLink(startup, "");
            else File.Delete(startup);
            return new StartupChange(this, current.Bytes, after);
        }

        private byte[] ReadStartupBytes()
        {
            CheckLocation(startup);
            try {
                using (var stream = new FileStream(startup, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (stream.Length > 65536) throw new IOException("Startup shortcut is too large.");
                    byte[] bytes = new byte[(int)stream.Length];
                    int offset = 0, read;
                    while (offset < bytes.Length && (read = stream.Read(bytes, offset, bytes.Length - offset)) > 0) offset += read;
                    if (offset != bytes.Length) throw new IOException("Startup shortcut could not be read completely.");
                    return bytes;
                }
            } catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            if (left == null || right == null) return left == right;
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        public void Install(bool wasInstalled, bool noStartup, bool noStartMenu)
        {
            if (!File.Exists(executable)) throw new IOException("Installed executable is missing.");
            if (!noStartMenu) {
                EnsureLink(Path.Combine(menu, "ProGo.lnk"), "--show");
                MigrateMenu();
            }
            // Existing links (including disabled/customized ones) are never
            // refreshed: preserve Windows' StartupApproved state and user edits.
            if (!wasInstalled && !noStartup) EnsureLink(startup, "");
        }

        public bool MigrateMenu()
        {
            string legacy = Path.Combine(menu, "ProGo Status.lnk");
            if (!Matches(legacy, "--show")) return false;
            string primary = Path.Combine(menu, "ProGo.lnk");
            // An unrelated/customized primary must not make us delete the only
            // usable old launcher. Create/verify the replacement first.
            EnsureLink(primary, "--show");
            if (!Matches(primary, "--show")) throw new IOException("Main shortcut could not be verified.");
            if (!Matches(legacy, "--show")) return false;
            File.Delete(legacy);
            return true;
        }

        public static bool MigrateForExecutable(string currentExecutable, string installDirectory, string programsDirectory, string startupDirectory)
        {
            if (!String.Equals(Path.GetFullPath(currentExecutable), Path.Combine(Path.GetFullPath(installDirectory), "ProGo.exe"), StringComparison.OrdinalIgnoreCase)) return false;
            return new ApplicationShortcuts(installDirectory, programsDirectory, startupDirectory).MigrateMenu();
        }

        private byte[] EnsureLink(string path, string arguments)
        {
            CheckLocation(path);
            if (File.Exists(path)) {
                if (!Matches(path, arguments)) throw new IOException("An unrelated or customized shortcut already exists. It was preserved.");
                return File.ReadAllBytes(path);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = Path.Combine(Path.GetDirectoryName(path), "progo-" + Guid.NewGuid().ToString("N") + ".lnk");
            object native = null;
            try {
                native = new ShellLink();
                var link = (IShellLinkW)native;
                link.SetPath(executable); link.SetArguments(arguments);
                link.SetWorkingDirectory(root);
                link.SetDescription("ProGo");
                string icon = Path.Combine(root, "ProGo.ico");
                if (File.Exists(icon)) link.SetIconLocation(icon, 0);
                ((IPersistFile)native).Save(temporary, true);
                byte[] bytes = File.ReadAllBytes(temporary);
                // Do not overwrite a shortcut created concurrently.
                File.Move(temporary, path);
                return bytes;
            } finally {
                if (native != null) Marshal.FinalReleaseComObject(native);
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private bool Matches(string path, string arguments)
        {
            CheckLocation(path);
            if (!File.Exists(path)) return false;
            object native = null;
            try {
                native = new ShellLink();
                ((IPersistFile)native).Load(path, 0x40); // read, share-deny-none
                var link = (IShellLinkW)native;
                var target = new StringBuilder(32768); var actualArguments = new StringBuilder(32768);
                link.GetPath(target, target.Capacity, IntPtr.Zero, 4); // raw path, no resolution/network lookup
                link.GetArguments(actualArguments, actualArguments.Capacity);
                return target.Length > 0 && String.Equals(Path.GetFullPath(target.ToString()), executable, StringComparison.OrdinalIgnoreCase) &&
                    String.Equals(actualArguments.ToString().Trim(), arguments, StringComparison.OrdinalIgnoreCase);
            } finally { if (native != null) Marshal.FinalReleaseComObject(native); }
        }

        private static void CheckLocation(string path)
        {
            if (Directory.Exists(path)) throw new IOException("A directory occupies the shortcut path.");
            if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked shortcuts are not modified.");
            // A redirected Windows Programs/Startup parent is legitimate, but
            // do not follow an extra link at the application-owned directory.
            string parent = Path.GetDirectoryName(path);
            if (Directory.Exists(parent) && (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked shortcut directories are not modified.");
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }
        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int length, IntPtr data, uint flags);
            void GetIDList(out IntPtr list);
            void SetIDList(IntPtr list);
            void GetDescription(IntPtr description, int length);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string description);
            void GetWorkingDirectory(IntPtr directory, int length);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int length);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
            void GetHotkey(out short hotkey);
            void SetHotkey(short hotkey);
            void GetShowCmd(out int command);
            void SetShowCmd(int command);
            void GetIconLocation(IntPtr path, int length, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr window, uint flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }
    }
}
