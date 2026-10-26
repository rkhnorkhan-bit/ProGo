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

        // Capture-only prerequisite: no disk publication, restore authority or automatic integration.
        public sealed class LegacyRollbackSource : IDisposable
        {
            internal readonly ApplicationShortcuts Owner;
            private readonly object gate = new object();
            private byte[] bytes;
            public string InstallIdentity { get; private set; }
            public string LocationIdentity { get; private set; }
            public string OriginIdentity { get; private set; }
            public string Hash { get; private set; }
            internal LegacyRollbackSource(ApplicationShortcuts owner, byte[] original, string origin)
            {
                Owner = owner; bytes = (byte[])original.Clone(); OriginIdentity = origin;
                InstallIdentity = Identity(owner.root);
                LocationIdentity = Identity(Path.Combine(owner.menu, "ProGo Status.lnk"));
                Hash = Digest(bytes);
            }
            internal byte[] CopyBytes()
            {
                lock (gate) { return bytes == null || Digest(bytes) != Hash ? null : (byte[])bytes.Clone(); }
            }
            public void Dispose()
            {
                lock (gate) { if (bytes != null) { Array.Clear(bytes, 0, bytes.Length); bytes = null; } }
            }
        }

        public LegacyRollbackSource CaptureLegacyRollbackSource(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string path = Path.Combine(menu, "ProGo Status.lnk"); CheckLocation(path);
            try {
                // Share.Read pins this exact object against write/delete/rename through the semantic oracle.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    string origin = LegacyOrigin(stream.SafeFileHandle);
                    byte[] bytes = ReadLegacyEvidenceBytes(stream);
                    if (!Matches(path, "--show")) return null; // custom/foreign does not become owned
                    token.ThrowIfCancellationRequested();
                    stream.Position = 0;
                    if (origin != LegacyOrigin(stream.SafeFileHandle) || !Equal(bytes, ReadLegacyEvidenceBytes(stream)))
                        throw new IOException("Legacy capture changed during verification.");
                    CheckLocation(path);
                    using (var current = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        if (origin != LegacyOrigin(current.SafeFileHandle) || !Equal(bytes, ReadLegacyEvidenceBytes(current)))
                            throw new IOException("Legacy capture no longer identifies the current object.");
                        token.ThrowIfCancellationRequested();
                        return new LegacyRollbackSource(this, bytes, origin);
                    }
                }
            } catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        // Observation only. Caller still needs durable source, snapshot and full installer context before restore.
        public byte[] ReadVerifiedLegacyRollbackBytes(LegacyRollbackSource source, LegacyInstallEvidence receipt)
        {
            if (source == null || source.Owner != this || receipt == null ||
                source.InstallIdentity != Identity(root) || source.LocationIdentity != Identity(Path.Combine(menu, "ProGo Status.lnk")) ||
                receipt.Disposition != "removed" || receipt.InstallIdentity != source.InstallIdentity ||
                receipt.LocationIdentity != source.LocationIdentity || receipt.OriginIdentity != source.OriginIdentity ||
                receipt.BeforeHash != source.Hash || receipt.OwnedHash != source.Hash || !VerifyLegacyInstallEvidence(receipt)) return null;
            byte[] bytes = source.CopyBytes();
            return bytes != null && Digest(bytes) == source.Hash ? bytes : null;
        }

        // Immutable pointer to a verified new durable file; not restore or terminal authority.
        public sealed class LegacyDurableSourceEvidence
        {
            public bool MutationAllowed { get { return false; } }
            public bool FullRollback { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            public Guid Operation { get; private set; }
            public string SnapshotHash { get; private set; }
            public string InstallIdentity { get; private set; }
            public string LocationIdentity { get; private set; }
            public string OriginIdentity { get; private set; }
            public string Hash { get; private set; }
            public string StorageOrigin { get; private set; }
            public string DocumentHash { get; private set; }
            internal LegacyDurableSourceEvidence(LegacyRollbackSource source, Guid operation, string snapshot, string storage, byte[] document)
            {
                Operation = operation; SnapshotHash = snapshot; InstallIdentity = source.InstallIdentity;
                LocationIdentity = source.LocationIdentity; OriginIdentity = source.OriginIdentity; Hash = source.Hash;
                StorageOrigin = storage; DocumentHash = Digest(document);
            }
        }
        private static bool LegacySourceHash(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (char c in value) if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }
        private static SafeFileHandle PinLegacySourceDirectory(string directory)
        {
            // Pin exact non-reparse directory against rename/delete while creating/opening the child.
            var handle = OpenLegacyForDisposition(directory, 0, 1, IntPtr.Zero, 3, 0x02200000u, IntPtr.Zero);
            LegacyFileInfo info;
            if (handle.IsInvalid || !GetFileInformationByHandle(handle, out info) || (info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0) {
                handle.Dispose(); throw new IOException("Legacy source directory is unavailable or redirected.");
            }
            return handle;
        }
        private string LegacySourcePath(Guid operation)
        {
            if (operation == Guid.Empty) throw new IOException("Legacy source operation is missing.");
            return Path.Combine(root, "backups", "installer-legacy-source-" + operation.ToString("N") + ".bin");
        }
        private static byte[] LegacySourceDocument(Guid operation, string snapshot, string install, string location,
            string origin, string hash, string storage, byte[] bytes)
        {
            if (!LegacySourceHash(snapshot) || !LegacySourceHash(install) || !LegacySourceHash(location) ||
                !LegacySourceHash(origin) || !LegacySourceHash(hash) || !LegacySourceHash(storage) || bytes == null ||
                bytes.Length < 1 || bytes.Length > 65536 || Digest(bytes) != hash) throw new IOException("Legacy source binding is invalid.");
            byte[] header = Encoding.ASCII.GetBytes("PROGO-LEGACY-SOURCE-1\n" + operation.ToString("N") + "\n" + snapshot + "\n" +
                install + "\n" + location + "\n" + origin + "\n" + hash + "\n" + storage + "\n" + bytes.Length.ToString("X8") + "\n");
            var document = new byte[header.Length + bytes.Length];
            Buffer.BlockCopy(header, 0, document, 0, header.Length); Buffer.BlockCopy(bytes, 0, document, header.Length, bytes.Length);
            return document;
        }
        private static byte[] ReadLegacySourceDocument(FileStream stream)
        {
            if (stream.Length < 1 || stream.Length > 70000) throw new IOException("Legacy source document size is invalid.");
            byte[] bytes = new byte[(int)stream.Length]; int offset = 0, read;
            while (offset < bytes.Length && (read = stream.Read(bytes, offset, bytes.Length - offset)) > 0) offset += read;
            if (offset != bytes.Length) throw new IOException("Legacy source document is incomplete.");
            return bytes;
        }
        private void AssertLegacySourceOriginal(LegacyRollbackSource source, FileStream original, byte[] bytes)
        {
            original.Position = 0;
            if (source.OriginIdentity != LegacyOrigin(original.SafeFileHandle) || !Equal(bytes, ReadLegacyEvidenceBytes(original)) ||
                source.Hash != Digest(bytes) || !Matches(Path.Combine(menu, "ProGo Status.lnk"), "--show"))
                throw new IOException("Legacy original no longer matches capture.");
        }
        public LegacyDurableSourceEvidence PersistLegacyRollbackSource(LegacyRollbackSource source, Guid operation,
            string snapshotHash, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (source == null || source.Owner != this || !LegacySourceHash(snapshotHash)) throw new IOException("Legacy source context is invalid.");
            string path = LegacySourcePath(operation), originalPath = Path.Combine(menu, "ProGo Status.lnk");
            byte[] bytes = source.CopyBytes();
            if (bytes == null) throw new IOException("Legacy source is disposed or changed.");
            using (var rootPin = PinLegacySourceDirectory(root))
            using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
            using (var menuPin = PinLegacySourceDirectory(menu)) {
                CheckLocation(originalPath); CheckLocation(path);
                using (var original = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    AssertLegacySourceOriginal(source, original, bytes); token.ThrowIfCancellationRequested();
                    // Never reuse an existing/partial/foreign destination, even when its bytes match.
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                        string storage = LegacyOrigin(stream.SafeFileHandle);
                        byte[] document = LegacySourceDocument(operation, snapshotHash, source.InstallIdentity, source.LocationIdentity,
                            source.OriginIdentity, source.Hash, storage, bytes);
                        stream.Write(document, 0, document.Length); stream.Flush(true); stream.Position = 0;
                        if (storage != LegacyOrigin(stream.SafeFileHandle) || !Equal(document, ReadLegacySourceDocument(stream)))
                            throw new IOException("Durable Legacy source did not verify.");
                        AssertLegacySourceOriginal(source, original, bytes); CheckLocation(path);
                        using (var current = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                            if (storage != LegacyOrigin(current.SafeFileHandle) || !Equal(document, ReadLegacySourceDocument(current)))
                                throw new IOException("Durable Legacy source publication changed.");
                        }
                        token.ThrowIfCancellationRequested();
                        return new LegacyDurableSourceEvidence(source, operation, snapshotHash, storage, document);
                    }
                }
            }
        }
        public byte[] ReadDurableLegacyRollbackBytes(LegacyDurableSourceEvidence proof, Guid operation,
            string snapshotHash, LegacyInstallEvidence receipt)
        {
            try {
                if (proof == null || receipt == null || operation == Guid.Empty || proof.Operation != operation ||
                    !LegacySourceHash(snapshotHash) || proof.SnapshotHash != snapshotHash || proof.InstallIdentity != Identity(root) ||
                    proof.LocationIdentity != Identity(Path.Combine(menu, "ProGo Status.lnk")) || receipt.Disposition != "removed" ||
                    receipt.OriginIdentity != proof.OriginIdentity || receipt.BeforeHash != proof.Hash || receipt.OwnedHash != proof.Hash ||
                    !VerifyLegacyInstallEvidence(receipt)) return null;
                using (var rootPin = PinLegacySourceDirectory(root))
                using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
                using (var menuPin = PinLegacySourceDirectory(menu)) {
                    string path = LegacySourcePath(operation); CheckLocation(path);
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        if (proof.StorageOrigin != LegacyOrigin(stream.SafeFileHandle)) return null;
                        byte[] document = ReadLegacySourceDocument(stream);
                        if (Digest(document) != proof.DocumentHash) return null;
                        int headerLength = Encoding.ASCII.GetByteCount("PROGO-LEGACY-SOURCE-1\n") + 33 + 6 * 65 + 9;
                        if (document.Length <= headerLength || document.Length - headerLength > 65536) return null;
                        byte[] bytes = new byte[document.Length - headerLength];
                        Buffer.BlockCopy(document, headerLength, bytes, 0, bytes.Length);
                        byte[] expected = LegacySourceDocument(operation, snapshotHash, proof.InstallIdentity, proof.LocationIdentity,
                            proof.OriginIdentity, proof.Hash, proof.StorageOrigin, bytes);
                        if (!Equal(document, expected) || proof.StorageOrigin != LegacyOrigin(stream.SafeFileHandle) ||
                            !VerifyLegacyInstallEvidence(receipt)) return null;
                        return bytes;
                    }
                }
            } catch { return null; }
        }

        // Installer-only prerequisite: an immutable reference witness, never restore/terminal authority.
        public sealed class LegacyJournalReferenceEvidence
        {
            public LegacyDurableSourceEvidence Source { get; private set; }
            public string Origin { get; private set; }
            public string Hash { get; private set; }
            public bool MutationAllowed { get { return false; } }
            public bool FullRollback { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            internal LegacyJournalReferenceEvidence(LegacyDurableSourceEvidence source, string origin, byte[] bytes)
            { Source = source; Origin = origin; Hash = Digest(bytes); }
        }
        private static byte[] ReadLegacyReferenceBytes(FileStream stream)
        {
            if (stream.Length < 1 || stream.Length > 1048576) throw new IOException("Legacy reference size is invalid.");
            byte[] bytes = new byte[(int)stream.Length]; int offset = 0, count;
            while (offset < bytes.Length && (count = stream.Read(bytes, offset, bytes.Length - offset)) > 0) offset += count;
            if (offset != bytes.Length) throw new IOException("Legacy reference read is incomplete.");
            return bytes;
        }
        private string LegacyReferencePath(Guid operation)
        {
            if (operation == Guid.Empty) throw new IOException("Legacy reference operation is missing.");
            return Path.Combine(root, "backups", "installer-legacy-reference-" + operation.ToString("N") + ".json");
        }
        // A pre-delete observation, independently checks the original and durable file. No caller hash is authority.
        private bool VerifyLegacySourceForReference(LegacyRollbackSource source, LegacyDurableSourceEvidence proof,
            Guid operation, string snapshot)
        {
            try {
                if (source == null || source.Owner != this || proof == null || operation == Guid.Empty ||
                    proof.Operation != operation || proof.SnapshotHash != snapshot || !LegacySourceHash(snapshot) ||
                    proof.InstallIdentity != Identity(root) || proof.LocationIdentity != source.LocationIdentity ||
                    proof.InstallIdentity != source.InstallIdentity || proof.OriginIdentity != source.OriginIdentity ||
                    proof.Hash != source.Hash) return false;
                byte[] bytes = source.CopyBytes(); if (bytes == null) return false;
                using (var rootPin = PinLegacySourceDirectory(root))
                using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
                using (var menuPin = PinLegacySourceDirectory(menu)) {
                    string originalPath = Path.Combine(menu, "ProGo Status.lnk"), path = LegacySourcePath(operation);
                    CheckLocation(originalPath); CheckLocation(path);
                    using (var original = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        AssertLegacySourceOriginal(source, original, bytes);
                        byte[] expected = LegacySourceDocument(operation, snapshot, source.InstallIdentity,
                            source.LocationIdentity, source.OriginIdentity, source.Hash, proof.StorageOrigin, bytes);
                        if (proof.StorageOrigin != LegacyOrigin(stream.SafeFileHandle) || proof.DocumentHash != Digest(expected) ||
                            !Equal(expected, ReadLegacySourceDocument(stream))) return false;
                        AssertLegacySourceOriginal(source, original, bytes);
                        return true;
                    }
                }
            } catch { return false; }
        }
        public LegacyJournalReferenceEvidence PersistLegacyJournalReference(LegacyRollbackSource source,
            LegacyDurableSourceEvidence proof, Guid operation, string snapshot, byte[] document, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (document == null || document.Length < 1 || document.Length > 1048576 ||
                !VerifyLegacySourceForReference(source, proof, operation, snapshot))
                throw new IOException("Legacy journal reference source is not verified.");
            // Caller supplies canonical journal binding; copy it before writing, never trust mutable caller bytes.
            byte[] expected = (byte[])document.Clone(); string path = LegacyReferencePath(operation);
            using (var rootPin = PinLegacySourceDirectory(root))
            using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups"))) {
                CheckLocation(path);
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                    string origin = LegacyOrigin(stream.SafeFileHandle);
                    stream.Write(expected, 0, expected.Length); stream.Flush(true); stream.Position = 0;
                    byte[] read = new byte[expected.Length]; int offset = 0, count;
                    while (offset < read.Length && (count = stream.Read(read, offset, read.Length - offset)) > 0) offset += count;
                    if (offset != read.Length || stream.Length != expected.Length || !Equal(expected, read) ||
                        origin != LegacyOrigin(stream.SafeFileHandle)) throw new IOException("Legacy reference readback failed.");
                    CheckLocation(path);
                    using (var current = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        if (origin != LegacyOrigin(current.SafeFileHandle) || Digest(expected) != Digest(ReadLegacyReferenceBytes(current)))
                            throw new IOException("Legacy reference publication changed.");
                    }
                    if (!VerifyLegacySourceForReference(source, proof, operation, snapshot))
                        throw new IOException("Legacy reference source changed.");
                    token.ThrowIfCancellationRequested();
                    return new LegacyJournalReferenceEvidence(proof, origin, expected);
                }
            }
        }
        public bool VerifyLegacyJournalReference(LegacyRollbackSource source, LegacyJournalReferenceEvidence reference,
            Guid operation, string snapshot, byte[] expected)
        {
            try {
                if (reference == null || expected == null || expected.Length < 1 || expected.Length > 1048576 ||
                    reference.Hash != Digest(expected) || !VerifyLegacySourceForReference(source, reference.Source, operation, snapshot)) return false;
                using (var rootPin = PinLegacySourceDirectory(root))
                using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups"))) {
                    string path = LegacyReferencePath(operation); CheckLocation(path);
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        if (reference.Origin != LegacyOrigin(stream.SafeFileHandle) || stream.Length != expected.Length ||
                            reference.Hash != Digest(ReadLegacyReferenceBytes(stream))) return false;
                        return VerifyLegacySourceForReference(source, reference.Source, operation, snapshot);
                    }
                }
            } catch { return false; }
        }

        private string LegacyRestartSealPath(Guid operation)
        {
            if (operation == Guid.Empty) throw new IOException("Legacy restart operation is missing.");
            return Path.Combine(root, "backups", "installer-legacy-seal-" + operation.ToString("N") + ".bin");
        }
        private static byte[] LegacyRestartSealDocument(Guid operation, string snapshot,
            LegacyJournalReferenceEvidence reference, string sealOrigin)
        {
            var source = reference.Source;
            return Encoding.ASCII.GetBytes("PROGO-LEGACY-SEAL-1\n" + operation.ToString("N") + "\n" + snapshot + "\n" +
                source.InstallIdentity + "\n" + source.LocationIdentity + "\n" + source.OriginIdentity + "\n" + source.Hash + "\n" +
                source.StorageOrigin + "\n" + source.DocumentHash + "\n" + reference.Origin + "\n" + reference.Hash + "\n" + sealOrigin + "\n");
        }
        public bool PersistLegacyRestartSeal(LegacyRollbackSource source, LegacyJournalReferenceEvidence reference,
            Guid operation, string snapshot, byte[] document, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!VerifyLegacyJournalReference(source, reference, operation, snapshot, document))
                throw new IOException("Legacy restart seal requires verified reference.");
            using (var rootPin = PinLegacySourceDirectory(root))
            using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups"))) {
                string path = LegacyRestartSealPath(operation); CheckLocation(path);
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                    string origin = LegacyOrigin(stream.SafeFileHandle);
                    byte[] bytes = LegacyRestartSealDocument(operation, snapshot, reference, origin);
                    stream.Write(bytes, 0, bytes.Length); stream.Flush(true); stream.Position = 0;
                    if (!Equal(bytes, ReadLegacyReferenceBytes(stream)) || origin != LegacyOrigin(stream.SafeFileHandle))
                        throw new IOException("Legacy restart seal readback failed.");
                    CheckLocation(path);
                    using (var current = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        if (origin != LegacyOrigin(current.SafeFileHandle) || !Equal(bytes, ReadLegacyReferenceBytes(current)))
                            throw new IOException("Legacy restart seal publication changed.");
                    }
                    if (!VerifyLegacyJournalReference(source, reference, operation, snapshot, document))
                        throw new IOException("Legacy restart evidence changed.");
                    token.ThrowIfCancellationRequested(); return true;
                }
            }
        }
        // Open the final leaf itself, never follow a leaf reparse point during restart verification.
        private static FileStream OpenLegacyRestartRead(string path)
        {
            CheckLocation(path);
            var handle = OpenLegacyForDisposition(path, 0x80000000u, 1, IntPtr.Zero, 3, 0x00200000u, IntPtr.Zero);
            try {
                if (handle.IsInvalid) throw new IOException("Legacy restart file is unavailable.");
                LegacyOrigin(handle); // rejects directory/reparse object before reading
                return new FileStream(handle, FileAccess.Read);
            } catch { handle.Dispose(); throw; }
        }
        // Rebuild from independently observed owned original/source, not serialized identity claims.
        public LegacyDurableSourceEvidence RehydrateLegacySource(Guid operation, string verifiedSnapshot)
        {
            try {
                if (operation == Guid.Empty || !LegacySourceHash(verifiedSnapshot)) return null;
                using (var source = CaptureLegacyRollbackSource(CancellationToken.None)) {
                    if (source == null) return null;
                    using (var rootPin = PinLegacySourceDirectory(root))
                    using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups"))) {
                        string path = LegacySourcePath(operation); CheckLocation(path);
                        using (var stream = OpenLegacyRestartRead(path)) {
                            string storage = LegacyOrigin(stream.SafeFileHandle);
                            byte[] bytes = source.CopyBytes();
                            byte[] expected = LegacySourceDocument(operation, verifiedSnapshot, source.InstallIdentity,
                                source.LocationIdentity, source.OriginIdentity, source.Hash, storage, bytes);
                            if (!Equal(expected, ReadLegacySourceDocument(stream))) return null;
                            var proof = new LegacyDurableSourceEvidence(source, operation, verifiedSnapshot, storage, expected);
                            return VerifyLegacySourceForReference(source, proof, operation, verifiedSnapshot) ? proof : null;
                        }
                    }
                }
            } catch { return null; }
        }
        public LegacyJournalReferenceEvidence RehydrateLegacyJournalReference(LegacyDurableSourceEvidence proof,
            Guid operation, string verifiedSnapshot, byte[] expectedDocument)
        {
            try {
                if (expectedDocument == null || expectedDocument.Length < 1 || expectedDocument.Length > 1048576) return null;
                using (var source = CaptureLegacyRollbackSource(CancellationToken.None)) {
                    if (!VerifyLegacySourceForReference(source, proof, operation, verifiedSnapshot)) return null;
                    using (var rootPin = PinLegacySourceDirectory(root))
                    using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups"))) {
                        string referencePath = LegacyReferencePath(operation), sealPath = LegacyRestartSealPath(operation);
                        CheckLocation(referencePath); CheckLocation(sealPath);
                        using (var stream = OpenLegacyRestartRead(referencePath))
                        using (var seal = OpenLegacyRestartRead(sealPath)) {
                            if (!Equal(expectedDocument, ReadLegacyReferenceBytes(stream))) return null;
                            var reference = new LegacyJournalReferenceEvidence(proof, LegacyOrigin(stream.SafeFileHandle), expectedDocument);
                            byte[] expectedSeal = LegacyRestartSealDocument(operation, verifiedSnapshot, reference, LegacyOrigin(seal.SafeFileHandle));
                            if (!Equal(expectedSeal, ReadLegacyReferenceBytes(seal)) ||
                                !VerifyLegacyJournalReference(source, reference, operation, verifiedSnapshot, expectedDocument)) return null;
                            return reference;
                        }
                    }
                }
            } catch { return null; }
        }

        private static void ReleaseLegacyDeletionPins(System.Collections.Generic.List<IDisposable> held)
        {
            bool failed = false;
            for (int i = held.Count - 1; i >= 0; i--) {
                try { held[i].Dispose(); } catch { failed = true; }
            }
            held.Clear();
            if (failed) throw new IOException("Legacy pin cleanup did not complete.");
        }
        // Pin-only prerequisite. No mutation method or durable outcome is exposed by this lease.
        public sealed class LegacyDeletionPins : IDisposable
        {
            private readonly object gate = new object();
            private System.Collections.Generic.List<IDisposable> resources;
            internal LegacyDeletionPins(System.Collections.Generic.List<IDisposable> held) { resources = held; }
            public bool IsPinned { get { lock (gate) { return resources != null; } } }
            public bool MutationAllowed { get { return false; } }
            public bool FullRollback { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            public void Dispose()
            {
                lock (gate) {
                    if (resources == null) return;
                    var held = resources; resources = null;
                    ReleaseLegacyDeletionPins(held);
                }
            }
        }
        public LegacyDeletionPins PinLegacyPreDeleteAdmission(LegacyJournalReferenceEvidence reference,
            Guid operation, string verifiedSnapshot, byte[] document, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var held = new System.Collections.Generic.List<IDisposable>();
            bool transferred = false;
            try {
                if (reference == null || document == null || document.Length < 1 || document.Length > 1048576) return null;
                byte[] expectedDocument = (byte[])document.Clone();
                held.Add(PinLegacySourceDirectory(root));
                held.Add(PinLegacySourceDirectory(Path.Combine(root, "backups")));
                held.Add(PinLegacySourceDirectory(menu));
                // Keep the complete evidence graph stable through the original-handle admission.
                held.Add(OpenLegacyRestartRead(LegacySourcePath(operation)));
                held.Add(OpenLegacyRestartRead(LegacyReferencePath(operation)));
                held.Add(OpenLegacyRestartRead(LegacyRestartSealPath(operation)));
                held.Add(OpenLegacyRestartRead(LegacyPreDeletePath(operation)));
                if (GetLegacyPreDeleteAdmissionState(reference, operation, verifiedSnapshot, expectedDocument) != "not-applied") return null;
                string path = Path.Combine(menu, "ProGo Status.lnk"); CheckLocation(path);
                // READ|DELETE on the exact non-followed leaf; deny writes/replacement. Acquiring
                // this access is not deletion. Future deletion must use this SAME retained handle.
                var handle = OpenLegacyForDisposition(path, 0x80010000u, 1, IntPtr.Zero, 3, 0x00200000u, IntPtr.Zero);
                held.Add(handle);
                if (handle.IsInvalid) return null;
                string origin = LegacyOrigin(handle); // rejects directories and reparse leaves
                var original = new FileStream(handle, FileAccess.Read); held.Add(original);
                byte[] bytes = ReadLegacyEvidenceBytes(original);
                if (reference.Source.InstallIdentity != Identity(root) ||
                    reference.Source.LocationIdentity != Identity(path) || reference.Source.Operation != operation ||
                    reference.Source.SnapshotHash != verifiedSnapshot || reference.Source.OriginIdentity != origin ||
                    reference.Source.Hash != Digest(bytes) || origin != LegacyOrigin(handle)) return null;
                token.ThrowIfCancellationRequested();
                var result = new LegacyDeletionPins(held); transferred = true; return result;
            } catch (OperationCanceledException) { throw; }
            catch { return null; }
            finally { if (!transferred) ReleaseLegacyDeletionPins(held); }
        }

        private string LegacyPreDeletePath(Guid operation)
        {
            if (operation == Guid.Empty) throw new IOException("Legacy admission operation is missing.");
            return Path.Combine(root, "backups", "installer-legacy-admission-" + operation.ToString("N") + ".bin");
        }
        private static byte[] LegacyPreDeleteDocument(Guid operation, string snapshot,
            LegacyJournalReferenceEvidence reference, string sealOrigin, byte[] sealBytes, string admissionOrigin)
        {
            return Encoding.ASCII.GetBytes("PROGO-LEGACY-PREDELETE-1\n" +
                Encoding.ASCII.GetString(LegacyRestartSealDocument(operation, snapshot, reference, sealOrigin)) +
                Digest(sealBytes) + "\n" + admissionOrigin + "\n");
        }
        // Durable intent only. No deletion, replay, restore or terminal authority.
        public bool PersistLegacyPreDeleteAdmission(LegacyJournalReferenceEvidence reference, Guid operation,
            string verifiedSnapshot, byte[] document, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (reference == null) throw new IOException("Legacy admission reference is missing.");
            var currentReference = RehydrateLegacyJournalReference(reference.Source, operation, verifiedSnapshot, document);
            if (currentReference == null || currentReference.Origin != reference.Origin || currentReference.Hash != reference.Hash)
                throw new IOException("Legacy admission evidence is not verified.");
            using (var rootPin = PinLegacySourceDirectory(root))
            using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
            using (var seal = OpenLegacyRestartRead(LegacyRestartSealPath(operation))) {
                string sealOrigin = LegacyOrigin(seal.SafeFileHandle);
                byte[] sealBytes = ReadLegacyReferenceBytes(seal);
                if (!Equal(sealBytes, LegacyRestartSealDocument(operation, verifiedSnapshot, reference, sealOrigin)))
                    throw new IOException("Legacy admission seal changed.");
                string path = LegacyPreDeletePath(operation); CheckLocation(path); token.ThrowIfCancellationRequested();
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                    string origin = LegacyOrigin(stream.SafeFileHandle);
                    byte[] expected = LegacyPreDeleteDocument(operation, verifiedSnapshot, reference, sealOrigin, sealBytes, origin);
                    stream.Write(expected, 0, expected.Length); stream.Flush(true); stream.Position = 0;
                    if (!Equal(expected, ReadLegacyReferenceBytes(stream)) || origin != LegacyOrigin(stream.SafeFileHandle))
                        throw new IOException("Legacy admission readback failed.");
                    CheckLocation(path);
                    using (var current = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        if (origin != LegacyOrigin(current.SafeFileHandle) || !Equal(expected, ReadLegacyReferenceBytes(current)))
                            throw new IOException("Legacy admission publication changed.");
                    }
                    currentReference = RehydrateLegacyJournalReference(reference.Source, operation, verifiedSnapshot, document);
                    if (currentReference == null || currentReference.Origin != reference.Origin || currentReference.Hash != reference.Hash)
                        throw new IOException("Legacy admission source changed.");
                    token.ThrowIfCancellationRequested(); return true;
                }
            }
        }
        public string GetLegacyPreDeleteAdmissionState(LegacyJournalReferenceEvidence reference, Guid operation,
            string verifiedSnapshot, byte[] document)
        {
            try {
                if (reference == null) return "blocked";
                var currentReference = RehydrateLegacyJournalReference(reference.Source, operation, verifiedSnapshot, document);
                if (currentReference == null || currentReference.Origin != reference.Origin || currentReference.Hash != reference.Hash) return "blocked";
                using (var rootPin = PinLegacySourceDirectory(root))
                using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
                using (var seal = OpenLegacyRestartRead(LegacyRestartSealPath(operation)))
                using (var admission = OpenLegacyRestartRead(LegacyPreDeletePath(operation))) {
                    string sealOrigin = LegacyOrigin(seal.SafeFileHandle);
                    byte[] sealBytes = ReadLegacyReferenceBytes(seal);
                    if (!Equal(sealBytes, LegacyRestartSealDocument(operation, verifiedSnapshot, reference, sealOrigin))) return "blocked";
                    byte[] expected = LegacyPreDeleteDocument(operation, verifiedSnapshot, reference, sealOrigin, sealBytes, LegacyOrigin(admission.SafeFileHandle));
                    if (!Equal(expected, ReadLegacyReferenceBytes(admission))) return "blocked";
                    currentReference = RehydrateLegacyJournalReference(reference.Source, operation, verifiedSnapshot, document);
                    return currentReference != null && currentReference.Origin == reference.Origin && currentReference.Hash == reference.Hash
                        ? "not-applied" : "blocked";
                }
            } catch { return "blocked"; }
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

        // Observation only. An absent object is never permission to recreate/delete anything.
        // The caller separately verifies durable admission and (if needed) Startup settlement.
        public bool VerifyDurableMenuOriginalResult(System.Collections.IDictionary startupReceipt,
            System.Collections.IDictionary menuReceipt, System.Collections.IDictionary legacyReceipt, bool startupOriginal)
        {
            try {
                var first = ReadDurableEvidence<StartupInstallEvidence>(startupReceipt);
                var primary = ReadDurableEvidence<MenuInstallEvidence>(menuReceipt);
                var legacy = ReadDurableEvidence<LegacyInstallEvidence>(legacyReceipt);
                bool startupValid = startupOriginal
                    ? first.Disposition == "created" && first.InstallIdentity == Identity(root) && first.LocationIdentity == Identity(startup) &&
                        !first.BeforeExists && first.BeforeHash == null && first.Exists && first.Hash != null && first.OriginIdentity != null && ReadStartupBytes() == null
                    : VerifyStartupInstallEvidence(first);
                if (!startupValid) return false;
                if (primary.Disposition == "preserved") return VerifyDurableMenuRollbackEvidence(menuReceipt) && VerifyLegacyInstallEvidence(legacy);
                if (primary.Disposition != "created" || !primary.Requested || primary.InstallIdentity != Identity(root) ||
                    primary.LocationIdentity != Identity(Path.Combine(menu, "ProGo.lnk")) || primary.BeforeExists || primary.BeforeHash != null ||
                    !primary.Exists || primary.Hash == null || primary.OriginIdentity == null || ReadMenuBytes() != null) return false;
                if (legacy.Disposition != "removed") return VerifyLegacyInstallEvidence(legacy);
                // The primary has now been removed. Verify the original, receipt-linked Legacy
                // absence; do not weaken the existing verifier that requires the live primary.
                return legacy.InstallIdentity == Identity(root) && legacy.LocationIdentity == Identity(Path.Combine(menu, "ProGo Status.lnk")) &&
                    legacy.Requested && legacy.BeforeMatched && legacy.BeforeExists && !legacy.Exists && legacy.Hash == null &&
                    legacy.OriginIdentity != null && legacy.BeforeHash != null && legacy.OwnedHash == legacy.BeforeHash &&
                    legacy.ReplacementHash == primary.Hash && ObserveLegacy(false).Bytes == null;
            } catch { return false; }
        }

        // Menu only: the caller retains the verified receipt and maintenance ownership.
        // No Legacy restoration, durable terminal outcome, replay or path-based deletion.
        public bool TryRollbackDurableMenu(System.Collections.IDictionary receipt, CancellationToken token)
        {
            try {
                token.ThrowIfCancellationRequested();
                var evidence = ReadDurableEvidence<MenuInstallEvidence>(receipt);
                if (!VerifyDurableMenuRollbackEvidence(receipt)) return false;
                if (evidence.Disposition == "preserved") return true;
                string path = Path.Combine(menu, "ProGo.lnk");
                CheckLocation(path);
                using (var handle = OpenLegacyForDisposition(path, 0x80010000u, 1, IntPtr.Zero, 3, 0x00200080u, IntPtr.Zero)) {
                    if (handle.IsInvalid) return false;
                    using (var stream = new FileStream(handle, FileAccess.Read)) {
                        // Full native origin (including creation), not bytes alone. Read/delete
                        // share denies external replacement or writes until this handle settles.
                        if (evidence.OriginIdentity != LegacyOrigin(handle) || evidence.Hash != Digest(ReadLegacyEvidenceBytes(stream))) return false;
                        token.ThrowIfCancellationRequested();
                        var disposition = new LegacyDisposition { DeleteFile = 1 };
                        if (!SetFileInformationByHandle(handle, 4, ref disposition, 1)) return false;
                    }
                }
                // A late cancellation must not discard the accepted physical result.
                return ReadMenuBytes() == null;
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
                    ? EnsureLink(Path.Combine(menu, "ProGo.lnk"), "--show", CancellationToken.None, handle => menuCreatedOrigin = LegacyOrigin(handle), true)
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
        { return EnsureLink(path, arguments, token, witness, false); }

        private byte[] EnsureLink(string path, string arguments, CancellationToken token, Action<SafeFileHandle> witness, bool publishedWitness)
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
                    LegacyFileInfo before = new LegacyFileInfo();
                    if (publishedWitness && (!GetFileInformationByHandle(stream.SafeFileHandle, out before) || (before.Attributes & 0x410) != 0))
                        throw new IOException("Created shortcut identity is unavailable.");
                    if (!publishedWitness) witness(stream.SafeFileHandle);
                    File.Move(temporary, path);
                    if (publishedWitness) WitnessPublishedLink(path, bytes, stream, before, witness);
                }
                return bytes;
            } finally {
                if (native != null) Marshal.FinalReleaseComObject(native);
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        // Keep the original object handle across rename; creation time may change at publication.
        // Only this continuity check uses volume/index. The receipt origin still includes creation.
        private void WitnessPublishedLink(string path, byte[] bytes, FileStream owned, LegacyFileInfo before, Action<SafeFileHandle> witness)
        {
            LegacyFileInfo after;
            if (!GetFileInformationByHandle(owned.SafeFileHandle, out after) || (after.Attributes & 0x410) != 0 ||
                before.Volume != after.Volume || before.IndexHigh != after.IndexHigh || before.IndexLow != after.IndexLow)
                throw new IOException("Created shortcut object continuity changed.");
            string origin = LegacyOrigin(owned.SafeFileHandle);
            owned.Position = 0;
            if (!Equal(bytes, ReadLegacyEvidenceBytes(owned))) throw new IOException("Created shortcut bytes changed.");
            CheckLocation(path);
            using (var published = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (origin != LegacyOrigin(published.SafeFileHandle) || !Equal(bytes, ReadLegacyEvidenceBytes(published)))
                    throw new IOException("Published shortcut is not the created object.");
                witness(owned.SafeFileHandle);
                // A failed witness or changed evidence must never return an accepted receipt.
                owned.Position = 0; published.Position = 0;
                if (origin != LegacyOrigin(owned.SafeFileHandle) || origin != LegacyOrigin(published.SafeFileHandle) ||
                    !Equal(bytes, ReadLegacyEvidenceBytes(owned)) || !Equal(bytes, ReadLegacyEvidenceBytes(published)))
                    throw new IOException("Published shortcut witness changed.");
                CheckLocation(path);
                using (var current = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (origin != LegacyOrigin(current.SafeFileHandle) || !Equal(bytes, ReadLegacyEvidenceBytes(current)))
                        throw new IOException("Published shortcut location changed.");
                }
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
