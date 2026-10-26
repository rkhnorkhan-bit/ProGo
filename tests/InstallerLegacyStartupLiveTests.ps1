param([switch]$BlockedChild,[string]$CaseDirectory)
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
    if ([IO.Path]::GetFileName($CaseDirectory) -cnotin @('complete','disposed-context') -or [IO.Path]::GetFileName($parent) -cnotmatch '^ProGo-live-startup-[a-f0-9]{32}$' -or
        [IO.Path]::GetDirectoryName($parent) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Live Startup child refuses unowned scope.' }
    foreach ($path in @($parent,$CaseDirectory)) { if ([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint) { throw 'Live Startup child refuses redirected scope.' } }
    $install = Join-Path $CaseDirectory 'app'; $programs = Join-Path $CaseDirectory 'Programs'; $startup = Join-Path $CaseDirectory 'Startup'
    $journals = @(Get-ChildItem -LiteralPath (Join-Path $install 'backups') -Directory | Where-Object { $_.Name -cmatch '^installer-journal-[a-f0-9]{32}$' })
    if ($journals.Count -ne 1) { throw 'Live Startup child journal mismatch.' }
    $backup = [pscustomobject]@{ Journal = [pscustomobject]@{ Directory = $journals[0].FullName } }
    $serialized = [pscustomobject]@{ StartupInvoked = $true; MenuAllowed = $false; OperationId = [IO.Path]::GetFileName($journals[0].FullName) }
    if ((Invoke-ProGoInstallerFailureStartup $true $backup $install $programs $startup) -cne 'blocked' -or
        (Invoke-ProGoInstallerFailureStartup $true $backup $install $programs $startup $serialized) -cne 'blocked' -or
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
$scenario = 'initial'; $work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-live-startup-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $physical = $null; $lease = $null; $context = $null; $cancel = $null; $metadata = $null; $stamp = $null
$process = $null; $outTask = $null; $errTask = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $cases = @('complete','preserved-startup','wrong-preparation','wrong-root','wrong-operation','disposed-context','disposed-lease','disposed-physical',
        'source-changed','result-tampered','legacy-equal-byte','primary-replaced','startup-replaced','error-after-legacy','error-after-startup','cancel-before','cancel-after-legacy','restore-partial','restore-result-fault')
    foreach ($scenario in $cases) {
        $fixture = @{ Mode = $scenario; StartupCalls = 0 }
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'; $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        $menu = Join-Path $programs 'ProGo'; $primary = Join-Path $menu 'ProGo.lnk'; $legacy = Join-Path $menu 'ProGo Status.lnk'; $startupLink = Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install,$menu,$startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic old executable')
        $private = @{}; foreach ($name in @('settings.json','vault.enc','key-reference.txt','foreign.txt')) { $path = Join-Path $install $name; [IO.File]::WriteAllText($path,'synthetic private or foreign bytes'); $private[$path] = Get-LiveStartupHash $path }
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
        $expected = $scenario -in @('complete','preserved-startup','wrong-preparation')
        Check-LiveStartup ($heldError.Exception.Message -ceq 'synthetic original install failure' -and ($state -ceq 'legacy-startup-observed-menu-blocked') -eq $expected) 'real failure handler preserves original error and reports only scoped live settlement'
        $settled = $expected -or $scenario -ceq 'error-after-startup'
        Check-LiveStartup ($context.VerifyStartupSettlement() -eq $settled -and [IO.File]::Exists($startupLink) -eq (-not $settled -or $scenario -ceq 'preserved-startup')) 'Startup rollback only follows verified live Legacy handoff'
        Check-LiveStartup ((Invoke-ProGoInstallerFailureMenu $true $backup $install $state $programs $startup $context) -ceq 'blocked' -and [IO.File]::Exists($primary) -and -not $context.MenuAllowed) 'real Menu handler is explicitly blocked and retains primary'
        Check-LiveStartup (-not $context.WholeRollbackVerified -and -not $context.InstallSucceeded -and -not $context.RestartAuthority) 'scoped live settlement grants no whole success or restart authority'
        Check-LiveStartup (-not $manager.VerifyDurableInstallEvidence($first,$main,$oldLegacy) -and -not $manager.VerifyDurableStartupOriginalResult($first,$main,$oldLegacy)) 'old receipt-only APIs stay strict before and after own Startup mutation'
        $inventory = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $inventory[$file.FullName] = Get-LiveStartupHash $file.FullName }
        $calls = $fixture.StartupCalls
        $again = Invoke-ProGoInstallerFailureStartup $true $callBackup $callInstall $programs $startup $context ([Threading.CancellationToken]::None)
        $repeatState = if ($scenario -ceq 'error-after-startup') { 'legacy-startup-observed-menu-blocked' } else { $state }
        Check-LiveStartup ($again -ceq $repeatState -and $fixture.StartupCalls -eq $calls -and @(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $inventory.Count) 'repeat handler never retries mutation after error cancellation or settlement'
        foreach ($path in $inventory.Keys) { Check-LiveStartup ((Get-LiveStartupHash $path) -ceq $inventory[$path]) 'repeat preserves every partial source result and foreign byte' }
        if ($scenario -in @('complete','disposed-context')) {
            $info = New-Object Diagnostics.ProcessStartInfo; $info.FileName = Join-Path $PSHOME 'powershell.exe'
            $info.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -BlockedChild -CaseDirectory "' + $case + '"'
            $info.UseShellExecute = $false; $info.CreateNoWindow = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
            $process = [Diagnostics.Process]::Start($info); $outTask = $process.StandardOutput.ReadToEndAsync(); $errTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(20000)) { throw 'Live Startup child exceeded bounded wait.' }
            Check-LiveStartup ([Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000) -and $process.ExitCode -eq 0 -and $errTask.Result.Length -eq 0 -and $outTask.Result.Contains('LIVE_STARTUP_CHILD blocked=True; replayPerformed=False')) 'fresh process and serialized flags never recover live authority'
            $process.Dispose(); $process = $null
        }
        if ($null -ne $metadata) { [LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp); $metadata = $null }
        $context.Dispose(); Check-LiveStartup (-not $context.VerifyHandoff() -and -not $context.VerifyStartupSettlement()) 'historical result cannot survive disposed proofs as authority'
        $context = $null; $lease = $null; $physical = $null; $cancel.Dispose(); $cancel = $null
        foreach ($path in $private.Keys) { Check-LiveStartup ((Get-LiveStartupHash $path) -ceq $private[$path]) 'private and foreign bytes unchanged' }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Legacy Startup live ' + $scenario)
    }
    Write-Host ('Legacy Startup live tests PASS: ' + $cases.Count + '; fresh-process refusal=2; Menu blocked; whole rollback/restart/power-loss NOT_RUN')
} finally {
    try { if ($null -ne $metadata) { [LegacyMetadataThreatFixture]::SetCreation($metadata,$stamp) } }
    finally { try { if ($null -ne $context) { $context.Dispose() } } finally { if ($null -ne $lease) { $lease.Dispose() }; if ($null -ne $physical) { $physical.Dispose() }; if ($null -ne $cancel) { $cancel.Dispose() } } }
    if ($null -ne $process) {
        try { if (-not $process.HasExited) { $process.Kill(); if (-not $process.WaitForExit(5000)) { throw 'Live Startup child did not settle.' } }; if ($null -ne $outTask -and $null -ne $errTask -and -not [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($outTask,$errTask),5000)) { throw 'Live Startup child streams did not drain.' } } finally { $process.Dispose() }
    }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ([IO.Directory]::Exists($work)) {
        if ([IO.Path]::GetFileName($work) -cnotmatch '^ProGo-live-startup-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($work) -cne [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)) { throw 'Live Startup cleanup refuses unowned scope.' }
        foreach ($path in @((Get-Item -LiteralPath $work -Force)) + @(Get-ChildItem -LiteralPath $work -Recurse -Force)) { if ($path.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Live Startup cleanup refuses links.' } }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
