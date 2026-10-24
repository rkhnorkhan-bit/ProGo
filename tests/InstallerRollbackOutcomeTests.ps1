Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer rollback outcome helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-RollbackOutcome($Condition, $Name) { if (-not $Condition) { throw ('Installer rollback outcome failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-rollback-outcome-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($scenario in @('existing', 'fresh', 'partial', 'unrestored', 'foreign', 'snapshot', 'ownership', 'record-corrupt', 'persist-fail', 'short', 'after-write-fail', 'identity')) {
        $install = Join-Path $work $scenario; New-Item -ItemType Directory -Path $install | Out-Null
        $target = Join-Path $install 'ProGo.exe'
        if ($scenario -ne 'fresh') { [IO.File]::WriteAllText($target, 'synthetic original bytes') }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        $rollbackOutcomeFixture = @{ Scenario = $scenario }
        function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
            $Target.SetLength(0)
            if ($rollbackOutcomeFixture.Scenario -eq 'partial') { $Target.WriteByte(42); throw 'synthetic partial runtime failure' }
            $Source.CopyTo($Target); $Target.Flush($true)
        }
        function Write-ProGoInstallerRollbackOutcomeBytes($Stream, [byte[]]$Bytes) {
            if ($rollbackOutcomeFixture.Scenario -eq 'persist-fail') { throw 'synthetic rollback terminal refusal' }
            $count = if ($rollbackOutcomeFixture.Scenario -eq 'short') { 1 } else { $Bytes.Length }
            $Stream.Write($Bytes, 0, $count); $Stream.Flush($true)
            if ($rollbackOutcomeFixture.Scenario -eq 'after-write-fail') { throw 'synthetic rollback terminal refusal after flush' }
        }
        $entry = @($backup.Entries | Where-Object { $_.Relative -eq 'ProGo.exe' })[0]
        try { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes } catch {
            if ($scenario -ne 'partial' -or -not $_.Exception.Message.Contains('synthetic partial runtime failure')) { throw }
        }
        if ($scenario -ne 'unrestored') { Restore-ProGoInstallerRuntime $backup $writes; Assert-ProGoInstallerRuntime $backup -Original }
        if ($scenario -eq 'foreign') { [IO.File]::WriteAllText($target, 'synthetic external change after restore') }
        if ($scenario -eq 'snapshot') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic invalid snapshot') }
        if ($scenario -eq 'ownership') { $writes[0].OwnedHash = $null }
        if ($scenario -eq 'record-corrupt') { [IO.File]::WriteAllText((Join-Path $backup.Journal.Directory 'admit-000000.json'), '{}') }
        $before = if ([IO.File]::Exists($target)) { (Get-FileHash -LiteralPath $target).Hash } else { $null }
        $caught = $false
        try { Complete-ProGoInstallerRollbackOutcome $backup $writes } catch { $caught = $true }
        $publishes = $scenario -in @('existing', 'fresh', 'partial', 'identity')
        $terminal = Join-Path $backup.Journal.Directory 'rolled-back.json'
        Check-RollbackOutcome ($caught -eq (-not $publishes) -and [IO.File]::Exists($terminal) -eq $publishes -and
            -not [IO.File]::Exists((Join-Path $backup.Journal.Directory 'installed.json'))) 'rollback terminal is separate from install success and requires complete proof'
        $after = if ([IO.File]::Exists($target)) { (Get-FileHash -LiteralPath $target).Hash } else { $null }
        Check-RollbackOutcome ($before -ceq $after) 'terminal writer does not restore or overwrite runtime/foreign bytes'
        if ($publishes) {
            Check-RollbackOutcome (Test-ProGoInstallerRollbackOutcome $backup.Journal.Directory $install) 'durable exact snapshot/identity/admission/ownership evidence recognized'
            if ($scenario -eq 'identity') {
                $saved = Get-Content -LiteralPath $terminal -Raw | ConvertFrom-Json
                $saved.Identity = ('0' * 64); [IO.File]::WriteAllText($terminal, ($saved | ConvertTo-Json -Depth 8 -Compress))
            }
        }
        $allowed = $scenario -in @('existing', 'fresh', 'partial'); $passed = $false
        try { Assert-ProGoInstallerNoPendingJournal $install; $passed = $true } catch { }
        Check-RollbackOutcome ($passed -eq $allowed) 'restart permits only verified rollback, all pending/invalid/identity failures stay closed'
        if ($allowed) {
            Assert-ProGoInstallerRuntime $backup -Original
            $next = New-ProGoInstallerBackup $payload $install; $nextJournal = New-ProGoInstallerJournal $next
            Check-RollbackOutcome ([IO.Directory]::Exists($nextJournal.Directory) -and [IO.File]::Exists($terminal)) 'explicit new attempt admitted without replay/deleting rollback evidence'
        }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-RollbackOutcome ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private and foreign bytes preserved' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer rollback outcome ' + $scenario)
    }
    Write-Host 'Installer rollback outcome tests PASS: 12'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
