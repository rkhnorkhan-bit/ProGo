using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ProGo.Tests
{
    // Test-only bridge: typed CLR callback and exact signatures, no PowerShell callback scope.
    public static class MenuRenameIdentityProbe
    {
        private const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags HiddenInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class Facts
        {
            internal string Origin;
            internal object Volume, IndexHigh, IndexLow, Creation;
        }
        private static Facts Read(Type shortcutType, SafeFileHandle handle)
        {
            var origin = shortcutType.GetMethod("LegacyOrigin", HiddenStatic, null, new[] { typeof(SafeFileHandle) }, null);
            var infoType = shortcutType.GetNestedType("LegacyFileInfo", BindingFlags.NonPublic);
            if (origin == null || infoType == null) throw new MissingMethodException();
            var read = shortcutType.GetMethod("GetFileInformationByHandle", HiddenStatic, null,
                new[] { typeof(SafeFileHandle), infoType.MakeByRefType() }, null);
            if (read == null) throw new MissingMethodException();
            object[] arguments = { handle, Activator.CreateInstance(infoType) };
            if (!(bool)read.Invoke(null, arguments)) throw new IOException();
            object info = arguments[1];
            return new Facts {
                Origin = (string)origin.Invoke(null, new object[] { handle }),
                Volume = infoType.GetField("Volume", HiddenInstance).GetValue(info),
                IndexHigh = infoType.GetField("IndexHigh", HiddenInstance).GetValue(info),
                IndexLow = infoType.GetField("IndexLow", HiddenInstance).GetValue(info),
                Creation = infoType.GetField("Creation", HiddenInstance).GetValue(info)
            };
        }
        private static string Hash(byte[] bytes)
        {
            using (var algorithm = SHA256.Create()) return Convert.ToBase64String(algorithm.ComputeHash(bytes));
        }
        // No path, hash, file index, timestamps or exception messages leave this method.
        public static string Run(Type shortcutType, string diagnosticRoot)
        {
            string phase = "root-guard";
            var watch = Stopwatch.StartNew();
            int calls = 0;
            bool beforeRead = false, afterRead = false;
            try {
                string full = Path.GetFullPath(diagnosticRoot);
                string leaf = Path.GetFileName(full), parent = Path.GetDirectoryName(full);
                if (!leaf.StartsWith("diagnostic-", StringComparison.Ordinal) || leaf.Length != 43 ||
                    leaf.Substring(11).Trim('0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f').Length != 0 ||
                    !Directory.Exists(parent) || File.Exists(full) || Directory.Exists(full) ||
                    (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException();
                Directory.CreateDirectory(full);
                string install = Path.Combine(full, "app"), programs = Path.Combine(full, "Programs");
                Directory.CreateDirectory(install); Directory.CreateDirectory(programs);
                File.WriteAllText(Path.Combine(install, "ProGo.exe"), "synthetic executable; never run");
                phase = "constructor";
                object manager = Activator.CreateInstance(shortcutType, new object[] { install, programs, Path.Combine(full, "Startup") });
                phase = "initial-install";
                var initial = shortcutType.GetMethod("Install", new[] { typeof(bool), typeof(bool), typeof(bool) });
                if (initial == null) throw new MissingMethodException();
                initial.Invoke(manager, new object[] { true, true, false });
                string primary = Path.Combine(programs, "ProGo", "ProGo.lnk");
                phase = "vacate-primary";
                File.Move(primary, Path.Combine(programs, "ProGo", "ProGo Status.lnk"));
                phase = "resolve-witness";
                Facts before = null;
                Action<SafeFileHandle> witness = delegate(SafeFileHandle handle) {
                    phase = "witness-read";
                    calls++; before = Read(shortcutType, handle); beforeRead = true;
                    phase = "publication-return";
                };
                var ensure = shortcutType.GetMethod("EnsureLink", HiddenInstance, null,
                    new[] { typeof(string), typeof(string), typeof(CancellationToken), typeof(Action<SafeFileHandle>) }, null);
                if (ensure == null || ensure.ReturnType != typeof(byte[])) throw new MissingMethodException();
                phase = "ensure-invoke";
                var created = (byte[])ensure.Invoke(manager, new object[] { primary, "--show", CancellationToken.None, witness });
                phase = "published-handle";
                Facts after;
                using (var stream = new FileStream(primary, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    after = Read(shortcutType, stream.SafeFileHandle); afterRead = true;
                }
                if (calls != 1 || before == null || created == null) throw new InvalidOperationException();
                phase = "compare";
                return "MENU_RENAME_PROBE completed=True; witnessCalls=" + calls + "; beforeRead=" + beforeRead + "; afterRead=" + afterRead +
                    "; originSame=" + (before.Origin == after.Origin) + "; volumeSame=" + Equals(before.Volume, after.Volume) +
                    "; indexSame=" + (Equals(before.IndexHigh, after.IndexHigh) && Equals(before.IndexLow, after.IndexLow)) +
                    "; creationSame=" + Equals(before.Creation, after.Creation) +
                    "; bytesSame=" + (Hash(created) == Hash(File.ReadAllBytes(primary))) + "; elapsedMs=" + watch.ElapsedMilliseconds;
            } catch (Exception error) {
                var result = new StringBuilder("MENU_RENAME_PROBE_ERROR phase=" + phase + "; witnessCalls=" + calls +
                    "; beforeRead=" + beforeRead + "; afterRead=" + afterRead + "; elapsedMs=" + watch.ElapsedMilliseconds);
                // Fixed depth/type/method/IL only; never ToString/Message/Data or source locations.
                for (int depth = 0; error != null && depth < 4; depth++, error = error.InnerException) {
                    result.Append("; error").Append(depth).Append('=').Append(error.GetType().Name);
                    var frames = new StackTrace(error, false).GetFrames();
                    if (frames == null) continue;
                    for (int index = 0; index < Math.Min(3, frames.Length); index++) {
                        var method = frames[index].GetMethod();
                        if (method != null) result.Append("; method=").Append(method.Name).Append("; il=").Append(frames[index].GetILOffset());
                    }
                }
                return result.ToString();
            } finally { watch.Stop(); }
        }
    }
}
