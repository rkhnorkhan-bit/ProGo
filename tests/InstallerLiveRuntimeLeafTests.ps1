param([switch]$BlockedChild,[string]$CaseDirectory)
Set-StrictMode -Version 2.0
$ErrorActionPreference='Stop'
$Root=Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1');Import-ProGoShortcutType
$tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Runtime fixture installer handlers do not parse.'}
foreach($fn in $ast.FindAll({param($node)$node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')},$false)){. ([scriptblock]::Create($fn.Extent.Text))}
function Check-RuntimeLeaf($Condition,[string]$Name){if(-not $Condition){throw ('Runtime fixture failed: '+$Name)}}
function Get-RuntimeFixtureHash([string]$Path){$h=[Security.Cryptography.SHA256]::Create();$f=$null;try{$f=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::Read -bor [IO.FileShare]::Delete));return [BitConverter]::ToString($h.ComputeHash($f)).Replace('-','')}finally{if($null -ne $f){$f.Dispose()};$h.Dispose()}}
function Get-RuntimeFixtureOrigin([string]$Path){
    $f=[IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::Read -bor [IO.FileShare]::Delete))
    try{return [RuntimeLeafFixture]::Origin($f.SafeFileHandle,[ProGo.ApplicationShortcuts])}finally{$f.Dispose()}
}
if($BlockedChild){
    $parent=[IO.Path]::GetDirectoryName($CaseDirectory)
    if([IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-runtime-leaf-[a-f0-9]{32}$' -or [IO.Path]::GetFileName($CaseDirectory) -cnotin @('complete','multi-complete') -or [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Runtime child scope refused.'}
    foreach($p in @($parent,$CaseDirectory)){if([IO.File]::GetAttributes($p) -band [IO.FileAttributes]::ReparsePoint){throw 'Runtime child refuses redirect.'}}
    $app=Join-Path $CaseDirectory 'app';$backup=[pscustomobject]@{InstallDirectory=$app;Journal=[pscustomobject]@{Directory=Join-Path (Join-Path $app 'backups') 'untrusted'}}
    $receipts=@(Get-ChildItem -LiteralPath (Join-Path $app 'backups') -File -Filter 'installer-auxiliary-scoped-*.bin')
    if($receipts.Count -ne 1 -or -not [IO.File]::ReadAllText($receipts[0].FullName).StartsWith('PROGO-AUXILIARY-SCOPED-1')){throw 'Valid synthetic auxiliary result required for child refusal.'}
    $summary=Invoke-ProGoInstallerFailureAuxiliary 'synthetic child failure' $backup $app (Join-Path $CaseDirectory 'Programs') (Join-Path $CaseDirectory 'Startup') $null -RestoreSingleRuntimeLeaf
    if($summary.RuntimeLeafVerified -or $summary.AuxiliaryOutcomeRecorded -or $null -ne $summary.RuntimeLeafProgress){throw 'Fresh process must not rehydrate runtime authority.'}
    Write-Host 'RUNTIME_CHILD blocked=True';return
}
if($env:GITHUB_ACTIONS -ne 'true'){throw 'Runtime native fixtures require isolated CI.'}
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class RuntimeLeafFixture {
    [DllImport("kernel32.dll", EntryPoint="CreateFileW", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern SafeFileHandle Open(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool SetFileTime(SafeFileHandle file,ref System.Runtime.InteropServices.ComTypes.FILETIME creation,IntPtr access,IntPtr write);
    public static void SetCreation(string path,DateTime value) {
        using (var handle=Open(path,0x100u,7,IntPtr.Zero,3,0x00200080u,IntPtr.Zero)) {
            if(handle.IsInvalid) throw new IOException("Fixture metadata open failed."); long t=value.ToFileTimeUtc();
            var f=new System.Runtime.InteropServices.ComTypes.FILETIME { dwLowDateTime=unchecked((int)t), dwHighDateTime=unchecked((int)(t>>32)) };
            if(!SetFileTime(handle,ref f,IntPtr.Zero,IntPtr.Zero)) throw new IOException("Fixture metadata change failed.");
        }
    }
    public static string Origin(SafeFileHandle handle,Type type) {
        if(handle==null || handle.GetType()!=typeof(SafeFileHandle) || type==null) throw new ArgumentException("Fixture native handle type");
        var method=type.GetMethod("LegacyOrigin",BindingFlags.NonPublic|BindingFlags.Static);
        if(method==null || method.GetParameters().Length!=1 || method.GetParameters()[0].ParameterType!=typeof(SafeFileHandle)) throw new ArgumentException("Fixture identity contract");
        return (string)method.Invoke(null,new object[]{handle});
    }
    private static object Unwrap(object value) {
        for(int i=0;value!=null && value.GetType().FullName=="System.Management.Automation.PSObject";i++) {
            if(i==8) throw new ArgumentException("Wrapper depth"); value=value.GetType().GetProperty("BaseObject").GetValue(value,null);
        } return value;
    }
    public static object InvokeChecked(MethodInfo method,object target,object[] args) {
        target=Unwrap(target); if(method==null || target==null || target.GetType()!=method.DeclaringType) throw new ArgumentException("Fixture target mismatch");
        var p=method.GetParameters(); if(p.Length!=args.Length) throw new ArgumentException("Fixture argument count");
        for(int i=0;i<args.Length;i++){ args[i]=Unwrap(args[i]); if(args[i]==null || args[i].GetType()!=p[i].ParameterType) throw new ArgumentException("Fixture argument type"); }
        return method.Invoke(target,args);
    }
}
'@
$actualRuntime=(Get-Command Invoke-ProGoInstallerLiveRuntimeLeaf).ScriptBlock
function Invoke-ProGoInstallerLiveRuntimeLeaf($LegacyContext,[Threading.CancellationToken]$CancellationToken){
    $calls.Runtime++
    if($scenario -in @('equal-byte-replaced','target-changed','reparse','snapshot-changed-after-release','cancel-after-release','error-after-release','write','short-write','flush','readback','readback-mismatch','cancel-written','cancel-readback','multi-second-write','multi-second-cancel-written','multi-last-write','multi-last-cancel-written','multi-between-cancel','multi-equal-second','multi-snapshot-second','multi-target-second','multi-reparse-second')){
        # Capture actual CLR values locally before GetNewClosure; the mutable observation object survives callback execution.
        $localScenario=[string]$scenario;$localTarget=if($scenario -in @('multi-equal-second','multi-target-second','multi-reparse-second')){[string](Join-Path $app 'VERSION')}else{[string]$target};$localRetained=[string]$retained;$localJunctionTarget=[string]$junctionTarget
        $localSnapshotPath=[string]$snapshotPath;$localSnapshotStamp=$snapshotStamp;$localCancel=$cancel;$observed=$calls
        $localGetOrigin=(Get-Command Get-RuntimeFixtureOrigin).ScriptBlock;$localGetHash=(Get-Command Get-RuntimeFixtureHash).ScriptBlock
        $localOrigin=$null;$localHash=$null;$localCreation=$null
        if($localScenario -in @('equal-byte-replaced','multi-equal-second')){
            $localOrigin=Get-RuntimeFixtureOrigin $localTarget;$localHash=Get-RuntimeFixtureHash $localTarget;$localCreation=[IO.File]::GetCreationTimeUtc($localTarget)
            $observed.BaselineOrigin=$localOrigin;$observed.BaselineHash=$localHash;$observed.BaselineCreation=$localCreation
        }
        $action=[Action]({
            $observed.CallbackEntered++
            try {
                if($localScenario -in @('multi-equal-second','multi-snapshot-second','multi-target-second','multi-reparse-second') -and $observed.CallbackEntered -ne 2){$observed.CallbackCompleted++;return}
                if($localScenario -in @('equal-byte-replaced','multi-equal-second','target-changed','reparse','multi-target-second','multi-reparse-second')){
                    $creation=[IO.File]::GetCreationTimeUtc($localTarget);[IO.File]::Move($localTarget,$localRetained)
                    if($localScenario -in @('equal-byte-replaced','multi-equal-second')){
                        [IO.File]::Copy($localRetained,$localTarget);[IO.File]::SetCreationTimeUtc($localTarget,$creation)
                        $retainedOrigin=& $localGetOrigin $localRetained;$newOrigin=& $localGetOrigin $localTarget
                        $retainedHash=& $localGetHash $localRetained;$copyHash=& $localGetHash $localTarget
                        $observed.EqualBytes=($retainedHash -ceq $copyHash -and $retainedHash -ceq $localHash)
                        $observed.EqualCreation=([IO.File]::GetCreationTimeUtc($localTarget) -eq $localCreation -and [IO.File]::GetCreationTimeUtc($localRetained) -eq $localCreation)
                        $observed.DifferentIdentity=($newOrigin -cne $retainedOrigin)
                        $observed.OriginalUnchanged=($retainedOrigin -ceq $localOrigin -and $retainedHash -ceq $localHash)
                        if(-not ($observed.EqualBytes -and $observed.EqualCreation -and $observed.DifferentIdentity -and $observed.OriginalUnchanged)){throw 'Equal-byte replacement fixture precondition failed.'}
                    }
                    if($localScenario -in @('target-changed','multi-target-second')){[IO.File]::WriteAllText($localTarget,'synthetic foreign target bytes')}
                    if($localScenario -in @('reparse','multi-reparse-second')){
                        New-Item -ItemType Directory -Path $localJunctionTarget | Out-Null
                        & $env:ComSpec /c mklink /J $localTarget $localJunctionTarget | Out-Null
                        if($LASTEXITCODE -ne 0 -or -not ([IO.File]::GetAttributes($localTarget) -band [IO.FileAttributes]::ReparsePoint)){throw 'Synthetic fixture junction unavailable.'}
                    }
                }
                if($localScenario -in @('snapshot-changed-after-release','multi-snapshot-second')){[RuntimeLeafFixture]::SetCreation($localSnapshotPath,$localSnapshotStamp.AddMinutes(1))}
                if($localScenario -ceq 'cancel-after-release'){$localCancel.Cancel()}
                $observed.CallbackCompleted++
                if($localScenario -ceq 'error-after-release'){throw 'synthetic after pin release'}
            } catch { if($localScenario -cne 'error-after-release' -or $observed.CallbackCompleted -ne 1){$observed.CallbackSetupError=$true}; throw }
        }.GetNewClosure())
        $fault=if($scenario -ceq 'multi-second-write'){'second-write'}elseif($scenario -ceq 'multi-second-cancel-written'){'second-cancel-written'}elseif($scenario -ceq 'multi-last-write'){'last-write'}elseif($scenario -ceq 'multi-last-cancel-written'){'last-cancel-written'}elseif($scenario -ceq 'multi-between-cancel'){'between-cancel'}elseif($scenario -in @('write','short-write','flush','readback','readback-mismatch','cancel-written','cancel-readback')){$scenario}else{'none'}
        $method=[ProGo.ApplicationShortcuts].GetMethod('RestoreLiveRuntimeLeaf',[Reflection.BindingFlags]'NonPublic,Instance')
        return [RuntimeLeafFixture]::InvokeChecked($method,$manager,[object[]]@($LegacyContext,$CancellationToken,$fault,$cancel,$action))
    }
    return & $actualRuntime $LegacyContext $CancellationToken
}
$work=Join-Path ([IO.Path]::GetTempPath()) ('ProGo-runtime-leaf-'+[Guid]::NewGuid().ToString('N'))
$context=$null;$lease=$null;$physical=$null;$stage=$null;$cancel=$null;$child=$null;$junction=$null;$snapshotStamp=$null;$snapshotPath=$null
try{
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload=Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    # Truly empty new install has no snapshot: real source-context admission must remain blocked.
    $newCase=Join-Path $work 'new-install-blocked';$newApp=Join-Path $newCase 'app';New-Item -ItemType Directory -Path $newApp | Out-Null
    $newBackup=New-ProGoInstallerBackup $payload $newApp;Check-RuntimeLeaf ($null -eq $newBackup.Directory) 'empty original runtime has no verified snapshot'
    $newStage=New-ProGoInstallerStage $payload;try{
        $newWrites=New-Object 'System.Collections.Generic.List[object]';foreach($entry in $newBackup.Entries){Set-ProGoInstallerRuntimeFile $newStage $newBackup $entry $newWrites}
        $blocked=$false;try{Get-ProGoInstallerLegacySourceContext $newBackup}catch{$blocked=$true};Check-RuntimeLeaf $blocked 'no snapshot blocks live Legacy source context even after actual synthetic writes'
    }finally{Remove-ProGoInstallerStage $newStage}
    foreach($scenario in @('complete','equal-byte-replaced','target-changed','reparse','snapshot-changed','snapshot-write-denied','snapshot-changed-after-release','cancel-before','cancel-after-release','error-after-release','write','short-write','flush','readback','readback-mismatch','cancel-written','cancel-readback','disposed','late-success','multi-complete','multi-second-write','multi-second-cancel-written','multi-last-write','multi-last-cancel-written','multi-between-cancel','multi-equal-second','multi-snapshot-second','multi-target-second','multi-reparse-second','multi-disposed','multi-late-success','multi-inventory-missing','multi-inventory-duplicate','multi-inventory-unknown','multi-installed-handling')){
        $calls=@{Runtime=0;CallbackEntered=0;CallbackCompleted=0;CallbackSetupError=$false;EqualBytes=$false;EqualCreation=$false;DifferentIdentity=$false;OriginalUnchanged=$false;BaselineOrigin=$null;BaselineHash=$null;BaselineCreation=$null};$case=Join-Path $work $scenario;$app=Join-Path $case 'app';$programs=Join-Path $case 'Programs';$startup=Join-Path $case 'Startup';$menu=Join-Path $programs 'ProGo'
        New-Item -ItemType Directory -Path $app,$menu,$startup | Out-Null
        $target=Join-Path $app 'ProGo.exe';$retained=Join-Path $case 'retained-native-target';$junctionTarget=Join-Path $case 'foreign-junction-target'
        [IO.File]::WriteAllText($target,'synthetic original executable');$oldHash=Get-RuntimeFixtureHash $target
        $multiOld=@{};if($scenario.StartsWith('multi-')){foreach($leaf in @('ProGo.exe','VERSION','ProGo.ico')){$path=Join-Path $app $leaf;[IO.File]::WriteAllText($path,('synthetic old '+$leaf+('x' * (17+$leaf.Length))));$multiOld[$leaf]=Get-RuntimeFixtureHash $path};$oldHash=$multiOld['ProGo.exe']}
        $private=@{};foreach($path in @((Join-Path $app 'settings.json'),(Join-Path $app 'vault.enc'),(Join-Path $menu 'foreign.txt'),(Join-Path $startup 'foreign.txt'))){[IO.File]::WriteAllText($path,'synthetic private bytes');$private[$path]=Get-RuntimeFixtureHash $path}
        $manager=New-Object ProGo.ApplicationShortcuts($app,$programs,$startup);$manager.Install($true,$true,$false);$legacy=Join-Path $menu 'ProGo Status.lnk';[IO.File]::Move((Join-Path $menu 'ProGo.lnk'),$legacy)
        $backup=New-ProGoInstallerBackup $payload $app;$stage=New-ProGoInstallerStage $payload;Check-RuntimeLeaf ($null -ne $backup.Directory) 'actual upgrade snapshot exists'
        $writes=New-Object 'System.Collections.Generic.List[object]';foreach($entry in $backup.Entries){Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes}
        $prepared=Initialize-ProGoShortcuts -InstallDirectory $app -WasInstalled $false -NoStartup $false -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation
        $reference=New-ProGoInstallerLegacySourceReference $backup $programs $startup;Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup;[void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        if($scenario -in @('multi-inventory-missing','multi-inventory-duplicate','multi-inventory-unknown')){
            $admissionPath=Join-Path $backup.Journal.Directory 'admit-000000.json'
            $doc=[IO.File]::ReadAllText($admissionPath) | ConvertFrom-Json -ErrorAction Stop
            $inv=$doc.Inventory | ConvertFrom-Json -ErrorAction Stop
            $entries=@($inv.Files)
            if($scenario -ceq 'multi-inventory-missing'){$inv.Files=@($entries | Select-Object -Skip 1)}
            elseif($scenario -ceq 'multi-inventory-duplicate'){$entries[1]=$entries[0];$inv.Files=$entries}
            else{$entries[1].Relative='unknown-synthetic-leaf';$inv.Files=$entries}
            $doc.Inventory=$inv | ConvertTo-Json -Depth 8 -Compress
            $tampered=$doc | ConvertTo-Json -Depth 10 -Compress
            [IO.File]::WriteAllText($admissionPath,$tampered,(New-Object Text.UTF8Encoding($false)))
            Check-RuntimeLeaf ([IO.File]::ReadAllText($admissionPath) -ceq $tampered) 'inventory corruption setup completed'
            $before=@{};foreach($leaf in $multiOld.Keys){$before[$leaf]=Get-RuntimeFixtureHash (Join-Path $app $leaf)}
            $blocked=Restore-ProGoInstallerLegacyReferenceEvidence $backup.Journal.Directory $app $programs $startup
            Check-RuntimeLeaf ($null -eq $blocked) 'corrupt inventory rejected at verified admission'
            foreach($leaf in $multiOld.Keys){Check-RuntimeLeaf ((Get-RuntimeFixtureHash (Join-Path $app $leaf)) -ceq $before[$leaf]) 'inventory refusal leaves runtime unchanged'}
            foreach($path in $private.Keys){Check-RuntimeLeaf ((Get-RuntimeFixtureHash $path) -ceq $private[$path]) 'inventory refusal leaves private bytes unchanged'}
            Remove-ProGoInstallerStage $stage;$stage=$null;Write-Host ('PASS runtime inventory refusal '+$scenario);continue
        }
        $physical=Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $app $programs $startup;$lease=Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $app $programs $startup
        $context=$manager.BindLegacyStartupContext($prepared,$lease,$physical);Check-RuntimeLeaf ($null -ne $context) 'same live context'
        $delete=$lease.DeleteOwnedOnce($physical,[Threading.CancellationToken]::None);$accepted=$lease.RecordAcceptedOutcome($physical,[Threading.CancellationToken]::None);Check-RuntimeLeaf ($delete.OperationAccepted -and $accepted.DurableOutcomeRecorded) 'accepted owned deletion and result'
        $cancel=New-Object Threading.CancellationTokenSource;$heldError=$null;try{throw 'synthetic original installer failure'}catch{$heldError=$_}
        if($scenario -ceq 'multi-installed-handling'){
            Check-RuntimeLeaf ($context.BeginInstalledHandling()) 'normal-success handling entered without publication'
            $summary=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $app $programs $startup $context $cancel.Token -RestoreSingleRuntimeLeaf
            Check-RuntimeLeaf ($null -eq $summary.RuntimeLeafProgress -and -not $summary.RuntimeLeafVerified -and $calls.Runtime -eq 0 -and -not $context.RuntimeFailureBranchAllowed) 'handling alone refuses runtime branch before publication'
        }elseif($scenario -in @('late-success','multi-late-success')){
            $success=Invoke-ProGoInstallerSuccess $backup $writes $prepared $programs $startup $context
            Check-RuntimeLeaf ($success.ScopedSuccessVerified -and $context.VerifyInstalledOutcome()) 'actual normal success first'
            $summary=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $app $programs $startup $context $cancel.Token -RestoreSingleRuntimeLeaf
            Check-RuntimeLeaf ($null -eq $summary.RuntimeLeafProgress -and -not $summary.RuntimeLeafVerified -and $context.VerifyInstalledOutcome() -and $calls.Runtime -eq 0) 'late failure cannot roll back accepted normal publication'
        }else{
            $first=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $app $programs $startup $context $cancel.Token
            Check-RuntimeLeaf ($first.AuxiliaryStateVerified -and $first.AuxiliaryOutcomeRecorded) 'real Legacy Startup Menu chain and auxiliary publication'
            $legacyHash=Get-RuntimeFixtureHash $legacy
            $frozen=@{};foreach($file in Get-ChildItem -LiteralPath (Join-Path $app 'backups') -Recurse -File){$frozen[$file.FullName]=Get-RuntimeFixtureHash $file.FullName}
            if($scenario -in @('snapshot-changed','snapshot-changed-after-release','multi-snapshot-second')){$snapshotPath=Join-Path $backup.Directory $(if($scenario -ceq 'multi-snapshot-second'){'VERSION'}else{'ProGo.exe'});$snapshotStamp=[IO.File]::GetCreationTimeUtc($snapshotPath);if($scenario -ceq 'snapshot-changed'){[RuntimeLeafFixture]::SetCreation($snapshotPath,$snapshotStamp.AddMinutes(1))}}
            if($scenario -ceq 'snapshot-write-denied'){
                $snapshotWrite=$null;$denied=$false
                try{$snapshotWrite=[IO.File]::Open((Join-Path $backup.Directory 'ProGo.exe'),[IO.FileMode]::Open,[IO.FileAccess]::Write,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete));$snapshotWrite.WriteByte(0)}catch [IO.IOException]{$denied=$true}finally{if($null -ne $snapshotWrite){$snapshotWrite.Dispose()}}
                Check-RuntimeLeaf $denied 'retained snapshot pin denies external byte modification'
            }
            if($scenario -ceq 'cancel-before'){$cancel.Cancel()}
            if($scenario -in @('disposed','multi-disposed')){$context.Dispose()}
            $summary=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $app $programs $startup $context $cancel.Token -RestoreSingleRuntimeLeaf
            if($scenario -in @('equal-byte-replaced','target-changed','reparse','snapshot-changed-after-release','cancel-after-release','error-after-release','write','short-write','flush','readback','readback-mismatch','cancel-written','cancel-readback')){
                $progress=$summary.RuntimeLeafProgress;$attempted=if($null -ne $progress){$progress.Attempted}else{$false};$released=if($null -ne $progress){$progress.PinReleased}else{$false};$reopened=if($null -ne $progress){$progress.HandleReopened}else{$false};$mutated=if($null -ne $progress){$progress.MutationStarted}else{$false}
                Write-Host ('RUNTIME_INJECTION case='+$scenario+'; entered='+$calls.CallbackEntered+'; completed='+$calls.CallbackCompleted+'; setupError='+$calls.CallbackSetupError+'; equalBytes='+$calls.EqualBytes+'; equalCreation='+$calls.EqualCreation+'; differentIdentity='+$calls.DifferentIdentity+'; originalUnchanged='+$calls.OriginalUnchanged+'; attempted='+$attempted+'; pinReleased='+$released+'; reopened='+$reopened+'; mutationStarted='+$mutated+'; verified='+$summary.RuntimeLeafVerified)
                if($calls.CallbackEntered -ne 1 -or $calls.CallbackCompleted -ne 1 -or $calls.CallbackSetupError){throw 'SETUP ERROR: required synthetic callback did not complete exactly once.'}
                if($scenario -ceq 'equal-byte-replaced' -and -not ($calls.EqualBytes -and $calls.EqualCreation -and $calls.DifferentIdentity -and $calls.OriginalUnchanged)){throw 'SETUP ERROR: equal-byte native replacement was not established.'}
                if($scenario -ceq 'equal-byte-replaced'){Check-RuntimeLeaf ([IO.File]::Exists($retained) -and (Get-RuntimeFixtureOrigin $retained) -ceq $calls.BaselineOrigin -and (Get-RuntimeFixtureHash $retained) -ceq $calls.BaselineHash -and [IO.File]::GetCreationTimeUtc($retained) -eq $calls.BaselineCreation) 'retained original native object and bytes remain intact after refusal'}
            }
            Check-RuntimeLeaf ([Object]::ReferenceEquals($summary.OriginalError,$heldError) -and -not $summary.WholeRollbackVerified -and -not $summary.RestartAuthority -and -not $summary.BackupCleanupAllowed) 'original error and scoped boundaries retained'
            if($scenario -in @('complete','snapshot-write-denied','multi-complete')){
                Check-RuntimeLeaf ($summary.RuntimeLeafVerified -and $summary.RuntimeLeafProgress.Attempted -and $summary.RuntimeLeafProgress.PinReleased -and $summary.RuntimeLeafProgress.HandleReopened -and $summary.RuntimeLeafProgress.MutationStarted -and $summary.RuntimeLeafProgress.Flushed -and $summary.RuntimeLeafProgress.ReadbackVerified -and $summary.AuxiliaryOutcomeRecorded) 'same process actual handle restore, flush, readback and live auxiliary revalidation'
                Check-RuntimeLeaf (-not $physical.VerifyPinned([Threading.CancellationToken]::None) -and $context.VerifyRuntimeLeafOutcome()) 'old strict physical plan refuses changed runtime and same live context verifies expected old leaf'
            }else{Check-RuntimeLeaf (-not $summary.RuntimeLeafVerified) 'conflict cancellation fault or lost proof never becomes success'}
            if($scenario -in @('equal-byte-replaced','target-changed','reparse','snapshot-changed-after-release','cancel-after-release','error-after-release')){Check-RuntimeLeaf ($summary.RuntimeLeafProgress.Attempted -and $summary.RuntimeLeafProgress.PinReleased -and -not $summary.RuntimeLeafProgress.MutationStarted) 'post-release conflict blocks before mutation'}
            if($scenario -in @('write','short-write','flush','readback','readback-mismatch','cancel-written','cancel-readback')){Check-RuntimeLeaf ($summary.RuntimeLeafProgress.MutationStarted -and -not $summary.RuntimeLeafVerified) 'actual partial write or late fault remains partial'}
            if($scenario -ceq 'cancel-readback'){Check-RuntimeLeaf ($summary.RuntimeLeafProgress.Flushed -and $summary.RuntimeLeafProgress.ReadbackVerified -and $summary.RuntimeLeafProgress.CancellationObserved -and -not $summary.RuntimeLeafVerified) 'late cancellation preserves actual flushed readback progress without scoped success'}
            if($scenario.StartsWith('multi-') -and $scenario -cne 'multi-disposed'){
                $progress=$summary.RuntimeLeafProgress
                Check-RuntimeLeaf ($progress.SelectedCount -eq 3 -and $progress.MissingSnapshotCount -gt 0 -and -not $progress.WholeRollbackVerified) 'fixed inventory selects three existing snapshot leaves and retains missing-leaf blocker'
                if($scenario -ceq 'multi-complete'){Check-RuntimeLeaf ($progress.CompletedCount -eq 3 -and $progress.SetVerified) 'all three exact transitions verified'}
                if($scenario -in @('multi-second-write','multi-second-cancel-written','multi-between-cancel','multi-equal-second','multi-snapshot-second','multi-target-second','multi-reparse-second')){Check-RuntimeLeaf ($progress.CompletedCount -eq 1 -and -not $progress.SetVerified) 'second leaf fault preserves one completed transition'}
                if($scenario -in @('multi-last-write','multi-last-cancel-written')){Check-RuntimeLeaf ($progress.CompletedCount -eq 2 -and -not $progress.SetVerified) 'last leaf fault preserves two completed transitions'}
                if($scenario -in @('multi-second-cancel-written','multi-last-cancel-written','multi-between-cancel')){Check-RuntimeLeaf $progress.CancellationObserved 'cancellation recorded'}
                if($scenario -in @('multi-target-second','multi-reparse-second')){Check-RuntimeLeaf ($calls.CallbackEntered -eq 2 -and $calls.CallbackCompleted -eq 2 -and -not $calls.CallbackSetupError -and -not $progress.MutationStarted) 'second target conflict injected and mutation refused'}
                if($scenario -ceq 'multi-snapshot-second'){Check-RuntimeLeaf ($calls.CallbackEntered -eq 2 -and $calls.CallbackCompleted -eq 2 -and -not $calls.CallbackSetupError -and -not $progress.MutationStarted) 'snapshot tamper callback ran and blocked second mutation'}
                if($scenario -ceq 'multi-equal-second'){
                    Check-RuntimeLeaf ($calls.CallbackEntered -eq 2 -and $calls.CallbackCompleted -eq 2 -and -not $calls.CallbackSetupError -and $calls.EqualBytes -and $calls.EqualCreation -and $calls.DifferentIdentity -and $calls.OriginalUnchanged -and -not $progress.MutationStarted) 'actual equal-byte second replacement refused before mutation'
                }
                if($scenario -in @('multi-second-write','multi-second-cancel-written','multi-last-write','multi-last-cancel-written','multi-between-cancel')){Check-RuntimeLeaf ($calls.CallbackEntered -eq $(if($scenario -ceq 'multi-between-cancel'){1}elseif($scenario -in @('multi-second-write','multi-second-cancel-written')){2}else{3}) -and $calls.CallbackCompleted -eq $calls.CallbackEntered -and -not $calls.CallbackSetupError) 'fault callback ran at every released leaf'}
            }
            $beforeCalls=$calls.Runtime;$again=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $app $programs $startup $context ([Threading.CancellationToken]::None) -RestoreSingleRuntimeLeaf
            Check-RuntimeLeaf ($calls.Runtime -eq $beforeCalls -and [Object]::ReferenceEquals($summary.OriginalError,$again.OriginalError)) 'repeat never replays runtime mutation or replaces original error'
            if($scenario -in @('complete','multi-complete')){
                $info=New-Object Diagnostics.ProcessStartInfo;$info.FileName=Join-Path $PSHOME 'powershell.exe';$info.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -BlockedChild -CaseDirectory "'+$case+'"';$info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
                $child=[Diagnostics.Process]::Start($info);$out=$child.StandardOutput.ReadToEndAsync();$err=$child.StandardError.ReadToEndAsync();if(-not $child.WaitForExit(20000)){throw 'Runtime child deadline exceeded.'};Check-RuntimeLeaf ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($out,$err),5000) -and $child.ExitCode -eq 0 -and $err.Result.Length -eq 0 -and $out.Result.Contains('RUNTIME_CHILD blocked=True')) 'fresh process cannot use stored auxiliary receipt';$child.Dispose();$child=$null
                if($scenario -ceq 'complete'){
                    $unknown=Join-Path $work 'unknown-child'
                    $info.Arguments='-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -BlockedChild -CaseDirectory "'+$unknown+'"'
                    $child=[Diagnostics.Process]::Start($info);$out=$child.StandardOutput.ReadToEndAsync();$err=$child.StandardError.ReadToEndAsync();if(-not $child.WaitForExit(20000)){throw 'Runtime unknown child deadline exceeded.'}
                    Check-RuntimeLeaf ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($out,$err),5000) -and $child.ExitCode -ne 0 -and $err.Result.Contains('Runtime child scope refused.') -and -not $out.Result.Contains('RUNTIME_CHILD blocked=True')) 'unknown child name refused before receipt access'
                    $child.Dispose();$child=$null
                }
            }
            if($null -ne $snapshotStamp){
                [RuntimeLeafFixture]::SetCreation($snapshotPath,$snapshotStamp);$snapshotStamp=$null
                $afterProofReturns=Invoke-ProGoInstallerFailureAuxiliary $heldError $backup $app $programs $startup $context ([Threading.CancellationToken]::None) -RestoreSingleRuntimeLeaf
                Check-RuntimeLeaf ($calls.Runtime -eq $beforeCalls -and -not $afterProofReturns.RuntimeLeafVerified) 'once blocked snapshot proof returns, repeat cannot mutate'
            }
            Check-RuntimeLeaf ((Get-RuntimeFixtureHash $legacy) -ceq $legacyHash) 'restored shortcut preserved'
            foreach($path in $frozen.Keys){Check-RuntimeLeaf ((Get-RuntimeFixtureHash $path) -ceq $frozen[$path]) 'snapshot source journal and auxiliary bytes preserved'}
        }
        $context.Dispose();$context=$null;if($null -ne $lease){$lease.Dispose();$lease=$null};if($null -ne $physical){$physical.Dispose();$physical=$null};$cancel.Dispose();$cancel=$null
        if($scenario -in @('complete','snapshot-write-denied','multi-complete')){Check-RuntimeLeaf ((Get-RuntimeFixtureHash $target) -ceq $oldHash) 'exact old runtime bytes after handles release'}
        if($scenario.StartsWith('multi-') -and $scenario -notin @('multi-disposed','multi-late-success','multi-installed-handling')){Check-RuntimeLeaf ((Get-RuntimeFixtureHash (Join-Path $app 'ProGo.exe')) -ceq $multiOld['ProGo.exe']) 'completed first leaf keeps exact snapshot bytes after release'}
        if($scenario -ceq 'multi-complete'){foreach($leaf in $multiOld.Keys){Check-RuntimeLeaf ((Get-RuntimeFixtureHash (Join-Path $app $leaf)) -ceq $multiOld[$leaf]) 'every multi-file snapshot restored exactly'}}
        if($scenario -ceq 'reparse'){[IO.Directory]::Delete($target)};if($scenario -ceq 'multi-reparse-second'){[IO.Directory]::Delete((Join-Path $app 'VERSION'))}
        foreach($path in $private.Keys){Check-RuntimeLeaf ((Get-RuntimeFixtureHash $path) -ceq $private[$path]) 'private and foreign fixture bytes intact'}
        foreach($file in Get-ChildItem -LiteralPath $case -Recurse -File){$p=[IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$p.Dispose()}
        Remove-ProGoInstallerStage $stage;$stage=$null;Write-Host ('PASS runtime leaf '+$scenario)
    }
    Write-Host 'Live runtime leaf tests PASS: 35 plus empty-snapshot refusal; fresh-process refusal=1; whole rollback/default/restart/power-loss/PC NOT_RUN'
}finally{
    if($null -ne $snapshotStamp){[RuntimeLeafFixture]::SetCreation($snapshotPath,$snapshotStamp)}
    if($null -ne $child){try{if(-not $child.HasExited){$child.Kill();if(-not $child.WaitForExit(5000)){throw 'Runtime child cleanup failed.'}}}finally{$child.Dispose()}}
    if($null -ne $context){$context.Dispose()};if($null -ne $lease){$lease.Dispose()};if($null -ne $physical){$physical.Dispose()};if($null -ne $cancel){$cancel.Dispose()};if($null -ne $stage){Remove-ProGoInstallerStage $stage}
    if([IO.Directory]::Exists($work)){if([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-runtime-leaf-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Runtime fixture cleanup scope refused.'};foreach($p in @(Get-ChildItem -LiteralPath $work -Recurse -Force)){if($p.Attributes -band [IO.FileAttributes]::ReparsePoint){[IO.Directory]::Delete($p.FullName)}};Remove-Item -LiteralPath $work -Recurse -Force}
}
