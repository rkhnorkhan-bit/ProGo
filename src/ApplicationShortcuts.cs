using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Win32.SafeHandles;

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
            if (enabled) {
                after = EnsureLink(startup, "");
                if (after == null) throw new IOException("Startup was added concurrently. Reopen settings.");
            }
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

        // Durable receipt contains identity and hashes only, never shortcut bytes or user paths.
        public sealed class StartupInstallEvidence
        {
            private StartupInstallEvidence() { }
            public int Schema { get { return 1; } }
            public string InstallIdentity { get; private set; }
            public string LocationIdentity { get; private set; }
            public string Disposition { get; private set; }
            public string OriginIdentity { get; private set; }
            public MenuInstallEvidence Menu { get; private set; }
            public LegacyInstallEvidence Legacy { get; private set; }
            public bool BeforeExists { get; private set; }
            public string BeforeHash { get; private set; }
            public bool Exists { get; private set; }
            public string Hash { get; private set; }
            internal StartupInstallEvidence(string install, string path, byte[] before, byte[] after, bool created, string origin, MenuInstallEvidence menuEvidence, LegacyInstallEvidence legacyEvidence)
            {
                InstallIdentity = Identity(install); LocationIdentity = Identity(path); Menu = menuEvidence; Legacy = legacyEvidence;
                Disposition = created ? "created" : "preserved"; OriginIdentity = origin;
                BeforeExists = before != null; BeforeHash = Digest(before);
                Exists = after != null; Hash = Digest(after);
            }
        }

        public sealed class MenuInstallEvidence
        {
            private MenuInstallEvidence() { }
            public int Schema { get { return 1; } }
            public string InstallIdentity { get; private set; }
            public string LocationIdentity { get; private set; }
            public string Disposition { get; private set; }
            public string OriginIdentity { get; private set; }
            public bool Requested { get; private set; }
            public bool BeforeExists { get; private set; }
            public string BeforeHash { get; private set; }
            public bool Exists { get; private set; }
            public string Hash { get; private set; }
            internal MenuInstallEvidence(string install, string path, byte[] before, byte[] after, bool created, bool requested, string origin)
            {
                InstallIdentity = Identity(install); LocationIdentity = Identity(path);
                Disposition = created ? "created" : "preserved"; Requested = requested; OriginIdentity = origin;
                BeforeExists = before != null; BeforeHash = Digest(before);
                Exists = after != null; Hash = Digest(after);
            }
        }

        private byte[] ReadMenuBytes()
        {
            string path = Path.Combine(menu, "ProGo.lnk");
            CheckLocation(path);
            try {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (stream.Length > 65536) throw new IOException("Menu shortcut is too large.");
                    byte[] bytes = new byte[(int)stream.Length];
                    int offset = 0, read;
                    while (offset < bytes.Length && (read = stream.Read(bytes, offset, bytes.Length - offset)) > 0) offset += read;
                    if (offset != bytes.Length) throw new IOException("Menu shortcut could not be read completely.");
                    return bytes;
                }
            } catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        // Primary Menu evidence only. Legacy migration evidence is a separate prerequisite.
        public bool VerifyMenuInstallEvidence(MenuInstallEvidence evidence)
        {
            try {
                string path = Path.Combine(menu, "ProGo.lnk");
                if (evidence == null || evidence.Schema != 1 || evidence.InstallIdentity != Identity(root) ||
                    evidence.LocationIdentity != Identity(path)) return false;
                if (evidence.Disposition == "preserved") {
                    if (evidence.BeforeExists != evidence.Exists || evidence.BeforeHash != evidence.Hash) return false;
                } else if (evidence.Disposition == "created") {
                    if (!evidence.Requested || evidence.BeforeExists || evidence.BeforeHash != null || !evidence.Exists) return false;
                } else return false;
                if (evidence.Requested && (!evidence.Exists || !Matches(path, "--show"))) return false;
                byte[] current = ReadMenuBytes();
                return evidence.Exists == (current != null) && evidence.Hash == Digest(current);
            } catch { return false; }
        }

        public sealed class LegacyInstallEvidence
        {
            private LegacyInstallEvidence() { }
            public int Schema { get { return 1; } }
            public string InstallIdentity { get; private set; }
            public string LocationIdentity { get; private set; }
            public string Disposition { get; private set; }
            public bool Requested { get; private set; }
            public bool BeforeExists { get; private set; }
            public string BeforeHash { get; private set; }
            public string OriginIdentity { get; private set; }
            public bool BeforeMatched { get; private set; }
            public bool Exists { get; private set; }
            public string Hash { get; private set; }
            public string OwnedHash { get; private set; }
            public string ReplacementHash { get; private set; }
            internal LegacyInstallEvidence(string install, string path, LegacyObservation before, LegacyObservation after, bool requested, bool removed, string replacementHash)
            {
                InstallIdentity = Identity(install); LocationIdentity = Identity(path);
                Disposition = removed ? "removed" : "preserved"; Requested = requested;
                BeforeExists = before.Bytes != null; BeforeHash = Digest(before.Bytes);
                OriginIdentity = before.Origin; BeforeMatched = before.Matched;
                Exists = after.Bytes != null; Hash = Digest(after.Bytes);
                OwnedHash = removed ? BeforeHash : null; ReplacementHash = replacementHash;
            }
        }

        internal sealed class LegacyObservation
        {
            internal byte[] Bytes;
            internal string Origin;
            internal bool Matched;
        }

        private static byte[] ReadLegacyEvidenceBytes(FileStream stream)
        {
            if (stream.Length > 65536) throw new IOException("Legacy shortcut is too large.");
            byte[] bytes = new byte[(int)stream.Length];
            int offset = 0, read;
            while (offset < bytes.Length && (read = stream.Read(bytes, offset, bytes.Length - offset)) > 0) offset += read;
            if (offset != bytes.Length) throw new IOException("Legacy shortcut could not be read completely.");
            return bytes;
        }

        private static string LegacyOrigin(SafeFileHandle handle)
        {
            LegacyFileInfo info;
            if (!GetFileInformationByHandle(handle, out info) || (info.Attributes & 0x410) != 0)
                throw new IOException("Legacy file identity is unavailable or linked.");
            string value = info.Volume.ToString("X8") + info.IndexHigh.ToString("X8") + info.IndexLow.ToString("X8") +
                info.Creation.dwHighDateTime.ToString("X8") + info.Creation.dwLowDateTime.ToString("X8");
            return Digest(Encoding.UTF8.GetBytes(value));
        }

        private LegacyObservation ObserveLegacy(bool match)
        {
            string path = Path.Combine(menu, "ProGo Status.lnk"); CheckLocation(path);
            try {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    var observation = new LegacyObservation { Bytes = ReadLegacyEvidenceBytes(stream), Origin = LegacyOrigin(stream.SafeFileHandle) };
                    // The read witness denies changes while the original native semantic oracle reads.
                    observation.Matched = match && Matches(path, "--show");
                    return observation;
                }
            } catch (FileNotFoundException) { return new LegacyObservation(); }
            catch (DirectoryNotFoundException) { return new LegacyObservation(); }
        }

        // DELETE access is used only for this existing file handle, never a path-based delete.
        private void DeleteLegacyOwned(byte[] expected, string origin)
        {
            string path = Path.Combine(menu, "ProGo Status.lnk"); CheckLocation(path);
            using (var handle = OpenLegacyForDisposition(path, 0x80010000u, 1, IntPtr.Zero, 3, 0x00200080u, IntPtr.Zero)) {
                if (handle.IsInvalid) throw new IOException("Legacy deletion ownership could not be acquired.");
                using (var stream = new FileStream(handle, FileAccess.Read)) {
                    if (LegacyOrigin(handle) != origin || !Equal(expected, ReadLegacyEvidenceBytes(stream)))
                        throw new IOException("Legacy shortcut changed before deletion admission. It was preserved.");
                    var disposition = new LegacyDisposition { DeleteFile = 1 };
                    if (!SetFileInformationByHandle(handle, 4, ref disposition, 1))
                        throw new IOException("Owned legacy deletion was not accepted.");
                }
            }
        }

        private LegacyInstallEvidence MigrateLegacyWithEvidence(bool requested)
        {
            string path = Path.Combine(menu, "ProGo Status.lnk");
            var before = ObserveLegacy(requested); bool removed = false; string replacement = null;
            if (requested && before.Matched) {
                string primary = Path.Combine(menu, "ProGo.lnk"); CheckLocation(primary);
                // Existing install already ensured primary; pin its verified bytes until legacy deletion settles.
                using (var stream = new FileStream(primary, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    byte[] bytes = ReadLegacyEvidenceBytes(stream);
                    if (!Matches(primary, "--show")) throw new IOException("Main shortcut could not be verified.");
                    replacement = Digest(bytes);
                    DeleteLegacyOwned(before.Bytes, before.Origin);
                    removed = true;
                }
            }
            var after = ObserveLegacy(false);
            if (removed ? after.Bytes != null : (!Equal(before.Bytes, after.Bytes) || before.Origin != after.Origin))
                throw new IOException("Legacy completion changed. Current state was preserved.");
            return new LegacyInstallEvidence(root, path, before, after, requested, removed, replacement);
        }

        public bool VerifyLegacyInstallEvidence(LegacyInstallEvidence evidence)
        {
            try {
                if (evidence == null || evidence.Schema != 1 || evidence.InstallIdentity != Identity(root) ||
                    evidence.LocationIdentity != Identity(Path.Combine(menu, "ProGo Status.lnk"))) return false;
                if (evidence.Disposition == "removed") {
                    if (!evidence.Requested || !evidence.BeforeMatched || !evidence.BeforeExists || evidence.Exists || evidence.Hash != null ||
                        evidence.OriginIdentity == null || evidence.OriginIdentity.Length != 64 || evidence.BeforeHash == null ||
                        evidence.OwnedHash != evidence.BeforeHash || evidence.ReplacementHash == null ||
                        evidence.ReplacementHash != Digest(ReadMenuBytes()) || !Matches(Path.Combine(menu, "ProGo.lnk"), "--show")) return false;
                } else if (evidence.Disposition == "preserved") {
                    if (evidence.OwnedHash != null || evidence.ReplacementHash != null || evidence.BeforeExists != evidence.Exists ||
                        evidence.BeforeHash != evidence.Hash) return false;
                } else return false;
                var current = ObserveLegacy(false);
                return evidence.Exists == (current.Bytes != null) && evidence.Hash == Digest(current.Bytes) &&
                    (evidence.Disposition == "removed" || evidence.OriginIdentity == current.Origin);
            } catch { return false; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LegacyFileInfo
        {
            internal uint Attributes;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Creation, Access, Write;
            internal uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct LegacyDisposition { internal byte DeleteFile; }
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern SafeFileHandle OpenLegacyForDisposition(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out LegacyFileInfo info);
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int kind, ref LegacyDisposition info, uint size);

        private static string Digest(byte[] bytes)
        {
            if (bytes == null) return null;
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }
        private static string Identity(string path)
        { return Digest(Encoding.UTF8.GetBytes(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant())); }

        // Read-only verification against THIS caller's expected install and Startup location.
        public bool VerifyStartupInstallEvidence(StartupInstallEvidence evidence)
        {
            try {
                if (evidence == null || evidence.Schema != 1 || evidence.InstallIdentity != Identity(root) ||
                    evidence.LocationIdentity != Identity(startup)) return false;
                if (evidence.Disposition == "preserved") {
                    if (evidence.BeforeExists != evidence.Exists || evidence.BeforeHash != evidence.Hash) return false;
                } else if (evidence.Disposition == "created") {
                    if (evidence.BeforeExists || evidence.BeforeHash != null || !evidence.Exists ||
                        evidence.OriginIdentity == null || evidence.OriginIdentity.Length != 64 || !Matches(startup, "")) return false;
                    CheckLocation(startup);
                    using (var stream = new FileStream(startup, FileMode.Open, FileAccess.Read, FileShare.Read))
                        return evidence.OriginIdentity == LegacyOrigin(stream.SafeFileHandle) && evidence.Hash == Digest(ReadLegacyEvidenceBytes(stream));
                } else return false;
                byte[] current = ReadStartupBytes();
                return evidence.Exists == (current != null) && evidence.Hash == Digest(current);
            } catch { return false; }
        }

        // Deserialize only fixed receipt fields; no path or operation is supplied by a receipt.
        private static T ReadDurableEvidence<T>(System.Collections.IDictionary values) where T : class
        {
            if (values == null) throw new InvalidDataException("Auxiliary receipt is missing.");
            var result = (T)Activator.CreateInstance(typeof(T), true);
            int count = 0;
            foreach (var property in typeof(T).GetProperties()) {
                if (property.Name == "Menu" || property.Name == "Legacy") continue;
                count++;
                if (!values.Contains(property.Name)) throw new InvalidDataException("Auxiliary receipt field is missing.");
                object value = values[property.Name];
                if (property.Name == "Schema") {
                    if (!(value is int || value is long) || Convert.ToInt64(value) != 1) throw new InvalidDataException("Auxiliary receipt schema is invalid.");
                    continue;
                }
                if (value == null ? property.PropertyType != typeof(string) : value.GetType() != property.PropertyType)
                    throw new InvalidDataException("Auxiliary receipt field type is invalid.");
                if (property.Name.EndsWith("Hash", StringComparison.Ordinal) || property.Name.EndsWith("Identity", StringComparison.Ordinal)) {
                    string hash = value as string;
                    if (hash != null && (hash.Length != 64 || hash.Trim('0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F').Length != 0))
                        throw new InvalidDataException("Auxiliary receipt digest is invalid.");
                }
                property.SetValue(result, value, null);
            }
            if (values.Count != count) throw new InvalidDataException("Auxiliary receipt has unknown fields.");
            foreach (string prefix in new[] { "Before", "" }) {
                bool exists = (bool)typeof(T).GetProperty(prefix + "Exists").GetValue(result, null);
                string hash = (string)typeof(T).GetProperty(prefix + "Hash").GetValue(result, null);
                if (exists != (hash != null)) throw new InvalidDataException("Auxiliary receipt presence is inconsistent.");
            }
            return result;
        }

        // Read-only. The caller supplies independently trusted locations through this manager.
        // A positive result is observational evidence, never authority to mutate or publish.
        public bool VerifyDurableInstallEvidence(System.Collections.IDictionary startupReceipt,
            System.Collections.IDictionary menuReceipt, System.Collections.IDictionary legacyReceipt)
        {
            try {
                var startupEvidence = ReadDurableEvidence<StartupInstallEvidence>(startupReceipt);
                var menuEvidence = ReadDurableEvidence<MenuInstallEvidence>(menuReceipt);
                var legacyEvidence = ReadDurableEvidence<LegacyInstallEvidence>(legacyReceipt);
                return VerifyStartupInstallEvidence(startupEvidence) && VerifyMenuInstallEvidence(menuEvidence) &&
                    VerifyLegacyInstallEvidence(legacyEvidence) &&
                    (legacyEvidence.Disposition != "removed" || legacyEvidence.ReplacementHash == menuEvidence.Hash);
            } catch { return false; }
        }

        // Read-only Menu rollback prerequisite. Byte equality alone cannot establish origin.
        public bool VerifyDurableMenuRollbackEvidence(System.Collections.IDictionary receipt)
        {
            try {
                var evidence = ReadDurableEvidence<MenuInstallEvidence>(receipt);
                if (!VerifyMenuInstallEvidence(evidence)) return false;
                if (evidence.Disposition == "preserved") return evidence.OriginIdentity == null;
                if (evidence.OriginIdentity == null || evidence.OriginIdentity.Length != 64) return false;
                string path = Path.Combine(menu, "ProGo.lnk");
                CheckLocation(path);
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    return evidence.OriginIdentity == LegacyOrigin(stream.SafeFileHandle) &&
                        evidence.Hash == Digest(ReadLegacyEvidenceBytes(stream));
                }
            } catch { return false; }
        }

        // Observe only the real Startup original state; does not prove who removed a link.
        public bool VerifyDurableStartupOriginalResult(System.Collections.IDictionary startupReceipt,
            System.Collections.IDictionary menuReceipt, System.Collections.IDictionary legacyReceipt)
        {
            try {
                var evidence = ReadDurableEvidence<StartupInstallEvidence>(startupReceipt);
                var menuEvidence = ReadDurableEvidence<MenuInstallEvidence>(menuReceipt);
                var legacyEvidence = ReadDurableEvidence<LegacyInstallEvidence>(legacyReceipt);
                if (!VerifyMenuInstallEvidence(menuEvidence) || !VerifyLegacyInstallEvidence(legacyEvidence) ||
                    (legacyEvidence.Disposition == "removed" && legacyEvidence.ReplacementHash != menuEvidence.Hash)) return false;
                if (evidence.Disposition == "preserved") return VerifyStartupInstallEvidence(evidence);
                return evidence.Disposition == "created" && evidence.InstallIdentity == Identity(root) &&
                    evidence.LocationIdentity == Identity(startup) && !evidence.BeforeExists && evidence.BeforeHash == null &&
                    evidence.Exists && evidence.Hash != null && evidence.OriginIdentity != null &&
                    ReadStartupBytes() == null;
            } catch { return false; }
        }

        // Startup only: final validation/deletion on one exact owned handle, no path-based delete.
        public bool TryRollbackDurableStartup(System.Collections.IDictionary receipt, CancellationToken token)
        {
            try {
                token.ThrowIfCancellationRequested();
                var evidence = ReadDurableEvidence<StartupInstallEvidence>(receipt);
                if (!VerifyStartupInstallEvidence(evidence)) return false;
                if (evidence.Disposition == "preserved") return true;
                CheckLocation(startup);
                using (var handle = OpenLegacyForDisposition(startup, 0x80010000u, 1, IntPtr.Zero, 3, 0x00200080u, IntPtr.Zero)) {
                    if (handle.IsInvalid) return false;
                    using (var stream = new FileStream(handle, FileAccess.Read)) {
                        if (evidence.OriginIdentity != LegacyOrigin(handle) || evidence.Hash != Digest(ReadLegacyEvidenceBytes(stream))) return false;
                        token.ThrowIfCancellationRequested();
                        var disposition = new LegacyDisposition { DeleteFile = 1 };
                        if (!SetFileInformationByHandle(handle, 4, ref disposition, 1)) return false;
                    }
                }
                // Cancellation after mutation cannot skip truthful settlement/result verification.
                return ReadStartupBytes() == null;
            } catch (OperationCanceledException) { throw; }
            catch { return false; }
        }

        public void Install(bool wasInstalled, bool noStartup, bool noStartMenu)
        { InstallCore(wasInstalled, noStartup, noStartMenu, false); }

        public StartupInstallEvidence InstallWithStartupEvidence(bool wasInstalled, bool noStartup, bool noStartMenu)
        {
            var evidence = InstallCore(wasInstalled, noStartup, noStartMenu, true);
            if (!VerifyStartupInstallEvidence(evidence) || !VerifyMenuInstallEvidence(evidence.Menu) || !VerifyLegacyInstallEvidence(evidence.Legacy)) throw new IOException("Startup completion evidence changed.");
            return evidence;
        }

        private StartupInstallEvidence InstallCore(bool wasInstalled, bool noStartup, bool noStartMenu, bool captureStartup)
        {
            if (!File.Exists(executable)) throw new IOException("Installed executable is missing.");
            byte[] before = captureStartup ? ReadStartupBytes() : null;
            byte[] menuBefore = captureStartup ? ReadMenuBytes() : null;
            byte[] menuCreated = null;
            string menuCreatedOrigin = null;
            if (!noStartMenu) {
                menuCreated = captureStartup
                    ? EnsureLink(Path.Combine(menu, "ProGo.lnk"), "--show", CancellationToken.None, handle => menuCreatedOrigin = LegacyOrigin(handle))
                    : EnsureLink(Path.Combine(menu, "ProGo.lnk"), "--show");
                if (!captureStartup) MigrateMenu();
            }
            var legacyEvidence = captureStartup ? MigrateLegacyWithEvidence(!noStartMenu) : null;
            // Existing links (including disabled/customized ones) are never
            // refreshed: preserve Windows' StartupApproved state and user edits.
            byte[] created = null;
            string createdOrigin = null;
            if (!wasInstalled && !noStartup) created = captureStartup
                ? EnsureLink(startup, "", CancellationToken.None, handle => createdOrigin = LegacyOrigin(handle))
                : EnsureLink(startup, "");
            if (!captureStartup) return null;
            byte[] after = ReadStartupBytes();
            if (created == null ? !Equal(before, after) :
                (before != null || !Equal(created, after) || !Matches(startup, "") || !Equal(after, ReadStartupBytes())))
                throw new IOException("Startup completion could not be verified. Current state was preserved.");
            byte[] menuAfter = ReadMenuBytes();
            if (menuCreated == null ? !Equal(menuBefore, menuAfter) :
                (menuBefore != null || !Equal(menuCreated, menuAfter)))
                throw new IOException("Menu completion could not be verified. Current state was preserved.");
            var menuEvidence = new MenuInstallEvidence(root, Path.Combine(menu, "ProGo.lnk"), menuBefore, menuAfter, menuCreated != null, !noStartMenu, menuCreatedOrigin);
            return new StartupInstallEvidence(root, startup, before, after, created != null, createdOrigin, menuEvidence, legacyEvidence);
        }

        public bool MigrateMenu()
        { return MigrateMenu(CancellationToken.None); }

        internal bool MigrateMenu(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string legacy = Path.Combine(menu, "ProGo Status.lnk");
            if (!Matches(legacy, "--show")) return false;
            token.ThrowIfCancellationRequested();
            string primary = Path.Combine(menu, "ProGo.lnk");
            // An unrelated/customized primary must not make us delete the only
            // usable old launcher. Create/verify the replacement first.
            EnsureLink(primary, "--show", token);
            if (!Matches(primary, "--show")) throw new IOException("Main shortcut could not be verified.");
            if (!Matches(legacy, "--show")) return false;
            token.ThrowIfCancellationRequested();
            File.Delete(legacy);
            return true;
        }

        public static bool MigrateForExecutable(string currentExecutable, string installDirectory, string programsDirectory, string startupDirectory)
        { return MigrateForExecutable(currentExecutable, installDirectory, programsDirectory, startupDirectory, CancellationToken.None); }

        internal static bool MigrateForExecutable(string currentExecutable, string installDirectory, string programsDirectory, string startupDirectory, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!String.Equals(Path.GetFullPath(currentExecutable), Path.Combine(Path.GetFullPath(installDirectory), "ProGo.exe"), StringComparison.OrdinalIgnoreCase)) return false;
            return new ApplicationShortcuts(installDirectory, programsDirectory, startupDirectory).MigrateMenu(token);
        }

        private byte[] EnsureLink(string path, string arguments)
        { return EnsureLink(path, arguments, CancellationToken.None); }

        private byte[] EnsureLink(string path, string arguments, CancellationToken token)
        { return EnsureLink(path, arguments, token, null); }

        private byte[] EnsureLink(string path, string arguments, CancellationToken token, Action<SafeFileHandle> witness)
        {
            token.ThrowIfCancellationRequested();
            CheckLocation(path);
            if (File.Exists(path)) {
                if (!Matches(path, arguments)) throw new IOException("An unrelated or customized shortcut already exists. It was preserved.");
                return null; // An existing link was not created by this operation.
            }
            token.ThrowIfCancellationRequested();
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
                token.ThrowIfCancellationRequested();
                ((IPersistFile)native).Save(temporary, true);
                byte[] bytes = File.ReadAllBytes(temporary);
                // Do not overwrite a shortcut created concurrently.
                token.ThrowIfCancellationRequested();
                if (witness == null) File.Move(temporary, path);
                else using (var stream = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)) {
                    if (!Equal(bytes, ReadLegacyEvidenceBytes(stream))) throw new IOException("Created shortcut changed before publication.");
                    witness(stream.SafeFileHandle);
                    File.Move(temporary, path);
                }
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
