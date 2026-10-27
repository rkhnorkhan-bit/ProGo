param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: separate-root repair requires isolated Windows CI'; return }
$root = Join-Path $env:TEMP ('ProGo-split-repair-' + [guid]::NewGuid().ToString('N'))
$driver = Join-Path $PSScriptRoot 'RepairSplitDriver.ps1'
$payload = Join-Path (Split-Path -Parent $Scripts) 'release'
$dataBase = Join-Path $root 'data'; $programBase = Join-Path $root 'programs'
$data = Join-Path $dataBase 'ProGo'; $runtime = Join-Path $programBase 'ProGo'
$foreign = Join-Path $root 'foreign'
$script:repairUnsettled = $false
function Run-Repair([string]$Mode) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $info.Arguments = '-NoProfile -NonInteractive -File "' + $driver + '" -Scripts "' + $Scripts + '" -Fixture "' + $root + '" -Payload "' + $payload + '" -Mode ' + $Mode
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
    $child = [Diagnostics.Process]::Start($info)
    $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
    $clock = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $child.WaitForExit(30000)) {
            $child.Kill()
            if (-not $child.WaitForExit([Math]::Max(0, 35000 - [int]$clock.ElapsedMilliseconds))) { $script:repairUnsettled = $true; throw 'Split repair child cleanup did not settle.' }
            throw 'Split repair timed out.'
        }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout,$stderr), [Math]::Max(0, [Math]::Min(5000, 35000 - [int]$clock.ElapsedMilliseconds)))) { $script:repairUnsettled = $true; throw 'Split repair output did not drain.' }
        return [pscustomobject]@{ ExitCode = $child.ExitCode; Output = $stdout.Result; ErrorText = $stderr.Result }
    } finally {
        try {
            if (-not $child.HasExited) {
                $child.Kill()
                if (-not $child.WaitForExit([Math]::Max(0, 35000 - [int]$clock.ElapsedMilliseconds))) { $script:repairUnsettled = $true; throw 'Split repair child cleanup did not settle.' }
            }
            if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout,$stderr), [Math]::Max(0, 35000 - [int]$clock.ElapsedMilliseconds))) { $script:repairUnsettled = $true; throw 'Split repair output cleanup did not drain.' }
        } finally { $child.Dispose() }
    }
}
function Check-Private {
    foreach ($name in @('settings.json','vault.enc.json','keep.txt')) {
        if ([IO.File]::ReadAllText((Join-Path $data $name)) -cne ('private-' + $name)) { throw ('Private data changed: ' + $name) }
    }
    if ([IO.File]::ReadAllText((Join-Path $foreign 'keep.txt')) -cne 'foreign') { throw 'Foreign sentinel changed.' }
}
try {
    foreach ($dir in @($data, $programBase, $foreign)) { [void][IO.Directory]::CreateDirectory($dir) }
    foreach ($name in @('settings.json','vault.enc.json','keep.txt')) { [IO.File]::WriteAllText((Join-Path $data $name), ('private-' + $name)) }
    [IO.File]::WriteAllText((Join-Path $foreign 'keep.txt'), 'foreign')
    $result = Run-Repair 'missing'
    if ($result.ExitCode -ne 0 -or -not $result.Output.Contains('Repair self-check PASS:') -or
        (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash -cne (Get-FileHash (Join-Path $payload 'ProGo.exe')).Hash) { throw ('Missing-root repair failed: ' + $result.ErrorText) }
    Check-Private
    [IO.File]::WriteAllText((Join-Path $runtime 'ProGo.exe'), 'corrupt fixture')
    $result = Run-Repair 'corrupt'
    if ($result.ExitCode -ne 0 -or (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash -cne (Get-FileHash (Join-Path $payload 'ProGo.exe')).Hash) { throw ('Corrupt executable repair failed: ' + $result.ErrorText) }
    Check-Private
    [IO.File]::WriteAllText((Join-Path $runtime 'ProGo.exe'), 'old damaged image')
    $before = (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash
    $result = Run-Repair 'fault'
    if ($result.ExitCode -eq 0 -or -not [IO.File]::Exists((Join-Path $root 'fault-injected')) -or
        -not $result.ErrorText.Contains('original runtime restored') -or (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash -cne $before) { throw 'Post-write failure did not restore the original EXE.' }
    Check-Private
    $marker = Join-Path $data 'system-proxy-backup.json'; [IO.File]::WriteAllText($marker, 'pending')
    $result = Run-Repair 'proxy'
    if ($result.ExitCode -eq 0 -or -not $result.ErrorText.Contains('Cause: InvalidOperationException; stage=maintenance-gate;') -or (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash -cne $before -or [IO.File]::ReadAllText($marker) -cne 'pending') { throw 'Data proxy refusal failed.' }
    [IO.File]::Delete($marker); Check-Private
    $result = Run-Repair 'foreign'
    if ($result.ExitCode -eq 0 -or -not $result.ErrorText.Contains('foreign or unconfirmed') -or (Get-FileHash (Join-Path $runtime 'ProGo.exe')).Hash -cne $before) { throw 'Foreign runtime was not refused before mutation.' }
    Check-Private
    Remove-Item -LiteralPath $runtime -Recurse -Force
    & $env:ComSpec /d /c mklink /J "$runtime" "$foreign" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create fixture junction.' }
    try { $result = Run-Repair 'junction'; if ($result.ExitCode -eq 0 -or -not $result.ErrorText.Contains('Recovery layout refuses a reparse point.')) { throw 'Redirected runtime refusal reason mismatch.' }; Check-Private }
    finally { [IO.Directory]::Delete($runtime) }
    [IO.File]::WriteAllText($runtime, 'conflicting file')
    $result = Run-Repair 'conflict'
    if ($result.ExitCode -eq 0 -or -not $result.ErrorText.Contains('Recovery layout requires directories.') -or [IO.File]::ReadAllText($runtime) -cne 'conflicting file') { throw 'Conflicting runtime root was accepted.' }
    Check-Private
    Write-Host 'PASS: synthetic separate-root actual repair and rollback'
} finally { if (-not $script:repairUnsettled -and [IO.Directory]::Exists($root)) { Remove-Item -LiteralPath $root -Recurse -Force } }
