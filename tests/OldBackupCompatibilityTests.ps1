param([string]$Exe, [string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: old backup transaction fixture requires isolated Windows CI'; return }
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
$work = Join-Path ([IO.Path]::GetTempPath()) ('progo-old-backup-' + [guid]::NewGuid().ToString('N'))
$install = Join-Path $work 'ProGo'; $backup = Join-Path $work 'backup'
$fixtureScripts = Join-Path $PSScriptRoot 'fixtures\old-launchers'
$driver = Join-Path $PSScriptRoot 'MaintenanceTestDriver.ps1'
$ps = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
function Check-Old($condition, $message) { if (-not $condition) { throw $message } }
function Snapshot-Old($path) {
    return (@(Get-ChildItem -LiteralPath $path -Recurse -File | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($path.Length) + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash
    }) -join "`n")
}
function Run-OldRestore($mode) {
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $ps; $info.Arguments = "-NoProfile -NonInteractive -File `"$driver`" -Mode $mode -Scripts `"$Scripts`" -Fixture `"$work`" -Release `"$(Split-Path -Parent $Exe)`""
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $child = [Diagnostics.Process]::Start($info)
    try { if (-not $child.WaitForExit(30000)) { $child.Kill(); $child.WaitForExit(); throw 'Old backup fixture timed out' }; return $child.ExitCode }
    finally { $child.Dispose() }
}
try {
    [void][IO.Directory]::CreateDirectory($install); [void][IO.Directory]::CreateDirectory($backup)
    foreach ($root in @($install, $backup)) {
        Copy-Item -LiteralPath $Exe -Destination (Join-Path $root 'ProGo.exe')
        Copy-Item -LiteralPath $Scripts -Destination (Join-Path $root 'scripts') -Recurse
        [IO.File]::WriteAllText((Join-Path $root 'VERSION'), '0.0.1')
        [IO.File]::WriteAllText((Join-Path $root 'settings.json'), '{"fixture":true}')
    }
    [IO.File]::WriteAllText((Join-Path $install 'VERSION'), '0.1.0')
    foreach ($name in @('Start-ProGo.ps1','Show-ProGo.ps1')) {
        Copy-Item -LiteralPath (Join-Path $fixtureScripts $name) -Destination (Join-Path $backup "scripts\$name") -Force
    }
    Remove-Item -LiteralPath (Join-Path $backup 'scripts\Layout-ProGo.ps1') -Force
    [IO.File]::WriteAllText((Join-Path $backup 'manifest.txt'), "product=ProGo`nversion=0.0.1`n")
    [ProGo.BackupIntegrity]::Write($backup); [ProGo.BackupIntegrity]::Validate($backup)
    $backupBefore = Snapshot-Old $backup
    $installedBefore = Snapshot-Old (Join-Path $install 'scripts')
    $beforeExe = (Get-FileHash -LiteralPath (Join-Path $install 'ProGo.exe')).Hash
    $beforeVersion = [IO.File]::ReadAllText((Join-Path $install 'VERSION'))
    $beforeSettings = [IO.File]::ReadAllText((Join-Path $install 'settings.json'))
    Check-Old ((Run-OldRestore 'restore-old-commit-fail') -ne 0) 'injected restore failure did not fail'
    Check-Old ([IO.File]::ReadAllText((Join-Path $work 'old-restore-injected')) -ceq 'move-4-version') 'fourth move injection not reached'
    Check-Old ([IO.File]::ReadAllText((Join-Path $work 'old-restore-undo-entered')) -ceq 'entered') 'real undo was not entered'
    Check-Old ([IO.File]::ReadAllText((Join-Path $work 'old-restore-undo-verified')) -ceq 'verified') 'real undo did not complete'
    Check-Old ([IO.File]::ReadAllText((Join-Path $install 'VERSION')) -ceq '0.1.0') 'original version was not restored'
    Check-Old ((Snapshot-Old (Join-Path $install 'scripts')) -ceq $installedBefore -and
        (Get-FileHash -LiteralPath (Join-Path $install 'ProGo.exe')).Hash -ceq $beforeExe -and
        [IO.File]::ReadAllText((Join-Path $install 'VERSION')) -ceq $beforeVersion -and
        [IO.File]::ReadAllText((Join-Path $install 'settings.json')) -ceq $beforeSettings) 'failed restore did not preserve selected runtime and data'
    Check-Old ((Snapshot-Old $backup) -ceq $backupBefore) 'failed restore changed old backup'
    Check-Old ((Run-OldRestore 'restore-old-backup') -eq 0) 'old backup restore failed'
    Check-Old ((Snapshot-Old $backup) -ceq $backupBefore) 'restore changed old hashed backup'
    Check-Old (-not [IO.File]::Exists((Join-Path $install 'scripts\Layout-ProGo.ps1'))) 'helper was inserted into old composition'
    Check-Old ((Snapshot-Old (Join-Path $install 'scripts')) -ceq (Snapshot-Old (Join-Path $backup 'scripts'))) 'restored scripts differ from old backup'
    # Execute the actual restored old entrypoints, but replace only process creation.
    $oldLocal = $env:LOCALAPPDATA; $env:LOCALAPPDATA = $work
    $launches = New-Object System.Collections.ArrayList
    Set-Item -Path Function:Start-Process -Value ({
        param($FilePath, $WorkingDirectory, $ArgumentList)
        [void]$launches.Add(@($FilePath, $WorkingDirectory, $ArgumentList))
    }.GetNewClosure())
    try {
        & (Join-Path $install 'scripts\Start-ProGo.ps1')
        & (Join-Path $install 'scripts\Show-ProGo.ps1')
        Check-Old ($launches.Count -eq 2) 'old entrypoints did not request both launches'
        foreach ($launch in $launches) { Check-Old ($launch[0] -ceq (Join-Path $install 'ProGo.exe') -and $launch[1] -ceq $install) 'old launcher escaped fixture' }
        Check-Old ($launches[1][2] -ceq '--show') 'old show argument missing'
    } finally { $env:LOCALAPPDATA = $oldLocal; Remove-Item Function:Start-Process }
    Write-Host 'PASS: old backup restore, rollback and launchers'
} finally { if ([IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force } }
