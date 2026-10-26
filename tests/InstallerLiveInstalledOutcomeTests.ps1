param([switch]$BlockedChild,[string]$CaseDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$Root=Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1');Import-ProGoShortcutType
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Live installed handlers do not parse.'}
foreach($function in $ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')},$false)){. ([scriptblock]::Create($function.Extent.Text))}
function Check-LiveInstalled($Condition,[string]$Name){if(-not $Condition){throw ('Live installed failed: case='+$scenario+'; check='+$Name)}}
function Get-LiveInstalledHash([string]$Path){$stream=$null;$hash=$null;try{$hash=[Security.Cryptography.SHA256]::Create();$stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::Read -bor [IO.FileShare]::Delete));return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','')}finally{if($null -ne $stream){$stream.Dispose()};if($null -ne $hash){$hash.Dispose()}}}
if($BlockedChild){
    $parent=[IO.Path]::GetDirectoryName($CaseDirectory)
    if([IO.Path]::GetFileName($CaseDirectory) -cnotin @('complete','context-lost-after-publish') -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-live-installed-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Live installed child refuses unowned scope.'}
    foreach($path in @($parent,$CaseDirectory)){if([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint){throw 'Live installed child refuses redirected scope.'}}
    $app=Join-Path $CaseDirectory 'app';$journals=@(Get-ChildItem -LiteralPath (Join-Path $app 'backups') -Directory | Where-Object {$_.Name -cmatch '^installer-journal-[a-f0-9]{32}$'})
    if($journals.Count -ne 1){throw 'Child journal mismatch.'};$journal=$journals[0].FullName
    $doc=Read-ProGoInstallerJson (Join-Path $journal 'installed.json')
    if($null -eq $doc.Completion.LiveLegacy -or (Test-ProGoInstallerInstalledOutcome $journal $app) -or (Test-ProGoInstallerPendingInstalledProof $journal $app)){throw 'Live terminal file cannot authorize fresh receipt-only reader.'}
    foreach($fake in @($null,[pscustomobject]@{InstalledOutcomeRecorded=$true;InstalledBinding=$doc.Completion.LiveLegacy})){
        $backup=[pscustomobject]@{InstallDirectory=$app;Journal=[pscustomobject]@{Directory=$journal}}
        $summary=Invoke-ProGoInstallerSuccess $backup @() $doc.Completion.Startup (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup') $fake
        if($summary.StateVerified -or $summary.InstalledOutcomeRecorded -or $summary.ScopedSuccessVerified -or $summary.RestartAuthority){throw 'Serialized evidence must remain blocked without live proofs.'}
    }
    Write-Host 'LIVE_INSTALLED_CHILD blocked=True; validFile=True; replay=False';return
}
if(-not ('LegacyRestoreReflectionFixture' -as [type])){throw 'Live host must execute CLR regression/restore prerequisites first.'}
$renameType=[ProGo.ApplicationShortcuts].GetNestedType('InstalledRenameInfo',[Reflection.BindingFlags]'NonPublic')
Check-LiveInstalled ([Runtime.InteropServices.Marshal]::OffsetOf($renameType,'FileName').ToInt32() -eq $(if([IntPtr]::Size -eq 8){20}else{12})) 'native FILE_RENAME_INFO layout matches process architecture'
function New-LiveInstalledForeignContext($Payload,[string]$Base) {
    $app=Join-Path $Base 'foreign-app';$programs=Join-Path $Base 'foreign-programs';$startup=Join-Path $Base 'foreign-startup';$menu=Join-Path $programs 'ProGo'
    $stage=$null;$lease=$null;$physical=$null
    try {
        New-Item -ItemType Directory -Path $app,$menu,$startup | Out-Null;[IO.File]::WriteAllText((Join-Path $app 'ProGo.exe'),'synthetic foreign executable')
        $manager=New-Object ProGo.ApplicationShortcuts($app,$programs,$startup);$manager.Install($true,$true,$false);[IO.File]::Move((Join-Path $menu 'ProGo.lnk'),(Join-Path $menu 'ProGo Status.lnk'))
        $backup=New-ProGoInstallerBackup $Payload $app;$stage=New-ProGoInstallerStage $Payload;$writes=New-Object 'System.Collections.Generic.List[object]'
        foreach($entry in $backup.Entries){Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes}
        $prepared=Initialize-ProGoShortcuts -InstallDirectory $app -WasInstalled $false -NoStartup $false -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation
        $reference=New-ProGoInstallerLegacySourceReference $backup $programs $startup;Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup;[void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        $physical=Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $app $programs $startup;$lease=Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $app $programs $startup
        $context=$manager.BindLegacyStartupContext($prepared,$lease,$physical);if($null -eq $context){throw 'Foreign native context fixture failed to bind.'}
        $result=[pscustomobject]@{Context=$context;Stage=$stage;Legacy=(Join-Path $menu 'ProGo Status.lnk')};$physical=$null;$lease=$null;$stage=$null;return $result
    } finally {if($null -ne $lease){$lease.Dispose()};if($null -ne $physical){$physical.Dispose()};if($null -ne $stage){Remove-ProGoInstallerStage $stage}}
}
$actualConfirm=(Get-Command Confirm-ProGoInstallerLiveInstalledPending).ScriptBlock
$actualPublish=(Get-Command Publish-ProGoInstallerLiveInstalledOutcome).ScriptBlock
function Write-ProGoInstallerOutcomeBytes($Stream,[byte[]]$Bytes){
    $fixture.Writes++
    if($scenario -ceq 'write'){throw 'synthetic installed Write fault'}
    if($scenario -ceq 'short-write'){$Stream.Write($Bytes,0,[Math]::Max(1,[int]($Bytes.Length/2)));return}
    $Stream.Write($Bytes,0,$Bytes.Length)
}
function Confirm-ProGoInstallerLiveInstalledPending($LiveContext,$Stream,[string]$Text,$CancellationToken){
    $fixture.Seals++
    $LiveContext.ObserveInstalledPending($Stream)
    Check-LiveInstalled ($LiveContext.VerifyAcceptedInstallState() -and -not $lease.VerifyAcceptedOutcome($physical)) 'own current pending writer is admitted only through same live typed context; old receipt-only namespace stays strict'
    if($scenario -in @('flush','readback')){
        $method=[ProGo.ApplicationShortcuts].GetMethod('SealLiveInstalledPending',[Reflection.BindingFlags]'NonPublic,Instance')
        return [LegacyRestoreReflectionFixture]::InvokeChecked($method,$manager,[object[]]@($LiveContext,$Stream,$Text,$CancellationToken,$scenario,$cancel))
    }
    $sealed=& $actualConfirm $LiveContext $Stream $Text $CancellationToken
    if($sealed){Check-LiveInstalled ($LiveContext.VerifyAcceptedInstallState() -and $LiveContext.InstalledFlushVerified -and $LiveContext.InstalledReadbackVerified) 'same writer namespace survives native seal Flush and exact readback'}
    return $sealed
}
function Publish-ProGoInstallerLiveInstalledOutcome($LiveContext,$CancellationToken){
    $fixture.Publishes++
    Check-LiveInstalled ($LiveContext.VerifyAcceptedInstallState() -and $LiveContext.VerifyInstalledMetadata('outcome.pending.json') -and -not $lease.VerifyAcceptedOutcome($physical)) 'closed owned pending survives current proof before exact rename without receipt-only bypass'
    if($scenario -ceq 'extra-after-seal'){$extra=Join-Path $journal 'foreign-extra.bin';[IO.File]::WriteAllText($extra,'synthetic extra');$private[$extra]=Get-LiveInstalledHash $extra}
    if($scenario -ceq 'pending-bytes-changed'){$b=[IO.File]::ReadAllBytes($pending);$b[0]=$b[0] -bxor 1;[IO.File]::WriteAllBytes($pending,$b);$private[$pending]=Get-LiveInstalledHash $pending}
    if($scenario -ceq 'cancel-before-publish'){$cancel.Cancel()}
    if($scenario -ceq 'foreign-target'){[IO.File]::WriteAllText($installed,'synthetic foreign installed result');$private[$installed]=Get-LiveInstalledHash $installed}
    if($scenario -ceq 'pending-replaced'){
        [byte[]]$bytes=[IO.File]::ReadAllBytes($pending);$creation=[IO.File]::GetCreationTimeUtc($pending);$retained=Join-Path $case 'retained-pending'
        [IO.File]::Move($pending,$retained);[IO.File]::WriteAllBytes($pending,$bytes);[IO.File]::SetCreationTimeUtc($pending,$creation);$private[$pending]=Get-LiveInstalledHash $pending
    }
    $observed=& $actualPublish $LiveContext $CancellationToken
    if($observed){Check-LiveInstalled ($LiveContext.VerifyAcceptedInstallState() -and $LiveContext.VerifyInstalledOutcome() -and -not $lease.VerifyAcceptedOutcome($physical)) 'same exact native object admitted at installed state after rename and final verification; old API strict'}
    if($scenario -ceq 'installed-extra'){$extra=Join-Path $journal 'foreign-extra.bin';[IO.File]::WriteAllText($extra,'synthetic extra');$private[$extra]=Get-LiveInstalledHash $extra}
    if($scenario -ceq 'cancel-after-publish'){$cancel.Cancel()}
    if($scenario -ceq 'context-lost-after-publish'){$context.Dispose()}
    if($scenario -ceq 'installed-replaced'){
        [byte[]]$bytes=[IO.File]::ReadAllBytes($installed);$creation=[IO.File]::GetCreationTimeUtc($installed);[IO.File]::Move($installed,(Join-Path $case 'retained-installed'));[IO.File]::WriteAllBytes($installed,$bytes);[IO.File]::SetCreationTimeUtc($installed,$creation);$private[$installed]=Get-LiveInstalledHash $installed
    }
    if($scenario -ceq 'error-after-publish'){throw 'synthetic error after accepted installed rename'}
    return $observed
}
$work=Join-Path ([IO.Path]::GetTempPath()) ('ProGo-live-installed-'+[Guid]::NewGuid().ToString('N'))
$foreignContext=$null;$foreignStage=$null
$scenario='initial';$context=$null;$lease=$null;$physical=$null;$stage=$null;$cancel=$null;$metadata=$null;$stamp=$null;$process=$null;$outTask=$null;$errTask=$null
try{
    New-Item -ItemType Directory -Path $work | Out-Null;$payload=Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $cases=@('complete','preserved-primary','preserved-startup','preserved-both','absent-startup','unsupported-absent','unsupported-custom','unsupported-optout',
        'wrong-operation','wrong-root','foreign-context','stale-evidence','serialized-context','disposed-context','failure-result','receipt-tampered','source-tampered',
        'primary-equal-byte','startup-equal-byte','custom-startup-equal-byte','reappeared-legacy','write','short-write','flush','readback','occupied-pending','foreign-target',
        'pending-replaced','installed-replaced','cancel-before','cancel-before-publish','cancel-after-publish','error-after-publish','context-lost-after-publish','preexisting-installed','extra-journal','extra-after-seal','pending-bytes-changed','installed-extra','unowned-stream')
    foreach($scenario in $cases){
        $fixture=@{Writes=0;Seals=0;Publishes=0};$case=Join-Path $work $scenario;$app=Join-Path $case 'app';$programs=Join-Path $case 'Programs';$startup=Join-Path $case 'Startup';$menu=Join-Path $programs 'ProGo'
        $primary=Join-Path $menu 'ProGo.lnk';$legacy=Join-Path $menu 'ProGo Status.lnk';$startupLink=Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $app,$menu,$startup | Out-Null;[IO.File]::WriteAllText((Join-Path $app 'ProGo.exe'),'synthetic old executable')
        $private=@{};foreach($path in @((Join-Path $app 'settings.json'),(Join-Path $app 'vault.enc'),(Join-Path $app 'key-reference.txt'),(Join-Path $menu 'foreign.txt'),(Join-Path $startup 'foreign.txt'))){[IO.File]::WriteAllText($path,'synthetic private or foreign bytes');$private[$path]=Get-LiveInstalledHash $path}
        $manager=New-Object ProGo.ApplicationShortcuts($app,$programs,$startup);$manager.Install($true,$true,$false);[IO.File]::Move($primary,$legacy)
        if($scenario -in @('preserved-primary','preserved-both')){[IO.File]::WriteAllBytes($primary,[IO.File]::ReadAllBytes($legacy))}
        $preserveStartup=$scenario -in @('preserved-startup','preserved-both','custom-startup-equal-byte','absent-startup')
        if($preserveStartup -and $scenario -cne 'absent-startup'){[IO.File]::WriteAllText($startupLink,'synthetic customized Startup')}
        if($scenario -ceq 'unsupported-absent'){[IO.File]::Delete($legacy)}
        if($scenario -ceq 'unsupported-custom'){[IO.File]::WriteAllText($legacy,'synthetic custom Legacy')}
        if($scenario -in @('unsupported-custom','unsupported-optout')){$private[$legacy]=Get-LiveInstalledHash $legacy}
        $backup=New-ProGoInstallerBackup $payload $app;$stage=New-ProGoInstallerStage $payload;$writes=New-Object 'System.Collections.Generic.List[object]'
        foreach($entry in $backup.Entries){Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes}
        $prepared=Initialize-ProGoShortcuts -InstallDirectory $app -WasInstalled $false -NoStartup $preserveStartup -NoStartMenu ($scenario -ceq 'unsupported-optout') -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation
        $unsupported=$scenario.StartsWith('unsupported-',[StringComparison]::Ordinal)
        if(-not $unsupported){
            $reference=New-ProGoInstallerLegacySourceReference $backup $programs $startup;Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup;[void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
            $physical=Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $app $programs $startup;$lease=Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $app $programs $startup
            $context=$manager.BindLegacyStartupContext($prepared,$lease,$physical);Check-LiveInstalled ($null -ne $context) 'same live native preparation binding before mutation'
            $deleted=$lease.DeleteOwnedOnce($physical,[Threading.CancellationToken]::None);$accepted=$lease.RecordAcceptedOutcome($physical,[Threading.CancellationToken]::None)
            Check-LiveInstalled ($deleted.OperationAccepted -and $accepted.DurableOutcomeRecorded -and $context.VerifyAcceptedInstallState()) 'actual accepted deletion and receipt, current normal state not restored state'
        }
        $journal=$backup.Journal.Directory;$op=[IO.Path]::GetFileName($journal).Substring('installer-journal-'.Length);$pending=Join-Path $journal 'outcome.pending.json';$installed=Join-Path $journal 'installed.json'
        $cancel=New-Object Threading.CancellationTokenSource
        if($scenario -ceq 'disposed-context'){$context.Dispose()}
        if($scenario -ceq 'failure-result'){[void](Invoke-ProGoInstallerFailureAuxiliary 'synthetic original failure' $backup $app $programs $startup $context);Check-LiveInstalled (-not $context.VerifyAcceptedInstallState()) 'failure auxiliary outcome not installed-success evidence'}
        if($scenario -ceq 'receipt-tampered'){[IO.File]::WriteAllText((Join-Path (Join-Path $app 'backups') ('installer-legacy-accepted-'+$op+'.bin')),'synthetic tampered accepted receipt')}
        if($scenario -ceq 'source-tampered'){$metadata=Join-Path (Join-Path $app 'backups') ('installer-legacy-source-'+$op+'.bin');$stamp=[IO.File]::GetCreationTimeUtc($metadata);[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp.AddMinutes(1))}
        if($scenario -in @('primary-equal-byte','startup-equal-byte','custom-startup-equal-byte')){
            $path=if($scenario -ceq 'primary-equal-byte'){$primary}else{$startupLink};[byte[]]$bytes=[IO.File]::ReadAllBytes($path);$creation=[IO.File]::GetCreationTimeUtc($path)
            [IO.File]::Move($path,(Join-Path $case 'retained-native-object'));[IO.File]::WriteAllBytes($path,$bytes);[IO.File]::SetCreationTimeUtc($path,$creation)
        }
        if($scenario -ceq 'reappeared-legacy'){[IO.File]::WriteAllText($legacy,'synthetic foreign Legacy');$private[$legacy]=Get-LiveInstalledHash $legacy}
        if($scenario -ceq 'occupied-pending'){[IO.File]::WriteAllText($pending,'synthetic foreign partial pending');$private[$pending]=Get-LiveInstalledHash $pending}
        if($scenario -in @('preexisting-installed','extra-journal','unowned-stream')){
            $extra=if($scenario -ceq 'preexisting-installed'){$installed}elseif($scenario -ceq 'unowned-stream'){$pending}else{Join-Path $journal 'foreign-extra.bin'}
            [IO.File]::WriteAllText($extra,'synthetic preexisting foreign bytes')
            if($scenario -ceq 'unowned-stream'){
                $unowned=[IO.File]::Open($extra,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
                try{$context.ObserveInstalledPending($unowned);Check-LiveInstalled (-not $context.InstalledPendingCreated) 'arbitrary existing stream cannot obtain CreateNew provenance'}finally{$unowned.Dispose()}
            }
            $private[$extra]=Get-LiveInstalledHash $extra
        }
        $callBackup=$backup;$callEvidence=$prepared;$callContext=$context
        if($scenario -ceq 'wrong-operation'){$callBackup=[pscustomobject]@{InstallDirectory=$app;Journal=[pscustomobject]@{Directory=Join-Path (Join-Path $app 'backups') ('installer-journal-'+[Guid]::NewGuid().ToString('N'))}}}
        if($scenario -ceq 'wrong-root'){$callBackup=[pscustomobject]@{InstallDirectory=Join-Path $case 'foreign-app';Journal=$backup.Journal}}
        if($scenario -ceq 'foreign-context'){$foreign=New-LiveInstalledForeignContext $payload $case;$foreignContext=$foreign.Context;$foreignStage=$foreign.Stage;$callContext=$foreignContext;$private[$foreign.Legacy]=Get-LiveInstalledHash $foreign.Legacy;Check-LiveInstalled ($foreignContext.OperationId -ne $context.OperationId) 'different real native operation cannot act for current journal'}
        if($scenario -ceq 'stale-evidence'){$callEvidence=[pscustomobject]@{Schema=1;InstallIdentity=$prepared.InstallIdentity;Menu=$prepared.Menu;Legacy=$prepared.Legacy}}
        if($scenario -ceq 'serialized-context'){$callContext=[pscustomobject]@{InstalledOutcomeRecorded=$true;OperationId=$context.OperationId}}
        if($scenario -ceq 'cancel-before'){$cancel.Cancel()}
        $frozen=@{};foreach($file in Get-ChildItem -LiteralPath (Join-Path $app 'backups') -Recurse -File){$frozen[$file.FullName]=Get-LiveInstalledHash $file.FullName}
        $heldError=$null;if($scenario -in @('write','short-write','flush','readback','cancel-before','cancel-before-publish','cancel-after-publish','error-after-publish')){try{throw 'synthetic original installer error'}catch{$heldError=$_}}
        $summary=Invoke-ProGoInstallerSuccess $callBackup $writes $callEvidence $programs $startup $callContext $cancel.Token $heldError
        $stateExpected=$scenario -notin @('unsupported-absent','unsupported-custom','unsupported-optout','wrong-operation','wrong-root','foreign-context','stale-evidence','serialized-context','disposed-context','failure-result','receipt-tampered','source-tampered','primary-equal-byte','startup-equal-byte','custom-startup-equal-byte','reappeared-legacy','context-lost-after-publish','occupied-pending','foreign-target','pending-replaced','installed-replaced','preexisting-installed','extra-journal','extra-after-seal','pending-bytes-changed','installed-extra','unowned-stream')
        $recordExpected=$scenario -in @('complete','preserved-primary','preserved-startup','preserved-both','absent-startup','cancel-after-publish','error-after-publish')
        # Whitelisted scalar diagnostics before the first assertion; never error messages, paths or document content.
        $safeDiagnostic=[ordered]@{Case=$scenario;ExpectedState=$stateExpected;ExpectedRecorded=$recordExpected}
        foreach($name in @('Status','StateVerified','InstalledOutcomeRecorded','CompletionErrorObserved','CancellationObserved','ScopedSuccessVerified',
            'PendingCreated','BytesWritten','FlushVerified','ReadbackVerified','RenameAccepted','DiagnosticStage','DiagnosticExceptionType','DiagnosticHResult',
            'DiagnosticExceptionChain','NativeDiagnosticStage','NativeDiagnosticExceptionType','NativeDiagnosticHResult','NativeDiagnosticWin32')){$safeDiagnostic[$name]=$summary.$name}
        Write-Host ('LIVE_INSTALLED_DIAGNOSTIC '+($safeDiagnostic|ConvertTo-Json -Compress))
        Check-LiveInstalled ($summary.StateVerified -eq $stateExpected -and $summary.InstalledOutcomeRecorded -eq $recordExpected) 'real success handler consumes current live state and existing installed transaction result'
        Check-LiveInstalled ($summary.ScopedSuccessVerified -eq ($scenario -in @('complete','preserved-primary','preserved-startup','preserved-both','absent-startup'))) 'handler distinguishes scoped successful metadata transaction from interruptions and actual installation'
        Check-LiveInstalled (-not $summary.ActualInstallationCompleted -and -not $summary.InstallSucceeded -and -not $summary.RuntimeRollbackAllowed -and -not $summary.BackupCleanupAllowed -and -not $summary.RestartAuthority) 'scoped outcome is not complete live installation rollback cleanup or restart authority'
        if($null -ne $heldError){Check-LiveInstalled ([Object]::ReferenceEquals($summary.OriginalError,$heldError) -and -not $summary.ScopedSuccessVerified) 'original error/late cancellation is not converted to success'}
        if($scenario -ceq 'write'){Check-LiveInstalled ($summary.DiagnosticStage -ceq 'pending-write' -and $summary.CompletionErrorObserved -and $summary.DiagnosticExceptionChain.Count -gt 0) 'diagnostic Write failure stage and bounded safe exception types are observed'}
        if($scenario -ceq 'flush'){Check-LiveInstalled ($summary.DiagnosticStage -ceq 'pending-seal' -and $summary.NativeDiagnosticStage -ceq 'seal-cancellation' -and $summary.NativeDiagnosticExceptionType -ceq 'System.IO.IOException') 'actual synthetic Flush exception is localized before Flush without claiming Win32 failure'}
        if($scenario -ceq 'readback'){Check-LiveInstalled ($summary.DiagnosticStage -ceq 'pending-seal' -and $summary.NativeDiagnosticStage -ceq 'seal-readback') 'actual readback mismatch stage survives catch and current summary verification'}
        if($scenario -in @('write','short-write','flush','readback')){
            Check-LiveInstalled ($summary.PendingCreated -and [IO.File]::Exists($pending) -and -not $summary.InstalledOutcomeRecorded) 'real Write/short-write/Flush/readback fault retains pending artifact and truthful progress'
            if($scenario -ceq 'short-write'){Check-LiveInstalled ($summary.BytesWritten -gt 0 -and -not $summary.ReadbackVerified) 'actual short bytes cannot be readback success'}
            if($scenario -ceq 'readback'){Check-LiveInstalled ($summary.FlushVerified -and -not $summary.ReadbackVerified) 'actual Flush precedes mismatching readback'}
        }
        if($recordExpected){Check-LiveInstalled ((Test-ProGoInstallerInstalledOutcome $journal $app 'installed.json' $context) -and -not (Test-ProGoInstallerInstalledOutcome $journal $app) -and -not [IO.File]::Exists($pending)) 'same fixed installed.json succeeds only with live evidence, old receipt-only API refuses'}
        if($recordExpected){Check-LiveInstalled (-not (Test-ProGoInstallerInstalledOutcome $journal (Join-Path $case 'foreign-app') 'installed.json' $context)) 'same live context cannot authorize reader at a foreign install root'}
        $inventory=@{};foreach($file in Get-ChildItem -LiteralPath $case -Recurse -File){$inventory[$file.FullName]=Get-LiveInstalledHash $file.FullName}
        $callCounts=@($fixture.Writes,$fixture.Seals,$fixture.Publishes)
        $again=Invoke-ProGoInstallerSuccess $callBackup $writes $callEvidence $programs $startup $callContext ([Threading.CancellationToken]::None) $heldError
        Check-LiveInstalled ($again.StateVerified -eq $stateExpected -and $again.InstalledOutcomeRecorded -eq $recordExpected -and
            ($callCounts -join ',') -ceq (@($fixture.Writes,$fixture.Seals,$fixture.Publishes) -join ',') -and @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count) 'repeat observes only, never retries publication/deletion or cleans partial artifact'
        foreach($path in $inventory.Keys){Check-LiveInstalled ((Get-LiveInstalledHash $path) -ceq $inventory[$path]) 'repeat preserves all bytes'}
        if($scenario -in @('complete','context-lost-after-publish')){
            $info=New-Object Diagnostics.ProcessStartInfo;$info.FileName=Join-Path $PSHOME 'powershell.exe';$info.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -BlockedChild -CaseDirectory "'+$case+'"';$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
            $process=[Diagnostics.Process]::Start($info);$outTask=$process.StandardOutput.ReadToEndAsync();$errTask=$process.StandardError.ReadToEndAsync();if(-not $process.WaitForExit(20000)){throw 'Live installed child exceeded bounded wait.'}
            Check-LiveInstalled ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and $errTask.Result.Length -eq 0 -and $outTask.Result.Contains('LIVE_INSTALLED_CHILD blocked=True; validFile=True; replay=False')) 'valid installed file/serialized flags never rehydrate authority'
            $process.Dispose();$process=$null
        }
        if($null -ne $metadata){[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp);$metadata=$null}
        foreach($path in $frozen.Keys){Check-LiveInstalled ((Get-LiveInstalledHash $path) -ceq $frozen[$path]) 'source evidence backup and foreign pending remain intact'}
        if($null -ne $context){$context.Dispose();Check-LiveInstalled (-not $context.VerifyAcceptedInstallState() -and -not $context.VerifyInstalledOutcome()) 'disposed proofs block despite historical flags'}
        if($null -ne $foreignContext){$foreignContext.Dispose();$foreignContext=$null};if($null -ne $foreignStage){Remove-ProGoInstallerStage $foreignStage;$foreignStage=$null}
        if($null -ne $lease){$lease.Dispose()};if($null -ne $physical){$physical.Dispose()};$context=$null;$lease=$null;$physical=$null;$cancel.Dispose();$cancel=$null
        foreach($path in $private.Keys){Check-LiveInstalled ((Get-LiveInstalledHash $path) -ceq $private[$path]) 'private foreign and unsupported Legacy bytes intact'}
        foreach($file in Get-ChildItem -LiteralPath $case -Recurse -File){$probe=[IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$probe.Dispose()}
        Remove-ProGoInstallerStage $stage;$stage=$null;Write-Host ('PASS: live installed transaction '+$scenario)
    }
    Write-Host ('Live installed transaction tests PASS: '+$cases.Count+'; fresh-process refusal=2; default/actual installation/runtime rollback/restart/power-loss NOT_RUN')
}finally{
    try{if($null -ne $metadata){[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp)}}finally{try{if($null -ne $context){$context.Dispose()}}finally{if($null -ne $lease){$lease.Dispose()};if($null -ne $physical){$physical.Dispose()};if($null -ne $cancel){$cancel.Dispose()}}}
    if($null -ne $process){try{if(-not $process.HasExited){$process.Kill();if(-not $process.WaitForExit(5000)){throw 'Live installed child did not settle.'}};if($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)){throw 'Live installed child streams did not drain.'}}finally{$process.Dispose()}}
    if($null -ne $foreignContext){$foreignContext.Dispose()};if($null -ne $foreignStage){Remove-ProGoInstallerStage $foreignStage}
    if($null -ne $stage){Remove-ProGoInstallerStage $stage}
    if([IO.Directory]::Exists($work)){if([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-live-installed-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Live installed cleanup refuses unowned scope.'};foreach($path in @((Get-Item -LiteralPath $work -Force))+@(Get-ChildItem -LiteralPath $work -Recurse -Force)){if($path.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Live installed cleanup refuses links.'}};Remove-Item -LiteralPath $work -Recurse -Force}
}
