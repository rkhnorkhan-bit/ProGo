Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
. (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1')
Import-ProGoShortcutType
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Durable Menu result helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-MenuResult($Condition, $Name) { if (-not $Condition) { throw ('Durable Menu result failed: case=' + $scenario + '; check=' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-menu-result-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $held = $null; $cancel = $null; $owned = $false
try {
    New-Item -ItemType Directory -Path $work | Out-Null; $owned = $true
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    $scenarios = @('created', 'preserved-custom', 'preserved-absence', 'not-rolled-back', 'foreign-startup', 'foreign-menu',
        'snapshot-changed', 'runtime-changed', 'persist-fail', 'short-write', 'conflict', 'tampered', 'missing-origin', 'legacy-removed', 'post-startup', 'admission-tampered', 'foreign-outcome', 'changed-during-write', 'changed-after-result')
    foreach ($scenario in $scenarios) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'app'
        $programs = Join-Path $case 'Programs'; $startup = Join-Path $case 'Startup'; $link = Join-Path $programs 'ProGo\ProGo.lnk'
        New-Item -ItemType Directory -Path $install, $programs, $startup, (Join-Path $programs 'ProGo') | Out-Null
        [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable')
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        if ($scenario -eq 'preserved-custom') { [IO.File]::WriteAllText($link, 'synthetic customization') }
        if ($scenario -eq 'legacy-removed') {
            $manager = New-Object ProGo.ApplicationShortcuts($install,$programs,$startup)
            $manager.Install($true,$true,$false)
            [IO.File]::Move($link,(Join-Path $programs 'ProGo\ProGo Status.lnk'))
        }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $receipt = Initialize-ProGoShortcuts -InstallDirectory $install -WasInstalled $false -NoStartup $false -NoStartMenu ($scenario.StartsWith('preserved-')) -ProgramsDirectory $programs -StartupDirectory $startup -CaptureStartupEvidence
        Check-MenuResult ($scenario.StartsWith('preserved-') -or $receipt.Menu.OriginIdentity -cmatch '^[A-F0-9]{64}$') 'new creation receipt includes native object origin witness'
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption after durable receipt'
        }
        $interrupted = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes -CompletedPhases @('self-check', 'shortcuts') -StartupEvidence $receipt -RequireStartupEvidence }
        catch { $interrupted = $_.Exception.Message.Contains('synthetic interruption after durable receipt') }
        Check-MenuResult $interrupted 'actual writer leaves pending receipt before publication'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'outcome.pending.json'
        $resultPath = Join-Path (Split-Path -Parent $journal) ('installer-menu-outcome-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
        if ($scenario -eq 'post-startup') {
            Check-MenuResult (Invoke-ProGoInstallerStartupRollback $journal $install $programs $startup) 'real Startup rollback precedes Menu mutation'
            Check-MenuResult (Complete-ProGoInstallerStartupMutationSettlement $journal $install $programs $startup) 'real durable Startup settlement independently verifies'
        }
        if ($scenario -ne 'not-rolled-back') { Check-MenuResult (Invoke-ProGoInstallerMenuRollback $journal $install $programs $startup) 'existing owned rollback settles before result recording' }
        if ($scenario -eq 'foreign-startup') { [IO.File]::WriteAllText((Join-Path $startup 'ProGo.lnk'), 'synthetic external replacement') }
        if ($scenario -eq 'foreign-menu') { [IO.File]::WriteAllText((Join-Path $programs 'ProGo\ProGo.lnk'), 'synthetic external replacement') }
        if ($scenario -eq 'snapshot-changed') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic damaged snapshot') }
        if ($scenario -eq 'runtime-changed') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic external runtime') }
        if ($scenario -eq 'missing-origin') {
            $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
            $saved.Completion.Startup.Menu.PSObject.Properties.Remove('OriginIdentity')
            [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress))
        }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText($resultPath, 'synthetic foreign outcome') }
        $admissionPath = Join-Path (Split-Path -Parent $journal) ('installer-menu-admission-' + (Split-Path -Leaf $journal).Substring('installer-journal-'.Length) + '.json')
        if ($scenario -eq 'admission-tampered') { [IO.File]::WriteAllText($admissionPath,'{}') }
        if ($scenario -eq 'foreign-outcome') { [IO.File]::WriteAllText((Join-Path (Split-Path -Parent $journal) 'installer-menu-outcome-unknown.json'),'synthetic foreign outcome') }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        $fault = @{ Mode = $scenario }
        function Write-ProGoInstallerMenuResultBytes($Stream, [byte[]]$Bytes) {
            if ($fault.Mode -eq 'persist-fail') { throw 'synthetic durable result failure' }
            if ($fault.Mode -eq 'short-write') { $Stream.WriteByte($Bytes[0]); $Stream.Flush($true); return }
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            if ($fault.Mode -eq 'changed-during-write') { [IO.File]::WriteAllText($link,'synthetic concurrent replacement') }
        }
        $success = $false
        try { Complete-ProGoInstallerMenuResult $journal $install $programs $startup; $success = $true } catch { }
        $expected = $scenario -in @('created', 'preserved-custom', 'preserved-absence', 'tampered', 'legacy-removed', 'post-startup', 'changed-after-result')
        Check-MenuResult ($success -eq $expected) 'only physically verified current result is durably recorded; persist and short writes fail'
        if ($expected) {
            Check-MenuResult (Test-ProGoInstallerMenuResult $journal $install $programs $startup) 'independent bounded reader verifies exact saved proof and current state'
            $saved = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
            Check-MenuResult ($saved.Phase -ceq 'menu-original-observed' -and $saved.FullRollback -is [bool] -and -not $saved.FullRollback -and
                -not $saved.InstallSucceeded -and -not $saved.MutationAllowed -and $saved.RuntimeState -ceq 'new-verified' -and
                $saved.Identity -ceq (Get-ProGoInstallerIdentity $install) -and $saved.ReceiptHash -ceq (Get-FileHash -LiteralPath $pending).Hash -and
                $saved.AdmissionHash -ceq (Get-FileHash -LiteralPath $admissionPath).Hash) 'durable proof is Menu-only, bound to exact identity/receipt, not full rollback/success/mutation authority'
            if ($scenario -eq 'created') {
                $resultHash = (Get-FileHash -LiteralPath $resultPath).Hash; $duplicateRefused = $false
                try { Complete-ProGoInstallerMenuResult $journal $install $programs $startup } catch { $duplicateRefused = $true }
                Check-MenuResult ($duplicateRefused -and (Get-FileHash -LiteralPath $resultPath).Hash -ceq $resultHash) 'existing verified result is never overwritten even by duplicate publication'
            }
            if ($scenario -eq 'changed-after-result') { [IO.File]::WriteAllText($link,'synthetic concurrent replacement') }
            if ($scenario -eq 'tampered') { $saved.FullRollback = $true; [IO.File]::WriteAllText($resultPath, ($saved | ConvertTo-Json -Depth 5 -Compress)) }
        }
        Check-MenuResult ((Test-ProGoInstallerMenuResult $journal $install $programs $startup) -eq ($expected -and $scenario -notin @('tampered','changed-after-result'))) 'incomplete/conflicting/tampered or externally changed evidence never verifies'
        $blocked = $false; try { Assert-ProGoInstallerNoPendingJournal $install } catch { $blocked = $true }
        Check-MenuResult $blocked 'partial auxiliary result or pending install never permits retry'
        if ([IO.File]::Exists($resultPath)) {
            $blocked = $false; try { Resolve-ProGoInstallerPendingInstalled $install -ShortcutScript (Join-Path $Root 'scripts\Shortcuts-ProGo.ps1') -ProgramsDirectory $programs -StartupDirectory $startup } catch { $blocked = $true }
            Check-MenuResult ($blocked -and [IO.File]::Exists($pending)) 'partial auxiliary artifact never implicitly publishes installed success'
        }
        foreach ($file in Get-ChildItem -LiteralPath $case -Recurse -File) {
            if ($scenario -in @('changed-during-write','changed-after-result') -and $file.FullName -ceq $link) {
                Check-MenuResult ([IO.File]::ReadAllText($link) -ceq 'synthetic concurrent replacement') 'concurrent external replacement is preserved rather than repaired'
                continue
            }
            if ($file.FullName -ceq $resultPath -and $scenario -ne 'conflict') { continue }
            Check-MenuResult ($before.ContainsKey($file.FullName) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$file.FullName]) 'all runtime receipt snapshot auxiliary private and foreign bytes unchanged'
        }
        foreach ($path in $before.Keys) { Check-MenuResult ([IO.File]::Exists($path)) 'no existing file is deleted by result recording' }
        if ([IO.File]::Exists($resultPath)) { $probe = [IO.File]::Open($resultPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $probe.Dispose() }
        if ([IO.File]::Exists($admissionPath)) { $probe = [IO.File]::Open($admissionPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        $probe = [IO.File]::Open($pending,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose()
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer durable Menu result ' + $scenario)
    }
    Write-Host ('Installer durable Menu result tests PASS: ' + $scenarios.Count)
} finally {
    if ($null -ne $held) { $held.Dispose() }
    if ($null -ne $cancel) { $cancel.Dispose() }
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if ($owned -and [IO.Path]::GetFileName($work) -cmatch '^ProGo-menu-result-[a-f0-9]{32}$' -and
        [IO.Path]::GetDirectoryName($work) -ceq [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) -and
        (Test-Path -LiteralPath $work)) {
        if ((Get-Item -LiteralPath $work -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture cleanup refuses reparse root.' }
        if (@(Get-ChildItem -LiteralPath $work -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'Fixture cleanup refuses reparse descendants.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
