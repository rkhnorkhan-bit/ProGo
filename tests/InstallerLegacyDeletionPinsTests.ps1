# Nested isolated-fixture checks only. No live installer call and no deletion operation.
$script:LegacyPinChecks = 0
function Check-LegacyPin($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Legacy pin gate failed: check=' + $Name) }
    $script:LegacyPinChecks++
    Write-Host ('PASS: installer Legacy pin gate ' + $Name)
}
function Test-ProGoLegacyDeletionPinsFixture([string]$Scenario,[string]$Journal,[string]$Install,
    [string]$Programs,[string]$Startup,[string[]]$Targets) {
    $pins = $null; $again = $null; $busy = $null; $cancel = $null
    try {
        $pins = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup
        if ($Scenario -cne 'complete') {
            Check-LegacyPin ($null -eq $pins) ('refuse-' + $Scenario)
            return
        }
        Check-LegacyPin ($null -ne $pins -and $pins.IsPinned) 'verified-acquisition'
        Check-LegacyPin (-not $pins.MutationAllowed -and -not $pins.FullRollback -and -not $pins.InstallSucceeded) 'no-mutation-or-result-authority'
        $index = 0
        foreach ($target in $Targets) {
            $index++
            $denied = $false; $probe = $null
            try { $probe = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::ReadWrite) }
            catch [IO.IOException] { $denied = $true }
            finally { if ($null -ne $probe) { $probe.Dispose() } }
            Check-LegacyPin $denied ('write-refused-' + $index)
            $denied = $false
            try { [IO.File]::Move($target,($target + '.pin-probe')) } catch [IO.IOException] { $denied = $true }
            Check-LegacyPin ($denied -and [IO.File]::Exists($target) -and -not [IO.File]::Exists($target + '.pin-probe')) ('replacement-refused-' + $index)
        }
        $pins.Dispose(); $pins.Dispose()
        Check-LegacyPin (-not $pins.IsPinned) 'idempotent-dispose'
        foreach ($target in $Targets) { $probe = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Check-LegacyPin $true 'all-owned-resources-released'
        $again = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup
        Check-LegacyPin ($null -ne $again -and $again.IsPinned) 'read-only-reacquisition-is-not-replay'
        $again.Dispose(); $again = $null
        # An external read handle that does not share DELETE blocks final exact-handle acquisition.
        $busy = [IO.File]::Open($Targets[0],[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $again = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup
        Check-LegacyPin ($null -eq $again) 'busy-original-refuses-and-unwinds'
        $busy.Dispose(); $busy = $null
        foreach ($target in $Targets) { $probe = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        $cancel = New-Object Threading.CancellationTokenSource; $cancel.Cancel(); $cancelled = $false
        try { $again = Open-ProGoInstallerLegacyDeletionPins $Journal $Install $Programs $Startup $cancel.Token }
        catch { $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }; if ($cause -isnot [OperationCanceledException]) { throw }; $cancelled = $true }
        Check-LegacyPin ($cancelled -and $null -eq $again) 'cancel-before-acquisition'
    } finally {
        if ($null -ne $again) { $again.Dispose() }
        if ($null -ne $pins) { $pins.Dispose() }
        if ($null -ne $busy) { $busy.Dispose() }
        if ($null -ne $cancel) { $cancel.Dispose() }
    }
}
