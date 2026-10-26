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

        private static readonly string[] LegacyOperationLeaves = new[] {
            "ProGo.exe",
            "VERSION",
            "ProGo.ico",
            "scripts/Install-ProGo.ps1",
            "scripts/Uninstall-ProGo.ps1",
            "scripts/Update-ProGo.ps1",
            "scripts/Update-ProGo.Core.ps1",
            "scripts/Restore-ProGoBackup.ps1",
            "scripts/Show-ProGo.ps1",
            "scripts/Start-ProGo.ps1",
            "scripts/Repair-ProGo.ps1",
            "scripts/Enable-HomeVpnFirewall.ps1",
            "scripts/Maintenance-ProGo.ps1",
            "scripts/BackupRetention-ProGo.ps1",
            "scripts/BackupIntegrity-ProGo.ps1",
            "scripts/Log-ProGo.ps1",
            "scripts/Diagnostics-ProGo.ps1",
            "scripts/Measure-RescueProGo.ps1",
            "scripts/Shortcuts-ProGo.ps1",
            "scripts/Firewall-ProGo.ps1",
            "scripts/ApplicationShortcuts.cs",
            "scripts/MaintenanceOperation.cs",
            "scripts/InstalledUpdateTransport.cs",
            "scripts/UpdateInstallSession.cs",
            "scripts/BackupRetention.cs",
            "scripts/BackupIntegrity.cs",
            "scripts/BoundedLog.cs",
            "scripts/RescueMeasurement.cs",
            "scripts/DiagnosticReport.cs",
            "scripts/DiagnosticPreview.cs",
            "scripts/UiTheme.cs",
            "scripts/BrandIcon.cs",
            "scripts/home-vpn/HOME_IKEV2.md",
            "scripts/home-vpn/server/ikev2_relay.py",
            "scripts/home-vpn/server/install-ikev2-relay.sh",
            "scripts/home-vpn/server/make_home_profile.py",
            "scripts/home-vpn/server/home_vpn_setup.py",
            "scripts/home-vpn/server/profile_share_setup.py",
            "scripts/home-vpn/server/profile_share.py",
            "scripts/home-vpn/server/qrcodegen.py",
            "scripts/home-vpn/server/QR_LICENSE.txt",
        };
        // Live-lease restore only. Private fault selector is invoked only by isolated reflection fixtures.
        // No serialized input, caller path, callback or process-wide fault switch grants authority.
        public sealed class LegacyRestoreResult
        {
            public LegacyOwnedDeleteResult AcceptedResult { get; private set; }
            public string State { get; private set; }
            public bool Attempted { get; private set; }
            public bool Created { get; private set; }
            public long BytesWritten { get; private set; }
            public bool Flushed { get; private set; }
            public bool ReadbackVerified { get; private set; }
            public bool DurableResultRecorded { get; private set; }
            public string RestoredOrigin { get; private set; }
            public string ResultOrigin { get; private set; }
            public string ResultHash { get; private set; }
            public bool WholeRollbackVerified { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            public bool RestartAuthority { get { return false; } }
            internal LegacyRestoreResult(LegacyOwnedDeleteResult accepted, string state, bool attempted, bool created,
                long written, bool flushed, bool readback, bool recorded, string origin, string resultOrigin, string resultHash)
            { AcceptedResult = accepted; State = state; Attempted = attempted; Created = created; BytesWritten = written;
              Flushed = flushed; ReadbackVerified = readback; DurableResultRecorded = recorded;
              RestoredOrigin = origin; ResultOrigin = resultOrigin; ResultHash = resultHash; }
        }
        private string LegacyRestoreResultPath(Guid operation)
        { return Path.Combine(root, "backups", "installer-legacy-restored-" + operation.ToString("N") + ".bin"); }
        private byte[] ReadLiveLegacyRestoreBytes(LegacyDeletionPins lease, LegacyPhysicalPins physical)
        {
            if (lease.resources == null || lease.lastResult == null || !lease.lastResult.OperationAccepted ||
                !lease.lastResult.AcceptanceKnown || !lease.lastResult.TargetHandleClosed || !lease.lastResult.ContextObservedAfterOperation ||
                lease.acceptedOutcome == null || !lease.acceptedOutcome.DurableOutcomeRecorded || physical == null ||
                !physical.MatchesOwner(this) || !physical.VerifyPinned(CancellationToken.None)) return null;
            byte[] document = null, original = null;
            try {
                lease.evidence[0].Position = 0; document = ReadLegacySourceDocument(lease.evidence[0]);
                int offset = 0, lines = 0;
                while (offset < document.Length && lines < 9) { if (document[offset++] == 10) lines++; }
                if (lines != 9 || offset == document.Length) return null;
                original = new byte[document.Length - offset]; Buffer.BlockCopy(document, offset, original, 0, original.Length);
                if (!VerifyLegacyOperationEvidence(lease, original) || !VerifyLegacyOperationPlan(lease, physical)) return null;
                using (var replacement = OpenLegacyRestartRead(Path.Combine(menu, "ProGo.lnk")))
                using (var accepted = OpenLegacyRestartRead(LegacyAcceptedPath(lease.reference.Source.Operation))) {
                    var receipt = lease.acceptedOutcome;
                    byte[] expected = LegacyAcceptedDocument(lease, receipt.Origin);
                    if (!VerifyLegacyAcceptedReplacement(lease, replacement) || receipt.Origin != LegacyOrigin(accepted.SafeFileHandle) ||
                        receipt.Hash != Digest(expected) || !Equal(expected, ReadLegacyReferenceBytes(accepted))) return null;
                }
                byte[] result = original; original = null; return result;
            } finally {
                if (document != null) Array.Clear(document, 0, document.Length);
                if (original != null) Array.Clear(original, 0, original.Length);
            }
        }
        private byte[] LegacyRestoreDocument(LegacyDeletionPins lease, string restoredOrigin, string resultOrigin, byte[] original)
        {
            // Canonical accepted document carries operation/snapshot/location/origin/hash/schema graph bindings.
            string text = "PROGO-LEGACY-RESTORED-1\n" + restoredOrigin + "\n" + Digest(original) + "\n" +
                original.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + resultOrigin + "\n" +
                "created-written-flushed-readback\nrollback-not-proven\nrestart-blocked\n";
            return Encoding.ASCII.GetBytes(text + Encoding.ASCII.GetString(LegacyAcceptedDocument(lease, lease.acceptedOutcome.Origin)));
        }
        private bool VerifyLegacyRestoreMaterial(LegacyDeletionPins lease, LegacyPhysicalPins physical, string origin, byte[] expected)
        {
            // Post-restore phase: require this newly created native object, never the deleted object's identity.
            byte[] current = ReadLiveLegacyRestoreBytes(lease, physical);
            try {
                if (current == null || !Equal(expected, current) || origin == null || origin == lease.reference.Source.OriginIdentity) return false;
                using (var restored = OpenLegacyAcceptedPublicationRead(Path.Combine(menu, "ProGo Status.lnk"))) {
                    return origin == LegacyOrigin(restored.SafeFileHandle) && Equal(expected, ReadLegacyEvidenceBytes(restored));
                }
            } finally { if (current != null) Array.Clear(current, 0, current.Length); }
        }
        private LegacyRestoreResult RestoreLegacyOwnedOnce(LegacyDeletionPins lease, LegacyPhysicalPins physical,
            CancellationToken token, string fault, CancellationTokenSource faultCancellation)
        {
            lock (lease.gate) {
                if (lease.restoreResult != null) return lease.restoreResult;
                // Reserve before any I/O; reentrant/second calls cannot perform another mutation.
                lease.restoreResult = new LegacyRestoreResult(lease.lastResult, "reserved", false, false, 0, false, false, false, null, null, null);
                bool attempted = false, created = false, flushed = false, readback = false, recorded = false;
                long written = 0; string origin = null, resultOrigin = null, resultHash = null, state = "blocked";
                byte[] original = null;
                try {
                    if (fault != null && Array.IndexOf(new[] { "write", "short-write", "flush", "readback", "readback-throw",
                        "cancel-created", "cancel-written", "cancel-flushed", "cancel-readback", "result-write", "result-flush", "result-readback" }, fault) < 0)
                        throw new ArgumentException("Unknown isolated restore fault.");
                    if (token.IsCancellationRequested) { state = "cancelled-before-create"; return lease.restoreResult =
                        new LegacyRestoreResult(lease.lastResult, state, false, false, 0, false, false, false, null, null, null); }
                    if (physical != null) physical.WithPins<LegacyRestoreResult>(() => {
                        original = ReadLiveLegacyRestoreBytes(lease, physical);
                        if (original == null || !VerifyLegacyAcceptedContext(lease, physical)) return null;
                        string path = Path.Combine(menu, "ProGo Status.lnk"); CheckLocation(path);
                        using (var rootPin = PinLegacySourceDirectory(root))
                        using (var menuPin = PinLegacySourceDirectory(menu))
                        using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
                        using (var replacement = OpenLegacyRestartRead(Path.Combine(menu, "ProGo.lnk"))) {
                            if (!VerifyLegacyAcceptedReplacement(lease, replacement)) return null;
                            token.ThrowIfCancellationRequested();
                            attempted = true;
                            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                                created = true; state = "created-incomplete"; origin = LegacyOrigin(stream.SafeFileHandle);
                                if (origin == lease.reference.Source.OriginIdentity) throw new IOException("Restore native identity was reused.");
                                if (fault == "cancel-created") faultCancellation.Cancel();
                                token.ThrowIfCancellationRequested();
                                if (fault == "write") throw new IOException("Synthetic restore Write fault.");
                                int count = fault == "short-write" ? Math.Max(1, original.Length / 2) : original.Length;
                                try { stream.Write(original, 0, count); }
                                finally { written = stream.Length; }
                                if (written != original.Length) throw new IOException("Restore Write was incomplete.");
                                if (fault == "cancel-written") faultCancellation.Cancel();
                                token.ThrowIfCancellationRequested();
                                if (fault == "flush") throw new IOException("Synthetic restore Flush fault.");
                                stream.Flush(true); flushed = true;
                                if (fault == "cancel-flushed") faultCancellation.Cancel();
                                token.ThrowIfCancellationRequested();
                                if (fault == "readback") { stream.Position = 0; stream.WriteByte((byte)(original[0] ^ 1)); stream.Flush(true); }
                                if (fault == "readback-throw") throw new IOException("Synthetic restore readback fault.");
                                stream.Position = 0;
                                if (origin != LegacyOrigin(stream.SafeFileHandle) || !Equal(original, ReadLegacyEvidenceBytes(stream)) ||
                                    !VerifyLegacyRestoreMaterial(lease, physical, origin, original) || !VerifyLegacyAcceptedReplacement(lease, replacement))
                                    throw new IOException("Restore readback or context changed.");
                                readback = true; state = "restored-unrecorded";
                                if (fault == "cancel-readback") faultCancellation.Cancel();
                                token.ThrowIfCancellationRequested();
                                string resultPath = LegacyRestoreResultPath(lease.reference.Source.Operation); CheckLocation(resultPath);
                                using (var result = new FileStream(resultPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                                    resultOrigin = LegacyOrigin(result.SafeFileHandle);
                                    byte[] document = LegacyRestoreDocument(lease, origin, resultOrigin, original);
                                    if (fault == "result-write") { result.Write(document, 0, document.Length / 2); throw new IOException("Synthetic result Write fault."); }
                                    result.Write(document, 0, document.Length);
                                    if (fault == "result-flush") throw new IOException("Synthetic result Flush fault.");
                                    result.Flush(true); result.Position = 0;
                                    if (fault == "result-readback") { result.WriteByte(0); result.Flush(true); result.Position = 0; }
                                    if (resultOrigin != LegacyOrigin(result.SafeFileHandle) || !Equal(document, ReadLegacyReferenceBytes(result)))
                                        throw new IOException("Restore result readback changed.");
                                    using (var observed = OpenLegacyAcceptedPublicationRead(resultPath)) {
                                        if (resultOrigin != LegacyOrigin(observed.SafeFileHandle) || !Equal(document, ReadLegacyReferenceBytes(observed)) ||
                                            !VerifyLegacyRestoreMaterial(lease, physical, origin, original) || !VerifyLegacyAcceptedReplacement(lease, replacement) ||
                                            !Equal(document, LegacyRestoreDocument(lease, origin, resultOrigin, original)))
                                            throw new IOException("Restore result context changed.");
                                    }
                                    // Cancellation after actual complete publication does not erase its durable truth.
                                    resultHash = Digest(document); recorded = true; state = "restored-recorded";
                                }
                            }
                        }
                        return lease.restoreResult;
                    });
                } catch (OperationCanceledException) { state = created ? "cancelled-after-create" : "cancelled-before-create"; }
                catch { state = created ? (readback ? "restored-result-error" : "created-incomplete") : (attempted ? "create-conflict" : "blocked"); }
                finally { if (original != null) Array.Clear(original, 0, original.Length); }
                lease.restoreResult = new LegacyRestoreResult(lease.lastResult, state, attempted, created, written, flushed, readback,
                    recorded, origin, resultOrigin, resultHash);
                return lease.restoreResult;
            }
        }
        private bool VerifyLegacyRestoredOutcome(LegacyDeletionPins lease, LegacyPhysicalPins physical)
        {
            lock (lease.gate) {
                byte[] original = null;
                try {
                    var result = lease.restoreResult;
                    if (result == null || !result.Created || !result.Flushed || !result.ReadbackVerified || !result.DurableResultRecorded || physical == null) return false;
                    return physical.WithPins<LegacyRestoreResult>(() => {
                        original = ReadLiveLegacyRestoreBytes(lease, physical);
                        if (original == null || result.BytesWritten != original.Length || !VerifyLegacyRestoreMaterial(lease, physical, result.RestoredOrigin, original)) return null;
                        using (var restored = OpenLegacyRestartRead(Path.Combine(menu, "ProGo Status.lnk")))
                        using (var receipt = OpenLegacyRestartRead(LegacyRestoreResultPath(lease.reference.Source.Operation))) {
                            byte[] expected = LegacyRestoreDocument(lease, result.RestoredOrigin, result.ResultOrigin, original);
                            return result.RestoredOrigin == LegacyOrigin(restored.SafeFileHandle) && Equal(original, ReadLegacyEvidenceBytes(restored)) &&
                                result.ResultOrigin == LegacyOrigin(receipt.SafeFileHandle) && result.ResultHash == Digest(expected) &&
                                Equal(expected, ReadLegacyReferenceBytes(receipt)) && VerifyLegacyRestoreMaterial(lease, physical, result.RestoredOrigin, original) ? result : null;
                        }
                    }) != null;
                } catch { return false; }
                finally { if (original != null) Array.Clear(original, 0, original.Length); }
            }
        }
        // One live preparation/operation binding; historical restore flags are never a capability.
        public sealed class LegacyStartupContext : IDisposable
        {
            internal readonly object gate = new object();
            internal readonly ApplicationShortcuts owner;
            internal readonly StartupInstallEvidence preparation;
            internal readonly LegacyDeletionPins lease;
            internal readonly LegacyPhysicalPins physical;
            internal readonly System.Collections.IDictionary startupMap, menuMap, legacyMap;
            internal LegacyRestoreResult restored;
            internal bool disposed, failureEntered, startupConsumed, startupVerified, menuConsumed, menuFailureEntered, auxiliaryEntered;
            internal readonly Guid session = Guid.NewGuid();
            internal AuxiliaryOutcomeResult auxiliaryResult;
            internal FileStream restoredPin, primaryPin;
            internal bool menuPreservedVerified;
            public bool MenuPreservedObserved { get { return menuPreservedVerified; } }
            internal string menuPrimaryOrigin;
            public bool MenuAttempted { get; internal set; }
            public bool MenuDispositionAccepted { get; internal set; }
            public bool MenuHandleClosed { get; internal set; }
            public bool MenuAbsenceObserved { get; internal set; }
            public bool StartupInvoked { get; private set; }
            public bool MenuAllowed { get { return VerifyStartupSettlement(); } }
            public bool WholeRollbackVerified { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            public bool RestartAuthority { get { return false; } }
            public Guid OperationId { get { return lease.reference.Source.Operation; } }
            internal LegacyStartupContext(ApplicationShortcuts owner, StartupInstallEvidence preparation, LegacyDeletionPins lease, LegacyPhysicalPins physical)
            {
                this.owner = owner; this.preparation = preparation; this.lease = lease; this.physical = physical;
                startupMap = LivePreparationMap(preparation); menuMap = LivePreparationMap(preparation.Menu); legacyMap = LivePreparationMap(preparation.Legacy);
            }
            public bool MatchesInstallerLocations(string directory, string install, string programs, string startup)
            {
                try { return !disposed && Identity(install) == Identity(owner.root) && Identity(Path.Combine(programs, "ProGo")) == Identity(owner.menu) &&
                    Identity(Path.Combine(startup, "ProGo.lnk")) == Identity(owner.startup) &&
                    String.Equals(Path.GetFullPath(directory), Path.Combine(owner.root, "backups", "installer-journal-" + OperationId.ToString("N")), StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            }
            public bool BeginFailureHandling()
            { lock (gate) { if (disposed || failureEntered) return false; failureEntered = true; return true; } }
            public LegacyRestoreResult RestoreLegacy(CancellationToken token)
            { lock (gate) { if (disposed) return null; if (restored == null) restored = lease.RestoreOwnedOnce(physical, token); return restored; } }
            public bool VerifyHandoff()
            { lock (gate) { return owner.VerifyLiveLegacyHandoff(this); } }
            public bool RollbackStartupOnce(CancellationToken token)
            {
                lock (gate) {
                    if (disposed) return false;
                    if (startupConsumed) return VerifyStartupSettlement();
                    startupConsumed = true; // no retry after cancellation, failed admission, or indeterminate mutation
                    try {
                        lock (lease.gate) { return physical.WithPins<LegacyStartupContext>(() => {
                            if (!owner.VerifyDurableInstallEvidence(startupMap, menuMap, legacyMap, this)) return null;
                            // Pin the exact restored launcher and primary throughout the Startup mutation.
                            using (var legacy = OpenLegacyRestartRead(Path.Combine(owner.menu, "ProGo Status.lnk")))
                            using (var primary = OpenLegacyRestartRead(Path.Combine(owner.menu, "ProGo.lnk"))) {
                                if (restored.RestoredOrigin != LegacyOrigin(legacy.SafeFileHandle) || !VerifyLegacyAcceptedReplacement(lease, primary) ||
                                    !owner.VerifyDurableInstallEvidence(startupMap, menuMap, legacyMap, this)) return null;
                                token.ThrowIfCancellationRequested(); StartupInvoked = true;
                                startupVerified = owner.TryRollbackDurableStartup(startupMap, token);
                                return startupVerified && owner.VerifyDurableStartupOriginalResult(startupMap, menuMap, legacyMap, this) ? this : null;
                            }
                        }) != null; }
                    } catch { return false; }
                }
            }
            public bool VerifyStartupSettlement()
            { lock (gate) { return !disposed && startupVerified && owner.VerifyDurableStartupOriginalResult(startupMap, menuMap, legacyMap, this); } }
            public bool BeginMenuHandling()
            { lock (gate) { if (disposed || menuFailureEntered) return false; menuFailureEntered = true; return true; } }
            public bool RollbackMenuOnce(CancellationToken token)
            {
                lock (gate) {
                    if (disposed) return false;
                    if (menuConsumed) return VerifyMenuSettlement();
                    menuConsumed = true; // cancellation/error never permits another destructive invocation
                    try {
                        lock (lease.gate) { return physical.WithPins<LegacyStartupContext>(() => {
                            if (!VerifyStartupSettlement() || !owner.VerifyDurableMenuRollbackEvidence(menuMap)) return null;
                            restoredPin = OpenLegacyRestartRead(Path.Combine(owner.menu, "ProGo Status.lnk"));
                            if (restored.RestoredOrigin != LegacyOrigin(restoredPin.SafeFileHandle) || !VerifyStartupSettlement()) return null;
                            if (preparation.Menu.Disposition == "preserved") {
                                // A read-only lease on the exact originally witnessed primary, never deletion/adoption.
                                primaryPin = OpenLegacyRestartRead(Path.Combine(owner.menu, "ProGo.lnk"));
                                if (!owner.VerifyLivePreservedPrimary(this) || !VerifyStartupSettlement()) return null;
                                token.ThrowIfCancellationRequested(); menuPreservedVerified = true;
                                return VerifyMenuSettlement() ? this : null;
                            }
                            token.ThrowIfCancellationRequested();
                            owner.TryRollbackDurableMenu(menuMap, token, this);
                            return VerifyMenuSettlement() ? this : null;
                        }) != null; }
                    } catch { return false; }
                }
            }
            public bool VerifyMenuSettlement()
            { lock (gate) { return owner.VerifyLiveMenuSettlement(this); } }
            public bool BeginAuxiliaryHandling()
            { lock (gate) { if (disposed || auxiliaryEntered) return false; auxiliaryEntered = true; return true; } }
            public AuxiliaryOutcomeResult RecordAuxiliaryOutcomeOnce(CancellationToken token)
            { return owner.RecordLiveAuxiliaryOutcome(this, token, null, null); }
            public LegacyRestoreResult RestoreProgress { get { lock (gate) { return restored; } } }
            public AuxiliaryOutcomeResult AuxiliaryPublication { get { lock (gate) { return auxiliaryResult; } } }
            public bool VerifyAuxiliaryOutcome()
            { lock (gate) { return owner.VerifyLiveAuxiliaryOutcome(this); } }
            public void Dispose()
            { lock (gate) { if (disposed) return; disposed = true;
                try { if (restoredPin != null) restoredPin.Dispose(); }
                finally { try { if (primaryPin != null) primaryPin.Dispose(); }
                    finally { try { lease.Dispose(); } finally { physical.Dispose(); } } } } }
        }
        // One terminal observation of all three auxiliary components, never restart/runtime authority.
        public sealed class AuxiliaryOutcomeResult
        {
            public bool AuxiliaryStateVerified { get; private set; }
            public bool AuxiliaryOutcomeRecorded { get; private set; }
            public bool Attempted { get; private set; }
            public bool Created { get; private set; }
            public long BytesWritten { get; private set; }
            public bool Flushed { get; private set; }
            public bool ReadbackVerified { get; private set; }
            public string Origin { get; private set; }
            public string Hash { get; private set; }
            public bool RestartAuthority { get { return false; } }
            public bool RuntimeRollbackAllowed { get { return false; } }
            public bool BackupCleanupAllowed { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            internal AuxiliaryOutcomeResult(bool verified, bool recorded, bool attempted, bool created, long written,
                bool flushed, bool readback, string origin, string hash)
            { AuxiliaryStateVerified = verified; AuxiliaryOutcomeRecorded = recorded; Attempted = attempted; Created = created;
                BytesWritten = written; Flushed = flushed; ReadbackVerified = readback; Origin = origin; Hash = hash; }
        }
        private string AuxiliaryOutcomePath(LegacyStartupContext context)
        { return Path.Combine(root, "backups", "installer-auxiliary-scoped-" + context.OperationId.ToString("N") + ".bin"); }
        private byte[] AuxiliaryOutcomeDocument(LegacyStartupContext context, string origin)
        {
            // Refer to existing canonical accepted/restore results; do not duplicate their documents.
            return Encoding.ASCII.GetBytes(String.Join("\n", new[] { "PROGO-AUXILIARY-SCOPED-1", context.session.ToString("N"),
                context.OperationId.ToString("N"), Identity(root), context.lease.reference.Source.SnapshotHash,
                context.lease.acceptedOutcome.Origin, context.lease.acceptedOutcome.Hash, context.restored.ResultOrigin,
                context.restored.ResultHash, context.restored.RestoredOrigin, context.preparation.OriginIdentity ?? "preserved",
                context.preparation.Hash ?? "absent", context.preparation.Menu.OriginIdentity ?? "preserved-original-evidence", context.preparation.Menu.Hash,
                context.preparation.Menu.Disposition == "preserved" ? "menu-preserved-no-mutation" : "menu-owned-deletion-absence",
                context.lease.replacementOrigin, context.lease.replacementHash,
                context.MenuDispositionAccepted ? "menu-disposition-accepted" : "menu-no-disposition",
                context.preparation.Menu.Disposition == "preserved" ? "menu-present-original" :
                    (context.MenuAbsenceObserved ? "menu-absence-observed" : "menu-absence-not-observed"),
                "legacy-restored-startup-original-menu-original", "runtime-unverified", "install-failed", "restart-blocked", origin, "" }));
        }
        private AuxiliaryOutcomeResult RecordLiveAuxiliaryOutcome(LegacyStartupContext context, CancellationToken token,
            string fault, CancellationTokenSource faultCancellation)
        {
            lock (context.gate) {
                if (context.auxiliaryResult != null) return context.auxiliaryResult;
                context.auxiliaryResult = new AuxiliaryOutcomeResult(false, false, false, false, 0, false, false, null, null);
                bool verified = false, attempted = false, created = false, flushed = false, readback = false, recorded = false;
                long written = 0; string origin = null, hash = null;
                try {
                    if (fault != null && Array.IndexOf(new[] { "write", "short-write", "flush", "readback", "readback-throw",
                        "cancel-created", "cancel-written", "cancel-flushed", "cancel-readback" }, fault) < 0) throw new ArgumentException("Unknown isolated auxiliary fault.");
                    lock (context.lease.gate) { context.physical.WithPins<LegacyStartupContext>(() => {
                        if (!Object.ReferenceEquals(context.owner, this) || !context.VerifyMenuSettlement()) return null;
                        verified = true; token.ThrowIfCancellationRequested();
                        string path = AuxiliaryOutcomePath(context); CheckLocation(path);
                        using (var rootPin = PinLegacySourceDirectory(root))
                        using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups"))) {
                            if (!context.VerifyMenuSettlement()) return null;
                            attempted = true;
                            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                                created = true; origin = LegacyOrigin(stream.SafeFileHandle);
                                byte[] document = AuxiliaryOutcomeDocument(context, origin);
                                if (fault == "cancel-created") faultCancellation.Cancel();
                                token.ThrowIfCancellationRequested();
                                if (fault == "write") throw new IOException("Synthetic auxiliary Write fault.");
                                int count = fault == "short-write" ? Math.Max(1, document.Length / 2) : document.Length;
                                try { stream.Write(document, 0, count); } finally { written = stream.Length; }
                                if (written != document.Length) throw new IOException("Auxiliary Write incomplete.");
                                if (fault == "cancel-written") faultCancellation.Cancel(); token.ThrowIfCancellationRequested();
                                if (fault == "flush") throw new IOException("Synthetic auxiliary Flush fault.");
                                stream.Flush(true); flushed = true;
                                if (fault == "cancel-flushed") faultCancellation.Cancel(); token.ThrowIfCancellationRequested();
                                if (fault == "readback") { stream.Position = 0; stream.WriteByte(0); stream.Flush(true); }
                                if (fault == "readback-throw") throw new IOException("Synthetic auxiliary readback fault.");
                                stream.Position = 0;
                                if (origin != LegacyOrigin(stream.SafeFileHandle) || !Equal(document, ReadLegacyReferenceBytes(stream))) throw new IOException("Auxiliary readback changed.");
                                readback = true;
                                if (fault == "cancel-readback") faultCancellation.Cancel(); token.ThrowIfCancellationRequested();
                                // The compatible reader permits this still-open READ|WRITE publisher, whose share denies external mutations.
                                using (var current = OpenLegacyAcceptedPublicationRead(path)) {
                                    if (origin != LegacyOrigin(current.SafeFileHandle) || !Equal(document, ReadLegacyReferenceBytes(current)) ||
                                        !context.VerifyMenuSettlement() || !Equal(document, AuxiliaryOutcomeDocument(context, origin))) throw new IOException("Auxiliary publication context changed.");
                                }
                                hash = Digest(document);
                            }
                        }
                        recorded = true; return context;
                    }); }
                } catch { /* Retain every partial/conflicting artifact; no overwrite, cleanup, repair or replay. */ }
                return context.auxiliaryResult = new AuxiliaryOutcomeResult(verified, recorded, attempted, created, written, flushed, readback, origin, hash);
            }
        }
        private bool VerifyLiveAuxiliaryOutcome(LegacyStartupContext context)
        {
            try {
                var result = context.auxiliaryResult;
                if (context.disposed || !Object.ReferenceEquals(context.owner, this) || result == null || !result.AuxiliaryOutcomeRecorded ||
                    !result.Created || !result.Flushed || !result.ReadbackVerified || !context.VerifyMenuSettlement()) return false;
                lock (context.lease.gate) { return context.physical.WithPins<LegacyStartupContext>(() => {
                    using (var current = OpenLegacyRestartRead(AuxiliaryOutcomePath(context))) {
                        byte[] document = AuxiliaryOutcomeDocument(context, result.Origin);
                        return result.Origin == LegacyOrigin(current.SafeFileHandle) && result.Hash == Digest(document) && result.BytesWritten == document.Length &&
                            Equal(document, ReadLegacyReferenceBytes(current)) && context.VerifyMenuSettlement() ? context : null;
                    }
                }) != null; }
            } catch { return false; }
        }
        private static System.Collections.IDictionary LivePreparationMap(object evidence)
        {
            var map = new System.Collections.Generic.Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var property in evidence.GetType().GetProperties()) {
                if (property.Name != "Menu" && property.Name != "Legacy") map.Add(property.Name, property.GetValue(evidence, null));
            }
            return map;
        }
        private static bool SameLivePreparation<T>(System.Collections.IDictionary map, T original) where T : class
        {
            var parsed = ReadDurableEvidence<T>(map);
            foreach (var property in typeof(T).GetProperties()) {
                if (property.Name != "Menu" && property.Name != "Legacy" && !Object.Equals(property.GetValue(parsed, null), property.GetValue(original, null))) return false;
            }
            return true;
        }
        private bool VerifyLivePreparationStartup(StartupInstallEvidence evidence)
        {
            if (!VerifyStartupInstallEvidence(evidence)) return false;
            if (evidence.Disposition == "preserved") return evidence.OriginIdentity == null;
            using (var stream = OpenLegacyRestartRead(startup)) {
                return evidence.OriginIdentity == LegacyOrigin(stream.SafeFileHandle) && evidence.Hash == Digest(ReadLegacyEvidenceBytes(stream));
            }
        }
        public LegacyStartupContext BindLegacyStartupContext(StartupInstallEvidence preparation, LegacyDeletionPins lease, LegacyPhysicalPins physical)
        {
            if (preparation == null || lease == null || physical == null) return null;
            lock (lease.gate) {
                try { return physical.WithPins<LegacyStartupContext>(() => {
                    if (lease.resources == null || lease.targetHandle.IsClosed || lease.lastResult != null || lease.consumed ||
                        lease.restoreResult != null || !physical.MatchesOwner(this) || !physical.VerifyPinned(CancellationToken.None) ||
                        preparation.Legacy == null || preparation.Menu == null || preparation.Legacy.Disposition != "preserved" ||
                        !preparation.Legacy.Requested || !preparation.Legacy.BeforeMatched || !preparation.Legacy.BeforeExists ||
                        preparation.Legacy.OriginIdentity != lease.reference.Source.OriginIdentity || preparation.Legacy.Hash != lease.reference.Source.Hash ||
                        preparation.Legacy.InstallIdentity != Identity(root) || preparation.Legacy.LocationIdentity != Identity(Path.Combine(menu, "ProGo Status.lnk")) ||
                        !VerifyLivePreparationStartup(preparation) || !VerifyDurableMenuRollbackEvidence(LivePreparationMap(preparation.Menu))) return null;
                    lease.target.Position = 0; byte[] bytes = ReadLegacyEvidenceBytes(lease.target);
                    try {
                        if (lease.reference.Source.OriginIdentity != LegacyOrigin(lease.target.SafeFileHandle) ||
                            !VerifyLegacyOperationEvidence(lease, bytes) || !VerifyLegacyOperationPlan(lease, physical)) return null;
                        return new LegacyStartupContext(this, preparation, lease, physical);
                    } finally { Array.Clear(bytes, 0, bytes.Length); }
                }); } catch { return null; }
            }
        }
        private bool VerifyLiveLegacyHandoff(LegacyStartupContext context)
        {
            try {
                return !context.disposed && Identity(context.owner.root) == Identity(root) && Identity(context.owner.menu) == Identity(menu) &&
                    Identity(context.owner.startup) == Identity(startup) && context.restored != null &&
                    Object.ReferenceEquals(context.restored, context.lease.restoreResult) &&
                    Object.ReferenceEquals(context.restored.AcceptedResult, context.lease.lastResult) &&
                    context.preparation.Legacy.OriginIdentity == context.lease.reference.Source.OriginIdentity &&
                    context.preparation.Legacy.Hash == context.lease.reference.Source.Hash &&
                    VerifyLegacyRestoredOutcome(context.lease, context.physical);
            } catch { return false; }
        }
        private bool VerifyLivePreservedPrimary(LegacyStartupContext context)
        {
            try {
                var primary = context.preparation.Menu;
                if (context.disposed || !Object.ReferenceEquals(context.owner, this) || primary.Schema != 1 ||
                    primary.Disposition != "preserved" || !primary.Requested || !primary.BeforeExists || !primary.Exists ||
                    primary.OriginIdentity != null || primary.BeforeHash == null || primary.BeforeHash != primary.Hash ||
                    primary.InstallIdentity != Identity(root) || primary.LocationIdentity != Identity(Path.Combine(menu, "ProGo.lnk")) ||
                    primary.Hash != context.lease.replacementHash || context.primaryPin == null || context.primaryPin.SafeFileHandle.IsClosed ||
                    !VerifyDurableMenuRollbackEvidence(context.menuMap) || !VerifyLegacyAcceptedReplacement(context.lease, context.primaryPin)) return false;
                using (var current = OpenLegacyRestartRead(Path.Combine(menu, "ProGo.lnk"))) {
                    return VerifyLegacyAcceptedReplacement(context.lease, current);
                }
            } catch { return false; }
        }
        private bool VerifyLiveMenuSettlement(LegacyStartupContext context)
        {
            if (context.disposed || !Object.ReferenceEquals(context.owner, this) || !context.menuConsumed ||
                !context.startupVerified ||
                context.restored == null || context.restoredPin == null || context.restoredPin.SafeFileHandle.IsClosed ||
                !Object.ReferenceEquals(context.restored, context.lease.restoreResult) ||
                !Object.ReferenceEquals(context.restored.AcceptedResult, context.lease.lastResult)) return false;
            lock (context.lease.gate) {
                byte[] document = null, original = null;
                try { return context.physical.WithPins<LegacyStartupContext>(() => {
                    var lease = context.lease; var result = context.restored; var primary = context.preparation.Menu;
                    var first = context.preparation;
                    if (lease.resources == null || lease.lastResult == null || !lease.lastResult.OperationAccepted ||
                        !lease.lastResult.AcceptanceKnown || !lease.lastResult.TargetHandleClosed || !lease.lastResult.ContextObservedAfterOperation ||
                        lease.acceptedOutcome == null || !lease.acceptedOutcome.DurableOutcomeRecorded ||
                        !context.physical.MatchesOwner(this) || !context.physical.VerifyPinned(CancellationToken.None) ||
                        !result.Created || !result.Flushed || !result.ReadbackVerified || !result.DurableResultRecorded ||
                        context.preparation.Legacy.OriginIdentity != lease.reference.Source.OriginIdentity || context.preparation.Legacy.Hash != lease.reference.Source.Hash) return null;
                    if (primary.Disposition == "preserved") {
                        if (!context.menuPreservedVerified || context.MenuAttempted || context.MenuDispositionAccepted || context.MenuAbsenceObserved ||
                            context.MenuHandleClosed || !VerifyLivePreservedPrimary(context)) return null;
                    } else {
                        if (!context.MenuDispositionAccepted || !context.MenuHandleClosed || primary.Schema != 1 || primary.Disposition != "created" ||
                            !primary.Requested || primary.BeforeExists || primary.BeforeHash != null || !primary.Exists || primary.Hash == null || primary.OriginIdentity == null ||
                            primary.InstallIdentity != Identity(root) || primary.LocationIdentity != Identity(Path.Combine(menu, "ProGo.lnk")) ||
                            primary.OriginIdentity != context.menuPrimaryOrigin || primary.OriginIdentity != lease.replacementOrigin || primary.Hash != lease.replacementHash) return null;
                        // Absence is only an expected result of this exact accepted live mutation, never admission.
                        string primaryPath = Path.Combine(menu, "ProGo.lnk"); CheckLocation(primaryPath);
                        try { File.GetAttributes(primaryPath); return null; } catch (FileNotFoundException) { }
                    }
                    bool startupOriginal = first.Disposition == "preserved" ? VerifyStartupInstallEvidence(first) :
                        first.Schema == 1 && first.Disposition == "created" && first.InstallIdentity == Identity(root) &&
                        first.LocationIdentity == Identity(startup) && !first.BeforeExists && first.BeforeHash == null && first.Exists &&
                        first.Hash != null && first.OriginIdentity != null && ReadStartupBytes() == null;
                    if (!startupOriginal) return null;
                    lease.evidence[0].Position = 0; document = ReadLegacySourceDocument(lease.evidence[0]);
                    int offset = 0, lines = 0; while (offset < document.Length && lines < 9) { if (document[offset++] == 10) lines++; }
                    if (lines != 9 || offset == document.Length) return null;
                    original = new byte[document.Length - offset]; Buffer.BlockCopy(document, offset, original, 0, original.Length);
                    if (!VerifyLegacyOperationEvidence(lease, original) || !VerifyLegacyOperationPlan(lease, context.physical) ||
                        result.BytesWritten != original.Length || result.RestoredOrigin == lease.reference.Source.OriginIdentity ||
                        result.RestoredOrigin != LegacyOrigin(context.restoredPin.SafeFileHandle)) return null;
                    context.restoredPin.Position = 0;
                    if (!Equal(original, ReadLegacyEvidenceBytes(context.restoredPin))) return null;
                    using (var current = OpenLegacyRestartRead(Path.Combine(menu, "ProGo Status.lnk")))
                    using (var accepted = OpenLegacyRestartRead(LegacyAcceptedPath(lease.reference.Source.Operation)))
                    using (var receipt = OpenLegacyRestartRead(LegacyRestoreResultPath(lease.reference.Source.Operation))) {
                        byte[] acceptedBytes = LegacyAcceptedDocument(lease, lease.acceptedOutcome.Origin);
                        byte[] restoreBytes = LegacyRestoreDocument(lease, result.RestoredOrigin, result.ResultOrigin, original);
                        return result.RestoredOrigin == LegacyOrigin(current.SafeFileHandle) && Equal(original, ReadLegacyEvidenceBytes(current)) &&
                            lease.acceptedOutcome.Origin == LegacyOrigin(accepted.SafeFileHandle) && lease.acceptedOutcome.Hash == Digest(acceptedBytes) &&
                            Equal(acceptedBytes, ReadLegacyReferenceBytes(accepted)) && result.ResultOrigin == LegacyOrigin(receipt.SafeFileHandle) &&
                            result.ResultHash == Digest(restoreBytes) && Equal(restoreBytes, ReadLegacyReferenceBytes(receipt)) ? context : null;
                    }
                }) != null; } catch { return false; }
                finally { if (document != null) Array.Clear(document, 0, document.Length); if (original != null) Array.Clear(original, 0, original.Length); }
            }
        }
        // Only the Legacy-state predicate differs in these typed-live overloads. Old APIs stay strict.
        public bool VerifyDurableInstallEvidence(System.Collections.IDictionary first, System.Collections.IDictionary primary,
            System.Collections.IDictionary legacy, LegacyStartupContext context)
        {
            if (context == null) return false;
            lock (context.gate) {
                try { return SameLivePreparation(first, context.preparation) && SameLivePreparation(primary, context.preparation.Menu) &&
                    SameLivePreparation(legacy, context.preparation.Legacy) && VerifyLiveLegacyHandoff(context) &&
                    VerifyLivePreparationStartup(ReadDurableEvidence<StartupInstallEvidence>(first)) && VerifyDurableMenuRollbackEvidence(primary); }
                catch { return false; }
            }
        }
        public bool VerifyDurableStartupOriginalResult(System.Collections.IDictionary first, System.Collections.IDictionary primary,
            System.Collections.IDictionary legacy, LegacyStartupContext context)
        {
            if (context == null) return false;
            lock (context.gate) {
                try {
                    if (!SameLivePreparation(first, context.preparation) || !SameLivePreparation(primary, context.preparation.Menu) ||
                        !SameLivePreparation(legacy, context.preparation.Legacy) || !VerifyLiveLegacyHandoff(context) ||
                        !VerifyDurableMenuRollbackEvidence(primary) || !context.startupVerified) return false;
                    var evidence = ReadDurableEvidence<StartupInstallEvidence>(first);
                    if (evidence.Disposition == "preserved") return VerifyStartupInstallEvidence(evidence);
                    return evidence.Disposition == "created" && evidence.InstallIdentity == Identity(root) &&
                        evidence.LocationIdentity == Identity(startup) && !evidence.BeforeExists && evidence.BeforeHash == null &&
                        evidence.Exists && evidence.Hash != null && evidence.OriginIdentity != null && ReadStartupBytes() == null;
                } catch { return false; }
            }
        }
        // Exact-owned-object operation only. No path-based delete, durable outcome or restore authority.
        public sealed class LegacyOwnedDeleteResult
        {
            private readonly LegacyJournalReferenceEvidence witness;
            public string State { get; private set; }
            public Guid OperationId { get; private set; }
            public string InstallIdentity { get; private set; }
            public string LocationIdentity { get; private set; }
            public string OriginalOrigin { get; private set; }
            public string OriginalHash { get; private set; }
            public bool OperationAttempted { get; private set; }
            public bool AcceptanceKnown { get; private set; }
            public bool OperationAccepted { get; private set; }
            public bool TargetHandleClosed { get; private set; }
            public bool ContextObservedAfterOperation { get; private set; }
            public bool DurableOutcomeRecorded { get { return false; } }
            public bool WholeRollbackVerified { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            internal LegacyOwnedDeleteResult(LegacyJournalReferenceEvidence witness, string state, bool attempted, bool known, bool accepted, bool closed, bool context)
            { this.witness = witness; OperationId = witness.Source.Operation; InstallIdentity = witness.Source.InstallIdentity; LocationIdentity = witness.Source.LocationIdentity;
              OriginalOrigin = witness.Source.OriginIdentity; OriginalHash = witness.Source.Hash;
              State = state; OperationAttempted = attempted; AcceptanceKnown = known; OperationAccepted = accepted;
              TargetHandleClosed = closed; ContextObservedAfterOperation = context; }
            public LegacyOwnedDeleteResult WithCleanupFailure()
            { return new LegacyOwnedDeleteResult(witness, OperationAccepted ? "accepted-cleanup-conflict-unrecorded" : "cleanup-conflict", OperationAttempted, AcceptanceKnown, OperationAccepted, TargetHandleClosed, false); }
        }
        // A durable observation of one live accepted disposition, never restore or unlink authority.
        public sealed class LegacyAcceptedOutcome
        {
            public LegacyOwnedDeleteResult AcceptedResult { get; private set; }
            public bool DurableOutcomeRecorded { get; private set; }
            public bool PhysicalUnlinkVerified { get { return false; } }
            public bool WholeRollbackVerified { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            public string Origin { get; private set; }
            public string Hash { get; private set; }
            internal LegacyAcceptedOutcome(LegacyOwnedDeleteResult accepted, bool recorded, string origin, string hash)
            { AcceptedResult = accepted; DurableOutcomeRecorded = recorded; Origin = origin; Hash = hash; }
        }
        private string LegacyAcceptedPath(Guid operation)
        { return Path.Combine(root, "backups", "installer-legacy-accepted-" + operation.ToString("N") + ".bin"); }
        private byte[] LegacyAcceptedDocument(LegacyDeletionPins lease, string origin)
        {
            var proof = lease.reference.Source;
            var text = new StringBuilder("PROGO-LEGACY-ACCEPTED-1\n");
            // Full context and actual native origins/hashes, not caller-supplied serialized authority.
            foreach (string value in new[] { proof.Operation.ToString("N"), proof.SnapshotHash, proof.InstallIdentity,
                proof.LocationIdentity, proof.OriginIdentity, proof.Hash, proof.StorageOrigin, proof.DocumentHash,
                lease.reference.Origin, lease.reference.Hash, lease.replacementOrigin, lease.replacementHash, origin, "accepted-disposition", "unlink-not-proven", "rollback-not-proven" })
                text.Append(value).Append('\n');
            foreach (var stream in lease.evidence) {
                stream.Position = 0;
                text.Append(LegacyOrigin(stream.SafeFileHandle)).Append('\n').Append(PhysicalPinHash(stream)).Append('\n');
            }
            return Encoding.ASCII.GetBytes(text.ToString());
        }
        private static bool VerifyLegacyAcceptedReplacement(LegacyDeletionPins lease, FileStream replacement)
        {
            replacement.Position = 0;
            return lease.replacementOrigin == LegacyOrigin(replacement.SafeFileHandle) &&
                lease.replacementHash == Digest(ReadLegacyEvidenceBytes(replacement));
        }
        private bool VerifyLegacyAcceptedContext(LegacyDeletionPins lease, LegacyPhysicalPins physical)
        {
            if (lease.resources == null || lease.lastResult == null || !lease.lastResult.OperationAccepted ||
                !lease.lastResult.AcceptanceKnown || !lease.lastResult.TargetHandleClosed || !lease.lastResult.ContextObservedAfterOperation ||
                physical == null || !physical.MatchesOwner(this) || !physical.VerifyPinned(CancellationToken.None)) return false;
            // A present/reappeared name is a conflict veto, never an object to adopt or overwrite.
            string originalPath = Path.Combine(menu, "ProGo Status.lnk"); CheckLocation(originalPath);
            // Unlike File.Exists, GetAttributes does not turn access/other probe errors into absence.
            try { File.GetAttributes(originalPath); return false; }
            catch (FileNotFoundException) { }
            // Extract original bytes from the still-pinned bounded source; canonical reconstruction below
            // validates every header byte, native origin, hash and full reference/seal/admission graph.
            lease.evidence[0].Position = 0;
            byte[] source = ReadLegacySourceDocument(lease.evidence[0]); int offset = 0, lines = 0;
            while (offset < source.Length && lines < 9) { if (source[offset++] == 10) lines++; }
            if (lines != 9 || offset == source.Length) return false;
            byte[] original = new byte[source.Length - offset]; Buffer.BlockCopy(source, offset, original, 0, original.Length);
            try { return VerifyLegacyOperationEvidence(lease, original) && VerifyLegacyOperationPlan(lease, physical); }
            finally { Array.Clear(original, 0, original.Length); Array.Clear(source, 0, source.Length); }
        }
        private static FileStream OpenLegacyAcceptedPublicationRead(string path)
        {
            // Symmetric Windows sharing: this reader must permit our still-open writer's access.
            // The writer itself shares READ only, preventing other write/delete opens. No following.
            var handle = OpenLegacyForDisposition(path, 0x80000000u, 3, IntPtr.Zero, 3, 0x00200000u, IntPtr.Zero);
            try {
                if (handle.IsInvalid) throw new IOException("Legacy accepted publication is unavailable.");
                LegacyOrigin(handle);
                return new FileStream(handle, FileAccess.Read);
            } catch { handle.Dispose(); throw; }
        }
        private LegacyAcceptedOutcome RecordLegacyAcceptedOutcome(LegacyDeletionPins lease, LegacyPhysicalPins physical, CancellationToken token)
        {
            lock (lease.gate) {
                if (lease.acceptedOutcome != null) return lease.acceptedOutcome;
                var failure = new LegacyAcceptedOutcome(lease.lastResult, false, null, null);
                // Cancellation after acceptance never erases the real result or claims a no-op.
                lease.acceptedOutcome = failure;
                try {
                    var result = physical == null ? null : physical.WithPins<LegacyAcceptedOutcome>(() => {
                        if (token.IsCancellationRequested || !VerifyLegacyAcceptedContext(lease, physical)) return failure;
                        string path = LegacyAcceptedPath(lease.reference.Source.Operation); CheckLocation(path);
                        using (var replacement = OpenLegacyRestartRead(Path.Combine(menu, "ProGo.lnk"))) {
                            if (!VerifyLegacyAcceptedReplacement(lease, replacement)) return failure;
                            using (var rootPin = PinLegacySourceDirectory(root))
                            using (var backupPin = PinLegacySourceDirectory(Path.Combine(root, "backups")))
                            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.WriteThrough)) {
                                string origin = LegacyOrigin(stream.SafeFileHandle);
                                byte[] expected = LegacyAcceptedDocument(lease, origin);
                                stream.Write(expected, 0, expected.Length); stream.Flush(true); stream.Position = 0;
                                if (origin != LegacyOrigin(stream.SafeFileHandle) || !Equal(expected, ReadLegacyReferenceBytes(stream))) return failure;
                                using (var current = OpenLegacyAcceptedPublicationRead(path)) {
                                    if (origin != LegacyOrigin(current.SafeFileHandle) || !Equal(expected, ReadLegacyReferenceBytes(current)) ||
                                        token.IsCancellationRequested || !VerifyLegacyAcceptedReplacement(lease, replacement) || !VerifyLegacyAcceptedContext(lease, physical) ||
                                        !Equal(expected, LegacyAcceptedDocument(lease, origin))) return failure;
                                }
                                return new LegacyAcceptedOutcome(lease.lastResult, true, origin, Digest(expected));
                            }
                        }
                    });
                    lease.acceptedOutcome = result ?? failure;
                } catch { /* Keep any partial/ambiguous artifact; no overwrite, delete, success or replay. */ }
                return lease.acceptedOutcome;
            }
        }
        private bool VerifyLegacyAcceptedOutcome(LegacyDeletionPins lease, LegacyPhysicalPins physical)
        {
            lock (lease.gate) {
                try {
                    var receipt = lease.acceptedOutcome;
                    if (receipt == null || !receipt.DurableOutcomeRecorded || physical == null) return false;
                    var observed = physical.WithPins<LegacyAcceptedOutcome>(() => {
                        if (!VerifyLegacyAcceptedContext(lease, physical)) return null;
                        using (var replacement = OpenLegacyRestartRead(Path.Combine(menu, "ProGo.lnk")))
                        using (var stream = OpenLegacyRestartRead(LegacyAcceptedPath(lease.reference.Source.Operation))) {
                            byte[] expected = LegacyAcceptedDocument(lease, receipt.Origin);
                            return receipt.Origin == LegacyOrigin(stream.SafeFileHandle) && receipt.Hash == Digest(expected) &&
                                Equal(expected, ReadLegacyReferenceBytes(stream)) && VerifyLegacyAcceptedReplacement(lease, replacement) &&
                                VerifyLegacyAcceptedContext(lease, physical) ? receipt : null;
                        }
                    });
                    return observed != null;
                } catch { return false; }
            }
        }
        private static System.Collections.Generic.IDictionary<string, object> LegacyJson(byte[] bytes, params string[] keys)
        {
            if (bytes == null || bytes.Length < 1 || bytes.Length > 1048576) throw new IOException("Legacy document is invalid.");
            // Existing .NET Framework dependency, already referenced by the application build.
            // Reflection keeps the standalone shortcut Add-Type source independent of compiler reference lists.
            var type = Type.GetType("System.Web.Script.Serialization.JavaScriptSerializer, System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", true);
            var value = type.GetMethod("DeserializeObject", new[] { typeof(string) }).Invoke(Activator.CreateInstance(type),
                new object[] { new UTF8Encoding(false, true).GetString(bytes) });
            return LegacyJsonObject(value, keys);
        }
        private static System.Collections.Generic.IDictionary<string, object> LegacyJsonObject(object value, params string[] keys)
        {
            var result = value as System.Collections.Generic.IDictionary<string, object>;
            if (result == null || result.Count != keys.Length) throw new IOException("Legacy schema is invalid.");
            foreach (string key in keys) if (!result.ContainsKey(key)) throw new IOException("Legacy schema is incomplete.");
            return result;
        }
        private static bool LegacySchemaOne(object value) { return value is int && (int)value == 1; }
        private static bool LegacyFalse(object value) { return value is bool && !(bool)value; }
        private static bool LegacySameEntry(object first, object second)
        {
            var a = LegacyJsonObject(first, "Relative", "Exists", "Hash", "NewHash");
            var b = LegacyJsonObject(second, "Relative", "Exists", "Hash", "NewHash");
            foreach (string key in a.Keys) if (!Object.Equals(a[key], b[key])) return false;
            return true;
        }
        private static bool LegacyOperationNamespace(string directory, System.Collections.Generic.Dictionary<string, string> plan)
        {
            var expectedFiles = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var expectedDirectories = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            string prefix = directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string file in plan.Keys) {
                if (!file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                expectedFiles.Add(file);
                for (string parent = Path.GetDirectoryName(file); !String.Equals(parent, directory, StringComparison.OrdinalIgnoreCase); parent = Path.GetDirectoryName(parent)) {
                    if (String.IsNullOrEmpty(parent) || !parent.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
                    expectedDirectories.Add(parent);
                }
            }
            if (expectedFiles.Count == 0) return false;
            var seenFiles = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var seenDirectories = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var pending = new System.Collections.Generic.Stack<string>(); pending.Push(directory);
            while (pending.Count > 0) {
                string current = pending.Pop();
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                foreach (string path in Directory.EnumerateFileSystemEntries(current)) {
                    FileAttributes attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) return false;
                    if ((attributes & FileAttributes.Directory) != 0) {
                        if (!expectedDirectories.Contains(path) || !seenDirectories.Add(path)) return false;
                        pending.Push(path);
                    } else if (!expectedFiles.Contains(path) || !seenFiles.Add(path)) return false;
                }
            }
            return seenFiles.SetEquals(expectedFiles) && seenDirectories.SetEquals(expectedDirectories);
        }
        private bool VerifyLegacyOperationPlan(LegacyDeletionPins lease, LegacyPhysicalPins physical)
        {
            var reference = lease.reference; var proof = reference.Source;
            if (!physical.MatchesOwner(this)) return false;
            lease.evidence[1].Position = 0;
            var doc = LegacyJson(ReadLegacyReferenceBytes(lease.evidence[1]), "Context", "Source");
            var source = LegacyJsonObject(doc["Source"], "LocationIdentity", "OriginIdentity", "Hash", "StorageOrigin", "DocumentHash");
            if (!Object.Equals(source["LocationIdentity"], proof.LocationIdentity) || !Object.Equals(source["OriginIdentity"], proof.OriginIdentity) ||
                !Object.Equals(source["Hash"], proof.Hash) || !Object.Equals(source["StorageOrigin"], proof.StorageOrigin) || !Object.Equals(source["DocumentHash"], proof.DocumentHash)) return false;
            var context = LegacyJsonObject(doc["Context"], "Schema", "Phase", "MutationAllowed", "FullRollback", "InstallSucceeded", "Operation", "Identity", "Journal", "Snapshot", "SnapshotHash", "Inventory", "Records");
            string journal = "installer-journal-" + proof.Operation.ToString("N"), snapshot = context["Snapshot"] as string;
            if (!LegacySchemaOne(context["Schema"]) || !Object.Equals(context["Phase"], "legacy-source-reference") ||
                !LegacyFalse(context["MutationAllowed"]) || !LegacyFalse(context["FullRollback"]) || !LegacyFalse(context["InstallSucceeded"]) ||
                !Object.Equals(context["Operation"], proof.Operation.ToString("N")) || !Object.Equals(context["Identity"], Identity(root)) ||
                !Object.Equals(context["SnapshotHash"], proof.SnapshotHash) || !Object.Equals(context["Journal"], journal) || snapshot == null ||
                !System.Text.RegularExpressions.Regex.IsMatch(snapshot, "^installer-[a-f0-9]{32}$")) return false;
            string inventoryText = context["Inventory"] as string; if (inventoryText == null) return false;
            var inventory = LegacyJson(Encoding.UTF8.GetBytes(inventoryText), "Schema", "SnapshotHash", "Files");
            var entries = inventory["Files"] as System.Collections.IList; var records = context["Records"] as System.Collections.IList;
            if (!LegacySchemaOne(inventory["Schema"]) || !Object.Equals(inventory["SnapshotHash"], proof.SnapshotHash) || entries == null || records == null ||
                entries.Count != LegacyOperationLeaves.Length || records.Count != entries.Count) return false;
            var expected = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            string snapshotRoot = Path.Combine(root, "backups", snapshot), journalRoot = Path.Combine(root, "backups", journal);
            string manifestPath = Path.Combine(snapshotRoot, "installer-snapshot.json"); expected.Add(manifestPath, proof.SnapshotHash);
            for (int i = 0; i < entries.Count; i++) {
                var entry = LegacyJsonObject(entries[i], "Relative", "Exists", "Hash", "NewHash");
                string leaf = entry["Relative"] as string, oldHash = entry["Hash"] as string, newHash = entry["NewHash"] as string;
                if (leaf == null || Array.IndexOf(LegacyOperationLeaves, leaf) < 0 || !seen.Add(leaf) || !(entry["Exists"] is bool) || !LegacySourceHash(newHash)) return false;
                bool exists = (bool)entry["Exists"];
                if (exists ? !LegacySourceHash(oldHash) : entry["Hash"] != null) return false;
                expected.Add(Path.GetFullPath(Path.Combine(root, leaf)), newHash);
                if (exists) expected.Add(Path.GetFullPath(Path.Combine(snapshotRoot, leaf)), oldHash);
                var record = LegacyJsonObject(records[i], "Name", "Hash");
                string name = "admit-" + i.ToString("D6") + ".json", digest = record["Hash"] as string;
                if (!Object.Equals(record["Name"], name) || !LegacySourceHash(digest)) return false;
                expected.Add(Path.Combine(journalRoot, name), digest);
            }
            if (!physical.MatchesPlan(expected)) return false;
            var manifest = LegacyJson(physical.ReadDocument(manifestPath), "Schema", "NewVersion", "Files");
            var saved = manifest["Files"] as System.Collections.IList;
            if (!LegacySchemaOne(manifest["Schema"]) || !(manifest["NewVersion"] is string) || saved == null || saved.Count != entries.Count) return false;
            for (int i = 0; i < entries.Count; i++) {
                if (!LegacySameEntry(saved[i], entries[i])) return false;
                var record = LegacyJson(physical.ReadDocument(Path.Combine(journalRoot, "admit-" + i.ToString("D6") + ".json")), "Schema", "Phase", "Sequence", "Inventory", "Entry");
                if (!LegacySchemaOne(record["Schema"]) || !Object.Equals(record["Phase"], "admitted") || !(record["Sequence"] is int) || (int)record["Sequence"] != i ||
                    !Object.Equals(record["Inventory"], inventoryText) || !LegacySameEntry(record["Entry"], entries[i])) return false;
            }
            return LegacyOperationNamespace(snapshotRoot, expected) && LegacyOperationNamespace(journalRoot, expected);
        }
        private bool VerifyLegacyOperationEvidence(LegacyDeletionPins lease, byte[] bytes)
        {
            var proof = lease.reference.Source;
            foreach (var stream in lease.evidence) stream.Position = 0;
            if (proof.InstallIdentity != Identity(root) || proof.LocationIdentity != Identity(Path.Combine(menu, "ProGo Status.lnk")) ||
                proof.Hash != Digest(bytes) || proof.StorageOrigin != LegacyOrigin(lease.evidence[0].SafeFileHandle)) return false;
            byte[] expectedSource = LegacySourceDocument(proof.Operation, proof.SnapshotHash, proof.InstallIdentity, proof.LocationIdentity,
                proof.OriginIdentity, proof.Hash, proof.StorageOrigin, bytes);
            if (Digest(expectedSource) != proof.DocumentHash || !Equal(expectedSource, ReadLegacySourceDocument(lease.evidence[0])) ||
                lease.reference.Origin != LegacyOrigin(lease.evidence[1].SafeFileHandle) || lease.reference.Hash != Digest(ReadLegacyReferenceBytes(lease.evidence[1]))) return false;
            string sealOrigin = LegacyOrigin(lease.evidence[2].SafeFileHandle); byte[] seal = ReadLegacyReferenceBytes(lease.evidence[2]);
            return Equal(seal, LegacyRestartSealDocument(proof.Operation, proof.SnapshotHash, lease.reference, sealOrigin)) &&
                Equal(ReadLegacyReferenceBytes(lease.evidence[3]), LegacyPreDeleteDocument(proof.Operation, proof.SnapshotHash, lease.reference, sealOrigin, seal, LegacyOrigin(lease.evidence[3].SafeFileHandle)));
        }
        private LegacyOwnedDeleteResult ExecuteLegacyOwnedDeletion(LegacyDeletionPins lease, LegacyPhysicalPins physical, Func<bool> observeContext, CancellationToken token)
        {
            lock (lease.gate) {
                if (lease.lastResult != null) return lease.lastResult; // accepted result survives cancellation/disposal/re-entry
                token.ThrowIfCancellationRequested();
                if (lease.consumed) return new LegacyOwnedDeleteResult(lease.reference, "in-progress", false, false, false, false, false);
                lease.consumed = true;
                bool attempted = false, known = true, accepted = false, closed = false, after = false, targetAfter = false;
                try {
                    var result = physical == null ? null : physical.WithPins<LegacyOwnedDeleteResult>(() => {
                      using (var replacement = OpenLegacyRestartRead(Path.Combine(menu, "ProGo.lnk"))) {
                        string replacementOrigin = LegacyOrigin(replacement.SafeFileHandle);
                        byte[] replacementBytes = ReadLegacyEvidenceBytes(replacement);
                        byte[] bytes = null;
                        try {
                            if (lease.resources == null || lease.targetHandle.IsClosed || observeContext == null || !physical.MatchesOwner(this) || !observeContext() || lease.resources == null ||
                                !physical.VerifyPinned(token) || !Matches(Path.Combine(menu, "ProGo.lnk"), "--show")) return new LegacyOwnedDeleteResult(lease.reference, "blocked", false, true, false, false, false);
                            lease.target.Position = 0; bytes = ReadLegacyEvidenceBytes(lease.target);
                            if (!VerifyLegacyOperationEvidence(lease, bytes) || !VerifyLegacyOperationPlan(lease, physical) ||
                                lease.reference.Source.OriginIdentity != LegacyOrigin(lease.target.SafeFileHandle) || !physical.VerifyPinned(token))
                                return new LegacyOwnedDeleteResult(lease.reference, "blocked", false, true, false, false, false);
                            // Final same-handle origin/bytes check; metadata is observed, not promised frozen.
                            lease.target.Position = 0;
                            if (lease.reference.Source.Hash != Digest(ReadLegacyEvidenceBytes(lease.target)) ||
                                lease.reference.Source.OriginIdentity != LegacyOrigin(lease.target.SafeFileHandle)) return new LegacyOwnedDeleteResult(lease.reference, "blocked", false, true, false, false, false);
                            replacement.Position = 0;
                            if (replacementOrigin != LegacyOrigin(replacement.SafeFileHandle) || !Equal(replacementBytes, ReadLegacyEvidenceBytes(replacement)))
                                return new LegacyOwnedDeleteResult(lease.reference, "blocked", false, true, false, false, false);
                            if (!VerifyLegacyOperationEvidence(lease, bytes)) return new LegacyOwnedDeleteResult(lease.reference, "blocked", false, true, false, false, false);
                            token.ThrowIfCancellationRequested();
                            lease.replacementOrigin = replacementOrigin; lease.replacementHash = Digest(replacementBytes);
                            var disposition = new LegacyDisposition { DeleteFile = 1 };
                            attempted = true; known = false;
                            accepted = SetFileInformationByHandle(lease.target.SafeFileHandle, 4, ref disposition, 1); known = true;
                            if (accepted) { lease.target.Position = 0; targetAfter = lease.reference.Source.OriginIdentity == LegacyOrigin(lease.target.SafeFileHandle) && lease.reference.Source.Hash == Digest(ReadLegacyEvidenceBytes(lease.target)); }
                        } finally {
                            if (attempted) { lease.target.Dispose(); closed = true; }
                        }
                        // Never throw late cancellation away as if the accepted disposition were a no-op.
                        if (accepted) {
                            try { replacement.Position = 0; after = targetAfter && observeContext() && physical.VerifyPinned(CancellationToken.None) && VerifyLegacyOperationEvidence(lease, bytes) && VerifyLegacyOperationPlan(lease, physical) && replacementOrigin == LegacyOrigin(replacement.SafeFileHandle) && Equal(replacementBytes, ReadLegacyEvidenceBytes(replacement)); }
                            catch { after = false; }
                        }
                        return new LegacyOwnedDeleteResult(lease.reference, accepted ? (after ? "accepted-unrecorded" : "accepted-context-conflict-unrecorded") : "refused", attempted, known, accepted, closed, after);
                      }
                    });
                    lease.lastResult = result ?? new LegacyOwnedDeleteResult(lease.reference, "blocked", false, true, false, false, false);
                } catch (OperationCanceledException) {
                    lease.lastResult = new LegacyOwnedDeleteResult(lease.reference, attempted ? (accepted ? "accepted-context-conflict-unrecorded" : "indeterminate") : "cancelled-before-operation", attempted, known, accepted, closed, false);
                    if (!attempted) throw;
                } catch {
                    lease.lastResult = new LegacyOwnedDeleteResult(lease.reference, accepted ? "accepted-context-conflict-unrecorded" : (attempted ? "indeterminate" : "blocked"), attempted, known, accepted, closed, false);
                }
                return lease.lastResult;
            }
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
        // Pin-only prerequisite. Explicit one-shot operation independently revalidates all positive proofs.
        public sealed class LegacyDeletionPins : IDisposable
        {
            internal readonly object gate = new object();
            internal System.Collections.Generic.List<IDisposable> resources;
            internal readonly ApplicationShortcuts owner;
            internal readonly FileStream target;
            internal readonly SafeFileHandle targetHandle;
            internal readonly LegacyJournalReferenceEvidence reference;
            internal readonly System.Collections.Generic.List<FileStream> evidence = new System.Collections.Generic.List<FileStream>();
            internal bool consumed;
            internal LegacyOwnedDeleteResult lastResult;
            internal LegacyAcceptedOutcome acceptedOutcome;
            internal LegacyRestoreResult restoreResult;
            internal string replacementOrigin, replacementHash;
            internal LegacyDeletionPins(ApplicationShortcuts manager, System.Collections.Generic.List<IDisposable> held, FileStream original, LegacyJournalReferenceEvidence witness)
            {
                resources = held; owner = manager; target = original; targetHandle = original.SafeFileHandle; reference = witness;
                foreach (var resource in held) { var stream = resource as FileStream; if (stream != null && stream != original) evidence.Add(stream); }
                if (evidence.Count != 4) throw new IOException("Legacy pin graph is incomplete.");
            }
            // Default production path has no caller-supplied authorization boolean. Full live native
            // namespace/positive-plan validation is mandatory; the overload is only an additional veto.
            public LegacyOwnedDeleteResult DeleteOwnedOnce(LegacyPhysicalPins physical, CancellationToken token)
            { return owner.ExecuteLegacyOwnedDeletion(this, physical, () => true, token); }
            public LegacyOwnedDeleteResult DeleteOwnedOnce(LegacyPhysicalPins physical, Func<bool> observeContext, CancellationToken token)
            { return owner.ExecuteLegacyOwnedDeletion(this, physical, observeContext, token); }
            public LegacyAcceptedOutcome RecordAcceptedOutcome(LegacyPhysicalPins physical, CancellationToken token)
            { return owner.RecordLegacyAcceptedOutcome(this, physical, token); }
            public bool VerifyAcceptedOutcome(LegacyPhysicalPins physical)
            { return owner.VerifyLegacyAcceptedOutcome(this, physical); }
            public LegacyRestoreResult RestoreOwnedOnce(LegacyPhysicalPins physical, CancellationToken token)
            { return owner.RestoreLegacyOwnedOnce(this, physical, token, null, null); }
            public bool VerifyRestoredOutcome(LegacyPhysicalPins physical)
            { return owner.VerifyLegacyRestoredOutcome(this, physical); }
            public bool IsPinned { get { lock (gate) { return resources != null && !targetHandle.IsClosed; } } }
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
                var result = new LegacyDeletionPins(this, held, original, reference); transferred = true; return result;
            } catch (OperationCanceledException) { throw; }
            catch { return null; }
            finally { if (!transferred) ReleaseLegacyDeletionPins(held); }
        }

        // Existing physical material only. No namespace/absence or mutation authority.
        public sealed class LegacyPhysicalPins : IDisposable
        {
            private readonly object gate = new object();
            private System.Collections.Generic.List<IDisposable> held;
            private readonly System.Collections.Generic.List<FileStream> files;
            private readonly ApplicationShortcuts owner;
            private readonly System.Collections.Generic.List<string> paths;
            private readonly System.Collections.Generic.List<string> origins, hashes;
            internal LegacyPhysicalPins(ApplicationShortcuts manager, System.Collections.Generic.List<string> pinnedPaths, System.Collections.Generic.List<IDisposable> resources,
                System.Collections.Generic.List<FileStream> streams, System.Collections.Generic.List<string> native,
                System.Collections.Generic.List<string> digests)
            { owner = manager; paths = pinnedPaths; held = resources; files = streams; origins = native; hashes = digests; }
            internal T WithPins<T>(Func<T> body) where T : class { lock (gate) { return held == null ? null : body(); } }
            internal bool MatchesOwner(ApplicationShortcuts manager)
            { return String.Equals(owner.root, manager.root, StringComparison.OrdinalIgnoreCase) && String.Equals(owner.menu, manager.menu, StringComparison.OrdinalIgnoreCase) && String.Equals(owner.startup, manager.startup, StringComparison.OrdinalIgnoreCase); }
            internal bool MatchesPlan(System.Collections.Generic.Dictionary<string, string> expected)
            {
                if (held == null || paths.Count != expected.Count) return false;
                for (int i = 0; i < paths.Count; i++) { string hash; if (!expected.TryGetValue(paths[i], out hash) || hash != hashes[i]) return false; }
                return true;
            }
            internal byte[] ReadDocument(string path)
            {
                for (int i = 0; i < paths.Count; i++) if (String.Equals(paths[i], path, StringComparison.OrdinalIgnoreCase)) { files[i].Position = 0; return ReadLegacyReferenceBytes(files[i]); }
                throw new IOException("Legacy physical document is not pinned.");
            }
            public bool MutationAllowed { get { return false; } }
            public bool FullRollback { get { return false; } }
            public bool InstallSucceeded { get { return false; } }
            public bool VerifyPinned(CancellationToken token)
            {
                lock (gate) {
                    token.ThrowIfCancellationRequested();
                    if (held == null) return false;
                    try {
                        for (int i = 0; i < files.Count; i++) {
                            token.ThrowIfCancellationRequested();
                            if (origins[i] != LegacyOrigin(files[i].SafeFileHandle) || hashes[i] != PhysicalPinHash(files[i])) return false;
                        }
                        token.ThrowIfCancellationRequested(); return true;
                    } catch (OperationCanceledException) { throw; } catch { return false; }
                }
            }
            public void Dispose()
            {
                lock (gate) {
                    if (held == null) return;
                    var resources = held; held = null;
                    try { ReleaseLegacyDeletionPins(resources); }
                    finally { files.Clear(); origins.Clear(); hashes.Clear(); }
                }
            }
        }
        private static string PhysicalPinHash(FileStream stream)
        {
            stream.Position = 0;
            using (var hash = SHA256.Create()) {
                string result = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "");
                stream.Position = 0; return result;
            }
        }
        // Relative leaves are a read/pin plan, NOT evidence. The trusted caller must independently
        // verify the full canonical journal/snapshot/runtime graph while these pins are held.
        public LegacyPhysicalPins PinLegacyPhysicalMaterial(string[] relativeFiles, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var held = new System.Collections.Generic.List<IDisposable>();
            var files = new System.Collections.Generic.List<FileStream>();
            var origins = new System.Collections.Generic.List<string>(); var hashes = new System.Collections.Generic.List<string>();
            bool transferred = false;
            try {
                if (relativeFiles == null || relativeFiles.Length < 1 || relativeFiles.Length > 256) return null;
                var paths = new System.Collections.Generic.List<string>();
                var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var parents = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string relative in (string[])relativeFiles.Clone()) {
                    if (String.IsNullOrEmpty(relative) || Path.IsPathRooted(relative) || relative.IndexOf(':') >= 0) return null;
                    foreach (string part in relative.Replace('\\', '/').Split('/'))
                        if (part.Length == 0 || part == "." || part == "..") return null;
                    string path = Path.GetFullPath(Path.Combine(root, relative));
                    if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !seen.Add(path)) return null;
                    CheckLocation(path); paths.Add(path);
                }
                // Pin every ancestor, including independently configured Menu/Startup locations.
                foreach (string leaf in paths.ToArray()) {
                    for (string parent = Path.GetDirectoryName(leaf); !String.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
                        if (parents.Add(parent)) held.Add(PinLegacySourceDirectory(parent));
                }
                foreach (string leaf in new[] { Path.Combine(menu, "ProGo Status.lnk"), startup }) {
                    for (string parent = Path.GetDirectoryName(leaf); !String.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
                        if (parents.Add(parent)) held.Add(PinLegacySourceDirectory(parent));
                }
                foreach (string path in paths) {
                    token.ThrowIfCancellationRequested();
                    var stream = OpenLegacyRestartRead(path); held.Add(stream); files.Add(stream);
                    origins.Add(LegacyOrigin(stream.SafeFileHandle)); hashes.Add(PhysicalPinHash(stream));
                }
                var result = new LegacyPhysicalPins(this, paths, held, files, origins, hashes);
                if (!result.VerifyPinned(token)) return null;
                transferred = true; return result;
            } catch (OperationCanceledException) { throw; } catch { return null; }
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

        private LegacyInstallEvidence MigrateLegacyWithEvidence(bool requested, bool preserveOnly = false)
        {
            string path = Path.Combine(menu, "ProGo Status.lnk");
            var before = ObserveLegacy(requested); bool removed = false; string replacement = null;
            if (requested && before.Matched && !preserveOnly) {
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
        { return TryRollbackDurableMenu(receipt, token, null); }
        private bool TryRollbackDurableMenu(System.Collections.IDictionary receipt, CancellationToken token, LegacyStartupContext context)
        {
            try {
                token.ThrowIfCancellationRequested();
                var evidence = ReadDurableEvidence<MenuInstallEvidence>(receipt);
                if (!VerifyDurableMenuRollbackEvidence(receipt)) return false;
                if (evidence.Disposition == "preserved") return true;
                string path = Path.Combine(menu, "ProGo.lnk");
                CheckLocation(path);
                if (context != null) context.MenuAttempted = true;
                try {
                using (var handle = OpenLegacyForDisposition(path, 0x80010000u, 1, IntPtr.Zero, 3, 0x00200080u, IntPtr.Zero)) {
                    if (handle.IsInvalid) return false;
                    using (var stream = new FileStream(handle, FileAccess.Read)) {
                        // Full native origin (including creation), not bytes alone. Read/delete
                        // share denies external replacement or writes until this handle settles.
                        if (evidence.OriginIdentity != LegacyOrigin(handle) || evidence.Hash != Digest(ReadLegacyEvidenceBytes(stream))) return false;
                        if (context != null && (!Object.ReferenceEquals(context.owner, this) || context.disposed ||
                            context.restoredPin == null || context.restored.RestoredOrigin != LegacyOrigin(context.restoredPin.SafeFileHandle))) return false;
                        token.ThrowIfCancellationRequested();
                        var disposition = new LegacyDisposition { DeleteFile = 1 };
                        if (!SetFileInformationByHandle(handle, 4, ref disposition, 1)) return false;
                        if (context != null) { context.menuPrimaryOrigin = evidence.OriginIdentity; context.MenuDispositionAccepted = true; }
                    }
                }
                } finally { if (context != null) context.MenuHandleClosed = true; }
                // A late cancellation must not discard the accepted physical result.
                bool absent = ReadMenuBytes() == null;
                if (context != null) context.MenuAbsenceObserved = absent;
                return absent;
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

        // Preparation only: preserve the exact Legacy object while ensuring the requested primary/Startup.
        // Existing evidence schema is reused; it grants no owned deletion, restoration or install success.
        public StartupInstallEvidence PrepareForOwnedLegacyOperation(bool wasInstalled, bool noStartup, bool noStartMenu)
        {
            var evidence = InstallCore(wasInstalled, noStartup, noStartMenu, true, true);
            if (!VerifyStartupInstallEvidence(evidence) || !VerifyMenuInstallEvidence(evidence.Menu) ||
                !VerifyLegacyInstallEvidence(evidence.Legacy) || evidence.Legacy.Disposition != "preserved")
                throw new IOException("Legacy preparation evidence changed.");
            return evidence;
        }

        private StartupInstallEvidence InstallCore(bool wasInstalled, bool noStartup, bool noStartMenu, bool captureStartup, bool preserveLegacy = false)
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
            var legacyEvidence = captureStartup ? MigrateLegacyWithEvidence(!noStartMenu, preserveLegacy) : null;
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
