param([switch]$BlockedChild,[string]$CaseDirectory,[switch]$MenuFlow)
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Live Startup handlers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -notin @('Enter-ProGoInstallerMaintenance','Stop-ProGoInstallerApplication')
},$false)) { . ([scriptblock]::Create($function.Extent.Text)) }
if ($BlockedChild) {
    $parent = [IO.Path]::GetDirectoryName($CaseDirectory)
    if ([IO.Path]::GetFileName($CaseDirectory) -cnotin @('complete','disposed-context','disposed-context-menu') -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-live-startup-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Live Startup child refuses unowned scope.' }
    foreach ($path in @($parent,$CaseDirectory)) { if ([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint) { throw 'Live Startup child refuses redirected scope.' } }
    $install = Join-Path $CaseDirectory 'app'; $programs = Join-Path $CaseDirectory 'Programs'; $startup = Join-Path $CaseDirectory 'Startup'
    $journals = @(Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-journal-[a-f0-9]{32}$' })
    if ($journals.Count -ne 1) { throw 'Live Startup child journal mismatch.' }
    $backup = [pscustomobject]@{ Journal = [pscustomobject]@{ Directory = $journals[0].FullName } }
    $serialized = [pscustomobject]@{ StartupInvoked = $true; MenuAllowed = $false; OperationId = [IO.Path]::GetFileName($journals[0].FullName) }
    if ((Invoke-ProGoInstallerFailureStartup $true $backup $install $programs $startup) -cne 'blocked' -or
        (Invoke-ProGoInstallerFailureStartup $true $backup $install $programs $startup $serialized) -cne 'blocked' -or
        (Invoke-ProGoInstallerFailureMenu $true $backup $install 'legacy-startup-observed-menu-blocked' $programs $startup) -cne 'blocked' -or
        (Invoke-ProGoInstallerFailureMenu $true $backup $install 'legacy-startup-observed-menu-blocked' $programs $startup $serialized) -cne 'blocked' -or
        $null -ne (Restore-ProGoInstallerLegacyReferenceEvidence $journals[0].FullName $install $programs $startup)) { throw 'Fresh process must remain blocked without the live context.' }
    Write-Host 'LIVE_STARTUP_CHILD blocked=True; replayPerformed=False'
    return
}
function Check-LiveStartup($Condition,[string]$Name) { if (-not $Condition) { throw ('Live Startup failed: case=' + $scenario + '; check=' + $Name) } }
function Get-LiveStartupHash([string]$Path) {
    $stream = $null; $hash = $null
    try {
        $hash = [Security.Cryptography.SHA256]::Create()
        $stream = [IO.File]::Open($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::Read -bor [IO.FileShare]::Delete))
        return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','')
    } finally { if ($null -ne $stream) { $stream.Dispose() }; if ($null -ne $hash) { $hash.Dispose() } }
}
function Get-LiveStartupMap($Evidence) {
    $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
    foreach ($property in $Evidence.PSObject.Properties) { if ($property.Name -cnotin @('Menu','Legacy')) { $map.Add($property.Name,$property.Value) } }
    return ,$map
}
# A second real operation in a different GUID-owned subtree cannot be adopted by the current handler.
function New-LiveMenuForeignContext($Payload,[string]$Base) {
    $app = Join-Path $Base 'foreign-app'; $programs = Join-Path $Base 'foreign-programs'; $startup = Join-Path $Base 'foreign-startup'
    $menu = Join-Path $programs 'ProGo'; $physical = $null; $lease = $null; $stage = $null
    try {
        New-Item -ItemType Directory -Path $app,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $app 'ProGo.exe'),'synthetic foreign executable')
        $manager = New-Object ProGo.ApplicationShortcuts($app,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move((Join-Path $menu 'ProGo.lnk'),(Join-Path $menu 'ProGo Status.lnk'))
        $backup = New-ProGoInstallerBackup $Payload $app; $stage = New-ProGoInstallerStage $Payload
        $writes = New-Object 'System.Collections.Generic.List[object]'; foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $prepared = Initialize-ProGoShortcuts -InstallDirectory $app -WasInstalled $false -NoStartup $false -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation
        $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
        Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup
        [void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        $physical = Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $app $programs $startup
        $lease = Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $app $programs $startup
        $context = $manager.BindLegacyStartupContext($prepared,$lease,$physical)
        if ($null -eq $context) { throw 'Foreign live context fixture failed to bind.' }
        $result = [pscustomobject]@{ Context = $context; Stage = $stage; Legacy = (Join-Path $menu 'ProGo Status.lnk') }
        $lease = $null; $physical = $null; $stage = $null
        return $result
    } finally { if ($null -ne $lease) { $lease.Dispose() }; if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $stage) { Remove-ProGoInstallerStage $stage } }
}
$actualStartup = (Get-Command Invoke-ProGoInstallerStartupRollback).ScriptBlock
function Invoke-ProGoInstallerStartupRollback($Directory,$InstallDirectory,$ProgramsDirectory,$StartupDirectory,$CancellationToken,$LegacyContext) {
    $fixture.StartupCalls++
    Check-LiveStartup ($LegacyContext.VerifyHandoff() -and [IO.File]::Exists($legacy) -and [IO.File]::Exists($primary)) 'real Startup handler entered only after verified Legacy restore with primary retained'
    if ($fixture.Mode -ceq 'error-after-legacy') { throw 'synthetic between Legacy and Startup' }
    if ($fixture.Mode -ceq 'cancel-after-legacy') { $cancel.Cancel() }
    $observed = & $actualStartup $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken $LegacyContext
    if ($fixture.Mode -ceq 'error-after-startup' -and $observed) { throw 'synthetic between Startup and blocked Menu' }
    return $observed
}
$actualMenu = (Get-Command Invoke-ProGoInstallerMenuRollback).ScriptBlock
function Invoke-ProGoInstallerMenuRollback($Directory,$InstallDirectory,$ProgramsDirectory,$StartupDirectory,$CancellationToken,$LegacyContext) {
    $fixture.MenuCalls++
    Check-LiveStartup ($LegacyContext.VerifyStartupSettlement() -and [IO.File]::Exists($legacy) -and [IO.File]::Exists($primary)) 'Menu follows live Legacy then Startup with verified primary'
    if ($fixture.Mode -ceq 'before-menu-error') { throw 'synthetic before Menu' }
    if ($fixture.Mode -ceq 'cancel-before-menu') { $cancel.Cancel() }
    $observed = & $actualMenu $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory $CancellationToken $LegacyContext
    if ($fixture.Mode -ceq 'cancel-after-menu') { $cancel.Cancel() }
    if ($fixture.Mode -ceq 'after-menu-error' -and $observed) { throw 'synthetic after Menu before handler settlement' }
    return $observed
}
$scenario = 'initial'; $work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-live-startup-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $physical = $null; $lease = $null; $context = $null; $cancel = $null; $metadata = $null; $stamp = $null
$process = $null; $outTask = $null; $errTask = $null; $foreignContext = $null; $foreignStage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $cases = @('complete','preserved-startup','wrong-preparation','wrong-root','wrong-operation','disposed-context','disposed-lease','disposed-physical',
        'source-changed','result-tampered','legacy-equal-byte','primary-replaced','startup-replaced','error-after-legacy','error-after-startup','cancel-before','cancel-after-legacy','restore-partial','restore-result-fault')
    if ($MenuFlow) {
        $cases = @('complete','preserved-startup','before-menu-error','after-menu-error','cancel-before-menu','cancel-after-menu',
            'primary-before-menu','legacy-before-menu','disposed-context-menu','disposed-lease-menu','disposed-physical-menu',
            'wrong-root-menu','wrong-operation-menu','foreign-context-menu','result-before-menu','result-after-menu','source-after-menu',
            'primary-after-menu','startup-after-menu','restored-pin-lock','restore-partial','restore-result-fault','cancel-before','error-after-legacy')
    }
    foreach ($scenario in $cases) {
        $fixture = @{ Mode = $scenario; StartupCalls = 0; MenuCalls = 0 }
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $primary = Join-Path $menu 'ProGo.lnk'; $legacy = Join-Path $menu 'ProGo Status.lnk'; $startupLink = Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        $private = @{}; foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { $path = Join-Path $install $name; [IO.File]::WriteAllText($path,'synthetic private or foreign bytes'); $private[$path] = Get-LiveStartupHash $path }
        foreach ($path in @((Join-Path $menu 'foreign-menu.txt'),(Join-Path $startup 'foreign-startup.txt'))) { [IO.File]::WriteAllText($path,'synthetic foreign shortcut sibling'); $private[$path] = Get-LiveStartupHash $path }
        $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
        $manager.Install($true,$true,$false); [IO.File]::Move($primary,$legacy)
        [byte[]]$original = [IO.File]::ReadAllBytes($legacy)
        if ($scenario -ceq 'preserved-startup') { [IO.File]::WriteAllText($startupLink,'synthetic customized Startup') }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'; foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $prepared = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup ($scenario -ceq 'preserved-startup') -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence -PreserveLegacyForOwnedOperation
        $reference = New-ProGoInstallerLegacySourceReference $backup $programs $startup
        Complete-ProGoInstallerLegacyRestartSeal $backup $reference $programs $startup
        [void](New-ProGoInstallerLegacyPreDeleteAdmission $backup $programs $startup)
        $physical = Open-ProGoInstallerLegacyPhysicalPins $backup.Journal.Directory $install $programs $startup
        $lease = Open-ProGoInstallerLegacyDeletionPins $backup.Journal.Directory $install $programs $startup
        $context = $manager.BindLegacyStartupContext($prepared,$lease,$physical)
        Check-LiveStartup ($null -ne $context -and $context.OperationId -eq $reference.Source.Operation -and -not $context.VerifyHandoff()) 'live binding precedes mutation and cannot use historical or absent result'
        if ($scenario -ceq 'wrong-preparation') {
            $otherRoot = Join-Path $case 'other-app'; $otherPrograms = Join-Path $case 'other-programs'; $otherStartup = Join-Path $case 'other-startup'
            New-Item -ItemType Directory -Path $otherRoot,$otherPrograms,$otherStartup | Out-Null; [IO.File]::WriteAllText((Join-Path $otherRoot 'ProGo.exe'),'synthetic other executable')
            $other = New-Object ProGo.ApplicationShortcuts($otherRoot,$otherPrograms,$otherStartup)
            $otherEvidence = $other.PrepareForOwnedLegacyOperation($false,$false,$false)
            Check-LiveStartup ($null -eq $manager.BindLegacyStartupContext($otherEvidence,$lease,$physical) -and [IO.File]::Exists($legacy)) 'foreign preparation refuses without mutation'
        }
        $cancel = New-Object Threading.CancellationTokenSource
        $deleted = $lease.DeleteOwnedOnce($physical,[Threading.CancellationToken]::None)
        $receipt = $lease.RecordAcceptedOutcome($physical,[Threading.CancellationToken]::None)
        Check-LiveStartup ($deleted.OperationAccepted -and $receipt.DurableOutcomeRecorded) 'real owned delete and accepted receipt'
        Check-LiveStartup (-not $manager.VerifyDurableInstallEvidence((Get-LiveStartupMap $prepared),(Get-LiveStartupMap $prepared.Menu),(Get-LiveStartupMap $prepared.Legacy),$context)) 'accepted deletion plus old preserved receipt is not a success path or restored handoff'
        $op = $reference.Source.Operation.ToString('N'); $resultPath = Join-Path (Join-Path $install 'backups') ('installer-legacy-restored-' + $op + '.bin')
        if ($scenario -in @('disposed-context','disposed-lease','disposed-physical','result-tampered','legacy-equal-byte','primary-replaced','startup-replaced')) {
            $historical = $context.RestoreLegacy([Threading.CancellationToken]::None)
            Check-LiveStartup ($historical.DurableResultRecorded -and $context.VerifyHandoff()) 'live verified result exists before adversarial loss or replacement'
        }
        if ($scenario -ceq 'disposed-context') { $context.Dispose() }
        if ($scenario -ceq 'disposed-lease') { $lease.Dispose() }
        if ($scenario -ceq 'disposed-physical') { $physical.Dispose() }
        if ($scenario -in @('disposed-context','disposed-lease','disposed-physical')) { Check-LiveStartup ($historical.DurableResultRecorded -and -not $context.VerifyHandoff()) 'historical durable flags do not authorize use after proofs are disposed' }
        if ($scenario -ceq 'source-changed') { $metadata = Join-Path (Join-Path $install 'backups') ('installer-legacy-source-' + $op + '.bin'); $stamp = [IO.File]::GetCreationTimeUtc($metadata); [LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp.AddMinutes(1)) }
        if ($scenario -ceq 'result-tampered') { [IO.File]::WriteAllText($resultPath,'synthetic tampered result') }
        if ($scenario -in @('legacy-equal-byte','primary-replaced','startup-replaced')) {
            $path = if ($scenario -ceq 'legacy-equal-byte') { $legacy } elseif ($scenario -ceq 'primary-replaced') { $primary } else { $startupLink }
            [byte[]]$bytes = [IO.File]::ReadAllBytes($path); $creation = [IO.File]::GetCreationTimeUtc($path)
            [IO.File]::Move($path,(Join-Path $case 'retained-native-object')); [IO.File]::WriteAllBytes($path,$bytes); [IO.File]::SetCreationTimeUtc($path,$creation)
        }
        if ($scenario -in @('restore-partial','restore-result-fault')) {
            $method = [ProGo.ApplicationShortcuts].GetMethod('RestoreLegacyOwnedOnce',[Reflection.BindingFlags]'NonPublic,Instance')
            $fault = if ($scenario -ceq 'restore-partial') { 'short-write' } else { 'result-write' }
            $faulted = [LegacyRestoreReflectionFixture]::InvokeChecked($method,$manager,[object[]]@($lease,$physical,$cancel.Token,$fault,$cancel))
            Check-LiveStartup ($faulted.Created -and -not $faulted.DurableResultRecorded) 'existing real partial/result fault retains created truth'
        }
        if ($scenario -ceq 'cancel-before') { $cancel.Cancel() }
        $first = Get-LiveStartupMap $prepared; $main = Get-LiveStartupMap $prepared.Menu; $oldLegacy = Get-LiveStartupMap $prepared.Legacy
        Check-LiveStartup (-not $manager.VerifyDurableInstallEvidence($first,$main,$oldLegacy)) 'receipt-only combined API refuses stale Legacy evidence'
        $callBackup = $backup; $callInstall = $install
        if ($scenario -ceq 'wrong-root') { $callInstall = Join-Path $case 'foreign-install' }
        if ($scenario -ceq 'wrong-operation') { $callBackup = [pscustomobject]@{ Journal = [pscustomobject]@{ Directory = Join-Path (Join-Path $install 'backups') ('installer-journal-' + [Guid]::NewGuid().ToString('N')) } } }
        $heldError = $null
        try { throw 'synthetic original install failure' } catch { $heldError = $_; $state = Invoke-ProGoInstallerFailureStartup $true $callBackup $callInstall $programs $startup $context $cancel.Token }
        if (-not $MenuFlow) {
        $expected = $scenario -in @('complete','preserved-startup','wrong-preparation')
        Check-LiveStartup ($heldError.Exception.Message -ceq 'synthetic original install failure' -and ($state -ceq 'legacy-startup-observed-menu-blocked') -eq $expected) 'real failure handler preserves original error and reports only scoped live settlement'
        $settled = $expected -or $scenario -ceq 'error-after-startup'
        Check-LiveStartup ($context.VerifyStartupSettlement() -eq $settled -and [IO.File]::Exists($startupLink) -eq (-not $settled -or $scenario -ceq 'preserved-startup')) 'Startup rollback only follows verified live Legacy handoff'
        $foreignBackup = [pscustomobject]@{ Journal = [pscustomobject]@{ Directory = Join-Path (Join-Path $install 'backups') ('installer-journal-' + [Guid]::NewGuid().ToString('N')) } }
        Check-LiveStartup ((Invoke-ProGoInstallerFailureMenu $true $foreignBackup $install $state $programs $startup $context) -ceq 'blocked' -and [IO.File]::Exists($primary)) 'real Menu handler refuses conflicting operation and retains primary'
        Check-LiveStartup (-not $context.WholeRollbackVerified -and -not $context.InstallSucceeded -and -not $context.RestartAuthority) 'scoped live settlement grants no whole success or restart authority'
        Check-LiveStartup (-not $manager.VerifyDurableInstallEvidence($first,$main,$oldLegacy) -and -not $manager.VerifyDurableStartupOriginalResult($first,$main,$oldLegacy)) 'old receipt-only APIs stay strict before and after own Startup mutation'
        $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = Get-LiveStartupHash $file.FullName }
        $calls = $fixture.StartupCalls
        $again = Invoke-ProGoInstallerFailureStartup $true $callBackup $callInstall $programs $startup $context ([Threading.CancellationToken]::None)
        $repeatState = if ($scenario -ceq 'error-after-startup') { 'legacy-startup-observed-menu-blocked' } else { $state }
        Check-LiveStartup ($again -ceq $repeatState -and $fixture.StartupCalls -eq $calls -and @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count) 'repeat handler never retries mutation after error cancellation or settlement'
        foreach ($path in $inventory.Keys) { Check-LiveStartup ((Get-LiveStartupHash $path) -ceq $inventory[$path]) 'repeat preserves every partial source result and foreign byte' }
        } else {
        $startupExpected = $scenario -notin @('restore-partial','restore-result-fault','cancel-before','error-after-legacy')
        Check-LiveStartup ($heldError.Exception.Message -ceq 'synthetic original install failure' -and $context.VerifyStartupSettlement() -eq $startupExpected) 'original error retained; real Legacy then Startup settlement'
        $menuBackup = $backup; $menuInstall = $install; $menuContext = $context
        if ($scenario -in @('primary-before-menu','legacy-before-menu')) {
            $path = if ($scenario -ceq 'primary-before-menu') { $primary } else { $legacy }
            [byte[]]$bytes = [IO.File]::ReadAllBytes($path); $creation = [IO.File]::GetCreationTimeUtc($path)
            [IO.File]::Move($path,(Join-Path $case 'foreign-retained-object')); [IO.File]::WriteAllBytes($path,$bytes); [IO.File]::SetCreationTimeUtc($path,$creation)
        }
        if ($scenario -ceq 'disposed-context-menu') { $context.Dispose() }
        if ($scenario -ceq 'disposed-lease-menu') { $lease.Dispose() }
        if ($scenario -ceq 'disposed-physical-menu') { $physical.Dispose() }
        if ($scenario -ceq 'wrong-root-menu') { $menuInstall = Join-Path $case 'foreign-install' }
        if ($scenario -ceq 'wrong-operation-menu') { $menuBackup = [pscustomobject]@{ Journal = [pscustomobject]@{ Directory = Join-Path (Join-Path $install 'backups') ('installer-journal-' + [Guid]::NewGuid().ToString('N')) } } }
        if ($scenario -ceq 'foreign-context-menu') {
            $foreign = New-LiveMenuForeignContext $payload $case; $foreignContext = $foreign.Context; $foreignStage = $foreign.Stage; $menuContext = $foreignContext
            Check-LiveStartup ($foreignContext.OperationId -ne $context.OperationId) 'real foreign live operation differs'
            $private[$foreign.Legacy] = Get-LiveStartupHash $foreign.Legacy
        }
        if ($scenario -ceq 'result-before-menu') { [IO.File]::WriteAllText($resultPath,'synthetic changed restore result') }
        $menuState = Invoke-ProGoInstallerFailureMenu $true $menuBackup $menuInstall $state $programs $startup $menuContext $cancel.Token
        $mutated = $scenario -in @('complete','preserved-startup','after-menu-error','cancel-after-menu','result-after-menu','source-after-menu','primary-after-menu','startup-after-menu','restored-pin-lock')
        $reported = $mutated -and $scenario -cne 'after-menu-error'
        Check-LiveStartup (($menuState -ceq 'legacy-startup-menu-observed') -eq $reported -and $context.MenuDispositionAccepted -eq $mutated -and
            [IO.File]::Exists($primary) -eq (-not $mutated)) 'actual Menu mutation and truthful accepted progress, no foreign deletion'
        Check-LiveStartup ($context.VerifyMenuSettlement() -eq $mutated -and (-not $mutated -or ($context.MenuAttempted -and $context.MenuHandleClosed -and $context.MenuAbsenceObserved))) 'post-transition exact live settlement without primary'
        if ($mutated) {
            Check-LiveStartup (-not $context.VerifyHandoff() -and -not $context.VerifyStartupSettlement() -and
                -not $manager.VerifyDurableMenuOriginalResult($first,$main,$oldLegacy,$true)) 'old primary-requiring APIs do not authorize new state'
            if ($scenario -ceq 'restored-pin-lock') {
                $denied = $false; try { [IO.File]::Move($legacy,(Join-Path $case 'forbidden-legacy-move')) } catch [IO.IOException] { $denied = ($_.Exception.HResult -band 0xffff) -eq 32 }
                Check-LiveStartup $denied 'exact restored identity held through settlement, native DELETE denied'
                $deniedWrite = $false; $writer = $null
                try { $writer = [IO.File]::Open($legacy,[IO.FileMode]::Open,[IO.FileAccess]::Write,([IO.FileShare]::Read -bor [IO.FileShare]::Delete)) }
                catch [IO.IOException] { $deniedWrite = ($_.Exception.HResult -band 0xffff) -eq 32 }
                finally { if ($null -ne $writer) { $writer.Dispose() } }
                Check-LiveStartup ($deniedWrite -and (Get-LiveStartupHash $legacy) -ceq $prepared.Legacy.Hash) 'retained pin permits compatible read-only hash while refusing writes, exact bytes preserved'
            }
        }
        if ($scenario -ceq 'result-after-menu') { [IO.File]::WriteAllText($resultPath,'synthetic post-Menu result change') }
        if ($scenario -ceq 'source-after-menu') { $metadata = Join-Path (Join-Path $install 'backups') ('installer-legacy-source-' + $op + '.bin'); $stamp = [IO.File]::GetCreationTimeUtc($metadata); [LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp.AddMinutes(1)) }
        if ($scenario -ceq 'primary-after-menu') { [IO.File]::WriteAllBytes($primary,[IO.File]::ReadAllBytes((Join-Path $case 'app\ProGo.exe'))) }
        if ($scenario -ceq 'startup-after-menu') { [IO.File]::WriteAllText($startupLink,'synthetic foreign Startup') }
        $settlementLost = $scenario -in @('result-after-menu','source-after-menu','primary-after-menu','startup-after-menu')
        Check-LiveStartup ($context.VerifyMenuSettlement() -eq ($mutated -and -not $settlementLost)) 'post-transition checks continue to require source/result/Startup/primary absence'
        Check-LiveStartup (-not $context.WholeRollbackVerified -and -not $context.InstallSucceeded -and -not $context.RestartAuthority) 'scoped settlement never whole rollback or restart'
        $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = Get-LiveStartupHash $file.FullName }
        $calls = $fixture.MenuCalls; $startupCalls = $fixture.StartupCalls
        $again = Invoke-ProGoInstallerFailureMenu $true $menuBackup $menuInstall $state $programs $startup $menuContext ([Threading.CancellationToken]::None)
        $expectedRepeat = if ($mutated -and -not $settlementLost) { 'legacy-startup-menu-observed' } else { 'blocked' }
        Check-LiveStartup ($again -ceq $expectedRepeat -and $fixture.MenuCalls -eq $calls -and $fixture.StartupCalls -eq $startupCalls -and
            @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count -and $heldError.Exception.Message -ceq 'synthetic original install failure') 'repeat handler observes only, including postmutation error/cancel/partial; original error unchanged'
        foreach ($path in $inventory.Keys) { Check-LiveStartup ((Get-LiveStartupHash $path) -ceq $inventory[$path]) 'repeat preserves every source partial result and foreign byte' }
        }
        if ($scenario -in @('complete','disposed-context','disposed-context-menu')) {
            $info = New-Object Diagnostics.ProcessStartInfo; $info.FileName = Join-Path $PSHOME 'powershell.exe'
            $info.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -BlockedChild -CaseDirectory "' + $case + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(20000)) { throw 'Live Startup child exceeded bounded wait.' }
            Check-LiveStartup ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and $errTask.Result.Length -eq 0 -and $outTask.Result.Contains('LIVE_STARTUP_CHILD blocked=True; replayPerformed=False')) 'fresh process and serialized flags never recover live authority'
            $process.Dispose(); $process = $null
        }
        if ($null -ne $metadata) { [LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp); $metadata = $null }
        if ($null -ne $foreignContext) { $foreignContext.Dispose(); $foreignContext = $null }; if ($null -ne $foreignStage) { Remove-ProGoInstallerStage $foreignStage; $foreignStage = $null }
        $context.Dispose(); Check-LiveStartup (-not $context.VerifyHandoff() -and -not $context.VerifyStartupSettlement() -and -not $context.VerifyMenuSettlement()) 'historical result cannot survive disposed proofs as authority'
        $context = $null; $lease = $null; $physical = $null; $cancel.Dispose(); $cancel = $null
        foreach ($path in $private.Keys) { Check-LiveStartup ((Get-LiveStartupHash $path) -ceq $private[$path]) 'private and foreign bytes unchanged' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy Startup live MenuFlow=' + $MenuFlow + '; ' + $scenario)
    }
    Write-Host ('Legacy Startup live tests PASS: ' + $cases.Count + '; fresh-process refusal=2; MenuFlow=' + $MenuFlow + '; whole rollback/restart/power-loss NOT_RUN')
} finally {
    try { if ($null -ne $metadata) { [LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp) } }
    finally { try { if ($null -ne $context) { $context.Dispose() } } finally { if ($null -ne $lease) { $lease.Dispose() }; if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $cancel) { $cancel.Dispose() } } }
    if ($null -ne $process) {
        try { if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Live Startup child did not settle.' } }; if ($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Live Startup child streams did not drain.' } } finally { $process.Dispose() }
    }
    if ($null -ne $foreignContext) { $foreignContext.Dispose() }; if ($null -ne $foreignStage) { Remove-ProGoInstallerStage $foreignStage }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ([IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-live-startup-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Live Startup cleanup refuses unowned scope.' }
        foreach ($path in @((Get-Item -LiteralPath $work -Force)) + @(Get-ChildItem -LiteralPath $work -Recurse -Force)) { if ($path.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Live Startup cleanup refuses links.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
