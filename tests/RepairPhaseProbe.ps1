param([string]$RepairScript, [string]$Payload, [string]$FixtureRoot, [switch]$CommittedProbe)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Repair phase probe requires isolated CI.' }
$fixture = [IO.Path]::GetFullPath($FixtureRoot).TrimEnd([char]'\')
$tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd([char]'\')
if ([IO.Path]::GetDirectoryName($fixture) -ine $tempRoot -or [IO.Path]::GetFileName($fixture) -cnotmatch '^ProGo-repair-preflight-[a-f0-9]{32}$' -or
    -not [IO.Directory]::Exists($fixture) -or ((Get-Item -LiteralPath $fixture -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Repair probe fixture scope is invalid.' }
if ([IO.Path]::GetFileName($RepairScript) -cne 'Repair-ProGo.ps1' -or -not [IO.File]::Exists($RepairScript) -or
    [IO.Path]::GetFullPath($Payload) -ine (Join-Path $fixture 'payload')) { throw 'Repair probe input scope is invalid.' }
$trace = Join-Path $fixture $(if ($CommittedProbe) { 'committed-repair.phases' } else { 'complete-repair.phases' })
$stream = New-Object IO.FileStream -ArgumentList @($trace, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
$clock = [Diagnostics.Stopwatch]::StartNew(); $probe = @{ Failed = $false }
$writePhase = {
    param([string]$Name)
    try {
        $bytes = [Text.Encoding]::ASCII.GetBytes($Name + ':' + $clock.ElapsedMilliseconds + "`n")
        $stream.Write($bytes, 0, $bytes.Length); $stream.Flush()
    } catch { $probe.Failed = $true }
}.GetNewClosure()
$breakpoints = New-Object 'System.Collections.Generic.List[object]'
try {
    & $writePhase 'wrapper-enter'
    if ($probe.Failed) { throw 'Repair probe could not record its entry.' }
    # Constant safe phase names, exact unique original source lines. No source rewrite,
    # stepping, prompt, runtime variable/journal/credential output or wait is introduced.
    $points = @(
        @{ Name = 'script-enter'; Text = '$InstallDir = $layout.RuntimeRoot' },
        @{ Name = 'preflight-complete'; Text = '$repairPhase = ''maintenance-load''; $selfCheckExit = -1' },
        @{ Name = 'helper-loaded'; Text = '$repairPhase = ''maintenance-gate''' },
        @{ Name = 'maintenance-acquired'; Text = 'Assert-RepairTarget ''ProGo.exe''' },
        @{ Name = 'snapshot-enter'; Text = '$repairPhase = ''snapshot''' },
        @{ Name = 'snapshot-complete'; Text = '$repairPhase = ''runtime-copy''' },
        @{ Name = 'copy-complete'; Text = '$repairPhase = ''runtime-verify''' },
        @{ Name = 'runtime-verified'; Text = '$check = New-Object Diagnostics.ProcessStartInfo' },
        @{ Name = 'selfcheck-call'; Text = '$selfCheckExit = [ProGo.MaintenanceOperation]::RunOwnedSelfCheck($check, 8000)' },
        @{ Name = 'selfcheck-return'; Text = 'if ($selfCheckExit -ne 0) { throw ''Installed repair self-check failed.'' }' },
        @{ Name = 'selfcheck-verified'; Text = 'Write-Host ''Repair self-check PASS: installed image, isolated local data root''' },
        @{ Name = 'commit-enter'; Text = '$repairPhase = ''commit''' },
        @{ Name = 'commit-durable'; Text = '$committed = $true; $journalSettled = $true' },
        @{ Name = 'catch-enter'; Text = '$failure = $_.Exception' },
        @{ Name = 'rollback-return'; Text = '$rolledBack = $conflicts.Count -eq 0' },
        @{ Name = 'transaction-cleanup-enter'; Text = 'if ($journalSettled -and ($committed -or $rolledBack)) {' },
        @{ Name = 'cleanup-return'; Text = '$exe = Join-Path $InstallDir ''ProGo.exe''' }
    )
    $lines = [IO.File]::ReadAllLines($RepairScript)
    foreach ($point in $points) {
        $anchorLines = @(); for ($index = 0; $index -lt $lines.Length; $index++) { if ($lines[$index].Trim() -ceq $point.Text) { $anchorLines += ($index + 1) } }
        if ($anchorLines.Count -ne 1) { throw 'Repair probe source anchor is ambiguous.' }
        $name = $point.Name
        $action = { & $writePhase $name }.GetNewClosure()
        $breakpoints.Add((Set-PSBreakpoint -Script $RepairScript -Line $anchorLines[0] -Action $action))
    }
    & $writePhase 'probe-ready'
    & $RepairScript -NoLaunch -PayloadDirectory $Payload
    & $writePhase 'repair-return'
    if ($probe.Failed) { throw 'Repair probe evidence is incomplete.' }
} catch {
    & $writePhase 'wrapper-error'
    $errorObject = $_.Exception.GetBaseException()
    $frames = @(New-Object Diagnostics.StackTrace -ArgumentList @($errorObject, $false))
    $methods = @($frames[0].GetFrames() | Select-Object -First 6 | ForEach-Object {
        $method = $_.GetMethod(); if ($null -ne $method -and $null -ne $method.DeclaringType) { (($method.DeclaringType.Name + '.' + $method.Name) -replace '[^A-Za-z0-9_.<>]', '') }
    })
    [Console]::WriteLine('REPAIR_PROBE_STACK ' + ($methods -join ','))
    throw
} finally {
    & $writePhase 'probe-cleanup-enter'
    try { foreach ($breakpoint in $breakpoints) { Remove-PSBreakpoint -Breakpoint $breakpoint } }
    finally { & $writePhase 'probe-cleanup-return'; $stream.Dispose() }
}
