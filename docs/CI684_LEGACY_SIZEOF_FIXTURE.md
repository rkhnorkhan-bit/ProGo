# CI684: exact Marshal.SizeOf(Type) fixture overload

Baseline source 2357a85691f17024a8a20381faa6a5051479c715, tested merge
488061a811c1609f6bc8e4cc0a3eb6657e9837c8; run40142564635 Windows121017174243 FAILURE,
Ubuntu121017174418 SUCCESS. Full Windows log independently read.

InstallerLegacyEvidenceTests.ps1:19 failed before any Legacy14 scenario:
SizeOf rejected System.RuntimeType as an unmanaged structure. The dynamic call did
not marshal the represented native structure. This is a fixture overload error,
not evidence against production handle ownership/deletion.

Select the exact non-generic SizeOf(System.Type) MethodInfo by its parameter type,
assert its signature, and invoke it with each original nested Type. The original
ABI assertion still requires LegacyDisposition=1 and LegacyFileInfo=52 bytes.
All scenario code, native declarations, assertions, guards and timeouts unchanged.
Repository-wide tests search found only these two SizeOf calls; production Marshal
calls are unchanged. No production changes or Windows PASS inferred locally.

CI684 scoped PASS: repair null interop1, preflight4/runtime copy/committed cleanup/
rollback/owned self-check/durable recovery; SOCKS51, Desktop6394 (PID fixture fix),
Instance30, Maintenance126, Shutdown47, installer Payload9/Maintenance19/Stage8/
Backup8/SelfCheck9/Rollback8/Journal8/RestartGate8/Outcome10/RollbackOutcome12/
Reconcile13/PendingProof11/Startup13/Menu14. Legacy14 NOT_RUN; relay/pack SKIPPED.

Incident ledger: PID double-read fixture fix now scoped Windows PASS on this merge.
Repair20s, Python startup, listener release and receipt-sharing causes remain UNKNOWN;
non-reproduction is not a runtime fix. Diagnostic instrumentation is preserved.
Private candidate remains BLOCKED_TRANSFER; no transfer retry. No score increase.

Acceptance: new exact-SHA Windows must execute unchanged native ABI and all Legacy14
scenarios, then report downstream suites only if reached. Until then NOT_RUN.
No pending publication, runtime recovery, install, service/security/network action,
release or binary upload is part of this fix.
