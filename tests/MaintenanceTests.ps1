param([string]$Exe, [string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: maintenance integration requires isolated Windows CI'; return }
. (Join-Path $Scripts 'Maintenance-ProGo.ps1')
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
. (Join-Path $Scripts 'BackupRetention-ProGo.ps1')
$passed = 0
function Check($Value, $Name) { if (-not $Value) { throw $Name }; $script:passed++; Write-Host "PASS: $Name" }
$fixture = Join-Path $env:TEMP ('ProGo-maintenance-test-' + [guid]::NewGuid().ToString('N'))
$install = Join-Path $fixture 'ProGo'
$backup = Join-Path $fixture 'backup'
$release = Split-Path -Parent $Exe
$driver = Join-Path $PSScriptRoot 'MaintenanceTestDriver.ps1'
$ps = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
function ChildInfo($File, $Arguments, [switch]$NoPermit) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $File; $info.Arguments = $Arguments; $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.EnvironmentVariables['LOCALAPPDATA'] = $fixture
    if ($NoPermit) { $info.EnvironmentVariables.Remove('PROGO_MAINTENANCE_PERMIT') }
    return $info
}
function Run($Info) {
    $Info.RedirectStandardOutput = $true; $Info.RedirectStandardError = $true
    $p = [Diagnostics.Process]::Start($Info)
    $stdout = $p.StandardOutput.ReadToEndAsync(); $stderr = $p.StandardError.ReadToEndAsync()
    try {
        if (-not $p.WaitForExit(20000)) { $p.Kill(); throw 'Maintenance child timed out' }
        if ($p.ExitCode -ne 0) {
            $details = $stdout.Result + $stderr.Result
            Write-Host ("Fixture child exit=" + $p.ExitCode + "; " + $details.Substring([Math]::Max(0, $details.Length - 3000)))
        }
        return $p.ExitCode
    } finally { $p.Dispose() }
}
function DriverInfo($Mode) { return (ChildInfo $ps "-NoProfile -File `"$driver`" -Mode $Mode -Scripts `"$Scripts`" -Fixture `"$fixture`" -Release `"$release`"") }
function PayloadSnapshot {
    $names = @('ProGo.exe','VERSION','scripts','ProGo.ico','settings.json','vault.enc.json')
    return (@(foreach ($name in $names) {
        $path = Join-Path $install $name
        if (-not (Test-Path -LiteralPath $path)) { $name + ':absent'; continue }
        foreach ($item in @(Get-Item -LiteralPath $path) + @(if ((Get-Item -LiteralPath $path).PSIsContainer) { Get-ChildItem -LiteralPath $path -Recurse -Force })) {
            if ($item.PSIsContainer) { $item.FullName.Substring($install.Length) + ':directory' }
            else { $item.FullName.Substring($install.Length) + ':' + (Get-FileHash -LiteralPath $item.FullName).Hash }
        }
    }) | Sort-Object) -join "`n"
}
function Snapshot {
    return (@(Get-ChildItem $install -Recurse -File | Sort-Object FullName | ForEach-Object { $_.FullName + ':' + (Get-FileHash $_.FullName).Hash }) -join "`n")
}
$holder = $null; $lease = $null; $primary = $null
$runtimeSettings = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'ProGo\settings.json'
$originalSettings = if (Test-Path $runtimeSettings) { [IO.File]::ReadAllBytes($runtimeSettings) } else { $null }
try {
    New-Item -ItemType Directory -Force $install,$backup | Out-Null
    Copy-Item $Exe (Join-Path $install 'ProGo.exe')
    Set-Content (Join-Path $install 'VERSION') '0.0.1'
    Set-Content (Join-Path $install 'settings.json') '{}'
    Copy-Item $Scripts (Join-Path $install 'scripts') -Recurse
    Copy-Item $Exe (Join-Path $backup 'ProGo.exe')
    Copy-Item $Scripts (Join-Path $backup 'scripts') -Recurse
    Set-Content (Join-Path $backup 'manifest.txt') @('product=ProGo','version=0.0.1')
    Set-Content (Join-Path $backup 'VERSION') '0.0.1'
    Set-Content (Join-Path $backup 'settings.json') '{"SocksPort":12345}'
    [ProGo.BackupIntegrity]::Write($backup)
    $beforeInvalidRestore = Snapshot
    Add-Content (Join-Path $backup 'VERSION') 'damaged-fixture'
    Check ((Run (DriverInfo 'restore')) -ne 0) 'actual restore refuses damaged backup before handoff'
    Check ((Snapshot) -eq $beforeInvalidRestore) 'failed backup preflight leaves installed files and logs untouched'
    Set-Content (Join-Path $backup 'VERSION') '0.0.1'
    [ProGo.BackupIntegrity]::Validate($backup)
    $lease = [ProGo.MaintenanceOperation]::Enter()
    $before = Snapshot
    foreach ($script in @('Update-ProGo.Core.ps1','Update-ProGo.ps1','Restore-ProGoBackup.ps1')) {
        $args = "-NoProfile -File `"$(Join-Path $Scripts $script)`" -NoLaunch"
        if ($script -eq 'Restore-ProGoBackup.ps1') { $args += " -BackupDir `"$backup`"" }
        Check ((Run (ChildInfo $ps $args)) -ne 0) "competing entry point rejects while maintenance owns state: $script"
        Check ((Snapshot) -eq $before) "rejected $script leaves files, backups and logs unchanged"
    }
    Check ((Run (ChildInfo $Exe '--show' -NoPermit)) -eq 4) 'normal compiled launch exits without a blocking window during maintenance'
    Check ((Run (ChildInfo $Exe '--self-check' -NoPermit)) -eq 4) 'ordinary self-check cannot touch shared state during maintenance'
    Check ((Run (DriverInfo 'startup')) -eq 0) 'authorized child inherits the owner permit'
    Check ((Run (ChildInfo $ps "-NoProfile -File `"$driver`" -Mode startup -Scripts `"$Scripts`"" -NoPermit)) -eq 4) 'normal startup is blocked while files may be replaced'
    Check ((Run (ChildInfo $Exe '--self-check')) -eq 0) 'compiled staging self-check runs under owner permit'
    Set-Content $runtimeSettings '{"AutoCliProxy":false,"AutoSystemProxy":false,"AutoStartSocks":false,"AutoRestartSocks":false}'
    $primary = [Diagnostics.Process]::Start((ChildInfo $Exe '--show'))
    Start-Sleep -Seconds 2
    Check (-not $primary.HasExited) 'authorized updated application starts while operation owns the gate'
    Check ((Run (ChildInfo $Exe '--self-check')) -eq 3) 'restarted application owns its lifetime before maintenance releases the gate'
    $permitName = $env:PROGO_MAINTENANCE_PERMIT
    $lease.Dispose(); $lease = $null
    Check ((Run (ChildInfo $Exe '--self-check' -NoPermit)) -eq 3) 'restarted application retains single-instance ownership after handoff'
    $primary.Kill(); [void]$primary.WaitForExit(5000); $primary.Dispose(); $primary = $null
    $stale = DriverInfo 'startup'; $stale.EnvironmentVariables['PROGO_MAINTENANCE_PERMIT'] = $permitName
    # A different current operation invalidates the old permit even if the name is replayed.
    $lease = [ProGo.MaintenanceOperation]::Enter()
    Check ((Run $stale) -eq 4) 'expired owner permit cannot bypass a later operation'
    $lease.Dispose(); $lease = $null
    Check ((Run (DriverInfo 'startup')) -eq 0) 'normal startup resumes after disposal'

    $handoff = DriverInfo 'handoff'
    Check ([ProGo.MaintenanceOperation]::StartHandoff($handoff)) 'handoff confirms actual lease acquisition before the app exits'
    # Wait for the acknowledged child to release its lease, rather than assume a delay.
    $deadline = (Get-Date).AddSeconds(10)
    do {
        try { $lease = [ProGo.MaintenanceOperation]::Enter() } catch { Start-Sleep -Milliseconds 100 }
    } while ($null -eq $lease -and (Get-Date) -lt $deadline)
    Check ($null -ne $lease) 'acknowledged helper releases ownership after completion'
    $lease.Dispose(); $lease = $null
    Check (-not [ProGo.MaintenanceOperation]::StartHandoff((ChildInfo $ps '-NoProfile -Command "exit 7"'))) 'failed helper never confirms handoff'
    $lease = [ProGo.MaintenanceOperation]::Enter()
    Check (-not [ProGo.MaintenanceOperation]::StartHandoff((DriverInfo 'handoff'))) 'busy helper does not close the initiating application'
    $lease.Dispose(); $lease = $null

    $holder = [Diagnostics.Process]::Start((DriverInfo 'hold'))
    $deadline = (Get-Date).AddSeconds(10)
    while (-not (Test-Path (Join-Path $fixture 'held'))) {
        if ($holder.HasExited -or (Get-Date) -gt $deadline) { throw 'Holder did not acquire maintenance' }
        Start-Sleep -Milliseconds 50
    }
    $holder.Kill(); [void]$holder.WaitForExit(5000); $holder.Dispose(); $holder = $null
    $lease = [ProGo.MaintenanceOperation]::Enter()
    Check ($null -ne $lease) 'abandoned operation mutex recovers after helper crash'
    $lease.Dispose(); $lease = $null

    $holder = [Diagnostics.Process]::Start((DriverInfo 'hold-app'))
    $deadline = (Get-Date).AddSeconds(10)
    while (-not (Test-Path (Join-Path $fixture 'held-app'))) {
        if ($holder.HasExited -or (Get-Date) -gt $deadline) { throw 'Application holder did not start' }
        Start-Sleep -Milliseconds 50
    }
    $settingsBefore = Get-Content (Join-Path $install 'settings.json') -Raw
    foreach ($mode in @('update','restore')) {
        Check ((Run (DriverInfo $mode)) -ne 0) "transaction refuses writes while application still owns its lifetime: $mode"
        Check ((Get-Content (Join-Path $install 'settings.json') -Raw) -eq $settingsBefore -and ((Get-Content (Join-Path $install 'VERSION')).Trim()) -eq '0.0.1') "active application settings/version preserved: $mode"
    }
    $holder.Kill(); [void]$holder.WaitForExit(5000); $holder.Dispose(); $holder = $null

    $pending = Join-Path $install 'system-proxy-backup.json'
    Set-Content $pending '{}'
    foreach ($mode in @('update','restore')) {
        Check ((Run (DriverInfo $mode)) -ne 0) "transaction refuses replacement after stopped owner left pending cleanup: $mode"
        Check ((Get-Content (Join-Path $install 'settings.json') -Raw) -eq $settingsBefore -and (Test-Path $pending)) "pending cleanup preserves settings and journal: $mode"
    }
    Remove-Item $pending # Isolated synthetic journal only.
    $mixedRoot = Join-Path $install 'backups'
    New-Item -ItemType Directory $mixedRoot -Force | Out-Null
    foreach ($i in 0..29) {
        $dir = Join-Path $mixedRoot ('backup-20260101-{0:D4}' -f $i)
        New-Item -ItemType Directory $dir | Out-Null
        Set-Content (Join-Path $dir 'manifest.txt') @('product=ProGo','backup_kind=automatic')
    }
    $manualCopy = Join-Path $mixedRoot 'backup-20250101-manual'
    $unknownCopy = Join-Path $mixedRoot 'unknown-directory'
    New-Item -ItemType Directory $manualCopy,$unknownCopy | Out-Null
    Set-Content (Join-Path $manualCopy 'manifest.txt') @('product=ProGo','backup_kind=manual')
    Set-Content (Join-Path $manualCopy 'payload.txt') 'original manual copy'
    Check ((Run (DriverInfo 'update')) -eq 0) 'actual update transaction validates staging and installed executable'
    Check ((Get-Content (Join-Path $manualCopy 'payload.txt')).Trim() -eq 'original manual copy' -and (Test-Path $unknownCopy)) 'actual update transaction preserves old manual contents and unknown directory beyond twenty copies'
    Check (((Get-Content (Join-Path $install 'VERSION')).Trim()) -eq ((Get-Content (Join-Path $release 'VERSION')).Trim())) 'successful update commits the fixture release'
    $beforeConsentRefusal = Snapshot
    Check ((Run (DriverInfo 'restore-no-consent')) -ne 0) 'actual helper rejects user-data replacement without explicit consent'
    Check ((Snapshot) -eq $beforeConsentRefusal) 'missing consent leaves installed files and logs untouched'
    $dataBeforeProgram = [IO.File]::ReadAllBytes((Join-Path $install 'settings.json'))
    Set-Content (Join-Path $install 'vault.enc.json') 'current opaque vault'
    Set-Content (Join-Path $backup 'vault.enc.json') 'selected opaque vault'
    [ProGo.BackupIntegrity]::Write($backup)
    Check ((Run (DriverInfo 'restore-program')) -eq 0) 'program-only restore completes with no data consent'
    Check ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $install 'settings.json'))) -eq [Convert]::ToBase64String($dataBeforeProgram) -and ((Get-Content (Join-Path $install 'vault.enc.json')).Trim()) -eq 'current opaque vault') 'program-only restore preserves current settings and opaque vault bytes'
    Set-Content (Join-Path $install 'VERSION') 'data-only-program-marker'
    $exeBeforeData = (Get-FileHash (Join-Path $install 'ProGo.exe')).Hash
    $scriptsBeforeData = @(Get-ChildItem (Join-Path $install 'scripts') -Recurse -File | Sort-Object FullName | ForEach-Object { $_.Name + ':' + (Get-FileHash $_.FullName).Hash }) -join ';'
    Check ((Run (DriverInfo 'restore-data')) -eq 0) 'data-only restore completes with explicit consent'
    Check (((Get-Content (Join-Path $install 'VERSION')).Trim()) -eq 'data-only-program-marker' -and (Get-FileHash (Join-Path $install 'ProGo.exe')).Hash -eq $exeBeforeData -and (@(Get-ChildItem (Join-Path $install 'scripts') -Recurse -File | Sort-Object FullName | ForEach-Object { $_.Name + ':' + (Get-FileHash $_.FullName).Hash }) -join ';') -eq $scriptsBeforeData) 'data-only restore preserves executable, version and all script bytes'
    Check (((Get-Content (Join-Path $install 'vault.enc.json')).Trim()) -eq 'selected opaque vault') 'data-only restore applies selected opaque vault without changing encryption'
    Check ((Run (DriverInfo 'restore-source-changed')) -eq 0) 'helper applies prepared input after original source changes'
    Check ((Get-Content (Join-Path $install 'settings.json') -Raw).Contains('12345')) 'post-preparation source mutation does not reach installed settings'
    Set-Content (Join-Path $backup 'settings.json') '{"SocksPort":12345}'
    [ProGo.BackupIntegrity]::Write($backup)
    Check ((Run (DriverInfo 'restore')) -eq 0) 'actual restore transaction completes under exclusive ownership'
    Check ((Get-Content (Join-Path $install 'settings.json') -Raw).Contains('12345')) 'restore applies the selected backup settings'
    $protected = @(Get-ChildItem $mixedRoot -Directory | Where-Object { $_.Name -like '*-pre-restore-*' })
    Check ($protected.Count -ge 4) 'each successful restore retains an independent protective backup'
    foreach ($copy in $protected) { [ProGo.BackupIntegrity]::Validate($copy.FullName) }
    Check ($true) 'all protective snapshots pass recorded integrity checks'
    $protectedBeforeRetention = $protected.Count
    [void][ProGo.BackupRetention]::Apply([ProGo.BackupRetention]::Plan($mixedRoot))
    Check (@(Get-ChildItem $mixedRoot -Directory | Where-Object { $_.Name -like '*-pre-restore-*' }).Count -eq $protectedBeforeRetention) 'ordinary retention preserves recovery snapshots'

    # Real exclusive Windows locks must refuse before the first installed root moves.
    $beforeLocks = PayloadSnapshot
    $lockedPath = Join-Path $install 'vault.enc.json'
    $locked = [IO.File]::Open($lockedPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
    try { Check ((Run (DriverInfo 'restore')) -ne 0) 'locked selected vault refuses restore before mutation' } finally { $locked.Dispose() }
    Check ((PayloadSnapshot) -eq $beforeLocks) 'exclusive lock refusal preserves all installed payload bytes'
    $unchanged = PayloadSnapshot
    # Repeat with a read-only-share lock: hashes can be read but replacement is forbidden.
    $locked = [IO.File]::Open($lockedPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try { Check ((Run (DriverInfo 'restore')) -ne 0) 'readable but nonreplaceable selected vault refuses restore' } finally { $locked.Dispose() }
    Check ((PayloadSnapshot) -eq $unchanged) 'lock refusal preserves every program and data byte'

    Set-Content (Join-Path $install 'VERSION') 'previous-installed-version'
    Set-Content (Join-Path $install 'settings.json') '{"SocksPort":23456}'
    Set-Content (Join-Path $install 'scripts\old-only.ps1') '# previous script'
    Remove-Item -LiteralPath (Join-Path $install 'vault.enc.json') -Force
    foreach ($mode in @('restore-stage-copy-fail','restore-commit-fail','restore-installed-corrupt','restore-self-check-fail','restore-launch-fail')) {
        $beforeFailure = PayloadSnapshot
        Check ((Run (DriverInfo $mode)) -ne 0) "real restore reports failure: $mode"
        Check ((PayloadSnapshot) -eq $beforeFailure) "failed restore preserves all original bytes, scripts and missing vault: $mode"
        Check (@(Get-ChildItem $install -Directory | Where-Object { $_.Name -like 'restore-txn-*' }).Count -eq 0) "verified failure cleanup removes disposable transaction: $mode"
        Check ((Run (DriverInfo 'startup')) -eq 0) "failed restore releases maintenance ownership: $mode"
    }
    # When rollback itself is denied, retain both original roots and the verified backup;
    # never emit the success marker or erase the remaining recovery input.
    Check ((Run (DriverInfo 'restore-rollback-fail')) -ne 0) 'rollback failure is reported rather than successful restore'
    $recovery = @(Get-ChildItem $install -Directory | Where-Object { $_.Name -like 'restore-txn-*' })
    Check ($recovery.Count -eq 1 -and (Test-Path (Join-Path $recovery[0].FullName 'previous\ProGo.exe'))) 'incomplete rollback retains original program roots'
    Check ((Get-Content -LiteralPath (Join-Path $install 'progo-restore.log') -Raw).Contains('Rollback incomplete. Recovery files retained:')) 'incomplete rollback log identifies retained recovery paths'
    # Repair only the disposable fixture so unrelated updater regressions can continue.
    foreach ($name in @('ProGo.exe','VERSION')) {
        $old = Join-Path $recovery[0].FullName ('previous\' + $name)
        if (Test-Path -LiteralPath $old) { Copy-Item -LiteralPath $old -Destination (Join-Path $install $name) -Force }
    }
    Remove-Item -LiteralPath $recovery[0].FullName -Recurse -Force
    Check ((Run (DriverInfo 'restore')) -eq 0) 'next full restore succeeds after recovery and releases prior faults'

    Check ((Run (DriverInfo 'rollback')) -ne 0) 'post-commit failure is reported'
    Check (((Get-Content (Join-Path $install 'VERSION')).Trim()) -eq '0.0.1') 'failed update rolls back while retaining exclusive ownership'
    Check ((Test-Path (Join-Path $manualCopy 'payload.txt')) -and (Test-Path $unknownCopy)) 'failed update rollback also preserves protected backup folders'
    Check ((Run (DriverInfo 'startup')) -eq 0) 'failure cleanup releases ownership for subsequent startup'
    Write-Host "Maintenance tests PASS: $passed"
} finally {
    if ($null -ne $lease) { $lease.Dispose() }
    if ($null -ne $primary) { if (-not $primary.HasExited) { $primary.Kill() }; $primary.Dispose() }
    if ($null -ne $originalSettings) { [IO.File]::WriteAllBytes($runtimeSettings, $originalSettings) } else { Remove-Item $runtimeSettings -ErrorAction SilentlyContinue }
    if ($null -ne $holder) { if (-not $holder.HasExited) { $holder.Kill() }; $holder.Dispose() }
    Remove-Item $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
