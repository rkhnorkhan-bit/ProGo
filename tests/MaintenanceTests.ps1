param([string]$Exe, [string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: maintenance integration requires isolated Windows CI'; return }
. (Join-Path $Scripts 'Maintenance-ProGo.ps1')
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
    $p = [Diagnostics.Process]::Start($Info)
    try {
        if (-not $p.WaitForExit(20000)) { $p.Kill(); throw 'Maintenance child timed out' }
        return $p.ExitCode
    } finally { $p.Dispose() }
}
function DriverInfo($Mode) { return (ChildInfo $ps "-NoProfile -File `"$driver`" -Mode $Mode -Scripts `"$Scripts`" -Fixture `"$fixture`" -Release `"$release`"") }
function Snapshot {
    return (@(Get-ChildItem $install -Recurse -File | Sort-Object FullName | ForEach-Object { $_.FullName + ':' + (Get-FileHash $_.FullName).Hash }) -join "`n")
}
$holder = $null; $lease = $null
try {
    New-Item -ItemType Directory -Force $install,$backup | Out-Null
    Copy-Item $Exe (Join-Path $install 'ProGo.exe')
    Set-Content (Join-Path $install 'VERSION') '0.0.1'
    Set-Content (Join-Path $install 'settings.json') '{}'
    Copy-Item $Scripts (Join-Path $install 'scripts') -Recurse
    Set-Content (Join-Path $backup 'manifest.txt') 'version=0.0.1'
    Set-Content (Join-Path $backup 'VERSION') '0.0.1'
    Set-Content (Join-Path $backup 'settings.json') '{"SocksPort":12345}'
    $lease = [ProGo.MaintenanceOperation]::Enter()
    $before = Snapshot
    foreach ($script in @('Update-ProGo.Core.ps1','Update-ProGo.ps1','Restore-ProGoBackup.ps1')) {
        $args = "-NoProfile -File `"$(Join-Path $Scripts $script)`" -NoLaunch"
        if ($script -eq 'Restore-ProGoBackup.ps1') { $args += " -BackupDir `"$backup`"" }
        Check ((Run (ChildInfo $ps $args)) -ne 0) "competing entry point rejects while maintenance owns state: $script"
        Check ((Snapshot) -eq $before) "rejected $script leaves files, backups and logs unchanged"
    }
    Check ((Run (ChildInfo $Exe '--self-check' -NoPermit)) -eq 4) 'ordinary self-check cannot touch shared state during maintenance'
    Check ((Run (DriverInfo 'startup')) -eq 0) 'authorized child inherits the owner permit'
    Check ((Run (ChildInfo $ps "-NoProfile -File `"$driver`" -Mode startup -Scripts `"$Scripts`"" -NoPermit)) -eq 4) 'normal startup is blocked while files may be replaced'
    Check ((Run (ChildInfo $Exe '--self-check')) -eq 0) 'compiled staging self-check runs under owner permit'
    $permitName = $env:PROGO_MAINTENANCE_PERMIT
    $lease.Dispose(); $lease = $null
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

    Check ((Run (DriverInfo 'update')) -eq 0) 'actual update transaction validates staging and installed executable'
    Check (((Get-Content (Join-Path $install 'VERSION')).Trim()) -eq ((Get-Content (Join-Path $release 'VERSION')).Trim())) 'successful update commits the fixture release'
    Check ((Run (DriverInfo 'restore')) -eq 0) 'actual restore transaction completes under exclusive ownership'
    Check ((Get-Content (Join-Path $install 'settings.json') -Raw).Contains('12345')) 'restore applies the selected backup settings'
    Check ((Run (DriverInfo 'rollback')) -ne 0) 'post-commit failure is reported'
    Check (((Get-Content (Join-Path $install 'VERSION')).Trim()) -eq '0.0.1') 'failed update rolls back while retaining exclusive ownership'
    Check ((Run (DriverInfo 'startup')) -eq 0) 'failure cleanup releases ownership for subsequent startup'
    Write-Host "Maintenance tests PASS: $passed"
} finally {
    if ($null -ne $lease) { $lease.Dispose() }
    if ($null -ne $holder) { if (-not $holder.HasExited) { $holder.Kill() }; $holder.Dispose() }
    Remove-Item $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
