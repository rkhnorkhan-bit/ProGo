Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $Root 'scripts\Install-ProGo.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer outcome helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
function Check-Outcome($Condition, $Name) { if (-not $Condition) { throw ('Installer outcome test failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-outcome-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($scenario in @('complete', 'fresh', 'persist-fail', 'short', 'after-write-fail', 'foreign-runtime', 'snapshot-corrupt', 'journal-corrupt', 'identity', 'unknown')) {
        $install = Join-Path $work $scenario; New-Item -ItemType Directory -Path $install | Out-Null
        if ($scenario -ne 'fresh') { [IO.File]::WriteAllText((Join-Path $install 'ProGo.exe'), 'synthetic old executable') }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        foreach ($entry in $backup.Entries) { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes }
        $outcomeFixture = @{ Scenario = $scenario }
        function Write-ProGoInstallerOutcomeBytes($Stream, [byte[]]$Bytes) {
            if ($outcomeFixture.Scenario -eq 'persist-fail') { throw 'synthetic outcome persistence failure' }
            $length = if ($outcomeFixture.Scenario -eq 'short') { 1 } else { $Bytes.Length }
            $Stream.Write($Bytes, 0, $length); $Stream.Flush($true)
            if ($outcomeFixture.Scenario -eq 'after-write-fail') { throw 'synthetic outcome failure after flush' }
        }
        $target = Join-Path $install 'ProGo.exe'
        if ($scenario -eq 'foreign-runtime') { [IO.File]::WriteAllText($target, 'synthetic external change') }
        if ($scenario -eq 'snapshot-corrupt') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic corrupt snapshot') }
        if ($scenario -eq 'journal-corrupt') { [IO.File]::WriteAllText((Join-Path $backup.Journal.Directory 'admit-000000.json'), '{}') }
        $before = (Get-FileHash -LiteralPath $target).Hash
        $caught = $false
        try { Complete-ProGoInstallerInstalledOutcome $backup $writes } catch { $caught = $true }
        $completes = $scenario -in @('complete', 'fresh', 'identity', 'unknown')
        Check-Outcome ($caught -eq (-not $completes) -and (Get-FileHash -LiteralPath $target).Hash -ceq $before) 'terminal creation never mutates runtime and refuses unverified ownership/material/persistence'
        $installed = Join-Path $backup.Journal.Directory 'installed.json'
        Check-Outcome ([IO.File]::Exists($installed) -eq $completes) 'only confirmed terminal bytes are atomically published'
        if ($completes) {
            Check-Outcome (Test-ProGoInstallerInstalledOutcome $backup.Journal.Directory $install) 'real snapshot identity and all exact admission proofs accepted'
            if ($scenario -eq 'identity' -or $scenario -eq 'unknown') {
                $terminal = Get-Content -LiteralPath $installed -Raw | ConvertFrom-Json
                if ($scenario -eq 'identity') { $terminal.Identity = ('0' * 64) } else { $terminal.Phase = 'unknown' }
                [IO.File]::WriteAllText($installed, ($terminal | ConvertTo-Json -Depth 8 -Compress))
            }
        }
        $allowed = $scenario -in @('complete', 'fresh')
        $gatePassed = $false
        try { Assert-ProGoInstallerNoPendingJournal $install; $gatePassed = $true } catch { }
        Check-Outcome ($gatePassed -eq $allowed) 'restart recognizes only verified installed outcome, not pending/unknown/other identity'
        if ($allowed) {
            # A genuinely new backup/admission can be created without deleting old journal.
            $nextBackup = New-ProGoInstallerBackup $payload $install
            $nextJournal = New-ProGoInstallerJournal $nextBackup
            Check-Outcome ([IO.Directory]::Exists($nextJournal.Directory) -and [IO.File]::Exists($installed)) 'repeat admitted with old terminal evidence retained'
        }
        if ($scenario -in @('persist-fail', 'short', 'after-write-fail')) {
            Restore-ProGoInstallerRuntime $backup $writes; Assert-ProGoInstallerRuntime $backup -Original
            Check-Outcome (-not (Test-ProGoInstallerInstalledOutcome $backup.Journal.Directory $install)) 'rollback after outcome error never leaves installed terminal'
        }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { Check-Outcome ([IO.File]::ReadAllText((Join-Path $install $leaf)) -ceq 'synthetic private bytes') 'private/foreign bytes unchanged' }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Write-Host ('PASS: installer outcome ' + $scenario)
    }
    Write-Host 'Installer outcome tests PASS: 10'
} finally {
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
