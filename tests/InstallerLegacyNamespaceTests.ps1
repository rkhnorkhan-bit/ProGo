# Read-only helpers; every deliberate namespace change below is in our parent's own synthetic fixture.
$script:LegacyNamespaceChecks = 0
function Check-LegacyNamespace($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Legacy namespace observation failed: check=' + $Name) }
    $script:LegacyNamespaceChecks++
    Write-Host ('PASS: installer Legacy namespace ' + $Name)
}
function Test-ProGoLegacyNamespaceFixture([string]$Scenario,[string]$Journal,[string]$Install,
    [string]$Programs,[string]$Startup,[string]$Snapshot) {
    $physical = $null; $original = $null; $cancel = $null
    $token = [Threading.CancellationToken]::None
    try {
        $state = Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup
        if ($Scenario -cne 'complete') { Check-LegacyNamespace ($state -ceq 'blocked') ('refuse-' + $Scenario); return }
        Check-LegacyNamespace ($state -ceq 'observed-only') 'valid-observation-never-operation-authority'
        $reference = Restore-ProGoInstallerLegacyReferenceEvidence $Journal $Install $Programs $Startup
        Check-LegacyNamespace ($null -ne $reference) 'independent-reference'
        $physical = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup
        $original = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup
        Check-LegacyNamespace ($null -ne $physical -and $null -ne $original -and $original.IsPinned) 'both-native-leases-held'
        Check-LegacyNamespace (Test-ProGoInstallerLegacyNamespace $reference $Journal $Install) 'namespace-before-appearance'
        $appeared = Join-Path $Snapshot 'VERSION'
        if ([IO.File]::Exists($appeared) -or [IO.Directory]::Exists($appeared)) { throw 'Synthetic absent fixture leaf is not absent.' }
        [IO.File]::WriteAllText($appeared,'synthetic external appearance')
        Check-LegacyNamespace ([IO.File]::Exists($appeared)) 'directory-pins-do-not-reserve-absent-child'
        Check-LegacyNamespace (-not (Test-ProGoInstallerLegacyNamespace $reference $Journal $Install)) 'appeared-absent-leaf-refused'
        Check-LegacyNamespace ($physical.VerifyPinned($token)) 'unchanged-pinned-material-does-not-prove-namespace'
        $original.Dispose(); $original = $null; $physical.Dispose(); $physical = $null
        Check-LegacyNamespace ((Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup) -ceq 'blocked') 'actual-boundary-refuses-appearance'
        Check-LegacyNamespace ([IO.File]::ReadAllText($appeared) -ceq 'synthetic external appearance') 'foreign-appearance-not-deleted-or-repaired'
        $case = [IO.Path]::GetDirectoryName($Install)
        [IO.File]::Move($appeared,(Join-Path $case 'retained-appeared-VERSION'))
        Check-LegacyNamespace ((Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup) -ceq 'observed-only') 'current-absence-observation-is-not-ABA-history-or-deletion-proof'
        $extra = Join-Path $Journal 'unexpected.json'; [IO.File]::WriteAllText($extra,'synthetic foreign journal record')
        Check-LegacyNamespace ((Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup) -ceq 'blocked') 'unknown-journal-record-refused'
        Check-LegacyNamespace ([IO.File]::ReadAllText($extra) -ceq 'synthetic foreign journal record') 'unknown-record-preserved'
        [IO.File]::Move($extra,(Join-Path $case 'retained-unknown-record'))
        $extraDirectory = Join-Path $Snapshot 'unexpected-directory'; [IO.Directory]::CreateDirectory($extraDirectory) | Out-Null
        Check-LegacyNamespace ((Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup) -ceq 'blocked') 'unknown-snapshot-directory-refused-without-descent'
        Check-LegacyNamespace ([IO.Directory]::Exists($extraDirectory)) 'unknown-directory-preserved'
        [IO.Directory]::Move($extraDirectory,(Join-Path $case 'retained-unknown-directory'))
        $manifest = Join-Path $Snapshot 'installer-snapshot.json'; $retained = Join-Path $case 'retained-manifest'
        [byte[]]$manifestBytes = [IO.File]::ReadAllBytes($manifest); [IO.File]::Move($manifest,$retained)
        try {
            Check-LegacyNamespace ((Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup) -ceq 'blocked') 'missing-required-object-refused'
            Check-LegacyNamespace ([Convert]::ToBase64String([IO.File]::ReadAllBytes($retained)) -ceq [Convert]::ToBase64String($manifestBytes)) 'missing-object-is-not-restored-by-observer'
        } finally { [IO.File]::Move($retained,$manifest) }
        $cancel = New-Object Threading.CancellationTokenSource; $cancel.Cancel(); $cancelled = $false
        try { Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup $cancel.Token | Out-Null }
        catch { $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }; if ($cause -isnot [OperationCanceledException]) { throw }; $cancelled = $true }
        Check-LegacyNamespace $cancelled 'cancel-propagates-not-observed-success'
        $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        if ((Get-ProGoInstallerLegacyBoundaryObservation $Journal $Install $Programs $Startup) -cne 'observed-only') { throw 'Restored synthetic context is not observable.' }
        $unchanged = @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count
        foreach ($path in $inventory.Keys) { $unchanged = $unchanged -and [IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $inventory[$path] }
        Check-LegacyNamespace $unchanged 'read-only-helper-preserves-all-private-and-foreign-bytes'
        foreach ($path in $inventory.Keys) { $probe = [IO.File]::Open($path,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Check-LegacyNamespace $true 'both-leases-released-on-every-return'
    } finally {
        try { if ($null -ne $original) { $original.Dispose() } }
        finally { if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $cancel) { $cancel.Dispose() } }
    }
}
