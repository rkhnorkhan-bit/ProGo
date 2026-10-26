param([switch]$BlockedChild,[string]$CaseDirectory)
# Runs after existing PreDelete/MutationThreat fixtures, reusing only the test-only metadata shim.
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw 'Owned operation helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
if ($BlockedChild) {
    $parent = [IO.Path]::GetDirectoryName($CaseDirectory)
    if ([IO.Path]::GetFileName($CaseDirectory) -cne 'complete' -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-legacy-ownedop-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Owned operation child refuses unowned scope.' }
    $install = Join-Path $CaseDirectory 'app'; $programs = Join-Path $CaseDirectory 'Programs'; $startup = Join-Path $CaseDirectory 'Startup'
    $journals = @(Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-journal-[a-f0-9]{32}$' })
    if ($journals.Count -ne 1 -or $null -ne (Restore-ProGoInstallerLegacyReferenceEvidence $journals[0].FullName $install $programs $startup) -or
        $null -ne (Invoke-ProGoInstallerLegacyOwnedDeletion $journals[0].FullName $install $programs $startup)) { throw 'New process must not mint authority after unrecorded disposition.' }
    Write-Host 'LEGACY_OWNED_CHILD blocked=True; replayPerformed=False'
    return
}
if (-not ('LegacyMetadataThreatFixture' -as [type])) { throw 'Owned operation test metadata prerequisite is absent.' }
function Check-OwnedLegacy($Condition,[string]$Name) {
    if (-not $Condition) { throw ('Owned Legacy operation failed: case=' + $scenario + '; check=' + $Name) }
}
$ownedWork = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-legacy-ownedop-' + [Guid]::NewGuid().ToString('N'))
$process = $null; $outTask = $null; $errTask = $null; $stage = $null; $physical = $null; $original = $null; $cancel = $null; $metadataTarget = $null; $metadataTouched = $false
try {
    New-Item -ItemType Directory -Path $ownedWork | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $cases = @('complete','equal-byte-replacement','foreign-appeared-name','target-metadata-change','evidence-metadata-change',
        'accepted-context-change','accepted-persistence-fail','cancel-before','cancel-during-check','late-cancel-no-replay','disposed-lease','incomplete-physical-plan','missing-replacement','foreign-replacement','production-path')
    foreach ($scenario in $cases) {
        $case = Join-Path $ownedWork $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $legacy = Join-Path $menu 'ProGo Status.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        foreach ($leaf in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf),'synthetic private bytes') }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        [byte[]]$oldBytes = [IO.File]::ReadAllBytes($legacy)
        # Existing migration semantics require an actually usable replacement before removing Legacy.
        $primary = Join-Path $menu 'ProGo.lnk'; [IO.File]::WriteAllBytes($primary,$oldBytes)
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
        Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup
        [void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        $journal = $backup.Journal.Directory; $operation = $reference.Source.Operation.ToString('N')
        $referencePath = Join-Path (Join-Path $install 'backups') ('installer-legacy-reference-' + $operation + '.json')
        $appeared = Join-Path $backup.Directory 'VERSION'; $token = [Threading.CancellationToken]::None
        $cancel = New-Object Threading.CancellationTokenSource
        if ($scenario -ceq 'missing-replacement') { [IO.File]::Move($primary,(Join-Path $case 'retained-primary')) }
        if ($scenario -ceq 'foreign-replacement') { [IO.File]::WriteAllText($primary,'synthetic foreign replacement') }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { if ($file.FullName -cne $legacy) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash } }
        if ($scenario -ceq 'equal-byte-replacement') {
            $creation = [IO.File]::GetCreationTimeUtc($legacy); [IO.File]::Move($legacy,(Join-Path $case 'retained-original'))
            [IO.File]::WriteAllBytes($legacy,$oldBytes); [IO.File]::SetCreationTimeUtc($legacy,$creation)
            $result = Invoke-ProGoInstallerLegacyOwnedDeletion $journal $install $programs $startup
            Check-OwnedLegacy ($null -eq $result -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($legacy)) -ceq [Convert]::ToBase64String($oldBytes)) 'equal-byte foreign native object refused and preserved'
        } elseif ($scenario -ceq 'production-path') {
            $result = Invoke-ProGoInstallerLegacyOwnedDeletion $journal $install $programs $startup
            Check-OwnedLegacy ($null -ne $result -and $result.OperationAccepted -and $result.OperationAttempted -and $result.TargetHandleClosed -and
                -not $result.DurableOutcomeRecorded -and -not $result.WholeRollbackVerified -and -not [IO.File]::Exists($legacy)) 'actual production helper executes exact owned disposition without full success claim'
            Check-OwnedLegacy ($null -eq (Invoke-ProGoInstallerLegacyOwnedDeletion $journal $install $programs $startup)) 'production helper cannot replay absent original from intent'
        } elseif ($scenario -ceq 'cancel-before') {
            $cancel.Cancel(); $cancelled = $false
            try { Invoke-ProGoInstallerLegacyOwnedDeletion $journal $install $programs $startup $cancel.Token | Out-Null }
            catch { $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }; if ($cause -isnot [OperationCanceledException]) { throw }; $cancelled = $true }
            Check-OwnedLegacy ($cancelled -and [IO.File]::Exists($legacy)) 'cancel before admission is not accepted mutation'
            $retry = Invoke-ProGoInstallerLegacyOwnedDeletion $journal $install $programs $startup
            Check-OwnedLegacy ($null -ne $retry -and $retry.OperationAccepted -and $retry.OperationId -eq $reference.Source.Operation -and -not $retry.WholeRollbackVerified) 'explicit fresh retry preserves operation identity after pre-admission cancellation'
        } else {
            $physical = Open-ProGoInstallerLegacyPhysicalPins $journal $install $programs $startup
            $original = Open-ProGoInstallerLegacyDeletionPins $journal $install $programs $startup
            Check-OwnedLegacy ($null -ne $physical -and $null -ne $original -and $original.IsPinned) 'actual source evidence and target pinned'
            if ($scenario -ceq 'incomplete-physical-plan') {
                $physical.Dispose(); $physical = $manager.PinLegacyPhysicalMaterial([string[]]@('ProGo.exe'),$token)
            }
            if ($scenario -in @('target-metadata-change','evidence-metadata-change')) {
                $metadataTarget = if ($scenario -ceq 'target-metadata-change') { $legacy } else { $referencePath }
                $metadataCreation = [IO.File]::GetCreationTimeUtc($metadataTarget); $metadataTouched = $true
                [LegacyMetadataThreatFixture]::SetCreation($metadataTarget,$metadataCreation.AddMinutes(1))
            }
            if ($scenario -ceq 'foreign-appeared-name') { [IO.File]::WriteAllText($appeared,'synthetic foreign child') }
            if ($scenario -ceq 'disposed-lease') { $original.Dispose() }
            $calls = New-Object 'System.Collections.Generic.List[int]'
            $check = {
                $calls.Add(1)
                if ($scenario -ceq 'cancel-during-check') { $cancel.Cancel() }
                if ($scenario -ceq 'accepted-context-change' -and $calls.Count -eq 2) { [IO.File]::WriteAllText($appeared,'synthetic foreign child') }
                return $true # native plan + namespace verification remains mandatory, independent of this veto
            }.GetNewClosure()
            $result = $null; $cancelled = $false
            try { $result = $original.DeleteOwnedOnce($physical,([Func[bool]]$check),$cancel.Token) }
            catch { $cause = $_.Exception; while ($null -ne $cause.InnerException) { $cause = $cause.InnerException }; if ($cause -isnot [OperationCanceledException]) { throw }; $cancelled = $true }
            $expected = $scenario -in @('complete','accepted-context-change','accepted-persistence-fail','late-cancel-no-replay')
            if ($scenario -ceq 'cancel-during-check') {
                Check-OwnedLegacy ($cancelled -and [IO.File]::Exists($legacy)) 'cancellation under retained pins precedes native operation'
                $cancelledResult = $original.DeleteOwnedOnce($physical,([Func[bool]]$check),$token)
                Check-OwnedLegacy (-not $cancelledResult.OperationAccepted -and -not $cancelledResult.OperationAttempted -and $calls.Count -eq 1) 'cancelled session cannot replay native operation'
            }
            else {
                Check-OwnedLegacy ($null -ne $result -and $result.OperationAccepted -eq $expected -and $result.AcceptanceKnown) 'accepted is distinct from blocked or refused'
                Check-OwnedLegacy (-not $result.DurableOutcomeRecorded -and -not $result.WholeRollbackVerified -and -not $result.InstallSucceeded) 'no durable outcome or whole rollback fabricated'
                Check-OwnedLegacy ($result.OperationId -eq $reference.Source.Operation -and $result.OriginalOrigin -ceq $reference.Source.OriginIdentity -and $result.OriginalHash -ceq $reference.Source.Hash) 'in-memory result remains bound to exact admitted operation'
                if ($expected) {
                    Check-OwnedLegacy ($result.OperationAttempted -and $result.TargetHandleClosed -and -not [IO.File]::Exists($legacy)) 'own synthetic target disposition accepted without foreign path resolution'
                    Check-OwnedLegacy ($result.ContextObservedAfterOperation -eq ($scenario -cne 'accepted-context-change')) 'post-operation conflict never changes accepted into no-op'
                    if ($scenario -ceq 'accepted-persistence-fail') {
                        $sink = Join-Path $case 'occupied-outcome'; [IO.File]::WriteAllText($sink,'synthetic foreign persistence destination')
                        $failed = $false; $stream = $null
                        try { $stream = [IO.File]::Open($sink,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None) }
                        catch [IO.IOException] { $failed = $true } finally { if ($null -ne $stream) { $stream.Dispose() } }
                        Check-OwnedLegacy ($failed -and $result.OperationAccepted -and -not $result.DurableOutcomeRecorded -and [IO.File]::ReadAllText($sink) -ceq 'synthetic foreign persistence destination') 'actual caller persistence refusal preserves accepted unrecorded result and foreign bytes'
                    }
                    if ($scenario -ceq 'late-cancel-no-replay') { [IO.File]::WriteAllText($legacy,'synthetic new foreign target'); $cancel.Cancel() }
                    $again = $original.DeleteOwnedOnce($physical,([Func[bool]]$check),$cancel.Token)
                    Check-OwnedLegacy ([object]::ReferenceEquals($result,$again) -and $calls.Count -eq 2) 'cached accepted result joins once even after cancellation'
                    if ($scenario -ceq 'late-cancel-no-replay') { Check-OwnedLegacy ([IO.File]::ReadAllText($legacy) -ceq 'synthetic new foreign target') 'replay never touches newly appeared target name' }
                } else { Check-OwnedLegacy (-not $result.OperationAttempted -and [IO.File]::Exists($legacy)) 'invalid proof blocked before native operation' }
            }
            if ($metadataTouched) { [LegacyMetadataThreatFixture]::SetCreation($metadataTarget,$metadataCreation); $metadataTouched = $false; $metadataTarget = $null }
            $original.Dispose(); $original = $null; $physical.Dispose(); $physical = $null
            if ($scenario -ceq 'cancel-during-check') {
                $retry = Invoke-ProGoInstallerLegacyOwnedDeletion $journal $install $programs $startup
                Check-OwnedLegacy ($null -ne $retry -and $retry.OperationAccepted -and $retry.OperationId -eq $reference.Source.Operation) 'explicit fresh retry revalidates same accepted request after cancelled lease settles'
            }
            if ($expected) {
                Check-OwnedLegacy ($null -eq (Restore-ProGoInstallerLegacyReferenceEvidence $journal $install $programs $startup)) 'path-only rehydration remains blocked after unrecorded disposition'
            }
        }
        if ($scenario -ceq 'complete') {
            $info = New-Object Diagnostics.ProcessStartInfo
            $info.FileName = Join-Path $PSHOME 'powershell.exe'; $info.Arguments = '-NoProfile -NonInteractive -File "' + $PSCommandPath + '" -BlockedChild -CaseDirectory "' + $case + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(20000)) { throw 'Owned operation restart check exceeded bounded twenty-second contract.' }
            Check-OwnedLegacy ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and
                $outTask.Result.Contains('LEGACY_OWNED_CHILD blocked=True; replayPerformed=False') -and $errTask.Result.Length -eq 0) 'actual fresh process cannot reuse intent as mutation authority'
            $process.Dispose(); $process = $null
        }
        $cancel.Dispose(); $cancel = $null
        foreach ($path in $before.Keys) { Check-OwnedLegacy ([IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $before[$path]) 'runtime snapshot source journal private and foreign bytes retained' }
        if ([IO.File]::Exists($appeared)) { Check-OwnedLegacy ([IO.File]::ReadAllText($appeared) -ceq 'synthetic foreign child') 'foreign child not adopted or deleted' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy owned operation ' + $scenario)
    }
    Write-Host ('Legacy owned operation tests PASS: ' + $cases.Count + '; fresh-process refusal=1; durable outcome/restore/power-loss NOT_RUN')
} finally {
    try { if ($metadataTouched -and $null -ne $original) { [LegacyMetadataThreatFixture]::SetCreation($metadataTarget,$metadataCreation) } }
    finally {
        try { if ($null -ne $original) { $original.Dispose() } }
        finally { if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $cancel) { $cancel.Dispose() } }
    }
    if ($null -ne $process) {
        try {
            if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Owned operation child did not settle.' } }
            if ($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Owned operation streams did not drain.' }
        } finally { $process.Dispose() }
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ([IO.Directory]::Exists($ownedWork)) {
        if ([IO.Path]::GetFileName($ownedWork) -cnotmatch '^ProGo-legacy-ownedop-[a-f0-9]{32}$' -or
            [IO.Path]::GetDirectoryName($ownedWork) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Owned operation cleanup refuses unowned scope.' }
        foreach ($path in @((Get-Item -LiteralPath $ownedWork -Force)) + @(Get-ChildItem -LiteralPath $ownedWork -Recurse -Force)) { if ($path.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Owned operation cleanup refuses links.' } }
        Remove-Item -LiteralPath $ownedWork -Recurse -Force
    }
}
