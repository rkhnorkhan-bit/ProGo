param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: repair payload tests require isolated Windows CI'; return }
$release = Join-Path (Split-Path -Parent $Scripts) 'release'
$work = Join-Path $env:TEMP ('ProGo-repair-preflight-' + [guid]::NewGuid().ToString('N'))
$payload = Join-Path $work 'payload'
$userRoot = Join-Path $work 'local-app-data'
$installed = Join-Path $userRoot 'ProGo'
function Get-FixtureSnapshot {
    $result = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $userRoot -File -Recurse)) {
        $result[$file.FullName.Substring($userRoot.Length)] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
    return $result
}
function Assert-FixtureSnapshot($Expected, [string]$Message) {
    $actual = Get-FixtureSnapshot
    if ($Expected.Count -ne $actual.Count) { throw $Message }
    foreach ($path in $Expected.Keys) { if (-not $actual.ContainsKey($path) -or $actual[$path] -cne $Expected[$path]) { throw $Message } }
}
function Invoke-FixtureRepair([switch]$Recover, [string]$ExpectedFailure) {
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
        } elseif ($child.ExitCode -ne 0 -or -not $stdout.Result.Contains('Repair recovery complete: applying; no application launched')) { throw 'Explicit repair recovery did not complete' }
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
    $sourceFiles = @(Get-ChildItem -LiteralPath $payload -File -Recurse | Where-Object {
        $documents -cnotcontains $_.FullName.Substring($payload.Length + 1)
    })
    if ($sourceFiles.Count -ne 41) { throw 'Built runtime changed: repair fixture must explicitly cover every mandatory leaf' }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $PSHOME 'powershell.exe'
    $info.Arguments = '-NoProfile -File "' + (Join-Path $Scripts 'Repair-ProGo.ps1') + '" -NoLaunch -PayloadDirectory "' + $payload + '"'
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $info.EnvironmentVariables['LOCALAPPDATA'] = $userRoot
    $child = [Diagnostics.Process]::Start($info)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    try {
        if (-not $child.WaitForExit(20000)) { throw 'Complete repair with bounded self-check exceeded its twenty-second limit' }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Complete repair output did not settle' }
        if ($child.ExitCode -ne 0 -or -not $stdout.Result.Contains('Repair runtime restored and verified:') -or -not $stdout.Result.Contains('Repair self-check PASS:')) {
            $safe = [regex]::Match($stderr.Result, 'Cause: [A-Za-z0-9]+; stage=[a-z-]+; hresult=-?\d+; native=-?\d+; check_exit=-?\d+')
            $evidence = if ($safe.Success) { $safe.Value } else { 'no classified repair failure' }
            throw ('Complete repair runtime copy failed; child_exit=' + $child.ExitCode + '; ' + $evidence)
        }
    } finally {
        try {
            if (-not $child.HasExited) { $child.Kill(); if (-not $child.WaitForExit(3000)) { throw 'Owned complete repair child cleanup failed' } }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), 3000)) { throw 'Owned complete repair output cleanup failed' }
        } finally { $child.Dispose() }
    }
    foreach ($source in $sourceFiles) {
        $relative = $source.FullName.Substring($payload.Length + 1)
        $target = Join-Path $installed $relative
        if (-not (Test-Path -LiteralPath $target -PathType Leaf) -or (Get-FileHash -LiteralPath $target).Hash -cne (Get-FileHash -LiteralPath $source.FullName).Hash) {
            throw ('Complete repair omitted or mixed runtime bytes: ' + $relative)
        }
    }
    $after = Get-FixtureSnapshot
    foreach ($path in $before.Keys) { if (-not $after.ContainsKey($path) -or $after[$path] -cne $before[$path]) { throw 'Complete runtime repair changed private fixture bytes' } }
    Write-Host 'PASS: complete prepared runtime copied byte-for-byte while private settings, vault and backup bytes remain unchanged'
    Write-Host 'Repair runtime copy tests PASS: 1'
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
                        if ($process.MainModule.FileName -ine (Join-Path $installed 'ProGo.exe')) { $process.Dispose(); throw 'Unexpected self-check fixture image' }
                        $ownedChecks.Add($process); $knownChecks[$name] = $true
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
                        if ($process.MainModule.FileName -ine (Join-Path $installed 'ProGo.exe')) { $process.Dispose(); throw 'Unexpected interrupted self-check fixture image' }
                        $ownedChecks.Add($process); $knownChecks[$name] = $true
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
