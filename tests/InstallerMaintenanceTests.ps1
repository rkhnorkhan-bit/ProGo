Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer does not parse.' }
# Load pure preflight/staging helpers only; Enter/Stop remain mocked below.
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$boundary = @($ast.EndBlock.Statements | Where-Object {
    $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir'
})
if ($boundary.Count -ne 1) { throw 'Installer maintenance boundary is ambiguous.' }
# Exercise the actual production write/try/finally/launch statements, not a copy
# of their logic. The adapters below replace every OS/IPC/shortcut/process action.
$body = [scriptblock]::Create((Get-Content -LiteralPath $installer -Raw).Substring($boundary[0].Extent.StartOffset))
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-installer-owner-' + [Guid]::NewGuid().ToString('N'))
$oldLocal = $env:LOCALAPPDATA
function Check-InstallerOwner($Condition, $Name) {
    if (-not $Condition) { throw ('Installer maintenance test failed: ' + $Name) }
}
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $ReleaseDir = Join-Path $work 'payload'
    Microsoft.PowerShell.Management\Copy-Item -LiteralPath (Join-Path $Root 'release') -Destination $ReleaseDir -Recurse
    $PayloadScripts = Join-Path $ReleaseDir 'scripts'
    # This shim is copied only inside the fixture. It never calls real known folders.
    @'
function Test-ProGoExistingInstallation { param($InstallDirectory) return $true }
function Initialize-ProGoShortcuts {
    param($InstallDirectory, $WasInstalled, $NoStartup, $NoStartMenu, [switch]$CaptureStartupEvidence)
    if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Shortcut escaped maintenance.' }
    $FixtureState.Events.Add('shortcuts')
    if ($FixtureState.Mode -eq 'shortcut-fail') { throw 'fixture-shortcut-fail' }
    # Synthetic auxiliary adapter result; no Windows known folder or shortcut is touched.
    if ($CaptureStartupEvidence) { return [pscustomobject]@{ Schema = 1; InstallIdentity = (Get-ProGoInstallerIdentity $InstallDirectory); Disposition = 'preserved'; Exists = $false; Hash = $null; Menu = [pscustomobject]@{ Schema = 1; InstallIdentity = (Get-ProGoInstallerIdentity $InstallDirectory) } } }
}
'@ | Set-Content -LiteralPath (Join-Path $PayloadScripts 'Shortcuts-ProGo.ps1') -Encoding UTF8
    $Payload = Get-ProGoInstallPayload -Directory $ReleaseDir
    $Exe = Join-Path $ReleaseDir 'ProGo.exe'
    $VersionFile = Join-Path $ReleaseDir 'VERSION'
    $NoStartup = $false; $NoStartMenuShortcut = $false
    function Run-InstallerOwnerCase($Mode) {
        $case = Join-Path $work $Mode
        New-Item -ItemType Directory -Path (Join-Path $case 'ProGo') | Out-Null
        $env:LOCALAPPDATA = $case
        $data = Join-Path $case 'ProGo\settings.json'
        [IO.File]::WriteAllText($data, 'synthetic-user-data-unchanged')
        $before = [IO.File]::ReadAllBytes($data)
        $oldRuntime = Join-Path $case 'ProGo\ProGo.exe'
        if ($Mode -in @('backup-fail', 'self-check-fail', 'bad-restore', 'rollback-fail', 'snapshot-corrupt', 'terminal-fail', 'terminal-after-write-fail', 'rollback-terminal-fail', 'rollback-terminal-after-write-fail')) { [IO.File]::WriteAllText($oldRuntime, 'synthetic prior runtime') }
        $FixtureState = @{ Mode = $Mode; LeaseHeld = $false; Stopped = $false; Disposes = 0; Copies = 0; CopyFaults = 0; SuccessMessage = $false; SelfChecks = 0; PriorVerified = $null; RollbackVerified = $null; Events = (New-Object 'System.Collections.Generic.List[string]') }
        function Enter-ProGoInstallerMaintenance {
            $FixtureState.Events.Add('enter')
            if ($Mode -eq 'busy') { throw 'fixture-busy' }
            $FixtureState.LeaseHeld = $true
            $lease = [pscustomobject]@{ State = $FixtureState }
            $lease | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
                $this.State.Disposes++; $this.State.LeaseHeld = $false
                $this.State.Events.Add('dispose')
            }
            return $lease
        }
        function Stop-ProGoInstallerApplication($InstallDirectory) {
            if (-not $FixtureState.LeaseHeld) { throw 'Stop escaped maintenance.' }
            $FixtureState.Events.Add('stop')
            if ($Mode -eq 'stop-fail' -or $Mode -eq 'retry-stop') { throw ('fixture-' + $Mode) }
            $FixtureState.Stopped = $true
        }
        function Copy-ProGoInstallerBackupFile([string]$Source, [string]$Destination) {
            if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Backup escaped maintenance.' }
            if ($Mode -eq 'backup-fail') { throw 'fixture-backup-fail' }
            [IO.File]::Copy($Source, $Destination, $false)
        }
        function Invoke-ProGoInstallerSelfCheck($Info) {
            if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Self-check escaped maintenance.' }
            $FixtureState.SelfChecks++; $FixtureState.Events.Add('self-check')
            if ($Mode -eq 'bad-restore') { [IO.File]::WriteAllText($Info.FileName, 'synthetic failed restoration'); return 17 }
            if ($Mode -eq 'snapshot-corrupt') {
                [IO.File]::WriteAllText((Join-Path $InstallBackup.Directory 'ProGo.exe'), 'synthetic damaged snapshot'); return 17
            }
            if ($Mode -in @('self-check-fail', 'rollback-fail', 'rollback-terminal-fail', 'rollback-terminal-after-write-fail')) { return 17 }
            [IO.Directory]::CreateDirectory($Info.EnvironmentVariables['PROGO_SELF_CHECK_ROOT']) | Out-Null
            return 0
        }
        function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
            if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Copy escaped maintenance.' }
            $FixtureState.Copies++; $FixtureState.Events.Add('copy')
            # Called from Set-ProGoInstallerRuntimeFile, whose local $mode is a
            # FileMode. Read the scenario from fixture state, not dynamic scope.
            if ($FixtureState.Mode -eq 'copy-fail') {
                Check-InstallerOwner ($Source -is [IO.FileStream] -and $Target -is [IO.FileStream] -and
                    $Source.CanRead -and $Target.CanWrite -and $InstallWrites.Count -eq 1) 'copy fault reaches admitted owned streams'
                $FixtureState.CopyFaults++
                throw 'fixture-copy-fail'
            }
            $Target.SetLength(0)
            if ($FixtureState.Mode -eq 'partial-copy' -and [IO.Path]::GetFileName($Target.Name) -eq 'ProGo.exe') {
                $bytes = [Text.Encoding]::UTF8.GetBytes('synthetic incomplete write')
                $Target.Write($bytes, 0, $bytes.Length); return
            }
            $Source.CopyTo($Target); $Target.Flush($true)
        }
        function Copy-ProGoInstallerRestoreFile([string]$Source, [string]$Destination) {
            if (-not $FixtureState.LeaseHeld -or -not $FixtureState.Stopped) { throw 'Rollback escaped maintenance.' }
            if ($Mode -eq 'rollback-fail') { [IO.File]::WriteAllText($Destination, 'synthetic wrong restored bytes'); return }
            [IO.File]::Copy($Source, $Destination, $true)
        }
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            if ($FixtureState.Mode -eq 'terminal-fail') { throw 'fixture-terminal-fail' }
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            if ($FixtureState.Mode -eq 'terminal-after-write-fail') { throw 'fixture-terminal-after-write-fail' }
        }
        function Write-ProGoInstallerRollbackOutcomeBytes($Stream, [byte[]]$Bytes) {
            if ($FixtureState.Mode -eq 'rollback-terminal-fail') { throw 'synthetic rollback terminal persistence failure' }
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            if ($FixtureState.Mode -eq 'rollback-terminal-after-write-fail') { throw 'synthetic rollback terminal persistence failure after flush' }
        }
        function Start-Process {
            param($FilePath, $WorkingDirectory, $ArgumentList)
            if ($FixtureState.LeaseHeld -or $FixtureState.Disposes -ne 1) { throw 'Launch preceded release.' }
            $FixtureState.Events.Add('launch')
            if ($Mode -eq 'launch-fail') { throw 'fixture-launch-fail' }
        }
        function Write-Host($Object) {
            # Existing installer status text contains its path. Capture only the
            # success boolean; never emit fixture/user temp paths into CI output.
            if ($Object -like 'Install OK:*') { $FixtureState.SuccessMessage = $true }
            if ($Object -match 'rollbackVerified=(True|False)') { $FixtureState.RollbackVerified = $Matches[1] -eq 'True' }
            if ($Object -match 'priorRuntimeVerified=(True|False)') { $FixtureState.PriorVerified = $Matches[1] -eq 'True' }
        }
        $Launch = $Mode -eq 'launch' -or $Mode -eq 'launch-fail'
        $savedVersion = [IO.File]::ReadAllBytes($VersionFile)
        if ($Mode -eq 'stage-changed') { [IO.File]::WriteAllText($VersionFile, '999.998.997') }
        $caught = $false
        try { & $body } catch {
            if ($Mode -in @('self-check-fail', 'bad-restore', 'rollback-fail', 'snapshot-corrupt', 'rollback-terminal-fail', 'rollback-terminal-after-write-fail')) {
                if (-not $_.Exception.Message.Contains('Installed installer self-check failed.')) { throw }
            } elseif ($Mode -eq 'partial-copy') {
                if (-not $_.Exception.Message.Contains('Installer runtime bytes are not verified.')) { throw }
            } elseif ($Mode -eq 'stage-changed') {
                if (-not $_.Exception.Message.Contains('Installer payload changed or stage copy is corrupt.')) { throw }
            } elseif (-not $_.Exception.Message.Contains('fixture-' + $Mode)) { throw }
            $caught = $true
        }
        [IO.File]::WriteAllBytes($VersionFile, $savedVersion)
        Check-InstallerOwner (-not $FixtureState.LeaseHeld) ($Mode + ' releases ownership')
        Check-InstallerOwner (([Convert]::ToBase64String($before)) -ceq ([Convert]::ToBase64String([IO.File]::ReadAllBytes($data)))) ($Mode + ' preserves user bytes')
        if ($Mode -eq 'busy') {
            Check-InstallerOwner ($caught -and $FixtureState.Disposes -eq 0 -and $FixtureState.Copies -eq 0 -and $FixtureState.Events.Count -eq 1) 'busy refuses all writes'
        } elseif ($Mode -eq 'stage-changed' -or $Mode -eq 'backup-fail') {
            Check-InstallerOwner ($caught -and $FixtureState.Disposes -eq 1 -and $FixtureState.Copies -eq 0 -and
                ($FixtureState.Events -join ',') -eq 'enter,stop,dispose') 'failed staging/backup releases without runtime writes'
            if ($Mode -eq 'backup-fail') { Check-InstallerOwner ([IO.File]::ReadAllText($oldRuntime) -ceq 'synthetic prior runtime') 'unconfirmed backup never overwrites runtime' }
        } elseif ($Mode -eq 'stop-fail' -or $Mode -eq 'retry-stop') {
            Check-InstallerOwner ($caught -and $FixtureState.Disposes -eq 1 -and $FixtureState.Copies -eq 0 -and ($FixtureState.Events -join ',') -eq 'enter,stop,dispose') 'stop refusal releases without writes'
            if ($Mode -eq 'retry-stop') {
                # A new explicit attempt must acquire and release a new owner too.
                try { & $body; throw 'Retry unexpectedly succeeded.' } catch {
                    if (-not $_.Exception.Message.Contains('fixture-retry-stop')) { throw }
                }
                Check-InstallerOwner ($FixtureState.Disposes -eq 2 -and $FixtureState.Copies -eq 0) 'retry retains refusal and releases new owner'
            }
        } else {
            Check-InstallerOwner ($FixtureState.Disposes -eq 1 -and $FixtureState.Events[0] -eq 'enter' -and $FixtureState.Events[1] -eq 'stop' -and $FixtureState.Copies -gt 0) 'copy lifecycle ordering'
            Check-InstallerOwner ($caught -eq ($Mode -in @('copy-fail', 'shortcut-fail', 'launch-fail', 'self-check-fail', 'partial-copy', 'bad-restore', 'rollback-fail', 'snapshot-corrupt', 'terminal-fail', 'terminal-after-write-fail', 'rollback-terminal-fail', 'rollback-terminal-after-write-fail'))) ($Mode + ' propagates failure')
            if ($Mode -eq 'launch' -or $Mode -eq 'launch-fail') {
                Check-InstallerOwner ($FixtureState.Events[$FixtureState.Events.Count - 2] -eq 'dispose' -and $FixtureState.Events[$FixtureState.Events.Count - 1] -eq 'launch') 'launch after release'
            } else { Check-InstallerOwner ($FixtureState.Events[$FixtureState.Events.Count - 1] -eq 'dispose') 'final release' }
        }
        if ($Mode -eq 'copy-fail') {
            Check-InstallerOwner ($FixtureState.CopyFaults -eq 1 -and $FixtureState.Copies -eq 1 -and
                $FixtureState.SelfChecks -eq 0 -and $FixtureState.PriorVerified -eq $true -and
                $FixtureState.RollbackVerified -eq $true -and -not [IO.File]::Exists($oldRuntime)) 'copy fault propagates after verified owned cleanup without self-check'
        }
        if ($Mode -in @('self-check-fail', 'partial-copy', 'bad-restore', 'rollback-fail', 'snapshot-corrupt')) {
            $restored = $Mode -in @('self-check-fail', 'partial-copy')
            Check-InstallerOwner ($FixtureState.PriorVerified -eq $restored -and $FixtureState.RollbackVerified -eq $restored) 'restoration claim requires verified old bytes and absence'
            if ($Mode -eq 'self-check-fail') { Check-InstallerOwner ([IO.File]::ReadAllText($oldRuntime) -ceq 'synthetic prior runtime') 'self-check refusal restores exact previous image' }
            if ($Mode -eq 'partial-copy') {
                Check-InstallerOwner ($FixtureState.SelfChecks -eq 0 -and -not [IO.File]::Exists($oldRuntime)) 'owned partial copy removed before native self-check'
            } else { Check-InstallerOwner ($FixtureState.SelfChecks -eq 1) 'one owned self-check attempt' }
            if ($Mode -eq 'bad-restore') { Check-InstallerOwner ([IO.File]::ReadAllText($oldRuntime) -ceq 'synthetic failed restoration') 'changed ownership left untouched' }
        }
        if ($Mode -in @('terminal-fail', 'terminal-after-write-fail')) {
            Check-InstallerOwner ($caught -and $FixtureState.PriorVerified -eq $true -and $FixtureState.RollbackVerified -eq $true -and
                [IO.File]::ReadAllText($oldRuntime) -ceq 'synthetic prior runtime' -and -not $FixtureState.Events.Contains('launch')) 'terminal persistence failure rolls back exactly and blocks Launch'
            Check-InstallerOwner (@(Get-ChildItem -LiteralPath (Join-Path $case 'ProGo\backups') -Recurse -Filter 'installed.json').Count -eq 0) 'terminal failure never publishes installed outcome'
        }
        if ($Mode -in @('copy-fail', 'partial-copy', 'self-check-fail', 'shortcut-fail')) {
            Check-InstallerOwner (@(Get-ChildItem -LiteralPath (Join-Path $case 'ProGo\backups') -Recurse -Filter 'rolled-back.json').Count -eq 1) 'known verified rollback persists separate terminal'
            Assert-ProGoInstallerNoPendingJournal (Join-Path $case 'ProGo')
        }
        if ($Mode -in @('rollback-terminal-fail', 'rollback-terminal-after-write-fail')) {
            Check-InstallerOwner ($caught -and $FixtureState.PriorVerified -eq $true -and $FixtureState.RollbackVerified -eq $true -and
                [IO.File]::ReadAllText($oldRuntime) -ceq 'synthetic prior runtime' -and -not $FixtureState.Events.Contains('launch')) 'terminal persistence error preserves original self-check refusal, verified bytes and no Launch'
            $blocked = $false
            try { Assert-ProGoInstallerNoPendingJournal (Join-Path $case 'ProGo') } catch { $blocked = $true }
            Check-InstallerOwner $blocked 'unconfirmed rollback terminal remains pending'
        }
        Check-InstallerOwner ($FixtureState.SuccessMessage -eq ($Mode -in @('complete', 'launch', 'launch-fail'))) ($Mode + ' never reports install success before admitted copies/shortcuts')
        Microsoft.PowerShell.Utility\Write-Host ('PASS: installer maintenance ' + $Mode)
    }
    foreach ($mode in @('complete', 'busy', 'stop-fail', 'copy-fail', 'shortcut-fail', 'launch', 'launch-fail', 'retry-stop', 'stage-changed', 'backup-fail', 'self-check-fail', 'partial-copy', 'bad-restore', 'rollback-fail', 'snapshot-corrupt', 'terminal-fail', 'terminal-after-write-fail', 'rollback-terminal-fail', 'rollback-terminal-after-write-fail')) { Run-InstallerOwnerCase $mode }
    Write-Host 'Installer maintenance tests PASS: 19'
} finally {
    $env:LOCALAPPDATA = $oldLocal
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
