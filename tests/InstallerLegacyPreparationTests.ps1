param([switch]$BlockedChild,[string]$CaseDirectory)
# Preparation prerequisite only; the default installer/error path is not activated here.
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Legacy preparation prerequisites do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
if ($BlockedChild) {
    $parent = [IO.Path]::GetDirectoryName($CaseDirectory)
    if ([IO.Path]::GetFileName($CaseDirectory) -cnotin @('matched-failure','context-lost') -or
        [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-legacy-prepare-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Preparation child refuses unowned scope.' }
    foreach ($path in @($parent,$CaseDirectory)) { if ([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint) { throw 'Preparation child refuses redirected scope.' } }
    $install = Join-Path $CaseDirectory 'app'; $programs = Join-Path $CaseDirectory 'Programs'; $startup = Join-Path $CaseDirectory 'Startup'
    $journals = @(Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-journal-[a-f0-9]{32}$' })
    if ($journals.Count -ne 1 -or $null -ne (Restore-ProGoInstallerLegacyReferenceEvidence $journals[0].FullName $install $programs $startup) -or
        $null -ne (Invoke-ProGoInstallerLegacyOwnedDeletion $journals[0].FullName $install $programs $startup)) { throw 'Preparation receipts cannot mint lost live authority.' }
    Write-Host 'LEGACY_PREPARE_CHILD blocked=True; replayPerformed=False'
    return
}
# The retained native target requests READ|DELETE and shares READ only. Symmetric sharing
# requires our reader to share DELETE too; this does not grant the reader DELETE/WRITE access.
function Get-LegacyPreparationFileHash([string]$LiteralPath) {
    $stream = $null; $algorithm = $null
    try {
        $algorithm = [Security.Cryptography.SHA256]::Create()
        $share = [IO.FileShare]([IO.FileShare]::Read -bor [IO.FileShare]::Delete)
        $stream = [IO.File]::Open($LiteralPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,$share)
        return [pscustomobject]@{ Hash = [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','') }
    } finally {
        if ($null -ne $stream) { $stream.Dispose() }
        if ($null -ne $algorithm) { $algorithm.Dispose() }
    }
}
function Check-LegacyPreparation($Condition,[string]$Name) { if (-not $Condition) { throw ('Legacy preparation failed: case=' + $scenario + '; check=' + $Name) } }
$scenario = 'initial'; $work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-prepare-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $physical = $null; $lease = $null; $cancel = $null; $process = $null; $outTask = $null; $errTask = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $invalid = Join-Path $work 'invalid-no-evidence'; $refused = $false
    try { Initialize-ProGoShortcuts -InstallDirectory $invalid -WasInstalled $false -NoStartup $true -NoStartMenu $true -ProgramsDirectory $work -StartupDirectory $work -PreserveLegacyForOwnedOperation | Out-Null } catch { $refused = $true }
    Check-LegacyPreparation ($refused -and -not [IO.Directory]::Exists($invalid)) 'preparation switch requires evidence before any write'
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $cases = @('matched-failure','absent','custom-preserved','menu-optout','startup-optout','existing-primary','foreign-primary',
        'cancel-before','cancel-after-accepted','cancel-after-receipt','foreign-destination','equal-byte-destination','context-lost')
    foreach ($scenario in $cases) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $legacy = Join-Path $menu 'ProGo Status.lnk'; $primary = Join-Path $menu 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic original executable')
        $private = @{}
        foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) {
            $path = Join-Path $install $name; [IO.File]::WriteAllText($path,'synthetic private or foreign bytes'); $private[$path] = (Get-LegacyPreparationFileHash -LiteralPath $path).Hash
        }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $seed = $manager
        if ($scenario -in @('custom-preserved','foreign-primary')) {
            $otherRoot = Join-Path $case 'other-app'; New-Item -ItemType Directory -Path $otherRoot | Out-Null
            [IO.File]::WriteAllText((Join-Path $otherRoot 'ProGo.exe'),'synthetic foreign executable')
            $other = New-Object ProGo.ApplicationShortcuts($otherRoot,$programs,$startup)
            if ($scenario -ceq 'custom-preserved') { $seed = $other }
        }
        if ($scenario -cne 'absent') { $seed.Install($true,$true,$false); [IO.File]::Move($primary,$legacy) }
        [byte[]]$before = if ([IO.File]::Exists($legacy)) { [IO.File]::ReadAllBytes($legacy) } else { $null }
        $origin = $null; $capture = $manager.CaptureLegacyRollbackSource([Threading.CancellationToken]::None)
        if ($null -ne $capture) { try { $origin = $capture.OriginIdentity } finally { $capture.Dispose() } }
        if ($scenario -ceq 'existing-primary') { [IO.File]::WriteAllBytes($primary,$before) }
        if ($scenario -ceq 'foreign-primary') { $other.Install($true,$true,$false); $foreignPrimaryHash = (Get-LegacyPreparationFileHash -LiteralPath $primary).Hash }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $prepared = $null; $failed = $false
        try { $prepared = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup ($scenario -ceq 'startup-optout') -NoStartMenu ($scenario -ceq 'menu-optout') -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation }
        catch { if ($scenario -cne 'foreign-primary') { throw }; $failed = $true }
        if ($scenario -ceq 'foreign-primary') {
            Check-LegacyPreparation ($failed -and (Get-LegacyPreparationFileHash -LiteralPath $primary).Hash -ceq $foreignPrimaryHash -and [IO.File]::Exists($legacy) -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($before)) 'foreign primary blocks preparation without deleting Legacy'
        } else {
            Check-LegacyPreparation ($prepared.Legacy.Disposition -ceq 'preserved' -and $manager.VerifyStartupInstallEvidence($prepared) -and $manager.VerifyMenuInstallEvidence($prepared.Menu) -and $manager.VerifyLegacyInstallEvidence($prepared.Legacy)) 'existing full evidence verifiers accept only preserved preparation'
            if ($null -ne $before) { Check-LegacyPreparation ([Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($before)) 'preparation never deletes or refreshes Legacy' }
            else { Check-LegacyPreparation (-not [IO.File]::Exists($legacy)) 'absent Legacy stays absent' }
            if ($null -ne $origin) { Check-LegacyPreparation ($prepared.Legacy.OriginIdentity -ceq $origin) 'preparation retains exact original native origin' }
            if ($scenario -ceq 'custom-preserved') { Check-LegacyPreparation (-not $prepared.Legacy.BeforeMatched) 'custom foreign target is not adopted' }
            if ($scenario -ceq 'menu-optout') { Check-LegacyPreparation (-not [IO.File]::Exists($primary) -and -not $prepared.Menu.Requested) 'menu optout creates no primary or deletion context' }
            if ($scenario -ceq 'startup-optout') { Check-LegacyPreparation (-not [IO.File]::Exists((Join-Path $startup 'ProGo.lnk'))) 'startup optout retained' }
            if ($scenario -ceq 'existing-primary') { Check-LegacyPreparation ($prepared.Menu.Disposition -ceq 'preserved') 'existing primary not recreated' }
            if ($scenario -notin @('absent','custom-preserved','menu-optout','startup-optout')) {
                $primaryHash = (Get-LegacyPreparationFileHash -LiteralPath $primary).Hash
                $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
                Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup
                [void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
                $physical = Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $install $programs $startup
                $lease = Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $install $programs $startup
                $cancel = New-Object Threading.CancellationTokenSource; $deleted = $null; $recorded = $null; $restored = $null
                if ($scenario -ceq 'cancel-before') { $cancel.Cancel() }
                $originalError = $null
                try {
                    $deleted = $lease.DeleteOwnedOnce($physical,$cancel.Token)
                    if ($scenario -ceq 'cancel-after-accepted') { $cancel.Cancel() }
                    $recorded = $lease.RecordAcceptedOutcome($physical,$cancel.Token)
                    if ($scenario -ceq 'cancel-after-receipt') { $cancel.Cancel() }
                    if ($scenario -ceq 'foreign-destination') { [IO.File]::WriteAllText($legacy,'synthetic foreign destination') }
                    if ($scenario -ceq 'equal-byte-destination') { [IO.File]::WriteAllBytes($legacy,$before) }
                    if ($scenario -ceq 'context-lost') { $lease.Dispose() }
                    throw 'synthetic failure after prepared shortcuts'
                } catch {
                    $originalError = $_
                    $restored = $lease.RestoreOwnedOnce($physical,$cancel.Token)
                }
                $verified = $lease.VerifyRestoredOutcome($physical)
                $expectedRestore = $scenario -in @('matched-failure','existing-primary')
                Check-LegacyPreparation ($verified -eq $expectedRestore -and $restored.DurableResultRecorded -eq $expectedRestore -and -not $restored.WholeRollbackVerified -and -not $restored.InstallSucceeded) 'scoped verified restoration is not whole auxiliary rollback'
                if ($scenario -ceq 'cancel-before') {
                    $cause = $originalError.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
                    Check-LegacyPreparation ($cause -is [OperationCanceledException] -and $null -eq $deleted -and -not $restored.Created -and [IO.File]::Exists($legacy)) 'cancellation before deletion preserves original error and object'
                }
                else { Check-LegacyPreparation ($deleted.OperationAccepted -and $deleted.TargetHandleClosed -and $originalError.Exception.Message -ceq 'synthetic failure after prepared shortcuts') 'accepted mutation and original failure remain truthful' }
                if ($scenario -ceq 'cancel-after-accepted') { Check-LegacyPreparation (-not $recorded.DurableOutcomeRecorded -and -not $restored.Created) 'accepted unrecorded cancellation stays manual' }
                if ($scenario -ceq 'cancel-after-receipt') { Check-LegacyPreparation ($recorded.DurableOutcomeRecorded -and -not $restored.Created) 'receipt does not override cancellation' }
                if ($expectedRestore) {
                    Check-LegacyPreparation ($restored.RestoredOrigin -cne $origin -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($before)) 'restoration has new identity and original bytes'
                    Check-LegacyPreparation (-not $manager.VerifyLegacyInstallEvidence($prepared.Legacy)) 'old preserved evidence cannot authorize later Startup Menu rollback after new native restore'
                }
                if ($scenario -ceq 'foreign-destination') { Check-LegacyPreparation ([IO.File]::ReadAllText($legacy) -ceq 'synthetic foreign destination' -and -not $restored.Created) 'foreign restore destination never overwritten' }
                if ($scenario -ceq 'equal-byte-destination') { Check-LegacyPreparation ([Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($before) -and -not $restored.Created) 'equal-byte destination never adopted' }
                if ($scenario -ceq 'cancel-before') {
                    Check-LegacyPreparation ($lease.IsPinned -and $physical.VerifyPinned([Threading.CancellationToken]::None)) 'regression begins with real live retained target and physical pins'
                    $defaultReaderRefused = $false
                    try { Get-FileHash -LiteralPath $legacy -ErrorAction Stop | Out-Null } catch { $defaultReaderRefused = $true }
                    Check-LegacyPreparation $defaultReaderRefused 'actual old cmdlet reader refuses the retained DELETE-access target'
                    $readRefused = $false; $writeRefused = $false; $sharingProbe = $null
                    try { $sharingProbe = [IO.File]::Open($legacy,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read) }
                    catch {
                        $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
                        $readRefused = $cause -is [IO.IOException] -and ($cause.HResult -band 65535) -eq 32
                    } finally { if ($null -ne $sharingProbe) { $sharingProbe.Dispose(); $sharingProbe = $null } }
                    $compatibleHash = (Get-LegacyPreparationFileHash -LiteralPath $legacy).Hash
                    Check-LegacyPreparation ($readRefused -and $compatibleHash -ceq $prepared.Legacy.Hash) 'explicit READ share1 fails Win32 sharing32 while READ share5 hashes exact retained bytes'
                    try { $sharingProbe = [IO.File]::Open($legacy,[IO.FileMode]::Open,[IO.FileAccess]::Write,([IO.FileShare]::Read -bor [IO.FileShare]::Delete)) }
                    catch {
                        $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }
                        $writeRefused = $cause -is [IO.IOException] -and ($cause.HResult -band 65535) -eq 32
                    } finally { if ($null -ne $sharingProbe) { $sharingProbe.Dispose(); $sharingProbe = $null } }
                    foreach ($path in $private.Keys) { Check-LegacyPreparation ((Get-LegacyPreparationFileHash -LiteralPath $path).Hash -ceq $private[$path]) 'live-pin inventory preserves private and foreign bytes' }
                    Check-LegacyPreparation ($writeRefused -and $lease.IsPinned -and $physical.VerifyPinned([Threading.CancellationToken]::None)) 'compatible read settles without releasing or weakening production pins'
                    Write-Host 'Legacy preparation sharing regression PASS: retained READ|DELETE share1; read share1 refused32; read share5 SHA256 verified; write refused32; pins retained'
                }
                $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = (Get-LegacyPreparationFileHash -LiteralPath $file.FullName).Hash }
                Check-LegacyPreparation ([object]::ReferenceEquals($restored,$lease.RestoreOwnedOnce($physical,$cancel.Token))) 'repeated error handler returns exact result without mutation'
                Check-LegacyPreparation (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count) 'repeated handler creates no artifact'
                foreach ($path in $inventory.Keys) { Check-LegacyPreparation ((Get-LegacyPreparationFileHash -LiteralPath $path).Hash -ceq $inventory[$path]) 'repeated handler preserves partial/source/evidence bytes' }
                if ($scenario -in @('matched-failure','context-lost')) {
                    $info = New-Object Diagnostics.ProcessStartInfo; $info.FileName = Join-Path $PSHOME 'powershell.exe'
                    $info.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -BlockedChild -CaseDirectory "' + $case + '"'
                    $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
                    $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
                    if (-not $process.WaitForExit(20000)) { throw 'Preparation owned child exceeded bounded wait.' }
                    Check-LegacyPreparation ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and $outTask.Result.Contains('LEGACY_PREPARE_CHILD blocked=True; replayPerformed=False') -and $errTask.Result.Length -eq 0) 'actual new process cannot recover live mutation authority'
                    $process.Dispose(); $process = $null
                }
                Check-LegacyPreparation ((Get-LegacyPreparationFileHash -LiteralPath $primary).Hash -ceq $primaryHash) 'primary retained through Legacy restore; Startup/Menu mutation not authorized early'
                $lease.Dispose(); Check-LegacyPreparation (-not $lease.VerifyRestoredOutcome($physical)) 'disposed live context cannot authorize restoration'
                $lease = $null; $physical.Dispose(); $physical = $null; $cancel.Dispose(); $cancel = $null
            }
        }
        foreach ($path in $private.Keys) { Check-LegacyPreparation ((Get-LegacyPreparationFileHash -LiteralPath $path).Hash -ceq $private[$path]) 'private and foreign bytes preserved' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy preparation ' + $scenario)
    }
    Write-Host ('Legacy preparation tests PASS: ' + $cases.Count + '; fresh-process refusal=2; main orchestration/whole auxiliary/restart/power-loss NOT_RUN')
} finally {
    try { if ($null -ne $lease) { $lease.Dispose() } } finally { if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $cancel) { $cancel.Dispose() } }
    if ($null -ne $process) {
        try {
            if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Preparation child did not settle.' } }
            if ($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Preparation child streams did not drain.' }
        } finally { $process.Dispose() }
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ([IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-legacy-prepare-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Preparation cleanup refuses unowned scope.' }
        foreach ($path in @((Get-Item -LiteralPath $work -Force)) + @(Get-ChildItem -LiteralPath $work -Recurse -Force)) { if ($path.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Preparation cleanup refuses links.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
