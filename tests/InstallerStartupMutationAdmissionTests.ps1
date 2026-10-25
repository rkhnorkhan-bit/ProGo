Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Owned Startup rollback helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-MutationAdmission($Condition, $Name) { if (-not $Condition) { throw ('Startup mutation admission failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-startup-mutation-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $cancel = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $actualInvoke = (Get-Command Invoke-ProGoInstallerStartupRollback).ScriptBlock
    $scenarios = @('before-crash', 'after-crash', 'preserved', 'persist-fail', 'short-write', 'tampered',
        'foreign-replaced', 'foreign-runtime', 'foreign-snapshot', 'missing-admission', 'delete-locked', 'cancelled-before', 'cancel-after-persist', 'foreign-admission')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'preserved') { [IO.File]::WriteAllText($link, 'synthetic customization') }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup ($scenario -eq 'preserved') -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) { $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true); throw 'synthetic pending receipt interruption' }
        $interrupted = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check','shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $interrupted = $_.Exception.Message.Contains('synthetic pending receipt interruption') }
        Check-MutationAdmission $interrupted 'actual durable pending receipt exists before mutation admission'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $mutationPath = Join-Path (Split-Path -Parent $journal) ('installer-startup-admission-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
        $originalHash = (Get-FileHash -LiteralPath $link).Hash
        $cancel = New-Object Threading.CancellationTokenSource
        $fault = @{ Mode = $scenario }
        function Write-ProGoInstallerStartupAdmissionBytes($Stream, [byte[]]$Bytes) {
            if ($fault.Mode -eq 'persist-fail') { throw 'synthetic admission persistence failure' }
            if ($fault.Mode -eq 'short-write') { $Stream.WriteByte($Bytes[0]); $Stream.Flush($true); return }
            $Stream.Write($Bytes,0,$Bytes.Length); $Stream.Flush($true)
            if ($fault.Mode -eq 'cancel-after-persist') { $cancel.Cancel() }
        }
        $invoked = $false; $failed = $false
        try {
            if ($scenario -eq 'cancelled-before') { $cancel.Cancel() }
            if ($scenario -eq 'delete-locked') { $held = [IO.File]::Open($link,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read) }
            if ($scenario -in @('after-crash','preserved','persist-fail','short-write','delete-locked','cancelled-before','cancel-after-persist')) {
                $invoked = & $actualInvoke $journal $install $programs $startup $cancel.Token
            } elseif ($scenario -ne 'missing-admission') {
                $lease = Open-ProGoInstallerStartupMutationAdmission $journal $install $programs $startup
                $lease.Dispose() # physical restart boundary: only durable evidence remains
            }
        } catch { $failed = $true }
        finally { if ($null -ne $held) { $held.Dispose(); $held = $null }; $cancel.Dispose(); $cancel = $null }
        Check-MutationAdmission ($failed -eq ($scenario -in @('persist-fail','short-write','cancelled-before','cancel-after-persist'))) 'persistence/readback/cancellation failures propagate before dangerous mutation'
        Check-MutationAdmission ($invoked -eq ($scenario -in @('after-crash','preserved'))) 'only verified mutation or preserved no-op completes'
        if ($scenario -notin @('after-crash')) { Check-MutationAdmission ((Get-FileHash -LiteralPath $link).Hash -ceq $originalHash) 'unapplied/failed/cancelled admission never changes the owned Startup bytes' }
        if ($scenario -eq 'tampered') { [IO.File]::WriteAllText($mutationPath, '{}') }
        if ($scenario -eq 'foreign-replaced') {
            $bytes = [IO.File]::ReadAllBytes($link); [IO.File]::Move($link,(Join-Path $case 'retained-original.lnk')); [IO.File]::WriteAllBytes($link,$bytes)
        }
        if ($scenario -eq 'foreign-admission') { [IO.File]::WriteAllText((Join-Path (Split-Path -Parent $journal) 'installer-startup-admission-unknown.json'),'synthetic conflicting admission') }
        if ($scenario -eq 'foreign-runtime') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'),'synthetic external runtime') }
        if ($scenario -eq 'foreign-snapshot') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'),'synthetic external snapshot') }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $expected = if ($scenario -in @('after-crash','preserved')) { 'original-observed' } elseif ($scenario -in @('before-crash','delete-locked','cancel-after-persist')) { 'not-applied' } else { 'blocked' }
        for ($read = 0; $read -lt 3; $read++) { Check-MutationAdmission ((Get-ProGoInstallerStartupMutationState $journal $install $programs $startup) -ceq $expected) 'fresh read-only crash inspection uses actual identity/origin/ownership/receipt/snapshot without replay' }
        $afterRead = @(Get-ChildItem -LiteralPath $case -Recurse -File)
        Check-MutationAdmission ($afterRead.Count -eq $before.Count) 'crash classification creates no resource'
        foreach ($file in $afterRead) { Check-MutationAdmission ((Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'read-only classification preserves every byte' }
        $settled = Complete-ProGoInstallerStartupMutationSettlement $journal $install $programs $startup
        Check-MutationAdmission ($settled -eq ($expected -ceq 'original-observed')) 'settlement records only independently verified original state and never executes mutation'
        if ($settled) { Check-MutationAdmission (Test-ProGoInstallerStartupResult $journal $install $programs $startup) 'durable observed result verifies without full rollback/success authority' }
        foreach ($path in $before.Keys) { Check-MutationAdmission ([IO.File]::Exists($path) -and (Get-FileHash -LiteralPath $path).Hash -ceq $before[$path]) 'settlement never rewrites or deletes existing resources' }
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-MutationAdmission $blocked 'intent/result never permits new installation or Launch'
        if ([IO.File]::Exists($mutationPath)) { $probe = [IO.File]::Open($mutationPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer Startup mutation admission ' + $scenario)
    }
    Write-Host ('Installer Startup mutation admission tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-startup-mutation-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
