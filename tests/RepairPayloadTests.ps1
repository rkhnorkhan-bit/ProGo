param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: repair payload tests require isolated Windows CI'; return }
if (-not ('RepairFixtureImagePath' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class RepairFixtureImagePath {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle file, StringBuilder path, uint size, uint flags);
    private static string Canonical(string path) {
        using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
            var text = new StringBuilder(32768);
            uint size = GetFinalPathNameByHandle(file.SafeFileHandle, text, (uint)text.Capacity, 0);
            if (size == 0 || size >= text.Capacity) throw new Win32Exception(Marshal.GetLastWin32Error());
            return text.ToString();
        }
    }
    public static bool Same(string left, string right) {
        return String.Equals(Canonical(left), Canonical(right), StringComparison.OrdinalIgnoreCase);
    }
}
'@
}
$release = Join-Path (Split-Path -Parent $Scripts) 'release'
$work = Join-Path $env:TEMP ('ProGo-repair-preflight-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $work 'payload'
$userRoot = Join-Path $work 'local-app-data'
$installed = Join-Path $userRoot 'ProGo'
function Get-FixtureSnapshot([string]$SnapshotRoot = $userRoot) {
    $result = @{}
    if (-not [IO.Directory]::Exists($SnapshotRoot)) { return $result }
    foreach ($file in @(Get-ChildItem -LiteralPath $SnapshotRoot -File -Recurse)) {
        $result[$file.FullName.Substring($SnapshotRoot.Length)] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
    return $result
}
function Assert-FixtureSnapshot($Expected, [string]$Message) {
    $actual = Get-FixtureSnapshot
    if ($Expected.Count -ne $actual.Count) { throw $Message }
    foreach ($path in $Expected.Keys) { if (-not $actual.ContainsKey($path) -or $actual[$path] -cne $Expected[$path]) { throw $Message } }
}
# Read-only evidence for only the complete positive repair. Never log captured output,
# exception messages, paths, command lines, raw process IDs or journal contents.
function Write-FixtureRepairProbe([string]$Checkpoint) {
    try {
    $known = $false; $exited = $false; $cpu = -1; $ram = -1
    try { $exited = $child.HasExited; $known = $true; if (-not $exited) { $child.Refresh(); $cpu = $child.TotalProcessorTime.TotalMilliseconds; $ram = $child.WorkingSet64 } } catch { }
    $phases = @(); $traceKnown = $false
    $allowedPhases = @('wrapper-enter', 'probe-ready', 'script-enter', 'preflight-complete', 'helper-loaded', 'maintenance-acquired', 'snapshot-enter', 'snapshot-complete', 'copy-complete', 'runtime-verified', 'selfcheck-call', 'selfcheck-return', 'selfcheck-verified', 'commit-enter', 'commit-durable', 'catch-enter', 'rollback-return', 'transaction-cleanup-enter', 'cleanup-return', 'repair-return', 'wrapper-error', 'probe-cleanup-enter', 'probe-cleanup-return')
    try {
        if ([IO.File]::Exists($probeTrace)) {
            $readerStream = New-Object IO.FileStream -ArgumentList @($probeTrace, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
            try {
                if ($readerStream.Length -gt 8192) { throw 'oversized diagnostic' }
                $reader = New-Object IO.StreamReader -ArgumentList @($readerStream, [Text.Encoding]::ASCII)
                try { $records = $reader.ReadToEnd() -split "`n" } finally { $reader.Dispose() }
                foreach ($record in $records) { if ($record -cmatch '^[a-z-]+:[0-9]+$' -and ($record -split ':')[0] -cin $allowedPhases) { $phases += $record } elseif ($record.Length -ne 0) { throw 'incomplete diagnostic record' } }
                $traceKnown = $true
            } finally { $readerStream.Dispose() }
        }
    } catch { $traceKnown = $false }
    $journalPresent = [IO.File]::Exists((Join-Path $installed '.repair-state.json'))
    $journalKnown = $false; $phase = 'unknown'; $entries = -1; $appliedCount = -1; $checkRoot = $false
    try {
        $journalFile = Join-Path $installed '.repair-state.json'
        if ($journalPresent -and (Get-Item -LiteralPath $journalFile).Length -le 65536) {
            $record = [IO.File]::ReadAllText($journalFile) | ConvertFrom-Json
            if ($record.phase -cin @('preparing', 'prepared', 'applying', 'committed', 'rolled-back') -and @($record.entries).Count -le 41 -and @($record.applied).Count -le 41) {
                $phase = $record.phase; $entries = @($record.entries).Count; $appliedCount = @($record.applied).Count; $journalKnown = $true
            }
        }
        $stages = @(Get-ChildItem -LiteralPath $installed -Directory -Filter '.repair-stage-*' | Select-Object -First 8)
        foreach ($stage in $stages) {
            if ($stage.Name -cmatch '^\.repair-stage-[a-f0-9]{32}$' -and -not ($stage.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                $checkRoot = $checkRoot -or [IO.Directory]::Exists((Join-Path $stage.FullName 'self-check-data'))
            }
        }
    } catch { $journalKnown = $false }
    [Console]::WriteLine('REPAIR_FIXTURE checkpoint=' + $Checkpoint + '; elapsedMs=' + $probeClock.ElapsedMilliseconds + '; startMs=' + $probeStartMs +
        '; exitedKnown=' + $known + '; exited=' + $exited + '; stdout=' + $stdout.Status + '; stderr=' + $stderr.Status + '; cpuMs=' + $cpu + '; ramBytes=' + $ram +
        '; traceKnown=' + $traceKnown + '; journalPresent=' + $journalPresent + '; journalKnown=' + $journalKnown + '; phase=' + $phase + '; entries=' + $entries + '; applied=' + $appliedCount + '; selfCheckRoot=' + $checkRoot)
    [Console]::WriteLine('REPAIR_FIXTURE phases=' + ($phases -join ','))
    if ($stdout.Status -eq [Threading.Tasks.TaskStatus]::RanToCompletion) {
        [Console]::WriteLine('REPAIR_FIXTURE output selfCheckPass=' + $stdout.Result.Contains('Repair self-check PASS:') + '; repairVerified=' + $stdout.Result.Contains('Repair runtime restored and verified:'))
        foreach ($line in ($stdout.Result -split "`n")) { if ($line.TrimEnd("`r") -cmatch '^REPAIR_PROBE_STACK [A-Za-z0-9_.<>,]*$') { [Console]::WriteLine($line.TrimEnd("`r")) } }
    }
    if ($Checkpoint -eq 'deadline') {
        $stack = New-Object Diagnostics.StackTrace -ArgumentList @($false)
        $methods = @($stack.GetFrames() | Select-Object -First 6 | ForEach-Object { $method = $_.GetMethod(); if ($null -ne $method -and $null -ne $method.DeclaringType) { (($method.DeclaringType.Name + '.' + $method.Name) -replace '[^A-Za-z0-9_.<>]', '') } })
        [Console]::WriteLine('REPAIR_FIXTURE_STACK ' + ($methods -join ','))
    }
    } catch { [Console]::WriteLine('REPAIR_FIXTURE diagnostic-error-type=' + $_.Exception.GetType().Name) }
}
function Invoke-FixtureRepair([switch]$Recover, [string]$ExpectedFailure, [ValidateSet('applying', 'committed')][string]$ExpectedPhase = 'applying') {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $PSHOME 'powershell.exe'
    $info.Arguments = '-NoProfile -File "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -NoLaunch'
    if ($Recover) { $info.Arguments += ' -Recover' }
    else { $info.Arguments += ' -PayloadDirectory "' + $payload + '"' }
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $info.EnvironmentVariables['LOCALAPPDATA'] = $userRoot
    $child = [Diagnostics.Process]::Start($info)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    try {
        if (-not $child.WaitForExit(10000)) { throw 'Owned repair recovery command exceeded ten seconds' }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Repair recovery command output did not settle' }
        if ($ExpectedFailure) {
            if ($child.ExitCode -eq 0 -or -not $stderr.Result.Contains($ExpectedFailure)) { throw ('Repair recovery did not refuse: ' + $ExpectedFailure) }
        } elseif ($child.ExitCode -ne 0 -or -not $stdout.Result.Contains('Repair recovery complete: ' + $ExpectedPhase + '; no application launched')) { throw 'Explicit repair recovery did not complete' }
    } finally {
        try {
            if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned recovery command cleanup failed' } }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned recovery command output cleanup failed' }
        } finally { $child.Dispose() }
    }
}
try {
    New-Item -ItemType Directory -Path $payload -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $installed 'backups') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $installed 'settings.json'), 'opaque private settings fixture')
    [IO.File]::WriteAllText((Join-Path $installed 'vault.enc.json'), 'opaque protected vault fixture')
    [IO.File]::WriteAllText((Join-Path $installed 'backups\keep.txt'), 'opaque backup fixture')
    $before = Get-FixtureSnapshot
    # Exercise the actual PS5.1-to-.NET string binder. A plain $null can
    # become an illegal empty backup path; NullString must pass a real null.
    $interop = Join-Path $work 'null-string-interop'; [IO.Directory]::CreateDirectory($interop) | Out-Null
    $newFile = Join-Path $interop 'new'; $oldFile = Join-Path $interop 'target'
    [IO.File]::WriteAllText($newFile, 'new'); [IO.File]::WriteAllText($oldFile, 'old')
    $plainNullRefused = $false
    try { [IO.File]::Replace($newFile, $oldFile, $null) }
    catch {
        $cause = $_.Exception.GetBaseException()
        if ($cause -isnot [ArgumentException]) { throw }
        $plainNullRefused = $true
    }
    if ($PSVersionTable.PSVersion.Major -eq 5 -and -not $plainNullRefused) { throw 'Expected native PS5 null-string boundary was not reproduced' }
    if ($plainNullRefused -and ([IO.File]::ReadAllText($oldFile) -cne 'old' -or [IO.File]::ReadAllText($newFile) -cne 'new')) { throw 'Refused null-string replacement changed fixture bytes' }
    [IO.File]::WriteAllText($newFile, 'new'); [IO.File]::WriteAllText($oldFile, 'old')
    [IO.File]::Replace($newFile, $oldFile, [System.Management.Automation.Language.NullString]::Value)
    if ([IO.File]::ReadAllText($oldFile) -cne 'new' -or [IO.File]::Exists($newFile) -or @(Get-ChildItem -LiteralPath $interop -File).Count -ne 1) { throw 'Real null backup replacement failed or created an unexpected file' }
    Write-Host 'Repair native null-string interop tests PASS: 1'
    $passed = 0
    foreach ($relative in @('ProGo.exe', 'scripts\InstalledUpdateTransport.cs', 'scripts\home-vpn\server\profile_share.py', 'scripts\Update-ProGo.Core.ps1')) {
        Get-ChildItem -LiteralPath $payload -Force | Remove-Item -Force -Recurse
        Get-ChildItem -LiteralPath $release -Force | Copy-Item -Destination $payload -Recurse -Force
        $damaged = Join-Path $payload $relative
        if (-not (Test-Path -LiteralPath $damaged -PathType Leaf)) { throw ('Missing built test input: ' + $relative) }
        if ($relative -eq 'scripts\Update-ProGo.Core.ps1') { [IO.File]::WriteAllBytes($damaged, [byte[]]@()) }
        else { Remove-Item -LiteralPath $damaged -Force }
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = Join-Path $PSHOME 'powershell.exe'
        $info.Arguments = '-NoProfile -File "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -NoLaunch -PayloadDirectory "' + $payload + '"'
        $info.UseShellExecute = $false; $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $info.EnvironmentVariables['LOCALAPPDATA'] = $userRoot
        $child = [Diagnostics.Process]::Start($info)
        $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
        try {
            if (-not $child.WaitForExit(10000)) { throw 'Repair preflight child exceeded its ten-second limit' }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Repair preflight output did not settle' }
            if ($child.ExitCode -eq 0 -or -not $stderr.Result.Contains('Repair payload incomplete or empty:') -or $stdout.Result.Contains('Repair OK')) {
                throw ('Repair did not explicitly refuse the damaged payload: ' + $relative)
            }
        } finally {
            try {
                if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned repair child cleanup failed' } }
                if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned repair output cleanup failed' }
            } finally { $child.Dispose() }
        }
        $after = Get-FixtureSnapshot
        if ($before.Count -ne $after.Count) { throw 'Refused repair added or removed installation files' }
        foreach ($path in $before.Keys) { if (-not $after.ContainsKey($path) -or $after[$path] -cne $before[$path]) { throw 'Refused repair changed private fixture bytes' } }
        if (Test-Path -LiteralPath (Join-Path $installed 'scripts')) { throw 'Refused repair created installation directories before preflight' }
        $passed++
        Write-Host ('PASS: incomplete repair payload refused before installation writes: ' + $relative)
    }
    # A complete, real Build output is copied from one payload. Its actual
    # installed image must pass the isolated self-check before success.
    Get-ChildItem -LiteralPath $payload -Force | Remove-Item -Force -Recurse
    Get-ChildItem -LiteralPath $release -Force | Copy-Item -Destination $payload -Recurse -Force
    # Build also supplies three distributable documents. Install-ProGo does
    # not deploy them as runtime leaves; every other built file is mandatory.
    $documents = @('README.md', 'README.ru.md', 'LICENSE.md')
    $payloadRoot = (Get-Item -LiteralPath $payload).FullName.TrimEnd([char]'\')
    $sourceFiles = @(Get-ChildItem -LiteralPath $payload -File -Recurse | Where-Object {
        -not ($_.DirectoryName -ieq $payloadRoot -and $documents -icontains $_.Name)
    })
    if ($sourceFiles.Count -ne 41) { throw ('Built runtime changed: repair fixture must explicitly cover every mandatory leaf; runtime_count=' + $sourceFiles.Count) }
    $knownRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'ProGo'
    $knownRootExisted = [IO.Directory]::Exists($knownRoot)
    $knownBefore = Get-FixtureSnapshot $knownRoot
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $PSHOME 'powershell.exe'
    $info.Arguments = '-NoProfile -File "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -NoLaunch -PayloadDirectory "' + $payload + '"'
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $info.EnvironmentVariables['LOCALAPPDATA'] = $userRoot
    $originalCompleteArguments = $info.Arguments
    $probeTrace = Join-Path $work 'complete-repair.phases'
    $info.Arguments = '-NoProfile -File "' + (Join-Path $PSScriptRoot 'RepairPhaseProbe.ps1') + '" -RepairScript "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -Payload "' + $payload + '" -FixtureRoot "' + $work + '"'
    $probeClock = [Diagnostics.Stopwatch]::StartNew()
    [Console]::WriteLine('REPAIR_FIXTURE checkpoint=start-enter')
    $child = [Diagnostics.Process]::Start($info)
    $probeStartMs = $probeClock.ElapsedMilliseconds
    [Console]::WriteLine('REPAIR_FIXTURE checkpoint=start-return; startMs=' + $probeStartMs)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    try {
        if (-not $child.WaitForExit(20000)) { Write-FixtureRepairProbe 'deadline'; throw 'Complete repair with bounded self-check exceeded its twenty-second limit' }
        Write-FixtureRepairProbe 'process-exit'
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Complete repair output did not settle' }
        if ($child.ExitCode -ne 0 -or -not $stdout.Result.Contains('Repair runtime restored and verified:') -or -not $stdout.Result.Contains('Repair self-check PASS:')) {
            $safe = [regex]::Match($stderr.Result, '(?m)^PROGO_REPAIR_FAILURE Cause: [A-Za-z0-9]+; stage=[a-z-]+; script_line=\d+; hresult=-?\d+; native=-?\d+; check_exit=-?\d+')
            $position = [regex]::Match($stderr.Result, ':(\d+) char:(\d+)')
            $evidence = if ($safe.Success) { $safe.Value } else {
                'no classified repair failure; payload_incomplete=' + $stderr.Result.Contains('Repair payload incomplete or empty:') +
                '; pending_journal=' + $stderr.Result.Contains('An unfinished repair journal exists.') +
                '; stderr_script_line=' + $(if ($position.Success) { $position.Groups[1].Value } else { 'unknown' })
            }
            throw ('Complete repair runtime copy failed; child_exit=' + $child.ExitCode + '; ' + $evidence)
        }
    } finally {
        try {
            if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned complete repair child cleanup failed' } }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned complete repair output cleanup failed' }
        } finally {
            try { Write-FixtureRepairProbe 'cleanup-return' }
            finally { $child.Dispose(); $info.Arguments = $originalCompleteArguments }
        }
    }
    foreach ($source in $sourceFiles) {
        if (-not $source.FullName.StartsWith($payloadRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Runtime fixture source escaped its own payload root' }
        $relative = $source.FullName.Substring($payloadRoot.Length + 1)
        $target = Join-Path $installed $relative
        if (-not (Test-Path -LiteralPath $target -PathType Leaf) -or (Get-FileHash -LiteralPath $target).Hash -cne (Get-FileHash -LiteralPath $source.FullName).Hash) {
            throw ('Complete repair omitted or mixed runtime bytes: ' + $relative)
        }
    }
    $knownAfter = Get-FixtureSnapshot $knownRoot
    if ($knownRootExisted -ne [IO.Directory]::Exists($knownRoot) -or $knownBefore.Count -ne $knownAfter.Count) { throw 'Repair self-check touched the real Windows known-folder profile instead of its isolated data root' }
    foreach ($path in $knownBefore.Keys) { if (-not $knownAfter.ContainsKey($path) -or $knownAfter[$path] -cne $knownBefore[$path]) { throw 'Repair self-check changed known-folder profile bytes' } }
    Write-Host 'PASS: actual installed self-check uses a fresh explicit data root and leaves the Windows known-folder profile unchanged'
    $after = Get-FixtureSnapshot
    foreach ($path in $before.Keys) { if (-not $after.ContainsKey($path) -or $after[$path] -cne $before[$path]) { throw 'Complete runtime repair changed private fixture bytes' } }
    Write-Host 'PASS: complete prepared runtime copied byte-for-byte while private settings, vault and backup bytes remain unchanged'
    Write-Host 'Repair runtime copy tests PASS: 1'
    # Keep one old snapshot readable but nondeletable. The actual image check
    # succeeds; only post-commit cleanup fails. Recovery must retain the accepted
    # new VERSION rather than restoring the deliberately different old one.
    $acceptedSnapshot = Get-FixtureSnapshot
    [IO.File]::WriteAllText((Join-Path $installed 'VERSION'), 'old version before accepted repair')
    $heldSnapshot = $null
    $child = [Diagnostics.Process]::Start($info)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    try {
        $deadline = [Diagnostics.Stopwatch]::StartNew()
        while ($null -eq $heldSnapshot) {
            if ($child.WaitForExit(20)) { throw 'Repair exited before the owned snapshot sharing barrier' }
            foreach ($stage in @(Get-ChildItem -LiteralPath $installed -Directory -Filter '.repair-stage-*')) {
                $oldVersion = Join-Path $stage.FullName 'old\VERSION'
                try {
                    if ([IO.File]::Exists($oldVersion)) { $heldSnapshot = [IO.File]::Open($oldVersion, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read); break }
                } catch [IO.IOException] { }
            }
            if ($deadline.ElapsedMilliseconds -gt 10000) { throw 'Owned committed cleanup barrier was not reached within ten seconds' }
        }
        if (-not $child.WaitForExit(20000)) { throw 'Post-commit repair cleanup exceeded its bounded image-check budget' }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Post-commit repair output did not settle' }
        $journalPath = Join-Path $installed '.repair-state.json'
        $journal = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json
        if ($child.ExitCode -eq 0 -or $journal.phase -cne 'committed' -or -not $journal.self_check_passed -or -not $stdout.Result.Contains('Repair self-check PASS:') -or $stdout.Result.Contains('Repair runtime restored and verified:')) { throw 'Failed post-commit cleanup lost its durable accepted outcome or claimed a completed repair' }
        $pending = Get-FixtureSnapshot
        Invoke-FixtureRepair -ExpectedFailure 'An unfinished repair journal exists.'
        Assert-FixtureSnapshot $pending 'Pending committed cleanup repeated the repair or altered transaction bytes'
        $heldSnapshot.Dispose(); $heldSnapshot = $null
        # An accepted outcome needs no new application startup or stopped-app
        # gate. Own the real lifetime mutex: an accidental self-check could not
        # return success, while residue-only recovery must still work.
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        try { $instanceName = 'Global\ProGo.Instance.' + $identity.User.Value } finally { $identity.Dispose() }
        $instanceGate = New-Object Threading.Mutex($false, $instanceName)
        $instanceOwned = $false
        try {
            try { $instanceOwned = $instanceGate.WaitOne(0) } catch [Threading.AbandonedMutexException] { $instanceOwned = $true }
            if (-not $instanceOwned) { throw 'Isolated committed fixture could not acquire its own application lifetime gate' }
            Invoke-FixtureRepair -Recover -ExpectedPhase 'committed'
        } finally {
            if ($instanceOwned) { $instanceGate.ReleaseMutex() }
            $instanceGate.Dispose()
        }
        Assert-FixtureSnapshot $acceptedSnapshot 'Committed recovery rolled back an accepted runtime or left private/transaction changes'
        Write-Host 'PASS: real locked old snapshot preserves committed outcome; explicit recovery only cleans owned residue without rollback or relaunch'
        Write-Host 'Repair committed cleanup tests PASS: 1'
    } finally {
        try {
            if ($null -ne $heldSnapshot) { $heldSnapshot.Dispose() }
            if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned committed repair child cleanup failed' } }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned committed repair output cleanup failed' }
        } finally { $child.Dispose() }
    }
    # A real Windows read-sharing handle allows the old VERSION snapshot but
    # denies its replacement after ProGo.exe has already been replaced.
    [IO.File]::WriteAllText((Join-Path $installed 'ProGo.exe'), 'opaque old runtime fixture; never execute')
    $rollbackBefore = Get-FixtureSnapshot
    $held = [IO.File]::Open((Join-Path $installed 'VERSION'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = Join-Path $PSHOME 'powershell.exe'
        $info.Arguments = '-NoProfile -File "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -NoLaunch -PayloadDirectory "' + $payload + '"'
        $info.UseShellExecute = $false; $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $info.EnvironmentVariables['LOCALAPPDATA'] = $userRoot
        $child = [Diagnostics.Process]::Start($info)
        $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
        try {
            if (-not $child.WaitForExit(10000)) { throw 'Locked runtime repair child exceeded its ten-second limit' }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Locked runtime repair output did not settle' }
            if ($child.ExitCode -eq 0 -or -not $stderr.Result.Contains('Repair failed; original runtime restored.') -or $stdout.Result.Contains('Repair runtime restored and verified:')) {
                throw 'Locked runtime did not cause explicit failed repair with verified rollback'
            }
        } finally {
            try {
                if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned locked repair child cleanup failed' } }
                if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned locked repair output cleanup failed' }
            } finally { $child.Dispose() }
        }
        $rollbackAfter = Get-FixtureSnapshot
        if ($rollbackBefore.Count -ne $rollbackAfter.Count) { throw 'Rollback left new runtime or transaction files' }
        foreach ($path in $rollbackBefore.Keys) {
            if (-not $rollbackAfter.ContainsKey($path) -or $rollbackAfter[$path] -cne $rollbackBefore[$path]) { throw 'Rollback changed an original runtime or private fixture byte' }
        }
        Write-Host 'PASS: real sharing violation after first atomic runtime replacement restores every original runtime and private byte'
        Write-Host 'Repair runtime rollback tests PASS: 1'
    } finally { $held.Dispose() }


    # A native controlled negative image spawns one child and hangs. The real
    # successful case above used ProGo itself; this vector tests job cleanup and
    # rollback, not functional application acceptance.
    $hang = Join-Path $work 'hanging-check.exe'
    Add-Type -OutputAssembly $hang -OutputType ConsoleApplication -TypeDefinition @'
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
internal static class RepairCheckFixture {
    private static void Main(string[] args) {
        string root = Environment.GetEnvironmentVariable("PROGO_REPAIR_NATIVE_CHECK");
        bool child = args.Length > 0 && args[0] == "--owned-child";
        string marker = Path.Combine(root, child ? "child.pid" : "root.pid");
        File.WriteAllText(marker + ".new", Process.GetCurrentProcess().Id.ToString());
        File.Move(marker + ".new", marker);
        if (!child) Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, "--owned-child") { UseShellExecute = false, CreateNoWindow = true });
        Thread.Sleep(60000);
    }
}
'@
    [IO.File]::Copy($hang, (Join-Path $payload 'ProGo.exe'), $true)
    $markers = Join-Path $work 'check-markers'; [IO.Directory]::CreateDirectory($markers) | Out-Null
    $checkBefore = Get-FixtureSnapshot
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $PSHOME 'powershell.exe'
    $info.Arguments = '-NoProfile -File "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -NoLaunch -PayloadDirectory "' + $payload + '"'
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $info.EnvironmentVariables['LOCALAPPDATA'] = $userRoot
    $info.EnvironmentVariables['PROGO_REPAIR_NATIVE_CHECK'] = $markers
    $ownedChecks = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
    $knownChecks = @{}; $journalObserved = $false
    $child = [Diagnostics.Process]::Start($info)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    try {
        $deadline = [Diagnostics.Stopwatch]::StartNew()
        while (-not $child.WaitForExit(50)) {
            foreach ($name in @('root.pid', 'child.pid')) {
                $marker = Join-Path $markers $name
                if (-not $knownChecks.ContainsKey($name) -and [IO.File]::Exists($marker)) {
                    try {
                        $idText = [IO.File]::ReadAllText($marker)
                        $process = [Diagnostics.Process]::GetProcessById([int]$idText)
                        try {
                            if (-not [RepairFixtureImagePath]::Same($process.MainModule.FileName, (Join-Path $installed 'ProGo.exe'))) { throw 'Unexpected self-check fixture image' }
                            $ownedChecks.Add($process); $knownChecks[$name] = $true
                        } catch { $process.Dispose(); throw }
                    } catch [IO.IOException] { } catch [ArgumentException] { }
                }
            }
            if ($ownedChecks.Count -eq 2 -and -not $journalObserved) {
                $journal = [IO.File]::ReadAllText((Join-Path $installed '.repair-state.json')) | ConvertFrom-Json
                if ($journal.schema -ne 1 -or $journal.phase -cne 'applying' -or @($journal.entries).Count -ne 41 -or @($journal.applied).Count -ne 41 -or $journal.self_check_passed) { throw 'Native self-check started before complete durable runtime admission' }
                $journalObserved = $true
            }
            if ($deadline.ElapsedMilliseconds -gt 20000) { throw 'Owned repair self-check exceeded its bounded check and cleanup budget' }
        }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Timed-out check repair output did not settle' }
        if ($child.ExitCode -eq 0 -or -not $stderr.Result.Contains('Repair failed; original runtime restored.') -or $stdout.Result.Contains('Repair self-check PASS:')) {
            throw 'Unfinished native self-check was treated as a successful repair'
        }
        if ($ownedChecks.Count -ne 2 -or -not $journalObserved) { throw 'Owned self-check root, descendant and durable admission were not observed' }
        foreach ($process in $ownedChecks) { if (-not $process.HasExited) { throw 'Repair left a self-check process running' } }
        $checkAfter = Get-FixtureSnapshot
        if ($checkBefore.Count -ne $checkAfter.Count) { throw 'Failed self-check left installation or transaction residue' }
        foreach ($path in $checkBefore.Keys) { if (-not $checkAfter.ContainsKey($path) -or $checkAfter[$path] -cne $checkBefore[$path]) { throw 'Failed self-check changed original runtime or private bytes' } }
        Write-Host 'PASS: native self-check timeout kills only its contained root and child, refuses success and rolls runtime back'
        Write-Host 'Repair owned self-check tests PASS: 1'
    } finally {
        try {
            if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned repair parent cleanup failed' } }
            # Closing the terminated repair owner's kill-on-close job settles
            # only these previously observed fixture children.
            foreach ($process in $ownedChecks) { if (-not $process.WaitForExit(3000)) { throw 'Owned self-check job did not settle after parent exit' } }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned check output cleanup failed' }
        } finally { foreach ($process in $ownedChecks) { $process.Dispose() }; $child.Dispose() }
    }
    # Terminate only our repair parent while its two contained fixture images
    # are alive. Recovery must use durable snapshots, not the original payload.
    Get-ChildItem -LiteralPath $markers -Force | Remove-Item -Force
    $interruptedBefore = Get-FixtureSnapshot
    $ownedChecks = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
    $knownChecks = @{}
    $child = [Diagnostics.Process]::Start($info)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    try {
        $deadline = [Diagnostics.Stopwatch]::StartNew()
        while ($ownedChecks.Count -ne 2) {
            if ($child.WaitForExit(50)) { throw 'Repair exited before the owned hard-kill barrier' }
            foreach ($name in @('root.pid', 'child.pid')) {
                $marker = Join-Path $markers $name
                if (-not $knownChecks.ContainsKey($name) -and [IO.File]::Exists($marker)) {
                    try {
                        $process = [Diagnostics.Process]::GetProcessById([int][IO.File]::ReadAllText($marker))
                        try {
                            if (-not [RepairFixtureImagePath]::Same($process.MainModule.FileName, (Join-Path $installed 'ProGo.exe'))) { throw 'Unexpected interrupted self-check fixture image' }
                            $ownedChecks.Add($process); $knownChecks[$name] = $true
                        } catch { $process.Dispose(); throw }
                    } catch [IO.IOException] { } catch [ArgumentException] { }
                }
            }
            if ($deadline.ElapsedMilliseconds -gt 20000) { throw 'Owned hard-kill barrier was not reached within the existing check budget' }
        }
        $journalPath = Join-Path $installed '.repair-state.json'
        $journal = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json
        if ($journal.phase -cne 'applying' -or @($journal.applied).Count -ne 41 -or $journal.self_check_passed) { throw 'Hard-kill barrier has no unaccepted complete admission' }
        $child.Kill()
        if (-not $child.WaitForExit(3000)) { throw 'Owned hard-killed repair parent did not exit' }
        foreach ($process in $ownedChecks) { if (-not $process.WaitForExit(3000)) { throw 'Owner death did not terminate its contained check image' } }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Interrupted repair output did not settle' }
        $interrupted = Get-FixtureSnapshot
        if (-not [IO.File]::Exists($journalPath)) { throw 'Interrupted repair lost its durable journal' }
        Invoke-FixtureRepair -ExpectedFailure 'An unfinished repair journal exists.'
        Assert-FixtureSnapshot $interrupted 'Default repair repeated a pending transaction'

        # Malformed schema and a path outside the canonical runtime must fail
        # before touching any other byte, not silently discard recovery state.
        $journalBytes = [IO.File]::ReadAllBytes($journalPath)
        foreach ($bad in @('schema', 'path', 'boolean', 'prefix', 'phase')) {
            $invalid = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json
            $reason = 'Invalid repair recovery journal schema or phase.'
            switch ($bad) {
                'schema' { $invalid.schema = 2 }
                'path' { $invalid.entries[0].path = '..\settings.json'; $reason = 'Invalid repair recovery runtime entry.' }
                'boolean' { $invalid.self_check_passed = 'false' }
                'prefix' { $invalid.applied[1] = $invalid.applied[0]; $reason = 'Repair recovery admission is not a known runtime prefix.' }
                'phase' { $invalid.phase = 'prepared'; $reason = 'Inconsistent repair recovery phase.' }
            }
            [IO.File]::WriteAllText($journalPath, ($invalid | ConvertTo-Json -Depth 6 -Compress))
            $invalidBefore = Get-FixtureSnapshot
            Invoke-FixtureRepair -Recover -ExpectedFailure $reason
            Assert-FixtureSnapshot $invalidBefore 'Rejected recovery journal changed installation bytes'
            [IO.File]::WriteAllBytes($journalPath, $journalBytes)
        }
        $oldImage = Join-Path $installed ('.repair-stage-' + $journal.transaction_id + '\old\ProGo.exe')
        $oldBytes = [IO.File]::ReadAllBytes($oldImage)
        [IO.File]::WriteAllText($oldImage, 'damaged old snapshot fixture')
        $damagedBefore = Get-FixtureSnapshot
        Invoke-FixtureRepair -Recover -ExpectedFailure 'Repair recovery original snapshot is missing or changed.'
        Assert-FixtureSnapshot $damagedBefore 'Recovery used a damaged original snapshot'
        [IO.File]::WriteAllBytes($oldImage, $oldBytes)
        $version = Join-Path $installed 'VERSION'; $versionBytes = [IO.File]::ReadAllBytes($version)
        [IO.File]::WriteAllText($version, 'foreign runtime edit after interruption')
        $foreignBefore = Get-FixtureSnapshot
        Invoke-FixtureRepair -Recover -ExpectedFailure 'Repair recovery found a foreign runtime change; snapshots retained.'
        Assert-FixtureSnapshot $foreignBefore 'Recovery overwrote a foreign runtime change'
        [IO.File]::WriteAllBytes($version, $versionBytes)

        # Remove the original source entirely. Only verified stage copies may
        # restore the baseline, and recovery must not execute the hanging image.
        Get-ChildItem -LiteralPath $payload -Force | Remove-Item -Force -Recurse
        $markerBefore = @(Get-ChildItem -LiteralPath $markers -File).Count
        Invoke-FixtureRepair -Recover
        Assert-FixtureSnapshot $interruptedBefore 'Explicit interrupted repair recovery did not restore every original/private byte'
        if (@(Get-ChildItem -LiteralPath $markers -File).Count -ne $markerBefore) { throw 'Repair recovery repeated the self-check' }
        Invoke-FixtureRepair -Recover -ExpectedFailure 'Repair recovery journal is missing or too large.'
        Assert-FixtureSnapshot $interruptedBefore 'Repeated recovery changed the restored installation'
        Write-Host 'PASS: hard-killed repair owner contains its child tree; explicit snapshot recovery restores baseline without payload or repeated actions'
        Write-Host 'PASS: pending, malformed, traversal, foreign runtime and repeated recovery refuse without changing installation bytes'
        Write-Host 'Repair durable recovery tests PASS: 1; refusal cases: 9'
    } finally {
        try {
            if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned interrupted parent cleanup failed' } }
            foreach ($process in $ownedChecks) { if (-not $process.WaitForExit(3000)) { throw 'Owned interrupted job cleanup failed' } }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned interrupted output cleanup failed' }
        } finally { foreach ($process in $ownedChecks) { $process.Dispose() }; $child.Dispose() }
    }
    Write-Host ('Repair payload preflight tests PASS: ' + $passed)
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
