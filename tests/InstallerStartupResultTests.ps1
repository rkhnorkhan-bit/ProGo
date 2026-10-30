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
function Check-StartupResult($Condition, $Name) { if (-not $Condition) { throw ('Durable Startup result failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-startup-result-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $cancel = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('created', 'preserved-custom', 'preserved-absence', 'not-rolled-back', 'foreign-startup', 'foreign-menu',
        'snapshot-changed', 'runtime-changed', 'persist-fail', 'short-write', 'conflict', 'tampered', 'missing-origin')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $startup 'ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'preserved-custom') { [IO.File]::WriteAllText($link, 'synthetic customization') }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup ($scenario.StartsWith('preserved-')) -NoStartMenu $false -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        Check-StartupResult ($scenario.StartsWith('preserved-') -or $receipt.OriginIdentity -cmatch '^[A-F0-9]{64}$') 'new creation receipt includes native object origin witness'
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption after durable receipt'
        }
        $interrupted = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $interrupted = $_.Exception.Message.Contains('synthetic interruption after durable receipt') }
        Check-StartupResult $interrupted 'actual writer leaves pending receipt before publication'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $resultPath = Join-Path (Split-Path -Parent $journal) ('installer-startup-outcome-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
        if ($scenario -ne 'not-rolled-back') { Check-StartupResult (Invoke-ProGoInstallerStartupRollback $journal $install $programs $startup) 'existing owned rollback settles before result recording' }
        if ($scenario -eq 'foreign-startup') { [IO.File]::WriteAllText($link, 'synthetic external replacement') }
        if ($scenario -eq 'foreign-menu') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo.lnk'), 'synthetic external replacement') }
        if ($scenario -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic damaged snapshot') }
        if ($scenario -eq 'runtime-changed') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic external runtime') }
        if ($scenario -eq 'missing-origin') {
            $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
            $saved.Completion.Startup.PSObject.Properties.Remove('OriginIdentity')
            [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress))
        }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText($resultPath, 'synthetic foreign outcome') }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $fault = @{ Mode = $scenario }
        function Write-ProGoInstallerStartupResultBytes($Stream, [byte[]]$Bytes) {
            if ($fault.Mode -eq 'persist-fail') { throw 'synthetic durable result failure' }
            if ($fault.Mode -eq 'short-write') { $Stream.WriteByte($Bytes[0]); $Stream.Flush($true); return }
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
        }
        $success = $false
        try { Complete-ProGoInstallerStartupResult $journal $install $programs $startup; $success = $true } catch { }
        $expected = $scenario -in @('created', 'preserved-custom', 'preserved-absence', 'tampered')
        Check-StartupResult ($success -eq $expected) 'only physically verified current result is durably recorded; persist and short writes fail'
        if ($expected) {
            Check-StartupResult (Test-ProGoInstallerStartupResult $journal $install $programs $startup) 'independent bounded reader verifies exact saved proof and current state'
            $saved = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
            Check-StartupResult ($saved.Phase -ceq 'startup-original-observed' -and $saved.FullRollback -is [bool] -and -not $saved.FullRollback -and
                -not $saved.InstallSucceeded -and -not $saved.MutationAllowed -and $saved.RuntimeState -ceq 'new-verified' -and
                $saved.Identity -ceq (Get-ProGoInstallerIdentity $install) -and $saved.ReceiptHash -ceq (Get-FileHash -LiteralPath $pending).Hash) 'durable proof is Startup-only, bound to exact identity/receipt, not full rollback/success/mutation authority'
            if ($scenario -eq 'tampered') { $saved.FullRollback = $true; [IO.File]::WriteAllText($resultPath, ($saved | ConvertTo-Json -Depth 5 -Compress)) }
        }
        Check-StartupResult ((Test-ProGoInstallerStartupResult $journal $install $programs $startup) -eq ($expected -and $scenario -ne 'tampered')) 'incomplete/conflicting/tampered or externally changed evidence never verifies'
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-StartupResult $blocked 'partial auxiliary result or pending install never permits retry'
        if ([IO.File]::Exists($resultPath)) {
            $blocked = $false; try { Resolve-ProGoInstallerPendingInstalled $install -ShortcutScript (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1') -ProgramsDirectory $programs -StartupDirectory $startup } catch { $blocked = $true }
            Check-StartupResult ($blocked -and [IO.File]::Exists($pending)) 'partial auxiliary artifact never implicitly publishes installed success'
        }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) {
            if ($file.FullName -ceq $resultPath -and $scenario -ne 'conflict') { continue }
            Check-StartupResult ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'all runtime receipt snapshot auxiliary private and foreign bytes unchanged'
        }
        foreach ($path in $before.Keys) { Check-StartupResult ([IO.File]::Exists($path)) 'no existing file is deleted by result recording' }
        if ([IO.File]::Exists($resultPath)) { $probe = [IO.File]::Open($resultPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $probe.Dispose() }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer durable Startup result ' + $scenario)
    }
    Write-Host ('Installer durable Startup result tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-startup-result-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
