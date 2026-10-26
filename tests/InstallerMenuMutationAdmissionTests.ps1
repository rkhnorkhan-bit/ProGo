Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Menu mutation admission helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-MenuMutation($Condition, $Name) { if (-not $Condition) { throw ('Menu mutation admission failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-menu-mutation-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $cancel = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $actualInvoke = (Get-Command Invoke-ProGoInstallerMenuRollback).ScriptBlock
    $scenarios = @('before-crash', 'after-crash', 'preserved', 'persist-fail', 'short-write', 'tampered',
        'foreign-replaced', 'foreign-runtime', 'foreign-snapshot', 'missing-admission', 'delete-locked', 'cancelled-before', 'cancel-after-persist', 'foreign-admission', 'legacy-removed', 'post-startup', 'startup-result-changed', 'legacy-appeared', 'replace-after-persist')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $programs 'ProGo\ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup, (Join-Path $programs 'ProGo') | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'preserved') { [IO.File]::WriteAllText($link, 'synthetic customization') }
        if ($scenario -eq 'legacy-removed') {
            $manager = New-Object ProGo.ApplicationShortcuts($install, $programs, $startup)
            $manager.Install($true,$true,$false)
            [IO.File]::Move($link,(Join-Path $programs 'ProGo\ProGo Status.lnk'))
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $false -NoStartMenu ($scenario -eq 'preserved') -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) { $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true); throw 'synthetic pending receipt interruption' }
        $interrupted = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check','shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $interrupted = $_.Exception.Message.Contains('synthetic pending receipt interruption') }
        Check-MenuMutation $interrupted 'actual durable pending receipt exists before mutation admission'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $mutationPath = Join-Path (Split-Path -Parent $journal) ('installer-menu-admission-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
        if ($scenario -in @('post-startup','startup-result-changed')) {
            Check-MenuMutation (Invoke-ProGoInstallerStartupRollback $journal $install $programs $startup) 'real Startup rollback precedes Menu admission'
            Check-MenuMutation (Complete-ProGoInstallerStartupMutationSettlement $journal $install $programs $startup) 'real durable Startup observation verifies before Menu admission'
        }
        $originalHash = (Get-FileHash -LiteralPath $link).Hash
        $cancel = New-Object Threading.CancellationTokenSource
        $fault = @{ Mode = $scenario }
        function Write-ProGoInstallerMenuAdmissionBytes($Stream, [byte[]]$Bytes) {
            if ($fault.Mode -eq 'persist-fail') { throw 'synthetic admission persistence failure' }
            if ($fault.Mode -eq 'short-write') { $Stream.WriteByte($Bytes[0]); $Stream.Flush($true); return }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
            if ($fault.Mode -eq 'replace-after-persist') {
                $bytesBefore = [IO.File]::ReadAllBytes($link); $creation = [IO.File]::GetCreationTimeUtc($link)
                [IO.File]::Move($link,(Join-Path $case 'retained-original.lnk'))
                [IO.File]::WriteAllBytes($link,$bytesBefore); [IO.File]::SetCreationTimeUtc($link,$creation)
            }
            if ($fault.Mode -eq 'cancel-after-persist') { $cancel.Cancel() }
        }
        $invoked = $false; $failed = $false
        try {
            if ($scenario -eq 'cancelled-before') { $cancel.Cancel() }
            if ($scenario -eq 'delete-locked') { $held = [IO.File]::Open($link,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read) }
            if ($scenario -in @('after-crash','preserved','persist-fail','short-write','delete-locked','cancelled-before','cancel-after-persist','legacy-removed','post-startup','startup-result-changed','replace-after-persist')) {
                $invoked = & $actualInvoke $journal $install $programs $startup $cancel.Token
            } elseif ($scenario -ne 'missing-admission') {
                $lease = Open-ProGoInstallerMenuMutationAdmission $journal $install $programs $startup
                $lease.Dispose() # physical restart boundary: only durable evidence remains
            }
        } catch { $failed = $true }
        finally { if ($null -ne $held) { $held.Dispose(); $held = $null }; $cancel.Dispose(); $cancel = $null }
        Check-MenuMutation ($failed -eq ($scenario -in @('persist-fail','short-write','cancelled-before','cancel-after-persist','replace-after-persist'))) 'persistence/readback/cancellation failures propagate before dangerous mutation'
        Check-MenuMutation ($invoked -eq ($scenario -in @('after-crash','preserved','legacy-removed','post-startup','startup-result-changed'))) 'only verified mutation or preserved no-op completes'
        if ($scenario -notin @('after-crash','legacy-removed','post-startup','startup-result-changed')) { Check-MenuMutation ((Get-FileHash -LiteralPath $link).Hash -ceq $originalHash) 'unapplied/failed/cancelled admission never changes the owned Menu bytes' }
        if ($scenario -eq 'tampered') { [IO.File]::WriteAllText($mutationPath, '{}') }
        if ($scenario -eq 'foreign-replaced') {
            $bytes = [IO.File]::ReadAllBytes($link); [IO.File]::Move($link,(Join-Path $case 'retained-original.lnk')); [IO.File]::WriteAllBytes($link,$bytes)
        }
        $foreignPath = Join-Path (Split-Path -Parent $journal) 'installer-menu-admission-unknown.json'
        $expectsForeignAdmission = $scenario -ceq 'foreign-admission'
        if ($expectsForeignAdmission) { [IO.File]::WriteAllText($foreignPath,'synthetic conflicting admission') }
        Check-MenuMutation ([IO.File]::Exists($foreignPath) -eq $expectsForeignAdmission) 'foreign admission injection exists exactly in its own scenario; neighbours have no marker'
        if ($scenario -eq 'foreign-runtime') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic external runtime') }
        if ($scenario -eq 'foreign-snapshot') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic external snapshot') }
        if ($scenario -eq 'startup-result-changed') {
            $resultPath = Join-Path (Split-Path -Parent $journal) ('installer-startup-outcome-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
            [IO.File]::WriteAllText($resultPath,'{}')
        }
        if ($scenario -eq 'legacy-appeared') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo Status.lnk'),'synthetic external legacy') }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $expected = if ($scenario -in @('after-crash','preserved','legacy-removed','post-startup')) { 'original-observed' } elseif ($scenario -in @('before-crash','delete-locked','cancel-after-persist')) { 'not-applied' } else { 'blocked' }
        for ($read = 0; $read -lt 3; $read++) {
            $observed = Get-ProGoInstallerMenuMutationState $journal $install $programs $startup
            Check-MenuMutation ($observed -ceq $expected) ('read-only crash state; expected=' + $expected + '; observed=' + $observed)
        }
        if ($scenario -eq 'tampered') {
            $retryFailed = $false
            try { [void](& $actualInvoke $journal $install $programs $startup) } catch { $retryFailed = $true }
            Check-MenuMutation $retryFailed 'explicit retry cannot overwrite corrupt admission or reach native mutation'
        }
        if ($expectsForeignAdmission) {
            $conflictRefused = $false; $lease = $null
            try { $lease = Open-ProGoInstallerMenuMutationAdmission $journal $install $programs $startup }
            catch { $conflictRefused = $true }
            finally { if ($null -ne $lease) { $lease.Dispose() } }
            Check-MenuMutation $conflictRefused 'explicit mutation admission refuses the actual foreign sibling before any write'
            Check-MenuMutation ((Get-FileHash -LiteralPath $foreignPath).Hash -ceq $before[$foreignPath] -and
                (Get-FileHash -LiteralPath $mutationPath).Hash -ceq $before[$mutationPath] -and
                (Get-FileHash -LiteralPath $link).Hash -ceq $originalHash) 'refusal preserves foreign record own admission and exact Menu bytes'
        }
        $afterRead = @(Get-ChildItem -LiteralPath $case -Recurse -File)
        Check-MenuMutation ($afterRead.Count -eq $before.Count) 'crash classification creates no resource'
        foreach ($file in $afterRead) { Check-MenuMutation ((Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'read-only classification preserves every byte' }
        $settled = (Get-ProGoInstallerMenuMutationState $journal $install $programs $startup) -ceq 'original-observed'
        Check-MenuMutation ($settled -eq ($expected -ceq 'original-observed')) 'read-only settlement verifies real original state without replay or terminal publication'
        Check-MenuMutation (@(Get-ChildItem -LiteralPath $case -Recurse -File).Count -eq $before.Count) 'settlement creates no outcomes or auxiliary files'
        foreach ($path in $before.Keys) { Check-MenuMutation ([IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $before[$path]) 'settlement never rewrites or deletes existing resources' }
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-MenuMutation $blocked 'intent/result never permits new installation or Launch'
        if ([IO.File]::Exists($mutationPath)) { $probe = [IO.File]::Open($mutationPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Menu mutation admission ' + $scenario)
    }
    Write-Host ('Installer Menu mutation admission tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-menu-mutation-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}

# Additive isolated restore cases use Foundation headroom; original suite and deadlines unchanged.
& (Join-Path $Root 'tests\InstallerLegacyOwnedRestoreTests.ps1')
