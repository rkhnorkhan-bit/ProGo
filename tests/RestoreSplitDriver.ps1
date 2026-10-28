param([string]$Scripts, [string]$Fixture, [string]$Mode, [string]$StorageRoot)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Requires isolated Windows CI' }
if (-not [string]::IsNullOrWhiteSpace($StorageRoot)) {
    $StorageRoot = [IO.Path]::GetFullPath($StorageRoot).TrimEnd([char]'\')
    $leaf = [IO.Path]::GetFileName($StorageRoot)
    if ([IO.Path]::GetDirectoryName($StorageRoot) -ine [IO.Path]::GetPathRoot($StorageRoot) -or
        $leaf -cnotmatch '^ProGo-storage-fixture-[a-f0-9]{32}$' -or
        ([IO.File]::GetAttributes($StorageRoot) -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or
        [IO.File]::ReadAllText((Join-Path $StorageRoot 'owner.txt')) -cne $leaf) { throw 'Storage fixture root is not owned.' }
}
$fixture = [IO.Path]::GetFullPath($Fixture)
if ([IO.Path]::GetDirectoryName($fixture) -ine [IO.Path]::GetFullPath($env:TEMP).TrimEnd([char]'\') -or [IO.Path]::GetFileName($fixture) -cnotmatch '^ProGo-split-restore-[a-f0-9]{32}$') { throw 'Restore fixture escaped its owned root.' }
$dataBase = Join-Path $fixture 'data'; $programBase = if ($StorageRoot) { Join-Path $StorageRoot ('restore-' + $Mode) } else { Join-Path $fixture 'programs' }
$runtime = Join-Path $programBase 'ProGo'; $data = Join-Path $dataBase 'ProGo'
$BackupDir = Join-Path $fixture 'backup'; $WaitPid = 0; $NoLaunch = $true
$Scope = if ($Mode -eq 'data') { 'Data' } elseif ($Mode -in @('all','fault-all','no-consent')) { 'All' } else { 'Program' }
$ConfirmData = $Mode -in @('data','all','fault-all')
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Scripts 'Restore-ProGoBackup.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Restore source does not parse.' }
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) { . ([scriptblock]::Create($statement.Extent.Text.Replace('$PSScriptRoot','$Scripts'))) }
}
function Show-UserMessage($Text, $Title) { }
# Default self-check is stubbed. selfcheck-live executes only a generated
# fixture image at the exact installed path; integrity/maintenance/Undo stay real.
$script:liveCheck = $null
function Test-RestoredApplication {
    if ($Mode -ne 'selfcheck-live') { [IO.File]::WriteAllText((Join-Path $fixture 'selfcheck-stub'), 'isolated'); return }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $InstallDir 'ProGo.exe'
    $ready = Join-Path $fixture 'live-ready'
    $info.Arguments = '"' + $ready + '"'
    $info.UseShellExecute = $false; $info.CreateNoWindow = $true
    $script:liveCheck = [Diagnostics.Process]::Start($info)
    $deadline = [DateTime]::UtcNow.AddSeconds(3)
    while (-not [IO.File]::Exists($ready) -and -not $script:liveCheck.HasExited -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 20 }
    if (-not [IO.File]::Exists($ready) -or $script:liveCheck.HasExited) { throw 'Native self-check fixture did not become ready.' }
    # Deliberately retain this exact live child to exercise cleanup-refusal,
    # rather than pretending that a failed/unfinished cleanup permits Undo.
    throw 'Synthetic self-check timeout with cleanup refusal'
}
$realStart = ${function:Start-ProGo}
function Start-ProGo {
    if ($Mode -eq 'selfcheck-live') { [IO.File]::WriteAllText((Join-Path $fixture 'unexpected-relaunch'), 'called') }
    & $realStart
}
$realMove = ${function:Move-RestoreRoot}; $realUndo = ${function:Undo-Restore}
function Move-RestoreRoot($From, $To) {
    $inject = ($Mode -in @('fault-program','fault-missing') -and $From -ceq (Join-Path $runtime 'VERSION')) -or ($Mode -eq 'fault-all' -and $From -ceq (Join-Path $data 'vault.enc.json'))
    if ($inject -and -not [IO.File]::Exists((Join-Path $fixture 'injected'))) {
        if ([IO.File]::ReadAllText((Join-Path $runtime 'ProGo.exe')) -cne 'backup-exe') { throw 'Fault was reached before runtime publication.' }
        if (-not $Transaction.Stage.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase) -or -not $Transaction.Previous.StartsWith($runtime + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Runtime transaction escaped its destination root.' }
        if ($Mode -eq 'fault-all' -and ([IO.File]::ReadAllText((Join-Path $data 'settings.json')) -cne 'backup-settings' -or -not $Transaction.DataStage.StartsWith($data + '\', [StringComparison]::OrdinalIgnoreCase) -or -not $Transaction.DataPrevious.StartsWith($data + '\', [StringComparison]::OrdinalIgnoreCase))) { throw 'Fault was reached before data publication on its own root.' }
        [IO.File]::WriteAllText((Join-Path $fixture 'injected'), $Mode)
        throw 'Synthetic split restore post-mutation fault'
    }
    & $realMove $From $To
}
function Undo-Restore {
    [IO.File]::WriteAllText((Join-Path $fixture 'undo-entered'), 'entered')
    & $realUndo
    if ((Get-InstalledRestoreState $RestoreNames) -cne $Transaction.Before) { throw 'Actual Undo did not restore both roots.' }
    [IO.File]::WriteAllText((Join-Path $fixture 'undo-completed'), 'verified')
}
$seams = 0
try {
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -isnot [Management.Automation.Language.FunctionDefinitionAst]) {
        $source = $statement.Extent.Text.Replace('$PSScriptRoot','$Scripts')
        if ($source -ceq '$layout = Resolve-ProGoRecoveryLayout') {
            $seams++
            $source = '$layout = Resolve-ProGoRecoveryLayoutCore -DataBase $dataBase -ProgramBase $programBase -RuntimeRoot $runtime -SeparateIdentityConfirmed $true'
        }
        . ([scriptblock]::Create($source))
    }
}
if ($seams -ne 1) { throw 'Restore fixture resolver seam mismatch.' }
} catch {
    $consentError = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('0JfQsNC80LXQvdCwINC90LDRgdGC0YDQvtC10Log0Lgg0YXRgNCw0L3QuNC70LjRidCwINGC0YDQtdCx0YPQtdGCINC+0YLQtNC10LvRjNC90L7Qs9C+INC/0L7QtNGC0LLQtdGA0LbQtNC10L3QuNGPLg=='))
    if ($Mode -eq 'no-consent' -and $_.Exception.ToString().Contains($consentError)) { [Console]::Error.WriteLine('SPLIT_RESTORE_CONSENT_REFUSED') }
    throw
}
finally {
    if ($null -ne $script:liveCheck) {
        try {
            if ($script:liveCheck.HasExited -or $script:liveCheck.MainModule.FileName -ine (Get-Item -LiteralPath (Join-Path $InstallDir 'ProGo.exe')).FullName -or
                [IO.File]::Exists((Join-Path $fixture 'undo-entered')) -or [IO.File]::Exists((Join-Path $fixture 'unexpected-relaunch')) -or
                $Transaction.SafeToRemove -or $Transaction.Complete -or $Transaction.Journal.Count -eq 0) { throw 'Live exact-runtime refusal was not preserved.' }
            foreach ($ownedRoot in $Transaction.Roots) { if (-not [IO.Directory]::Exists($ownedRoot)) { throw 'Live refusal lost transaction evidence.' } }
            $log = [IO.File]::ReadAllText($RestoreLog)
            if (-not $log.Contains('Rollback incomplete. Recovery files retained:') -or -not $log.Contains('Installed ProGo is still running.')) { throw 'Actual stopped-process guard did not refuse Undo.' }
            [IO.File]::WriteAllText((Join-Path $fixture 'live-guard-verified'), 'exact-runtime-before-undo')
        } finally {
            try {
                if (-not $script:liveCheck.HasExited) { $script:liveCheck.Kill() }
                if (-not $script:liveCheck.WaitForExit(2000)) { throw 'Owned native self-check fixture cleanup unconfirmed.' }
                [IO.File]::WriteAllText((Join-Path $fixture 'live-child-settled'), 'settled')
            } finally { $script:liveCheck.Dispose() }
        }
    }
}
