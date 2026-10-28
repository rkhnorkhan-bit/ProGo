param([string]$Scripts)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { Write-Host 'SKIP: split restore requires isolated Windows CI'; return }
. (Join-Path $Scripts 'BackupIntegrity-ProGo.ps1')
$driver = Join-Path $PSScriptRoot 'RestoreSplitDriver.ps1'
function Snapshot-SplitChildren($Path, $Prefix) {
    foreach ($item in @(Get-ChildItem -LiteralPath $Path -Force)) {
        $relative = $Prefix + '\' + $item.Name
        if ($item.PSIsContainer) {
            $relative + ':directory'
            Snapshot-SplitChildren $item.FullName $relative
        } else { $relative + ':' + (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash }
    }
}
function Snapshot-Split($Root, $Names) {
    return (@(foreach ($name in $Names) {
        $path = Join-Path $Root $name
        if (-not (Test-Path -LiteralPath $path)) { $name + ':absent'; continue }
        $item = Get-Item -LiteralPath $path -Force
        if ($item.PSIsContainer) {
            $name + ':directory'
            Snapshot-SplitChildren $path $name
        } else { $name + ':' + (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    }) | Sort-Object) -join "`n"
}
function Write-SplitSnapshotDifference($Expected, $Actual, $ExpectedRoot, $ActualRoot) {
    foreach ($pair in @(@('expected', $ExpectedRoot), @('actual', $ActualRoot))) {
        $resolved = (Get-Item -LiteralPath $pair[1] -Force).FullName
        Write-Host ('SPLIT_SNAPSHOT_ROOT side={0}; rawLength={1}; resolvedLength={2}; lengthMismatch={3}' -f $pair[0], $pair[1].Length, $resolved.Length, ($pair[1].Length -ne $resolved.Length))
    }
    # Generated fixture relative keys/types/digests only; never file contents.
    $differences = @(Compare-Object -ReferenceObject @($Expected -split "`n") -DifferenceObject @($Actual -split "`n") -CaseSensitive)
    foreach ($difference in @($differences | Select-Object -First 12)) {
        $record = [string]$difference.InputObject
        $separator = $record.LastIndexOf(':')
        $key = $record.Substring(0, $separator)
        $value = $record.Substring($separator + 1)
        $type = if ($value -in @('directory','absent')) { $value } else { 'file' }
        $hash = if ($type -eq 'file') { $value } else { '-' }
        Write-Host ('SPLIT_SNAPSHOT_DIFF side={0}; key={1}; type={2}; sha256={3}' -f $difference.SideIndicator, $key, $type, $hash)
    }
    Write-Host ('SPLIT_SNAPSHOT_DIFF total={0}; shown={1}; <=expected; =>actual' -f $differences.Count, [Math]::Min(12, $differences.Count))
}
# Exercise actual Windows short/long aliases without changing volume policy.
Add-Type -TypeDefinition 'using System.Text; using System.Runtime.InteropServices; public static class RestoreSplitAliasFixture { [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern uint GetShortPathName(string path, StringBuilder result, uint capacity); }'
$aliasWork = Join-Path $env:TEMP ('ProGo-split-alias-' + [guid]::NewGuid().ToString('N'))
try {
    [void][IO.Directory]::CreateDirectory((Join-Path $aliasWork 'nested\empty'))
    [IO.File]::WriteAllText((Join-Path $aliasWork 'nested\probe.txt'), 'generated-alias-sentinel')
    $longRoot = (Get-Item -LiteralPath $aliasWork -Force).FullName
    $shortBuffer = New-Object Text.StringBuilder 32768
    $shortLength = [RestoreSplitAliasFixture]::GetShortPathName($longRoot, $shortBuffer, 32768)
    if ($shortLength -eq 0) { throw ('Short-path fixture lookup failed; win32=' + [Runtime.InteropServices.Marshal]::GetLastWin32Error()) }
    if ($shortLength -ge 32768) { throw 'Short-path fixture exceeded bounded buffer.' }
    $shortRoot = $shortBuffer.ToString()
    if ($shortRoot -ieq $longRoot) { Write-Host 'SKIP: split snapshot real short/long alias unavailable; alias regression NOT_RUN' }
    else {
        $names = @('nested','absent.txt')
        $digest = (Get-FileHash -LiteralPath (Join-Path $longRoot 'nested\probe.txt') -Algorithm SHA256).Hash
        $expectedAlias = (@('nested:directory','nested\empty:directory',('nested\probe.txt:' + $digest),'absent.txt:absent') | Sort-Object) -join "`n"
        $longState = Snapshot-Split $longRoot $names
        $shortState = Snapshot-Split $shortRoot $names
        if ($longState -cne $expectedAlias -or $shortState -cne $expectedAlias) {
            Write-SplitSnapshotDifference $expectedAlias $shortState $longRoot $shortRoot
            throw 'Real Windows alias snapshot regression failed.'
        }
        # Evidence for the old substring assumption, not a claim about CI744.
        $rawLeaf = Join-Path $shortRoot 'nested'
        $resolvedLeaf = (Get-Item -LiteralPath $rawLeaf -Force).FullName
        Write-Host ('SPLIT_ALIAS_EVIDENCE rawRootLength={0}; resolvedRootLength={1}; oldRelativeMatches={2}' -f $shortRoot.Length, $longRoot.Length, ($resolvedLeaf.Substring($shortRoot.Length) -ceq '\nested'))
        Write-Host 'PASS: split snapshot real Windows short/long alias; exact directory/absent/SHA256 records'
    }
} finally { if ([IO.Directory]::Exists($aliasWork)) { Remove-Item -LiteralPath $aliasWork -Recurse -Force } }
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
            if ($mode -ne 'data') {
                $expectedProgram = Snapshot-Split $backup $programNames
                $actualProgram = Snapshot-Split $runtime $programNames
                if ($actualProgram -cne $expectedProgram) {
                    Write-SplitSnapshotDifference $expectedProgram $actualProgram $backup $runtime
                    throw 'Restored runtime composition mismatch.'
                }
            }
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
        if ($mode -eq 'selfcheck-live' -and $null -ne $child -and -not [IO.File]::Exists((Join-Path $work 'live-child-settled'))) {
            $settled = $false
            Write-Warning ('Native self-check cleanup unconfirmed; fixture evidence retained: ' + $work)
        }
        if ($settled -and [IO.Directory]::Exists($work)) { Remove-Item -LiteralPath $work -Recurse -Force }
    }
}
