# Microtask 01: restore preparation cancellation

Scope: `BackupWorkSession.cs`, `BackupIntegrity.cs`, `DesktopAsyncRestoreTests.cs`.

Baseline: candidate `a41bbcf41ab17a79245f4f5ae14cfd4c0b52f690`; Windows CI run `38083031654`, job `114303686547`, failed in `AsyncRestoreOwnedCopyDisposal`.

## Change

A linked cancellation token thrown by synchronous `Task.Run` was published as a faulted task on .NET Framework. The async wrapper now publishes cancellation and settles with `ConfigureAwait(false)`, without depending on the Windows UI message pump. An already accepted helper handoff remains accepted if cancellation arrives later.

A prepared restore directory registered with its session has one cleanup owner. Failed preparation leaves that directory to the session, which waits for the worker before deleting its exact path. Standalone preparation still cleans up its own directory.

Regression coverage keeps the strict `IsCanceled` assertion, holds cleanup after worker settlement to verify ownership, checks repeated `FinishAsync` shares one task, and exercises a real locked staging file so failed cleanup retains evidence and reports an error. Selected source backups and unrelated directories must remain unchanged.

## Evidence

- Targeted diff/whitespace review: PASS.
- Manual review of cancellation, helper acknowledgement and owned-path ordering: completed.
- Windows C# compilation: PASS.
- Targeted native cancellation/cleanup/locked-file and helper-handoff fixtures: PASS.
- Full Windows CI: FAIL later in `UiControls -> ServiceCommandDialogs`, at `RestoreBackup` dialog dispatch; subsequent suites were not executed.
- Isolated VPN server CI: PASS.
- User PC/VPS, installed application and live restoration: NOT_CHECKED, deliberately deferred until the complete test installer.

No F01–F31 acceptance status is closed by this microtask. The reviewed code checkpoint is `7440ec5eda97bf37708583af079be3694ea0039c` in [draft PR #88](https://github.com/rkhnorkhan-bit/ProGo/pull/88). Verification: [CI run 38146890018](https://github.com/rkhnorkhan-bit/ProGo/actions/runs/38146890018), Windows job `114492030367`, server job `114492030575`. The restore microtask passed its native fixtures; this is not a passing full candidate. The next separate microtask must investigate asynchronous `RestoreBackup` command/dialog dispatch and reveal the remaining suite results. Follow-up draft work is saved separately and is outside this checkpoint.
