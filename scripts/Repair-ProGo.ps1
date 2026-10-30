param(
    [switch]$NoLaunch,
    [string]$PayloadDirectory,
    [switch]$Recover
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot 'Layout-ProGo.ps1')
$layout = Resolve-ProGoRecoveryLayout
$DataRoot = $layout.DataRoot
$InstallDir = $layout.RuntimeRoot
$ScriptsDir = Join-Path $InstallDir "scripts"
$BackupsDir = Join-Path $DataRoot "backups"
$Root = Split-Path -Parent $PSScriptRoot
$ReleaseDir = if ([string]::IsNullOrWhiteSpace($PayloadDirectory)) { Join-Path $Root "release" } else { [IO.Path]::GetFullPath($PayloadDirectory) }
$journalPath = Join-Path $InstallDir '.repair-state.json'

# A complete prepared payload is required before touching the installation.
# Presence/length is only preflight, not signature or whole-repair acceptance.
$RequiredRuntimePaths = @(
    'ProGo.exe',
    'VERSION',
    'ProGo.ico',
    'scripts\Install-ProGo.ps1',
    'scripts\Uninstall-ProGo.ps1',
    'scripts\Update-ProGo.ps1',
    'scripts\Update-ProGo.Core.ps1',
    'scripts\Restore-ProGoBackup.ps1',
    'scripts\Show-ProGo.ps1',
    'scripts\Start-ProGo.ps1',
    'scripts\Repair-ProGo.ps1',
    'scripts\Enable-HomeVpnFirewall.ps1',
    'scripts\Maintenance-ProGo.ps1',
    'scripts\BackupRetention-ProGo.ps1',
    'scripts\BackupIntegrity-ProGo.ps1',
    'scripts\Log-ProGo.ps1',
    'scripts\Diagnostics-ProGo.ps1',
    'scripts\Measure-RescueProGo.ps1',
    'scripts\Shortcuts-ProGo.ps1',
    'scripts\Layout-ProGo.ps1',
    'scripts\Firewall-ProGo.ps1',
    'scripts\ApplicationShortcuts.cs',
    'scripts\MaintenanceOperation.cs',
    'scripts\InstalledUpdateTransport.cs',
    'scripts\UpdateInstallSession.cs',
    'scripts\BackupRetention.cs',
    'scripts\BackupIntegrity.cs',
    'scripts\BoundedLog.cs',
    'scripts\RescueMeasurement.cs',
    'scripts\DiagnosticReport.cs',
    'scripts\DiagnosticPreview.cs',
    'scripts\UiTheme.cs',
    'scripts\BrandIcon.cs',
    'scripts\home-vpn\HOME_IKEV2.md',
    'scripts\home-vpn\server\ikev2_relay.py',
    'scripts\home-vpn\server\install-ikev2-relay.sh',
    'scripts\home-vpn\server\make_home_profile.py',
    'scripts\home-vpn\server\home_vpn_setup.py',
    'scripts\home-vpn\server\profile_share_setup.py',
    'scripts\home-vpn\server\profile_share.py',
    'scripts\home-vpn\server\qrcodegen.py',
    'scripts\home-vpn\server\QR_LICENSE.txt'
)
$payloadHashes = @{}
$createdDirectories = New-Object 'System.Collections.Generic.List[string]'
$temporaryFiles = New-Object 'System.Collections.Generic.List[string]'
$applied = New-Object 'System.Collections.Generic.List[string]'
$before = @{}
$lease = $null; $leaseCreated = $false; $maintenance = $null
$committed = $false; $rolledBack = $false
$journalCreated = $false; $journalSettled = $false; $selfCheckPassed = $false
$transactionId = [guid]::NewGuid().ToString('N')
$transaction = Join-Path $InstallDir ('.repair-stage-' + $transactionId)
$lockPath = Join-Path $InstallDir '.repair.lock'
function Ensure-RepairDirectory([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Container) { return }
    if (Test-Path -LiteralPath $Path) { throw 'Repair directory is occupied by a file.' }
    $parent = Split-Path -Parent $Path
    if ($parent -and -not (Test-Path -LiteralPath $parent -PathType Container)) { Ensure-RepairDirectory $parent }
    [IO.Directory]::CreateDirectory($Path) | Out-Null
    $createdDirectories.Add($Path)
}
function Assert-RepairTarget([string]$Relative) {
    $path = $InstallDir
    foreach ($part in @('') + @($Relative -split '[\\/]')) {
        if ($part) { $path = Join-Path $path $part }
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw ('Repair cannot own a redirected runtime path: ' + $Relative)
        }
    }
}
function Get-RepairHash([string]$Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
function Set-RepairFile([string]$Source, [string]$Destination, [string]$Hash) {
    Ensure-RepairDirectory (Split-Path -Parent $Destination)
    # The stage is on the same volume as the target. A killed writer leaves
    # its temporary file inside the journal-owned directory, never beside data.
    $temporary = Join-Path $transaction ('write-' + [guid]::NewGuid().ToString('N') + '.tmp')
    $temporaryFiles.Add($temporary)
    [IO.File]::Copy($Source, $temporary, $false)
    if ((Get-RepairHash $temporary) -cne $Hash) { throw 'Repair staged file changed before replacement.' }
    if ([IO.File]::Exists($Destination)) { [IO.File]::Replace($temporary, $Destination, [System.Management.Automation.Language.NullString]::Value) }
    else { [IO.File]::Move($temporary, $Destination) }
}
function Write-RepairJournal([string]$Phase) {
    if ([IO.File]::Exists($journalPath)) {
        $current = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json
        if (-not $journalCreated -or $current.transaction_id -cne $transactionId) { throw 'Repair journal ownership changed.' }
    }
    $entries = @()
    foreach ($relative in $RequiredRuntimePaths) {
        if ($before.ContainsKey($relative)) { $entries += @{
            path = $relative; before_exists = [bool]$before[$relative].Exists;
            before_hash = $before[$relative].Hash; new_hash = $payloadHashes[$relative]
        } }
    }
    $record = @{ schema = 1; transaction_id = $transactionId; phase = $Phase;
        entries = @($entries); applied = @($applied.ToArray()); self_check_passed = $selfCheckPassed; lock_was_new = $leaseCreated }
    $bytes = (New-Object Text.UTF8Encoding($false)).GetBytes(($record | ConvertTo-Json -Depth 6 -Compress))
    $temporary = Join-Path $transaction ('journal-' + [guid]::NewGuid().ToString('N') + '.new')
    $temporaryFiles.Add($temporary)
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    if ([IO.File]::Exists($journalPath)) { [IO.File]::Replace($temporary, $journalPath, [System.Management.Automation.Language.NullString]::Value) }
    else { [IO.File]::Move($temporary, $journalPath) }
    $script:journalCreated = $true
}
function Read-RepairRecoveryJournal {
    Assert-RepairTarget '.repair-state.json'
    if (-not [IO.File]::Exists($journalPath) -or (Get-Item -LiteralPath $journalPath).Length -gt 65536) { throw 'Repair recovery journal is missing or too large.' }
    $bytes = [IO.File]::ReadAllBytes($journalPath)
    if ($bytes.Length -gt 65536) { throw 'Repair recovery journal is missing or too large.' }
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { $script:recoveryJournalHash = [BitConverter]::ToString($algorithm.ComputeHash($bytes)).Replace('-', '') }
    finally { $algorithm.Dispose() }
    $record = (New-Object Text.UTF8Encoding($false, $true)).GetString($bytes) | ConvertFrom-Json
    if ($null -eq $record -or $record -isnot [PSCustomObject]) { throw 'Invalid repair recovery journal object.' }
    $fields = @('schema', 'transaction_id', 'phase', 'entries', 'applied', 'self_check_passed', 'lock_was_new')
    if (@($record.PSObject.Properties).Count -ne $fields.Count) { throw 'Invalid repair recovery journal fields.' }
    foreach ($field in $fields) { if ($null -eq $record.PSObject.Properties[$field]) { throw 'Missing repair recovery journal field.' } }
    if ($record.schema -isnot [int] -or $record.schema -ne 1 -or $record.transaction_id -isnot [string] -or $record.transaction_id -cnotmatch '^[a-f0-9]{32}$' -or
        $record.self_check_passed -isnot [bool] -or $record.lock_was_new -isnot [bool] -or
        @('preparing', 'prepared', 'applying', 'committed', 'rolled-back') -cnotcontains $record.phase -or
        $record.entries -isnot [Array] -or $record.applied -isnot [Array]) { throw 'Invalid repair recovery journal schema or phase.' }
    $entryPaths = @{}
    foreach ($entry in $record.entries) {
        if (@($entry.PSObject.Properties).Count -ne 4) { throw 'Invalid repair recovery entry fields.' }
        foreach ($field in @('path', 'before_exists', 'before_hash', 'new_hash')) { if ($null -eq $entry.PSObject.Properties[$field]) { throw 'Missing repair recovery entry field.' } }
        if ($RequiredRuntimePaths -cnotcontains $entry.path -or $entryPaths.ContainsKey($entry.path) -or $entry.before_exists -isnot [bool] -or
            $entry.new_hash -isnot [string] -or $entry.new_hash -cnotmatch '^[A-F0-9]{64}$' -or
            ($entry.before_exists -and ($entry.before_hash -isnot [string] -or $entry.before_hash -cnotmatch '^[A-F0-9]{64}$')) -or
            (-not $entry.before_exists -and $null -ne $entry.before_hash)) { throw 'Invalid repair recovery runtime entry.' }
        $entryPaths[$entry.path] = $true
    }
    if (($record.phase -ceq 'preparing' -and ($record.entries.Count -ne 0 -or $record.applied.Count -ne 0)) -or
        (@('prepared', 'applying', 'committed') -ccontains $record.phase -and $record.entries.Count -ne $RequiredRuntimePaths.Count) -or
        ($record.phase -ceq 'rolled-back' -and $record.applied.Count -gt 0 -and $record.entries.Count -ne $RequiredRuntimePaths.Count) -or
        ($record.phase -ceq 'prepared' -and $record.applied.Count -ne 0) -or
        ($record.phase -ceq 'applying' -and $record.applied.Count -eq 0) -or
        ($record.phase -ceq 'committed' -and ($record.applied.Count -ne $RequiredRuntimePaths.Count -or -not $record.self_check_passed)) -or
        ($record.self_check_passed -and @('committed', 'rolled-back') -cnotcontains $record.phase) -or
        $record.applied.Count -gt $RequiredRuntimePaths.Count) { throw 'Inconsistent repair recovery phase.' }
    for ($index = 0; $index -lt $record.applied.Count; $index++) {
        if ($record.applied[$index] -cne $RequiredRuntimePaths[$index]) { throw 'Repair recovery admission is not a known runtime prefix.' }
    }
    return $record
}
function Assert-RepairSnapshotTree([string]$Path) {
    # Inspect before descending: never traverse a junction while validating it.
    foreach ($item in @(Get-ChildItem -LiteralPath $Path -Force)) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Repair recovery snapshot contains a redirected path.' }
        if ($item.PSIsContainer) { Assert-RepairSnapshotTree $item.FullName }
    }
}
if ($Recover) {
    # Explicit recovery never repeats repair, self-check or launch. Validation
    # precedes writes; every changed byte must still belong to this transaction.
    $record = Read-RepairRecoveryJournal
    $journalHash = $recoveryJournalHash
    $transactionId = $record.transaction_id
    $transaction = Join-Path $InstallDir ('.repair-stage-' + $transactionId)
    Assert-RepairTarget ('.repair-stage-' + $transactionId)
    Assert-RepairTarget '.repair.lock'
    if ((Test-Path -LiteralPath $transaction) -and -not [IO.Directory]::Exists($transaction)) { throw 'Repair recovery snapshot path is occupied by a file.' }
    if ([IO.Directory]::Exists($transaction)) {
        Assert-RepairSnapshotTree $transaction
    } elseif ($record.phase -ceq 'applying') { throw 'Repair recovery snapshot is missing; installation was not changed.' }
    foreach ($entry in $record.entries) {
        $before[$entry.path] = @{ Exists = $entry.before_exists; Hash = $entry.before_hash }
        $payloadHashes[$entry.path] = $entry.new_hash
    }
    foreach ($relative in $record.applied) { $applied.Add($relative) }
    $journalCreated = $true; $leaseCreated = $record.lock_was_new
    $recoveryLockCreated = $false; $recoverySettled = $false
    try {
        $recoveryLockCreated = -not (Test-Path -LiteralPath $lockPath)
        $lease = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        if ((Get-RepairHash $journalPath) -cne $journalHash) { throw 'Repair recovery journal changed before ownership was acquired.' }
        # Check every declared target before the first restoration. Unrelated
        # settings/keys/backups are neither journal entries nor deletion targets.
        foreach ($entry in $record.entries) {
            Assert-RepairTarget $entry.path
            $target = Join-Path $InstallDir $entry.path
            $exists = Test-Path -LiteralPath $target
            if ($exists -and -not [IO.File]::Exists($target)) { throw 'Repair recovery runtime leaf is not a file.' }
            $hash = if ($exists) { Get-RepairHash $target } else { $null }
            $original = ($entry.before_exists -and $exists -and $hash -ceq $entry.before_hash) -or (-not $entry.before_exists -and -not $exists)
            $candidate = $exists -and $hash -ceq $entry.new_hash
            if ($record.phase -ceq 'committed') {
                if (-not $candidate) { throw 'Committed repair runtime changed; recovery records retained.' }
            } elseif (-not $original -and -not ($record.phase -ceq 'applying' -and $record.applied -ccontains $entry.path -and $candidate)) {
                throw 'Repair recovery found a foreign runtime change; snapshots retained.'
            }
            if ($record.phase -ceq 'applying' -and $record.applied -ccontains $entry.path -and $entry.before_exists) {
                $backup = Join-Path (Join-Path $transaction 'old') $entry.path
                if (-not [IO.File]::Exists($backup) -or (Get-RepairHash $backup) -cne $entry.before_hash) { throw 'Repair recovery original snapshot is missing or changed.' }
            }
        }
        if ($record.phase -ceq 'applying') {
            $helperRelative = 'scripts\MaintenanceOperation.cs'
            $helper = Join-Path (Join-Path $transaction 'new') $helperRelative
            if (-not [IO.File]::Exists($helper) -or (Get-RepairHash $helper) -cne $payloadHashes[$helperRelative]) { throw 'Repair recovery maintenance snapshot is missing or changed.' }
            if (-not ('ProGo.MaintenanceOperation' -as [type])) { Add-Type -TypeDefinition ([IO.File]::ReadAllText($helper)) }
            $maintenance = [ProGo.MaintenanceOperation]::Enter()
            [ProGo.MaintenanceOperation]::RequireApplicationStopped($InstallDir)
            [ProGo.MaintenanceOperation]::RequireProxyCleanupCompleted($DataRoot)
            for ($index = $applied.Count - 1; $index -ge 0; $index--) {
                $relative = $applied[$index]; Assert-RepairTarget $relative
                $target = Join-Path $InstallDir $relative
                if ($before[$relative].Exists) {
                    $hash = Get-RepairHash $target
                    if ($hash -ceq $before[$relative].Hash) { continue }
                    if ($hash -cne $payloadHashes[$relative]) { throw 'Runtime ownership changed during repair recovery.' }
                    Set-RepairFile (Join-Path (Join-Path $transaction 'old') $relative) $target $before[$relative].Hash
                    if ((Get-RepairHash $target) -cne $before[$relative].Hash) { throw 'Repair recovery restore verification failed.' }
                } elseif (Test-Path -LiteralPath $target) {
                    if (-not [IO.File]::Exists($target) -or (Get-RepairHash $target) -cne $payloadHashes[$relative]) { throw 'New runtime ownership changed during recovery.' }
                    [IO.File]::Delete($target)
                }
            }
            Write-RepairJournal 'rolled-back'
        }
        # A committed record only needs cleanup: never roll back an accepted
        # image or rerun its self-check. A rolled-back record is also idempotent.
        if ([IO.Directory]::Exists($transaction)) { Assert-RepairSnapshotTree $transaction; Remove-Item -LiteralPath $transaction -Recurse -Force }
        $current = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json
        if ($current.transaction_id -cne $transactionId) { throw 'Repair recovery journal changed before cleanup.' }
        [IO.File]::Delete($journalPath)
        $recoverySettled = $true
        Write-Host ('Repair recovery complete: ' + $record.phase + '; no application launched')
    } finally {
        try {
            foreach ($temporary in $temporaryFiles) { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
        } finally {
            try { if ($null -ne $lease) { $lease.Dispose() } }
            finally {
                if ($null -ne $maintenance) { $maintenance.Dispose() }
                if (($recoveryLockCreated -or ($recoverySettled -and $leaseCreated)) -and [IO.File]::Exists($lockPath)) { try { [IO.File]::Delete($lockPath) } catch { } }
            }
        }
    }
    return
}
if (Test-Path -LiteralPath $journalPath) { throw 'An unfinished repair journal exists. Recover it with Repair-ProGo.ps1 -Recover before starting another repair; installation was not changed.' }
$missing = @()
foreach ($relative in $RequiredRuntimePaths) {
    $source = Join-Path $ReleaseDir $relative
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { $missing += $relative }
    elseif ((Get-Item -LiteralPath $source).Length -eq 0) { $missing += $relative }
}
if ($missing.Count -gt 0) { throw ('Repair payload incomplete or empty: ' + ($missing -join ', ')) }
# Freeze all expected bytes before any installation write.
foreach ($relative in $RequiredRuntimePaths) { $payloadHashes[$relative] = Get-RepairHash (Join-Path $ReleaseDir $relative) }
$repairPhase = 'maintenance-load'; $selfCheckExit = -1
try {
    if (-not ('ProGo.MaintenanceOperation' -as [type])) {
        Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $ReleaseDir 'scripts\MaintenanceOperation.cs')))
    }
    if ($null -eq [ProGo.MaintenanceOperation].GetMethod('RunOwnedSelfCheck')) { throw 'Repair requires a fresh PowerShell process with its prepared maintenance helper.' }
    $repairPhase = 'maintenance-gate'
    $maintenance = [ProGo.MaintenanceOperation]::Enter()
    [ProGo.MaintenanceOperation]::RequireApplicationStopped($InstallDir)
    [ProGo.MaintenanceOperation]::RequireProxyCleanupCompleted($DataRoot)
    Assert-RepairTarget 'ProGo.exe'
    Ensure-RepairDirectory $InstallDir
    if ((Test-Path -LiteralPath $lockPath) -and ((Get-Item -LiteralPath $lockPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Repair ownership file is redirected.' }
    $leaseCreated = -not (Test-Path -LiteralPath $lockPath)
    $lease = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    Ensure-RepairDirectory $transaction
    $repairPhase = 'snapshot'
    Write-RepairJournal 'preparing'
    foreach ($relative in $RequiredRuntimePaths) {
        Assert-RepairTarget $relative
        $target = Join-Path $InstallDir $relative
        if ((Test-Path -LiteralPath $target) -and -not (Test-Path -LiteralPath $target -PathType Leaf)) { throw ('Runtime leaf is a directory: ' + $relative) }
        $exists = [IO.File]::Exists($target)
        $hash = if ($exists) { Get-RepairHash $target } else { $null }
        $before[$relative] = @{ Exists = $exists; Hash = $hash }
        if ($exists) {
            $backup = Join-Path (Join-Path $transaction 'old') $relative
            Ensure-RepairDirectory (Split-Path -Parent $backup)
            [IO.File]::Copy($target, $backup, $false)
            if ((Get-RepairHash $backup) -cne $hash) { throw ('Runtime changed while taking repair snapshot: ' + $relative) }
        }
        $staged = Join-Path (Join-Path $transaction 'new') $relative
        Ensure-RepairDirectory (Split-Path -Parent $staged)
        [IO.File]::Copy((Join-Path $ReleaseDir $relative), $staged, $false)
        if ((Get-RepairHash $staged) -cne $payloadHashes[$relative]) { throw ('Repair payload changed while staging: ' + $relative) }
    }
    Write-RepairJournal 'prepared'
    $repairPhase = 'runtime-copy'
    foreach ($relative in $RequiredRuntimePaths) {
        Assert-RepairTarget $relative
        $target = Join-Path $InstallDir $relative
        if ($before[$relative].Exists) {
            if (-not [IO.File]::Exists($target) -or (Get-RepairHash $target) -cne $before[$relative].Hash) { throw ('Runtime changed before repair admission: ' + $relative) }
        } elseif (Test-Path -LiteralPath $target) { throw ('Runtime path appeared before repair admission: ' + $relative) }
        $applied.Add($relative) # Durable admission precedes the atomic OS call.
        Write-RepairJournal 'applying'
        Set-RepairFile (Join-Path (Join-Path $transaction 'new') $relative) $target $payloadHashes[$relative]
    }
    $repairPhase = 'runtime-verify'
    foreach ($relative in $RequiredRuntimePaths) {
        if ((Get-RepairHash (Join-Path $InstallDir $relative)) -cne $payloadHashes[$relative]) { throw ('Repair runtime verification failed: ' + $relative) }
    }
    # Exercise the actual installed image in an isolated local data root. A
    # settings migration during the smoke check must not rewrite personal data.
    $check = New-Object Diagnostics.ProcessStartInfo
    $check.FileName = Join-Path $InstallDir 'ProGo.exe'
    $check.Arguments = '--self-check'; $check.WorkingDirectory = $InstallDir
    $check.UseShellExecute = $false; $check.CreateNoWindow = $true
    $checkDataRoot = Join-Path $transaction 'self-check-data'
    $check.EnvironmentVariables['LOCALAPPDATA'] = $checkDataRoot
    $check.EnvironmentVariables['PROGO_SELF_CHECK_ROOT'] = $checkDataRoot
    $repairPhase = 'self-check'
    $selfCheckExit = [ProGo.MaintenanceOperation]::RunOwnedSelfCheck($check, 8000)
    if ($selfCheckExit -ne 0) { throw 'Installed repair self-check failed.' }
    if (-not [IO.Directory]::Exists($checkDataRoot)) { throw 'Installed repair self-check did not confirm its isolated data root.' }
    foreach ($relative in $RequiredRuntimePaths) {
        if ((Get-RepairHash (Join-Path $InstallDir $relative)) -cne $payloadHashes[$relative]) { throw ('Runtime changed during repair self-check: ' + $relative) }
    }
    Write-Host 'Repair self-check PASS: installed image, isolated local data root'
    $selfCheckPassed = $true
    $repairPhase = 'commit'
    Ensure-RepairDirectory $BackupsDir
    Write-RepairJournal 'committed'
    $committed = $true; $journalSettled = $true
} catch {
    # Never print exception messages: paths or personal data can appear there.
    # Stable phase, type and numeric codes retain actionable CI evidence.
    $failure = $_.Exception
    for ($depth = 0; $depth -lt 8 -and $null -ne $failure.InnerException; $depth++) { $failure = $failure.InnerException }
    $failureType = $failure.GetType().Name
    $failureCode = $failure.HResult
    $nativeCode = if ($failure -is [ComponentModel.Win32Exception]) { $failure.NativeErrorCode } else { 0 }
    # Console output bypasses PowerShell's width-dependent error formatting.
    # Emit before rollback too: a cleanup failure must not erase the first cause.
    [Console]::Error.WriteLine('PROGO_REPAIR_FAILURE Cause: ' + $failureType + '; stage=' + $repairPhase + '; script_line=' + $_.InvocationInfo.ScriptLineNumber + '; hresult=' + $failureCode + '; native=' + $nativeCode + '; check_exit=' + $selfCheckExit)
    $conflicts = @()
    for ($index = $applied.Count - 1; $index -ge 0; $index--) {
        $relative = $applied[$index]
        try {
            Assert-RepairTarget $relative
            $target = Join-Path $InstallDir $relative
            if ($before[$relative].Exists) {
                if (-not [IO.File]::Exists($target)) { throw 'An original runtime file disappeared.' }
                $current = Get-RepairHash $target
                if ($current -ceq $before[$relative].Hash) { continue }
                if ($current -cne $payloadHashes[$relative]) { throw 'Runtime ownership changed after repair.' }
                Set-RepairFile (Join-Path (Join-Path $transaction 'old') $relative) $target $before[$relative].Hash
                if ((Get-RepairHash $target) -cne $before[$relative].Hash) { throw 'Runtime rollback verification failed.' }
            } elseif (Test-Path -LiteralPath $target) {
                if (-not [IO.File]::Exists($target) -or (Get-RepairHash $target) -cne $payloadHashes[$relative]) { throw 'New runtime leaf no longer belongs to this repair.' }
                [IO.File]::Delete($target)
            }
        } catch { $conflicts += $relative }
    }
    $rolledBack = $conflicts.Count -eq 0
    if (-not $rolledBack) { throw ('Repair rollback incomplete; owned snapshot retained at ' + $transaction + '; paths: ' + ($conflicts -join ', ')) }
    if ($journalCreated) {
        try { Write-RepairJournal 'rolled-back'; $journalSettled = $true }
        catch { throw ('Original runtime restored but durable cleanup is still pending: ' + $transaction) }
    } else { $journalSettled = $true }
    throw ('Repair failed; original runtime restored. Cause: ' + $failureType + '; stage=' + $repairPhase + '; hresult=' + $failureCode + '; native=' + $nativeCode + '; check_exit=' + $selfCheckExit)
} finally {
    try {
        foreach ($temporary in $temporaryFiles) { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
        if ($journalSettled -and ($committed -or $rolledBack)) {
            if ([IO.Directory]::Exists($transaction)) { Remove-Item -LiteralPath $transaction -Recurse -Force }
            if ($journalCreated -and [IO.File]::Exists($journalPath)) {
                $current = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json
                if ($current.transaction_id -cne $transactionId) { throw 'Repair journal changed before cleanup.' }
                [IO.File]::Delete($journalPath)
            }
        }
    } finally {
        if ($null -ne $lease) { $lease.Dispose() }
        if ($null -ne $maintenance) { $maintenance.Dispose() }
        if ($leaseCreated -and [IO.File]::Exists($lockPath)) { try { [IO.File]::Delete($lockPath) } catch { } }
        if (-not $committed -and $rolledBack) {
            for ($index = $createdDirectories.Count - 1; $index -ge 0; $index--) {
                $directory = $createdDirectories[$index]
                if ([IO.Directory]::Exists($directory) -and @(Get-ChildItem -LiteralPath $directory -Force).Count -eq 0) { [IO.Directory]::Delete($directory) }
            }
        }
    }
}
$exe = Join-Path $InstallDir 'ProGo.exe'

Write-Host "Repair runtime restored and verified: $InstallDir"
if (-not $NoLaunch) {
    Start-Process -FilePath $exe -WorkingDirectory $InstallDir | Out-Null
    Write-Host "ProGo launched."
}
