param([switch]$BlockedChild,[string]$CaseDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Scoped auxiliary handlers do not parse.' }
foreach ($function in $ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-Auxiliary($Condition,[string]$Name) { if (-not $Condition) { throw ('Scoped auxiliary failed: case='+$scenario+'; check='+$Name) } }
function Get-AuxiliaryHash([string]$Path) {
    $stream=$null; $hash=$null
    try { $hash=[Security.Cryptography.SHA256]::Create(); $stream=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::Read -bor [IO.FileShare]::Delete)); return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','') }
    finally { if($null -ne $stream){$stream.Dispose()};if($null -ne $hash){$hash.Dispose()} }
}
if($BlockedChild) {
    $parent=[IO.Path]::GetDirectoryName($CaseDirectory)
    if([IO.Path]::GetFileName($CaseDirectory) -cnotin @('complete','disposed-outcome') -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-auxiliary-scoped-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Auxiliary child refuses unowned scope.'}
    foreach($path in @($parent,$CaseDirectory)){if([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint){throw 'Auxiliary child refuses redirected scope.'}}
    $app=Join-Path $CaseDirectory 'app';$programs=Join-Path $CaseDirectory 'Programs';$startup=Join-Path $CaseDirectory 'Startup'
    $journals=@(Get-ChildItem -LiteralPath (Join-Path $app 'backups') -Directory | Where-Object {$_.Name -cmatch '^installer-journal-[a-f0-9]{32}$'})
    $outcomes=@(Get-ChildItem -LiteralPath (Join-Path $app 'backups') -File | Where-Object {$_.Name -cmatch '^installer-auxiliary-scoped-[a-f0-9]{32}\.bin$'})
    if($journals.Count -ne 1 -or $outcomes.Count -ne 1 -or [IO.File]::ReadAllText($outcomes[0].FullName) -cnotmatch '^PROGO-AUXILIARY-SCOPED-1'){throw 'Child requires the actual valid parent result.'}
    $backup=[pscustomobject]@{Journal=[pscustomobject]@{Directory=$journals[0].FullName}}
    foreach($fake in @($null,[pscustomobject]@{AuxiliaryOutcomeRecorded=$true;AuxiliaryStateVerified=$true})) {
        $summary=Invoke-ProGoInstallerFailureAuxiliary 'synthetic child error' $backup $app $programs $startup $fake
        if($summary.AuxiliaryStateVerified -or $summary.AuxiliaryOutcomeRecorded -or $summary.RestartAuthority -or $summary.RuntimeRollbackAllowed){throw 'Durable file and serialized flags must not rehydrate authority.'}
    }
    Write-Host 'AUXILIARY_CHILD blocked=True; validFile=True; replay=False';return
}
if(-not ('LegacyRestoreReflectionFixture' -as [type]) -or -not ('LegacyMetadataThreatFixture' -as [type])){throw 'Live host must execute CLR/sharing/restore prerequisites first.'}
$actualPublication=(Get-Command Complete-ProGoInstallerAuxiliaryOutcome).ScriptBlock
function Complete-ProGoInstallerAuxiliaryOutcome($LegacyContext,$CancellationToken) {
    $fixture.Publications++
    Check-Auxiliary $LegacyContext.VerifyMenuSettlement() 'publication only after actual Legacy Startup Menu settlement'
    if($scenario -ceq 'error-before-publication'){throw 'synthetic between Menu and publication'}
    if($scenario -ceq 'cancel-before-publication'){$cancel.Cancel()}
    if($scenario -in @('source-before-publication','legacy-before-publication')){
        $script:metadata=if($scenario -ceq 'source-before-publication'){Join-Path (Join-Path $install 'backups') ('installer-legacy-source-'+$op+'.bin')}else{$legacy}
        $script:stamp=[IO.File]::GetCreationTimeUtc($script:metadata);[LegacyMetadataThreatFixture]::SetCreation($script:metadata,$script:stamp.AddMinutes(1))
    }
    if($scenario -ceq 'primary-before-publication'){[IO.File]::WriteAllText($primary,'synthetic foreign primary');$private[$primary]=Get-AuxiliaryHash $primary}
    if($scenario -in @('write','short-write','flush','readback','readback-throw','cancel-created','cancel-written','cancel-flushed','cancel-readback')) {
        $method=[ProGo.ApplicationShortcuts].GetMethod('RecordLiveAuxiliaryOutcome',[Reflection.BindingFlags]'NonPublic,Instance')
        return [LegacyRestoreReflectionFixture]::InvokeChecked($method,$manager,[object[]]@($LegacyContext,$cancel.Token,$scenario,$cancel))
    }
    $result=& $actualPublication $LegacyContext $CancellationToken
    if($scenario -ceq 'cancel-after-publication'){$cancel.Cancel()}
    if($scenario -ceq 'error-after-publication'){throw 'synthetic after durable publication'}
    return $result
}
$work=Join-Path ([IO.Path]::GetTempPath()) ('ProGo-auxiliary-scoped-'+[Guid]::NewGuid().ToString('N'))
$scenario='initial';$context=$null;$lease=$null;$physical=$null;$stage=$null;$cancel=$null;$metadata=$null;$stamp=$null;$process=$null;$outTask=$null;$errTask=$null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload=Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $cases=@('complete','preserved-startup','preserved-primary','write','short-write','flush','readback','readback-throw',
        'occupied-foreign','occupied-partial','equal-byte-replaced','source-tampered','legacy-tampered','primary-reappeared',
        'cancel-before-publication','cancel-created','cancel-written','cancel-flushed','cancel-readback','cancel-after-publication',
        'error-before-publication','error-after-publication','disposed-outcome','source-before-publication','legacy-before-publication','primary-before-publication')
    foreach($scenario in $cases) {
        $fixture=@{Publications=0};$case=Join-Path $work $scenario;$install=Join-Path $case 'app';$programs=Join-Path $case 'Programs';$startup=Join-Path $case 'Startup';$menu=Join-Path $programs 'ProGo'
        $primary=Join-Path $menu 'ProGo.lnk';$legacy=Join-Path $menu 'ProGo Status.lnk';$startupLink=Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        $private=@{};foreach($path in @((Join-Path $install 'settings.json'),(Join-Path $install 'vault.enc'),(Join-Path $install 'key-reference.txt'),(Join-Path $menu 'foreign.txt'),(Join-Path $startup 'foreign.txt'))){[IO.File]::WriteAllText($path,'synthetic private or foreign bytes');$private[$path]=Get-AuxiliaryHash $path}
        $manager=New-Object ProGo.ApplicationShortcuts($install,$programs,$startup);$manager.Install($true,$true,$false);[IO.File]::Move($primary,$legacy)
        if($scenario -ceq 'preserved-primary'){[IO.File]::WriteAllBytes($primary,[IO.File]::ReadAllBytes($legacy));$private[$primary]=Get-AuxiliaryHash $primary}
        $preserveStartup=$scenario -in @('preserved-startup','preserved-primary')
        if($preserveStartup){[IO.File]::WriteAllText($startupLink,'synthetic custom Startup');$private[$startupLink]=Get-AuxiliaryHash $startupLink}
        $backup=New-ProGoInstallerBackup $payload $install;$stage=New-ProGoInstallerStage $payload
        $writes=New-Object 'System.Collections.Generic.List[object]';foreach($entry in $backup.Entries){Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes}
        $prepared=Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $preserveStartup -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation
        $reference=New-ProGoInstallerLegacySourceReference $backup $programs $startup;Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup
        [void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        $physical=Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $install $programs $startup;$lease=Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $install $programs $startup
        $context=$manager.BindLegacyStartupContext($prepared,$lease,$physical);Check-Auxiliary ($null -ne $context) 'native preparation/same live pins binding'
        $deleted=$lease.DeleteOwnedOnce($physical,[Threading.CancellationToken]::None);$accepted=$lease.RecordAcceptedOutcome($physical,[Threading.CancellationToken]::None)
        Check-Auxiliary ($deleted.OperationAccepted -and $accepted.DurableOutcomeRecorded) 'actual owned deletion and accepted record'
        $op=$context.OperationId.ToString('N');$outcomePath=Join-Path (Join-Path $install 'backups') ('installer-auxiliary-scoped-'+$op+'.bin')
        $frozen=@{};foreach($file in Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Recurse -File){$frozen[$file.FullName]=Get-AuxiliaryHash $file.FullName}
        if($scenario -in @('occupied-foreign','occupied-partial')){[IO.File]::WriteAllText($outcomePath, $(if($scenario -ceq 'occupied-foreign'){'synthetic foreign outcome'}else{'PROGO-AUXILIARY'}));$private[$outcomePath]=Get-AuxiliaryHash $outcomePath}
        $cancel=New-Object Threading.CancellationTokenSource;$heldError=$null
        try{throw 'synthetic original install failure'}catch{$heldError=$_;$summary=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $install $programs $startup $context $cancel.Token}
        $expectedState=$scenario -cnotin @('preserved-primary','source-before-publication','legacy-before-publication','primary-before-publication');$expectedRecorded=$scenario -in @('complete','preserved-startup','equal-byte-replaced','source-tampered','legacy-tampered','primary-reappeared','cancel-after-publication','error-after-publication','disposed-outcome')
        Check-Auxiliary ($summary.AuxiliaryStateVerified -eq $expectedState -and $summary.AuxiliaryOutcomeRecorded -eq $expectedRecorded -and
            [Object]::ReferenceEquals($summary.OriginalError,$heldError) -and $heldError.Exception.Message -ceq 'synthetic original install failure') 'real full failure chain and consumed outcome preserve original error and independent state/durable truth'
        Check-Auxiliary ($summary.Message -ceq $(if($expectedRecorded){'install-failed; scoped-auxiliary-verified-recorded; runtime-unverified'}elseif($expectedState){'install-failed; scoped-auxiliary-verified-unrecorded; runtime-unverified'}else{'install-failed; scoped-auxiliary-blocked; runtime-unverified'})) 'failure message actually consumes both outcome flags'
        Check-Auxiliary (-not $summary.InstallSucceeded -and -not $summary.RuntimeRollbackAllowed -and -not $summary.BackupCleanupAllowed -and -not $summary.RestartAuthority -and -not $summary.WholeRollbackVerified) 'no runtime rollback cleanup success or restart authority'
        if($expectedState){Check-Auxiliary ($context.MenuDispositionAccepted -and -not [IO.File]::Exists($primary)) 'real Menu mutation precedes publication fault'}
        elseif($scenario -ceq 'preserved-primary'){Check-Auxiliary ([IO.File]::Exists($primary) -and -not $context.MenuDispositionAccepted -and -not [IO.File]::Exists($outcomePath) -and $summary.LegacyProgress.Created) 'preserved primary explicitly blocked and untouched after real restore, no whole auxiliary coverage'}
        else{Check-Auxiliary ($context.MenuDispositionAccepted -and -not [IO.File]::Exists($outcomePath) -and -not $summary.Publication.Attempted) 'changed proofs veto publication after real mutations without cleanup or foreign deletion'}
        if($scenario -in @('write','short-write','flush','readback','readback-throw','cancel-created','cancel-written','cancel-flushed','cancel-readback')) {
            $progress=$summary.Publication;Check-Auxiliary ($progress.Attempted -and $progress.Created -and -not $progress.AuxiliaryOutcomeRecorded -and [IO.File]::Exists($outcomePath)) 'actual new publication partial artifact and progress retained after real mutations'
            if($scenario -eq 'short-write'){Check-Auxiliary ($progress.BytesWritten -gt 0 -and -not $progress.Flushed) 'real short-write precedes flush'}
            if($scenario -in @('readback','readback-throw','cancel-flushed','cancel-readback')){Check-Auxiliary $progress.Flushed 'actual explicit Flush completed before late fault'}
            if($scenario -eq 'cancel-readback'){Check-Auxiliary $progress.ReadbackVerified 'late cancellation retains actual readback truth'}
        }
        if($scenario -ceq 'equal-byte-replaced'){
            [byte[]]$bytes=[IO.File]::ReadAllBytes($outcomePath);$creation=[IO.File]::GetCreationTimeUtc($outcomePath);[IO.File]::Move($outcomePath,(Join-Path $case 'retained-outcome'));[IO.File]::WriteAllBytes($outcomePath,$bytes);[IO.File]::SetCreationTimeUtc($outcomePath,$creation)
        }
        if($scenario -ceq 'source-tampered'){$metadata=Join-Path (Join-Path $install 'backups') ('installer-legacy-source-'+$op+'.bin');$stamp=[IO.File]::GetCreationTimeUtc($metadata);[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp.AddMinutes(1))}
        if($scenario -ceq 'legacy-tampered'){$metadata=$legacy;$stamp=[IO.File]::GetCreationTimeUtc($metadata);[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp.AddMinutes(1))}
        if($scenario -ceq 'primary-reappeared'){[IO.File]::WriteAllText($primary,'synthetic foreign primary');$private[$primary]=Get-AuxiliaryHash $primary}
        if($scenario -ceq 'disposed-outcome'){$context.Dispose();Check-Auxiliary $summary.Publication.AuxiliaryOutcomeRecorded 'historical durable flag survives disposal but is not capability'}
        $inventory=@{};foreach($file in Get-ChildItem -LiteralPath $case -Recurse -File){$inventory[$file.FullName]=Get-AuxiliaryHash $file.FullName}
        $calls=$fixture.Publications;$again=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $install $programs $startup $context ([Threading.CancellationToken]::None)
        $lost=$scenario -in @('source-tampered','legacy-tampered','primary-reappeared','disposed-outcome','source-before-publication','legacy-before-publication','primary-before-publication');$lostOutcome=$lost -or $scenario -ceq 'equal-byte-replaced'
        Check-Auxiliary ($again.AuxiliaryStateVerified -eq ($expectedState -and -not $lost) -and $again.AuxiliaryOutcomeRecorded -eq ($expectedRecorded -and -not $lostOutcome) -and $fixture.Publications -eq $calls -and
            [Object]::ReferenceEquals($again.OriginalError,$heldError) -and @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count) 'repeat current verification only; no replay cleanup adoption or original error loss'
        if($scenario -ceq 'equal-byte-replaced'){Check-Auxiliary ($summary.Publication.AuxiliaryOutcomeRecorded -and -not $again.AuxiliaryOutcomeRecorded) 'historical recorded flag never adopts equal-byte replacement'}
        foreach($path in $inventory.Keys){Check-Auxiliary ((Get-AuxiliaryHash $path) -ceq $inventory[$path]) 'repeat keeps every byte including partial/foreign outcome'}
        if($scenario -in @('complete','disposed-outcome')){
            $info=New-Object Diagnostics.ProcessStartInfo;$info.FileName=Join-Path $PSHOME 'powershell.exe';$info.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -BlockedChild -CaseDirectory "'+$case+'"';$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
            $process=[Diagnostics.Process]::Start($info);$outTask=$process.StandardOutput.ReadToEndAsync();$errTask=$process.StandardError.ReadToEndAsync()
            if(-not $process.WaitForExit(20000)){throw 'Auxiliary child exceeded bounded wait.'}
            Check-Auxiliary ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and $errTask.Result.Length -eq 0 -and $outTask.Result.Contains('AUXILIARY_CHILD blocked=True; validFile=True; replay=False')) 'fresh process with valid result and serialized flags remains blocked/manual'
            $process.Dispose();$process=$null
            foreach($path in $inventory.Keys){Check-Auxiliary ((Get-AuxiliaryHash $path) -ceq $inventory[$path]) 'fresh child no mutation'}
        }
        if($null -ne $metadata){[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp);$metadata=$null}
        foreach($path in $frozen.Keys){Check-Auxiliary ((Get-AuxiliaryHash $path) -ceq $frozen[$path]) 'original backup/source/evidence retained byte exact'}
        $context.Dispose();Check-Auxiliary (-not $context.VerifyAuxiliaryOutcome() -and -not $context.VerifyMenuSettlement()) 'disposed live proofs cannot regain authority'
        $context=$null;$lease=$null;$physical=$null;$cancel.Dispose();$cancel=$null
        foreach($path in $private.Keys){Check-Auxiliary ((Get-AuxiliaryHash $path) -ceq $private[$path]) 'private foreign preserved primary and Startup bytes unchanged'}
        foreach($file in Get-ChildItem -LiteralPath $case -Recurse -File){$probe=[IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$probe.Dispose()}
        Remove-ProGoInstallerStage $stage;$stage=$null;Write-Host ('PASS: scoped auxiliary outcome '+$scenario)
    }
    Write-Host ('Scoped auxiliary outcome tests PASS: '+$cases.Count+'; fresh-process refusal=2; preserved primary blocked; whole/runtime rollback/restart/power-loss NOT_RUN')
}finally{
    try{if($null -ne $metadata){[LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp)}}finally{try{if($null -ne $context){$context.Dispose()}}finally{if($null -ne $lease){$lease.Dispose()};if($null -ne $physical){$physical.Dispose()};if($null -ne $cancel){$cancel.Dispose()}}}
    if($null -ne $process){try{if(-not $process.HasExited){$process.Kill();if(-not $process.WaitForExit(5000)){throw 'Auxiliary child did not settle.'}};if($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)){throw 'Auxiliary child streams did not drain.'}}finally{$process.Dispose()}}
    if($null -ne $stage){Remove-ProGoInstallerStage $stage}
    if([IO.Directory]::Exists($work)){
        if([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-auxiliary-scoped-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Auxiliary cleanup refuses unowned scope.'}
        foreach($path in @((Get-Item -LiteralPath $work -Force))+@(Get-ChildItem -LiteralPath $work -Recurse -Force)){if($path.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Auxiliary cleanup refuses links.'}}
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
