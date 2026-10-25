Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer auxiliary helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-MenuAdmission($Condition, $Name) { if (-not $Condition) { throw ('Installer Menu rollback admission failed: ' + $Name) } }
# Failure-only, read-only diagnostics of the ORIGINAL fixture. Never substitutes a proof result.
function Get-MenuAdmissionHandleFacts($Handle) {
    $flags = [Reflection.BindingFlags]'Static,NonPublic'
    $type = [ProGo.ApplicationShortcuts]
    $origin = $type.GetMethod('LegacyOrigin', $flags).Invoke($null, [object[]]@($Handle))
    $infoType = $type.GetNestedType('LegacyFileInfo', [Reflection.BindingFlags]::NonPublic)
    $nativeArgs = [object[]]@($Handle, [Activator]::CreateInstance($infoType))
    if (-not $type.GetMethod('GetFileInformationByHandle', $flags).Invoke($null, $nativeArgs)) { throw 'Native read-only fixture identity unavailable.' }
    $fields = [Reflection.BindingFlags]'Instance,NonPublic'
    $info = $nativeArgs[1]; $creation = $infoType.GetField('Creation', $fields).GetValue($info)
    return [pscustomobject]@{ Origin = $origin; Volume = $infoType.GetField('Volume', $fields).GetValue($info);
        IndexHigh = $infoType.GetField('IndexHigh', $fields).GetValue($info); IndexLow = $infoType.GetField('IndexLow', $fields).GetValue($info);
        CreationHigh = $creation.dwHighDateTime; CreationLow = $creation.dwLowDateTime }
}
function Write-MenuAdmissionRefusal($Directory, $InstallDirectory, $ProgramsDirectory, $StartupDirectory, $Receipt, $DiagnosticRoot) {
    $phase = 'snapshot'; $timer = [Diagnostics.Stopwatch]::StartNew(); $stream = $null
    try {
        $snapshot = $false; try { Assert-ProGoInstallerSnapshotMaterial $backup; $snapshot = $true } catch { }
        $runtime = $false; try { Assert-ProGoInstallerRuntime $backup; $runtime = $true } catch { }
        $pending = Test-ProGoInstallerPendingInstalledProof $Directory $InstallDirectory
        $auxiliary = Test-ProGoInstallerPendingAuxiliaryProof $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory
        $contextOk = $false; $contextState = 'unavailable'; $phase = 'context'
        try { $context = Get-ProGoInstallerStartupMutationContext $Directory $InstallDirectory $ProgramsDirectory $StartupDirectory; $contextOk = $true; $contextState = $context.State } catch { }
        $phase = 'native-verifiers'
        $manager = New-Object ProGo.ApplicationShortcuts($InstallDirectory, $ProgramsDirectory, $StartupDirectory)
        $typedMenu = $manager.VerifyMenuInstallEvidence($Receipt.Menu)
        $typedStartup = $manager.VerifyStartupInstallEvidence($Receipt)
        $typedLegacy = $manager.VerifyLegacyInstallEvidence($Receipt.Legacy)
        $map = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
        foreach ($property in $Receipt.Menu.PSObject.Properties) { $map.Add($property.Name, $property.Value) }
        $durableMenu = $manager.VerifyDurableMenuRollbackEvidence($map)
        $primary = Join-Path $ProgramsDirectory 'ProGo\ProGo.lnk'
        $stream = [IO.File]::Open($primary,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        $facts = Get-MenuAdmissionHandleFacts $stream.SafeFileHandle
        $stream.Dispose(); $stream = $null
        $originPresent = $null -ne $Receipt.Menu.OriginIdentity
        $originSame = $originPresent -and $Receipt.Menu.OriginIdentity -ceq $facts.Origin
        $hashSame = (Get-FileHash -LiteralPath $primary).Hash -ceq $Receipt.Menu.Hash
        $legacyReplacementSame = $Receipt.Legacy.ReplacementHash -ceq $Receipt.Menu.Hash
        Write-Host ('MENU_ADMISSION_REFUSAL case=legacy-removed snapshot=' + $snapshot + '; runtime=' + $runtime +
            '; pending=' + $pending + '; auxiliary=' + $auxiliary + '; context=' + $contextOk + '; contextState=' + $contextState +
            '; typedMenu=' + $typedMenu + '; typedStartup=' + $typedStartup + '; typedLegacy=' + $typedLegacy +
            '; durableMenu=' + $durableMenu + '; originPresent=' + $originPresent + '; originSame=' + $originSame +
            '; bytesSame=' + $hashSame + '; legacyRemoved=' + ($Receipt.Legacy.Disposition -ceq 'removed') + '; replacementSame=' + $legacyReplacementSame)
        # One distinguishing reproduction under a NEW own GUID root. Same existing EnsureLink
        # overload and vacated Menu-name sequence; no production modification or relaxed oracle.
        $phase = 'probe-load'
        if (-not ('ProGo.Tests.MenuRenameIdentityProbe' -as [type])) {
            Add-Type -Path (Join-Path $PSScriptRoot 'MenuRenameIdentityProbe.cs')
        }
        $phase = 'probe-run'
        Write-Host ([ProGo.Tests.MenuRenameIdentityProbe]::Run([ProGo.ApplicationShortcuts], $DiagnosticRoot))
    } catch {
        Write-Host ('MENU_DIAGNOSTIC_ERROR phase=' + $phase + '; type=' + $_.Exception.GetType().Name + '; elapsedMs=' + $timer.ElapsedMilliseconds)
        $frames = (New-Object Diagnostics.StackTrace($_.Exception, $false)).GetFrames()
        if ($null -ne $frames) { foreach ($frame in @($frames | Select-Object -First 5)) {
            $method = $frame.GetMethod(); if ($null -ne $method) { Write-Host ('MENU_SAFE_STACK method=' + $method.Name + '; il=' + $frame.GetILOffset()) }
        } }
    } finally { if ($null -ne $stream) { $stream.Dispose() }; $timer.Stop() }
}
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-menu-admission-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('created', 'custom-preserved', 'legacy-removed', 'absent-preserved', 'missing-receipt', 'schema', 'unknown-field',
        'boolean-type', 'install-identity', 'startup-location', 'menu-location', 'legacy-location', 'ownership',
        'startup-changed', 'menu-changed', 'legacy-appeared', 'runtime-changed', 'snapshot-changed',
        'admission-changed', 'conflict', 'other-pending', 'trusted-location', 'invalid-json', 'oversized', 'receipt-busy', 'menu-replaced', 'missing-origin', 'null-origin', 'wrong-origin', 'post-startup', 'post-startup-replaced', 'unproved-startup-absence')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'
        New-Item -ItemType Directory -Path $install, $programs, $startup, (Join-Path $programs 'ProGo') | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'custom-preserved') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo.lnk'), 'synthetic custom shortcut') }
        if ($scenario -eq 'legacy-removed') {
            $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
            $manager.Install($true, $true, $false)
            [IO.File]::Move((Join-Path $programs 'ProGo\ProGo.lnk'), (Join-Path $programs 'ProGo\ProGo Status.lnk'))
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $false -NoStartMenu ($scenario -in @('custom-preserved', 'absent-preserved')) -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption after durable auxiliary proof'
        }
        $caught = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $caught = $_.Exception.Message.Contains('synthetic interruption after durable auxiliary proof') }
        Check-MenuAdmission $caught 'actual writer stopped after durable receipt before publication'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
        if ($scenario -in @('post-startup','post-startup-replaced')) {
            Check-MenuAdmission (Invoke-ProGoInstallerStartupRollback $journal $install $programs $startup) 'real existing Startup rollback precedes Menu classification'
            Check-MenuAdmission (Complete-ProGoInstallerStartupMutationSettlement $journal $install $programs $startup) 'Startup result independently verified before Menu prerequisite'
        }
        if ($scenario -eq 'unproved-startup-absence') { [IO.File]::Delete((Join-Path $startup 'ProGo.lnk')) }
        if ($scenario -eq 'missing-origin') { $saved.Completion.Startup.Menu.PSObject.Properties.Remove('OriginIdentity') }
        if ($scenario -eq 'null-origin') { $saved.Completion.Startup.Menu.OriginIdentity = $null }
        if ($scenario -eq 'wrong-origin') { $saved.Completion.Startup.Menu.OriginIdentity = ('0' * 64) }
        if ($scenario -eq 'missing-receipt') { $saved.Completion.Startup = $null }
        if ($scenario -eq 'schema') { $saved.Completion.Startup.Schema = 2 }
        if ($scenario -eq 'unknown-field') { $saved.Completion.Startup | Add-Member NoteProperty Path 'synthetic untrusted location' }
        if ($scenario -eq 'boolean-type') { $saved.Completion.Startup.Exists = 'true' }
        if ($scenario -eq 'install-identity') { $saved.Completion.Startup.InstallIdentity = ('0' * 64) }
        if ($scenario -eq 'startup-location') { $saved.Completion.Startup.LocationIdentity = ('0' * 64) }
        if ($scenario -eq 'menu-location') { $saved.Completion.Startup.Menu.LocationIdentity = ('0' * 64) }
        if ($scenario -eq 'legacy-location') { $saved.Completion.Startup.Legacy.LocationIdentity = ('0' * 64) }
        if ($scenario -eq 'ownership') { $saved.Completion.Startup.Menu.Disposition = 'foreign' }
        if ($scenario -notin @('post-startup','post-startup-replaced')) { [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress)) }
        if ($scenario -in @('menu-replaced','post-startup-replaced')) {
            $primary = Join-Path $programs 'ProGo\ProGo.lnk'; $bytes = [IO.File]::ReadAllBytes($primary)
            [IO.File]::Move($primary,(Join-Path $case 'retained-original-menu.lnk')); [IO.File]::WriteAllBytes($primary,$bytes)
        }
        if ($scenario -eq 'startup-changed') { [IO.File]::WriteAllText((Join-Path $startup 'ProGo.lnk'), 'synthetic external bytes') }
        if ($scenario -eq 'menu-changed') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo.lnk'), 'synthetic external bytes') }
        if ($scenario -eq 'legacy-appeared') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo Status.lnk'), 'synthetic external bytes') }
        if ($scenario -eq 'runtime-changed') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic external bytes') }
        if ($scenario -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic external bytes') }
        if ($scenario -eq 'admission-changed') { [IO.File]::WriteAllText((Join-Path $journal 'admit-000000.json'), '{}') }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText((Join-Path $journal 'installed.json'), '{}') }
        if ($scenario -eq 'other-pending') { New-Item -ItemType Directory -Path (Join-Path (Split-Path -Parent $journal) ('installer-journal-' + [Guid]::NewGuid().ToString('N'))) | Out-Null }
        if ($scenario -eq 'invalid-json') { [IO.File]::WriteAllText($pending, '{') }
        if ($scenario -eq 'oversized') { [IO.File]::WriteAllText($pending, (' ' * 1048577)) }
        $expectedPrograms = if ($scenario -eq 'trusted-location') { Join-Path $case 'other-Programs' } else { $programs }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $expected = $scenario -in @('created', 'custom-preserved', 'legacy-removed', 'absent-preserved', 'post-startup')
        if ($scenario -eq 'receipt-busy') { $held = [IO.File]::Open($pending, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::ReadWrite) }
        try { for ($read = 0; $read -lt 3; $read++) {
            $admission = Get-ProGoInstallerMenuRollbackAdmission $journal $install $expectedPrograms $startup
            $matchesOriginalExpectation = (($null -ne $admission) -eq $expected)
            if (-not $matchesOriginalExpectation -and $scenario -eq 'legacy-removed') {
                Write-MenuAdmissionRefusal $journal $install $expectedPrograms $startup $receipt (Join-Path $work ('diagnostic-' + [Guid]::NewGuid().ToString('N')))
            }
            Check-MenuAdmission $matchesOriginalExpectation 'repeated fresh classification remains fail-closed'
        } }
        finally { if ($null -ne $held) { $held.Dispose(); $held = $null } }
        Check-MenuAdmission (($null -ne $admission) -eq $expected) 'only full current snapshot/runtime/auxiliary ownership proof admits Menu classification'
        if ($expected) {
            $kind = if ($scenario -in @('custom-preserved', 'absent-preserved')) { 'preserve-only' } else { 'created-from-absence' }
            Check-MenuAdmission ($admission.Schema -eq 1 -and $admission.Kind -ceq $kind -and
                $admission.InstallIdentity -ceq (Get-ProGoInstallerIdentity $install) -and
                $admission.LocationIdentity -ceq (Get-ProGoInstallerIdentity (Join-Path $programs 'ProGo\ProGo.lnk')) -and
                $admission.ReceiptHash -ceq (Get-FileHash -LiteralPath $pending).Hash) 'classification binds independently trusted install/location and exact durable receipt'
            Check-MenuAdmission ($admission.BeforeExists -eq $receipt.Menu.BeforeExists -and $admission.BeforeHash -ceq $receipt.Menu.BeforeHash -and
                $admission.Exists -eq $receipt.Menu.Exists -and $admission.Hash -ceq $receipt.Menu.Hash) 'actual old/current bytes or absence remain explicit without claiming preserved state owned'
            Check-MenuAdmission (-not $admission.MutationAllowed -and $admission.OriginIdentity -ceq $receipt.Menu.OriginIdentity -and
                ($kind -cne 'created-from-absence' -or $admission.OriginIdentity -cmatch '^[A-F0-9]{64}$')) 'origin is required only for actual created ownership; classification never grants mutation authority'
            Check-MenuAdmission (@($admission.PSObject.Properties.Name | Where-Object { $_ -match 'Path|Bytes|Command|Credential' }).Count -eq 0) 'admission contains hashes/booleans only, no operation target or private bytes'
        }
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-MenuAdmission $blocked 'Menu admission alone never mutates state, publishes rollback or authorizes retry'
        $after = @(Get-ChildItem -LiteralPath $case -Recurse -File)
        Check-MenuAdmission ($after.Count -eq $before.Count) 'read-only revalidation preserves file count'
        foreach ($file in $after) { Check-MenuAdmission ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'all runtime/auxiliary/private/foreign/evidence bytes preserved' }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-MenuAdmission ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private and foreign files unchanged' }
        foreach ($file in $after) { $probe = [IO.File]::Open($file.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Menu rollback admission ' + $scenario)
    }
    Write-Host ('Installer Menu rollback admission tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-menu-admission-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
