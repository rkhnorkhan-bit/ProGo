param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: split restore requires isolated Windows CI'; return }
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
$driver = Join-Path $PSScriptRoot 'RestoreSplitDriver.ps1'
function Snapshot-Split($Root, $Names) {
    return (@(foreach ($name in $Names) {
        $path = Join-Path $Root $name
        if (-not (Test-Path -LiteralPath $path)) { $name + ':absent'; continue }
        foreach ($item in @(Get-Item -LiteralPath $path) + @(if ([IO.Directory]::Exists($path)) { Get-ChildItem -LiteralPath $path -Recurse -Force })) {
            if ($item.PSIsContainer) { $item.FullName.Substring($Root.Length) + ':directory' }
            else { $item.FullName.Substring($Root.Length) + ':' + (Get-FileHash -LiteralPath $item.FullName).Hash }
        }
    }) | Sort-Object) -join "`n"
}
foreach ($mode in @('program','missing','fault-program','fault-missing','all','data','fault-all','no-consent','selfcheck-live')) {
    $work = Join-Path $env:TEMP ('ProGo-split-restore-' + [guid]::NewGuid().ToString('N'))
    $data = Join-Path $work 'data\ProGo'; $runtime = Join-Path $work 'programs\ProGo'; $backup = Join-Path $work 'backup'
    $settled = $true; $child = $null
    try {
        foreach ($root in @($runtime, $backup)) {
            [void][IO.Directory]::CreateDirectory((Join-Path $root 'scripts'))
            [IO.File]::WriteAllText((Join-Path $root 'ProGo.exe'), $(if ($root -ceq $backup) { 'backup-exe' } else { 'original-exe' }))
            [IO.File]::WriteAllText((Join-Path $root 'VERSION'), $(if ($root -ceq $backup) { '0.0.1' } else { '0.1.0' }))
            foreach ($name in @('Start-ProGo.ps1','Restore-ProGoBackup.ps1','Update-ProGo.Core.ps1','old-composition.txt')) { [IO.File]::WriteAllText((Join-Path $root "scripts\$name"), ($root + $name)) }
        }
        if ($mode -eq 'selfcheck-live') {
            $nativeImage = Join-Path $backup 'ProGo.exe'; [IO.File]::Delete($nativeImage)
            Add-Type -TypeDefinition 'public static class RestoreLiveCheckFixture { public static void Main(string[] args) { System.IO.File.WriteAllText(args[0], "ready"); System.Threading.Thread.Sleep(30000); } }' -OutputAssembly $nativeImage -OutputType ConsoleApplication
        }
        [void][IO.Directory]::CreateDirectory((Join-Path $data 'backups'))
        foreach ($pair in @(@('settings.json','original-settings'),@('vault.enc.json','original-vault'),@('progo.log','log-sentinel'),@('backups\keep.txt','backup-sentinel'))) { [IO.File]::WriteAllText((Join-Path $data $pair[0]), $pair[1]) }
        [IO.File]::WriteAllText((Join-Path $runtime 'settings.json'), 'foreign-runtime-settings')
        [IO.File]::WriteAllText((Join-Path $runtime 'foreign.txt'), 'foreign-runtime-file')
        [IO.File]::WriteAllText((Join-Path $backup 'settings.json'), 'backup-settings')
        [IO.File]::WriteAllText((Join-Path $backup 'vault.enc.json'), 'backup-vault')
        [IO.File]::WriteAllText((Join-Path $backup 'manifest.txt'), "product=ProGo`nversion=0.0.1")
        [ProGo.BackupIntegrity]::Write($backup); [ProGo.BackupIntegrity]::Validate($backup)
        if ($mode -in @('missing','fault-missing')) { [IO.File]::Delete((Join-Path $runtime 'ProGo.exe')) }
        $programNames = @('ProGo.exe','VERSION','scripts')
        $privateNames = @('settings.json','vault.enc.json','progo.log','backups\keep.txt')
        $oldProgram = Snapshot-Split $runtime $programNames; $oldData = Snapshot-Split $data $privateNames
        $backupState = Snapshot-Split $backup @('ProGo.exe','VERSION','scripts','settings.json','vault.enc.json','manifest.txt','backup-files.sha256')
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $info.Arguments = '-NoProfile -NonInteractive -File "' + $driver + '" -Scripts "' + $Scripts + '" -Fixture "' + $work + '" -Mode ' + $mode
        $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $clock = [Diagnostics.Stopwatch]::StartNew()
        $child = [Diagnostics.Process]::Start($info)
        $stdout = $child.StandardOutput.ReadToEndAsync(); $stderr = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(30000)) { $child.Kill(); $settled = $child.WaitForExit([Math]::Max(0, 35000 - [int]$clock.ElapsedMilliseconds)); throw 'Split restore deadline exceeded.' }
        if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout,$stderr), [Math]::Max(0, [Math]::Min(5000, 35000 - [int]$clock.ElapsedMilliseconds)))) { $settled = $false; throw 'Split restore output drain failed.' }
        $errorText = $stderr.Result
        if ($mode -like 'fault-*') {
            if ($child.ExitCode -eq 0 -or -not $errorText.Contains('Synthetic split restore post-mutation fault') -or
                [IO.File]::ReadAllText((Join-Path $work 'injected')) -cne $mode -or [IO.File]::ReadAllText((Join-Path $work 'undo-completed')) -cne 'verified' -or
                (Snapshot-Split $runtime $programNames) -cne $oldProgram -or (Snapshot-Split $data $privateNames) -cne $oldData) { throw 'Actual split-root Undo not proven.' }
        } elseif ($mode -eq 'selfcheck-live') {
            $retained = @(Get-ChildItem -LiteralPath $runtime -Directory -Filter 'restore-txn-*')
            if ($child.ExitCode -eq 0 -or -not $errorText.Contains('Synthetic self-check timeout with cleanup refusal') -or
                [IO.File]::ReadAllText((Join-Path $work 'live-guard-verified')) -cne 'exact-runtime-before-undo' -or
                [IO.File]::ReadAllText((Join-Path $work 'live-child-settled')) -cne 'settled' -or
                [IO.File]::Exists((Join-Path $work 'undo-entered')) -or [IO.File]::Exists((Join-Path $work 'unexpected-relaunch')) -or
                $retained.Count -ne 1 -or (Snapshot-Split (Join-Path $retained[0].FullName 'previous') $programNames) -cne $oldProgram -or
                (Snapshot-Split $runtime $programNames) -cne (Snapshot-Split $backup $programNames) -or
                (Snapshot-Split $data $privateNames) -cne $oldData) { throw 'Live exact-runtime child did not block Undo/relaunch and retain evidence.' }
        } elseif ($mode -eq 'no-consent') {
            if ($child.ExitCode -eq 0 -or -not $errorText.Contains('SPLIT_RESTORE_CONSENT_REFUSED') -or (Snapshot-Split $runtime $programNames) -cne $oldProgram -or (Snapshot-Split $data $privateNames) -cne $oldData -or [IO.File]::Exists((Join-Path $work 'selfcheck-stub'))) { throw 'Data consent refusal failed.' }
        } else {
            if ($child.ExitCode -ne 0 -or -not $stdout.Result.Contains('ProGo restore completed.') -or [IO.File]::ReadAllText((Join-Path $work 'selfcheck-stub')) -cne 'isolated') { throw ('Split restore failed: ' + $mode + '; ' + $errorText) }
            if ($mode -ne 'data' -and (Snapshot-Split $runtime $programNames) -cne (Snapshot-Split $backup $programNames)) { throw 'Restored runtime composition mismatch.' }
            if ($mode -eq 'data' -and (Snapshot-Split $runtime $programNames) -cne $oldProgram) { throw 'Data-only restore changed runtime.' }
            if ($mode -in @('all','data')) {
                if ([IO.File]::ReadAllText((Join-Path $data 'settings.json')) -cne 'backup-settings' -or [IO.File]::ReadAllText((Join-Path $data 'vault.enc.json')) -cne 'backup-vault') { throw 'Opted-in data restore missed DataRoot.' }
            } elseif ((Snapshot-Split $data $privateNames) -cne $oldData) { throw 'Program-only restore changed private data.' }
        }
        if ([IO.File]::ReadAllText((Join-Path $runtime 'settings.json')) -cne 'foreign-runtime-settings' -or [IO.File]::ReadAllText((Join-Path $runtime 'foreign.txt')) -cne 'foreign-runtime-file' -or
            [IO.File]::ReadAllText((Join-Path $data 'progo.log')) -cne 'log-sentinel' -or [IO.File]::ReadAllText((Join-Path $data 'backups\keep.txt')) -cne 'backup-sentinel') { throw 'Foreign/private sentinel changed.' }
        if ((Snapshot-Split $backup @('ProGo.exe','VERSION','scripts','settings.json','vault.enc.json','manifest.txt','backup-files.sha256')) -cne $backupState) { throw 'Source backup changed.' }
        Write-Host ('PASS: actual split restore ' + $mode)
    } finally {
        if ($null -ne $child) {
            try {
                if (-not $child.HasExited) { $child.Kill(); $settled = $child.WaitForExit([Math]::Max(0, 35000 - [int]$clock.ElapsedMilliseconds)); if (-not $settled) { throw 'Split restore child cleanup unconfirmed.' } }
                if (-not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout,$stderr), [Math]::Max(0, 35000 - [int]$clock.ElapsedMilliseconds))) { $settled = $false; throw 'Split restore output cleanup did not drain.' }
            }
            finally { $child.Dispose() }
        }
        if ($settled -and [IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force }
    }
}
