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
- C# compilation, new native fixtures, full Windows suite: NOT_CHECKED.
- User PC/VPS, installed application and live restoration: NOT_CHECKED, deliberately deferred until the complete test installer.

No F01–F31 acceptance status is closed by this microtask. Native verification requires sending this concrete branch to GitHub CI; that publication is currently blocked by automatic approval review, pending explicit user authorization. No replacement upload or other bypass is used.
