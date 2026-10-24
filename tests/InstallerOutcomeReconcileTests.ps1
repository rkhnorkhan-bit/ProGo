Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$installer = Join-Path $Root 'scripts\Install-ProGo.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($installer, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Installer reconcile helpers do not parse.' }
foreach ($function in $ast.FindAll({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -notin @('Enter-ProGoInstallerMaintenance', 'Stop-ProGoInstallerApplication')
}, $false)) { . ([scriptblock]::Create($function.Extent.Text)) }
$boundary = @($ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -eq '$InstallDir' })
if ($boundary.Count -ne 1) { throw 'Installer reconcile boundary is ambiguous.' }
$body = [scriptblock]::Create((Get-Content -LiteralPath $installer -Raw).Substring($boundary[0].Extent.StartOffset))
function Check-Reconcile($Condition, $Name) { if (-not $Condition) { throw ('Installer reconcile failed: ' + $Name) } }
$work = Join-Path ([IO.Path]::GetTempPath()) ('ProGo-reconcile-tests-' + [Guid]::NewGuid().ToString('N'))
$stage = $null; $oldLocal = $env:LOCALAPPDATA
try {
    New-Item -ItemType Directory -Path $work | Out-Null
    $payload = Get-ProGoInstallPayload -Directory (Join-Path $Root 'release')
    foreach ($scenario in @('existing', 'fresh', 'partial', 'corrupt', 'snapshot', 'foreign', 'absence', 'identity', 'ownership', 'conflict', 'intent-only', 'pending-installed', 'multiple')) {
        $case = Join-Path $work $scenario; $install = Join-Path $case 'ProGo'
        New-Item -ItemType Directory -Path $install | Out-Null; $env:LOCALAPPDATA = $case
        $target = Join-Path $install 'ProGo.exe'
        if ($scenario -notin @('fresh', 'absence')) { [IO.File]::WriteAllText($target, 'synthetic old bytes') }
        foreach ($leaf in @('settings.json', 'vault.enc', 'key-reference.txt', 'foreign.txt')) { [IO.File]::WriteAllText((Join-Path $install $leaf), 'synthetic private bytes') }
        $backup = New-ProGoInstallerBackup $payload $install; $stage = New-ProGoInstallerStage $payload
        $writes = New-Object 'System.Collections.Generic.List[object]'
        $reconcileFixture = @{ Scenario = $scenario }
        function Write-ProGoInstallerRuntimeBytes($Source, $Target) {
            $Target.SetLength(0)
            if ($reconcileFixture.Scenario -eq 'partial') { $Target.WriteByte(42); throw 'synthetic partial failure' }
            $Source.CopyTo($Target); $Target.Flush($true)
        }
        function Write-ProGoInstallerRollbackOutcomeBytes($Stream, [byte[]]$Bytes) {
            $Stream.Write($Bytes, 0, $Bytes.Length); $Stream.Flush($true)
            throw 'synthetic interruption before terminal publication'
        }
        $entry = @($backup.Entries | Where-Object { $_.Relative -eq 'ProGo.exe' })[0]
        try { Set-ProGoInstallerRuntimeFile $stage $backup $entry $writes } catch {
            if ($scenario -ne 'partial' -or -not $_.Exception.Message.Contains('synthetic partial failure')) { throw }
        }
        Restore-ProGoInstallerRuntime $backup $writes; Assert-ProGoInstallerRuntime $backup -Original
        $interrupted = $false
        try { Complete-ProGoInstallerRollbackOutcome $backup $writes } catch { $interrupted = $_.Exception.Message.Contains('synthetic interruption before terminal publication') }
        Check-Reconcile $interrupted 'actual durable pending outcome exists without published terminal'
        $journal = $backup.Journal.Directory; $pending = Join-Path $journal 'rollback.pending.json'; $terminal = Join-Path $journal 'rolled-back.json'
        $saved = Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json
        if ($scenario -eq 'corrupt') { [IO.File]::WriteAllText($pending, '{invalid') }
        if ($scenario -eq 'snapshot') { [IO.File]::WriteAllText((Join-Path $backup.Directory 'ProGo.exe'), 'synthetic damaged snapshot') }
        if ($scenario -eq 'foreign') { [IO.File]::WriteAllText($target, 'synthetic externally changed bytes') }
        if ($scenario -eq 'absence') { [IO.File]::WriteAllText((Join-Path $install 'VERSION'), 'synthetic foreign leaf in prior absence') }
        if ($scenario -eq 'identity') { $saved.Identity = ('0' * 64); [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress)) }
        if ($scenario -eq 'ownership') { $saved.Ownership[0].OwnedHash = $null; [IO.File]::WriteAllText($pending, ($saved | ConvertTo-Json -Depth 8 -Compress)) }
        if ($scenario -eq 'conflict') { [IO.File]::WriteAllText((Join-Path $journal 'installed.json'), '{}') }
        if ($scenario -eq 'intent-only') { [IO.File]::Delete($pending) }
        if ($scenario -eq 'pending-installed') { [IO.File]::Move($pending, (Join-Path $journal 'outcome.pending.json')) }
        if ($scenario -eq 'multiple') {
            $second = Join-Path (Split-Path -Parent $journal) ('installer-journal-' + [Guid]::NewGuid().ToString('N'))
            Microsoft.PowerShell.Management\Copy-Item -LiteralPath $journal -Destination $second -Recurse
            $saved.Journal = Split-Path -Leaf $second
            [IO.File]::WriteAllText((Join-Path $second 'rollback.pending.json'), ($saved | ConvertTo-Json -Depth 8 -Compress))
            Check-Reconcile (Test-ProGoInstallerRollbackOutcome $second $install 'rollback.pending.json') 'multiple case contains a second independently valid pending proof'
        }
        $before = @{}; foreach ($file in Get-ChildItem -LiteralPath $install -Recurse -File) { $before[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
        # New actual installer invocation, no old in-memory write/admission passed to it.
        $newAttempt = @{ Stops = 0; Disposes = 0; Launches = 0; Success = $false }
        function Enter-ProGoInstallerMaintenance {
            $lease = [pscustomobject]@{ State = $newAttempt }
            $lease | Add-Member -MemberType ScriptMethod -Name Dispose -Value { $this.State.Disposes++ }
            return $lease
        }
        function Stop-ProGoInstallerApplication($InstallDirectory) { $newAttempt.Stops++; throw 'synthetic new attempt reached Stop' }
        function Start-Process { $newAttempt.Launches++; throw 'unexpected launch' }
        function Write-Host($Object) { if ($Object -like 'Install OK:*') { $newAttempt.Success = $true } }
        $caught = $false; $reason = $null
        try { & $body } catch { $caught = $true; $reason = $_.Exception.Message }
        $allowed = $scenario -in @('existing', 'fresh', 'partial')
        Check-Reconcile ($caught -and $newAttempt.Stops -eq [int]$allowed -and $newAttempt.Disposes -eq 1 -and
            $newAttempt.Launches -eq 0 -and -not $newAttempt.Success) 'real new installer reconciles under owner before Stop and always preserves failure/no Launch'
        Check-Reconcile ([IO.File]::Exists($terminal) -eq $allowed) 'only verified old bytes/absence and unique known pending proof publish rollback metadata'
        if ($allowed) {
            Check-Reconcile ($reason -ceq 'synthetic new attempt reached Stop' -and -not [IO.File]::Exists($pending) -and
                (Get-FileHash -LiteralPath $terminal).Hash -ceq $before[$pending]) 'metadata move preserves exact evidence bytes, no runtime restoration'
            Assert-ProGoInstallerNoPendingJournal $install
        }
        $after = @(Get-ChildItem -LiteralPath $install -Recurse -File)
        Check-Reconcile ($after.Count -eq $before.Count) 'file count preserved'
        foreach ($file in $after) {
            $prior = if ($allowed -and $file.FullName -ceq $terminal) { $pending } else { $file.FullName }
            Check-Reconcile ($before.ContainsKey($prior) -and (Get-FileHash -LiteralPath $file.FullName).Hash -ceq $before[$prior]) 'runtime/private/foreign/snapshot/other journal bytes untouched'
        }
        Remove-ProGoInstallerStage $stage; $stage = $null
        Microsoft.PowerShell.Utility\Write-Host ('PASS: installer outcome reconcile ' + $scenario)
    }
    Microsoft.PowerShell.Utility\Write-Host 'Installer outcome reconcile tests PASS: 13'
} finally {
    $env:LOCALAPPDATA = $oldLocal
    if ($null -ne $stage) { Remove-ProGoInstallerStage $stage }
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
