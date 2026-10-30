# Uses only the parent's own isolated synthetic fixture. No live installation or deletion.
$script:LegacyPhysicalChecks = 0
function Check-LegacyPhysical($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Legacy physical pins failed: check=' + $Name) }
    $script:LegacyPhysicalChecks++
    Write-Host ('PASS: installer Legacy physical pins ' + $Name)
}
function Test-ProGoLegacyPhysicalPinsFixture([string]$Scenario,[string]$Journal,[string]$Install,
    [string]$Programs,[string]$Startup,[string]$Snapshot) {
    $pins = $null; $original = $null; $busy = $null; $cancel = $null
    $token = [Threading.CancellationToken]::None
    try {
        $pins = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup
        if ($Scenario -cne 'complete') { Check-LegacyPhysical ($null -eq $pins) ('refuse-' + $Scenario); return }
        Check-LegacyPhysical ($null -ne $pins) 'verified-acquisition'
        Check-LegacyPhysical (-not $pins.MutationAllowed -and -not $pins.FullRollback -and -not $pins.InstallSucceeded) 'no-mutation-or-outcome-authority'
        Check-LegacyPhysical ($pins.VerifyPinned($token)) 'native-origins-and-hashes'
        $original = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup
        Check-LegacyPhysical ($null -ne $original -and $original.IsPinned -and $pins.VerifyPinned($token)) 'coexists-with-exact-original-pins'
        $original.Dispose(); $original = $null
        $verifyCancel = New-Object Threading.CancellationTokenSource
        try {
            $verifyCancel.Cancel(); $verifyCancelled = $false
            try { [void]$pins.VerifyPinned($verifyCancel.Token) }
            catch { $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }; if ($cause -isnot [OperationCanceledException]) { throw }; $verifyCancelled = $true }
            Check-LegacyPhysical ($verifyCancelled -and $pins.VerifyPinned($token)) 'cancelled-verification-retains-owned-pins-until-dispose'
        } finally { $verifyCancel.Dispose() }
        $targets = @((Join-Path $Install 'ProGo.exe'),(Join-Path $Snapshot 'ProGo.exe'),
            (Join-Path $Snapshot 'installer-snapshot.json'),(Join-Path $Journal 'admit-000000.json'))
        $index = 0
        foreach ($target in $targets) {
            $index++; $denied = $false; $probe = $null
            try { $probe = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::ReadWrite) }
            catch [IO.IOException] { $denied = $true } finally { if ($null -ne $probe) { $probe.Dispose() } }
            Check-LegacyPhysical $denied ('write-refused-' + $index)
            $denied = $false
            try { [IO.File]::Move($target,($target + '.physical-probe')) } catch [IO.IOException] { $denied = $true }
            Check-LegacyPhysical ($denied -and [IO.File]::Exists($target) -and -not [IO.File]::Exists($target + '.physical-probe')) ('replacement-refused-' + $index)
        }
        foreach ($directory in @($Snapshot,$Journal,$Install)) {
            $denied = $false
            try { [IO.Directory]::Move($directory,($directory + '-physical-probe')) } catch [IO.IOException] { $denied = $true }
            Check-LegacyPhysical ($denied -and [IO.Directory]::Exists($directory)) 'required-parent-rename-refused'
        }
        $pins.Dispose(); $pins.Dispose()
        Check-LegacyPhysical (-not $pins.VerifyPinned($token)) 'disposed-lease-refuses'
        foreach ($target in $targets) { $probe = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Check-LegacyPhysical $true 'all-owned-handles-released'
        $pins = $null
        foreach ($target in $targets) {
            [byte[]]$bytes = [IO.File]::ReadAllBytes($target)
            try {
                [IO.File]::WriteAllText($target,'synthetic partial mutation')
                $pins = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup
                Check-LegacyPhysical ($null -eq $pins) 'partial-tamper-refused-before-pins-return'
            } finally { if ($null -ne $pins) { $pins.Dispose(); $pins = $null }; [IO.File]::WriteAllBytes($target,$bytes) }
        }
        $busy = [IO.File]::Open($targets[0],[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
        $pins = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup
        Check-LegacyPhysical ($null -eq $pins) 'busy-context-fails-closed'
        $busy.Dispose(); $busy = $null
        $manager = New-Object ProGo.ApplicationShortcuts($Install,$Programs,$Startup)
        foreach ($plan in @(@('ProGo.exe','missing-synthetic-leaf'),@('../foreign.txt'),@('ProGo.exe','ProGo.exe'))) {
            $pins = $manager.PinLegacyPhysicalMaterial([string[]]$plan,$token)
            Check-LegacyPhysical ($null -eq $pins) 'native-invalid-plan-or-late-open-refuses'
            $probe = [IO.File]::Open($targets[0],[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose()
        }
        $cancel = New-Object Threading.CancellationTokenSource; $cancel.Cancel(); $cancelled = $false
        try { $pins = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup $cancel.Token }
        catch { $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }; if ($cause -isnot [OperationCanceledException]) { throw }; $cancelled = $true }
        Check-LegacyPhysical ($cancelled -and $null -eq $pins) 'cancel-before-pinning'
        $pins = Open-ProGoInstallerLegacyPhysicalPins $Journal $Install $Programs $Startup
        Check-LegacyPhysical ($null -ne $pins -and $pins.VerifyPinned($token)) 'fresh-verified-reacquisition-after-failed-attempts'
    } finally {
        if ($null -ne $original) { $original.Dispose() }
        if ($null -ne $pins) { $pins.Dispose() }
        if ($null -ne $busy) { $busy.Dispose() }
        if ($null -ne $cancel) { $cancel.Dispose() }
    }
}
