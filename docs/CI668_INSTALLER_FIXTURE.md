# CI668: installer maintenance fault-injection scope

CI668 source `1bfc5a35ecad56605d385c02ff9cf85f39aa2532`, tested merge
`636007e04a3f86d35afeab70245fb8fa79394ee3`, run40106066218 / Windows120907893622.
Full final Windows log independently read (10073 lines). Windows FAILURE; Ubuntu SUCCESS.
Failure: InstallerMaintenanceTests.ps1:23, copy-fail propagates failure.
Maintenance complete/busy/stop-fail PASS (3/15); remaining12 NOT_RUN. Rollback8,
Stage8/Backup8/SelfCheck9, relay/Home VPN/Portable and later suites NOT_RUN.
Payload9/Desktop6388/Instance30/Maintenance126/Shutdown47 and earlier logged suites PASS.
No whole installer or new rollback PASS inferred.

## Proven cause and bounded correction

PowerShell variable names are case-insensitive and nested functions resolve dynamic scope.
The production Set-ProGoInstallerRuntimeFile assigns local `$mode` to Open/CreateNew.
The test Write-ProGoInstallerRuntimeBytes mock reads `$Mode`, so it sees FileMode instead
of Run-InstallerOwnerCase's scenario. Copies>0 passes, but copy-fail never throws; the
outer caught flag stays false. Production rethrow is not shown broken by this failure.
The same selector in the same mock also suppresses the not-yet-reached partial-copy case.
Both selectors now read `$FixtureState.Mode`. Runtime is unchanged.

The copy-fail branch additionally proves real readable/writable FileStreams and exactly
one owned admission, then increments one fault counter and throws the original error.
Existing catch checks its exact message; original failure/success/Launch/cleanup assertions
remain. Added assertion requires exactly one write/fault, no self-check, verified original
absence after rollback and no executable left behind. Partial-copy retains its real
truncation/write and original byte-verification assertion. No assertion, timeout, lifecycle,
security/network setting or production guard is relaxed.

New Maintenance15/Rollback8 and regression execution: Windows NOT_RUN locally; exact new
CI final logs required. No old PASS inherited. Journal/retention remain separate mandatory
slices. CI662 root UNKNOWN; diagnostic retained. Private candidate BLOCKED_TRANSFER; no
transfer retry. Fixed readiness implementation58–80/100, verified22/100, change0.
